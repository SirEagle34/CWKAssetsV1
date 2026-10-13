"""Remove selected creature IDs from persisted player saves.

ENABLED controls the cleanup. DATABASE_TARGET selects which active database(s)
are scanned when app.py loads: primary, localinstance, or all. Archive databases
are never modified automatically. A one-time .removegameitems.bak backup is
created before each database is changed.
"""

from typing import Callable, Tuple, Any

# Master switch. False disables both startup cleanup and per-player cleanup.
ENABLED = True

# Valid values: "primary", "localinstance", "all".
# "all" means Primary + LocalInstance only; archive backups are excluded.
DATABASE_TARGET = "localinstance"

CREATURE_IDS_TO_REMOVE = frozenset({
    "CornRonin_Awaken",
    "CornRonin_Base",
    "Fluffantry_Awaken",
    "Fluffantry_Base",
    "SunKing_Awaken",
    "SunKing_Base",
})


def remove_game_items(
    game_text: Any,
    decode_save: Callable[[Any], Tuple[dict, Any]],
    encode_save: Callable[[Any, dict, Any], str],
) -> Tuple[Any, int]:
    """Return (updated_game_text, removed_count), preserving save format."""
    if not ENABLED or not game_text:
        return game_text, 0

    save_data, save_format = decode_save(game_text)
    if not isinstance(save_data, dict):
        return game_text, 0

    inventory = save_data.get("Inventory")
    if not isinstance(inventory, list) or not inventory:
        return game_text, 0

    kept_items = []
    removed_count = 0
    for item in inventory:
        if not isinstance(item, dict):
            kept_items.append(item)
            continue

        creature_id = item.get("ID")
        if (item.get("_T") == "CR" and isinstance(creature_id, str)
                and creature_id in CREATURE_IDS_TO_REMOVE):
            removed_count += 1
        else:
            kept_items.append(item)

    if not removed_count:
        return game_text, 0

    save_data["Inventory"] = kept_items
    return encode_save(game_text, save_data, save_format), removed_count
