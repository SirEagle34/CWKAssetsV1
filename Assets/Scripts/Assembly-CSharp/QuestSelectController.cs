using System;
using System.Collections.Generic;
using UnityEngine;

public class QuestSelectController : Singleton<QuestSelectController>
{
	public class SpecialLeagueEntry
	{
		public LeagueData League;

		public string TextString;

		public string FullTextString;

		public bool Clickable;

		public DateTime StartTime;

		public DateTime EndTime;

		public bool Expired;

		public SpecialLeagueEntry(LeagueData league, string textString, string fullTextString, bool clickable, bool expired, DateTime startTime, DateTime endTime)
		{
			League = league;
			TextString = textString;
			FullTextString = fullTextString;
			Clickable = clickable;
			StartTime = startTime;
			EndTime = endTime;
			Expired = expired;
		}
	}

	public const string DungeonMapLeague = "DungeonMaps";

	public const string DailyRandomLeague = "DailyRandom";

	public float QuestListOffset = 808f;

	public GameObject LeaguePrefab;

	public GameObject SpecialLeaguePrefab;

	public GameObject QuestPrefab;

	public GameObject RandomBattlePrefab;

	public UITweenController ShowTween;

	public UITweenController ShowQuestsTween;

	public UITweenController HideQuestsTween;

	public UILabel TitleLabel;

	public UILabel PlayerStamina;

	public UILabel PlayerStaminaTimer;
	
    [Header("Daily Dungeon Unlock")]
    public GameObject UnlockAllDailyDungeonsButton;
    public UILabel UnlockDailyDungeonsCostLabel;

	public UIStreamingGrid LeagueGrid;

	public UIScrollView LeagueScrollView;

	private UIStreamingGridDataSource<LeagueData> mLeagueGridDataSource = new UIStreamingGridDataSource<LeagueData>();

	private UIStreamingGridDataSource<SpecialLeagueEntry> mSpecialLeagueGridDataSource = new UIStreamingGridDataSource<SpecialLeagueEntry>();

	public UIStreamingGrid QuestGrid;

	public UIScrollView QuestScrollView;

	private UIStreamingGridDataSource<QuestData> mQuestGridDataSource = new UIStreamingGridDataSource<QuestData>();

	public GameObject BackButton;

	private QuestSelectLeague mShowingLeague;

	private List<SpecialLeagueEntry> mSpecialLeagueEntries = new List<SpecialLeagueEntry>();

	private bool mShowingSpecialLeagues;

	public GameObject BackgroundMain;

	public GameObject BackgroundSpecial;

	public GameObject ScrollBar;

	public string DailyDungeonLeagueID = string.Empty;
	
	// Session-based unlock flag
    private bool hasUnlockedAllDailyDungeons = false;

  // The 8 daily dungeon league IDs (7 days + SnackHour)
    private readonly string[] dailyDungeonLeagueIDs = new string[]
    {
        "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday", "SnackHour"
    };

	private QuestSelectQuest selectedQuest;

