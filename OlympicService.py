import json
import os
import re
import sqlite3
import time
import uuid
from contextlib import closing

from flask import jsonify


def missing_locked_creatures(trade, username, game, decode_game_save):
    owner = trade["normal_id"](username)
    with closing(trade["connect"]()) as conn:
        rows = conn.execute(
            "SELECT DISTINCT unique_id FROM olympic_creatures WHERE owner_id=?", (owner,),
        ).fetchall()
    locked = {row["unique_id"] for row in rows}
    if not locked:
        return set()
    save, _ = decode_game_save(game)
    if not isinstance(save, dict) or not isinstance(save.get("Inventory"), list):
        raise ValueError("Player inventory is unavailable")
    owned = set()
    for item in save["Inventory"]:
        if isinstance(item, dict) and item.get("_T") == "CR":
            value = item.get("UniqueID")
            if not isinstance(value, bool) and re.fullmatch(r"[0-9]+", str(value)):
                owned.add(int(value))
    return locked - owned


def has_pending_currency(trade, username):
    with closing(trade["connect"]()) as conn:
        return conn.execute(
            "SELECT 1 FROM olympic_currency_pending WHERE owner_id=?",
            (trade["normal_id"](username),),
        ).fetchone() is not None


def clear_pending_currency(trade, username):
    with closing(trade["connect"]()) as conn, conn:
        conn.execute("DELETE FROM olympic_currency_pending WHERE owner_id=?",
                     (trade["normal_id"](username),))


def matching_currency(incoming, stored, decode_game_save):
    new_save, _ = decode_game_save(incoming)
    old_save, _ = decode_game_save(stored)
    for key in ("PaidHardCurrency", "FreeHardCurrency"):
        new_value = new_save.get(key)
        old_value = old_save.get(key)
        if isinstance(new_value, bool) or isinstance(old_value, bool):
            return False
        if not re.fullmatch(r"[0-9]+", str(new_value)) or not re.fullmatch(r"[0-9]+", str(old_value)):
            return False
        if int(new_value) != int(old_value):
            return False
    return True


