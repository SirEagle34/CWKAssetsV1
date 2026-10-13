"""
Remove selected creature IDs from persisted player saves when players open the game.

Set ENABLED = True to activate automatic cleanup, or False to disable it.
Only matching creature entries in the save's Inventory list are removed.
All other inventory entries and save fields are preserved.
"""

from typing import Callable, Tuple, Any

ENABLED = True

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
    """
    Return (updated_game_text, removed_count).

    decode_save must return (save_dict, save_format); encode_save must accept
    (original_game_text, updated_save_dict, save_format). The caller supplies
    the application's existing codec so the stored save format is preserved.
    """
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
        if item.get("_T") == "CR" and isinstance(creature_id, str) and creature_id in CREATURE_IDS_TO_REMOVE:
            removed_count += 1
        else:
            kept_items.append(item)

    if removed_count == 0:
        return game_text, 0

    save_data["Inventory"] = kept_items
    updated_game_text = encode_save(game_text, save_data, save_format)
    return updated_game_text, removed_count