	public void Populate(string param)
	{
		if (param == "Main")
		{
			TitleLabel.text = KFFLocalization.Get("!!BUILDING_QUESTS");
		    mShowingSpecialLeagues = false;
		    UpdateDailyDungeonUnlockButtonVisibility(); // Hide button in main quest menu
		    bool flag = false;
			List<LeagueData> leagues = LeagueDataManager.Instance.GetLeagues(QuestLineEnum.Main);
			mLeagueGridDataSource.SetScrollPos(new Vector2(Singleton<PlayerInfoScript>.Instance.SaveData.QuestSelectScrollPos, 0f), LeagueGrid);
			mLeagueGridDataSource.Init(LeagueGrid, LeaguePrefab, leagues);
			LeagueData leagueData = null;
			LeagueData leagueData2 = null;
			LeagueData leagueData3 = null;
			foreach (LeagueData item in leagues)
			{
				if (Singleton<PlayerInfoScript>.Instance.IsLeagueUnlocking(item))
				{
					leagueData3 = item;
				}
				else if (Singleton<PlayerInfoScript>.Instance.IsLeagueUnlocked(item))
				{
					leagueData2 = item;
				}
			}
			if (leagueData3 != null)
			{
				GameObject gameObject = mLeagueGridDataSource.FindPrefab(leagueData3);
				QuestSelectLeague component = gameObject.GetComponent<QuestSelectLeague>();
				if (component.Lock != null)
				{
					component.Lock.SetActive(true);
				}
				if (component.HideWhenLocked != null)
				{
					component.HideWhenLocked.SetActive(false);
				}
				GameObject gameObject2 = mLeagueGridDataSource.FindPrefab(leagueData2);
				QuestSelectLeague component2 = gameObject2.GetComponent<QuestSelectLeague>();
				component2.CompleteTween.PlayWithCallback(component.DelayedShowUnlockedElements);
			}
			else
			{
				QuestData questData = leagueData2.Quests.Find((QuestData m) => Singleton<PlayerInfoScript>.Instance.IsQuestUnlocking(m));
				if (questData != null)
				{
					flag = true;
					GameObject gameObject3 = mLeagueGridDataSource.FindPrefab(leagueData2);
					OnLeagueClicked(gameObject3.GetComponent<QuestSelectLeague>());
					GameObject gameObject4 = mQuestGridDataSource.FindPrefab(questData);
					QuestSelectQuest component3 = gameObject4.GetComponent<QuestSelectQuest>();
					component3.DelayedShowUnlockedElements();
				}
			}
			Singleton<PlayerInfoScript>.Instance.SaveData.TopShownCompletedQuestId = Singleton<PlayerInfoScript>.Instance.SaveData.TopCompletedQuestId;
			BackgroundMain.SetActive(true);
			BackgroundSpecial.SetActive(false);
			BackButton.SetActive(false);
			Singleton<TutorialController>.Instance.AdvanceIfTargetingBuilding("TBuilding_Quests");
			if (flag)
			{
				Singleton<TutorialController>.Instance.AdvanceIfOnState("SC_TapLeague");
				Singleton<TutorialController>.Instance.AdvanceIfOnState("Q3_LeagueList");
				Singleton<TutorialController>.Instance.AdvanceIfOnState("Q2_LeagueList");
			}
		}
		else if (param == "Special")
		{
			TitleLabel.text = KFFLocalization.Get("!!BUILDING_DUNGEON");
			DetachedSingleton<DailyRandomDungeonManager>.Instance.BuildDailyRandomBattles();
			mShowingSpecialLeagues = true;
		    mSpecialLeagueEntries = BuildSpecialLeagueList();
		    mSpecialLeagueGridDataSource.Init(LeagueGrid, SpecialLeaguePrefab, mSpecialLeagueEntries, true);
		    UpdateDailyDungeonUnlockButtonVisibility(); // Show button in dungeon menu
		    BackgroundMain.SetActive(false);
			BackgroundSpecial.SetActive(true);
			BackButton.SetActive(false);
			Singleton<TutorialController>.Instance.AdvanceIfTargetingBuilding("TBuilding_Dungeon");
			if (!Singleton<TutorialController>.Instance.IsAnyTutorialActive())
			{
				UpsightRequester.RequestContent("enter_dungeon");
			}
			if (DailyDungeonLeagueID != string.Empty)
			{
				foreach (SpecialLeagueEntry mSpecialLeagueEntry in mSpecialLeagueEntries)
				{
					if (mSpecialLeagueEntry.League.ID == DailyDungeonLeagueID)
					{
						DailyDungeonLeagueID = string.Empty;
						GameObject gameObject5 = mSpecialLeagueGridDataSource.FindPrefab(mSpecialLeagueEntry);
						OnLeagueClicked(gameObject5.GetComponent<QuestSelectLeague>());
						break;
					}
				}
			}
		}
		UpdateTimers();
	}

	public void Unload()
{
	Singleton<TownHudController>.Instance.ReturnToTownView();
	mLeagueGridDataSource.Clear();
	mQuestGridDataSource.Clear();
	mSpecialLeagueGridDataSource.Clear();
	mShowingLeague = null;
	
	// Hide unlock button
	if (UnlockAllDailyDungeonsButton != null)
	{
		UnlockAllDailyDungeonsButton.SetActive(false);
	}
}

		public void Show(string param)
	{
		ResetDailyDungeonUnlock(); 
		ShowTween.Play();
		Populate(param);
	}

