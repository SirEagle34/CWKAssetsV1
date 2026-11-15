# -*- coding: utf-8 -*-
"""SQServer-compatible trade endpoints for Card Wars Kingdom.

Trade state is kept in a dedicated SQLite database. Player saves remain in the
existing Primary / LocalInstance databases and are changed by the game's
trade_syncDW response, matching TradePanelController.SyncTradeChanges().
"""
import json
import os
import sqlite3
import threading
import time
from urllib.parse import parse_qs

from flask import jsonify, request


_TRADE_LOCK = threading.RLock()
_TRADE_PACKS = {
    100: {"gems": 7000, "coins": 700000},
    500: {"gems": 21500, "coins": 2500000},
    1000: {"gems": 40000, "coins": 4500000},
}


def register_trade_routes(app, db, Player, decrypt_game_data,
                          decode_game_save, encode_game_save,
                          local_has_player, local_db_path):
    """Register the trade endpoints called by SQServer.cs."""

    def _trade_db_path():
        path = os.path.join(app.root_path, "data", "persist", "trade.sqlite3")
        os.makedirs(os.path.dirname(path), exist_ok=True)
        return path

    def _connect():
        conn = sqlite3.connect(_trade_db_path(), timeout=20, isolation_level=None)
        conn.row_factory = sqlite3.Row
        conn.execute("PRAGMA busy_timeout = 20000")
        conn.execute("PRAGMA journal_mode = WAL")
        return conn

    def _init_schema():
        with _connect() as conn:
            conn.executescript("""
                CREATE TABLE IF NOT EXISTS trade_wallet (
                    player_id TEXT PRIMARY KEY,
                    coins INTEGER NOT NULL DEFAULT 0 CHECK(coins >= 0)
                );
                CREATE TABLE IF NOT EXISTS trade_listings (
                    listing_id INTEGER PRIMARY KEY AUTOINCREMENT,
                    seller_id TEXT NOT NULL,
                    unique_id INTEGER NOT NULL,
                    creature_id TEXT NOT NULL,
                    creature_json TEXT NOT NULL,
                    price INTEGER NOT NULL CHECK(price > 0),
                    status TEXT NOT NULL DEFAULT 'active',
                    buyer_id TEXT,
                    created_at INTEGER NOT NULL,
                    updated_at INTEGER NOT NULL
                );
                CREATE INDEX IF NOT EXISTS idx_trade_listings_status
                    ON trade_listings(status, listing_id);
                CREATE INDEX IF NOT EXISTS idx_trade_listings_seller
                    ON trade_listings(seller_id, status);
                CREATE TABLE IF NOT EXISTS trade_offers (
                    offer_id INTEGER PRIMARY KEY AUTOINCREMENT,
                    from_id TEXT NOT NULL,
                    to_id TEXT NOT NULL,
                    give_items TEXT NOT NULL,
                    want_ids TEXT NOT NULL,
                    give_coins INTEGER NOT NULL DEFAULT 0,
                    want_coins INTEGER NOT NULL DEFAULT 0,
                    status TEXT NOT NULL DEFAULT 'pending',
                    created_at INTEGER NOT NULL,
                    updated_at INTEGER NOT NULL
                );
                CREATE INDEX IF NOT EXISTS idx_trade_offers_recipient
                    ON trade_offers(to_id, status, offer_id);
                CREATE INDEX IF NOT EXISTS idx_trade_offers_sender
                    ON trade_offers(from_id, status);
                CREATE TABLE IF NOT EXISTS trade_inventory_delta (
                    player_id TEXT NOT NULL,
                    unique_id INTEGER NOT NULL,
                    action TEXT NOT NULL CHECK(action IN ('remove', 'add')),
                    creature_id TEXT NOT NULL,
                    creature_json TEXT,
                    updated_at INTEGER NOT NULL,
                    PRIMARY KEY(player_id, unique_id)
                );
            """)

    def _normal_id(value):
        value = str(value or "").strip().replace("-", "_")
        if not value or len(value) > 80:
            return ""
        return value

    def _request_data():
        payload = request.get_json(silent=True)
        if isinstance(payload, dict):
            return dict(payload)
        raw = request.get_data(as_text=True) or ""
        if raw.startswith("data="):
            raw = raw[5:]
        parsed = parse_qs(raw, keep_blank_values=True)
        return {key: values[0] if len(values) == 1 else values
                for key, values in parsed.items()}

    def _current_player():
        player_id = _normal_id(request.headers.get("Player-Id"))
        if not player_id:
            return "", None, (jsonify({"success": False, "error": "player_id required"}), 401)
        if app.config.get("TRADE_REQUIRE_GAME_HEADERS", True):
            if request.headers.get("User-Agent") != "Innertube Explorer v0.1":
                return "", None, (jsonify({"success": False, "error": "invalid game client"}), 403)
        return player_id, _load_player(player_id), None

    def _player_db_path(player_id):
        try:
            if local_has_player(player_id):
                path = local_db_path()
                if path and os.path.isfile(path):
                    return path
        except Exception:
            app.logger.exception("[Trade] LocalInstance lookup failed for %s", player_id)
        database = db.engine.url.database
        if not database:
            raise RuntimeError("Primary player database path is unavailable")
        if not os.path.isabs(database):
            database = os.path.join(app.instance_path, database)
        return database

    def _load_player(player_id):
        path = _player_db_path(player_id)
        if not os.path.isfile(path):
            return None
        conn = sqlite3.connect(path, timeout=15)
        try:
            row = conn.execute(
                'SELECT username, game, multiplayer_name, friends FROM "player" WHERE username = ?',
                (player_id,)
            ).fetchone()
        finally:
            conn.close()
        if row is None:
            return None
        game_text = row[1]
        save = decrypt_game_data(game_text)
        if not isinstance(save, dict):
            return None
        return {
            "username": row[0],
            "game": game_text,
            "save": save,
            "multiplayer_name": row[2] or save.get("MultiplayerPlayerName") or save.get("PlayerName") or row[0],
            "friends": _safe_json(row[3], []),
            "path": path,
        }

    def _safe_json(value, fallback):
        try:
            result = json.loads(value) if isinstance(value, str) else value
            return result if result is not None else fallback
        except (TypeError, ValueError):
            return fallback

    def _save_player_game(player, save):
        data, save_format = decode_game_save(player["game"])
        if not isinstance(data, dict):
            raise ValueError("player save could not be decoded")
        encoded = encode_game_save(player["game"], save, save_format)
        conn = sqlite3.connect(player["path"], timeout=15)
        try:
            conn.execute("BEGIN IMMEDIATE")
            conn.execute('UPDATE "player" SET game = ? WHERE username = ?',
                         (encoded, player["username"]))
            conn.commit()
        except Exception:
            conn.rollback()
            raise
        finally:
            conn.close()

    def _response(data=None, status=200, **extra):
        result = {"success": True}
        if data is not None:
            result["data"] = data
        result.update(extra)
        return jsonify(result), status

    def _error(message, status=400):
        return jsonify({"success": False, "error": str(message)}), status

    def _int(value, default=None, minimum=None, maximum=None):
        try:
            if isinstance(value, bool):
                return default
            number = int(value)
        except (TypeError, ValueError, OverflowError):
            return default
        if minimum is not None and number < minimum:
            return default
        if maximum is not None and number > maximum:
            return default
        return number

    def _ids(value, maximum=3):
        if value is None or value == "":
            return []
        if isinstance(value, list):
            raw = value
        else:
            raw = str(value).split(",")
        result = []
        for item in raw:
            uid = _int(item, None, 1, 2147483647)
            if uid is None or uid in result:
                raise ValueError("invalid or duplicate creature unique_id")
            result.append(uid)
        if len(result) > maximum:
            raise ValueError("too many creatures in one trade")
        return result

    def _inventory(save):
        inventory = save.get("Inventory", [])
        if not isinstance(inventory, list):
            raise ValueError("player inventory is invalid")
        return [item for item in inventory
                if isinstance(item, dict) and item.get("_T") == "CR"]

    def _find_creature(player, unique_id):
        for item in _inventory(player["save"]):
            if _int(item.get("UniqueID"), None, 1) == unique_id:
                creature_id = str(item.get("ID") or "").strip()
                if not creature_id:
                    return None
                return item
        return None

    def _loadout_ids(save):
        found = set()
        def walk(value, key=""):
            if isinstance(value, dict):
                for k, v in value.items():
                    if str(k).lower() in ("uniqueid", "unique_id", "creatureuid"):
                        uid = _int(v, None, 1)
                        if uid is not None:
                            found.add(uid)
                    walk(v, str(k))
            elif isinstance(value, list):
                for v in value:
                    walk(v, key)
            elif isinstance(value, (int, float)) and key.lower() in ("uniqueid", "unique_id", "creatureuid"):
                uid = _int(value, None, 1)
                if uid is not None:
                    found.add(uid)
        walk(save.get("Loadouts", []))
        return found

    def _tradeable(player, unique_id):
        item = _find_creature(player, unique_id)
        if item is None:
            return None
        if unique_id == _int(player["save"].get("MyHelperCreatureID"), -1):
            return None
        if unique_id in _loadout_ids(player["save"]):
            return None
        if item.get("Favorite") is True or item.get("IsFavorite") is True:
            return None
        return item

    def _reserved(conn, player_id, unique_id):
        row = conn.execute(
            "SELECT 1 FROM trade_inventory_delta WHERE player_id=? AND unique_id=? AND action='remove'",
            (player_id, unique_id)
        ).fetchone()
        if row:
            return True
        row = conn.execute(
            "SELECT 1 FROM trade_listings WHERE seller_id=? AND unique_id=? AND status='active'",
            (player_id, unique_id)
        ).fetchone()
        if row:
            return True
        # Active offers have already put their give items into the delta table.
        return False

    def _delta(conn, player_id, item, action):
        uid = _int(item.get("UniqueID"), None, 1)
        creature_id = str(item.get("ID") or "").strip()
        if uid is None or not creature_id:
            raise ValueError("invalid creature inventory record")
        conn.execute("""
            INSERT INTO trade_inventory_delta
                (player_id, unique_id, action, creature_id, creature_json, updated_at)
            VALUES (?, ?, ?, ?, ?, ?)
            ON CONFLICT(player_id, unique_id) DO UPDATE SET
                action=excluded.action,
                creature_id=excluded.creature_id,
                creature_json=excluded.creature_json,
                updated_at=excluded.updated_at
        """, (player_id, uid, action, creature_id,
              json.dumps(item, ensure_ascii=False, separators=(",", ":")) if action == "add" else None,
              int(time.time())))

    def _wallet(conn, player_id):
        row = conn.execute("SELECT coins FROM trade_wallet WHERE player_id=?", (player_id,)).fetchone()
        return int(row["coins"]) if row else 0

    def _set_wallet(conn, player_id, amount):
        amount = max(0, int(amount))
        conn.execute("""
            INSERT INTO trade_wallet(player_id, coins) VALUES(?, ?)
            ON CONFLICT(player_id) DO UPDATE SET coins=excluded.coins
        """, (player_id, amount))

    def _add_wallet(conn, player_id, amount):
        _set_wallet(conn, player_id, _wallet(conn, player_id) + int(amount))

    def _effective_inventory_count(conn, player):
        items = {int(item.get("UniqueID")): item for item in _inventory(player["save"])
                 if _int(item.get("UniqueID"), None, 1) is not None}
        rows = conn.execute(
            "SELECT unique_id, action FROM trade_inventory_delta WHERE player_id=?",
            (player["username"],)
        ).fetchall()
        for row in rows:
            uid = int(row["unique_id"])
            if row["action"] == "remove":
                items.pop(uid, None)
            else:
                items[uid] = True
        return len(items)

    def _inventory_capacity(player):
        return _int(player["save"].get("InventorySpace"), 0, 0, 100000)

    def _check_capacity(conn, player, incoming):
        current = _effective_inventory_count(conn, player)
        capacity = _inventory_capacity(player)
        if capacity and current + incoming > capacity:
            return False
        return True

    def _creature_summary(item):
        return {
            "unique_id": int(item.get("UniqueID")),
            "creature_id": str(item.get("ID")),
            "star": _int(item.get("StarRating"), 0, 0, 99),
        }

    def _friend_allowed(player, target_id):
        friends = player.get("friends") or []
        normalized = {_normal_id(value) for value in friends if value}
        return target_id in normalized

    def _transaction():
        conn = _connect()
        conn.execute("BEGIN IMMEDIATE")
        return conn

    _init_schema()

    @app.route("/persist/trade_syncDW/", methods=["POST"])
    def TradeSyncDW():
        player_id, player, failure = _current_player()
        if failure:
            return failure
        if player is None:
            return _error("player not found", 404)
        payload = _request_data()
        requested = _normal_id(payload.get("player_id"))
        if requested and requested != player_id:
            return _error("player_id mismatch", 403)
        with _TRADE_LOCK:
            conn = _connect()
            try:
                removes, adds = [], []
                for row in conn.execute(
                    "SELECT unique_id, creature_id, creature_json, action FROM trade_inventory_delta WHERE player_id=? ORDER BY updated_at, unique_id",
                    (player_id,)
                ):
                    if row["action"] == "remove":
                        removes.append({"unique_id": int(row["unique_id"]), "creature_id": row["creature_id"]})
                    elif row["creature_json"]:
                        item = _safe_json(row["creature_json"], None)
                        if isinstance(item, dict):
                            adds.append(item)
                return _response({"remove": removes, "add": adds})
            finally:
                conn.close()

    @app.route("/persist/trade_balanceDW/", methods=["POST"])
    def TradeBalanceDW():
        player_id, player, failure = _current_player()
        if failure:
            return failure
        if player is None:
            return _error("player not found", 404)
        payload = _request_data()
        if _normal_id(payload.get("player_id")) not in ("", player_id):
            return _error("player_id mismatch", 403)
        conn = _connect()
        try:
            return _response({"coins": _wallet(conn, player_id)})
        finally:
            conn.close()

    @app.route("/persist/trade_friendsDW/", methods=["POST"])
    def TradeFriendsDW():
        player_id, player, failure = _current_player()
        if failure:
            return failure
        if player is None:
            return _error("player not found", 404)
        friends = []
        for friend_id in player["friends"] if isinstance(player["friends"], list) else []:
            friend_id = _normal_id(friend_id)
            if not friend_id:
                continue
            friend = _load_player(friend_id)
            if friend is not None:
                friends.append({
                    "user_id": friend_id,
                    "name": friend["multiplayer_name"],
                })
        return _response(friends)

    @app.route("/persist/trade_listDW/", methods=["POST"])
    def TradeListDW():
        player_id, player, failure = _current_player()
        if failure:
            return failure
        if player is None:
            return _error("player not found", 404)
        payload = _request_data()
        if _normal_id(payload.get("player_id")) != player_id:
            return _error("player_id required or mismatched", 403)
        unique_id = _int(payload.get("unique_id"), None, 1, 2147483647)
        price = _int(payload.get("price"), None, 1, 1000000000)
        if unique_id is None or price is None:
            return _error("unique_id and integer price required")
        item = _tradeable(player, unique_id)
        if item is None:
            return _error("that creature can't be traded")
        now = int(time.time())
        with _TRADE_LOCK:
            conn = _transaction()
            try:
                if _reserved(conn, player_id, unique_id):
                    conn.rollback()
                    return _error("creature already listed or reserved")
                conn.execute("""
                    INSERT INTO trade_listings
                    (seller_id, unique_id, creature_id, creature_json, price, status, created_at, updated_at)
                    VALUES (?, ?, ?, ?, ?, 'active', ?, ?)
                """, (player_id, unique_id, item["ID"],
                      json.dumps(item, ensure_ascii=False, separators=(",", ":")),
                      price, now, now))
                _delta(conn, player_id, item, "remove")
                conn.commit()
                return _response({"listing_id": conn.execute("SELECT last_insert_rowid()").fetchone()[0]})
            except Exception:
                conn.rollback()
                app.logger.exception("[Trade] Listing creation failed")
                return _error("internal: listing creation failed", 500)
            finally:
                conn.close()

    @app.route("/persist/trade_browseDW/", methods=["POST"])
    def TradeBrowseDW():
        payload = _request_data()
        start = _int(payload.get("startpos"), 1, 1, 1000000)
        end = _int(payload.get("endpos"), 30, 1, 1000000)
        start, end = min(start, end), max(start, end)
        if end - start > 99:
            end = start + 99
        conn = _connect()
        try:
            rows = conn.execute("""
                SELECT l.*, p.multiplayer_name
                FROM trade_listings l
                LEFT JOIN (
                    SELECT username, multiplayer_name FROM player
                ) p ON p.username=l.seller_id
                WHERE l.status='active'
                ORDER BY l.listing_id DESC
                LIMIT ? OFFSET ?
            """, (end - start + 1, start - 1)).fetchall()
            listings = [{
                "listing_id": int(row["listing_id"]),
                "seller_id": row["seller_id"],
                "seller_name": row["multiplayer_name"] or row["seller_id"],
                "creature_id": row["creature_id"],
                "price": int(row["price"]),
            } for row in rows]
            return _response(listings)
        except sqlite3.Error:
            # LocalInstance sellers are not in Primary; still return their listings.
            rows = conn.execute("""
                SELECT * FROM trade_listings
                WHERE status='active' ORDER BY listing_id DESC LIMIT ? OFFSET ?
            """, (end - start + 1, start - 1)).fetchall()
            listings = []
            for row in rows:
                seller = _load_player(row["seller_id"])
                listings.append({
                    "listing_id": int(row["listing_id"]),
                    "seller_id": row["seller_id"],
                    "seller_name": seller["multiplayer_name"] if seller else row["seller_id"],
                    "creature_id": row["creature_id"],
                    "price": int(row["price"]),
                })
            return _response(listings)
        finally:
            conn.close()

    @app.route("/persist/trade_buyDW/", methods=["POST"])
    def TradeBuyDW():
        buyer_id, buyer, failure = _current_player()
        if failure:
            return failure
        if buyer is None:
            return _error("buyer not found", 404)
        payload = _request_data()
        if _normal_id(payload.get("player_id")) != buyer_id:
            return _error("player_id required or mismatched", 403)
        listing_id = _int(payload.get("listing_id"), None, 1)
        if listing_id is None:
            return _error("listing_id required")
        with _TRADE_LOCK:
            conn = _transaction()
            try:
                listing = conn.execute(
                    "SELECT * FROM trade_listings WHERE listing_id=? AND status='active'",
                    (listing_id,)
                ).fetchone()
                if listing is None:
                    conn.rollback()
                    return _error("listing not available", 409)
                if listing["seller_id"] == buyer_id:
                    conn.rollback()
                    return _error("cannot buy your own listing", 403)
                seller = _load_player(listing["seller_id"])
                if seller is None:
                    conn.rollback()
                    return _error("seller not found", 404)
                price = int(listing["price"])
                if _wallet(conn, buyer_id) < price:
                    conn.rollback()
                    return _error("not enough trade coins", 400)
                item = _safe_json(listing["creature_json"], None)
                if not isinstance(item, dict):
                    conn.rollback()
                    return _error("listing escrow missing", 409)
                if not _check_capacity(conn, buyer, 1):
                    conn.rollback()
                    return _error("your inventory is full", 409)
                _set_wallet(conn, buyer_id, _wallet(conn, buyer_id) - price)
                _add_wallet(conn, listing["seller_id"], price)
                _delta(conn, buyer_id, item, "add")
                conn.execute(
                    "UPDATE trade_listings SET status='sold', buyer_id=?, updated_at=? WHERE listing_id=? AND status='active'",
                    (buyer_id, int(time.time()), listing_id)
                )
                conn.commit()
                return _response({"listing_id": listing_id, "spent": price})
            except Exception:
                conn.rollback()
                app.logger.exception("[Trade] Purchase failed")
                return _error("internal: purchase failed", 500)
            finally:
                conn.close()

    @app.route("/persist/trade_cancelDW/", methods=["POST"])
    def TradeCancelDW():
        player_id, player, failure = _current_player()
        if failure:
            return failure
        if player is None:
            return _error("player not found", 404)
        payload = _request_data()
        if _normal_id(payload.get("player_id")) != player_id:
            return _error("player_id required or mismatched", 403)
        listing_id = _int(payload.get("listing_id"), None, 1)
        if listing_id is None:
            return _error("listing_id required")
        with _TRADE_LOCK:
            conn = _transaction()
            try:
                row = conn.execute(
                    "SELECT * FROM trade_listings WHERE listing_id=? AND status='active'",
                    (listing_id,)
                ).fetchone()
                if row is None:
                    conn.rollback()
                    return _error("listing not available", 409)
                if row["seller_id"] != player_id:
                    conn.rollback()
                    return _error("not your listing", 403)
                item = _safe_json(row["creature_json"], None)
                if not isinstance(item, dict):
                    conn.rollback()
                    return _error("listing escrow missing", 409)
                _delta(conn, player_id, item, "add")
                conn.execute(
                    "UPDATE trade_listings SET status='cancelled', updated_at=? WHERE listing_id=?",
                    (int(time.time()), listing_id)
                )
                conn.commit()
                return _response({"listing_id": listing_id})
            except Exception:
                conn.rollback()
                app.logger.exception("[Trade] Listing cancellation failed")
                return _error("internal: listing cancellation failed", 500)
            finally:
                conn.close()

    @app.route("/persist/trade_buycoinsDW/", methods=["POST"])
    def TradeBuyCoinsDW():
        player_id, player, failure = _current_player()
        if failure:
            return failure
        if player is None:
            return _error("player not found", 404)
        payload = _request_data()
        if _normal_id(payload.get("player_id")) != player_id:
            return _error("player_id required or mismatched", 403)
        pack = _int(payload.get("pack"), None)
        currency = str(payload.get("currency") or "").lower()
        if pack not in _TRADE_PACKS or currency not in ("coins", "gems"):
            return _error("invalid pack or currency")
        cost = _TRADE_PACKS[pack][currency]
        save = dict(player["save"])
        if currency == "coins":
            balance = _int(save.get("SoftCurrency"), 0, 0)
            if balance < cost:
                return _error("not enough coins", 400)
            save["SoftCurrency"] = balance - cost
        else:
            balance = (_int(save.get("PaidHardCurrency"), 0, 0) +
                       _int(save.get("FreeHardCurrency"), 0, 0))
            if balance < cost:
                return _error("not enough gems", 400)
            # Match PlayerSaveData.HardCurrency's setter, which applies the delta
            # to FreeHardCurrency while preserving PaidHardCurrency.
            save["FreeHardCurrency"] = _int(save.get("FreeHardCurrency"), 0) - cost
        try:
            with _TRADE_LOCK:
                _save_player_game(player, save)
                conn = _transaction()
                try:
                    _add_wallet(conn, player_id, pack)
                    conn.commit()
                finally:
                    conn.close()
            return _response({"coins": _wallet(_connect(), player_id), "spent": cost})
        except Exception:
            app.logger.exception("[Trade] Coin pack purchase failed")
            return _error("internal: coin purchase failed", 500)

    @app.route("/persist/trade_offerDW/", methods=["POST"])
    def TradeOfferDW():
        from_id, sender, failure = _current_player()
        if failure:
            return failure
        if sender is None:
            return _error("player not found", 404)
        payload = _request_data()
        if _normal_id(payload.get("from_id")) != from_id:
            return _error("from_id required or mismatched", 403)
        to_id = _normal_id(payload.get("to_id"))
        if not to_id or to_id == from_id:
            return _error("distinct to_id required")
        recipient = _load_player(to_id)
        if recipient is None:
            return _error("player not found", 404)
        if not _friend_allowed(sender, to_id):
            return _error("trade recipient must be your friend", 403)
        try:
            give_ids = _ids(payload.get("offer_unique_ids"), 3)
            want_ids = _ids(payload.get("request_unique_ids"), 3)
        except ValueError as exc:
            return _error(str(exc))
        give_coins = _int(payload.get("offer_coins", 0), None, 0, 1000000000)
        want_coins = _int(payload.get("request_coins", 0), None, 0, 1000000000)
        if give_coins is None or want_coins is None:
            return _error("coins must be >= 0")
        if not give_ids and give_coins <= 0:
            return _error("offer something first")
        give_items = []
        for uid in give_ids:
            item = _tradeable(sender, uid)
            if item is None:
                return _error("creature not in your inventory or cannot be traded")
            give_items.append(item)
        want_items = []
        for uid in want_ids:
            item = _tradeable(recipient, uid)
            if item is None:
                return _error("requested creature is no longer available")
            want_items.append(item)
        now = int(time.time())
        with _TRADE_LOCK:
            conn = _transaction()
            try:
                if _wallet(conn, from_id) < give_coins:
                    conn.rollback()
                    return _error("not enough trade coins")
                for uid in give_ids:
                    if _reserved(conn, from_id, uid):
                        conn.rollback()
                        return _error("creature already listed or reserved")
                _set_wallet(conn, from_id, _wallet(conn, from_id) - give_coins)
                cur = conn.execute("""
                    INSERT INTO trade_offers
                    (from_id, to_id, give_items, want_ids, give_coins, want_coins, status, created_at, updated_at)
                    VALUES (?, ?, ?, ?, ?, ?, 'pending', ?, ?)
                """, (from_id, to_id,
                      json.dumps(give_items, ensure_ascii=False, separators=(",", ":")),
                      json.dumps(want_ids), give_coins, want_coins, now, now))
                for item in give_items:
                    _delta(conn, from_id, item, "remove")
                offer_id = cur.lastrowid
                conn.commit()
                return _response({"offer_id": offer_id})
            except Exception:
                conn.rollback()
                app.logger.exception("[Trade] Offer creation failed")
                return _error("internal: offer creation failed", 500)
            finally:
                conn.close()

    @app.route("/persist/trade_inventoryDW/", methods=["POST"])
    def TradeInventoryDW():
        requester_id, requester, failure = _current_player()
        if failure:
            return failure
        if requester is None:
            return _error("player not found", 404)
        payload = _request_data()
        target_id = _normal_id(payload.get("player_id"))
        if not target_id:
            return _error("player_id required")
        if target_id != requester_id and not _friend_allowed(requester, target_id):
            return _error("trade recipient must be your friend", 403)
        target = requester if target_id == requester_id else _load_player(target_id)
        if target is None:
            return _error("player not found", 404)
        conn = _connect()
        try:
            creatures = []
            for item in _inventory(target["save"]):
                uid = _int(item.get("UniqueID"), None, 1)
                if uid is None:
                    continue
                if target_id == requester_id and _reserved(conn, target_id, uid):
                    continue
                if _tradeable(target, uid) is None:
                    continue
                creatures.append(_creature_summary(item))
            return _response({"creatures": creatures})
        finally:
            conn.close()

    @app.route("/persist/trade_offers_receivedDW/", methods=["POST"])
    def TradeOffersReceivedDW():
        player_id, player, failure = _current_player()
        if failure:
            return failure
        if player is None:
            return _error("player not found", 404)
        payload = _request_data()
        if _normal_id(payload.get("player_id")) not in ("", player_id):
            return _error("player_id mismatch", 403)
        conn = _connect()
        try:
            rows = conn.execute("""
                SELECT * FROM trade_offers
                WHERE to_id=? AND status='pending'
                ORDER BY offer_id DESC
            """, (player_id,)).fetchall()
            offers = []
            for row in rows:
                from_player = _load_player(row["from_id"])
                give_items = _safe_json(row["give_items"], [])
                want_ids = _safe_json(row["want_ids"], [])
                offers.append({
                    "offer_id": int(row["offer_id"]),
                    "from_id": row["from_id"],
                    "from_name": from_player["multiplayer_name"] if from_player else row["from_id"],
                    "they_give": {
                        "creatures": [{"creature_id": item.get("ID")} for item in give_items if isinstance(item, dict)],
                        "coins": int(row["give_coins"]),
                    },
                    "they_want": {
                        "unique_ids": want_ids,
                        "coins": int(row["want_coins"]),
                    },
                })
            return _response(offers)
        finally:
            conn.close()

    @app.route("/persist/trade_acceptDW/", methods=["POST"])
    def TradeAcceptDW():
        to_id, recipient, failure = _current_player()
        if failure:
            return failure
        if recipient is None:
            return _error("player not found", 404)
        payload = _request_data()
        if _normal_id(payload.get("player_id")) != to_id:
            return _error("player_id required or mismatched", 403)
        offer_id = _int(payload.get("offer_id"), None, 1)
        if offer_id is None:
            return _error("offer_id required")
        with _TRADE_LOCK:
            conn = _transaction()
            try:
                offer = conn.execute(
                    "SELECT * FROM trade_offers WHERE offer_id=? AND to_id=? AND status='pending'",
                    (offer_id, to_id)
                ).fetchone()
                if offer is None:
                    conn.rollback()
                    return _error("offer not available", 409)
                sender = _load_player(offer["from_id"])
                if sender is None:
                    conn.rollback()
                    return _error("player not found", 404)
                give_items = _safe_json(offer["give_items"], [])
                want_ids = _safe_json(offer["want_ids"], [])
                requested_items = []
                for uid in want_ids:
                    item = _tradeable(recipient, int(uid))
                    if item is None or _reserved(conn, to_id, int(uid)):
                        conn.rollback()
                        return _error("you no longer have creature requested", 409)
                    requested_items.append(item)
                if _wallet(conn, to_id) < int(offer["want_coins"]):
                    conn.rollback()
                    return _error("not enough trade coins")
                if not _check_capacity(conn, recipient, len(give_items)):
                    conn.rollback()
                    return _error("your inventory is full", 409)
                if not _check_capacity(conn, sender, len(requested_items)):
                    conn.rollback()
                    return _error("their inventory is full", 409)
                for item in requested_items:
                    _delta(conn, to_id, item, "remove")
                    _delta(conn, offer["from_id"], item, "add")
                for item in give_items:
                    _delta(conn, to_id, item, "add")
                _add_wallet(conn, to_id, int(offer["give_coins"]))
                _add_wallet(conn, offer["from_id"], int(offer["want_coins"]))
                _set_wallet(conn, to_id, _wallet(conn, to_id) - int(offer["want_coins"]))
                conn.execute(
                    "UPDATE trade_offers SET status='accepted', updated_at=? WHERE offer_id=?",
                    (int(time.time()), offer_id)
                )
                conn.commit()
                return _response({"offer_id": offer_id})
            except Exception:
                conn.rollback()
                app.logger.exception("[Trade] Offer acceptance failed")
                return _error("internal: offer acceptance failed", 500)
            finally:
                conn.close()

    @app.route("/persist/trade_declineDW/", methods=["POST"])
    def TradeDeclineDW():
        player_id, player, failure = _current_player()
        if failure:
            return failure
        if player is None:
            return _error("player not found", 404)
        payload = _request_data()
        if _normal_id(payload.get("player_id")) != player_id:
            return _error("player_id required or mismatched", 403)
        offer_id = _int(payload.get("offer_id"), None, 1)
        if offer_id is None:
            return _error("offer_id required")
        with _TRADE_LOCK:
            conn = _transaction()
            try:
                offer = conn.execute(
                    "SELECT * FROM trade_offers WHERE offer_id=? AND to_id=? AND status='pending'",
                    (offer_id, player_id)
                ).fetchone()
                if offer is None:
                    conn.rollback()
                    return _error("offer not available", 409)
                for item in _safe_json(offer["give_items"], []):
                    if isinstance(item, dict):
                        _delta(conn, offer["from_id"], item, "add")
                _add_wallet(conn, offer["from_id"], int(offer["give_coins"]))
                conn.execute(
                    "UPDATE trade_offers SET status='declined', updated_at=? WHERE offer_id=?",
                    (int(time.time()), offer_id)
                )
                conn.commit()
                return _response({"offer_id": offer_id})
            except Exception:
                conn.rollback()
                app.logger.exception("[Trade] Offer decline failed")
                return _error("internal: offer decline failed", 500)
            finally:
                conn.close()

    app.logger.info("[Trade] Registered SQServer trade endpoints.")
