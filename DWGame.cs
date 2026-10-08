using System;
using System.Collections;
using System.Collections.Generic;
using CodeStage.AntiCheat.ObscuredTypes;
using UnityEngine;

public class DWGame : Singleton<DWGame>
{
	public enum TurnActions
	{
		PlayCard = 0,
		PlayMonster = 1,
		Attack = 2
	}

	private GameState mGameState;

	private bool mRevivingPlayerThisTurn;

	public List<InventorySlotItem> TempUserSet = new List<InventorySlotItem>();

	public List<InventorySlotItem> TempAISet = new List<InventorySlotItem>();

	public Loadout UserLoadout;

	public Loadout OpLoadout;

	private static int mAutoRestartCount;

	private BoardState MasterBoardState;

	public bool IsTutorialSetup;

	private IAbilitySource PendingEffectSource;

	private SelectionType PendingSelectionType;

	private GameObject mLoadingCreatureModel;

	private bool mUserWonCoinFlip;

	private bool mStateSwitchInputLock;

	private ObscuredFloat mMultiplayerTimeLeft = -1f;

	private ObscuredFloat mSaveMultiplayerTimeLeft = -1f;

	public int turnNumber;

	public float battleDuration;

	public bool battleStarted;

	public bool turnStarted;

	public float turnDuration;

	public List<TurnActions> turnActions = new List<TurnActions>();

	private bool IsAwakeDone;

	private bool IntroStarted;

	private bool DeployEnemyDone;

	private bool P1ActionStarted;

	private bool P1SelectLaneStarted;

	private bool P1AttackStarted;

	private bool DeployCreatureStart;

	private bool _pvpReplayCaptureStarted;

	private const bool DisablePvpScreenRecording = true;

	public bool LostDuringMyTurn { get; set; }

	public bool SelectingLane { get; set; }

	public bool WaitingForCreatureDeployAfterRevive { get; private set; }

	public bool IsSetUp { get; set; }

	public BoardState CurrentBoardState => MasterBoardState;

	public int MultiplayerSecondsLeft => (int)(float)mMultiplayerTimeLeft;

	public bool InDeploymentPhase()
	{
		return MasterBoardState.IsDeployment;
	}

	public bool IsFirstTurnAfterRevive()
	{
		return mRevivingPlayerThisTurn;
	}

	public void StopMultiplayerTimer()
	{
		mSaveMultiplayerTimeLeft = mMultiplayerTimeLeft;
		mMultiplayerTimeLeft = -1f;
	}

	public void ResumeMultiplayerTimer()
	{
		mMultiplayerTimeLeft = mSaveMultiplayerTimeLeft;
	}

	public void Debug5SecondMultiplayerTimer()
	{
		mMultiplayerTimeLeft = 5f;
	}

	private void Awake()
	{
		StartCoroutine(DWGameAwake());
	}

	private IEnumerator DWGameAwake()
	{
		StartCoroutine(CardBlocklist.Fetch());
		while (!SessionManager.Instance.IsLoadDataDone())
		{
			yield return null;
		}
		while (!Singleton<PlayerInfoScript>.Instance.IsInitialized)
		{
			yield return null;
		}
		yield return StartCoroutine(Singleton<AIManager>.Instance.CreatePool());
		float num = (PreMatchController.IsDailyDungeonBattle() ? PlayerPrefs.GetFloat("BattleSpeedMultiplier", 1f) : 1f);
		PlayerPrefs.SetFloat("BattleSpeedMultiplier", num);
		Time.timeScale = num;
		Debug.Log($"[DWGame] Battle speed set to {num}x");
		MasterBoardState = BoardState.Create();
		IsAwakeDone = true;
	}

	private void OnDestroy()
	{
		Time.timeScale = 1f;
		if (MasterBoardState != null)
		{
			BoardState.Destroy(MasterBoardState);
		}
	}

	public void InitPlayer(PlayerType idx, Loadout loadout, bool reviving = false)
	{
		loadout.Leader.Form.ParseKeywords();
		foreach (InventorySlotItem item in loadout.CreatureSet)
		{
			item?.Creature.Form.ParseKeywords();
		}
		MasterBoardState.InitPlayer(idx, loadout, reviving);
	}

	private void OnApplicationQuit()
	{
		Time.timeScale = 1f;
		PlayerPrefs.SetFloat("BattleSpeedMultiplier", 1f);
		if (Singleton<PlayerInfoScript>.Instance.StateData.MultiplayerMode)
		{
			Singleton<DWGame>.Instance.turnNumber = 0;
			Singleton<DWGame>.Instance.battleDuration = 0f;
		}
	}

	public void Setup()
	{
		PlayerInfoScript instance = Singleton<PlayerInfoScript>.Instance;
		if (instance.StateData.MultiplayerMode)
		{
			instance.StateData.CurrentActiveQuest = QuestDataManager.Instance.GetData("PVP");
		}
		else if (instance.StateData.CurrentActiveQuest == null)
		{
			instance.StateData.CurrentActiveQuest = QuestDataManager.Instance.GetData(0);
		}
		QuestData currentActiveQuest = instance.StateData.CurrentActiveQuest;
		if (instance.StateData.CurrentLoadout != null)
		{
			UserLoadout = instance.StateData.CurrentLoadout.Copy();
		}
		if (UserLoadout == null)
		{
			UserLoadout = instance.GetCurrentLoadout().Copy();
		}
		if (Singleton<PlayerInfoScript>.Instance.StateData.SelectedHelper != null)
		{
			InventorySlotItem helperCreature = Singleton<PlayerInfoScript>.Instance.StateData.HelperCreature;
			UserLoadout.CreatureSet[MiscParams.CreaturesOnTeam - 1] = helperCreature;
		}
		if (!instance.StateData.MultiplayerMode)
		{
			OpLoadout = BuildOpponentLoadout(currentActiveQuest);
		}
		else
		{
			OpLoadout = Singleton<PlayerInfoScript>.Instance.PvPData.OpponentLoadout;
		}
		if (Singleton<PlayerInfoScript>.Instance.StateData.MultiplayerMode && !Singleton<PlayerInfoScript>.Instance.PvPData.AmIPrimary)
		{
			InitPlayer(PlayerType.Opponent, OpLoadout);
			InitPlayer(PlayerType.User, UserLoadout);
		}
		else
		{
			InitPlayer(PlayerType.User, UserLoadout);
			InitPlayer(PlayerType.Opponent, OpLoadout);
		}
		Singleton<BattleHudController>.Instance.PopulateHeros(Singleton<DWGame>.Instance.CurrentBoardState.GetPlayerState(0), Singleton<DWGame>.Instance.CurrentBoardState.GetPlayerState(1));
		RewardManager.Init(currentActiveQuest, OpLoadout);
		MasterBoardState.Setup();
		IsSetUp = true;
		TryStartPvpReplayCapture();
	}

	private void TryStartPvpReplayCapture()
	{
		if (!_pvpReplayCaptureStarted && Singleton<PlayerInfoScript>.Instance.StateData.MultiplayerMode)
		{
			BattleReplayRecorder orCreateInstance = BattleReplayRecorder.GetOrCreateInstance();
			if (!orCreateInstance.IsRecording)
			{
				orCreateInstance.StartRecording();
			}
			if (UnityRecorderBattleRecorder.Instance.IsRecording)
			{
				UnityRecorderBattleRecorder.Instance.StopBattleRecording();
			}
			_pvpReplayCaptureStarted = true;
		}
	}

	private void TryStopPvpReplayCapture(bool playerWon)
	{
		if (Singleton<PlayerInfoScript>.Instance.StateData.MultiplayerMode)
		{
			if (Singleton<BattleReplayRecorder>.Instance != null && Singleton<BattleReplayRecorder>.Instance.IsRecording)
			{
				Singleton<BattleReplayRecorder>.Instance.StopRecording(playerWon);
			}
			if (UnityRecorderBattleRecorder.Instance.IsRecording)
			{
				UnityRecorderBattleRecorder.Instance.StopBattleRecording();
			}
			_pvpReplayCaptureStarted = false;
		}
	}

