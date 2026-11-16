using System;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

public class EditCustomDungeon : MonoBehaviour
{
	public enum XPMaterialOption
	{
		CAKE_T1_Corn,
		CAKE_T1_Plains,
		CAKE_T1_Sand,
		CAKE_T1_Nice,
		CAKE_T1_Swamp,
		CAKE_T2_Corn,
		CAKE_T2_Plains,
		CAKE_T2_Sand,
		CAKE_T2_Nice,
		CAKE_T2_Swamp,
		CAKE_T3_Corn,
		CAKE_T3_Plains,
		CAKE_T3_Sand,
		CAKE_T3_Nice,
		CAKE_T3_Swamp,
		CAKE_T1_Icy,
		CAKE_T2_Icy,
		CAKE_T3_Icy
	}

	private class RewardOption
	{
		public string Type;
		public int Amount;
		public int Price;
		public RewardOption(string type, int amount, int price)
		{
			Type = type;
			Amount = amount;
			Price = price;
		}
	}

	private static readonly RewardOption[] Options =
	{
		new RewardOption("key", 1, 500),
		new RewardOption("shard", 1, 2000),
		new RewardOption("shard", 3, 5000),
		new RewardOption("pvp", 100, 300),
		new RewardOption("pvp", 250, 500),
		new RewardOption("pvp", 500, 750),
		new RewardOption("craft", 1, 1500),
		new RewardOption("craft", 3, 2500),
		new RewardOption("craft", 6, 4000),
		new RewardOption("trade", 100, 3000),
		new RewardOption("trade", 300, 5000),
		new RewardOption("trade", 500, 7000),
		new RewardOption("trade", 1000, 11000),
		new RewardOption("cake", 3, 300),
		new RewardOption("cake", 10, 1000)
	};

	public GameObject Panel;
	public UIStreamingGrid CreatureGrid;
	public UIScrollView CreatureScrollView;
	public UITexture LeaderPortrait;
	public UILabel LeaderName;
	public Transform RewardNode;
	public UILabel RewardName;
	public UILabel CostLabel;
	public UILabel SelectionLabel;
	public UIInput QuestName;
	public string GachaKeyId;
	public string EvolutionShardId;
	public XPMaterialOption XPMaterial;
	[Tooltip("Optional custom XP material ID; leave empty to use XPMaterial.")]
	public string XPMaterialId;

	private readonly UIStreamingGridDataSource<InventorySlotItem> mGrid = new UIStreamingGridDataSource<InventorySlotItem>();
	private readonly List<int> mSelected = new List<int>();
	private GameObject mRewardTile;
	private int mLeaderIndex;
	private int mRewardIndex = -1;
	private string mRequestId;
	private string mPendingOwner;
	private string mPendingName;
	private string mPendingLeaderId;
	private Dictionary<string, object> mPendingReward;
	private List<int> mPendingCreatures;
	private int mPendingCost;
	private bool mSaving;
	private bool mUncertain;
	private bool mDeactivateRootOnClose;
	private CustomQuestController mReturnController;

	private string PendingKey
	{
		get { return "Olympic.PendingCreate." + Singleton<PlayerInfoScript>.Instance.GetPlayerCode(); }
	}

	public void Show()
	{
		TryShow();
	}

	public bool ShowFrom(CustomQuestController controller)
	{
		if (controller == null || Panel == null || transform.IsChildOf(controller.transform) ||
			Panel.transform.IsChildOf(controller.transform))
		{
			Debug.LogError("[Olympic] The quest editor must be outside the quest selection hierarchy.");
			Singleton<SimplePopupController>.Instance.ShowMessage("Olympic Dungeon", "The quest editor is not configured correctly.");
			return false;
		}
		if (!TryShow()) return false;
		mReturnController = controller;
		return true;
	}