       private void OnEnable()
{
	// Reset unlock when panel becomes active
	// Check actual league state instead of the flag (which gets reset on scene load)
	Debug.Log("[QuestSelect] OnEnable - Checking if leagues are force-visible");
	
	bool anyLeagueForceVisible = false;
	foreach (string leagueID in dailyDungeonLeagueIDs)
	{
		LeagueData league = LeagueDataManager.Instance.GetData(leagueID);
		if (league != null && league.ForceVisible)
		{
			anyLeagueForceVisible = true;
			Debug.Log($"[QuestSelect] Found force-visible league: {leagueID}");
			break;
		}
	}
	
	if (anyLeagueForceVisible)
	{
		Debug.Log("[QuestSelect] OnEnable - Resetting unlock (leagues are force-visible)");
		
		// Reset all leagues
		foreach (string leagueID in dailyDungeonLeagueIDs)
		{
			LeagueData league = LeagueDataManager.Instance.GetData(leagueID);
			if (league != null)
			{
				league.ForceVisible = false;
			}
		}
		
		// Rebuild dungeons
		DetachedSingleton<DailyRandomDungeonManager>.Instance.BuildDailyRandomBattles();
		
		// Refresh UI if showing special leagues
		if (mSpecialLeagueGridDataSource != null && LeagueGrid != null)
		{
			mSpecialLeagueEntries = BuildSpecialLeagueList();
			mSpecialLeagueGridDataSource.Init(LeagueGrid, SpecialLeaguePrefab, mSpecialLeagueEntries, true);
			Debug.Log("[QuestSelect] Refreshed special league UI after reset");
		}
		
		Debug.Log("[QuestSelect] OnEnable - Reset complete");
	}
	else
	{
		Debug.Log("[QuestSelect] OnEnable - No reset needed (no force-visible leagues)");
	}
}

	private void Update()
	{
		UpdateTimers();
		if (LeagueGrid.gameObject.activeInHierarchy)
		{
			if (mShowingSpecialLeagues && !hasUnlockedAllDailyDungeons)
            {
	           CheckForSpecialLeagueChanges();
            }

			else
			{
				Singleton<PlayerInfoScript>.Instance.SaveData.QuestSelectScrollPos = mLeagueGridDataSource.CurrentScrollDistance();
			}
			CheckLeagueBonuses();
		}
	}

	private void CheckForSpecialLeagueChanges()
	{
		List<SpecialLeagueEntry> list = BuildSpecialLeagueList();
		bool flag = list.Count == mSpecialLeagueEntries.Count;
		if (flag)
		{
			for (int i = 0; i < list.Count; i++)
			{
				if (list[i].League != mSpecialLeagueEntries[i].League)
				{
					flag = true;
					break;
				}
				mSpecialLeagueEntries[i].Clickable = list[i].Clickable;
				mSpecialLeagueEntries[i].TextString = list[i].TextString;
				mSpecialLeagueEntries[i].FullTextString = list[i].FullTextString;
			}
		}
		if (!flag)
		{
			if (mShowingLeague != null)
			{
				mShowingLeague = null;
				HideQuestsTween.Play();
			}
			mSpecialLeagueEntries = list;
			mSpecialLeagueGridDataSource.Init(LeagueGrid, SpecialLeaguePrefab, mSpecialLeagueEntries);
			return;
		}
		for (int j = 0; j < LeagueGrid.transform.childCount; j++)
		{
			Transform child = LeagueGrid.transform.GetChild(j);
			if (child.gameObject.activeInHierarchy)
			{
				QuestSelectLeague component = child.GetComponent<QuestSelectLeague>();
				component.PopulateSpecialLeague();
			}
		}
	}