	public IEnumerator SetupIntroBattleBoard()
	{
		IsTutorialSetup = true;
		DetachedSingleton<MissionManager>.Instance.ResetBattle();
		Singleton<AIManager>.Instance.ResetCreatureIndex();
		TutorialBoardData tutorialBoard = Singleton<TutorialController>.Instance.GetTutorialBoard();
		PlayerState playerState = MasterBoardState.GetPlayerState(PlayerType.User);
		PlayerState playerState2 = MasterBoardState.GetPlayerState(PlayerType.Opponent);
		foreach (TutorialBoardEntry entry in tutorialBoard.Entries)
		{
			if (entry.whichPlayer == PlayerType.User)
			{
				CreatureState creatureState = playerState.DeploymentList.Find((CreatureState m) => m.Data.Form.ID == entry.CreatureID);
				if (creatureState != null)
				{
					if (entry.isOut)
					{
						DeployCreatureFromCard(entry.whichPlayer, creatureState.Data, 0);
						int hP = Mathf.RoundToInt(Mathf.Ceil((float)creatureState.MaxHP * entry.HPFactor));
						creatureState.SetHP(hP);
					}
					else
					{
						Singleton<HandCardController>.Instance.ShowCreatureCardDraw(PlayerType.User, creatureState.Data);
					}
				}
			}
			else
			{
				if (entry.whichPlayer != PlayerType.Opponent)
				{
					continue;
				}
				CreatureState creatureState2 = playerState2.DeploymentList.Find((CreatureState m) => m.Data.Form.ID == entry.CreatureID);
				if (creatureState2 != null)
				{
					if (entry.isOut)
					{
						DeployCreatureFromCard(entry.whichPlayer, creatureState2.Data, 0);
						int hP2 = Mathf.RoundToInt(Mathf.Ceil((float)creatureState2.MaxHP * entry.HPFactor));
						creatureState2.SetHP(hP2);
					}
					else
					{
						Singleton<HandCardController>.Instance.ShowCreatureCardDraw(PlayerType.Opponent, creatureState2.Data);
					}
				}
			}
		}
		Singleton<DWGameCamera>.Instance.InitPIPCams();
		Singleton<DWBattleLane>.Instance.RepositionLaneObjects();
		Singleton<BattleHudController>.Instance.HideBattleButtonTween.Play();
		Singleton<HandCardController>.Instance.HideHand();
		Singleton<HandCardController>.Instance.HideOpponentTween.Play();
		SetGameState(GameState.P1StartTurn);
		yield return null;
		MasterBoardState.IsDeployment = false;
		MasterBoardState.ClearFirstTurnFlag();
		Singleton<DWGameCamera>.Instance.MoveCameraToP2Setup();
		while (!Singleton<DWGameMessageHandler>.Instance.IsEffectDone())
		{
			yield return null;
		}
		IsTutorialSetup = false;
		if (LoadingScreenController.GetInstance() != null)
		{
			LoadingScreenController.GetInstance().HideLoading(OnHideLoadingFinished);
		}
	}

	private Loadout BuildOpponentLoadout(QuestData questData)
	{
		Loadout loadout = new Loadout();
		loadout.Leader = new LeaderItem(questData.Opponent);
		for (int i = 0; i < questData.SetLoadout.Entries.Count; i++)
		{
			loadout.CreatureSet.Add(new InventorySlotItem(questData.SetLoadout.Entries[i].BuildCreatureItem()));
		}
		int num = questData.EnemyCount - questData.SetLoadout.Entries.Count;
		for (int j = 0; j < num; j++)
		{
			QuestLoadoutEntry randomEntry = questData.RandomLoadout.GetRandomEntry();
			loadout.CreatureSet.Add(new InventorySlotItem(randomEntry.BuildCreatureItem()));
		}
		return loadout;
	}

	public void StartTurn(PlayerType player)
	{
		DetachedSingleton<CustomAIManager>.Instance.SetSpecialRulesThisTurn(Singleton<DWGame>.Instance.CurrentBoardState, player);
		MasterBoardState.StartTurn(player);
		ProcessMessages();
		while (AttackProgress.AttacksInProgress)
		{
			MasterBoardState.UpdateAttackState(player);
			ProcessMessages();
		}
	}

	public int DetermineCost(PlayerType player, CardData card)
	{
		return card.Cost;
	}

	public PlayerState.CanPlayResult CanPlay(PlayerType player, CardData card, PlayerType targetPlayer = null, int LaneIndex = -1)
	{
		return MasterBoardState.CanPlay(player, card, targetPlayer, LaneIndex);
	}

	public PlayerState.CanPlayResult CanPlay(PlayerType player, CreatureItem creature)
	{
		if (InDeploymentPhase())
		{
			return PlayerState.CanPlayResult.CanPlay;
		}
		if (MasterBoardState.CanDeploy(player, creature))
		{
			return PlayerState.CanPlayResult.CanPlay;
		}
		return PlayerState.CanPlayResult.NotEnoughAP;
	}

	public List<LaneState> GetOccupiedLanes()
	{
		return MasterBoardState.GetOccupiedLanes();
	}

	public List<LaneState> GetFirstTargetList(PlayerType WhichPlayer, CardData Card)
	{
		return MasterBoardState.GetFirstTargetList(WhichPlayer, Card);
	}

	public List<LaneState> GetSecondTargetList(PlayerType WhichPlayer, CardData Card)
	{
		return MasterBoardState.GetSecondTargetList(WhichPlayer, Card);
	}

	public int GetCreatureCount(PlayerType WhichPlayer)
	{
		return MasterBoardState.GetCreatureCount(WhichPlayer);
	}

	public void SetTarget(PlayerType WhichPlayer, CreatureState Target)
	{
		MasterBoardState.SetTarget(WhichPlayer, Target);
	}

	public void SetTargetByLane(PlayerType WhichPlayer, int laneIndex)
	{
		PlayerType idx = ((WhichPlayer != PlayerType.User) ? PlayerType.User : PlayerType.Opponent);
		CreatureState creature = MasterBoardState.GetCreature(idx, laneIndex);
		MasterBoardState.SetTarget(WhichPlayer, creature);
	}

	public void PlayActionCard(PlayerType WhichPlayer, CardData Card, PlayerType targetPlayer, int LaneIndex1 = -1, string shapeShiftPick = null)
	{
		if (Card.ShapeShift && shapeShiftPick == null && WhichPlayer == PlayerType.User && targetPlayer == PlayerType.User)
		{
			CreatureState creature = MasterBoardState.GetCreature(PlayerType.User, LaneIndex1);
			ShapeShiftCinematic.Instance.BeginPick(creature, (creature == null) ? null : ShapeShiftAdvisor.Choose(creature, Card.ShapeShiftPool, 5), KFFLocalization.Get(Card.Name), delegate(string pick)
			{
				PlayActionCard(WhichPlayer, Card, targetPlayer, LaneIndex1, pick);
			});
			return;
		}
		if (Card.Graveyard && shapeShiftPick == null && WhichPlayer == PlayerType.User && targetPlayer == PlayerType.User)
		{
			GraveyardCinematic.Instance.BeginPick(MasterBoardState.GetPlayerState(PlayerType.User), MasterBoardState.GetCreature(PlayerType.User, LaneIndex1), KFFLocalization.Get(Card.Name), delegate(string pick)
			{
				PlayActionCard(WhichPlayer, Card, targetPlayer, LaneIndex1, pick);
			});
			return;
		}
		if (Singleton<PlayerInfoScript>.Instance.StateData.MultiplayerMode && WhichPlayer == PlayerType.User)
		{
			int targetLane = CurrentBoardState.GetPlayerState(PlayerType.User).GetTargetLane();
			Singleton<MultiplayerMessageHandler>.Instance.SendCardPlay(Card, targetPlayer, LaneIndex1, targetLane, shapeShiftPick);
		}
		MasterBoardState.PlayActionCard(WhichPlayer, Card, targetPlayer, LaneIndex1);
		CardProgress.Instance.ShapeShiftPick = shapeShiftPick;
		Card.AlreadySeen = true;
	}

	public IEnumerator RaisedCreatureVisual(CreatureState c)
	{
		Debug.Log("[Graveyard] raise visual " + ((c != null && c.Data != null) ? c.Data.Form.ID : "null") + " lane " + ((c != null && c.Lane != null) ? c.Lane.Index.ToString() : "none"));
		if (c == null || c.Data == null || c.Lane == null)
		{
			yield break;
		}
		if (Singleton<DWBattleLane>.Instance.CreaturePool.TryGetValue(c.Data, out var value))
		{
			Singleton<DWBattleLane>.Instance.CreaturePool.Remove(c.Data);
			if (value != null)
			{
				UnityEngine.Object.Destroy(value);
			}
		}
		yield return StartCoroutine(PoolCreatureData(c.Data));
		SpawnLaneObject(c.Owner.Type, c, c.Data, c.Lane.Index, rising: true);
		DWBattleLaneObject laneObject = Singleton<DWBattleLane>.Instance.GetLaneObject(c);
		GameObject body = ((laneObject != null) ? laneObject.CreatureObject : null);
		if (body == null)
		{
			Debug.LogError("[Graveyard] raised creature has no body");
			yield break;
		}
		body.SetActive(value: false);
		yield return StartCoroutine(GraveyardCinematic.Instance.RiseFromGround(c, body));
		body.SetActive(value: true);
	}

