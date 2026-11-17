using System;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

/// <summary>
/// Edits the current player's existing Olympic / Custom Dungeon quests.
/// Keep this component outside the CustomQuestController hierarchy.
/// </summary>
public class EditCustomDungeon02 : MonoBehaviour
{
    public GameObject Panel;
    public GameObject EditPanel01;
    public GameObject CurrentQuestsPanel;
    public GameObject EditorPanel;

    [Header("Current Quests")]
    public UIGrid QuestGrid;
    public UIScrollView QuestScrollView;
    public GameObject EditQuestTilePrefab;
    public UILabel QuestStatus;

    [Header("Creature selection")]
    public UIGrid CreatureGrid;
    public UIScrollView CreatureScrollView;
    public UILabel SelectionLabel;

    [Header("Hero")]
    public UITexture LeaderPortrait;
    public UILabel LeaderName;

    [Header("Reward")]
    public Transform RewardNode;
    public UILabel RewardName;
    public UILabel RewardAmount;

    [Header("Quest")]
    public UIInput QuestName;

    [Header("Reward item IDs (same values as EditCustomDungeon)")]
    public string GachaKeyId;
    public string EvolutionShardId;
    public EditCustomDungeon.XPMaterialOption XPMaterial;
    public string XPMaterialId;

    private sealed class RewardOption
    {
        public string Type;
        public int Amount;
        public RewardOption(string type, int amount) { Type = type; Amount = amount; }
    }

    private static readonly RewardOption[] RewardOptions =
    {
        new RewardOption("key", 1),
        new RewardOption("shard", 1), new RewardOption("shard", 3),
        new RewardOption("pvp", 100), new RewardOption("pvp", 250), new RewardOption("pvp", 500),
        new RewardOption("craft", 1), new RewardOption("craft", 3), new RewardOption("craft", 6),
        new RewardOption("trade", 100), new RewardOption("trade", 300),
        new RewardOption("trade", 500), new RewardOption("trade", 1000),
        new RewardOption("cake", 3), new RewardOption("cake", 10)
    };

    private readonly List<CustomQuestController.QuestEntry> mEntries = new List<CustomQuestController.QuestEntry>();

    private readonly List<InventorySlotItem> mOwnedCreatures = new List<InventorySlotItem>();
    private readonly List<int> mSelectedCreatures = new List<int>();
    private readonly List<Dictionary<string, object>> mRewards =
        new List<Dictionary<string, object>>();

    private CustomQuestController mReturnController;
    private CustomQuestController.QuestEntry mEditing;
    private GameObject mRewardTile;
    private int mLeaderIndex;
    private int mRewardSlotIndex;
    private int mRewardOptionIndex;
    private int mRequest;
    private bool mBusy;
    private bool mDeactivateRootOnClose;

    private SQServer Server { get { return SessionManager.Instance.theSession.Server; } }

    public bool IsSelectingQuest { get; private set; }

    public bool ShowFrom(CustomQuestController controller)
    {
        if (controller == null || Panel == null || transform.IsChildOf(controller.transform) ||
            Panel.transform.IsChildOf(controller.transform))
        {
            ShowMessage("The editor must be outside the quest selection hierarchy.");
            return false;
        }

        PlayerInfoScript player = Singleton<PlayerInfoScript>.Instance;
        if (player == null || player.SaveData == null || player.SaveData.Leaders == null ||
            player.SaveData.Leaders.Count == 0 || !ValidateReferences())
        {
            ShowMessage("The Custom Dungeon editor is not configured.");
            return false;
        }

        mReturnController = controller;
        mDeactivateRootOnClose = !gameObject.activeSelf;
        if (mDeactivateRootOnClose) gameObject.SetActive(true);
        if (!gameObject.activeInHierarchy)
        {
            if (mDeactivateRootOnClose) gameObject.SetActive(false);
            mDeactivateRootOnClose = false;
            ShowMessage("The editor has an inactive parent.");
            return false;
        }

        Panel.SetActive(true);
        IsSelectingQuest = true;
        mEditing = null;
        if (CurrentQuestsPanel != null) CurrentQuestsPanel.SetActive(true);
        if (EditorPanel != null) EditorPanel.SetActive(false);
        LoadCurrentQuests();
        return true;
    }