	private bool TryShow()
	{
		if (mSaving || mReturnController != null) return false;
		PlayerInfoScript player = Singleton<PlayerInfoScript>.Instance;
		GameObject tile = Singleton<PrefabReferences>.Instance != null ? Singleton<PrefabReferences>.Instance.InventoryTile : null;
		InventoryTile tilePrefab = tile != null ? tile.GetComponent<InventoryTile>() : null;
		UIPanel gridPanel = CreatureGrid != null ? NGUITools.FindInParents<UIPanel>(CreatureGrid.transform.parent) : null;
		UIScrollView gridScroll = CreatureGrid != null ? NGUITools.FindInParents<UIScrollView>(CreatureGrid.transform.parent) : null;
		if (Panel == null || CreatureGrid == null || CreatureScrollView == null || gridScroll != CreatureScrollView ||
			CreatureScrollView.movement != UIScrollView.Movement.Vertical || gridPanel == null ||
			CreatureGrid.cellHeight <= 0f || tilePrefab == null || tilePrefab.PortraitCollider == null || tilePrefab.FrameSprite == null ||
			tilePrefab.PortraitCollider.size.x <= 0f || tilePrefab.PortraitCollider.size.y <= 0f ||
			LeaderPortrait == null || LeaderName == null || RewardNode == null || RewardName == null ||
			CostLabel == null || QuestName == null || player == null || player.SaveData == null ||
			player.SaveData.Leaders.Count == 0)
		{
			Debug.LogError("[Olympic] Quest creation scene references are invalid.");
			Singleton<SimplePopupController>.Instance.ShowMessage("Olympic Dungeon", "The quest creation screen is not configured.");
			return false;
		}
		if (mUncertain && mPendingOwner != player.GetPlayerCode())
		{
			mUncertain = false;
			mRequestId = null;
		}
		if (!mUncertain && PlayerPrefs.HasKey(PendingKey) && !LoadPending())
		{
			Debug.LogError("[Olympic] Saved pending create request is invalid.");
			Singleton<SimplePopupController>.Instance.ShowMessage("Olympic Dungeon", "The pending quest request cannot be read. Contact support before creating another quest.");
			return false;
		}
		mDeactivateRootOnClose = !gameObject.activeSelf;
		if (mDeactivateRootOnClose) gameObject.SetActive(true);
		if (!gameObject.activeInHierarchy)
		{
			if (mDeactivateRootOnClose) gameObject.SetActive(false);
			Debug.LogError("[Olympic] Quest editor has an inactive parent.");
			Singleton<SimplePopupController>.Instance.ShowMessage("Olympic Dungeon", "The quest editor cannot be opened.");
			return false;
		}
		Panel.SetActive(true);
		if (!mUncertain)
		{
			mSelected.Clear();
			mRewardIndex = -1;
			mRequestId = null;
			QuestName.value = string.Empty;
			mLeaderIndex = 0;
			if (player.SaveData.SelectedLoadout >= 0 && player.SaveData.SelectedLoadout < player.SaveData.Loadouts.Count)
			{
				Loadout loadout = player.SaveData.Loadouts[player.SaveData.SelectedLoadout];
				if (loadout != null) mLeaderIndex = player.SaveData.Leaders.IndexOf(loadout.Leader);
			}
			if (mLeaderIndex < 0) mLeaderIndex = 0;
		}
		else
		{
			QuestName.value = mPendingName;
		}
		QuestName.enabled = !mUncertain;
		gridPanel.ResetAndUpdateAnchors();
		float width = gridPanel.GetViewSize().x;
		if (width <= 0f)
		{
			Debug.LogError("[Olympic] Creature grid panel has no width.");
			Singleton<SimplePopupController>.Instance.ShowMessage("Olympic Dungeon", "The creature grid is not configured.");
			Panel.SetActive(false);
			if (mDeactivateRootOnClose) gameObject.SetActive(false);
			return false;
		}
		CreatureGrid.cellWidth = width / 3.001f;
		List<InventorySlotItem> creatures = new List<InventorySlotItem>();
		foreach (InventorySlotItem item in player.SaveData.InventorySlots)
			if (item != null && item.SlotType == InventorySlotType.Creature && item.Creature != null)
				creatures.Add(item);
		InventoryTile.SetDelegates(InventorySlotType.Creature, null, null, null, RefreshOverlay, null, null, ClickCreature);
		mGrid.Init(CreatureGrid, tile, creatures, true);
		float tileWidth = Mathf.Max(tilePrefab.FrameSprite.width, tilePrefab.PortraitCollider.size.x);
		float tileHeight = Mathf.Max(tilePrefab.FrameSprite.height, tilePrefab.PortraitCollider.size.y);
		float scale = 0.9f * Mathf.Min(CreatureGrid.cellWidth / tileWidth, CreatureGrid.cellHeight / tileHeight);
		for (int i = 0; i < CreatureGrid.transform.childCount; i++)
			CreatureGrid.transform.GetChild(i).localScale = Vector3.one * scale;
		RefreshLeader();
		RefreshReward();
		RefreshSelection();
		if (mUncertain)
			Singleton<SimplePopupController>.Instance.ShowMessage("Olympic Dungeon", "A previous quest request is pending. Press Create to check its result.");
		return true;
	}