	public IEnumerator ShapeShiftVisual(CreatureState c)
	{
		DWBattleLaneObject laneObj = Singleton<DWBattleLane>.Instance.GetLaneObject(c);
		if (!(laneObj == null) && c.Data != null)
		{
			GameObject old = laneObj.CreatureObject;
			bool pooled = false;
			StartCoroutine(PoolThen(c.Data, delegate
			{
				pooled = true;
			}));
			yield return StartCoroutine(ShapeShiftCinematic.Instance.Shift(c, old, () => pooled, delegate
			{
				SwapShiftedModel(c, laneObj, old);
			}));
		}
	}

	private IEnumerator PoolThen(CreatureItem item, Action done)
	{
		yield return StartCoroutine(PoolCreatureData(item));
		done();
	}

	private void SwapShiftedModel(CreatureState c, DWBattleLaneObject laneObj, GameObject old)
	{
		GameObject value = null;
		Singleton<DWBattleLane>.Instance.CreaturePool.TryGetValue(c.Data, out value);
		if (!(value == null))
		{
			value.transform.parent = laneObj.transform;
			value.transform.localPosition = ((old != null) ? old.transform.localPosition : Vector3.zero);
			value.transform.localRotation = ((old != null) ? old.transform.localRotation : Quaternion.identity);
			value.transform.localScale = ((old != null) ? old.transform.localScale : Vector3.one);
			laneObj.CreatureObject = value;
			value.SetActive(value: true);
			Animator componentInChildren = value.GetComponentInChildren<Animator>();
			if (componentInChildren != null)
			{
				componentInChildren.speed = 1f;
			}
			if (old != null)
			{
				iTween.Stop(old);
				old.SetActive(value: false);
			}
			Vector3 size = laneObj.LaneCollider.size;
			size.x = Mathf.Max(c.Data.Form.Width, Singleton<DWBattleLane>.Instance.MinimumCreatureWidth);
			laneObj.LaneCollider.size = size;
			laneObj.ShadowBlob.Spawn(value, c.Data);
			Singleton<DWBattleLane>.Instance.RepositionLaneObjects();
			CreatureHPBar hPBar = Singleton<BattleHudController>.Instance.GetHPBar(c);
			if (hPBar != null)
			{
				hPBar.Init(c);
			}
			CreatureBuffBar buffBar = Singleton<BattleHudController>.Instance.GetBuffBar(c);
			if (buffBar != null)
			{
				buffBar.Init(c);
			}
			if (old != null)
			{
				UnityEngine.Object.Destroy(old);
			}
			if (c.Data.Form.IntroSound != null)
			{
				Singleton<SLOTAudioManager>.Instance.PlaySound("creature/" + c.Data.Form.IntroSound);
			}
			StartCoroutine(TriggerSpawnCreatureEffects(c));
		}
	}

	public void DeployCreatureFromCard(PlayerType player, CreatureItem creature, int laneIndex, bool fromPullCard = false)
	{
		if (Singleton<PlayerInfoScript>.Instance.StateData.MultiplayerMode && player == PlayerType.User)
		{
			Singleton<MultiplayerMessageHandler>.Instance.SendCreaturePlay(creature, laneIndex);
		}
		CreatureState creature2;
		if (fromPullCard)
		{
			PlayerState playerState = MasterBoardState.GetPlayerState(player);
			creature2 = playerState.GetCreature(creature);
			if (creature2 == null)
			{
				Debug.LogError("Graveyard revive failed: creatureState NULL");
				return;
			}
			creature2 = creature2.DeepCopy();
			creature2.Owner = playerState;
			creature2.Lane = null;
		}
		else
		{
			creature2 = MasterBoardState.DeployCreature(player, creature, laneIndex);
			if (creature2 == null)
			{
				Debug.LogError("DeployCreature returned NULL");
				return;
			}
			if (player == PlayerType.User)
			{
				WaitingForCreatureDeployAfterRevive = false;
			}
		}
		SpawnLaneObject(player, creature2, creature, laneIndex);
	}

	private void SpawnLaneObject(PlayerType player, CreatureState creatureState, CreatureItem creature, int laneIndex, bool rising = false)
	{
		if (creatureState == null || creatureState.Data == null)
		{
			Debug.LogError("Invalid creatureState before AddLaneObject");
			return;
		}
		DWBattleLaneObject dWBattleLaneObject = Singleton<DWBattleLane>.Instance.AddLaneObject(creatureState, creature, laneIndex);
		if (dWBattleLaneObject == null)
		{
			Debug.LogError("AddLaneObject failed! Creature skipped: " + creature?.Form?.ID);
			return;
		}
		Vector3 size = dWBattleLaneObject.LaneCollider.size;
		size.x = creature.Form.Width;
		if (size.x < Singleton<DWBattleLane>.Instance.MinimumCreatureWidth)
		{
			size.x = Singleton<DWBattleLane>.Instance.MinimumCreatureWidth;
		}
		dWBattleLaneObject.LaneCollider.size = size;
		GameObject value = null;
		Singleton<DWBattleLane>.Instance.CreaturePool.TryGetValue(creature, out value);
		if (value == null)
		{
			Debug.LogError("CreaturePool missing prefab for: " + creature.Form?.ID);
			return;
		}
		value.transform.parent = dWBattleLaneObject.transform;
		value.transform.localPosition = Vector3.zero;
		if (player == PlayerType.Opponent && creature.QuestLoadoutEntryData != null)
		{
			float creatureScale = creature.QuestLoadoutEntryData.CreatureScale;
			value.transform.localScale = Vector3.one * creatureScale;
			if (creatureScale > 1f)
			{
				Vector3 localPosition = value.transform.localPosition;
				localPosition.z += Singleton<DWBattleLane>.Instance.MoveScaledUpCreaturesBackFactor * (creatureScale - 1f);
				value.transform.localPosition = localPosition;
			}
		}
		else
		{
			value.transform.localScale = Vector3.one;
		}
		float y = ((player != PlayerType.Opponent) ? 0f : 180f);
		value.transform.localRotation = Quaternion.Euler(0f, y, 0f);
		dWBattleLaneObject.CreatureObject = value;
		Singleton<DWBattleLane>.Instance.RepositionLaneObjects();
		if (rising)
		{
			dWBattleLaneObject.ShadowBlob.Spawn(value, creatureState.Data);
			StartCoroutine(TriggerSpawnCreatureEffects(creatureState));
		}
		else if (!IsTutorialSetup)
		{
			StartCoroutine(StartSummonAnim(value, creatureState));
			StartCoroutine(TriggerSpawnCreatureEffects(creatureState));
		}
		else
		{
			value.SetActive(value: true);
			dWBattleLaneObject.ShadowBlob.Spawn(value, creatureState.Data);
		}
		Singleton<BattleHudController>.Instance.SpawnHPBar(dWBattleLaneObject);
		creature.Form.AlreadySeen = true;
	}

	public IEnumerator TriggerSpawnCreatureEffects(CreatureState creatureState)
	{
		while (Singleton<PauseController>.Instance.Paused)
		{
			yield return null;
		}
		if (creatureState == null || creatureState.Data == null || creatureState.Data.Form == null)
		{
			yield break;
		}
		CreatureData form = creatureState.Data.Form;
		GameObject creatureObject = Singleton<DWBattleLane>.Instance.GetCreatureObject(creatureState);
		DWBattleLaneObject laneObject = Singleton<DWBattleLane>.Instance.GetLaneObject(creatureState);
		if (creatureObject == null || laneObject == null)
		{
			yield break;
		}
		laneObject.RevertMaterials();
		GameObject value = null;
		Singleton<DWBattleLane>.Instance.CreatureVFXPool.TryGetValue(form.PersistentVFX, out value);
		if (value == null && !string.IsNullOrEmpty(form.PersistentVFX))
		{
			value = SLOTResourceManager.Load<GameObject>("VFX/Creatures/" + form.PersistentVFX);
		}
		if (value != null && !string.IsNullOrEmpty(form.PersistentVFXAttachBone))
		{
			foreach (Transform item in FindAllInChildren(creatureObject.transform, form.PersistentVFXAttachBone))
			{
				if (item != null)
				{
					item.InstantiateAsChild(value);
				}
			}
		}
		value = null;
		Singleton<DWBattleLane>.Instance.CreatureVFXPool.TryGetValue(form.RezInVFX, out value);
		if (value == null && !string.IsNullOrEmpty(form.RezInVFX))
		{
			value = SLOTResourceManager.Load<GameObject>("VFX/Creatures/" + form.RezInVFX);
		}
		if (value != null)
		{
			Singleton<SLOTAudioManager>.Instance.PlaySound("battle/SFX_Rez_In");
			Transform transform = creatureObject.transform;
			UnityEngine.Object.Instantiate(value, transform.position, transform.rotation, transform);
			StartCoroutine(PlayRezInDelayed());
		}
		static IEnumerator PlayRezInDelayed()
		{
			yield return new WaitForSeconds(0.25f);
			Singleton<SLOTAudioManager>.Instance.PlaySound("battle/SFX_RezIn3", 0.2f);
		}
	}