def register_olympic_routes(app, trade, decode_game_save, encode_game_save):
    blueprint = os.path.join(app.root_path, "data", "persist", "blueprints")

    def catalog(name):
        with open(os.path.join(blueprint, name), encoding="utf-8") as file:
            return json.load(file)

    ranks = catalog("db_XPTables.json")
    thresholds = [int(row["PlayerRank"]) for row in ranks if int(row["Level"]) == 70]
    if len(thresholds) != 1:
        raise ValueError("Olympic rank 70 XP threshold is missing or ambiguous")
    rank_threshold = thresholds[0]
    cakes = {row["ID"] for row in catalog("db_XPMaterials.json")}
    shards = {
        row["ID"] for row in catalog("db_EvoMaterials.json")
        if str(row.get("AwakenMat", "")).lower() == "true"
    }
    keys = {row["ID"] for row in catalog("db_GachaSlots.json") if row.get("KeyName")}
    prices = {
        "trade": {100: 3000, 300: 5000, 500: 7000, 1000: 11000},
        "craft": {1: 1500, 3: 2500, 6: 4000},
        "pvp": {100: 300, 250: 500, 500: 750},
        "cake": {3: 300, 10: 1000},
        "key": {1: 500},
        "shard": {1: 2000, 3: 5000},
    }
    edit_prices = {
        "trade": (100, 2500), "craft": (1, 3000), "pvp": (100, 500),
        "cake": (1, 500), "key": (1, 500), "shard": (1, 3000),
    }

    with closing(trade["connect"]()) as conn, conn:
        conn.executescript("""
            CREATE TABLE IF NOT EXISTS olympic_dungeons (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                owner_id TEXT NOT NULL,
                owner_name TEXT NOT NULL,
                name TEXT NOT NULL,
                rewards TEXT NOT NULL,
                battle_count INTEGER NOT NULL DEFAULT 0,
                revision INTEGER NOT NULL DEFAULT 1,
                created_at INTEGER NOT NULL,
                updated_at INTEGER NOT NULL
            );
            CREATE INDEX IF NOT EXISTS idx_olympic_owner
                ON olympic_dungeons(owner_id, id);
            CREATE TABLE IF NOT EXISTS olympic_operations (
                owner_id TEXT NOT NULL,
                request_id TEXT NOT NULL,
                operation TEXT NOT NULL,
                payload TEXT NOT NULL,
                response TEXT NOT NULL,
                PRIMARY KEY(owner_id, request_id)
            );
            CREATE TABLE IF NOT EXISTS olympic_currency_pending (
                owner_id TEXT PRIMARY KEY
            );
        """)
        conn.execute("BEGIN IMMEDIATE")
        columns = {row["name"] for row in conn.execute("PRAGMA table_info(olympic_dungeons)")}
        if "leader_id" not in columns:
            conn.execute("ALTER TABLE olympic_dungeons ADD COLUMN leader_id TEXT")
        if "leader_skin" not in columns:
            conn.execute("ALTER TABLE olympic_dungeons ADD COLUMN leader_skin TEXT")

    def fail(message, status=400):
        return jsonify({"success": False, "error": message}), status

    def success(data):
        return jsonify({"success": True, "data": data})

    def integer(value, minimum=0, maximum=2147483647):
        if isinstance(value, bool) or not re.fullmatch(r"[0-9]+", str(value)):
            raise ValueError("Expected a non-negative integer")
        number = int(value)
        if number < minimum or number > maximum:
            raise ValueError("Integer is outside the allowed range")
        return number

    def json_value(value):
        if isinstance(value, str):
            try:
                return json.loads(value)
            except json.JSONDecodeError as error:
                raise ValueError("Invalid JSON") from error
        return value

    def reward_list(value, edit=False):
        value = json_value(value)
        if not isinstance(value, list) or not 1 <= len(value) <= 6:
            raise ValueError("Choose between one and six reward types")
        result = []
        seen = set()
        for item in value:
            if not isinstance(item, dict):
                raise ValueError("Invalid reward")
            kind = item.get("type")
            if kind not in prices or kind in seen:
                raise ValueError("Invalid or duplicate reward type")
            seen.add(kind)
            amount = integer(item.get("amount"), 1)
            if (edit and amount % edit_prices[kind][0] and not (kind == "pvp" and amount == 250)) or (not edit and amount not in prices[kind]):
                raise ValueError("Invalid reward amount")
            item_id = item.get("item_id", "")
            valid_ids = {"cake": cakes, "shard": shards, "key": keys}
            if kind in valid_ids:
                if not isinstance(item_id, str) or item_id not in valid_ids[kind]:
                    raise ValueError("Unknown reward item")
            elif item_id:
                raise ValueError("Currency rewards cannot have an item ID")
            result.append({"type": kind, "amount": amount, "item_id": item_id})
        return result

    def creature_list(value):
        value = json_value(value)
        if not isinstance(value, list) or len(value) != 12:
            raise ValueError("Exactly twelve creatures are required")
        result = [integer(uid, 1) for uid in value]
        if len(set(result)) != 12:
            raise ValueError("Each creature must be different")
        return result

    def inventory(save):
        items = save.get("Inventory")
        if not isinstance(items, list):
            raise ValueError("Player inventory is unavailable")
        return items

    def selected_leader(save):
        loadouts = save.get("Loadouts")
        try:
            selected = integer(save.get("SelectedLoadout", 0))
        except ValueError:
            return None
        if not isinstance(loadouts, list) or selected >= len(loadouts) or not isinstance(loadouts[selected], dict):
            return None
        leader_id = loadouts[selected].get("LeaderID")
        leaders = save.get("Leaders")
        if not isinstance(leader_id, str) or not leader_id or not isinstance(leaders, list):
            return None
        leader = next((item for item in leaders if isinstance(item, dict) and item.get("ID") == leader_id), None)
        if leader is None:
            return None
        skin = leader.get("Skin") or leader_id
        owned_skins = leader.get("OwnedSkins", [])
        if not isinstance(skin, str) or (skin != leader_id and
                                        (not isinstance(owned_skins, list) or skin not in owned_skins)):
            app.logger.warning("[Olympic] Creator's selected leader skin is unavailable")
            skin = leader_id
        return {"id": leader_id, "skin": skin}

    def requested_leader(save, leader_id):
        if not isinstance(leader_id, str) or not leader_id:
            raise ValueError("Choose an owned leader")
        leaders = save.get("Leaders")
        if not isinstance(leaders, list):
            raise ValueError("Player leaders are unavailable")
        leader = next((item for item in leaders if isinstance(item, dict) and item.get("ID") == leader_id), None)
        if leader is None:
            raise ValueError("The selected leader is not owned")
        skin = leader.get("Skin") or leader_id
        owned_skins = leader.get("OwnedSkins", [])
        if not isinstance(skin, str) or (skin != leader_id and
                                        (not isinstance(owned_skins, list) or skin not in owned_skins)):
            skin = leader_id
        return {"id": leader_id, "skin": skin}

    def cost(rewards, previous=None):
        old = {(row["type"], row["item_id"]): row["amount"] for row in previous or []}
        total = 0
        for row in rewards:
            old_amount = old.get((row["type"], row["item_id"]), 0)
            previous_price = prices[row["type"]].get(old_amount, 0)
            total += max(0, prices[row["type"]][row["amount"]] - previous_price)
        return total

    def edit_cost(rewards, previous):
        old = {(row["type"], row["item_id"]): row["amount"] for row in previous}
        total = 0
        for row in rewards:
            unit, price = edit_prices[row["type"]]
            amount = row["amount"]
            old_amount = old.get((row["type"], row["item_id"]), 0)
            new_units = (amount + unit - 1) // unit
            old_units = (old_amount + unit - 1) // unit
            total += max(0, new_units - old_units) * price
        return total

    def payload():
        data = trade["request_data"]()
        request_id = data.get("request_id")
        try:
            uuid.UUID(str(request_id))
        except (ValueError, AttributeError):
            raise ValueError("A valid request_id is required")
        return data, str(request_id)

    def current_player():
        player_id, player, failure = trade["current_player"]()
        if failure:
            return None, None, failure
        if player is None:
            return None, None, fail("Player not found", 404)
        return player_id, player, None

    def open_transaction(player):
        conn = trade["connect"]()
        try:
            conn.execute("ATTACH DATABASE ? AS playerdb", (player["path"],))
            conn.execute("BEGIN IMMEDIATE")
            row = conn.execute('SELECT game FROM playerdb."player" WHERE username=?',
                               (player["username"],)).fetchone()
            if row is None:
                raise ValueError("Player not found")
            game = row["game"]
            save, save_format = decode_game_save(game)
            if not isinstance(save, dict):
                raise ValueError("Player save is unavailable")
            if integer(save.get("RankXP")) < rank_threshold:
                raise ValueError("Olympic unlocks at level 70")
            return conn, game, save, save_format
        except Exception:
            conn.close()
            raise

    def operation(conn, owner, request_id, action, canonical):
        row = conn.execute(
            "SELECT operation, payload, response FROM olympic_operations WHERE owner_id=? AND request_id=?",
            (owner, request_id),
        ).fetchone()
        if row is None:
            return None
        if row["operation"] != action or row["payload"] != canonical:
            raise ValueError("request_id was already used for a different operation")
        return json.loads(row["response"])

    def store_operation(conn, owner, request_id, action, canonical, response):
        conn.execute(
            "INSERT INTO olympic_operations VALUES (?, ?, ?, ?, ?)",
            (owner, request_id, action, canonical, json.dumps(response)),
        )

    def change_dungeon(edit):
        owner, player, failure = current_player()
        if failure:
            return failure
        try:
            data, request_id = payload()
            name = data.get("name")
            if not isinstance(name, str) or not name.strip() or len(name.strip()) > 60:
                raise ValueError("Dungeon name must contain 1 to 60 characters")
            name = name.strip()
            if any(ord(ch) < 32 or ch in "[]" for ch in name):
                raise ValueError("Dungeon name contains invalid characters")
            creatures = creature_list(data.get("creatures"))
            rewards = reward_list(data.get("rewards"), edit)
            dungeon_id = integer(data.get("id"), 1) if edit else None
            action = "edit" if edit else "create"
            leader_id = data.get("leader_id")
            expected_cost = integer(data.get("expected_cost"), 1) if not edit else None
            canonical_values = [dungeon_id, name, creatures, rewards, leader_id]
            if not edit:
                canonical_values.append(expected_cost)
            canonical = json.dumps(canonical_values, ensure_ascii=False,
                                   sort_keys=True, separators=(",", ":"))
            with trade["lock"]:
                conn, game, save, save_format = open_transaction(player)
                try:
                    prior = operation(conn, owner, request_id, action, canonical)
                    if prior is not None:
                        prior["paid_hard_currency"] = integer(save.get("PaidHardCurrency", 0))
                        prior["free_hard_currency"] = integer(save.get("FreeHardCurrency", 0))
                        conn.rollback()
                        return success(prior)
                    old_rewards = []
                    leader = requested_leader(save, leader_id)
                    if edit:
                        row = conn.execute(
                            "SELECT rewards FROM olympic_dungeons WHERE id=? AND owner_id=?",
                            (dungeon_id, owner),
                        ).fetchone()
                        if row is None:
                            conn.rollback()
                            return fail("Dungeon not found", 404)
                        old_rewards = json.loads(row["rewards"])
                    owned = {
                        integer(item.get("UniqueID"), 1): item
                        for item in inventory(save)
                        if isinstance(item, dict) and item.get("_T") == "CR"
                    }
                    for uid in creatures:
                        if uid not in owned or not owned[uid].get("ID"):
                            raise ValueError("A selected creature is not owned")
                        if trade["reserved"](conn, owner, uid, include_olympic=False):
                            raise ValueError("A selected creature is reserved for a trade")
                    amount = (200 + edit_cost(rewards, old_rewards)) if edit else (500 + cost(rewards))
                    if not edit and amount != expected_cost:
                        raise ValueError("Quest cost changed; review the price and try again")
                    paid = integer(save.get("PaidHardCurrency", 0))
                    free = integer(save.get("FreeHardCurrency", 0))
                    if paid + free < amount:
                        conn.rollback()
                        return fail("Not enough HardCurrency", 409)
                    paid_spent = min(paid, amount)
                    paid -= paid_spent
                    free -= amount - paid_spent
                    save["PaidHardCurrency"] = paid
                    save["FreeHardCurrency"] = free
                    encoded = encode_game_save(game, save, save_format)
                    changed = conn.execute(
                        'UPDATE playerdb."player" SET game=? WHERE username=? AND game=?',
                        (encoded, player["username"], game),
                    ).rowcount
                    if changed != 1:
                        raise ValueError("Player save changed; retry")
                    conn.execute(
                        "INSERT OR IGNORE INTO olympic_currency_pending(owner_id) VALUES (?)", (owner,),
                    )
                    now = int(time.time())
                    reward_json = json.dumps(rewards, ensure_ascii=False, separators=(",", ":"))
                    if edit:
                        conn.execute(
                            """UPDATE olympic_dungeons
                               SET name=?, rewards=?, leader_id=?, leader_skin=?,
                                   revision=revision+1, updated_at=?
                               WHERE id=? AND owner_id=?""",
                            (name, reward_json, leader["id"], leader["skin"], now, dungeon_id, owner),
                        )
                        conn.execute("DELETE FROM olympic_creatures WHERE dungeon_id=?", (dungeon_id,))
                    else:
                        cursor = conn.execute(
                            """INSERT INTO olympic_dungeons
                               (owner_id, owner_name, name, rewards, created_at, updated_at, leader_id, leader_skin)
                               VALUES (?, ?, ?, ?, ?, ?, ?, ?)""",
                            (owner, player["multiplayer_name"], name, reward_json, now, now,
                             leader["id"], leader["skin"]),
                        )
                        dungeon_id = cursor.lastrowid
                    conn.executemany(
                        "INSERT INTO olympic_creatures(dungeon_id, owner_id, slot, unique_id) VALUES (?, ?, ?, ?)",
                        [(dungeon_id, owner, index, uid) for index, uid in enumerate(creatures)],
                    )
                    response = {"id": dungeon_id, "cost": amount,
                                "paid_hard_currency": paid, "free_hard_currency": free}
                    store_operation(conn, owner, request_id, action, canonical, response)
                    conn.commit()
                    return success(response)
                except Exception:
                    conn.rollback()
                    raise
                finally:
                    conn.close()
        except ValueError as error:
            return fail(str(error))
        except sqlite3.Error:
            app.logger.exception("[Olympic] Could not save dungeon")
            return fail("Could not save dungeon", 500)

    @app.route("/persist/olympic_createDW/", methods=["POST"])
    def OlympicCreateDW():
        return change_dungeon(False)

    @app.route("/persist/olympic_create_statusDW/", methods=["POST"])
    def OlympicCreateStatusDW():
        owner, player, failure = current_player()
        if failure:
            return failure
        try:
            _, request_id = payload()
            with closing(trade["connect"]()) as conn:
                row = conn.execute(
                    "SELECT operation, response FROM olympic_operations WHERE owner_id=? AND request_id=?",
                    (owner, request_id),
                ).fetchone()
            if row is None:
                return success({"found": False})
            if row["operation"] != "create":
                raise ValueError("request_id belongs to another operation")
            result = json.loads(row["response"])
            result["found"] = True
            refreshed = trade["load_player"](owner)
            if refreshed is None:
                raise ValueError("Player not found")
            result["paid_hard_currency"] = integer(refreshed["save"].get("PaidHardCurrency", 0))
            result["free_hard_currency"] = integer(refreshed["save"].get("FreeHardCurrency", 0))
            return success(result)
        except ValueError as error:
            return fail(str(error))

    @app.route("/persist/olympic_editDW/", methods=["POST"])
    def OlympicEditDW():
        return change_dungeon(True)

    @app.route("/persist/olympic_deleteDW/", methods=["POST"])
    def OlympicDeleteDW():
        owner, player, failure = current_player()
        if failure:
            return failure
        try:
            data, request_id = payload()
            dungeon_id = integer(data.get("id"), 1)
            canonical = str(dungeon_id)
            with trade["lock"]:
                conn, _, _, _ = open_transaction(player)
                try:
                    prior = operation(conn, owner, request_id, "delete", canonical)
                    if prior is not None:
                        conn.rollback()
                        return success(prior)
                    changed = conn.execute("DELETE FROM olympic_dungeons WHERE id=? AND owner_id=?",
                                           (dungeon_id, owner)).rowcount
                    if changed != 1:
                        conn.rollback()
                        return fail("Dungeon not found", 404)
                    conn.execute("DELETE FROM olympic_creatures WHERE dungeon_id=?", (dungeon_id,))
                    response = {"id": dungeon_id}
                    store_operation(conn, owner, request_id, "delete", canonical, response)
                    conn.commit()
                    return success(response)
                except Exception:
                    conn.rollback()
                    raise
                finally:
                    conn.close()
        except ValueError as error:
            return fail(str(error))
        except sqlite3.Error:
            app.logger.exception("[Olympic] Could not delete dungeon")
            return fail("Could not delete dungeon", 500)

    @app.route("/persist/olympic_listDW/", methods=["POST"])
    def OlympicListDW():
        owner, player, failure = current_player()
        if failure:
            return failure
        try:
            if integer(player["save"].get("RankXP")) < rank_threshold:
                return fail("Olympic unlocks at level 70", 403)
            offset = integer(trade["request_data"]().get("offset", 0), 0, 1000000)
        except ValueError as error:
            return fail(str(error))
        with closing(trade["connect"]()) as conn:
            rows = conn.execute(
                """SELECT id, owner_id, owner_name, name, rewards, battle_count, revision, leader_id, leader_skin
                   FROM olympic_dungeons ORDER BY id DESC LIMIT 30 OFFSET ?""", (offset,),
            ).fetchall()
        return success([dict(row, rewards=json.loads(row["rewards"])) for row in rows])

    @app.route("/persist/olympic_ownersDW/", methods=["POST"])
    def OlympicOwnersDW():
        owner, player, failure = current_player()
        if failure:
            return failure
        try:
            if integer(player["save"].get("RankXP")) < rank_threshold:
                return fail("Olympic unlocks at level 70", 403)
            offset = integer(trade["request_data"]().get("offset", 0))
        except ValueError as error:
            return fail(str(error))
        with closing(trade["connect"]()) as conn:
            rows = conn.execute(
                """SELECT d.owner_id, d.owner_name, grouped.quest_count
                   FROM olympic_dungeons d JOIN
                       (SELECT owner_id, MAX(id) AS latest_id, COUNT(*) AS quest_count
                        FROM olympic_dungeons GROUP BY owner_id) grouped ON d.id=grouped.latest_id
                   ORDER BY d.id DESC LIMIT 30 OFFSET ?""", (offset,),
            ).fetchall()
        return success([dict(row) for row in rows])

    @app.route("/persist/olympic_questsDW/", methods=["POST"])
    def OlympicQuestsDW():
        owner, player, failure = current_player()
        if failure:
            return failure

        try:
            if integer(player["save"].get("RankXP")) < rank_threshold:
                return fail("Olympic unlocks at level 70", 403)

            data = trade["request_data"]()
            owner_id = data.get("owner_id")

            # Edit ekranı kendi questlerini istediğinde,
            # authenticated oyuncunun gerçek owner_id değerini kullan.
            if owner_id == "__self__":
                owner_id = owner
            elif not isinstance(owner_id, str) or not owner_id:
                raise ValueError("Dungeon owner is required")

            offset = integer(data.get("offset", 0))

        except ValueError as error:
            return fail(str(error))

        with closing(trade["connect"]()) as conn:
            rows = conn.execute(
                """SELECT id, name FROM olympic_dungeons
                   WHERE owner_id=?
                   ORDER BY id DESC LIMIT 30 OFFSET ?""",
                (owner_id, offset),
            ).fetchall()

        return success([dict(row) for row in rows])

    @app.route("/persist/olympic_getDW/", methods=["POST"])
    def OlympicGetDW():
        _, player, failure = current_player()
        if failure:
            return failure
        try:
            if integer(player["save"].get("RankXP")) < rank_threshold:
                return fail("Olympic unlocks at level 70", 403)
            dungeon_id = integer(trade["request_data"]().get("id"), 1)
            with closing(trade["connect"]()) as conn:
                row = conn.execute(
                    """SELECT id, owner_id, owner_name, name, rewards, battle_count, revision, leader_id, leader_skin
                       FROM olympic_dungeons WHERE id=?""", (dungeon_id,),
                ).fetchone()
                if row is None:
                    return fail("Dungeon not found", 404)
                slots = conn.execute(
                    "SELECT unique_id FROM olympic_creatures WHERE dungeon_id=? ORDER BY slot",
                    (dungeon_id,),
                ).fetchall()
            creator = trade["load_player"](row["owner_id"])
            if creator is None:
                return fail("Dungeon creator is unavailable", 409)
            items = inventory(creator["save"])
            creatures = {
                integer(item.get("UniqueID"), 1): item for item in items
                if isinstance(item, dict) and item.get("_T") == "CR"
            }
            if len(slots) != 12 or any(slot["unique_id"] not in creatures for slot in slots):
                app.logger.error("[Olympic] Dungeon %s has missing locked creatures", dungeon_id)
                return fail("Dungeon creatures are unavailable", 409)
            result = dict(row, rewards=json.loads(row["rewards"]), creatures=[])
            result["owner_name"] = creator["multiplayer_name"]
            result["owner_leader"] = ({"id": row["leader_id"], "skin": row["leader_skin"]}
                                      if row["leader_id"] and row["leader_skin"]
                                      else selected_leader(creator["save"]))
            if result["owner_leader"] is None:
                app.logger.warning("[Olympic] Dungeon %s creator's selected leader is unavailable", dungeon_id)
            for slot in slots:
                creature = dict(creatures[slot["unique_id"]])
                creature["cards"] = [
                    item for item in items
                    if isinstance(item, dict) and item.get("_T") == "CA"
                    and str(item.get("CreatureUID")) == str(slot["unique_id"])
                ]
                result["creatures"].append(creature)
            return success(result)
        except ValueError as error:
            return fail(str(error))

    @app.route("/persist/olympic_locksDW/", methods=["POST"])
    def OlympicLocksDW():
        owner, player, failure = current_player()
        if failure:
            return failure
        with closing(trade["connect"]()) as conn:
            rows = conn.execute(
                "SELECT DISTINCT unique_id FROM olympic_creatures WHERE owner_id=? ORDER BY unique_id",
                (owner,),
            ).fetchall()
        return success([row["unique_id"] for row in rows])

    @app.route("/persist/olympic_balanceDW/", methods=["POST"])
    def OlympicBalanceDW():
        _, player, failure = current_player()
        if failure:
            return failure
        try:
            return success({
                "paid_hard_currency": integer(player["save"].get("PaidHardCurrency", 0)),
                "free_hard_currency": integer(player["save"].get("FreeHardCurrency", 0)),
            })
        except ValueError as error:
            return fail(str(error))

    @app.route("/persist/olympic_reward_claimDW/", methods=["POST"])
    def OlympicRewardClaimDW():
        player_id, player, failure = trade["current_player"]()
        if failure:
            return failure
        if player is None:
            return fail("player not found", 404)
        try:
            if integer(player["save"].get("RankXP")) < rank_threshold:
                return fail("Olympic unlocks at level 70", 403)
        except ValueError as error:
            return fail(str(error))
        payload = trade["request_data"]()
        dungeon_id = integer(payload.get("id"), 1)
        with trade["lock"]:
            conn = trade["transaction"]()
            try:
                conn.execute("""
                    CREATE TABLE IF NOT EXISTS olympic_reward_claims (
                        player_id TEXT NOT NULL,
                        dungeon_id INTEGER NOT NULL,
                        claimed_at INTEGER NOT NULL,
                        PRIMARY KEY(player_id, dungeon_id)
                    )
                """)
                dungeon = conn.execute(
                    "SELECT rewards FROM olympic_dungeons WHERE id=?", (dungeon_id,)
                ).fetchone()
                if dungeon is None:
                    conn.rollback()
                    return fail("Dungeon not found", 404)

                rewards = json.loads(dungeon["rewards"])
                if not isinstance(rewards, list):
                    conn.rollback()
                    return fail("Dungeon rewards are invalid", 409)
                amount = 0
                for reward in rewards:
                    if isinstance(reward, dict) and reward.get("type") == "trade":
                        amount += integer(reward.get("amount"), 1)
                if amount <= 0 or amount > 1000000000:
                    conn.rollback()
                    return fail("Dungeon has no valid trade reward", 409)

                cursor = conn.execute(
                    """INSERT OR IGNORE INTO olympic_reward_claims
                       (player_id, dungeon_id, claimed_at) VALUES (?, ?, ?)""",
                    (player_id, dungeon_id, int(time.time())),
                )
                if cursor.rowcount == 1:
                    trade["add_wallet"](conn, player_id, amount)
                balance = trade["wallet"](conn, player_id)
                conn.commit()
                return success({
                    "dungeon_id": dungeon_id,
                    "trade_currency": balance,
                    "already_claimed": cursor.rowcount != 1,
                })
            except Exception:
                conn.rollback()
                app.logger.exception("[Olympic] Trade reward claim failed")
                return fail("internal: reward claim failed", 500)
            finally:
                conn.close()

    app.logger.info("[Olympic] Registered dungeon endpoints")