	public static List<SpecialLeagueEntry> BuildSpecialLeagueList()
	{
		List<SpecialLeagueEntry> list = new List<SpecialLeagueEntry>();
		List<LeagueData> leagues = LeagueDataManager.Instance.GetLeagues(QuestLineEnum.Special);
		foreach (LeagueData item in leagues)
		{
			if (item.ID == "DungeonMaps")
			{
				if (Singleton<PlayerInfoScript>.Instance.SaveData.DungeonMaps.Count > 0)
				{
					list.Add(new SpecialLeagueEntry(item, string.Empty, string.Empty, true, false, DateTime.MinValue, DateTime.MaxValue));
				}
			}
			else if (item.ID == "DailyRandom")
			{
				list.Add(new SpecialLeagueEntry(item, string.Empty, string.Empty, true, false, DateTime.MinValue, DateTime.MaxValue));
			}
			else if (!item.HasOnlyCompletedOneTimeQuests())
			{
				bool isVisible;
				bool isClickable;
				bool expired;
				string timeText;
				string fullTimeText;
				DateTime startTime;
				DateTime endTime;
				item.GetTimeStatus(out isVisible, out isClickable, out expired, out timeText, out fullTimeText, out startTime, out endTime);
				if (isVisible)
				{
					list.Add(new SpecialLeagueEntry(item, timeText, fullTimeText, isClickable, expired, startTime, endTime));
				}
			}
		}
		list.Sort(delegate(SpecialLeagueEntry a, SpecialLeagueEntry b)
		{
			if (a.Clickable != b.Clickable)
			{
				return b.Clickable.CompareTo(a.Clickable);
			}
			if (a.League.AvailabilityType != b.League.AvailabilityType)
			{
				return a.League.AvailabilityType.CompareTo(b.League.AvailabilityType);
			}
			if (a.Expired != b.Expired)
			{
				return a.Expired.CompareTo(b.Expired);
			}
			return a.Clickable ? a.EndTime.CompareTo(b.EndTime) : a.StartTime.CompareTo(b.StartTime);
		});
		return list;
	}

	private void CheckLeagueBonuses()
	{
		bool stamina = false;
		for (int i = 0; i < LeagueGrid.transform.childCount; i++)
		{
			Transform child = LeagueGrid.transform.GetChild(i);
			if (!child.gameObject.activeInHierarchy)
			{
				continue;
			}
			QuestSelectLeague component = child.GetComponent<QuestSelectLeague>();
			bool bonusActive;
			QuestBonusType bonusType;
			string timeText;
			component.League.GetBonusStatus(out bonusActive, out bonusType, out timeText);
			if (component == mShowingLeague && bonusActive && bonusType == QuestBonusType.ReducedStamina)
			{
				stamina = true;
			}
			component.BonusText.text = timeText;
			if (bonusActive)
			{
				if (!component.FlashBonus.AnyTweenPlaying())
				{
					component.FlashBonus.Play();
				}
			}
			else
			{
				component.FlashBonus.StopAndReset();
			}
		}
		if (!(mShowingLeague != null))
		{
			return;
		}
		for (int j = 0; j < QuestGrid.transform.childCount; j++)
		{
			QuestSelectQuest component2 = QuestGrid.transform.GetChild(j).GetComponent<QuestSelectQuest>();
			if (component2.gameObject.activeInHierarchy)
			{
				component2.SetStamina(stamina);
			}
		}
	}

	public void OnLeagueClicked(QuestSelectLeague clickedLeague)
	{
		if (mShowingLeague == null)
		{
			clickedLeague.EnterExitLabel.text = KFFLocalization.Get(clickedLeague.ExitText);
			ExpandLeague(clickedLeague);
		}
		else
		{
			clickedLeague.EnterExitLabel.text = KFFLocalization.Get(clickedLeague.EnterText);
			CollapseLeague();
		}
	}

