"""Remove configured inventory items from persisted player saves.

ENABLED controls cleanup. DATABASE_TARGET selects active database(s) for the
app.py cleanup integration: "primary", "localinstance", or "all".
Archive databases must never be modified automatically.
"""

from typing import Callable, Tuple, Any

# Master switch. False disables all cleanup.
ENABLED = True

# Valid values: "primary", "localinstance", or "all".
# "all" means Primary + LocalInstance only; archive backups are excluded.
DATABASE_TARGET = "all"

# Add IDs to the relevant set to remove complete Inventory objects.
# Matching uses both the save item's "_T" type and its "ID".
CREATURE_IDS_TO_REMOVE = frozenset({
    "CornRonin_Awaken",
    "CornRonin_Base",
    "Fluffantry_Awaken",
    "Fluffantry_Base",
    "SunKing_Awaken",
    "SunKing_Base",
})

ACTION_CARD_IDS_TO_REMOVE = frozenset({
    # Example: "SomeActionCard_ID",
})

EVO_MATERIAL_IDS_TO_REMOVE = frozenset({
    # Example: "SomeEvoMaterial_ID",
})

# Inventory type code -> configured IDs.
ITEM_IDS_TO_REMOVE = {
    "CR": CREATURE_IDS_TO_REMOVE,
    "CA": ACTION_CARD_IDS_TO_REMOVE,
    "EV": EVO_MATERIAL_IDS_TO_REMOVE,
}


def remove_game_items(
    game_text: Any,
    decode_save: Callable[[Any], Tuple[dict, Any]],
    encode_save: Callable[[Any, dict, Any], str],
) -> Tuple[Any, int]:
    """Return (updated_game_text, removed_count), preserving save format.

    Removes the whole matching dictionary from Inventory, including all its
    fields and braces. Non-matching entries and other save fields are retained.
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

        item_type = item.get("_T")
        item_id = item.get("ID")
        configured_ids = ITEM_IDS_TO_REMOVE.get(item_type, ())

        if isinstance(item_id, str) and item_id in configured_ids:
            removed_count += 1
        else:
            kept_items.append(item)

    if removed_count == 0:
        return game_text, 0

    save_data["Inventory"] = kept_items
    return encode_save(game_text, save_data, save_format), removed_count