	public IEnumerator StartSummonAnim(GameObject creatureObj, CreatureState creatureState)
	{
		while (Singleton<PauseController>.Instance.Paused)
		{
			yield return null;
		}
		CardProgress.Instance.State = CardState.Intro;
		if (InDeploymentPhase() && GetCurrentGameState().IsP1Turn())
		{
			Singleton<BattleHudController>.Instance.HideDeployTween.Play();
		}
		Singleton<DWGameCamera>.Instance.MoveCameraToCreatureSummon(creatureState);
		Singleton<DWGameCamera>.Instance.Battle3DUICam.enabled = false;
		yield return new WaitForSeconds(0.4f);
		creatureObj.SetActive(value: true);
		Animator componentInChildren = creatureObj.GetComponentInChildren<Animator>();
		if (componentInChildren != null)
		{
			componentInChildren.speed = 1f;
		}
		if (creatureState.Data.Form.IntroSound != null)
		{
			Singleton<SLOTAudioManager>.Instance.PlaySound("creature/" + creatureState.Data.Form.IntroSound);
		}
		yield return new WaitForSeconds(1f);
		CardProgress.Instance.State = CardState.Idle;
		Singleton<DWGameCamera>.Instance.Battle3DUICam.enabled = true;
		Singleton<DWGameCamera>.Instance.MoveCameraToP1Setup();
	}

	private Transform FindInChildren(Transform tr, string childName)
	{
		Transform[] componentsInChildren = tr.gameObject.GetComponentsInChildren<Transform>();
		foreach (Transform transform in componentsInChildren)
		{
			if (transform.name.Contains(childName))
			{
				return transform;
			}
		}
		return null;
	}

	public List<Transform> FindAllInChildren(Transform tr, string childName)
	{
		List<Transform> list = new List<Transform>();
		if (childName == string.Empty)
		{
			list.Add(tr);
			return list;
		}
		return tr.gameObject.GetComponentsInChildren<Transform>().FindAll((Transform m) => m.name.Contains(childName));
	}

	public void AddMessage(GameMessage Message)
	{
		MasterBoardState.AddMessage(Message);
	}

	public void ProcessMessages()
	{
		MasterBoardState.RecordMessages();
		List<GameMessage> list = MasterBoardState.ProcessMessages();
		if (list.Count > 0)
		{
			Singleton<DWGameMessageHandler>.Instance.ProcessMessages(list);
			MasterBoardState.ClearProcessedMessageList();
		}
	}

	public void Update()
	{
		if (!IsAwakeDone)
		{
			return;
		}
		if (battleStarted)
		{
			battleDuration += Time.deltaTime;
		}
		if (turnStarted)
		{
			turnDuration += Time.deltaTime;
		}
		if (Singleton<PlayerInfoScript>.Instance.StateData.MultiplayerMode && (float)mMultiplayerTimeLeft >= 0f)
		{
			mMultiplayerTimeLeft = (float)mMultiplayerTimeLeft - Time.unscaledDeltaTime;
			if ((float)mMultiplayerTimeLeft <= 0f)
			{
				mMultiplayerTimeLeft = -1f;
				StartCoroutine(EndTurnOnMultiplayerTimeUp());
			}
		}
		UpdateGameState();
		if (CardProgress.Instance.State != CardState.Idle && CardProgress.Instance.State != CardState.Intro)
		{
			MasterBoardState.UpdateActionCard(CardProgress.Instance.UsingPlayer);
		}
		if (AttackProgress.AttacksInProgress)
		{
			MasterBoardState.UpdateAttackState(AttackProgress.Attacks[0].UsingPlayer);
		}
		ProcessMessages();
		if (mGameState == GameState.P1Turn)
		{
			Singleton<SLOTAudioManager>.Instance.TriggerVOEvent(UserLoadout.Leader.Form, VOEvent.Idle);
		}
	}

	private IEnumerator EndTurnOnMultiplayerTimeUp()
	{
		UICamera.LockInput();
		while (!Singleton<DWGameMessageHandler>.Instance.IsEffectDone())
		{
			yield return null;
		}
		if (SelectingLane)
		{
			Singleton<DWBattleLane>.Instance.CancelTargetSelection();
		}
		Singleton<CreatureInfoPopup>.Instance.HideIfShowing();
		Singleton<HandCardController>.Instance.CancelCardDrag();
		Singleton<DWBattleLane>.Instance.CancelDragAttack();
		Singleton<HandCardController>.Instance.UnzoomCard();
		Singleton<BattleHudController>.Instance.CloseLeaderPopupIfShowing(showHands: true);
		Singleton<DWBattleLane>.Instance.EndFreeCam();
		yield return StartCoroutine(Singleton<BattleHudController>.Instance.TimeUpTween.PlayAsCoroutine());
		PlayerState playerState = Singleton<DWGame>.Instance.CurrentBoardState.GetPlayerState(PlayerType.User);
		if (playerState.GetCreatureCount() == 0 && playerState.CanDeploy())
		{
			List<CardPrefabScript> creatureCards = Singleton<HandCardController>.Instance.GetCreatureCards();
			CardPrefabScript cardPrefabScript = creatureCards[UnityEngine.Random.Range(0, creatureCards.Count)];
			DeployCreatureFromCard(PlayerType.User, cardPrefabScript.Creature, 0);
			yield return StartCoroutine(cardPrefabScript.ShowPlayAnim());
			while (!Singleton<DWGameMessageHandler>.Instance.IsEffectDone())
			{
				yield return null;
			}
		}
		if (!Singleton<DWGame>.Instance.InDeploymentPhase())
		{
			Singleton<DWGame>.Instance.EndPlayerTurn();
		}
		Singleton<BattleHudController>.Instance.ClearPvpTimer(timeUp: true);
		UICamera.UnlockInput();
	}