	private void ExpandLeague(QuestSelectLeague league)
	{
		mShowingLeague = league;
		Vector3 localPosition = mShowingLeague.transform.localPosition;
		float x = LeagueScrollView.transform.localPosition.x;
		float num = localPosition.x + x;
		ScrollBar.SetActive(false);
		for (int i = 0; i < LeagueGrid.transform.childCount; i++)
		{
			QuestSelectLeague component = LeagueGrid.transform.GetChild(i).GetComponent<QuestSelectLeague>();
			if (component.gameObject.activeInHierarchy)
			{
				Vector3 vector = (component.StoredPosition = component.transform.localPosition);
				TweenPosition component2 = component.MoveTween.GetComponent<TweenPosition>();
				component2.target = component.gameObject.GetComponent<UIWidget>();
				component2.from = vector;
				component2.to = vector;
				component2.to.x -= num;
				if (vector.x > localPosition.x)
				{
					component2.to.x += QuestListOffset;
				}
				component.MoveTween.Play();
			}
		}
		ShowQuestsTween.Play();
		if (league.League.ID == "DungeonMaps")
		{
			List<QuestData> list = new List<QuestData>();
			foreach (string key in Singleton<PlayerInfoScript>.Instance.SaveData.DungeonMaps.Keys)
			{
				QuestData data = QuestDataManager.Instance.GetData(key);
				if (data != null)
				{
					list.Add(data);
				}
			}
			mQuestGridDataSource.Init(QuestGrid, QuestPrefab, list, true);
		}
		else if (league.League.QuestLine == QuestLineEnum.Special)
		{
			List<QuestData> dataList = mShowingLeague.League.Quests.FindAll((QuestData m) => !m.IsCompletedOneTimeQuest());
			if (league.League.ID == "DailyRandom")
			{
				mQuestGridDataSource.Init(QuestGrid, RandomBattlePrefab, dataList, true);
			}
			else
			{
				mQuestGridDataSource.Init(QuestGrid, QuestPrefab, dataList, true);
			}
		}
		else
		{
			mQuestGridDataSource.Init(QuestGrid, QuestPrefab, mShowingLeague.League.Quests, true);
		}
		BackButton.SetActive(true);
	}

	public void CollapseLeague()
{
	ScrollBar.SetActive(true);
	for (int i = 0; i < LeagueGrid.transform.childCount; i++)
	{
		QuestSelectLeague component = LeagueGrid.transform.GetChild(i).GetComponent<QuestSelectLeague>();
		if (component.gameObject.activeInHierarchy)
		{
			Vector3 localPosition = component.transform.localPosition;
			TweenPosition component2 = component.MoveTween.GetComponent<TweenPosition>();
			component2.from = component.transform.localPosition;
			component2.to = component.StoredPosition;
			component.MoveTween.Play();
			if (component != mShowingLeague && component.MoveAlphaTween != null)
			{
				component.MoveAlphaTween.PlayReverse();
			}
			mShowingLeague = null;
		}
	}
	HideQuestsTween.Play();
	BackButton.SetActive(false);
	ResetAllLeagueButtons();
	
		mShowingSpecialLeagues = false;

    // Hide unlock button when collapsing the league
    if (UnlockAllDailyDungeonsButton != null)  
    {
        UnlockAllDailyDungeonsButton.SetActive(false);
	}
}

	public void OnQuestClicked(QuestSelectQuest questObject)
	{
		if (Singleton<PlayerInfoScript>.Instance.IsQuestUnlocked(questObject.Quest))
		{
			Singleton<PlayerInfoScript>.Instance.StateData.CurrentActiveQuest = questObject.Quest;
			Singleton<PlayerInfoScript>.Instance.StateData.DungeonMapQuest = mShowingLeague.League.ID == "DungeonMaps";
			selectedQuest = questObject;
			Singleton<PreMatchController>.Instance.Show();
		}
	}

	private void UpdateTimers()
	{
		if (PlayerStamina.gameObject.activeInHierarchy)
		{
			int currentStamina;
			int maxStamina;
			int secondsUntilNextStamina;
			DetachedSingleton<StaminaManager>.Instance.GetStaminaInfo(StaminaType.Quests, out currentStamina, out maxStamina, out secondsUntilNextStamina);
			PlayerStamina.text = maxStamina.ToString();
			PlayerStaminaTimer.text = ((secondsUntilNextStamina <= 0) ? string.Empty : PlayerInfoScript.BuildTimerString(secondsUntilNextStamina));
		}
	}

	public GameObject GetLeagueObject(string leagueId)
	{
		for (int i = 0; i < LeagueGrid.transform.childCount; i++)
		{
			QuestSelectLeague component = LeagueGrid.transform.GetChild(i).GetComponent<QuestSelectLeague>();
			if (component.League.ID == leagueId)
			{
				return component.gameObject;
			}
		}
		return null;
	}