    private bool ValidateReferences()
    {
        GameObject inventoryPrefab = Singleton<PrefabReferences>.Instance != null
            ? Singleton<PrefabReferences>.Instance.InventoryTile : null;
        InventoryTile inventory = inventoryPrefab != null ? inventoryPrefab.GetComponent<InventoryTile>() : null;
        UIScrollView questParent = QuestGrid != null ? NGUITools.FindInParents<UIScrollView>(QuestGrid.transform.parent) : null;
        UIScrollView creatureParent = CreatureGrid != null ? NGUITools.FindInParents<UIScrollView>(CreatureGrid.transform.parent) : null;

        bool valid = Panel != null && CurrentQuestsPanel != null && EditorPanel != null &&
            QuestGrid != null && QuestScrollView != null && questParent == QuestScrollView &&
            QuestScrollView.movement == UIScrollView.Movement.Vertical &&
            EditQuestTilePrefab != null && EditQuestTilePrefab.GetComponent<EditQuestTile>() != null &&
            QuestGrid.cellWidth > 0f && QuestGrid.cellHeight > 0f &&
            CreatureGrid != null && CreatureScrollView != null && creatureParent == CreatureScrollView &&
            CreatureScrollView.movement == UIScrollView.Movement.Vertical &&
            CreatureGrid.cellHeight > 0f && NGUITools.FindInParents<UIPanel>(CreatureGrid.transform.parent) != null &&
            inventory != null && inventory.PortraitCollider != null && inventory.FrameSprite != null &&
            LeaderPortrait != null && LeaderName != null && RewardNode != null && RewardName != null &&
            QuestName != null;

        if (!valid) Debug.LogError("[Olympic Edit02] Assign valid quest/creature grids, scroll views and UI references.");
        return valid;
    }

    private void LoadCurrentQuests()
    {
        int request = ++mRequest;

        mEntries.Clear();
        ClearQuestGrid();

        SetQuestStatus("Loading current quests...");

        LoadQuestIdPage("__self__", 0, new List<int>(), request);
    }

    private void LoadQuestIdPage(string ownerId, int offset, List<int> ids, int request)
    {
        if (request != mRequest) return;
        Server.OlympicQuests(ownerId, offset, delegate(Dictionary<string, object> response, HttpStatusCode status)
        {
            if (this == null || request != mRequest || Panel == null || !Panel.activeInHierarchy) return;
            object data;
            if (!TryResponse(response, status, out data)) { SetQuestStatus(ReadError(response)); return; }
            List<object> rows = data as List<object>;
            if (rows == null) { SetQuestStatus("Invalid quest list."); return; }

            foreach (object value in rows)
            {
                Dictionary<string, object> row = value as Dictionary<string, object>;
                int id;
                if (row == null || !TryInt(row, "id", out id))
                {
                    SetQuestStatus("Invalid quest entry.");
                    return;
                }
                ids.Add(id);
            }
            if (rows.Count == 30) LoadQuestIdPage(ownerId, offset + 30, ids, request);
            else LoadQuestDetails(ids, 0, request);
        });
    }