	private bool ClickCreature(InventoryTile tile)
	{
		if (mSaving || mUncertain) return false;
		int id = tile.InventoryItem.Creature.UniqueId;
		if (id <= 0)
		{
			Debug.LogError("[Olympic] Creature has an invalid inventory ID.");
			Singleton<SimplePopupController>.Instance.ShowMessage("Olympic Dungeon", "This creature cannot be selected.");
			return false;
		}
		if (mSelected.Contains(id)) mSelected.Remove(id);
		else if (mSelected.Count < 12) mSelected.Add(id);
		else Singleton<SimplePopupController>.Instance.ShowMessage("Olympic Dungeon", "Choose exactly 12 creatures.");
		mGrid.RepopulateObjects();
		RefreshSelection();
		return false;
	}

	private void RefreshOverlay(InventoryTile tile)
	{
		bool selected = tile.InventoryItem.Creature != null && mSelected.Contains(tile.InventoryItem.Creature.UniqueId);
		tile.SetOverlayStatus(selected ? InventoryTile.TileStatus.SelectedAsMaterial : InventoryTile.TileStatus.NotSelected, false);
	}

	private void RefreshSelection()
	{
		if (SelectionLabel != null) SelectionLabel.text = mSelected.Count + " / 12";
	}

	public void OnClickPreviousLeader() { ChangeLeader(-1); }
	public void OnClickNextLeader() { ChangeLeader(1); }

	private void ChangeLeader(int direction)
	{
		if (mSaving || mUncertain) return;
		int count = Singleton<PlayerInfoScript>.Instance.SaveData.Leaders.Count;
		if (count == 0) return;
		mLeaderIndex = (mLeaderIndex + direction + count) % count;
		RefreshLeader();
	}

	private void RefreshLeader()
	{
		List<LeaderItem> leaders = Singleton<PlayerInfoScript>.Instance.SaveData.Leaders;
		LeaderItem leader = mLeaderIndex >= 0 && mLeaderIndex < leaders.Count ? leaders[mLeaderIndex] : null;
		if (leader == null || leader.Form == null || leader.SelectedSkin == null)
		{
			Debug.LogError("[Olympic] Selected leader is unavailable.");
			LeaderName.text = "Leader unavailable";
			LeaderPortrait.UnloadTexture();
			return;
		}
		LeaderName.text = leader.Form.Name;
		LeaderPortrait.ReplaceTexture(leader.SelectedSkin.PortraitTexture);
	}

	public void OnClickPreviousReward() { ChangeReward(-1); }
	public void OnClickNextReward() { ChangeReward(1); }

	private void ChangeReward(int direction)
	{
		if (mSaving || mUncertain) return;
		if (mRewardIndex < 0) mRewardIndex = direction > 0 ? 0 : Options.Length - 1;
		else mRewardIndex = (mRewardIndex + direction + Options.Length) % Options.Length;
		RefreshReward();
	}

	private Dictionary<string, object> RewardData()
	{
		RewardOption option = Options[mRewardIndex];
		string id = option.Type == "key" ? GachaKeyId : option.Type == "shard" ? EvolutionShardId :
			option.Type == "cake" ? (string.IsNullOrEmpty(XPMaterialId) ? XPMaterial.ToString() : XPMaterialId) : string.Empty;
		return new Dictionary<string, object> { { "type", option.Type }, { "amount", option.Amount }, { "item_id", id ?? string.Empty } };
	}

	private void RefreshReward()
	{
		if (mRewardTile != null) NGUITools.Destroy(mRewardTile);
		mRewardTile = null;
		if (mRewardIndex < 0)
		{
			RewardName.text = "Choose a reward";
			CostLabel.text = "Cost: 500 Gems + reward";
			return;
		}
		RewardOption option = Options[mRewardIndex];
		CostLabel.text = "Cost: " + (mUncertain ? mPendingCost : 500 + option.Price) + " Gems";
		CustomQuestController.RewardEntry reward;
		if (!CustomQuestController.TryReward(mUncertain ? mPendingReward : RewardData(), out reward))
		{
			RewardName.text = option.Type + ": reward ID not configured";
			Debug.LogWarning("[Olympic] Reward is unavailable: " + option.Type);
			return;
		}
		RewardName.text = reward.Amount + " x " + (reward.Key != null ? reward.Key.KeyName :
			reward.Shard != null ? reward.Shard.Name : reward.Cake != null ? reward.Cake.Name : option.Type);
		mRewardTile = CustomQuestController.SpawnRewardTile(RewardNode, RewardNode.gameObject.layer, reward);
	}