	public GameObject GetQuestObject(string questId)
	{
		for (int i = 0; i < QuestGrid.transform.childCount; i++)
		{
			QuestSelectQuest component = QuestGrid.transform.GetChild(i).GetComponent<QuestSelectQuest>();
			if (component.Quest.ID == questId)
			{
				GameObject gameObject = component.gameObject.FindInChildren("Button");
				if (gameObject != null)
				{
					return gameObject;
				}
				return component.gameObject;
			}
		}
		return null;
	}

	public void OnLeagueDrag()
	{
		if (mShowingLeague != null)
		{
			CollapseLeague();
			ResetAllLeagueButtons();
		}
	}


	private void ResetAllLeagueButtons()
	{
		for (int i = 0; i < LeagueGrid.transform.childCount; i++)
		{
			QuestSelectLeague component = LeagueGrid.transform.GetChild(i).GetComponent<QuestSelectLeague>();
			component.EnterExitLabel.text = KFFLocalization.Get(component.EnterText);
			ButtonDepressEffect component2 = LeagueGrid.transform.GetChild(i).GetComponent<ButtonDepressEffect>();
			component2.ResetDepression();
		}
	}

	public void OnReturnShowBossFX()
	{
		if (selectedQuest != null)
		{
			selectedQuest.ShowFX();
		}
	}

	public GameObject GetHighestLeagueObject()
	{
		for (int i = 0; i < LeagueGrid.transform.childCount; i++)
		{
			QuestSelectLeague component = LeagueGrid.transform.GetChild(i).GetComponent<QuestSelectLeague>();
			if (component.gameObject.activeInHierarchy && !Singleton<PlayerInfoScript>.Instance.IsLeagueUnlocked(component.League))
			{
				return LeagueGrid.transform.GetChild(i - 1).gameObject;
			}
		}
		return null;
	}

	public GameObject GetHighestQuestObject()
	{
		GameObject result = null;
		for (int i = 0; i < QuestGrid.transform.childCount; i++)
		{
			QuestSelectQuest component = QuestGrid.transform.GetChild(i).GetComponent<QuestSelectQuest>();
			if (!component.gameObject.activeInHierarchy)
			{
				continue;
			}
			if (!Singleton<PlayerInfoScript>.Instance.IsQuestUnlocked(component.Quest))
			{
				GameObject gameObject = QuestGrid.transform.GetChild(i - 1).gameObject.FindInChildren("Button");
				if (gameObject != null)
				{
					return gameObject;
				}
				return QuestGrid.transform.GetChild(i - 1).gameObject;
			}
			GameObject gameObject2 = QuestGrid.transform.GetChild(i).gameObject.FindInChildren("Button");
			result = ((!(gameObject2 != null)) ? QuestGrid.transform.GetChild(i).gameObject : gameObject2);
		}
		return result;
	}

	public bool ShowingDungeonMapsLeague()
	{
		return mShowingLeague != null && mShowingLeague.League.QuestLine == QuestLineEnum.Special && mShowingLeague.League.ID == "DungeonMaps";
	}
	
	public void OnClickUnlockAllDailyDungeons()
	{
		// Check if player has enough gems
		if (Singleton<PlayerInfoScript>.Instance.SaveData.HardCurrency < MiscParams.UnlockAllDailyDungeonsGemCost)
		{
			Singleton<SimplePopupController>.Instance.ShowCantAffordPurchasePrompt(
				"Unlock all 8 daily material dungeons to farm any ingredient!",
				MiscParams.UnlockAllDailyDungeonsGemCost
			);
			return;
		}
		
		// Show confirmation
		string confirmText = string.Format(
			"Unlock all daily material dungeons for {0} gems?\n\n" +
			"Farm ANY material (Mon-Sun + SnackHour) until you return to the lobby!",
			MiscParams.UnlockAllDailyDungeonsGemCost
		);
		
		Singleton<SimplePopupController>.Instance.ShowPurchasePrompt(
			confirmText,
			"Not enough gems!",
			MiscParams.UnlockAllDailyDungeonsGemCost,
			ConfirmUnlockAllDailyDungeons
		);
	}