    private void LoadQuestDetails(List<int> ids, int index, int request)
    {
        if (request != mRequest) return;

        if (index >= ids.Count)
        {
            ClearQuestGrid();

            if (ids.Count == 0)
            {
                SetQuestStatus("No current quests.");
            }
            else if (mEntries.Count == 0)
            {
                SetQuestStatus("Quests were found, but their details could not be loaded. Check the logs.");
            }
            else
            {
                SetQuestStatus(string.Empty);
            }

            foreach (CustomQuestController.QuestEntry entry in mEntries)
            {
                GameObject obj = NGUITools.AddChild(
                    QuestGrid.gameObject,
                    EditQuestTilePrefab
                );

                EditQuestTile tile = obj.GetComponent<EditQuestTile>();

                if (tile == null)
                {
                    NGUITools.Destroy(obj);
                    continue;
                }

                tile.Populate(entry);
            }

            QuestGrid.Reposition();
            QuestScrollView.ResetPosition();

            return;
        }

        Server.OlympicGet(ids[index],
            delegate(Dictionary<string, object> response, HttpStatusCode status)
        {
            if (this == null || request != mRequest ||
                Panel == null || !Panel.activeInHierarchy)
                return;

            object data;

            if (TryResponse(response, status, out data))
            {
                CustomQuestController.QuestEntry entry;

                if (TryBuildEntry(
                    data as Dictionary<string, object>,
                    ids[index],
                    out entry))
                {
                    mEntries.Add(entry);
                }
                else
                {
                    Debug.LogWarning(
                        "[Olympic Edit02] Failed to load quest " +
                        ids[index] + ": " + ReadError(response) +
                        " (HTTP " + status + ")"
                    );
                }
            }

            LoadQuestDetails(ids, index + 1, request);
        });
    }

    private bool TryBuildEntry(Dictionary<string, object> row, int id, out CustomQuestController.QuestEntry entry)
    {
        entry = null;
        object nameValue, ownerNameValue, creatureValue, rewardValue;
        List<object> creatures, rewards;
        if (row == null || !row.TryGetValue("name", out nameValue) || !(nameValue is string) ||
            !row.TryGetValue("owner_name", out ownerNameValue) || !(ownerNameValue is string) ||
            !row.TryGetValue("creatures", out creatureValue) || (creatures = creatureValue as List<object>) == null ||
            creatures.Count != 12 || !row.TryGetValue("rewards", out rewardValue) ||
            (rewards = rewardValue as List<object>) == null || rewards.Count < 1 || rewards.Count > 6)
            return false;

        foreach (object item in creatures)
        {
            Dictionary<string, object> creature = item as Dictionary<string, object>;
            if (creature == null || !creature.ContainsKey("ID") || !(creature["ID"] is string) ||
                CreatureDataManager.Instance.GetData((string)creature["ID"]) == null)
                return false;
        }

        List<CustomQuestController.RewardEntry> parsedRewards = new List<CustomQuestController.RewardEntry>();
        HashSet<string> rewardTypes = new HashSet<string>();
        foreach (object item in rewards)
        {
            CustomQuestController.RewardEntry reward;
            if (!CustomQuestController.TryReward(item as Dictionary<string, object>, out reward) ||
                !rewardTypes.Add(reward.Type)) return false;
            parsedRewards.Add(reward);
        }

        LeaderData leader = null;
        LeaderData skin = null;
        object leaderValue;
        Dictionary<string, object> leaderRow = row.TryGetValue("owner_leader", out leaderValue)
            ? leaderValue as Dictionary<string, object> : null;
        object leaderId, skinId;
        if (leaderRow != null && leaderRow.TryGetValue("id", out leaderId) && leaderId is string &&
            leaderRow.TryGetValue("skin", out skinId) && skinId is string)
        {
            leader = LeaderDataManager.Instance.GetData((string)leaderId);
            skin = LeaderDataManager.Instance.GetData((string)skinId);
            if (leader != null && skin != leader &&
                (leader.AlternateSkins == null || !leader.AlternateSkins.Contains(skin))) skin = leader;
        }

        entry = new CustomQuestController.QuestEntry
        {
            Controller = mReturnController,
            Id = id,
            Name = (string)nameValue,
            OwnerName = (string)ownerNameValue,
            Creatures = creatures,
            Rewards = parsedRewards,
            OwnerLeader = leader,
            OwnerLeaderSkin = skin
        };
        return true;
    }

    private void SetQuestStatus(string value)
    {
        if (QuestStatus != null) QuestStatus.text = value;
    }

    public bool IsQuestSelected(CustomQuestController.QuestEntry entry)
    {
        return entry != null && mEditing != null && entry.Id == mEditing.Id;
    }