	public void OnClickCreate()
	{
		if (mSaving) return;
		if (mUncertain)
		{
			ResolvePending();
			return;
		}
		string name = QuestName.value != null ? QuestName.value.Trim() : string.Empty;
		if (mSelected.Count != 12 || mRewardIndex < 0 || name.Length == 0 || name.Length > 60 ||
			name.IndexOfAny(new char[] { '[', ']' }) >= 0)
		{
			Singleton<SimplePopupController>.Instance.ShowMessage("Olympic Dungeon", "Choose 12 creatures, a reward, and a valid quest name (1-60 characters).");
			return;
		}
		foreach (char c in name)
			if (char.IsControl(c))
			{
				Singleton<SimplePopupController>.Instance.ShowMessage("Olympic Dungeon", "Quest name contains invalid characters.");
				return;
			}
		CustomQuestController.RewardEntry reward;
		if (!CustomQuestController.TryReward(RewardData(), out reward))
		{
			Singleton<SimplePopupController>.Instance.ShowMessage("Olympic Dungeon", "The selected reward is not configured.");
			return;
		}
		LeaderItem leader = Singleton<PlayerInfoScript>.Instance.SaveData.Leaders[mLeaderIndex];
		if (leader == null || leader.Form == null || leader.SelectedSkin == null)
		{
			Singleton<SimplePopupController>.Instance.ShowMessage("Olympic Dungeon", "The selected leader is unavailable.");
			return;
		}
		int cost = 500 + Options[mRewardIndex].Price;
		Dictionary<string, object> rewardData = RewardData();
		List<int> creatures = new List<int>(mSelected);
		string leaderId = leader.Form.ID;
		int rewardIndex = mRewardIndex;
		string owner = Singleton<PlayerInfoScript>.Instance.GetPlayerCode();
		Singleton<SimplePopupController>.Instance.ShowPurchasePrompt("Create this quest for <val1>?", "Not enough gems.", cost,
			delegate
			{
				if (Singleton<PlayerInfoScript>.Instance.GetPlayerCode() != owner)
				{
					Debug.LogWarning("[Olympic] Player changed before quest creation was confirmed.");
					Singleton<SimplePopupController>.Instance.ShowMessage("Olympic Dungeon", "The player session changed. Please try again.");
					return;
				}
				mRequestId = Guid.NewGuid().ToString();
				mPendingOwner = owner;
				mPendingName = name;
				mPendingLeaderId = leaderId;
				mPendingReward = rewardData;
				mPendingCreatures = creatures;
				mPendingCost = cost;
				mRewardIndex = rewardIndex;
				SavePending();
				Submit();
			});
	}

	private void SavePending()
	{
		Dictionary<string, object> pending = new Dictionary<string, object>
		{
			{ "request_id", mRequestId }, { "name", mPendingName }, { "leader_id", mPendingLeaderId },
			{ "reward", mPendingReward }, { "creatures", mPendingCreatures }, { "reward_index", mRewardIndex },
			{ "expected_cost", mPendingCost }
		};
		PlayerPrefs.SetString(PendingKey, MiniJSON.Json.Serialize(pending));
		PlayerPrefs.Save();
	}

	private bool LoadPending()
	{
		Dictionary<string, object> data = MiniJSON.Json.Deserialize(PlayerPrefs.GetString(PendingKey)) as Dictionary<string, object>;
		object request, name, leader, reward, creatures, index, cost;
		int rewardIndex, expectedCost;
		Guid requestId;
		if (data == null || !data.TryGetValue("request_id", out request) || !(request is string) ||
			!Guid.TryParse((string)request, out requestId) ||
			!data.TryGetValue("name", out name) || !(name is string) ||
			!data.TryGetValue("leader_id", out leader) || !(leader is string) ||
			!data.TryGetValue("reward", out reward) || !(reward is Dictionary<string, object>) ||
			!data.TryGetValue("creatures", out creatures) || !(creatures is List<object>) ||
			!data.TryGetValue("reward_index", out index) || !int.TryParse(Convert.ToString(index), out rewardIndex) ||
			!data.TryGetValue("expected_cost", out cost) || !int.TryParse(Convert.ToString(cost), out expectedCost) || expectedCost <= 0 ||
			rewardIndex < 0 || rewardIndex >= Options.Length)
			return false;
		List<int> ids = new List<int>();
		foreach (object value in (List<object>)creatures)
		{
			int id;
			if (!int.TryParse(Convert.ToString(value), out id) || id <= 0 || ids.Contains(id)) return false;
			ids.Add(id);
		}
		if (ids.Count != 12) return false;
		mRequestId = (string)request;
		mPendingOwner = Singleton<PlayerInfoScript>.Instance.GetPlayerCode();
		mPendingName = (string)name;
		mPendingLeaderId = (string)leader;
		mPendingReward = (Dictionary<string, object>)reward;
		mPendingCreatures = ids;
		mPendingCost = expectedCost;
		mSelected.Clear();
		mSelected.AddRange(ids);
		mRewardIndex = rewardIndex;
		List<LeaderItem> leaders = Singleton<PlayerInfoScript>.Instance.SaveData.Leaders;
		mLeaderIndex = leaders.FindIndex(item => item != null && item.Form != null && item.Form.ID == mPendingLeaderId);
		if (mLeaderIndex < 0)
		{
			Debug.LogWarning("[Olympic] Pending quest leader is no longer available.");
		}
		mUncertain = true;
		return true;
	}

