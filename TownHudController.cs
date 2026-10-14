using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TownHudController : Singleton<TownHudController>
{
	public UITweenController MissionCompleteTween;

	public UITweenController BuildingLockedTween;

	public UILabel PlayerName;

	public UILabel PlayerLevel;

	public UITexture PlayerPortrait;

	public UILabel PlayerXpPercent;

	public UISprite PlayerXpBar;

	public UILabel PlayerStamina;

	public UILabel PlayerStaminaTimer;

	public UILabel PlayerPvpStamina;

	public UILabel PlayerPvpStaminaTimer;

	public UILabel PlayerHardCurrency;

	public UILabel PlayerSoftCurrency;

	public UILabel PlayerPvPCurrency;

	public UILabel PlayerEvolutionShards;

	public UILabel PlayerCraftCurrency;

	public GameObject PvpStaminaObject;

	public GameObject SoftCurrencyObject;

	public GameObject HardCurrencyObject;

	public GameObject PvpCurrencyObject;

	public GameObject EvolutionShardsObject;
	public GameObject CraftCurrencyObject;

	public UIGrid CurrenciesGrid;

	public GameObject MissionStackObject;

	public UILabel MissionStackCount;

	public UILabel MissionTimerLabel;

	public GameObject GachaButtonsShowHide;

	public GameObject DungeonButtonsShowHide;

	public GameObject MissionsButton;

	public GameObject ExpeditionsButton;

	public GameObject DailyRouletteButton;

	public GameObject DailyRouletteLock;

	public UILabel DailyRouletteLevelRequirementLabel;

	public GameObject CraftRoomButton;

	public GameObject CraftRoomLock;

	public UILabel CraftRoomLevelRequirementLabel;

	public UISprite DailyRouletteButtonBackground;

	public GameObject DailyRouletteButtonRot;

	public GameObject DailyRouletteButtonTitle;

	public UISprite CraftRoomButtonBackground;

	public GameObject CraftRoomButtonRot;

	public GameObject CraftRoomButtonTitle;

	public UILabel ExpeditionStackValue;

	public UILabel ExpeditionTimerLabel;

	public UILabel DailyChestTimerLabel;

	public UILabel DailyDungeonNameLabel;

	public UILabel DailyDungeonTimerLabel;

	public UITexture DailyDungeonIcon;

	public GameObject BuildingUnlockPanel;

	public UITweenController ShowHUD;

	public UITweenController HideHUD;

	public UITweenController ShowBuildingUnlockBanner;

	public UITweenController ScribbleStartTween;

	public UITweenController DailyRouletteShow;

	public CardCraftingController CraftRoomShow;

	public ChangeLogController changeLogControllerShow;

	public UILabel BuildingUnlockLabel;

	public UILabel BuildingLockedLabel;

	public UISprite[] DeepLinkButtonBgs = new UISprite[0];

	public UITexture LeaderTex;

	public GameObject promoBannerObject;

	private bool mPopulated;

	private bool mMissionSoundPlayed;

	private int mDisplayedCompleteMissionCount;

	private int mDisplayedExpeditionCount = -1;

	private int mDisplayedCompletedExpeditionCount;

	private UIPanel mPanel;

	private bool mShowingLoadingScreen = true;

	private GachaSlotData _DailyGachaSlotData;

	private QuestSelectController.SpecialLeagueEntry _DailyDungeonLeague;

	private int _GlintIndex;

	public GameObject UIBar;

	private List<FrontEndBuildingUIBar> UIBarObjects = new List<FrontEndBuildingUIBar>();

	private GoogleRewardBanner mGoogleBanner;

	private float mNextHudRefresh;

	private const float kHudRefreshInterval = 0.2f;

	private void Awake()
	{
		mPanel = GetComponent<UIPanel>();
	}

	private void Update()
	{
		PopulateIfReady();
		if (!mPopulated)
		{
			return;
		}
		if (!GachaButtonsShowHide.activeSelf)
		{
			GachaButtonsShowHide.SetActive(Singleton<PlayerInfoScript>.Instance.IsFeatureUnlocked("TBuilding_Gacha"));
		}
		if (!DungeonButtonsShowHide.activeSelf)
		{
			DungeonButtonsShowHide.SetActive(Singleton<PlayerInfoScript>.Instance.IsFeatureUnlocked("TBuilding_Dungeon"));
		}
		if (!(RealTime.time < mNextHudRefresh))
		{
			mNextHudRefresh = RealTime.time + 0.2f;
			UpdateDynamicInfo();
			UpdateTimers();
			UpdateCurrencies();
			UpdateMissions();
			UpdateExpeditions();
			UpdateDailyDungeonTimer();
			UpdateRouletteCooldown();
			if (mShowingLoadingScreen)
			{
				mPanel.Refresh();
			}
			mShowingLoadingScreen = LoadingScreenController.ShowingLoadingScreen();
		}
	}

	private void PopulateIfReady()
	{
		if (!mPopulated && SessionManager.Instance.IsLoadDataDone() && Singleton<PlayerInfoScript>.Instance.IsInitialized)
		{
			mPopulated = true;
			DetachedSingleton<MissionManager>.Instance.ShouldCheckCompletion = true;
			PopulatePlayerInfo();
			_DailyDungeonLeague = GetDailyDungeon();
			UpdateTimers();
			UpdateCurrencies();
			UpdateLeaderIcon();
			SetScheduledUIElements();
			RefreshFeatureButtons();
			GachaButtonsShowHide.SetActive(value: false);
			DungeonButtonsShowHide.SetActive(value: false);
			ScribbleStartTween.Play();
			EnablePromoBanner();
			mGoogleBanner = GoogleRewardBanner.Wire(this);
		}
	}

	public void UpdateLeaderIcon()
	{
		Singleton<PlayerInfoScript>.Instance.SaveData.SelectedPortrait.ApplyTexture(LeaderTex);
	}

	public void PopulatePlayerInfo()
	{
		_ = Singleton<PlayerInfoScript>.Instance.SaveData;
		Singleton<PlayerInfoScript>.Instance.RankXpLevelData.PopulateUI(verbose: false, PlayerLevel, null, PlayerXpPercent, PlayerXpBar);
		PlayerLevel.text = KFFLocalization.Get("!!RANK") + " " + PlayerLevel.text;
		PlayerName.text = Singleton<PlayerInfoScript>.Instance.GetPlayerName();
		Singleton<PlayerInfoScript>.Instance.SaveData.SelectedPortrait.ApplyTexture(PlayerPortrait);
		bool active = Singleton<PlayerInfoScript>.Instance.CanGacha();
		PvpCurrencyObject.SetActive(active);
		HardCurrencyObject.SetActive(active);
		EvolutionShardsObject.SetActive(active);
		CraftCurrencyObject.SetActive(active);
		PvpStaminaObject.SetActive(value: false);
		SoftCurrencyObject.SetActive(Singleton<PlayerInfoScript>.Instance.CanEditDeck());
		CurrenciesGrid.Reposition();
	}

	private void UpdateDynamicInfo()
	{
		PlayerName.text = Singleton<PlayerInfoScript>.Instance.GetPlayerName();
	}

	private void UpdateTimers()
	{
		DetachedSingleton<StaminaManager>.Instance.GetStaminaInfo(StaminaType.Quests, out var currentStamina, out var maxStamina, out var secondsUntilNextStamina);
		PlayerStamina.text = maxStamina.ToString();
		PlayerStaminaTimer.text = ((secondsUntilNextStamina <= 0) ? string.Empty : PlayerInfoScript.BuildTimerString(secondsUntilNextStamina));
		DetachedSingleton<StaminaManager>.Instance.GetStaminaInfo(StaminaType.Pvp, out currentStamina, out maxStamina, out secondsUntilNextStamina);
		PlayerPvpStamina.text = maxStamina.ToString();
		PlayerPvpStaminaTimer.text = ((secondsUntilNextStamina <= 0) ? string.Empty : PlayerInfoScript.BuildTimerString(secondsUntilNextStamina));
		MissionTimerLabel.text = KFFLocalization.Get("!!TIME_VAL_LEFT").Replace("<val1>", PlayerInfoScript.FormatTimeString((int)DetachedSingleton<MissionManager>.Instance.GetMinutesUntilQuestRefresh() * 60));
		if (GachaSlotDataManager.Instance != null)
		{
			if (_DailyGachaSlotData == null)
			{
				_DailyGachaSlotData = GachaSlotDataManager.Instance.GetData("Daily");
			}
			int gachaSlotCooldownSeconds = Singleton<PlayerInfoScript>.Instance.GetGachaSlotCooldownSeconds(_DailyGachaSlotData);
			if (gachaSlotCooldownSeconds > 0)
			{
				DailyChestTimerLabel.text = PlayerInfoScript.FormatTimeString(gachaSlotCooldownSeconds, showSeconds: true);
			}
			else
			{
				DailyChestTimerLabel.text = KFFLocalization.Get("!!FREE_CHEST");
			}
		}
		else
		{
			DailyChestTimerLabel.text = string.Empty;
		}
		DailyDungeonTimerLabel.text = "00:00";
	}

	private void UpdateCurrencies()
	{
		PlayerSaveData saveData = Singleton<PlayerInfoScript>.Instance.SaveData;
		PlayerHardCurrency.text = CurrencyUI.Format(saveData.HardCurrency);
		PlayerSoftCurrency.text = CurrencyUI.Format(saveData.SoftCurrency);
		PlayerPvPCurrency.text = CurrencyUI.Format(saveData.PvPCurrency);
		PlayerEvolutionShards.text = CurrencyUI.Format(saveData.EvolutionShards);
		PlayerCraftCurrency.text = CurrencyUI.Format(saveData.CraftCurrency);
	}

	private QuestSelectController.SpecialLeagueEntry GetDailyDungeon()
	{
		List<QuestSelectController.SpecialLeagueEntry> list = QuestSelectController.BuildSpecialLeagueList().FindAll((QuestSelectController.SpecialLeagueEntry m) => m.Clickable);
		for (int num = 0; num < list.Count; num++)
		{
			QuestSelectController.SpecialLeagueEntry specialLeagueEntry = list[num];
			if (specialLeagueEntry.League.AvailabilityType == QuestAvailabilityType.Weekday || specialLeagueEntry.League.AvailabilityType == QuestAvailabilityType.Weekend)
			{
				DailyDungeonNameLabel.text = specialLeagueEntry.League.Name;
				DailyDungeonIcon.ReplaceTexture(specialLeagueEntry.League.LeagueIconTexture);
				return specialLeagueEntry;
			}
		}
		DailyDungeonNameLabel.text = string.Empty;
		return null;
	}

	public void UpdateDailyDungeonTimer()
	{
		if (_DailyDungeonLeague != null)
		{
			_DailyDungeonLeague.League.GetTimeStatus(out var _, out var _, out var _, out var timeText, out var _, out var _, out var _);
			DailyDungeonTimerLabel.text = timeText;
		}
		else
		{
			_DailyDungeonLeague = GetDailyDungeon();
		}
	}

	public void OnClickMissions()
	{
		Singleton<MissionListController>.Instance.Show();
	}

	public void OnClickExpeditions()
	{
		Singleton<ExpeditionStartController>.Instance.Show();
	}

	private void UpdateMissions(bool forceUpdate = false)
	{
		bool flag = forceUpdate;
		if (DetachedSingleton<MissionManager>.Instance.IsTimeForDailyMissions())
		{
			DetachedSingleton<MissionManager>.Instance.AssignDailyMissions();
			DetachedSingleton<MissionManager>.Instance.StampDailyMissions();
			flag = true;
		}
		if (flag || DetachedSingleton<MissionManager>.Instance.ShouldCheckCompletion)
		{
			DetachedSingleton<MissionManager>.Instance.CheckCompletion();
			DetachedSingleton<MissionManager>.Instance.ShouldCheckCompletion = false;
			int completedMissionCount = DetachedSingleton<MissionManager>.Instance.GetCompletedMissionCount();
			if (completedMissionCount > 0)
			{
				MissionCompleteTween.Play();
				if (!mMissionSoundPlayed)
				{
					StartCoroutine(PlayMissionSoundDelayed(1.5f));
					mMissionSoundPlayed = true;
				}
				MissionStackObject.SetActive(value: true);
				MissionStackCount.text = completedMissionCount.ToString();
				if (completedMissionCount > mDisplayedCompleteMissionCount)
				{
					Singleton<CornerNotificationPopupController>.Instance.Show(CornerNotificationPopupController.PopupTypeEnum.MissionsComplete, completedMissionCount);
				}
			}
			else
			{
				MissionCompleteTween.StopAndReset();
				MissionStackObject.SetActive(value: false);
			}
			mDisplayedCompleteMissionCount = completedMissionCount;
		}
		if (flag)
		{
			Singleton<MissionListController>.Instance.Repopulate();
		}
	}

	private IEnumerator PlayMissionSoundDelayed(float delay)
	{
		yield return new WaitForSeconds(delay);
		Singleton<SLOTAudioManager>.Instance.PlaySound("ui/SFX_MissionPopupShow");
	}

	public void ClaimCompletedMission(Mission mission)
	{
		Singleton<PlayerInfoScript>.Instance.SaveData.ClaimCurrency("mission reward", mission.Data.ID, mission.Data.SoftCurrency, mission.Data.HardCurrency, mission.Data.SocialCurrency, 0);
		if (mission.Data.Type == MissionType.Global)
		{
			mission.Claimed = true;
			DetachedSingleton<MissionManager>.Instance.AssignGlobalMissions();
		}
		else
		{
			DetachedSingleton<MissionManager>.Instance.RemoveDailyMission(mission);
		}
		UpdateMissions(forceUpdate: true);
		Singleton<PlayerInfoScript>.Instance.Save();
	}

	private void UpdateExpeditions()
	{
		uint num = TFUtils.ServerTime.UnixTimestamp();
		if (num >= Singleton<PlayerInfoScript>.Instance.SaveData.ExpeditionRefreshTime)
		{
			DetachedSingleton<ExpeditionManager>.Instance.AssignNewExpeditions();
		}
		int num2 = (int)(Singleton<PlayerInfoScript>.Instance.SaveData.ExpeditionRefreshTime - num);
		ExpeditionTimerLabel.text = KFFLocalization.Get("!!TIME_VAL_LEFT").Replace("<val1>", PlayerInfoScript.FormatTimeString(num2, showSeconds: true));
		Singleton<ExpeditionStartController>.Instance.SetTimeUntilRefresh(num2);
		DetachedSingleton<ExpeditionManager>.Instance.CompletedOrAvailableExpeditionCount(out var completed, out var total);
		if (total != mDisplayedExpeditionCount)
		{
			if (total > 0)
			{
				ExpeditionStackValue.transform.SetParentActive(value: true);
				ExpeditionStackValue.text = total.ToString();
			}
			else
			{
				ExpeditionStackValue.transform.SetParentActive(value: false);
			}
			mDisplayedExpeditionCount = total;
		}
		if (completed > mDisplayedCompletedExpeditionCount)
		{
			Singleton<CornerNotificationPopupController>.Instance.Show(CornerNotificationPopupController.PopupTypeEnum.ExpeditionsComplete, completed);
		}
		mDisplayedCompletedExpeditionCount = completed;
	}

	public void InitializeBuildings()
	{
		TownBuildingScript[] componentsInChildren = Singleton<TownController>.Instance.gameObject.GetComponentsInChildren<TownBuildingScript>();
		foreach (TownBuildingScript townBuildingScript in componentsInChildren)
		{
			townBuildingScript.Init();
			FrontEndBuildingUIBar component = Singleton<TownHudController>.Instance.transform.InstantiateAsChild(UIBar).GetComponent<FrontEndBuildingUIBar>();
			component.mBuilding = townBuildingScript;
			component.Init();
			UIBarObjects.Add(component);
		}
		RefreshBuildingLocks();
		PlayRandomBadgeGlint();
	}

	private void PlayRandomBadgeGlint()
	{
		int num = 0;
		int num2 = Random.Range(0, UIBarObjects.Count);
		while (num < UIBarObjects.Count)
		{
			num++;
			if (num2 == _GlintIndex || !UIBarObjects[num2].gameObject.activeSelf)
			{
				num2++;
				num2 %= UIBarObjects.Count;
				continue;
			}
			_GlintIndex = num2;
			float time = Random.Range(1, 5);
			CancelInvoke("PlayBadgeGlint");
			Invoke("PlayBadgeGlint", time);
			break;
		}
	}

	private void PlayBadgeGlint()
	{
		UIBarObjects[_GlintIndex].GlintAnim.PlayOnce();
		PlayRandomBadgeGlint();
	}

	public void RefreshBuildingLocks(bool forceLock = false)
	{
		foreach (FrontEndBuildingUIBar uIBarObject in UIBarObjects)
		{
			if (forceLock)
			{
				uIBarObject.gameObject.SetActive(value: false);
			}
			else
			{
				uIBarObject.gameObject.SetActive(Singleton<PlayerInfoScript>.Instance.IsFeatureUnlocked(uIBarObject.mBuilding.BuildingId));
			}
		}
		ExpeditionsButton.SetActive(Singleton<PlayerInfoScript>.Instance.IsFeatureUnlocked("Expedition"));
	}

	public void HideUIBar(bool hide)
	{
		foreach (FrontEndBuildingUIBar uIBarObject in UIBarObjects)
		{
			uIBarObject.gameObject.SetActive(!hide);
		}
	}

	public void ReturnToTownView()
	{
		Singleton<MouseOrbitCamera>.Instance.CheckTiltCamSettingBeforeTutorial();
		Singleton<MouseOrbitCamera>.Instance.UnZoom();
		CheckPromoBanner();
		ShowMainHUD();
	}

	public void ShowMainHUD()
	{
		RefreshFeatureButtons();
		ShowHUD.Play();
		for (int i = 0; i < UIBarObjects.Count; i++)
		{
			FrontEndBuildingUIBar component = UIBarObjects[i].GetComponent<FrontEndBuildingUIBar>();
			if (component != null && Singleton<PlayerInfoScript>.Instance.IsFeatureUnlocked(component.mBuilding.BuildingId))
			{
				UIBarObjects[i].ShowTween.Play();
				UIBarObjects[i].TryToShowAttract();
			}
		}
		ExpeditionsButton.SetActive(Singleton<PlayerInfoScript>.Instance.IsFeatureUnlocked("Expedition"));
		if (UIBarObjects.Count > 0)
		{
			PlayRandomBadgeGlint();
		}
		EnablePromoBanner();
		if (mGoogleBanner != null)
		{
			mGoogleBanner.Refresh();
		}
	}

	public void HideMainHUD()
	{
		promoBannerObject.SetActive(value: false);
		ExpeditionsButton.SetActive(Singleton<PlayerInfoScript>.Instance.IsFeatureUnlocked("Expedition"));
		HideHUD.Play();
		for (int i = 0; i < UIBarObjects.Count; i++)
		{
			FrontEndBuildingUIBar component = UIBarObjects[i].GetComponent<FrontEndBuildingUIBar>();
			if (component != null && Singleton<PlayerInfoScript>.Instance.IsFeatureUnlocked(component.mBuilding.BuildingId))
			{
				UIBarObjects[i].HideTween.Play();
			}
		}
		CancelInvoke("PlayBadgeGlint");
	}

	public void OnClickSettings()
	{
		Singleton<TownSettingsController>.Instance.Show();
	}

	public void OnClickCustomize()
	{
		Singleton<PlayerCustomizationController>.Instance.ShowDefault();
	}

	public void ShowLockedBuilding(TownBuildingData building)
	{
		PlayerRankData playerRankData = PlayerRankDataManager.Instance.UnlockRanks.Find((PlayerRankData m) => m.UnlockType == UnlockTypeEnum.Building && m.UnlockId == building.ID);
		if (playerRankData != null)
		{
			BuildingLockedTween.Play();
			BuildingLockedLabel.text = KFFLocalization.Get("!!BUILDING_LOCKED_INFO").Replace("<val1>", playerRankData.Level.ToString());
		}
	}

	public void ShowGachaAttractionPointer()
	{
		foreach (FrontEndBuildingUIBar uIBarObject in UIBarObjects)
		{
			if (uIBarObject != null && uIBarObject.isActiveAndEnabled)
			{
				uIBarObject.TryToShowAttract();
			}
		}
	}

	public void HandleStoreButtonPress()
	{
		Application.OpenURL("https://discord.com/invite/sp7dt7xrnh");
	}

	public void HandleStoreLibraryButtonPress()
	{
		Application.OpenURL("https://cardwars-kingdom.net");
	}

	private int GetPlayerLevel()
	{
		return Singleton<PlayerInfoScript>.Instance.RankData.Level;
	}

	private bool CanUseDailyRoulette()
	{
		return GetPlayerLevel() >= MiscParams.DailyRouletteLevelRequired;
	}

	private bool CanUseCardCrafting()
	{
		return GetPlayerLevel() >= MiscParams.CardCraftingLevelRequired;
	}

	private void RefreshFeatureButtons()
	{
		int playerLevel = GetPlayerLevel();
		bool flag = playerLevel >= MiscParams.DailyRouletteLevelRequired;
		UIButton uIButton = ((DailyRouletteButton != null) ? DailyRouletteButton.GetComponent<UIButton>() : null);
		if (uIButton != null)
		{
			uIButton.isEnabled = flag;
		}
		SetGameObjectGray(DailyRouletteButtonRot, !flag);
		if (DailyRouletteButtonBackground != null)
		{
			DailyRouletteButtonBackground.color = (flag ? Color.white : Color.gray);
		}
		if (DailyRouletteLock != null)
		{
			DailyRouletteLock.SetActive(!flag);
		}
		if (DailyRouletteButtonTitle != null)
		{
			DailyRouletteButtonTitle.SetActive(flag);
		}
		if (DailyRouletteLevelRequirementLabel != null)
		{
			DailyRouletteLevelRequirementLabel.gameObject.SetActive(!flag);
			DailyRouletteLevelRequirementLabel.text = "Unlock Level " + MiscParams.DailyRouletteLevelRequired;
		}
		bool flag2 = playerLevel >= MiscParams.CardCraftingLevelRequired;
		UIButton uIButton2 = ((CraftRoomButton != null) ? CraftRoomButton.GetComponent<UIButton>() : null);
		if (uIButton2 != null)
		{
			uIButton2.isEnabled = flag2;
		}
		SetGameObjectGray(CraftRoomButtonRot, !flag2);
		if (CraftRoomButtonBackground != null)
		{
			CraftRoomButtonBackground.color = (flag2 ? Color.white : Color.gray);
		}
		if (CraftRoomLock != null)
		{
			CraftRoomLock.SetActive(!flag2);
		}
		if (CraftRoomButtonTitle != null)
		{
			CraftRoomButtonTitle.SetActive(flag2);
		}
		if (CraftRoomLevelRequirementLabel != null)
		{
			CraftRoomLevelRequirementLabel.gameObject.SetActive(!flag2);
			CraftRoomLevelRequirementLabel.text = "Unlock Level " + MiscParams.CardCraftingLevelRequired;
		}
	}

	private void UpdateRouletteCooldown()
	{
		if (CanUseDailyRoulette() && !(DailyRouletteLevelRequirementLabel == null))
		{
			long num = Singleton<PlayerInfoScript>.Instance.DailySpinSecondsLeft();
			bool flag = num > 0;
			SetGameObjectGray(DailyRouletteButtonRot, flag);
			if (DailyRouletteButtonTitle != null)
			{
				DailyRouletteButtonTitle.SetActive(!flag);
			}
			DailyRouletteLevelRequirementLabel.gameObject.SetActive(flag);
			if (flag)
			{
				DailyRouletteLevelRequirementLabel.text = PlayerInfoScript.FormatSpinTime(num);
			}
		}
	}

	public void DailyRouletteButtonPress()
	{
		if (CanUseDailyRoulette() && LiveChannel.Allowed("Roulette"))
		{
			DailyRouletteShow.Play();
		}
	}

	public void CraftRoomButtonPress()
	{
		if (CanUseCardCrafting() && LiveChannel.Allowed("Craft"))
		{
			Singleton<CardCraftingController>.Instance.Open();
		}
	}

	public void TradeRoomButtonPress()
	{
		if (!LiveChannel.Allowed("Trade"))
		{
			return;
		}
		TradePanelController tradePanelController = null;
		TradePanelController[] array = Resources.FindObjectsOfTypeAll<TradePanelController>();
		foreach (TradePanelController tradePanelController2 in array)
		{
			if (tradePanelController2.gameObject.scene.IsValid())
			{
				tradePanelController = tradePanelController2;
				break;
			}
		}
		if (tradePanelController == null)
		{
			Debug.LogError("[Trade] TradePanelController not found in scene.");
			return;
		}
		Transform parent = tradePanelController.transform;
		while (parent != null)
		{
			parent.gameObject.SetActive(value: true);
			parent = parent.parent;
		}
		tradePanelController.Open();
	}

	public void PatchNotesButtonPress()
	{
		ChangeLogPopupUI.Instance.ShowLatest();
	}

	private void SetGameObjectGray(GameObject obj, bool gray)
	{
		if (!(obj == null))
		{
			UISprite[] componentsInChildren = obj.GetComponentsInChildren<UISprite>(includeInactive: true);
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				componentsInChildren[i].color = (gray ? Color.gray : Color.white);
			}
			UITexture[] componentsInChildren2 = obj.GetComponentsInChildren<UITexture>(includeInactive: true);
			for (int i = 0; i < componentsInChildren2.Length; i++)
			{
				componentsInChildren2[i].color = (gray ? Color.gray : Color.white);
			}
		}
	}

	public void HandleDailyChestButtonPress()
	{
		Singleton<GachaScreenController>.Instance.DailyChestID = _DailyGachaSlotData.ID;
		Singleton<TownController>.Instance.GetBuildingObject("TBuilding_Gacha").GetComponent<TownBuildingScript>().SendMessage("OnClick");
	}

	public void HandleTreasureCaveButtonPress()
	{
		Singleton<TownController>.Instance.GetBuildingObject("TBuilding_Gacha").GetComponent<TownBuildingScript>().SendMessage("OnClick");
	}

	public void HandleDailyDungeonButtonPress()
	{
		if (_DailyDungeonLeague != null)
		{
			Singleton<QuestSelectController>.Instance.DailyDungeonLeagueID = _DailyDungeonLeague.League.ID;
		}
		Singleton<TownController>.Instance.GetBuildingObject("TBuilding_Dungeon").GetComponent<TownBuildingScript>().SendMessage("OnClick");
	}

	private void CheckPromoBanner()
	{
		PromoBannerController component = promoBannerObject.GetComponent<PromoBannerController>();
		if (component.IsInitialized())
		{
			component.CheckBannerStatus();
		}
	}

	private void SetScheduledUIElements()
	{
		TownScheduleData currentScheduledTownData = TownScheduleDataManager.Instance.GetCurrentScheduledTownData();
		for (int i = 0; i < DeepLinkButtonBgs.Length; i++)
		{
			PackSprite.Apply(DeepLinkButtonBgs[i], currentScheduledTownData.DeepLinkButtonBgSprite);
		}
	}

	private void EnablePromoBanner()
	{
		PromoBannerController component = promoBannerObject.GetComponent<PromoBannerController>();
		if (component.IsInitialized())
		{
			promoBannerObject.SetActive(value: true);
			return;
		}
		promoBannerObject.SetActive(value: false);
		bool flag = false;
		List<EventBannersData> database = EventBannersDataManager.Instance.GetDatabase();
		List<EventTemplateData> database2 = EventTemplateDataManager.Instance.GetDatabase();
		foreach (EventBannersData item in database)
		{
			if (!EventBannersDataManager.Instance.CheckBannerDates(item))
			{
				continue;
			}
			int intEventTemplateId = item.GetIntEventTemplateId();
			foreach (EventTemplateData item2 in database2)
			{
				if (intEventTemplateId == item2.GetIntEventTemplateId() && EventTemplateDataManager.Instance.CheckEventCondition(item2))
				{
					if (!flag)
					{
						promoBannerObject.SetActive(value: true);
						flag = true;
					}
					component.LoadBannerData(item2, item);
				}
			}
		}
		if (flag)
		{
			component.Init();
			promoBannerObject.SetActive(value: true);
		}
	}
}
