using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

public class CustomQuestController : MonoBehaviour
{
	public class LeagueEntry
	{
		public CustomQuestController Controller;
		public int Id;
		public string OwnerId;
		public string Name;
		public string OwnerName;
		public int QuestCount;
	}

	public class QuestEntry
	{
		public CustomQuestController Controller;
		public int Id;
		public string Name;
		public string OwnerName;
		public List<object> Creatures;
		public List<RewardEntry> Rewards;
		public LeaderData OwnerLeader;
		public LeaderData OwnerLeaderSkin;
	}

	public class RewardEntry
	{
		public string Type;
		public int Amount;
		public string ItemId;
		public XPMaterialData Cake;
		public EvoMaterialData Shard;
		public GachaSlotData Key;
	}

	public GameObject LeagueTilePrefab;
	public GameObject QuestTilePrefab;
	public UIStreamingGrid LeagueGrid;
	public UIScrollView LeagueScrollView;
	public UIStreamingGrid QuestGrid;
	public UIScrollView QuestScrollView;
	public GameObject LeaguePanel;
	public GameObject QuestPanel;
	public GameObject BackButton;
	public GameObject ScrollBar;
	public UITweenController ShowTween;
	public UITweenController ShowQuestsTween;
	public UITweenController HideQuestsTween;
	public UILabel TitleLabel;
	public UILabel PlayerStamina;
	public UILabel PlayerStaminaTimer;
	public UILabel StatusLabel;
	public UILabel RewardsLabel;
	public CustomPreMatchController PreMatch;
	public EditCustomDungeon EditDungeon;
	public EditCustomDungeon02 EditDungeon02;
	private bool mEditingExistingDungeon02;

	private readonly UIStreamingGridDataSource<LeagueEntry> mLeagueData = new UIStreamingGridDataSource<LeagueEntry>();
	private readonly UIStreamingGridDataSource<QuestEntry> mQuestData = new UIStreamingGridDataSource<QuestEntry>();
	private int mOffset;
	private int mQuestOffset;
	private string mOwnerId;
	private int mRequest;
	private bool mHasNext;
	private bool mIsOpen;
	private bool mShowingQuest;
	private bool mTransitioning;
	private bool mEditingDungeon;
	private Coroutine mHideRoutine;

	private SQServer Server => SessionManager.Instance.theSession.Server;
	public void Open()
	{
		gameObject.SetActive(true);
		CancelHideTransition();
		mIsOpen = true;
		mShowingQuest = false;
		mTransitioning = false;
		mOffset = 0;
		if (ShowTween == null || ShowQuestsTween == null || HideQuestsTween == null ||
			TitleLabel == null || PlayerStamina == null || PlayerStaminaTimer == null)
		{
			Debug.LogWarning("[Olympic] Assign the optional tweens and header labels to enable the full selection screen.");
		}
		if (TitleLabel != null) TitleLabel.text = "Olympic Dungeons";
		if (ShowTween != null) ShowTween.Play();
		UpdateTimers();
		ShowList();
	}

	public void Close()
	{
		CancelHideTransition();
		mRequest++;
		mIsOpen = false;
		mShowingQuest = false;
		mTransitioning = false;
		ClearTiles();
		if (BackButton != null) BackButton.SetActive(false);
		gameObject.SetActive(false);
	}

	public void OnClickEditCustomDungeon()
	{
		if (!mIsOpen || mTransitioning || mEditingDungeon) return;
		if (EditDungeon == null)
		{
			Debug.LogError("[Olympic] Assign the quest editor to CustomQuestController.");
			Singleton<SimplePopupController>.Instance.ShowMessage("Olympic Dungeon", "The quest editor is not configured.");
			return;
		}
		if (EditDungeon.ShowFrom(this))
		{
			mEditingDungeon = true;
			mRequest++;
			gameObject.SetActive(false);
		}
	}

	public void OnClickEditCustomDungeon02()
	{
		if (!mIsOpen || mTransitioning || mEditingDungeon) return;
		if (EditDungeon02 == null)
		{
			Singleton<SimplePopupController>.Instance.ShowMessage("Olympic Dungeon", "The existing quest editor is not configured.");
			return;
		}
		if (EditDungeon02.ShowFrom(this))
		{
			mEditingDungeon = true;
			mEditingExistingDungeon02 = true;
			mRequest++;
			gameObject.SetActive(false);
		}
	}