   public void SelectQuest(CustomQuestController.QuestEntry entry)
    {
        if (!IsSelectingQuest || mBusy ||
            entry == null || entry.Controller != mReturnController)
            return;

        mEditing = entry;

        RefreshQuestTiles();

        IsSelectingQuest = true;

        if (CurrentQuestsPanel != null)
            CurrentQuestsPanel.SetActive(true);

        if (EditorPanel != null)
            EditorPanel.SetActive(true);

        LoadEditor(entry);
    }

    private void LoadEditor(CustomQuestController.QuestEntry entry)
    {
        QuestName.value = entry.Name;
        mSelectedCreatures.Clear();
        foreach (object value in entry.Creatures)
        {
            Dictionary<string, object> creature = value as Dictionary<string, object>;
            int uniqueId;
            if (creature == null || !TryInt(creature, "UniqueID", out uniqueId))
            {
                ShowMessage("A dungeon creature has invalid inventory data.");
                return;
            }
            mSelectedCreatures.Add(uniqueId);
        }

        mRewards.Clear();
        foreach (CustomQuestController.RewardEntry reward in entry.Rewards)
        {
            mRewards.Add(new Dictionary<string, object>
            {
                { "type", reward.Type }, { "amount", reward.Amount }, { "item_id", reward.ItemId }
            });
        }
        mRewardSlotIndex = 0;
        mRewardOptionIndex = FindRewardOption(mRewards[mRewardSlotIndex]);

        PlayerInfoScript player = Singleton<PlayerInfoScript>.Instance;
        mLeaderIndex = 0;
        if (entry.OwnerLeader != null)
        {
            for (int i = 0; i < player.SaveData.Leaders.Count; i++)
            {
                LeaderItem candidate = player.SaveData.Leaders[i];
                if (candidate != null && candidate.Form != null && candidate.Form.ID == entry.OwnerLeader.ID)
                {
                    mLeaderIndex = i;
                    break;
                }
            }
        }
        ClearCreatureGrid();
        BuildCreatureGrid();
        RefreshSelection();
        RefreshLeader();
        RefreshReward();
    }

    private void BuildCreatureGrid()
    {
        PlayerInfoScript player = Singleton<PlayerInfoScript>.Instance;

        if (player == null || player.SaveData == null ||
            player.SaveData.InventorySlots == null)
            return;

        GameObject prefab = Singleton<PrefabReferences>.Instance.InventoryTile;

        if (prefab == null)
        {
            ShowMessage("Inventory tile prefab is missing.");
            return;
        }

        InventoryTile tilePrefab = prefab.GetComponent<InventoryTile>();

        if (tilePrefab == null)
        {
            ShowMessage("Invalid inventory tile prefab.");
            return;
        }

        UIPanel gridPanel =
            NGUITools.FindInParents<UIPanel>(CreatureGrid.transform.parent);

        if (gridPanel == null)
        {
            ShowMessage("Creature grid panel is missing.");
            return;
        }

        gridPanel.ResetAndUpdateAnchors();

        float width = gridPanel.GetViewSize().x;

        if (width <= 0f)
        {
            ShowMessage("The creature grid has no available width.");
            return;
        }

        CreatureGrid.cellWidth = width / 3.001f;

        ClearCreatureGrid();
        mOwnedCreatures.Clear();

        foreach (InventorySlotItem item in player.SaveData.InventorySlots)
        {
            if (item != null &&
                item.SlotType == InventorySlotType.Creature &&
                item.Creature != null)
            {
                mOwnedCreatures.Add(item);
            }
        }

        InventoryTile.SetDelegates(
            InventorySlotType.Creature,
            null,
            null,
            null,
            RefreshCreatureOverlay,
            null,
            null,
            ClickCreature
        );

        float tileWidth = Mathf.Max(
            tilePrefab.FrameSprite.width,
            tilePrefab.PortraitCollider.size.x
        );

        float tileHeight = Mathf.Max(
            tilePrefab.FrameSprite.height,
            tilePrefab.PortraitCollider.size.y
        );

        float scale = 0.9f * Mathf.Min(
            CreatureGrid.cellWidth / tileWidth,
            CreatureGrid.cellHeight / tileHeight
        );

        foreach (InventorySlotItem item in mOwnedCreatures)
        {
            GameObject obj = NGUITools.AddChild(
                CreatureGrid.gameObject,
                prefab
            );

            obj.ChangeLayer(CreatureGrid.gameObject.layer);

            InventoryTile tile = obj.GetComponent<InventoryTile>();

            if (tile == null)
            {
                NGUITools.Destroy(obj);
                continue;
            }

            tile.Populate(item);

            obj.transform.localScale = Vector3.one * scale;

            RefreshCreatureOverlay(tile);
        }

        CreatureGrid.Reposition();
        CreatureScrollView.ResetPosition();

        RefreshSelection();
    }