	private void ClearPending()
	{
		PlayerPrefs.DeleteKey("Olympic.PendingCreate." + mPendingOwner);
		PlayerPrefs.Save();
		mRequestId = null;
	}

	private void Submit()
	{
		if (mSaving) return;
		mSaving = true;
		string pendingOwner = mPendingOwner;
		string requestId = mRequestId;
		SessionManager.Instance.theSession.Server.OlympicCreate(mRequestId, mPendingName, mPendingLeaderId, mPendingCost, mPendingCreatures,
			new List<Dictionary<string, object>> { mPendingReward }, delegate(Dictionary<string, object> response, HttpStatusCode status)
		{
			if (Singleton<PlayerInfoScript>.Instance.GetPlayerCode() != pendingOwner || mRequestId != requestId)
			{
				mSaving = false;
				Debug.LogWarning("[Olympic] Player changed while a quest request was pending.");
				return;
			}
			object success;
			if (status == HttpStatusCode.OK && response != null && response.TryGetValue("success", out success) &&
				success is bool && (bool)success)
			{
				Complete();
				return;
			}
			mSaving = false;
			mUncertain = status != HttpStatusCode.BadRequest && status != HttpStatusCode.Conflict;
			QuestName.enabled = !mUncertain;
			if (mUncertain) QuestName.value = mPendingName;
			if (!mUncertain) ClearPending();
			object error;
			string message = response != null && response.TryGetValue("error", out error) ? Convert.ToString(error) : "Could not create quest.";
			Debug.LogError("[Olympic] " + message);
			if (mUncertain) message += " The result is uncertain; press Create to check and retry.";
			Singleton<SimplePopupController>.Instance.ShowMessage("Olympic Dungeon", message);
		});
	}

	private void ResolvePending()
	{
		mSaving = true;
		string pendingOwner = mPendingOwner;
		string requestId = mRequestId;
		SessionManager.Instance.theSession.Server.OlympicCreateStatus(mRequestId, delegate(Dictionary<string, object> response, HttpStatusCode status)
		{
			mSaving = false;
			if (Singleton<PlayerInfoScript>.Instance.GetPlayerCode() != pendingOwner || mRequestId != requestId)
			{
				Debug.LogWarning("[Olympic] Player changed while checking a quest request.");
				return;
			}
			object success;
			object dataValue;
			object found;
			Dictionary<string, object> data = response != null && response.TryGetValue("data", out dataValue)
				? dataValue as Dictionary<string, object> : null;
			if (status != HttpStatusCode.OK || response == null || !response.TryGetValue("success", out success) ||
				!(success is bool) || !(bool)success || data == null || !data.TryGetValue("found", out found) || !(found is bool))
			{
				Debug.LogError("[Olympic] Could not resolve the previous create request.");
				Singleton<SimplePopupController>.Instance.ShowMessage("Olympic Dungeon", "Could not check the previous request. Press Create to retry.");
				return;
			}
			if ((bool)found) Complete();
			else Submit();
		});
	}

	private void Complete()
	{
		mSaving = false;
		mUncertain = false;
		ClearPending();
		QuestName.enabled = true;
		CloseEditor(true);
		Singleton<SimplePopupController>.Instance.ShowMessage("Olympic Dungeon", "Quest created.");
	}

	public void OnClickBack()
	{
		OnCloseClicked();
	}

	public void OnCloseClicked()
	{
		CloseEditor(false);
	}

	private void CloseEditor(bool created)
	{
		if (mSaving) return;
		mGrid.Clear();
		InventoryTile.ClearDelegates(true);
		if (mRewardTile != null) NGUITools.Destroy(mRewardTile);
		mRewardTile = null;
		LeaderPortrait.UnloadTexture();
		if (Panel != null) Panel.SetActive(false);
		if (mDeactivateRootOnClose) gameObject.SetActive(false);
		mDeactivateRootOnClose = false;
		CustomQuestController controller = mReturnController;
		mReturnController = null;
		if (controller != null) controller.OnEditCustomDungeonClosed(created);
	}
}