	private void UpdateGameState()
	{
		if (mStateSwitchInputLock)
		{
			UICamera.UnlockInput();
			mStateSwitchInputLock = false;
		}
		switch (mGameState)
		{
		case GameState.LoadingData:
			if (SessionManager.Instance.IsLoadDataDone() && Singleton<PlayerInfoScript>.Instance.IsInitialized)
			{
				SetGameState(GameState.LoadingLevel);
			}
			break;
		case GameState.LoadingLevel:
			StartCoroutine(LoadLevel());
			SetGameState(GameState.WaitForLevelLoad);
			break;
		case GameState.Intro:
			if (LoadingScreenController.GetInstance() != null)
			{
				LoadingScreenController.GetInstance().HideLoading(OnHideLoadingFinished);
			}
			DetachedSingleton<MissionManager>.Instance.ResetBattle();
			Singleton<AIManager>.Instance.ResetCreatureIndex();
			Singleton<BattleIntroController>.Instance.LaunchBattleIntro();
			SetGameState(GameState.WaitForIntro);
			Singleton<CharacterAnimController>.Instance.StopCharacterFidgets();
			if (!Singleton<PlayerInfoScript>.Instance.StateData.MultiplayerMode)
			{
				Singleton<PlayerInfoScript>.Instance.Save();
			}
			break;
		case GameState.WaitingResultSequence:
			if (!Singleton<PvpBattleResultsController>.Instance.Showing)
			{
				SetGameState(GameState.Waiting);
			}
			break;
		case GameState.FirstTurnCoinFlip:
		{
			SetGameState(GameState.Waiting);
			UICamera.LockInput();
			Singleton<DWBattleLane>.Instance.RepositionLaneObjects();
			Singleton<BattleHudController>.Instance.ShowTrayTween.Play();
			Singleton<PauseController>.Instance.ShowButton();
			Singleton<QuickMessageController>.Instance.ShowButton();
			bool multiplayerMode = Singleton<PlayerInfoScript>.Instance.StateData.MultiplayerMode;
			QuestData currentActiveQuest = Singleton<PlayerInfoScript>.Instance.StateData.CurrentActiveQuest;
			if (Singleton<TutorialController>.Instance.IsFTUETutorialActive())
			{
				mUserWonCoinFlip = true;
			}
			else if (!multiplayerMode && currentActiveQuest.ForceFirstPlayer == 0)
			{
				mUserWonCoinFlip = true;
			}
			else if (!multiplayerMode && currentActiveQuest.ForceFirstPlayer == 1)
			{
				mUserWonCoinFlip = false;
			}
			else if (multiplayerMode)
			{
				mUserWonCoinFlip = Singleton<PlayerInfoScript>.Instance.PvPData.WonInitialCoinFlip;
			}
			else
			{
				mUserWonCoinFlip = UnityEngine.Random.Range(0, 2) == 0;
			}
			if (mUserWonCoinFlip)
			{
				CurrentBoardState.GetPlayerState(PlayerType.Opponent).IsCoinflipLoser = true;
			}
			else
			{
				CurrentBoardState.GetPlayerState(PlayerType.User).IsCoinflipLoser = true;
			}
			Singleton<DWGameCamera>.Instance.MoveCameraToP1Setup();
			if (mUserWonCoinFlip)
			{
				Singleton<BattleHudController>.Instance.PlayerWonCoinFlipTween.PlayWithCallback(OnCoinFlipTweenFinished);
			}
			else
			{
				Singleton<BattleHudController>.Instance.PlayerLostCoinFlipTween.PlayWithCallback(OnCoinFlipTweenFinished);
			}
			Singleton<DWBattleLane>.Instance.ShowEnvVFXObj(toogleEnvVFX: false);
			break;
		}
		case GameState.DealCreatureCards:
			UICamera.LockInput();
			Singleton<DWGameCamera>.Instance.RenderP1Character(render: false);
			foreach (InventorySlotItem item in UserLoadout.CreatureSet.FindAll((InventorySlotItem m) => m != null))
			{
				Singleton<HandCardController>.Instance.ShowCreatureCardDraw(PlayerType.User, item.Creature);
			}
			if (!mRevivingPlayerThisTurn)
			{
				foreach (InventorySlotItem item2 in OpLoadout.CreatureSet.FindAll((InventorySlotItem m) => m != null))
				{
					Singleton<HandCardController>.Instance.ShowCreatureCardDraw(PlayerType.Opponent, item2.Creature);
				}
			}
			SetGameState(GameState.DealCreatureCardsWait);
			break;
		case GameState.DealCreatureCardsWait:
			if (Singleton<HandCardController>.Instance.CardEventsInProgress())
			{
				break;
			}
			UICamera.UnlockInput();
			Singleton<DWGameCamera>.Instance.RenderP1Character(render: true);
			if (mRevivingPlayerThisTurn)
			{
				if (LostDuringMyTurn)
				{
					MasterBoardState.GetPlayerState(PlayerType.User).RefillActionPoints();
					SetGameState(GameState.P1StartTurn);
					LostDuringMyTurn = false;
				}
				else
				{
					SetGameState(GameState.P2EndTurn);
				}
			}
			else
			{
				battleStarted = true;
				if (mUserWonCoinFlip)
				{
					SetGameState(GameState.P1StartTurn);
				}
				else
				{
					SetGameState(GameState.P2StartTurn);
				}
			}
			break;
		case GameState.P1StartTurn:
			UICamera.LockInput();
			Singleton<PauseController>.Instance.BlockInput(blocked: false);
			if (MasterBoardState.IsFirstTurn() && Singleton<TutorialController>.Instance.IsFTUETutorialActive())
			{
				Singleton<TutorialController>.Instance.AdvanceTutorialState();
			}
			Singleton<DWBattleLane>.Instance.SetLaneColliders(enable: true);
			if (InDeploymentPhase())
			{
				Singleton<BattleHudController>.Instance.OnStartP1Turn();
			}
			Singleton<CharacterAnimController>.Instance.TriggerHeroThinking2(PlayerType.User);
			if (Singleton<PlayerInfoScript>.Instance.StateData.MultiplayerMode)
			{
				mMultiplayerTimeLeft = (int)CurrentBoardState.GetPlayerState(PlayerType.User).CurrentPvpTimeLimit;
			}
			StartTurn(PlayerType.User);
			SetGameState(GameState.P1Turn);
			break;
		case GameState.P1Turn:
			if (Singleton<PlayerInfoScript>.Instance.StateData.MultiplayerMode)
			{
				Singleton<MultiplayerMessageHandler>.Instance.CheckPlayerLeft();
			}
			break;
		case GameState.P1EndTurn:
			mRevivingPlayerThisTurn = false;
			if (!InDeploymentPhase())
			{
				if (Singleton<PlayerInfoScript>.Instance.StateData.MultiplayerMode)
				{
					Singleton<MultiplayerMessageHandler>.Instance.SendEndTurn();
				}
				Singleton<BattleHudController>.Instance.EndTurnTween.Play();
			}
			EndTurn(PlayerType.User);
			SetGameState(GameState.P2StartTurn);
			break;
		case GameState.P2StartTurn:
			Singleton<PauseController>.Instance.BlockInput(blocked: true);
			if (InDeploymentPhase())
			{
				Singleton<BattleHudController>.Instance.OnStartP2Turn();
			}
			StartTurn(PlayerType.Opponent);
			if (!InDeploymentPhase())
			{
				Singleton<CharacterAnimController>.Instance.TriggerHeroThinking2(PlayerType.Opponent);
			}
			if (!Singleton<PlayerInfoScript>.Instance.StateData.MultiplayerMode)
			{
				if (Singleton<TutorialController>.Instance.OverrideAIInCurrentState())
				{
					Singleton<TutorialController>.Instance.BuildAIPlan();
				}
				else
				{
					Singleton<AIManager>.Instance.StartPlanning(PlayerType.Opponent);
				}
			}
			StartCoroutine(Singleton<DWBattleLane>.Instance.ReadP2Actions());
			SetGameState(GameState.P2Turn);
			break;
		case GameState.P2Turn:
			if (Singleton<PlayerInfoScript>.Instance.StateData.MultiplayerMode)
			{
				Singleton<MultiplayerMessageHandler>.Instance.CheckPlayerLeft();
			}
			break;
		case GameState.P2EndTurn:
			EndTurn(PlayerType.Opponent, IsFirstTurnAfterRevive());
			SetGameState(GameState.P1StartTurn);
			break;
		case GameState.P2Defeated:
			Singleton<DWBattleLane>.Instance.ShowEnvVFXObj(toogleEnvVFX: true);
			battleStarted = false;
			if (Singleton<PlayerInfoScript>.Instance.StateData.MultiplayerMode)
			{
				mMultiplayerTimeLeft = -1f;
				Singleton<BattleHudController>.Instance.ClearPvpTimer(timeUp: false);
			}
			Singleton<DWGameCamera>.Instance.RenderP1Character(render: true);
			Singleton<CharacterAnimController>.Instance.StopCharacterFidgets();
			Singleton<BattleHudController>.Instance.OnGameEnd();
			Singleton<HandCardController>.Instance.HideHand();
			DetachedSingleton<ConditionalTutorialController>.Instance.EndConditionalBlock();
			SetGameState(GameState.P2DefeatedWaiting);
			StartCoroutine(SetGameStateWithDelay(GameState.PlayerVictory, 0.4f));
			break;
		case GameState.P1Defeated:
			Singleton<DWBattleLane>.Instance.ShowEnvVFXObj(toogleEnvVFX: true);
			battleStarted = false;
			if (Singleton<PlayerInfoScript>.Instance.StateData.MultiplayerMode)
			{
				mMultiplayerTimeLeft = -1f;
				Singleton<BattleHudController>.Instance.ClearPvpTimer(timeUp: false);
			}
			Singleton<DWBattleLane>.Instance.PlayDefeatedAnim(PlayerType.User);
			Singleton<DWGameCamera>.Instance.RenderP1Character(render: true);
			Singleton<DWGameCamera>.Instance.MoveCameraToP1Defeated();
			Singleton<CharacterAnimController>.Instance.StopCharacterFidgets();
			Singleton<BattleHudController>.Instance.OnGameEnd();
			Singleton<HandCardController>.Instance.HideHand();
			Singleton<BattleIntroController>.Instance.PlayLoserBanner();
			Singleton<SLOTAudioManager>.Instance.TriggerVOEvent(UserLoadout.Leader.Form, VOEvent.Lose);
			SetGameState(GameState.P1DefeatedWaiting);
			break;
		case GameState.PlayerVictory:
			Singleton<DWBattleLane>.Instance.ShowEnvVFXObj(toogleEnvVFX: true);
			Singleton<PauseController>.Instance.BlockInput(blocked: false);
			if (Singleton<PlayerInfoScript>.Instance.StateData.MultiplayerMode)
			{
				TryStopPvpReplayCapture(playerWon: true);
				SetGameState(GameState.WaitingResultSequence);
				Singleton<PvPFinishController>.Instance.Finish(win: true);
				Singleton<PvpBattleResultsController>.Instance.ShowResultsSequence();
			}
			else
			{
				SetGameState(GameState.Waiting);
				Singleton<BattleIntroController>.Instance.PlayWinnerCamera(PlayerType.User);
			}
			Singleton<SLOTAudioManager>.Instance.TriggerVOEvent(UserLoadout.Leader.Form, VOEvent.Win);
			if (Singleton<TutorialController>.Instance.IsBlockActive("Q1"))
			{
				Singleton<TutorialController>.Instance.AdvanceTutorialState();
			}
			break;
		case GameState.EnemyVictory:
			Singleton<DWBattleLane>.Instance.ShowEnvVFXObj(toogleEnvVFX: true);
			Singleton<PauseController>.Instance.BlockInput(blocked: false);
			if (Singleton<PlayerInfoScript>.Instance.StateData.MultiplayerMode)
			{
				TryStopPvpReplayCapture(playerWon: false);
				Singleton<PvPFinishController>.Instance.Finish(win: false);
			}
			Singleton<DWBattleLane>.Instance.PlayWinnerAnim(PlayerType.Opponent);
			Singleton<DWGameCamera>.Instance.MoveCameraToP2Winner();
			if (Singleton<PlayerInfoScript>.Instance.StateData.MultiplayerMode)
			{
				SetGameState(GameState.WaitingResultSequence);
				Singleton<PvpBattleResultsController>.Instance.ShowResultsSequence();
			}
			else
			{
				SetGameState(GameState.Waiting);
				Singleton<BattleResultsFailController>.Instance.Show();
			}
			break;
		case GameState.RevivePlayer:
			SetGameState(GameState.Waiting);
			mRevivingPlayerThisTurn = true;
			Singleton<HandCardController>.Instance.ClearPlayerHand();
			StartCoroutine(Singleton<SLOTMusic>.Instance.PlayBattleMusic());
			WaitingForCreatureDeployAfterRevive = true;
			InitPlayer(PlayerType.User, UserLoadout, reviving: true);
			MasterBoardState.GetPlayerState(PlayerType.User).Setup();
			StartCoroutine(ShowRevive());
			Singleton<DWBattleLane>.Instance.ShowEnvVFXObj(toogleEnvVFX: false);
			break;
		case GameState.WaitForLevelLoad:
		case GameState.WaitForIntro:
		case GameState.Waiting:
		case GameState.LootCollect:
		case GameState.EndGameWait:
		case GameState.P1DefeatedWaiting:
		case GameState.P2DefeatedWaiting:
			break;
		}
	}