	internal void OnEditCustomDungeon02Closed()
	{
		if (!mEditingExistingDungeon02) return;
		mEditingExistingDungeon02 = false;
		mEditingDungeon = false;
		if (!mIsOpen) return;
		gameObject.SetActive(true);
		mShowingQuest = false;
		mOffset = 0;
		ShowList();
	}

	internal void OnEditCustomDungeonClosed(bool created)
	{
		if (!mEditingDungeon) return;
		mEditingDungeon = false;
		if (!mIsOpen) return;
		gameObject.SetActive(true);
		if (created)
		{
			mShowingQuest = false;
			mOffset = 0;
		}
		if (mShowingQuest) ShowDungeonPage();
		else ShowList();
	}

	public void Back()
	{
		if (mTransitioning)
		{
			return;
		}
		if (mShowingQuest && HideQuestsTween != null)
		{
			mRequest++;
			mTransitioning = true;
			if (BackButton != null) BackButton.SetActive(false);
			mHideRoutine = StartCoroutine(HideQuestAndShowList());
			return;
		}
		mShowingQuest = false;
		ShowList();
	}

	private IEnumerator HideQuestAndShowList()
	{
		HideQuestsTween.Play();
		while (HideQuestsTween != null && HideQuestsTween.AnyTweenPlaying())
		{
			yield return null;
		}
		mTransitioning = false;
		mShowingQuest = false;
		mHideRoutine = null;
		if (mIsOpen)
		{
			ShowList();
		}
	}

	private void CancelHideTransition()
	{
		if (mHideRoutine != null)
		{
			StopCoroutine(mHideRoutine);
			mHideRoutine = null;
			if (HideQuestsTween != null) HideQuestsTween.End();
		}
	}

	private void Update()
	{
		if (mIsOpen)
		{
			UpdateTimers();
		}
	}

	private void UpdateTimers()
	{
		if (PlayerStamina == null || PlayerStaminaTimer == null || !PlayerStamina.gameObject.activeInHierarchy)
		{
			return;
		}
		int currentStamina;
		int maxStamina;
		int secondsUntilNextStamina;
		DetachedSingleton<StaminaManager>.Instance.GetStaminaInfo(StaminaType.Quests, out currentStamina, out maxStamina, out secondsUntilNextStamina);
		PlayerStamina.text = maxStamina.ToString();
		PlayerStaminaTimer.text = secondsUntilNextStamina <= 0 ? string.Empty : PlayerInfoScript.BuildTimerString(secondsUntilNextStamina);
	}

	public void NextPage()
	{
		if (!mTransitioning && mHasNext)
		{
			if (mShowingQuest) { mQuestOffset += 30; ShowDungeonPage(); }
			else { mOffset += 30; ShowList(); }
		}
	}

	public void PreviousPage()
	{
		if (!mTransitioning && mShowingQuest && mQuestOffset > 0)
		{
			mQuestOffset -= 30;
			ShowDungeonPage();
		}
		else if (!mTransitioning && !mShowingQuest && mOffset > 0)
		{
			mOffset = Math.Max(0, mOffset - 30);
			ShowList();
		}
	}

	private bool ValidateReferences()
	{
		QuestSelectLeague league = LeagueTilePrefab != null ? LeagueTilePrefab.GetComponent<QuestSelectLeague>() : null;
		QuestSelectQuest quest = QuestTilePrefab != null ? QuestTilePrefab.GetComponent<QuestSelectQuest>() : null;
		UIScrollView leagueParent = LeagueGrid != null ? NGUITools.FindInParents<UIScrollView>(LeagueGrid.transform.parent) : null;
		UIScrollView questParent = QuestGrid != null ? NGUITools.FindInParents<UIScrollView>(QuestGrid.transform.parent) : null;
		if (LeagueScrollView == null) LeagueScrollView = leagueParent;
		if (QuestScrollView == null) QuestScrollView = questParent;
		if (LeagueTilePrefab != null && QuestTilePrefab != null && LeagueGrid != null &&
			QuestGrid != null && LeagueGrid != QuestGrid && LeaguePanel != null && QuestPanel != null &&
			StatusLabel != null &&
			league != null && league.Name != null && league.Battles != null && league.BossPortrait != null &&
			quest != null && quest.Name != null && quest.CreatureNodeParent != null &&
			HasClick(LeagueTilePrefab, league, "OnLeagueClicked") &&
			HasClick(QuestTilePrefab, quest, "OnQuestClicked") &&
			LeagueGrid.cellWidth > 0f && LeagueGrid.cellHeight > 0f &&
			QuestGrid.cellWidth > 0f && QuestGrid.cellHeight > 0f &&
			LeagueScrollView != null && LeagueScrollView == leagueParent &&
			NGUITools.FindInParents<UIPanel>(LeagueGrid.transform.parent) != null &&
			QuestScrollView != null && QuestScrollView == questParent &&
			NGUITools.FindInParents<UIPanel>(QuestGrid.transform.parent) != null)
		{
			return true;
		}
		Debug.LogError("[Olympic] CustomQuestController has invalid scene references or tile prefabs.");
		return false;
	}

