# EditCustomDungeon02 setup

The new editor is in `EditCustomDungeon02.cs`. The existing create screen `EditCustomDungeon.cs` is unchanged.

## CustomQuestController

1. Add the `EditDungeon02` reference to the existing `CustomQuestController` component.
2. Assign the separate GameObject that contains `EditCustomDungeon02`.
3. Add a UI button for opening the existing-quest editor and bind it to
   `CustomQuestController.OnClickEditCustomDungeon02`.

The editor root and its Panel must be outside the `CustomQuestController` hierarchy. The editor uses its own minimal `EditQuestTile` prefab. It does not use `QuestSelectQuest` or `SimplePopupController`.

## EditCustomDungeon02 Inspector references

### Root and panels
- `Panel`: whole editor panel.
- `CurrentQuestsPanel`: left quest list area.
- `EditorPanel`: creature, hero, reward and rename area.

### Current Quests
- `QuestGrid`: vertical UIStreamingGrid for quest tiles.
- `QuestScrollView`: the UIScrollView containing QuestGrid; movement must be Vertical.
- `EditQuestTilePrefab`: prefab with `EditQuestTile`; it has only `SelectedLabel` (GameObject) and `QuestName` (UILabel). Bind its click event to `EditQuestTile.OnClick`.
- `QuestStatus`: UILabel for loading, empty, and validation/error messages.

### Creatures
- `CreatureGrid`: vertical UIStreamingGrid.
- `CreatureScrollView`: the UIScrollView containing CreatureGrid; movement must be Vertical.
- `SelectionLabel`: optional selected count label (for example, 12 / 12).

### Hero
- `LeaderPortrait`: UITexture for the selected owned hero skin.
- `LeaderName`: UILabel for the selected hero name.
- Hero arrow buttons call `OnClickPreviousLeader` and `OnClickNextLeader`.

### Rewards
- `RewardNode`: Transform where the existing InventoryTile reward preview is spawned.
- `RewardName`, optional `RewardAmount`.
- Reward-slot arrows call `OnClickPreviousRewardSlot` / `OnClickNextRewardSlot`.
- Reward-option arrows call `OnClickPreviousRewardOption` / `OnClickNextRewardOption`.

### Quest name and item IDs
- `QuestName`: UIInput for renaming.
- `GachaKeyId`, `EvolutionShardId`, `XPMaterial`, and optional `XPMaterialId`: use the same values as the existing `EditCustomDungeon` component.

## Button bindings

| Button | Method |
|---|---|
| Open editor | `CustomQuestController.OnClickEditCustomDungeon02` |
| Hero previous / next | `OnClickPreviousLeader` / `OnClickNextLeader` |
| Reward slot previous / next | `OnClickPreviousRewardSlot` / `OnClickNextRewardSlot` |
| Reward option previous / next | `OnClickPreviousRewardOption` / `OnClickNextRewardOption` |
| Remove | `OnClickRemove` |
| Save | `OnClickConfirm` |
| Back | `OnClickBack` |

## Backend behavior

- `SQServer.OlympicEdit` now sends `leader_id` with edit requests.
- `OlympicService.py` validates that the selected leader is owned by the current player and persists both `leader_id` and `leader_skin`.
- Creature ownership, reserved-trade checks, quest ownership, and server-side currency charging remain enforced.
- Legacy PvP reward amount 250 remains editable. Edit cost is calculated by the increase in rounded-up reward units, so existing legacy quantities are not blocked and increases are not undercharged.
- The editor loads every page of the current player's quests, not other players' quests.

## Important

The C# code and backend were committed to `main`, but this repository integration cannot run a Unity 2022.3 build or your live PythonAnywhere database tests. Assign the Inspector references above and test on a staging server before deploying the backend update to production.