	public void SkipCoinFlipForIntroBattle()
	{
		Singleton<TutorialController>.Instance.AdvanceTutorialState();
		Singleton<DWBattleLane>.Instance.RepositionLaneObjects();
		Singleton<DWGameCamera>.Instance.RenderP1Character(render: false);
		Singleton<DWGameCamera>.Instance.MoveCameraToP2Setup();
		mUserWonCoinFlip = true;
		SetGameState(GameState.Waiting);
	}

	private void OnHideLoadingFinished()
	{
		if (Singleton<TutorialController>.Instance.IsBlockActive("IntroBattle"))
		{
			Singleton<TutorialController>.Instance.AdvanceTutorialState();
		}
	}

	private IEnumerator ShowRevive()
	{
		Singleton<CharacterAnimController>.Instance.PlayHeroAnim(PlayerType.User, CharAnimType.Revive);
		BattleCharacterAnimState animState = Singleton<CharacterAnimController>.Instance.playerAnimState[PlayerType.User];
		bool isDone = false;
		float timePast = 0f;
		while (!isDone)
		{
			timePast += Time.deltaTime;
			if (animState.GetCurrentAnimType() == CharAnimType.Revive)
			{
				if (animState.IsCurrentAnimDone())
				{
					isDone = true;
				}
				else
				{
					yield return null;
				}
			}
			else if (timePast >= 2.1f)
			{
				isDone = true;
			}
			else
			{
				yield return null;
			}
		}
		Singleton<BattleHudController>.Instance.ShowTrayTween.Play();
		Singleton<PauseController>.Instance.ShowButton();
		Singleton<QuickMessageController>.Instance.ShowButton();
		Singleton<HandCardController>.Instance.ShowHand();
		Singleton<HandCardController>.Instance.ShowOpponentTween.Play();
		Singleton<CharacterAnimController>.Instance.HideHandCards(hide: false);
		Singleton<CharacterAnimController>.Instance.ForceIdleForBoth();
		Singleton<CharacterAnimController>.Instance.StopCharacterFidgets(stop: false);
		Singleton<DWGameCamera>.Instance.MoveCameraToP1Setup();
		yield return new WaitForSeconds(0.5f);
		SetGameState(GameState.DealCreatureCards);
	}

	public GameState GetCurrentGameState()
	{
		return mGameState;
	}

	private IEnumerator SetGameStateWithDelay(GameState state, float delay)
	{
		yield return new WaitForSeconds(delay);
		SetGameState(state);
	}

	public void SetGameState(GameState state)
	{
		if (!CurrentBoardState.IsDeployment)
		{
			switch (state)
			{
			case GameState.P1StartTurn:
				turnNumber++;
				turnDuration = 0f;
				turnActions.Clear();
				turnStarted = true;
				break;
			case GameState.P1EndTurn:
				turnStarted = false;
				break;
			case GameState.P1Defeated:
			case GameState.P2Defeated:
				if (Singleton<PlayerInfoScript>.Instance.StateData.MultiplayerMode && turnStarted)
				{
					turnStarted = false;
					turnDuration = 0f;
					turnActions.Clear();
				}
				break;
			}
		}
		mGameState = state;
		Singleton<SLOTAudioManager>.Instance.SetVOEventCooldown(VOEvent.Idle);
		if (!mStateSwitchInputLock)
		{
			mStateSwitchInputLock = true;
			UICamera.LockInput();
		}
	}

	private void OnCoinFlipTweenFinished()
	{
		if (mGameState != GameState.P1Defeated && mGameState != GameState.P1DefeatedWaiting)
		{
			SetGameState(GameState.DealCreatureCards);
		}
	}

	public bool IsGameOver()
	{
		if (mGameState != GameState.P1Defeated && mGameState != GameState.P2Defeated && mGameState != GameState.P1DefeatedWaiting)
		{
			return mGameState == GameState.P2DefeatedWaiting;
		}
		return true;
	}

	public void RemoveCardFromHand(PlayerType player, CardData card)
	{
		MasterBoardState.RemoveCardFromHand(player, card);
		_ = PlayerType.User;
	}

	public void DiscardCard(PlayerType player, CardData card)
	{
		MasterBoardState.DiscardCard(player, card);
	}

	public void DiscardHand(PlayerType player)
	{
		MasterBoardState.DiscardHand(player);
	}

	public void MoveCardFromDiscardToDraw(PlayerType player)
	{
		MasterBoardState.MoveCardFromDiscardToDraw(player);
	}

	public void Reshuffle(PlayerType player)
	{
		MasterBoardState.Reshuffle(player);
	}

	public void DrawHeroCard(PlayerType player)
	{
		MasterBoardState.DrawHeroCard(player);
	}

	public void DrawCreatureCard(PlayerType player, int lane)
	{
		MasterBoardState.DrawCreatureCard(player, lane);
	}

	public void DragAttack(PlayerType player, int attackLane, int targetLane)
	{
		MasterBoardState.DragAttack(player, attackLane, targetLane);
	}

	public void DoResultBleed(PlayerType player)
	{
	}

	public void EndTurn(PlayerType player, bool reviving = false)
	{
		if (KFFLODManager.IsLowEndDevice())
		{
			Resources.UnloadUnusedAssets();
		}
		MasterBoardState.EndTurn(player, reviving);
	}

	public bool HasLegalMove(PlayerType player)
	{
		return MasterBoardState.HasLegalPlay(player);
	}

	public bool IsDrawPileEmpty(PlayerType idx)
	{
		return MasterBoardState.IsDrawPileEmpty(idx);
	}

	public bool IsDiscardPileEmpty(PlayerType idx)
	{
		return MasterBoardState.IsDiscardPileEmpty(idx);
	}

	public bool IsMarkedForDeath(PlayerType player, int lane)
	{
		if (MasterBoardState.GetCreature(player, lane) == null)
		{
			return true;
		}
		return false;
	}

	public LeaderData GetCharacter(PlayerType player)
	{
		return GetLeader(player).SelectedSkin;
	}

	public int GetActionPoints(PlayerType idx)
	{
		return MasterBoardState.GetActionPoints(idx);
	}

	public LaneState GetLaneState(PlayerType idx, int LaneIndex)
	{
		return MasterBoardState.GetLaneState(idx, LaneIndex);
	}

	public CreatureState GetCreature(PlayerType idx, int LaneIndex)
	{
		return MasterBoardState.GetCreature(idx, LaneIndex);
	}

	public List<CreatureState> GetCreatures(PlayerType idx)
	{
		return MasterBoardState.GetCreatures(idx);
	}