    private bool ClickCreature(InventoryTile tile)
    {
        if (mBusy || tile == null ||
            tile.InventoryItem == null ||
            tile.InventoryItem.Creature == null)
            return false;

        int id = tile.InventoryItem.Creature.UniqueId;

        if (id <= 0)
            return false;

        if (mSelectedCreatures.Contains(id))
        {
            mSelectedCreatures.Remove(id);
        }
        else if (mSelectedCreatures.Count < 12)
        {
            mSelectedCreatures.Add(id);
        }
        else
        {
            ShowMessage("A Custom Dungeon requires exactly 12 creatures.");
        }

        RefreshAllCreatureOverlays();
        RefreshSelection();

        return false;
    }

    private void RefreshAllCreatureOverlays()
    {
        if (CreatureGrid == null) return;

        for (int i = 0; i < CreatureGrid.transform.childCount; i++)
        {
            InventoryTile tile =
                CreatureGrid.transform.GetChild(i).GetComponent<InventoryTile>();

            if (tile != null)
                RefreshCreatureOverlay(tile);
        }
    }

    private void RefreshCreatureOverlay(InventoryTile tile)
    {
        if (tile == null || tile.InventoryItem == null || tile.InventoryItem.Creature == null) return;
        bool selected = mSelectedCreatures.Contains(tile.InventoryItem.Creature.UniqueId);
        tile.SetOverlayStatus(selected ? InventoryTile.TileStatus.SelectedAsMaterial :
            InventoryTile.TileStatus.NotSelected, false);
    }

    private void RefreshSelection()
    {
        if (SelectionLabel != null) SelectionLabel.text = mSelectedCreatures.Count + " / 12";
    }

    public void OnClickPreviousLeader() { ChangeLeader(-1); }
    public void OnClickNextLeader() { ChangeLeader(1); }

    private void ChangeLeader(int direction)
    {
        if (mBusy) return;
        List<LeaderItem> leaders = Singleton<PlayerInfoScript>.Instance.SaveData.Leaders;
        if (leaders == null || leaders.Count == 0) return;
        mLeaderIndex = (mLeaderIndex + direction + leaders.Count) % leaders.Count;
        RefreshLeader();
    }

    private void RefreshLeader()
    {
        List<LeaderItem> leaders = Singleton<PlayerInfoScript>.Instance.SaveData.Leaders;
        LeaderItem leader = mLeaderIndex >= 0 && mLeaderIndex < leaders.Count ? leaders[mLeaderIndex] : null;
        if (leader == null || leader.Form == null || leader.SelectedSkin == null)
        {
            LeaderName.text = "Hero unavailable";
            LeaderPortrait.UnloadTexture();
            return;
        }
        LeaderName.text = leader.Form.Name;
        LeaderPortrait.ReplaceTexture(leader.SelectedSkin.PortraitTexture);
    }

    public void OnClickPreviousRewardSlot() { ChangeRewardSlot(-1); }
    public void OnClickNextRewardSlot() { ChangeRewardSlot(1); }
    public void OnClickPreviousRewardOption() { ChangeRewardOption(-1); }
    public void OnClickNextRewardOption() { ChangeRewardOption(1); }

    private void ChangeRewardSlot(int direction)
    {
        if (mBusy || mRewards.Count == 0) return;
        mRewardSlotIndex = (mRewardSlotIndex + direction + mRewards.Count) % mRewards.Count;
        mRewardOptionIndex = FindRewardOption(mRewards[mRewardSlotIndex]);
        RefreshReward();
    }