	private static bool HasClick(GameObject prefab, MonoBehaviour target, string method)
	{
		foreach (UIEventTrigger trigger in prefab.GetComponentsInChildren<UIEventTrigger>(true))
		{
			foreach (EventDelegate entry in trigger.onClick)
			{
				if (entry != null && entry.target == target && entry.methodName == method)
				{
					return true;
				}
			}
		}
		return false;
	}

	private void ShowList()
	{
		if (!ValidateReferences())
		{
			return;
		}
		int request = ++mRequest;
		mHasNext = false;
		ClearTiles();
		LeaguePanel.SetActive(true);
		QuestPanel.SetActive(false);
		if (BackButton != null) BackButton.SetActive(false);
		if (ScrollBar != null) ScrollBar.SetActive(true);
		if (RewardsLabel != null) RewardsLabel.text = string.Empty;
		StatusLabel.text = "Loading dungeons...";
		Server.OlympicOwners(mOffset, delegate(Dictionary<string, object> response, HttpStatusCode status)
		{
			if (this == null || request != mRequest)
			{
				return;
			}
			object data;
			List<object> rows;
			if (!TryResponse(response, status, out data))
			{
				return;
			}
			rows = data as List<object>;
			if (rows == null)
			{
				ShowError("Invalid dungeon list.");
				return;
			}
			List<LeagueEntry> entries = new List<LeagueEntry>();
			foreach (object value in rows)
			{
				Dictionary<string, object> row = value as Dictionary<string, object>;
				int count;
				if (row == null || !row.ContainsKey("owner_id") || !(row["owner_id"] is string) ||
					!row.ContainsKey("owner_name") || !(row["owner_name"] is string) ||
					!row.ContainsKey("quest_count") || !int.TryParse(Convert.ToString(row["quest_count"]), out count))
				{
					ShowError("Invalid dungeon list entry.");
					return;
				}
				entries.Add(new LeagueEntry
				{
					Controller = this,
					OwnerId = (string)row["owner_id"],
					Name = (string)row["owner_name"],
					OwnerName = (string)row["owner_name"],
					QuestCount = count
				});
			}
			mHasNext = rows.Count == 30;
			StatusLabel.text = rows.Count == 0 ? "No dungeons found." : string.Empty;
			mLeagueData.Init(LeagueGrid, LeagueTilePrefab, entries, true);
		});
	}

	public void ShowDungeon(string ownerId)
	{
		mOwnerId = ownerId;
		mQuestOffset = 0;
		mShowingQuest = true;
		LeaguePanel.SetActive(false);
		QuestPanel.SetActive(true);
		if (BackButton != null) BackButton.SetActive(true);
		if (ShowQuestsTween != null) ShowQuestsTween.Play();
		ShowDungeonPage();
	}

