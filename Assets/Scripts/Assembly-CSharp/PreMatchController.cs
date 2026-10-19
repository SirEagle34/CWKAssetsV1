using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PreMatchController : Singleton<PreMatchController>
{
	private enum NextAction
	{
		NONE = 0,
		WAITING = 1,
		PROCEED = 2,
		ERROR = 3,
		RETRY = 4
	}

	public Color[] TutorialTeamColors;

	public UITweenController ShowTween;

	public UITweenController ExceededTeamCostTween;

	public UITweenController ConsumeTicketTween;

	public UITweenController ShowHelperBeingDraggedTween;

	public UITweenController HideHelperBeingDraggedTween;

	public UITweenController BattleStartTween;

	public UITweenController PulseTutorialButtonsTween;

	public UITweenController FadeInHelperButtonTween;

	public UITweenController FadeOutHelperButtonTween;

	public UITexture[] PIPIconTargets = new UITexture[2];

	public UITexture[] PlayerBackgroundTextures = new UITexture[2];

	public Transform[] MyCreatureNodes;

	public Transform[] EnemyCreatureNodes;

	public UILabel StaminaCost;

	public UILabel PlayerStamina;

	public UILabel PlayerStaminaTimer;

	public GameObject EditDeckButton;

	public UILabel QuestName;

	public UISprite IconVS;

	public UILabel TeamCost;

	public UILabel TeamName;

	public UILabel[] LeaderNames = new UILabel[2];

	public GameObject AddHelperButton;

	public GameObject RemoveHelperButton;

	public GameObject HelperBlockingSlot;

	public GameObject StandardQuestParent;

	public GameObject RandomQuestParent;

	public UILabel RandomQuestFloor;

	public UILabel RandomQuestReward;

	public UILabel RandomQuestBattles;

	public GameObject StartButton;

	public GameObject RequiredCreatureParent;

	public GameObject RequiredCreatureSpawnNode;

	public GameObject CloseButton;

	public UILabel TeamDescription;

	public UISprite TeamDescriptionBox;

	private List<GameObject> mMySpawnedCreatures = new List<GameObject>();

	private List<GameObject> mEnemySpawnedCreatures = new List<GameObject>();

	[Header("Battle Speed Toggle")]
	public GameObject SpeedToggleButton;

	public UIButton SpeedToggle;

	private bool use2xSpeed;

	private List<string> pvpBanlist = new List<string> { "BrainGooey_Base", "CornGolem_Base" };

	private List<string> pvpLeaderBanlist = new List<string> { "Leader_PoweredJk" };

	private bool mDeclinedHelper;

	private GameObject mSpawnedRequiredCreature;

	private bool shouldShowHelperTile;

	private bool isHelperTileFadedOut;

	public BoxCollider HelperSlot;

	public bool mDeclineSlotPurchase;

	private string opponentBGTexture;

	private bool mWaitForUserAction;

	private NextAction mUserActionProceed;

	private PlayerSaveData.ProceedNextStep mNextFunction;

	private bool hasShownBanPopup;

	public void Show()
	{
		AddHelperButton.SetActive(Singleton<PlayerInfoScript>.Instance.CanUseHelper());
		mDeclinedHelper = false;
		mDeclineSlotPurchase = false;
		ShowTween.Play();
		bool flag = Singleton<TutorialController>.Instance.IsFTUETutorialActive();
		if (Singleton<TutorialController>.Instance.IsBlockActive("Q1"))
		{
			PulseTutorialButtonsTween.Play();
		}
		Singleton<PlayerInfoScript>.Instance.StateData.SelectedHelper = null;
		QuestData currentActiveQuest = Singleton<PlayerInfoScript>.Instance.StateData.CurrentActiveQuest;
		StaminaCost.text = string.Empty;
		QuestName.text = currentActiveQuest.LevelName;
		EditDeckButton.SetActive(Singleton<PlayerInfoScript>.Instance.CanEditDeck());
		PackSprite.Apply(IconVS, currentActiveQuest.VSIcon);
		RefreshLoadouts(isInit: true);
		Update();
		EditDeckButton.SetActive(!flag);
		CloseButton.SetActive(!flag);
		if (DetachedSingleton<StaminaManager>.Instance.GetStamina(StaminaType.Quests) < currentActiveQuest.StaminaCost && !Singleton<TutorialController>.Instance.IsAnyTutorialActive())
		{
			Singleton<TutorialController>.Instance.StartTutorialBlockIfNotComplete("Stamina");
		}
		shouldShowHelperTile = Singleton<PlayerInfoScript>.Instance.CanUseHelper();
		isHelperTileFadedOut = false;
		UpdateTutorialTeamDescription();
		UpdateSpeedToggleVisibility();
	}

	public void RefreshLoadouts(bool isInit = false)
	{
		QuestData currentActiveQuest = Singleton<PlayerInfoScript>.Instance.StateData.CurrentActiveQuest;
		if (currentActiveQuest != null)
		{
			if (currentActiveQuest.League.ID == "DailyRandom")
			{
				RefreshMyLoadout(loadModel: true, isInit);
				StandardQuestParent.SetActive(value: false);
				RandomQuestParent.SetActive(value: true);
				RandomDungeonFloorData currentData = RandomDungeonFloorDataManager.Instance.GetCurrentData();
				RandomQuestFloor.text = KFFLocalization.Get("!!RANDOM_DUNGEON_FLOOR_X").Replace("<val1>", Singleton<PlayerInfoScript>.Instance.SaveData.RandomDungeonLevel.ToString());
				RandomQuestReward.text = currentActiveQuest.RandomDungeonReward.RewardLabel();
				RandomQuestBattles.text = KFFLocalization.Get("!!X_BATTLES").Replace("<val1>", currentData.Battles.ToString());
			}
			else
			{
				RefreshMyLoadout(loadModel: false, isInit);
				StandardQuestParent.SetActive(value: true);
				RandomQuestParent.SetActive(value: false);
				RefreshEnemyLoadout();
				LeaderData[] leaders = new LeaderData[2]
				{
					Singleton<PlayerInfoScript>.Instance.GetCurrentLoadout().Leader.SelectedSkin,
					currentActiveQuest.Opponent
				};
				FrontEndPIPController instance = Singleton<FrontEndPIPController>.Instance;
				UIWidget[] pIPIconTargets = PIPIconTargets;
				instance.ShowModelPortraits(leaders, pIPIconTargets);
			}
			HideHelperSlot(hide: false);
		}
	}

	private void RefreshMyLoadout(bool loadModel, bool isInit = false)
	{
		if (isInit && Singleton<TutorialController>.Instance.IsBlockActive("Q1"))
		{
			Singleton<PlayerInfoScript>.Instance.SetDefaultLoadout();
		}
		Loadout currentLoadout = Singleton<PlayerInfoScript>.Instance.GetCurrentLoadout();
		_ = Singleton<PlayerInfoScript>.Instance.StateData.CurrentLoadout;
		if (loadModel)
		{
			Singleton<FrontEndPIPController>.Instance.ShowModelPortraits(new LeaderData[1] { currentLoadout.Leader.SelectedSkin }, new UIWidget[1] { PIPIconTargets[0] });
		}
		foreach (GameObject mMySpawnedCreature in mMySpawnedCreatures)
		{
			NGUITools.Destroy(mMySpawnedCreature);
		}
		mMySpawnedCreatures.Clear();
		if (mSpawnedRequiredCreature != null)
		{
			NGUITools.Destroy(mSpawnedRequiredCreature);
			mSpawnedRequiredCreature = null;
		}
		InventoryTile.ClearDelegates(!Singleton<TutorialController>.Instance.IsBlockActive("Q1"));
		for (int i = 0; i < currentLoadout.CreatureSet.Count; i++)
		{
			InventorySlotItem inventorySlotItem = currentLoadout.CreatureSet[i];
			if (inventorySlotItem != null)
			{
				GameObject gameObject = MyCreatureNodes[i].InstantiateAsChild(Singleton<PrefabReferences>.Instance.InventoryTile);
				gameObject.transform.localScale = new Vector3(0.9f, 0.9f, 0.9f);
				gameObject.ChangeLayer(gameObject.transform.parent.gameObject.layer);
				InventoryTile component = gameObject.GetComponent<InventoryTile>();
				component.Populate(inventorySlotItem);
				component.ShowRarityFrameMini();
				mMySpawnedCreatures.Add(gameObject);
			}
		}
		int selectedLoadout = Singleton<PlayerInfoScript>.Instance.SaveData.SelectedLoadout;
		TeamName.text = Singleton<PlayerInfoScript>.Instance.GetTeamName(selectedLoadout);
		UpdateTeamCost();
		CheckCreatureRequirement();
		UpdateTutorialTeamDescription();
	}

	private void UpdateTutorialTeamDescription()
	{
		if (Singleton<TutorialController>.Instance.IsBlockActive("Q1") || Singleton<TutorialController>.Instance.IsFTUETutorialActive())
		{
			int selectedLoadout = Singleton<PlayerInfoScript>.Instance.SaveData.SelectedLoadout;
			TeamDescription.parent.gameObject.SetActive(value: true);
			TeamDescription.text = KFFLocalization.Get("!!TUT_TEAM_DESC_" + (selectedLoadout + 1));
			TeamDescription.color = TutorialTeamColors[selectedLoadout];
			TeamDescriptionBox.color = TutorialTeamColors[selectedLoadout];
		}
		else
		{
			TeamDescription.parent.gameObject.SetActive(value: false);
		}
	}

	private void UpdateTeamCost()
	{
		Loadout currentLoadout = Singleton<PlayerInfoScript>.Instance.GetCurrentLoadout();
		int teamCost = currentLoadout.GetTeamCost(HasHelper());
		int teamCost2 = Singleton<PlayerInfoScript>.Instance.RankData.TeamCost;
		TeamCost.text = KFFLocalization.Get("!!TEAM_WEIGHT") + "  " + teamCost + " / " + teamCost2;
		LeaderNames[0].text = currentLoadout.Leader.Form.Name;
		bool shouldUnload = true;
		if (opponentBGTexture != null && currentLoadout.Leader.Form.VSScreenBackground == opponentBGTexture)
		{
			shouldUnload = false;
		}
		PlayerBackgroundTextures[0].ReplaceTexture(currentLoadout.Leader.Form.VSScreenBackground, shouldUnload);
		if (teamCost > teamCost2)
		{
			ExceededTeamCostTween.Play();
		}
		else
		{
			ExceededTeamCostTween.StopAndReset();
		}
	}

	public void CheckCreatureRequirement()
	{
		Loadout currentLoadout = Singleton<PlayerInfoScript>.Instance.GetCurrentLoadout();
		QuestData currentActiveQuest = Singleton<PlayerInfoScript>.Instance.StateData.CurrentActiveQuest;
		if (currentActiveQuest != null && currentActiveQuest.RequiredCreature != null && !currentLoadout.ContainsCreatureOrEvo(currentActiveQuest.RequiredCreature, HasHelper()))
		{
			StartButton.SetActive(value: false);
			RequiredCreatureParent.SetActive(value: true);
			if (mSpawnedRequiredCreature != null)
			{
				NGUITools.Destroy(mSpawnedRequiredCreature);
				mSpawnedRequiredCreature = null;
			}
			mSpawnedRequiredCreature = RequiredCreatureSpawnNode.transform.InstantiateAsChild(Singleton<PrefabReferences>.Instance.InventoryTile);
			mSpawnedRequiredCreature.GetComponent<InventoryTile>().PopulateAndForceDisplay(currentActiveQuest.RequiredCreature);
		}
		else
		{
			if (mSpawnedRequiredCreature != null)
			{
				NGUITools.Destroy(mSpawnedRequiredCreature);
				mSpawnedRequiredCreature = null;
			}
			StartButton.SetActive(value: true);
			RequiredCreatureParent.SetActive(value: false);
			CheckPvPBanlistInPrep();
		}
	}

	private void CheckPvPBanlistInPrep()
	{
		Loadout currentLoadout = Singleton<PlayerInfoScript>.Instance.GetCurrentLoadout();
		if (Singleton<PlayerInfoScript>.Instance.StateData.CurrentActiveQuest == null || currentLoadout == null)
		{
			return;
		}
		if (!Singleton<PlayerInfoScript>.Instance.ShouldEnforcePvPBans())
		{
			Debug.Log("[PreMatch] Skipping ban check – not in Ranked mode.");
			return;
		}
		if (currentLoadout.Leader?.Form != null)
		{
			string iD = currentLoadout.Leader.Form.ID;
			if (pvpLeaderBanlist.Contains(iD))
			{
				StartButton.SetActive(value: false);
				RequiredCreatureParent.SetActive(value: true);
				if (mSpawnedRequiredCreature != null)
				{
					NGUITools.Destroy(mSpawnedRequiredCreature);
					mSpawnedRequiredCreature = null;
				}
				string newValue = currentLoadout.Leader.Form.Name;
				string body = KFFLocalization.Get("!!PVP_BANNED_CREATURE").Replace("<creature>", newValue);
				Singleton<SimplePopupController>.Instance.ShowMessage(string.Empty, body);
				return;
			}
		}
		foreach (InventorySlotItem item in currentLoadout.CreatureSet)
		{
			if (item?.Creature?.Form == null)
			{
				continue;
			}
			string iD2 = item.Creature.Form.ID;
			if (pvpBanlist.Contains(iD2))
			{
				StartButton.SetActive(value: false);
				RequiredCreatureParent.SetActive(value: true);
				if (mSpawnedRequiredCreature != null)
				{
					NGUITools.Destroy(mSpawnedRequiredCreature);
					mSpawnedRequiredCreature = null;
				}
				mSpawnedRequiredCreature = RequiredCreatureSpawnNode.transform.InstantiateAsChild(Singleton<PrefabReferences>.Instance.InventoryTile);
				mSpawnedRequiredCreature.GetComponent<InventoryTile>().PopulateAndForceDisplay(item.Creature.Form);
				string newValue2 = item.Creature.Form.Name;
				string body2 = KFFLocalization.Get("!!PVP_BANNED_CREATURE").Replace("<creature>", newValue2);
				Singleton<SimplePopupController>.Instance.ShowMessage(string.Empty, body2);
				return;
			}
		}
		if (mSpawnedRequiredCreature != null)
		{
			NGUITools.Destroy(mSpawnedRequiredCreature);
			mSpawnedRequiredCreature = null;
		}
		StartButton.SetActive(value: true);
		RequiredCreatureParent.SetActive(value: false);
	}

	private void RefreshEnemyLoadout()
	{
		QuestData currentActiveQuest = Singleton<PlayerInfoScript>.Instance.StateData.CurrentActiveQuest;
		if (currentActiveQuest == null)
		{
			return;
		}
		InventorySlotItem[] preMatchCreatures = currentActiveQuest.GetPreMatchCreatures();
		foreach (GameObject mEnemySpawnedCreature in mEnemySpawnedCreatures)
		{
			NGUITools.Destroy(mEnemySpawnedCreature);
		}
		mEnemySpawnedCreatures.Clear();
		InventoryTile.ClearDelegates(!Singleton<TutorialController>.Instance.IsBlockActive("Q1"));
		for (int i = 0; i < preMatchCreatures.Length; i++)
		{
			if (preMatchCreatures[i] != null)
			{
				GameObject gameObject = EnemyCreatureNodes[i].InstantiateAsChild(Singleton<PrefabReferences>.Instance.InventoryTile);
				gameObject.ChangeLayer(gameObject.transform.parent.gameObject.layer);
				gameObject.transform.localScale = new Vector3(0.9f, 0.9f, 0.9f);
				InventoryTile component = gameObject.GetComponent<InventoryTile>();
				component.Populate(preMatchCreatures[i]);
				component.DisableCardEditing = true;
				component.ShowRarityFrameMini();
				mEnemySpawnedCreatures.Add(gameObject);
			}
		}
		LeaderNames[1].text = currentActiveQuest.Opponent.Name;
		opponentBGTexture = currentActiveQuest.Opponent.VSScreenBackground;
		PlayerBackgroundTextures[1].ReplaceTexture(currentActiveQuest.Opponent.VSScreenBackground);
	}

	public void OnCloseClicked()
	{
		Singleton<FrontEndPIPController>.Instance.HideModelPortrait();
		OnClickRemoveHelper();
		Singleton<QuestSelectController>.Instance.OnReturnShowBossFX();
	}

	public void Unload()
	{
		foreach (GameObject mMySpawnedCreature in mMySpawnedCreatures)
		{
			NGUITools.Destroy(mMySpawnedCreature);
		}
		mMySpawnedCreatures.Clear();
		foreach (GameObject mEnemySpawnedCreature in mEnemySpawnedCreatures)
		{
			NGUITools.Destroy(mEnemySpawnedCreature);
		}
		mEnemySpawnedCreatures.Clear();
		if (mSpawnedRequiredCreature != null)
		{
			NGUITools.Destroy(mSpawnedRequiredCreature);
			mSpawnedRequiredCreature = null;
		}
	}

	public void OnClickNextLoadout()
	{
		Singleton<PlayerInfoScript>.Instance.GoToNextLoadout(Singleton<TutorialController>.Instance.IsFTUETutorialActive());
		RefreshMyLoadout(loadModel: true);
	}

	public void OnClickPrevLoadout()
	{
		Singleton<PlayerInfoScript>.Instance.GoToPrevLoadout(Singleton<TutorialController>.Instance.IsFTUETutorialActive());
		RefreshMyLoadout(loadModel: true);
	}

	public void OnClickPlay()
	{
		ApplyBattleSpeed();
		GameStateData stateData = Singleton<PlayerInfoScript>.Instance.StateData;
		QuestData currentActiveQuest = stateData.CurrentActiveQuest;
		currentActiveQuest.League.GetBonusStatus(out var bonusActive, out var bonusType, out var _);
		if (bonusActive)
		{
			stateData.ActiveQuestBonus = bonusType;
		}
		else
		{
			stateData.ActiveQuestBonus = QuestBonusType.None;
		}
		Loadout currentLoadout = Singleton<PlayerInfoScript>.Instance.GetCurrentLoadout();
		if (!currentLoadout.IsUsable())
		{
			Singleton<SimplePopupController>.Instance.ShowMessage(string.Empty, KFFLocalization.Get("!!NEED_CREATURE"));
			Singleton<SLOTAudioManager>.Instance.PlayErrorSound();
			return;
		}
		if (currentLoadout.GetTeamCost(HasHelper()) > Singleton<PlayerInfoScript>.Instance.RankData.TeamCost)
		{
			Singleton<SimplePopupController>.Instance.ShowMessage(string.Empty, KFFLocalization.Get("!!EXCEEDS_WEIGHT"));
			Singleton<SLOTAudioManager>.Instance.PlayErrorSound();
			return;
		}
		if (Singleton<PlayerInfoScript>.Instance.SaveData.IsInventorySpaceFull() && Singleton<PlayerInfoScript>.Instance.IsFeatureUnlocked("TBuilding_Sell") && !mDeclineSlotPurchase)
		{
			Singleton<SimplePopupController>.Instance.ShowPurchasePrompt(KFFLocalization.Get("!!INVENTORY_FULL_BUY").Replace("<val2>", MiscParams.InventorySpacePerPurchase.ToString()), KFFLocalization.Get("!!INVENTORY_FULL_NOBUY"), MiscParams.InventorySpacePurchaseCost, PurchaseSlotsAndTryAgain);
			Singleton<SLOTAudioManager>.Instance.PlayErrorSound();
			return;
		}
		bool flag = true;
		int num = ((stateData.ActiveQuestBonus != QuestBonusType.ReducedStamina) ? currentActiveQuest.StaminaCost : currentActiveQuest.ReducedStaminaCost);
		if (DetachedSingleton<StaminaManager>.Instance.GetStamina(StaminaType.Quests) < num)
		{
			if (!Singleton<TutorialController>.Instance.IsFTUETutorialActive())
			{
				if (num > Singleton<PlayerInfoScript>.Instance.RankData.Stamina)
				{
					Singleton<SimplePopupController>.Instance.ShowMessage(string.Empty, KFFLocalization.Get("!!NO_STAMINA_RANKUP"));
				}
				else
				{
					Singleton<SimplePopupController>.Instance.ShowPurchasePrompt(KFFLocalization.Get("!!NO_STAMINA_BUY"), KFFLocalization.Get("!!NO_STAMINA_NOBUY"), MiscParams.StaminaRefillCost, RefillStaminaAndTryAgain);
				}
				Singleton<SLOTAudioManager>.Instance.PlayErrorSound();
				return;
			}
			flag = false;
		}
		if (Singleton<PlayerInfoScript>.Instance.CanUseHelper() && !HasHelper() && !mDeclinedHelper)
		{
			Singleton<SimplePopupController>.Instance.ShowPrompt(string.Empty, KFFLocalization.Get("!!NO_HELPER_MESSAGE"), DeclineHelperAndTryAgain, OnClickAddHelper);
			return;
		}
		UICamera.LockInput();
		if (flag)
		{
			ConsumeTicketTween.Play();
			DetachedSingleton<StaminaManager>.Instance.ConsumeStamina(StaminaType.Quests, num);
		}
		stateData.CurrentLoadout = currentLoadout;
		if (currentActiveQuest.League.QuestLine == QuestLineEnum.Special)
		{
			if (currentActiveQuest.League.ID == "DailyRandom")
			{
				currentActiveQuest.FinalizeRandomQuest();
				stateData.PerfectRandomDungeonRun = true;
				stateData.NoReviveRandomDungeonRun = true;
			}
			else
			{
				Singleton<PlayerInfoScript>.Instance.OnSpecialQuestAttempted(currentActiveQuest.ID);
				if (stateData.DungeonMapQuest)
				{
					Singleton<PlayerInfoScript>.Instance.SaveData.ConsumeDungeonMap(currentActiveQuest.ID);
				}
			}
		}
		RewardManager.UpdatePvPInfo();
		if (Singleton<PlayerInfoScript>.Instance.StateData.SelectedHelper != null)
		{
			if (Singleton<PlayerInfoScript>.Instance.StateData.UsedHelperList == null)
			{
				Singleton<PlayerInfoScript>.Instance.StateData.UsedHelperList = new List<string>();
			}
			if (Singleton<PlayerInfoScript>.Instance.StateData.SelectedHelper.IsAlly == 0)
			{
				Singleton<PlayerInfoScript>.Instance.StateData.UsedHelperList.Add(Singleton<PlayerInfoScript>.Instance.StateData.SelectedHelper.HelperID);
			}
		}
		Singleton<SLOTAudioManager>.Instance.TriggerVOEvent(currentLoadout.Leader.Form, VOEvent.GameStart);
		StartCoroutine(PlayStartAnimInPrep());
	}

	private void DeclineHelperAndTryAgain()
	{
		mDeclinedHelper = true;
		OnClickPlay();
	}

	private IEnumerator PlayStartAnimInPrep()
	{
		Singleton<SLOTAudioManager>.Instance.PlaySound("ui/UI_VS_Screen_Start");
		Singleton<FrontEndPIPController>.Instance.PlayBattleStartAnim();
		yield return new WaitForSeconds(2.5f);
		Singleton<ScreenFadeController>.Instance.ShowLoadScreen(LoadBattleScene);
	}

	private void LoadBattleScene()
	{
		BattleStartTween.StopAndReset();
		DetachedSingleton<SceneFlowManager>.Instance.LoadBattleScene();
	}

	private void PurchaseSlotsAndTryAgain()
	{
		PlayerSaveData saveData = Singleton<PlayerInfoScript>.Instance.SaveData;
		mWaitForUserAction = true;
		mUserActionProceed = NextAction.WAITING;
		Singleton<BusyIconPanelController>.Instance.Show();
		saveData.Purchase("inventory", string.Empty, 1, UserActionCallback);
		mNextFunction = InventorySpaceExecute;
	}

	private void DenyPurchaseSlots()
	{
		mDeclineSlotPurchase = true;
		OnClickPlay();
	}

	public void UserActionCallback(PlayerSaveData.ActionResult result)
	{
		if (result.success)
		{
			mUserActionProceed = NextAction.PROCEED;
		}
		else
		{
			mUserActionProceed = NextAction.ERROR;
		}
	}

	private void InventorySpaceExecute()
	{
		Singleton<PlayerInfoScript>.Instance.SaveData.AddEmptyInventorySlots(MiscParams.InventorySpacePerPurchase);
		Singleton<BuyInventoryPopupController>.Instance.Show(OnClickPlay);
	}

	private void RefillStaminaAndTryAgain()
	{
		mWaitForUserAction = true;
		mUserActionProceed = NextAction.WAITING;
		Singleton<BusyIconPanelController>.Instance.Show();
		Singleton<PlayerInfoScript>.Instance.SaveData.Purchase("stamina", string.Empty, 1, UserActionCallback);
		mNextFunction = StaminaRefillExecute;
	}

	private void StaminaRefillExecute()
	{
		DetachedSingleton<StaminaManager>.Instance.RefillStamina();
		Singleton<BuyStaminaPopupController>.Instance.Show(OnClickPlay);
	}

	public void OnClickEditDeck()
	{
		Singleton<FrontEndPIPController>.Instance.HideModelPortrait();
		HideHelperSlot(hide: true);
		Singleton<EditDeckController>.Instance.Show(delegate
		{
			RefreshLoadouts();
		});
	}

	public void OnClickMyLeader()
	{
		Loadout currentLoadout = Singleton<PlayerInfoScript>.Instance.GetCurrentLoadout();
		Singleton<LeaderDetailsController>.Instance.Show(currentLoadout.Leader, OnLeaderPopupClosed);
	}

	private void OnLeaderPopupClosed()
	{
		RefreshMyLoadout(loadModel: false);
	}

	public void OnClickOpponentLeader()
	{
		QuestData currentActiveQuest = Singleton<PlayerInfoScript>.Instance.StateData.CurrentActiveQuest;
		Singleton<LeaderDetailsController>.Instance.Show(currentActiveQuest.Opponent);
	}

	private void Update()
	{
		HelperSlot.gameObject.SetActive(shouldShowHelperTile);
		UpdateTimers();
		HelperBlockingSlot.SetActive(HasHelper());
		RemoveHelperButton.SetActive(HasHelper());
		if (!mWaitForUserAction)
		{
			return;
		}
		if (mUserActionProceed == NextAction.PROCEED)
		{
			Singleton<BusyIconPanelController>.Instance.Hide();
			mWaitForUserAction = false;
			mUserActionProceed = NextAction.NONE;
			if (mNextFunction != null)
			{
				mNextFunction();
			}
			mWaitForUserAction = false;
			mUserActionProceed = NextAction.NONE;
		}
		if (mUserActionProceed == NextAction.ERROR)
		{
			mWaitForUserAction = false;
			mUserActionProceed = NextAction.NONE;
			Singleton<BusyIconPanelController>.Instance.Hide();
			Singleton<SimplePopupController>.Instance.ShowMessage(string.Empty, KFFLocalization.Get("!!SERVER_ERROR_MESSAGE"), OnCloseServerAccessErrorPopup);
		}
	}

	private void OnCloseServerAccessErrorPopup()
	{
	}

	private void UpdateTimers()
	{
		if (PlayerStamina.gameObject.activeInHierarchy)
		{
			DetachedSingleton<StaminaManager>.Instance.GetStaminaInfo(StaminaType.Quests, out var _, out var maxStamina, out var secondsUntilNextStamina);
			PlayerStamina.text = maxStamina.ToString();
			PlayerStaminaTimer.text = ((secondsUntilNextStamina <= 0) ? string.Empty : PlayerInfoScript.BuildTimerString(secondsUntilNextStamina));
			QuestData currentActiveQuest = Singleton<PlayerInfoScript>.Instance.StateData.CurrentActiveQuest;
			currentActiveQuest.League.GetBonusStatus(out var bonusActive, out var bonusType, out var _);
			int num = ((!bonusActive || bonusType != QuestBonusType.ReducedStamina) ? currentActiveQuest.StaminaCost : currentActiveQuest.ReducedStaminaCost);
			StaminaCost.text = num.ToString();
		}
	}

	public void OnClickAddHelper()
	{
		Singleton<PreMatchHelperSelectController>.Instance.Show();
	}

	public void OnClickRemoveHelper()
	{
		Singleton<PreMatchHelperSelectController>.Instance.SetHelperToLoadout(null);
		if (Singleton<PreMatchHelperSelectController>.Instance.SelectedHelperCreatureTile != null)
		{
			Singleton<PreMatchHelperSelectController>.Instance.SelectedHelperCreatureTile.gameObject.name += "_ToBeRemoved";
			NGUITools.Destroy(Singleton<PreMatchHelperSelectController>.Instance.SelectedHelperCreatureTile.gameObject);
		}
		Singleton<PreMatchHelperSelectController>.Instance.SelectedHelperCreatureTile = null;
	}

	public void PlayTweenForSetHelperCreature(CreatureItem creature)
	{
		foreach (GameObject mMySpawnedCreature in mMySpawnedCreatures)
		{
			InventoryTile component = mMySpawnedCreature.GetComponent<InventoryTile>();
			if (component.InventoryItem.Creature == creature)
			{
				component.SetCreatureToTarget();
			}
		}
	}

	public void HideHelperSlot(bool hide)
	{
		if (isHelperTileFadedOut != hide)
		{
			if (hide)
			{
				isHelperTileFadedOut = true;
				FadeOutHelperButtonTween.Play();
			}
			else
			{
				isHelperTileFadedOut = false;
				FadeInHelperButtonTween.Play();
			}
		}
	}

	public void TriggerTweenForDraggingHelper()
	{
		if (DetachedSingleton<SceneFlowManager>.Instance.GetCurrentScene() == SceneFlowManager.Scene.FrontEnd)
		{
			Singleton<PreMatchController>.Instance.ShowHelperBeingDraggedTween.Play();
		}
	}

	public void StopTweenForDraggingHelper()
	{
		HideHelperBeingDraggedTween.Play();
		ShowHelperBeingDraggedTween.StopAndReset();
	}

	private bool HasHelper()
	{
		return Singleton<PlayerInfoScript>.Instance.StateData.HelperCreature != null;
	}

	public void OnHelperChanged()
	{
		CheckCreatureRequirement();
		UpdateTeamCost();
	}

	public void OnSpeedToggleChanged()
	{
		use2xSpeed = !use2xSpeed;
		UpdateButtonAppearance();
		Debug.Log($"[PreMatch] 2x speed: {use2xSpeed}");
	}

	private void UpdateButtonAppearance()
	{
		if (!(SpeedToggleButton == null))
		{
			Transform transform = SpeedToggleButton.transform.Find("Checkmark Icon/ReadyCheck (HIDE)");
			if (transform != null)
			{
				transform.gameObject.SetActive(use2xSpeed);
			}
			else
			{
				Debug.LogWarning("[PreMatch] Could not find checkmark!");
			}
		}
	}

	public void UpdateSpeedToggleVisibility()
	{
		if (!(SpeedToggleButton == null))
		{
			QuestData currentActiveQuest = Singleton<PlayerInfoScript>.Instance.StateData.CurrentActiveQuest;
			bool flag = currentActiveQuest != null && currentActiveQuest.League != null && IsDailyMaterialDungeon(currentActiveQuest.League.ID);
			SpeedToggleButton.SetActive(flag);
			if (flag)
			{
				use2xSpeed = false;
				UpdateButtonAppearance();
			}
		}
	}

	public static bool IsDailyDungeonBattle()
	{
		GameStateData stateData = Singleton<PlayerInfoScript>.Instance.StateData;
		QuestData currentActiveQuest = stateData.CurrentActiveQuest;
		if (!stateData.MultiplayerMode && currentActiveQuest != null && currentActiveQuest.League != null)
		{
			return IsDailyMaterialDungeon(currentActiveQuest.League.ID);
		}
		return false;
	}

	private static bool IsDailyMaterialDungeon(string leagueID)
	{
		switch (leagueID)
		{
		default:
			return leagueID == "SnackHour";
		case "Monday":
		case "Tuesday":
		case "Wednesday":
		case "Thursday":
		case "Friday":
		case "Saturday":
		case "Sunday":
			return true;
		}
	}

	private void ApplyBattleSpeed()
	{
		QuestData currentActiveQuest = Singleton<PlayerInfoScript>.Instance.StateData.CurrentActiveQuest;
		if (currentActiveQuest != null && currentActiveQuest.League != null && IsDailyMaterialDungeon(currentActiveQuest.League.ID) && use2xSpeed)
		{
			PlayerPrefs.SetFloat("BattleSpeedMultiplier", 2f);
			PlayerPrefs.Save();
			Debug.Log("[PreMatch] 2x battle speed enabled for daily dungeon");
		}
		else
		{
			PlayerPrefs.SetFloat("BattleSpeedMultiplier", 1f);
			PlayerPrefs.Save();
		}
	}
}