    private void ChangeRewardOption(int direction)
    {
        if (mBusy || mRewards.Count == 0) return;
        if (mRewardOptionIndex < 0) mRewardOptionIndex = direction > 0 ? 0 : RewardOptions.Length - 1;
        else mRewardOptionIndex = (mRewardOptionIndex + direction + RewardOptions.Length) % RewardOptions.Length;

        RewardOption option = RewardOptions[mRewardOptionIndex];
        string itemId = option.Type == "key" ? GachaKeyId :
            option.Type == "shard" ? EvolutionShardId :
            option.Type == "cake" ? (string.IsNullOrEmpty(XPMaterialId) ? XPMaterial.ToString() : XPMaterialId) : string.Empty;

        for (int i = 0; i < mRewards.Count; i++)
        {
            if (i != mRewardSlotIndex && Convert.ToString(mRewards[i]["type"]) == option.Type)
            {
                ShowMessage("Each reward type can only be used once.");
                return;
            }
        }

        Dictionary<string, object> candidate = new Dictionary<string, object>
        {
            { "type", option.Type }, { "amount", option.Amount }, { "item_id", itemId ?? string.Empty }
        };
        CustomQuestController.RewardEntry parsed;
        if (!CustomQuestController.TryReward(candidate, out parsed))
        {
            ShowMessage("This reward is not configured in the current game data.");
            return;
        }
        mRewards[mRewardSlotIndex] = candidate;
        RefreshReward();
    }

    private int FindRewardOption(Dictionary<string, object> reward)
    {
        if (reward == null) return -1;
        string type = Convert.ToString(reward["type"]);
        int amount;
        if (!int.TryParse(Convert.ToString(reward["amount"]), out amount)) return -1;
        for (int i = 0; i < RewardOptions.Length; i++)
            if (RewardOptions[i].Type == type && RewardOptions[i].Amount == amount) return i;
        return -1;
    }

    private void RefreshReward()
    {
        if (mRewardTile != null) NGUITools.Destroy(mRewardTile);
        mRewardTile = null;
        if (mRewards.Count == 0)
        {
            RewardName.text = "No reward";
            if (RewardAmount != null) RewardAmount.text = string.Empty;
            return;
        }

        Dictionary<string, object> data = mRewards[mRewardSlotIndex];
        CustomQuestController.RewardEntry reward;
        if (!CustomQuestController.TryReward(data, out reward))
        {
            RewardName.text = "Reward unavailable";
            if (RewardAmount != null) RewardAmount.text = string.Empty;
            return;
        }
        string displayName = reward.Key != null ? reward.Key.KeyName :
            reward.Shard != null ? reward.Shard.Name :
            reward.Cake != null ? reward.Cake.Name : reward.Type;
        RewardName.text = displayName;
        if (RewardAmount != null) RewardAmount.text = reward.Amount.ToString();
        mRewardTile = CustomQuestController.SpawnRewardTile(RewardNode, RewardNode.gameObject.layer, reward);
    }

    public void OnClickConfirm()
    {
        if (mBusy || mEditing == null) return;
        string name = QuestName.value == null ? string.Empty : QuestName.value.Trim();
        if (mSelectedCreatures.Count != 12 || mRewards.Count < 1 || !ValidName(name))
        {
            ShowMessage("Choose exactly 12 creatures and enter a valid quest name (1-60 characters).");
            return;
        }
        SimplePopupController popup = Singleton<SimplePopupController>.Instance;
        if (popup == null)
        {
            Debug.LogError("[Olympic Edit02] SimplePopupController is unavailable.");
            return;
        }
        popup.ShowMessage("Olympic Dungeon", "Save changes to " + name + "?", SaveChanges);
    }