	private void ShowDungeonPage()
	{
		QuestSelectQuest prefab = QuestTilePrefab.GetComponent<QuestSelectQuest>();
		GameObject inventoryPrefab = Singleton<PrefabReferences>.Instance.InventoryTile;
		InventoryTile inventory = inventoryPrefab != null ? inventoryPrefab.GetComponent<InventoryTile>() : null;
		if (prefab == null || prefab.Name == null || prefab.CreatureNodeParent == null ||
			inventory == null || inventory.CurrencyParent == null || inventory.CurrencySprite == null ||
			inventory.CurrencyAmount == null || inventory.PortraitTexture == null)
		{
			ShowError("Invalid Olympic quest tile prefab.");
			return;
		}
		for (int i = 0; i < 12; i++)
			if (prefab.CreatureNodeParent.Find("CreatureNode_" + (i + 1).ToString("D2")) == null)
			{
				ShowError("Olympic quest tile must contain twelve creature nodes.");
				return;
			}
		for (int i = 0; i < 6; i++)
			if (prefab.GetOlympicRewardNode(i) == null)
			{
				ShowError("Olympic quest tile must contain six reward nodes.");
				return;
			}
		ClearTiles();
		mHasNext = false;
		StatusLabel.text = "Loading quests...";
		int request = ++mRequest;
		Server.OlympicQuests(mOwnerId, mQuestOffset, delegate(Dictionary<string, object> response, HttpStatusCode status)
		{
			if (this == null || request != mRequest) return;
			object data;
			if (!TryResponse(response, status, out data)) return;
			List<object> rows = data as List<object>;
			if (rows == null) { ShowError("Invalid quest list."); return; }
			List<int> ids = new List<int>();
			foreach (object value in rows)
			{
				int id;
				Dictionary<string, object> row = value as Dictionary<string, object>;
				if (row == null || !TryId(row, out id)) { ShowError("Invalid quest list entry."); return; }
				ids.Add(id);
			}
			mHasNext = rows.Count == 30;
			LoadQuestPage(ids, 0, new List<QuestEntry>());
		});
	}

	private void LoadQuestPage(List<int> ids, int index, List<QuestEntry> entries)
	{
		if (index == ids.Count)
		{
			StatusLabel.text = entries.Count == 0 ? "No quests found." : string.Empty;
			mQuestData.Init(QuestGrid, QuestTilePrefab, entries, true);
			return;
		}
		FetchDungeon(ids[index], delegate(QuestEntry quest)
		{
			entries.Add(quest);
			LoadQuestPage(ids, index + 1, entries);
		});
	}

	public void ShowPreMatch(QuestEntry selected)
	{
		if (!mIsOpen || !mShowingQuest || mTransitioning || selected == null || selected.Controller != this)
		{
			return;
		}
		if (PreMatch == null)
		{
			ShowError("Olympic pre-match screen is not configured.");
			return;
		}
		FetchDungeon(selected.Id, delegate(QuestEntry dungeon)
		{
			if (PreMatch.Show(dungeon))
			{
				StatusLabel.text = string.Empty;
			}
			else
			{
				ShowError("Olympic pre-match screen could not be opened.");
			}
		});
	}

	private void FetchDungeon(int id, Action<QuestEntry> onSuccess)
	{
		int request = ++mRequest;
		StatusLabel.text = "Loading dungeon...";
		Server.OlympicGet(id, delegate(Dictionary<string, object> response, HttpStatusCode status)
		{
			if (this == null || request != mRequest)
			{
				return;
			}
			object data;
			if (!TryResponse(response, status, out data))
			{
				return;
			}
			Dictionary<string, object> row = data as Dictionary<string, object>;
			object creatureValue;
			object rewardValue;
			List<object> creatures;
			List<object> rewards;
			int returnedId;
			if (row == null || !TryId(row, out returnedId) || returnedId != id ||
				!row.ContainsKey("name") || !(row["name"] is string) ||
				!row.ContainsKey("owner_name") || !(row["owner_name"] is string) ||
				!row.TryGetValue("creatures", out creatureValue) ||
				(creatures = creatureValue as List<object>) == null || creatures.Count != 12 ||
				!row.TryGetValue("rewards", out rewardValue) || (rewards = rewardValue as List<object>) == null ||
				rewards.Count < 1 || rewards.Count > 6)
			{
				ShowError("Invalid dungeon details.");
				return;
			}
			foreach (object value in creatures)
			{
				Dictionary<string, object> creature = value as Dictionary<string, object>;
				if (creature == null || !creature.ContainsKey("ID") || !(creature["ID"] is string) ||
					CreatureDataManager.Instance.GetData((string)creature["ID"]) == null)
				{
					ShowError("Dungeon creature data is unavailable.");
					return;
				}
			}
			List<RewardEntry> rewardEntries = new List<RewardEntry>();
			HashSet<string> rewardTypes = new HashSet<string>();
			foreach (object value in rewards)
			{
				RewardEntry entry;
				if (!TryReward(value as Dictionary<string, object>, out entry) || !rewardTypes.Add(entry.Type))
				{
					ShowError("Dungeon reward data or its icon is unavailable.");
					return;
				}
				rewardEntries.Add(entry);
			}
			LeaderData ownerLeader = null;
			LeaderData ownerLeaderSkin = null;
			object leaderValue;
			Dictionary<string, object> leader = row.TryGetValue("owner_leader", out leaderValue)
				? leaderValue as Dictionary<string, object> : null;
			object leaderId;
			object skinId;
			if (leader != null && leader.TryGetValue("id", out leaderId) && leaderId is string &&
				leader.TryGetValue("skin", out skinId) && skinId is string)
			{
				ownerLeader = LeaderDataManager.Instance.GetData((string)leaderId);
				ownerLeaderSkin = LeaderDataManager.Instance.GetData((string)skinId);
				if (ownerLeader != null && ownerLeaderSkin != ownerLeader &&
					(ownerLeader.AlternateSkins == null || !ownerLeader.AlternateSkins.Contains(ownerLeaderSkin)))
				{
					Debug.LogWarning("[Olympic] Dungeon creator's leader skin is unavailable.");
					ownerLeaderSkin = ownerLeader;
				}
			}
			if (ownerLeader == null || ownerLeaderSkin == null)
			{
				Debug.LogWarning("[Olympic] Dungeon creator's leader is unavailable; the opponent portrait cannot be displayed.");
			}
			onSuccess(new QuestEntry
			{
				Controller = this, Id = id, Name = (string)row["name"], OwnerName = (string)row["owner_name"],
				Creatures = creatures, Rewards = rewardEntries, OwnerLeader = ownerLeader, OwnerLeaderSkin = ownerLeaderSkin
			});
		});
	}