	public List<LaneState> GetLanes(PlayerType idx)
	{
		return MasterBoardState.GetLanes(idx);
	}

	public List<CardData> GetHand(PlayerType idx)
	{
		return MasterBoardState.GetHand(idx);
	}

	public List<CardData> GetDrawPile(PlayerType idx)
	{
		return MasterBoardState.GetDrawPile(idx);
	}

	public List<CardData> GetDiscardPile(PlayerType idx)
	{
		return MasterBoardState.GetDiscardPile(idx);
	}

	public LeaderItem GetLeader(PlayerType idx)
	{
		return MasterBoardState.GetLeader(idx);
	}

	public int GetHandCount(PlayerType idx)
	{
		return MasterBoardState.GetHandCount(idx);
	}

	public bool IsCreatureTargetRestricted(CreatureState creature)
	{
		List<CreatureState> list = GetCreatures(creature.Owner.Type).FindAll((CreatureState m) => m.HasBravery);
		if (list.Count > 0 && !list.Contains(creature))
		{
			return true;
		}
		return false;
	}

	private IEnumerator CreateCharacters()
	{
		yield return null;
		Singleton<CharacterAnimController>.Instance.ResetPlayers();
		_ = Singleton<PlayerInfoScript>.Instance.StateData.CurrentActiveQuest;
		for (int i = 0; i < 2; i++)
		{
			LeaderData CurrentCharacter = GetCharacter(i);
			GameObject NewCharacter = null;
			Transform tr = ((i != 0) ? Singleton<DWBattleLane>.Instance.P2CharacterPos : Singleton<DWBattleLane>.Instance.P1CharacterPos);
			yield return StartCoroutine(Singleton<SLOTResourceManager>.Instance.LoadLeaderResources(CurrentCharacter, delegate(GameObject loadedObjData)
			{
				GameObject gameObject2 = ((loadedObjData != null && loadedObjData.GetComponent<Animator>() != null && loadedObjData.GetComponent<BattleCharacterAnimState>() != null) ? loadedObjData : (SLOTResourceManager.Load("Characters/" + CurrentCharacter.Prefab + "/" + CurrentCharacter.Prefab, typeof(GameObject)) as GameObject));
				NewCharacter = ((gameObject2 != null) ? UnityEngine.Object.Instantiate(gameObject2, tr.position, tr.rotation) : null);
			}));
			bool flag = NewCharacter != null && (NewCharacter.GetComponent<Animator>() == null || NewCharacter.GetComponent<BattleCharacterAnimState>() == null);
			if (NewCharacter == null || flag)
			{
				Debug.LogError("[Battle] leader model '" + ((CurrentCharacter != null) ? CurrentCharacter.Prefab : "?") + "' " + (flag ? "loaded without a usable rig (Animator/AnimState)" : "failed to load") + "; using the Finn stand-in so the match does not freeze.");
				if (NewCharacter != null)
				{
					UnityEngine.Object.Destroy(NewCharacter);
					NewCharacter = null;
				}
				GameObject gameObject = SLOTResourceManager.Load("Characters/Finn/Finn", typeof(GameObject)) as GameObject;
				if (gameObject != null)
				{
					NewCharacter = UnityEngine.Object.Instantiate(gameObject, tr.position, tr.rotation);
				}
			}
			if (NewCharacter == null)
			{
				Debug.LogError("[Battle] fallback leader model also failed to load; skipping character " + i + " so the match still starts.");
				continue;
			}
			OffsetCharacterObject(offsetX: (i != 0) ? (0f - CurrentCharacter.CharacterOffsetX) : CurrentCharacter.CharacterOffsetX, offsetY: CurrentCharacter.CharacterOffsetY, tr: tr, chair: NewCharacter.transform);
			Singleton<DWBattleLane>.Instance.StoreCharacterObj(i, NewCharacter);
			if (i == 1)
			{
				NewCharacter.ChangeLayer(LayerMask.NameToLayer("PIP2"));
				Singleton<DWGameCamera>.Instance.ShowOpponentCharacter();
			}
			Debug.Log("[PvPDiag] hero setup i=" + i + " prefab=" + ((CurrentCharacter != null) ? CurrentCharacter.Prefab : "?") + " layer=" + NewCharacter.layer + " (" + LayerMask.LayerToName(NewCharacter.layer) + ") active=" + NewCharacter.activeInHierarchy + " pos=" + NewCharacter.transform.position.ToString());
			Animator component = NewCharacter.GetComponent<Animator>();
			Singleton<CharacterAnimController>.Instance.AddPlayer(component);
			Singleton<CharacterAnimController>.Instance.playerData[i] = CurrentCharacter;
			BattleCharacterAnimState component2 = NewCharacter.GetComponent<BattleCharacterAnimState>();
			component2.player = (PlayerType)i;
			component2.Init(CurrentCharacter);
			string text = ((i != (int)PlayerType.User) ? "Chair_P2" : "Chair_P1");
			Transform chairObj = Singleton<DWBattleLane>.Instance.GetChairObj(text);
			if (CurrentCharacter.UseChair)
			{
				OffsetCharacterObject(offsetX: (i != (int)PlayerType.User) ? (0f - CurrentCharacter.ChairOffsetX) : CurrentCharacter.ChairOffsetX, offsetY: CurrentCharacter.ChairOffsetY, tr: tr, chair: chairObj.transform);
				Transform[] componentsInChildren = chairObj.GetComponentsInChildren<Transform>();
				foreach (Transform transform in componentsInChildren)
				{
					if (transform.name == "Chair_Shadow")
					{
						transform.position = new Vector3(transform.position.x, -49f, transform.position.z);
					}
				}
			}
			else
			{
				chairObj.gameObject.SetActive(value: false);
			}
		}
		Singleton<CharacterAnimController>.Instance.SetupCharacters();
		yield return null;
	}

	private void OffsetCharacterObject(Transform tr, Transform chair, float offsetX, float offsetY)
	{
		Vector3 position = new Vector3(tr.position.x + offsetX, tr.position.y + offsetY, 0f);
		chair.position = position;
		chair.rotation = tr.rotation;
	}

	private GameObject SpawnCharacterObject(Transform tr, string prefabName, float offsetX, float offsetY)
	{
		GameObject gameObject = null;
		Vector3 position = new Vector3(tr.position.x + offsetX, tr.position.y + offsetY, 0f);
		UnityEngine.Object obj = SLOTResourceManager.Load(prefabName);
		if (obj != null)
		{
			gameObject = SLOTGame.InstantiateFX(obj, position, tr.rotation);
			gameObject.transform.parent = tr;
		}
		return gameObject;
	}

	private IEnumerator LoadLevel()
	{
		yield return null;
		Setup();
		yield return null;
		PlayerInfoScript instance = Singleton<PlayerInfoScript>.Instance;
		QuestData qData = instance.StateData.CurrentActiveQuest;
		DetachedSingleton<CustomAIManager>.Instance.ParseAIData(qData.CustomAI);
		int num = 0;
		foreach (InventorySlotItem item in UserLoadout.CreatureSet)
		{
			if (item != null)
			{
				num++;
			}
		}
		foreach (InventorySlotItem item2 in OpLoadout.CreatureSet)
		{
			if (item2 != null)
			{
				num++;
			}
		}
		int totalBundles = 2 * num + 4;
		Singleton<SLOTResourceManager>.Instance.StartResourceLoadProgress(totalBundles);
		bool inIntroBattle = Singleton<TutorialController>.Instance.IsBlockActive("IntroBattle");
		yield return StartCoroutine(Singleton<SLOTResourceManager>.Instance.LoadEnvironmentResources(qData, delegate
		{
			GameObject gameObject = SLOTResourceManager.Load("Environment/" + qData.LevelPrefab + "/" + qData.LevelPrefab, typeof(GameObject)) as GameObject;
			if (gameObject != null)
			{
				GameObject gameObject2 = UnityEngine.Object.Instantiate(gameObject);
				gameObject2.transform.parent = Singleton<DWBattleLane>.Instance.transform;
				Singleton<DWBattleLane>.Instance.EnvironmentObj = gameObject2;
			}
			else
			{
				Debug.LogWarning("[Battle] environment '" + qData.LevelPrefab + "' failed to load; continuing without a backdrop so the match still starts.");
			}
		}));
		yield return StartCoroutine(Singleton<SLOTResourceManager>.Instance.LoadGameBoardResources(qData, delegate
		{
			GameObject gameObject = SLOTResourceManager.Load("GameBoard/" + qData.BoardPrefab + "/" + qData.BoardPrefab, typeof(GameObject)) as GameObject;
			if (gameObject != null)
			{
				GameObject gameObject2 = UnityEngine.Object.Instantiate(gameObject);
				gameObject2.transform.parent = Singleton<DWBattleLane>.Instance.transform;
				Singleton<DWBattleLane>.Instance.BoardObj = gameObject2;
				if (!inIntroBattle)
				{
					gameObject2.SetActive(value: false);
				}
			}
			else
			{
				Debug.LogWarning("[Battle] game board '" + qData.BoardPrefab + "' failed to load; continuing without it so the match still starts.");
			}
		}));
		yield return StartCoroutine(CreateCharacters());
		yield return StartCoroutine(PoolCreatureObjects());
		yield return StartCoroutine(Singleton<SLOTMusic>.Instance.PlayBattleMusic());
		if (inIntroBattle)
		{
			yield return StartCoroutine(SetupIntroBattleBoard());
		}
		if (inIntroBattle)
		{
			Singleton<SLOTResourceManager>.Instance.StartAssetBundlePreload(SLOTResourceManager.PreloadBundlesPoint.IntroBattle);
		}
		else if (Singleton<TutorialController>.Instance.IsBlockActive("Q1"))
		{
			Singleton<SLOTResourceManager>.Instance.StartAssetBundlePreload(SLOTResourceManager.PreloadBundlesPoint.Q1);
		}
		else if (Singleton<TutorialController>.Instance.IsBlockActive("Q2"))
		{
			Singleton<SLOTResourceManager>.Instance.StartAssetBundlePreload(SLOTResourceManager.PreloadBundlesPoint.Q2);
		}
		if (!inIntroBattle)
		{
			SetGameState(GameState.Intro);
			StartCoroutine(Singleton<DWBattleLane>.Instance.ShowBoardAnim());
		}
		else
		{
			Singleton<DWBattleLane>.Instance.BoardHologram.SetActive(value: false);
		}
	}