    private void SaveChanges()
    {
        if (mBusy || mEditing == null) return;
        PlayerInfoScript player = Singleton<PlayerInfoScript>.Instance;
        if (mLeaderIndex < 0 || mLeaderIndex >= player.SaveData.Leaders.Count)
        {
            ShowMessage("The selected hero is unavailable.");
            return;
        }
        LeaderItem leader = player.SaveData.Leaders[mLeaderIndex];
        if (leader == null || leader.Form == null || leader.SelectedSkin == null)
        {
            ShowMessage("The selected hero is unavailable.");
            return;
        }

        List<Dictionary<string, object>> rewards = new List<Dictionary<string, object>>();
        foreach (Dictionary<string, object> reward in mRewards)
            rewards.Add(new Dictionary<string, object>(reward));

        string playerCode = player.GetPlayerCode();
        string requestId = Guid.NewGuid().ToString();
        int dungeonId = mEditing.Id;
        string name = QuestName.value.Trim();
        List<int> creatures = new List<int>(mSelectedCreatures);
        string leaderId = leader.Form.ID;

        mBusy = true;
        Server.OlympicEdit(requestId, dungeonId, name, leaderId, creatures, rewards,
            delegate(Dictionary<string, object> response, HttpStatusCode status)
        {
            if (this == null || Singleton<PlayerInfoScript>.Instance != player ||
                player.GetPlayerCode() != playerCode) return;
            mBusy = false;
            object success;
            if (status == HttpStatusCode.OK && response != null &&
                response.TryGetValue("success", out success) && success is bool && (bool)success)
            {
                ShowMessage("Custom Quest updated.");
                ReturnToQuestList();
                return;
            }
            ShowMessage(ReadError(response));
        });
    }

    public void OnClickRemove()
    {
        if (mBusy || mEditing == null) return;
        SimplePopupController popup = Singleton<SimplePopupController>.Instance;
        if (popup == null)
        {
            Debug.LogError("[Olympic Edit02] SimplePopupController is unavailable.");
            return;
        }
        popup.ShowMessage("Olympic Dungeon", "Permanently remove " + mEditing.Name + "?", DeleteSelectedQuest);
    }

    private void DeleteSelectedQuest()
    {
        if (mBusy || mEditing == null) return;
        string playerCode = Singleton<PlayerInfoScript>.Instance.GetPlayerCode();
        int id = mEditing.Id;
        mBusy = true;
        Server.OlympicDelete(Guid.NewGuid().ToString(), id,
            delegate(Dictionary<string, object> response, HttpStatusCode status)
        {
            if (this == null || Singleton<PlayerInfoScript>.Instance.GetPlayerCode() != playerCode) return;
            mBusy = false;
            object success;
            if (status == HttpStatusCode.OK && response != null &&
                response.TryGetValue("success", out success) && success is bool && (bool)success)
            {
                ShowMessage("Custom Quest removed.");
                ReturnToQuestList();
            }
            else ShowMessage(ReadError(response));
        });
    }
    
    public void OnClickBack()
    {
        if (mBusy) return;

        mRequest++;
        ClearQuestGrid();
        ClearCreatureGrid();

        mEntries.Clear();
        mOwnedCreatures.Clear();
        mSelectedCreatures.Clear();
        InventoryTile.ClearDelegates(true);

        if (mRewardTile != null)
            NGUITools.Destroy(mRewardTile);

        mRewardTile = null;

        if (LeaderPortrait != null)
            LeaderPortrait.UnloadTexture();

        if (Panel != null)
            Panel.SetActive(false);

        if (CurrentQuestsPanel != null)
            CurrentQuestsPanel.SetActive(false);

        if (EditorPanel != null)
            EditorPanel.SetActive(false);

        IsSelectingQuest = false;
        mEditing = null;

        EditCustomDungeon creationEditor =
            EditPanel01 != null
                ? EditPanel01.GetComponent<EditCustomDungeon>()
                : null;

        if (creationEditor == null && EditPanel01 != null)
            creationEditor = EditPanel01.GetComponentInChildren<EditCustomDungeon>(true);

        if (creationEditor == null)
        {
            Debug.LogError("[Olympic Edit02] EditCustomDungeon component was not found.");
            return;
        }

        if (!creationEditor.gameObject.activeSelf)
            creationEditor.gameObject.SetActive(true);

        if (creationEditor.Panel != null)
            creationEditor.Panel.SetActive(true);

        if (creationEditor.EditPanel02 != null)
            creationEditor.EditPanel02.SetActive(false);
    }