	public void PopulateLeague(QuestSelectLeague league, LeagueEntry entry)
	{
		league.Name.text = entry.Name;
		league.Battles.text = entry.QuestCount + (entry.QuestCount == 1 ? " Quest" : " Quests");
		if (league.Lock != null) league.Lock.SetActive(false);
		if (league.HideWhenLocked != null) league.HideWhenLocked.SetActive(true);
		if (league.UnavailableOverlay != null) league.UnavailableOverlay.SetActive(false);
	}

	public void PopulateQuest(QuestSelectQuest quest, QuestEntry entry)
	{
		InventoryTile.ClearDelegates(false);
		quest.Name.text = entry.Name;
		if (quest.Index != null) quest.Index.text = string.Empty;
		if (quest.Stamina != null) quest.Stamina.text = string.Empty;
		if (quest.MapCount != null) quest.MapCount.text = string.Empty;
		if (quest.Lock != null) quest.Lock.SetActive(false);
		if (quest.HideWhenLocked != null) quest.HideWhenLocked.SetActive(true);
		if (quest.NewLabel != null) quest.NewLabel.SetActive(false);
		if (quest.BossBanner != null) quest.BossBanner.SetActive(false);
		foreach (GameObject star in quest.Stars)
		{
			if (star != null) star.SetActive(false);
		}
		for (int i = 0; i < entry.Creatures.Count; i++)
		{
			Transform node = quest.CreatureNodeParent.Find("CreatureNode_" + (i + 1).ToString("D2"));
			GameObject portrait = node.InstantiateAsChild(Singleton<PrefabReferences>.Instance.InventoryTile);
			quest.TrackOlympicTile(portrait);
			portrait.ChangeLayer(quest.gameObject.layer);
			portrait.GetComponent<InventoryTile>().Populate(
				new InventorySlotItem(new CreatureItem((Dictionary<string, object>)entry.Creatures[i], true)));
			foreach (Collider collider in portrait.GetComponentsInChildren<Collider>(true))
			{
				collider.enabled = false;
			}
		}
		for (int i = 0; i < entry.Rewards.Count; i++)
		{
			Transform node = quest.GetOlympicRewardNode(i);
			GameObject rewardObject = SpawnRewardTile(node, quest.gameObject.layer, entry.Rewards[i]);
			quest.TrackOlympicTile(rewardObject);
		}
	}