	private void ConfirmUnlockAllDailyDungeons()
	{
		// Deduct gems
		Singleton<PlayerInfoScript>.Instance.SaveData.ConsumeHardCurrency2(
			MiscParams.UnlockAllDailyDungeonsGemCost,
			"unlock all daily dungeons",
			delegate(PlayerSaveData.ActionResult result)
			{
				if (result.success)

				{
					// Set unlock flag (session-based)
					hasUnlockedAllDailyDungeons = true;
					
					// Force all daily leagues to be visible
					UnlockAllDailyLeagues();
					
					// Refresh UI to show them
			        mSpecialLeagueEntries = BuildSpecialLeagueList();
					mSpecialLeagueGridDataSource.Init(LeagueGrid, SpecialLeaguePrefab, mSpecialLeagueEntries, true);
					
					// Hide unlock button since they already unlocked
					if (UnlockAllDailyDungeonsButton != null)
					{
						UnlockAllDailyDungeonsButton.SetActive(false);
					}
					
					Debug.Log("[QuestSelect] All 8 daily dungeons unlocked for this session!");
				}
			}
		);
	}

	private void UnlockAllDailyLeagues()
	{
		// Make all 8 daily leagues visible by overriding their time checks
		foreach (string leagueID in dailyDungeonLeagueIDs)
		{
			LeagueData league = LeagueDataManager.Instance.GetData(leagueID);
			if (league != null)
			{
				// Override the day-of-week restriction (or time restriction for SnackHour)
				league.ForceVisible = true;
				Debug.Log($"[QuestSelect] Unlocked {leagueID} dungeon");
			}
			else
			{
				Debug.LogWarning($"[QuestSelect] Could not find league: {leagueID}");
			}
		}
	}

	public void ResetDailyDungeonUnlock()
{
	
	Debug.Log($"[QuestSelect] ResetDailyDungeonUnlock called. hasUnlockedAllDailyDungeons = {hasUnlockedAllDailyDungeons}");
	
	// Called when opening Quest Select to resets the unlock
	if (hasUnlockedAllDailyDungeons || mSpecialLeagueGridDataSource != null)
	{
		hasUnlockedAllDailyDungeons = false;
		
		// Restore normal day-of-week restrictions
		foreach (string leagueID in dailyDungeonLeagueIDs)
		{
			LeagueData league = LeagueDataManager.Instance.GetData(leagueID);
			if (league != null)
			{
				league.ForceVisible = false;
			}
		}
		
		// Rebuild dungeon list to show only today's dungeon
		DetachedSingleton<DailyRandomDungeonManager>.Instance.BuildDailyRandomBattles();
		
		// Always refresh the special league UI to reflect changes
		if (mSpecialLeagueGridDataSource != null && LeagueGrid != null)
		{
			mSpecialLeagueEntries = BuildSpecialLeagueList();
			mSpecialLeagueGridDataSource.Init(LeagueGrid, SpecialLeaguePrefab, mSpecialLeagueEntries, true);
			Debug.Log("[QuestSelect] Refreshed special league UI after reset");
		}
		
		// Update button visibility based on current menu
		if (UnlockDailyDungeonsCostLabel != null)
		{
			UnlockDailyDungeonsCostLabel.text = MiscParams.UnlockAllDailyDungeonsGemCost.ToString();
		}
		
		//Debug.Log("[QuestSelect] Daily dungeon unlock reset COMPLETE");
	}
	else
	{
		Debug.Log("[QuestSelect] Skipping reset because hasUnlockedAllDailyDungeons is FALSE");
	}
}


	public void UpdateDailyDungeonUnlockButtonVisibility()
	{
		if (UnlockAllDailyDungeonsButton == null) return;
		
		// Only show button when viewing the special (dungeon) quest list 
		bool showButton = mShowingSpecialLeagues && !hasUnlockedAllDailyDungeons;
		
		UnlockAllDailyDungeonsButton.SetActive(showButton);
		
		// Update cost label
		if (UnlockDailyDungeonsCostLabel != null && showButton)
		{
			UnlockDailyDungeonsCostLabel.text = MiscParams.UnlockAllDailyDungeonsGemCost.ToString();
		}
	}
	
	private void OnDisable()
{
	// Hide unlock button when leaving Quest Select screen
	if (UnlockAllDailyDungeonsButton != null)
	{
		UnlockAllDailyDungeonsButton.SetActive(false);
	}
}




}