    private void ReturnToQuestList()
    {
        mEditing = null;

        ClearCreatureGrid();
        mOwnedCreatures.Clear();
        mSelectedCreatures.Clear();

        InventoryTile.ClearDelegates(true);

        if (mRewardTile != null)
            NGUITools.Destroy(mRewardTile);

        mRewardTile = null;

        if (LeaderPortrait != null)
            LeaderPortrait.UnloadTexture();

        if (EditorPanel != null)
            EditorPanel.SetActive(false);

        if (CurrentQuestsPanel != null)
            CurrentQuestsPanel.SetActive(true);

        IsSelectingQuest = true;

        LoadCurrentQuests();
    }

    public void Close()
    {
        if (mBusy) return;

        mRequest++;

        ClearQuestGrid();
        ClearCreatureGrid();

        mEntries.Clear();
        mOwnedCreatures.Clear();
        mSelectedCreatures.Clear();

        InventoryTile.ClearDelegates(true);

        if (mRewardTile != null)
            NGUITools.Destroy(mRewardTile);

        mRewardTile = null;

        if (LeaderPortrait != null)
            LeaderPortrait.UnloadTexture();

        if (Panel != null)
            Panel.SetActive(false);

        if (mDeactivateRootOnClose)
            gameObject.SetActive(false);

        mDeactivateRootOnClose = false;
        IsSelectingQuest = false;
        mEditing = null;

        CustomQuestController controller = mReturnController;
        mReturnController = null;

        if (controller != null)
            controller.OnEditCustomDungeon02Closed();
    }

    private static bool ValidName(string value)
    {
        if (string.IsNullOrEmpty(value) || value.Length > 60 || value.IndexOfAny(new char[] { '[', ']' }) >= 0)
            return false;
        foreach (char c in value) if (char.IsControl(c)) return false;
        return true;
    }

    private static bool TryResponse(Dictionary<string, object> response, HttpStatusCode status, out object data)
    {
        data = null;
        object success;
        return status == HttpStatusCode.OK && response != null &&
            response.TryGetValue("success", out success) && success is bool && (bool)success &&
            response.TryGetValue("data", out data);
    }

    private static bool TryInt(Dictionary<string, object> row, string key, out int value)
    {
        object raw;
        value = 0;
        return row != null && row.TryGetValue(key, out raw) &&
            int.TryParse(Convert.ToString(raw), out value);
    }

    private static string ReadError(Dictionary<string, object> response)
    {
        object error;
        return response != null && response.TryGetValue("error", out error)
            ? Convert.ToString(error) : "The server could not complete the request.";
    }

    private void ShowMessage(string message)
    {
        Debug.LogWarning("[Olympic Edit02] " + message);
        if (QuestStatus != null) QuestStatus.text = message;
        SimplePopupController popup = Singleton<SimplePopupController>.Instance;
        if (popup != null) popup.ShowMessage("Olympic Dungeon", message);
    }

    private void ClearGrid(UIGrid grid)
    {
        if (grid == null) return;

        for (int i = grid.transform.childCount - 1; i >= 0; i--)
        {
            Transform child = grid.transform.GetChild(i);
            NGUITools.Destroy(child.gameObject);
        }

        grid.Reposition();
    }

    private void ClearQuestGrid()
    {
        ClearGrid(QuestGrid);
    }

    private void ClearCreatureGrid()
    {
        ClearGrid(CreatureGrid);
    }

    private void RefreshQuestTiles()
    {
        if (QuestGrid == null) return;

        for (int i = 0; i < QuestGrid.transform.childCount; i++)
        {
            EditQuestTile tile =
                QuestGrid.transform.GetChild(i).GetComponent<EditQuestTile>();

            if (tile != null)
                tile.RefreshSelectedState();
        }
    }
}