	public static GameObject SpawnRewardTile(Transform node, int layer, RewardEntry reward)
	{
		GameObject rewardObject = node.InstantiateAsChild(Singleton<PrefabReferences>.Instance.InventoryTile);
		rewardObject.ChangeLayer(layer);
		InventoryTile tile = rewardObject.GetComponent<InventoryTile>();
		tile.Populate(new InventorySlotItem(InventorySlotType.Empty));
		if (reward.Cake != null) tile.Populate(new InventorySlotItem(reward.Cake));
		else if (reward.Shard != null) tile.Populate(new InventorySlotItem(reward.Shard));
		else if (reward.Key != null) tile.Populate(reward.Key);
		tile.CurrencyParent.SetActive(true);
		tile.CurrencyAmount.text = reward.Amount.ToString();
		tile.CurrencySprite.gameObject.SetActive(reward.Type == "trade" || reward.Type == "craft" || reward.Type == "pvp");
		if (reward.Type == "pvp")
		{
			tile.PortraitTexture.gameObject.SetActive(false);
			tile.CurrencySprite.spriteName = "Icon_Currency_PVPCurrency";
		}
		else if (reward.Type == "trade" || reward.Type == "craft")
		{
			tile.PortraitTexture.gameObject.SetActive(false);
			if (reward.Type == "trade") CurrencyUI.ReplaceWithTradeIcon(tile.CurrencySprite);
			else CurrencyUI.ReplaceWithCraftIcon(tile.CurrencySprite);
		}
		foreach (Collider collider in rewardObject.GetComponentsInChildren<Collider>(true))
		{
			collider.enabled = false;
		}
		return rewardObject;
	}

	public static bool TryReward(Dictionary<string, object> data, out RewardEntry reward)
	{
		reward = null;
		object type;
		object amountValue;
		object itemId;
		int amount;
		if (data == null || !data.TryGetValue("type", out type) || !(type is string) ||
			!data.TryGetValue("amount", out amountValue) || !int.TryParse(Convert.ToString(amountValue), out amount) || amount <= 0 ||
			!data.TryGetValue("item_id", out itemId) || !(itemId is string))
		{
			return false;
		}
		RewardEntry entry = new RewardEntry { Type = (string)type, Amount = amount, ItemId = (string)itemId };
		switch (entry.Type)
		{
		case "trade":
			if (entry.ItemId != string.Empty || CurrencyUI.TradeIcon == null) return false;
			break;
		case "craft":
			if (entry.ItemId != string.Empty || CurrencyUI.CraftIcon == null) return false;
			break;
		case "pvp":
			if (entry.ItemId != string.Empty) return false;
			break;
		case "cake":
			entry.Cake = XPMaterialDataManager.Instance.GetData(entry.ItemId);
			if (entry.Cake == null) return false;
			break;
		case "shard":
			entry.Shard = EvoMaterialDataManager.Instance.GetData(entry.ItemId);
			if (entry.Shard == null || !entry.Shard.AwakenMat) return false;
			break;
		case "key":
			entry.Key = GachaSlotDataManager.Instance.GetData(entry.ItemId);
			if (entry.Key == null || string.IsNullOrEmpty(entry.Key.KeyUITexture)) return false;
			break;
		default:
			return false;
		}
		reward = entry;
		return true;
	}

	private static bool TryId(Dictionary<string, object> row, out int id)
	{
		object value;
		id = 0;
		return row.TryGetValue("id", out value) && int.TryParse(Convert.ToString(value), out id) && id > 0;
	}

	private bool TryResponse(Dictionary<string, object> response, HttpStatusCode status, out object data)
	{
		data = null;
		object success;
		if (status == HttpStatusCode.OK && response != null && response.TryGetValue("success", out success) &&
			success is bool && (bool)success && response.TryGetValue("data", out data))
		{
			return true;
		}
		object error;
		ShowError(response != null && response.TryGetValue("error", out error) ?
			Convert.ToString(error) : "Could not load Olympic dungeons.");
		return false;
	}

	private void ShowError(string message)
	{
		Debug.LogError("[Olympic] " + message);
		StatusLabel.text = message;
	}

	public void BattleUnavailable()
	{
		Singleton<SimplePopupController>.Instance.ShowMessage("Olympic Dungeon", "Olympic battles are not available yet.", null);
	}

	private void ClearTiles()
	{
		StopScrolling(LeagueScrollView);
		StopScrolling(QuestScrollView);
		mLeagueData.Clear();
		mQuestData.Clear();
	}

	private static void StopScrolling(UIScrollView scrollView)
	{
		if (scrollView != null)
		{
			scrollView.currentMomentum = Vector3.zero;
			scrollView.DisableSpring();
		}
	}

	public void OnQuestClicked(QuestSelectQuest questObject)
	{
		if (questObject != null)
		{
			if (mEditingExistingDungeon02 && EditDungeon02 != null && EditDungeon02.IsSelectingQuest)
			{
				EditDungeon02.SelectQuest(questObject.OlympicEntry);
				return;
			}
			ShowPreMatch(questObject.OlympicEntry);
		}
	}
}