	public IEnumerator RestoreCreatureObjectsPool()
	{
		List<CreatureItem> toRepool = new List<CreatureItem>();
		foreach (CreatureItem key in Singleton<DWBattleLane>.Instance.CreaturePool.Keys)
		{
			if (Singleton<DWBattleLane>.Instance.CreaturePool[key] == null)
			{
				toRepool.Add(key);
			}
		}
		for (int i = 0; i < toRepool.Count; i++)
		{
			Singleton<DWBattleLane>.Instance.CreaturePool.Remove(toRepool[i]);
			yield return StartCoroutine(PoolCreatureData(toRepool[i]));
		}
		yield return null;
	}

	private IEnumerator PoolCreatureObjects()
	{
		Singleton<DWBattleLane>.Instance.PoolLaneObjects();
		yield return null;
		foreach (InventorySlotItem item in UserLoadout.CreatureSet)
		{
			if (item != null)
			{
				yield return StartCoroutine(PoolCreatureData(item.Creature));
			}
		}
		yield return null;
		foreach (InventorySlotItem item2 in OpLoadout.CreatureSet)
		{
			if (item2 != null)
			{
				yield return StartCoroutine(PoolCreatureData(item2.Creature));
			}
		}
		yield return null;
	}

	private IEnumerator PoolCreatureData(CreatureItem creature)
	{
		if (creature == null)
		{
			yield break;
		}
		UnityEngine.Object objData = null;
		Texture2D tex = null;
		yield return StartCoroutine(Singleton<SLOTResourceManager>.Instance.LoadCreatureResources(creature.Form, delegate(GameObject loadedObjData, Texture2D loadedTexture)
		{
			objData = loadedObjData;
			tex = loadedTexture;
		}));
		GameObject gameObject = SLOTResourceManager.Load("Creatures/" + creature.Form.Prefab + "/" + creature.Form.Prefab, typeof(GameObject)) as GameObject;
		if (gameObject == null)
		{
			Debug.LogWarning("[Battle] creature '" + creature.Form.Prefab + "' failed to load; skipping its model so the match still starts.");
			yield break;
		}
		GameObject gameObject2 = UnityEngine.Object.Instantiate(gameObject);
		Texture2D mainTexture = SLOTResourceManager.Load("Creatures/" + creature.Form.Prefab + "/Textures/" + creature.Faction.ToString() + "/" + creature.Form.PrefabTexture, typeof(Texture2D)) as Texture2D;
		Singleton<DWBattleLane>.Instance.CreaturePool.Add(creature, gameObject2);
		if (tex != null)
		{
			Renderer[] componentsInChildren = gameObject2.GetComponentsInChildren<Renderer>(includeInactive: true);
			for (int num = 0; num < componentsInChildren.Length; num++)
			{
				componentsInChildren[num].material.mainTexture = mainTexture;
			}
		}
		if (gameObject2 != null)
		{
			gameObject2.GetComponentInChildren<Animator>().StartPlayback();
			gameObject2.GetComponentInChildren<Animator>().StopPlayback();
			gameObject2.SetActive(value: false);
		}
		if (!Singleton<DWBattleLane>.Instance.CreatureVFXPool.ContainsKey(creature.Form.HitVFX))
		{
			PoolCreatureVFX(creature.Form.HitVFX);
		}
		if (!Singleton<DWBattleLane>.Instance.CreatureVFXPool.ContainsKey(creature.Form.ShootVFX))
		{
			PoolCreatureVFX(creature.Form.ShootVFX);
		}
		if (!Singleton<DWBattleLane>.Instance.CreatureVFXPool.ContainsKey(creature.Form.WeaponTrailVFX))
		{
			PoolCreatureVFX(creature.Form.WeaponTrailVFX);
		}
		if (!Singleton<DWBattleLane>.Instance.CreatureVFXPool.ContainsKey(creature.Form.AttackChargeVFX))
		{
			PoolCreatureVFX(creature.Form.AttackChargeVFX);
		}
		if (!Singleton<DWBattleLane>.Instance.CreatureVFXPool.ContainsKey(creature.Form.CritHitVFX))
		{
			PoolCreatureVFX(creature.Form.CritHitVFX);
		}
		if (!Singleton<DWBattleLane>.Instance.CreatureVFXPool.ContainsKey(creature.Form.PersistentVFX))
		{
			PoolCreatureVFX(creature.Form.PersistentVFX);
		}
		if (!Singleton<DWBattleLane>.Instance.CreatureVFXPool.ContainsKey(creature.Form.RezInVFX))
		{
			PoolCreatureVFX(creature.Form.RezInVFX);
		}
	}

	public void PoolCreatureVFX(string path)
	{
		if (string.IsNullOrEmpty(path))
		{
			return;
		}
		Dictionary<string, GameObject> creatureVFXPool = Singleton<DWBattleLane>.Instance.CreatureVFXPool;
		if (creatureVFXPool.ContainsKey(path))
		{
			return;
		}
		GameObject gameObject = Singleton<SLOTResourceManager>.Instance.LoadResource("VFX/Creatures/" + path) as GameObject;
		if (!(gameObject == null))
		{
			creatureVFXPool.Add(path, gameObject);
			GameObject obj = UnityEngine.Object.Instantiate(gameObject);
			obj.SetActive(value: false);
			ParticleSystem[] componentsInChildren = obj.GetComponentsInChildren<ParticleSystem>();
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				componentsInChildren[i].Simulate(0f, withChildren: true, restart: true);
			}
		}
	}

	public void EndPlayerTurn()
	{
		if (Singleton<PlayerInfoScript>.Instance.StateData.MultiplayerMode)
		{
			mMultiplayerTimeLeft = -1f;
		}
		SetGameState(GameState.P1EndTurn);
	}

	public void RevivePlayer()
	{
		StartCoroutine(RestoreCreatureObjectsPool());
		SetGameState(GameState.RevivePlayer);
	}

	public bool FindKeywordForTutorial(string keywordIDString, out CardPrefabScript foundCard, out StatusIconItem foundStatusIcon)
	{
		foundCard = null;
		foundStatusIcon = null;
		string[] array = keywordIDString.Split('+');
		foreach (CardPrefabScript handCard in Singleton<HandCardController>.Instance.GetHandCards())
		{
			if (handCard.Card != null && handCard.Card.HasKeywords(array))
			{
				foundCard = handCard;
				return true;
			}
		}
		string statusKeyword = array[^1];
		CreatureBuffBar[] componentsInChildren = Singleton<BattleHudController>.Instance.GetComponentsInChildren<CreatureBuffBar>();
		DWBattleLaneObject lane;
		foreach (DWBattleLaneObject item in Singleton<DWBattleLane>.Instance.BattleLaneObjects[0])
		{
			lane = item;
			StatusIconItem statusIconItem = componentsInChildren.Find((CreatureBuffBar m) => m.mCreature == lane.Creature).FindStatusIcon(statusKeyword);
			if (statusIconItem != null)
			{
				foundStatusIcon = statusIconItem;
				return true;
			}
		}
		return false;
	}
}
