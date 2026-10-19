using System;
using System.Collections;
using System.Collections.Generic;
using Multiplayer;
using UnityEngine;

public class TutorialController : Singleton<TutorialController>
{
	public int PopupTextPadding;

	public float PopupArrowSpacing;

	public float PopupSpacingCornerFactor;

	public float WidthRatio;

	public int MinimumWidth;

	public float GeneralDragDistance;

	public Vector2 CharacterTextureOffset;

	public float BoardYPosAdjust;

	public UITweenController ShowPopupTween;

	public UITweenController HidePopupTween;

	public UITweenController ShowArrowTween;

	public UITweenController HideArrowTween;

	public UITweenController ShowDragTween;

	public UITweenController ShowDragFadeTween;

	public UITweenController HideDragTween;

	public Transform PopupObject;

	public UILabel PopupText;

	public UILabel PopupTextTitle;

	public UILabel NameLabel;

	public UISprite NameBackground;

	public GameObject NextArrow;

	public UISprite PopupBackground;

	public Transform PointerObject;

	public GameObject ArrowPointer;

	public GameObject TapPointer;

	public GameObject FullScreenCollider;

	public Transform DragArrow;

	public UITexture CharacterTexture;

	public GameObject TapIndicator;

	public bool manulSetAutoFillMeterBar;

	[SerializeField]
	private UISprite _HexBgSprite;

	[SerializeField]
	private Color[] _BackgroundColors = new Color[2];

	private TutorialState mDisplayedState;

	private Camera mUICam;

	private Camera m3DCam;

	private int mLayerTown;

	private int mLayerCardHand;

	private int mLayerBattleBoard;

	private float mStartDelayTimer = -1f;

	private string mBaseColorString;

	private string mWildcardID;

	private string mPopupTextReplacement;

	private List<AIDecision> mAIPlan = new List<AIDecision>();

	private bool mAskingName;

	public bool AIOverridden { get; set; }

	public void SetWildcardID(string id)
	{
		mWildcardID = id;
	}

	private void Awake()
	{
		if (Singleton<TutorialController>.mInstance == null)
		{
			Singleton<TutorialController>.mInstance = this;
		}
		if (DetachedSingleton<SceneFlowManager>.Instance.InBattleScene())
		{
			base.gameObject.ChangeLayer(LayerMask.NameToLayer("TopGUI"));
		}
		Color color = PopupText.color;
		PopupText.color = Color.white;
		mBaseColorString = "[" + color.ToHexString() + "]";
	}

	private void Start()
	{
		UICamera.restrictedColliderClickDelegate = OnClickAllowedButton;
		mLayerTown = LayerMask.NameToLayer("3DFrontEnd");
		mLayerCardHand = LayerMask.NameToLayer("3DGUI");
		mLayerBattleBoard = LayerMask.NameToLayer("Default");
		if (DetachedSingleton<SceneFlowManager>.Instance.InFrontEnd())
		{
			mUICam = GameObject.Find("Camera_Main").GetComponent<Camera>();
			m3DCam = GameObject.Find("3DFrontEnd").GetComponent<Camera>();
		}
	}

	public void LoadInitialTutorialQuest()
	{
		StartTutorialBlock("IntroBattle");
		GameStateData stateData = Singleton<PlayerInfoScript>.Instance.StateData;
		stateData.CurrentActiveQuest = QuestDataManager.Instance.GetData("Tutorial1");
		stateData.CurrentLoadout = new Loadout();
		stateData.CurrentLoadout.Leader = Singleton<PlayerInfoScript>.Instance.SaveData.Leaders[0];
		stateData.tutorialBoardID = GetActiveState().tutorialBoard;
		QuestLoadoutData data = QuestLoadoutDataManager.Instance.GetData("TutorialLoadout");
		for (int i = 0; i < data.Entries.Count; i++)
		{
			if (data.Entries[i] != null)
			{
				stateData.CurrentLoadout.CreatureSet.Add(new InventorySlotItem(data.Entries[i].BuildCreatureItem()));
			}
		}
		DetachedSingleton<SceneFlowManager>.Instance.LoadBattleScene();
	}

	private XPMaterialData GetTutorialFodder(CreatureFaction faction, bool anythingButThisFaction = false)
	{
		XPMaterialData xPMaterialData = ((!anythingButThisFaction) ? XPMaterialDataManager.Instance.GetDatabase().Find((XPMaterialData m) => m.Rarity == 1 && m.Faction == faction) : XPMaterialDataManager.Instance.GetDatabase().Find((XPMaterialData m) => m.Rarity == 1 && m.Faction != faction));
		if (xPMaterialData == null)
		{
			xPMaterialData = ((!anythingButThisFaction) ? XPMaterialDataManager.Instance.GetDatabase().Find((XPMaterialData m) => m.Rarity == 2 && m.Faction == faction) : XPMaterialDataManager.Instance.GetDatabase().Find((XPMaterialData m) => m.Rarity == 2 && m.Faction != faction));
		}
		if (xPMaterialData == null)
		{
			return null;
		}
		return xPMaterialData;
	}

	public void AddCreaturesForFusion()
	{
		PlayerSaveData.BeginGrant();
		AddCreaturesForFusionLocal();
		TutorialGrantReport("fusion");
	}

	private void AddCreaturesForFusionLocal()
	{
		PlayerSaveData saveData = Singleton<PlayerInfoScript>.Instance.SaveData;
		CreatureItem creature = saveData.GetBestCreature().Creature;
		List<XPMaterialData> list = new List<XPMaterialData>();
		XPMaterialData tutorialFodder = GetTutorialFodder(creature.Faction);
		list.Add(tutorialFodder);
		saveData.AddXPMaterial(tutorialFodder).GivenForTutorial = true;
		tutorialFodder = GetTutorialFodder(creature.Faction, anythingButThisFaction: true);
		list.Add(tutorialFodder);
		saveData.AddXPMaterial(tutorialFodder).GivenForTutorial = true;
		creature.CalculateXpFusion(new List<CreatureItem>(), list, out var _, out var cost);
		saveData.SoftCurrency += cost;
	}

	public void AddMaterialsForEnhance()
	{
		PlayerSaveData.BeginGrant();
		AddMaterialsForEnhanceLocal();
		TutorialGrantReport("enhance");
	}

	private void AddMaterialsForEnhanceLocal()
	{
		PlayerSaveData saveData = Singleton<PlayerInfoScript>.Instance.SaveData;
		InventorySlotItem inventorySlotItem = saveData.GetBestCreature(notMaxStars: true);
		if (inventorySlotItem == null)
		{
			CreatureItem creatureItem = new CreatureItem(MiscParams.TutorialCreature);
			creatureItem.Xp = creatureItem.XPTable.GetXpToReachLevel(creatureItem.MaxLevel);
			inventorySlotItem = saveData.AddCreature(creatureItem);
		}
		CreatureItem creature = inventorySlotItem.Creature;
		creature.Xp = creature.XPTable.GetXpToReachLevel(creature.MaxLevel);
		saveData.SoftCurrency += creature.StarRatingData.CostToEnhance;
		foreach (EvoMaterialData item in creature.Form.GetEnhanceRecipe(creature.StarRating))
		{
			if (item != null)
			{
				saveData.AddEvoMaterial(item);
			}
		}
	}

	public void AddMaterialsForEvo()
	{
		PlayerSaveData.BeginGrant();
		AddMaterialsForEvoLocal();
		TutorialGrantReport("evo");
	}

	private void AddMaterialsForEvoLocal()
	{
		PlayerSaveData saveData = Singleton<PlayerInfoScript>.Instance.SaveData;
		CreatureItem creatureItem = new CreatureItem(MiscParams.TutorialEVOCreature);
		creatureItem.GivenForEvoTutorial = true;
		creatureItem.Xp = creatureItem.XPTable.GetXpToReachLevel(creatureItem.MaxLevel);
		saveData.AddCreature(creatureItem);
		saveData.SoftCurrency += creatureItem.Form.EvolveCost;
		for (int i = 0; i < 5; i++)
		{
			saveData.AddEvoMaterial(creatureItem.Form.AwakenMaterial);
		}
	}

	public void AddCreatureForGemcraft()
	{
	}

	public void AddCreatureAndGemForGemFuse()
	{
	}

	private static void TutorialGrantReport(string step)
	{
		string text = PlayerSaveData.EndGrant();
		if (text.Length > 0)
		{
			Singleton<PlayerInfoScript>.Instance.SaveData.ClaimCurrency("tutorial", step, 0, 0, 0, 0, null, text);
		}
	}

	public void AddCardToEquip(CreatureFaction faction)
	{
		PlayerSaveData.BeginGrant();
		AddCardToEquipLocal(faction);
		TutorialGrantReport("card");
	}

	private void AddCardToEquipLocal(CreatureFaction faction)
	{
		PlayerSaveData saveData = Singleton<PlayerInfoScript>.Instance.SaveData;
		CardData cardData = MiscParams.TutorialCards.Find((CardData m) => m.Faction == faction);
		if (cardData == null)
		{
			cardData = MiscParams.TutorialCards[0];
		}
		CardItem card = new CardItem(cardData);
		saveData.AddExCard(card);
	}

	public void AddStateObject(TutorialObject tutObject)
	{
		TutorialState data = TutorialDataManager.Instance.GetData(tutObject.TutorialState);
		if (data == null)
		{
			return;
		}
		data.ClearOutDestroyedTutorialObjects();
		if (tutObject is TutorialDragObject)
		{
			TutorialDragObject item = tutObject as TutorialDragObject;
			if (!data.DragTargetList.Contains(item))
			{
				data.DragTargetList.Add(item);
			}
		}
		else if (!data.ObjectList.Contains(tutObject))
		{
			data.ObjectList.Add(tutObject);
		}
	}

	private void OnClickAllowedButton()
	{
		if (!GetActiveState().ManualClose())
		{
			if (GetActiveState().ManualAdvance())
			{
				PassCurrentState();
			}
			else
			{
				AdvanceTutorialState();
			}
		}
	}

	public void StartTutorialBlock(string blockId)
	{
		if (GetActiveState() == null)
		{
			TutorialDataManager.TutorialBlock block = TutorialDataManager.Instance.GetBlock(blockId);
			if (block != null)
			{
				TutorialState data = TutorialDataManager.Instance.GetData(block.StartState);
				EnterTutorialState(data);
			}
		}
	}

	public void StartTutorialBlockIfNotComplete(string blockId)
	{
		if (GetActiveState() == null)
		{
			TutorialDataManager.TutorialBlock block = TutorialDataManager.Instance.GetBlock(blockId);
			if (block != null && !block.Completed)
			{
				TutorialState data = TutorialDataManager.Instance.GetData(block.StartState);
				EnterTutorialState(data);
			}
		}
	}

	private void EnterTutorialState(TutorialState state)
	{
		Singleton<PlayerInfoScript>.Instance.StateData.ActiveTutorialState = state;
		mDisplayedState = null;
		if (state == null)
		{
			return;
		}
		Singleton<SLOTAudioManager>.Instance.SetVOEventCooldown(VOEvent.Idle);
		SetColliderRestrictions(state);
		if (state.Target == TutorialState.TargetEnum.Conditionals)
		{
			state.Passed = true;
			TutorialDataManager.TutorialBlock block = TutorialDataManager.Instance.GetBlock(state.TargetId(0));
			DetachedSingleton<ConditionalTutorialController>.Instance.StartConditionalBlock(block);
		}
		else
		{
			state.Passed = false;
			AttachTutorialObjects(state);
		}
		foreach (string entryFunctionCall in state.EntryFunctionCalls)
		{
			SendMessage(entryFunctionCall);
		}
	}

	public void EnterConditionalState(TutorialState conditionalState)
	{
		Singleton<PlayerInfoScript>.Instance.StateData.ActiveConditionalState = conditionalState;
		mDisplayedState = null;
		if (conditionalState != null)
		{
			conditionalState.Passed = false;
			AttachTutorialObjects(conditionalState);
		}
	}

	public bool InConditionalState()
	{
		return Singleton<PlayerInfoScript>.Instance.StateData.ActiveConditionalState != null;
	}

	public bool InConditionalState(string stateName)
	{
		TutorialState activeConditionalState = Singleton<PlayerInfoScript>.Instance.StateData.ActiveConditionalState;
		if (activeConditionalState != null)
		{
			return activeConditionalState.ID == stateName;
		}
		return false;
	}

	private void AttachTutorialObjects(TutorialState state)
	{
		state.ClearOutDestroyedTutorialObjects();
		if (state.Target == TutorialState.TargetEnum.TapBuilding || state.Target == TutorialState.TargetEnum.PointBuilding)
		{
			GameObject buildingObject = Singleton<TownController>.Instance.GetBuildingObject(state.TargetId(0));
			if (buildingObject != null)
			{
				TutorialObject.Attach(buildingObject, state.ID);
			}
		}
		else if (state.Target == TutorialState.TargetEnum.League)
		{
			GameObject leagueObject = Singleton<QuestSelectController>.Instance.GetLeagueObject(state.TargetId(0));
			if (leagueObject != null)
			{
				TutorialObject.Attach(leagueObject, state.ID);
			}
		}
		else if (state.Target == TutorialState.TargetEnum.Quest)
		{
			GameObject questObject = Singleton<QuestSelectController>.Instance.GetQuestObject(state.TargetId(0));
			if (questObject != null)
			{
				TutorialObject.Attach(questObject, state.ID);
			}
		}
		else if (state.Target == TutorialState.TargetEnum.PointCard || state.Target == TutorialState.TargetEnum.TapCard || state.Target == TutorialState.TargetEnum.CardEnergy)
		{
			string targetID;
			if (state.TargetId(0) == "*")
			{
				targetID = mWildcardID;
			}
			else
			{
				targetID = state.TargetId(0);
			}
			CardPrefabScript cardPrefabScript = Singleton<HandCardController>.Instance.GetHandCards().Find((CardPrefabScript m) => m.Card != null && m.Card.ID == targetID);
			if (cardPrefabScript == null)
			{
				cardPrefabScript = Singleton<HandCardController>.Instance.GetHandCards().Find((CardPrefabScript m) => m.Creature != null && m.Creature.Form.Faction.ClassName() == targetID);
			}
			if (cardPrefabScript != null)
			{
				if (state.Target == TutorialState.TargetEnum.CardEnergy)
				{
					TutorialObject.Attach(cardPrefabScript.Cost.gameObject, state.ID);
				}
				else
				{
					TutorialObject.Attach(cardPrefabScript.gameObject, state.ID);
				}
			}
		}
		else if (state.Target == TutorialState.TargetEnum.PointKeyword)
		{
			if (Singleton<DWGame>.Instance.FindKeywordForTutorial(state.TargetId(0), out var foundCard, out var foundStatusIcon))
			{
				if (foundCard != null)
				{
					TutorialObject.Attach(foundCard.gameObject, state.ID);
				}
				else
				{
					TutorialObject.Attach(foundStatusIcon.gameObject, state.ID);
				}
			}
		}
		else if (state.Target == TutorialState.TargetEnum.PopupCard)
		{
			CardPrefabScript cardPrefabScript2 = Singleton<CreatureInfoPopup>.Instance.GetCards().Find((CardPrefabScript m) => m.Card != null && m.Card.ID == state.TargetId(0));
			if (cardPrefabScript2 != null)
			{
				TutorialObject.Attach(cardPrefabScript2.gameObject, state.ID);
			}
		}
		else if (state.Target == TutorialState.TargetEnum.CardPlay || state.Target == TutorialState.TargetEnum.CardPlayOnEnemy)
		{
			if (state.TargetId(0) == "Any")
			{
				foreach (CardPrefabScript handCard in Singleton<HandCardController>.Instance.GetHandCards())
				{
					if (handCard.Card != null)
					{
						TutorialObject.Attach(handCard.gameObject, state.ID);
					}
				}
			}
			else
			{
				CardPrefabScript cardPrefabScript3 = Singleton<HandCardController>.Instance.GetHandCards().Find((CardPrefabScript m) => m.Card != null && m.Card.ID == state.TargetId(0));
				if (cardPrefabScript3 != null)
				{
					TutorialObject.Attach(cardPrefabScript3.gameObject, state.ID);
				}
			}
			if (state.TargetId(1) != null)
			{
				PlayerType player = ((state.Target != TutorialState.TargetEnum.CardPlay) ? PlayerType.Opponent : PlayerType.User);
				DWBattleLaneObject dWBattleLaneObject = ((state.TargetId(1) == "LowestHealth") ? Singleton<DWBattleLane>.Instance.GetLowestHealthCreatureLane(player) : ((!(state.TargetId(1) == "Debuffed")) ? Singleton<DWBattleLane>.Instance.GetLaneObject(player, state.TargetId(1)) : Singleton<DWBattleLane>.Instance.GetDebuffedCreatureLane(player)));
				if (dWBattleLaneObject == null)
				{
					dWBattleLaneObject = Singleton<DWBattleLane>.Instance.GetLaneObject(player, (DWBattleLaneObject m) => m.Creature.StatusEffects.Find((StatusState statusState) => statusState.Data.FXData.Keyword.ID == state.TargetId(1)) != null);
				}
				if (dWBattleLaneObject != null)
				{
					TutorialDragObject.Attach(dWBattleLaneObject.gameObject, state.ID);
				}
				return;
			}
			for (int num = 0; num < 2; num++)
			{
				foreach (DWBattleLaneObject item in Singleton<DWBattleLane>.Instance.BattleLaneObjects[num])
				{
					TutorialDragObject.Attach(item.gameObject, state.ID);
				}
			}
		}
		else if (state.Target == TutorialState.TargetEnum.CreatureCardPlay)
		{
			CardPrefabScript cardPrefabScript4 = Singleton<HandCardController>.Instance.GetHandCards().Find((CardPrefabScript m) => m.Creature != null && m.Creature.Form.Faction.ClassName() == state.TargetId(0));
			if (cardPrefabScript4 != null)
			{
				TutorialObject.Attach(cardPrefabScript4.gameObject, state.ID);
			}
		}
		else if (state.Target == TutorialState.TargetEnum.PointCreature || state.Target == TutorialState.TargetEnum.TapCreature || state.Target == TutorialState.TargetEnum.TapEnemyCreature || state.Target == TutorialState.TargetEnum.PointEnemyCreature)
		{
			string creatureOrFactionId = ((!(state.TargetId(0) == "*")) ? state.TargetId(0) : mWildcardID);
			PlayerType player2 = ((state.Target != TutorialState.TargetEnum.PointCreature && state.Target != TutorialState.TargetEnum.TapCreature) ? PlayerType.Opponent : PlayerType.User);
			DWBattleLaneObject laneObject = Singleton<DWBattleLane>.Instance.GetLaneObject(player2, creatureOrFactionId);
			if (laneObject != null)
			{
				TutorialObject.Attach(laneObject.gameObject, state.ID);
			}
		}
		else if (state.Target == TutorialState.TargetEnum.EnemyCreatureStr)
		{
			DWBattleLaneObject laneObject2 = Singleton<DWBattleLane>.Instance.GetLaneObject(PlayerType.Opponent, state.TargetId(0));
			if (laneObject2 != null)
			{
				TutorialObject.Attach(laneObject2.HealthBar.MGCValue.gameObject, state.ID);
			}
		}
		else if (state.Target == TutorialState.TargetEnum.CreatureAttack)
		{
			DWBattleLaneObject dWBattleLaneObject2 = null;
			if (state.TargetId(0) == "Cheapest")
			{
				foreach (DWBattleLaneObject item2 in Singleton<DWBattleLane>.Instance.BattleLaneObjects[0])
				{
					if (dWBattleLaneObject2 == null || item2.Creature.AttackCost < dWBattleLaneObject2.Creature.AttackCost)
					{
						dWBattleLaneObject2 = item2;
					}
				}
			}
			else
			{
				dWBattleLaneObject2 = Singleton<DWBattleLane>.Instance.GetLaneObject(PlayerType.User, state.TargetId(0));
			}
			if (dWBattleLaneObject2 != null)
			{
				TutorialObject.Attach(dWBattleLaneObject2.gameObject, state.ID);
			}
			if (state.TargetId(1) == null)
			{
				foreach (DWBattleLaneObject item3 in Singleton<DWBattleLane>.Instance.BattleLaneObjects[1])
				{
					TutorialDragObject.Attach(item3.gameObject, state.ID);
				}
				return;
			}
			dWBattleLaneObject2 = Singleton<DWBattleLane>.Instance.GetLaneObject(PlayerType.Opponent, state.TargetId(1));
			if (dWBattleLaneObject2 != null)
			{
				TutorialDragObject.Attach(dWBattleLaneObject2.gameObject, state.ID);
			}
		}
		else if (state.Target == TutorialState.TargetEnum.Status)
		{
			GameObject statusEffectIcon = Singleton<BattleHudController>.Instance.GetStatusEffectIcon(state.TargetId(0));
			if (statusEffectIcon != null)
			{
				TutorialObject.Attach(statusEffectIcon, state.ID);
			}
		}
		else if (state.Target == TutorialState.TargetEnum.EditDeck)
		{
			TutorialObject.Attach(Singleton<EditDeckController>.Instance.ButtonTeamNext, state.ID);
			TutorialObject.Attach(Singleton<EditDeckController>.Instance.ButtonTeamPrev, state.ID);
		}
		else
		{
			if (state.Target != TutorialState.TargetEnum.Special)
			{
				return;
			}
			if (state.SpecialTarget == TutorialState.SpecialTargetEnum.PlayableCards)
			{
				foreach (CardPrefabScript handCard2 in Singleton<HandCardController>.Instance.GetHandCards())
				{
					if (Singleton<DWGame>.Instance.CanPlay(PlayerType.User, handCard2.Card) == PlayerState.CanPlayResult.CanPlay)
					{
						TutorialObject.Attach(handCard2.gameObject, state.ID);
						break;
					}
				}
				return;
			}
			if (state.SpecialTarget == TutorialState.SpecialTargetEnum.DroppedEgg)
			{
				GameObject lootObject = Singleton<DWBattleLane>.Instance.GetLootObject();
				if (lootObject != null)
				{
					TutorialObject.Attach(lootObject, state.ID);
				}
			}
			else if (state.SpecialTarget == TutorialState.SpecialTargetEnum.UnusedCreatureInList)
			{
				GameObject unusedCreature = Singleton<EditDeckController>.Instance.GetUnusedCreature();
				if (unusedCreature == null)
				{
					unusedCreature = Singleton<XpFusionController>.Instance.GetUnusedCreature();
				}
				if (unusedCreature != null)
				{
					TutorialObject.Attach(unusedCreature, state.ID);
				}
			}
			else if (state.SpecialTarget == TutorialState.SpecialTargetEnum.BestCreatureInList)
			{
				GameObject bestCreature = Singleton<XpFusionController>.Instance.GetBestCreature();
				if (bestCreature != null)
				{
					TutorialObject.Attach(bestCreature, state.ID);
				}
			}
			else if (state.SpecialTarget == TutorialState.SpecialTargetEnum.TutorialFuseCreature)
			{
				GameObject tutorialFuseFodder = Singleton<XpFusionController>.Instance.GetTutorialFuseFodder();
				if (tutorialFuseFodder != null)
				{
					TutorialObject.Attach(tutorialFuseFodder, state.ID);
				}
			}
			else if (state.SpecialTarget == TutorialState.SpecialTargetEnum.TutorialEnhanceCreature)
			{
				GameObject tutorialEnhanceCreature = Singleton<EnhanceCreatureScreenController>.Instance.GetTutorialEnhanceCreature();
				if (tutorialEnhanceCreature != null)
				{
					TutorialObject.Attach(tutorialEnhanceCreature, state.ID);
				}
			}
			else if (state.SpecialTarget == TutorialState.SpecialTargetEnum.TutorialEvoCreature)
			{
				GameObject tutorialEvoCreature = Singleton<EvoScreenController>.Instance.GetTutorialEvoCreature();
				if (tutorialEvoCreature != null)
				{
					TutorialObject.Attach(tutorialEvoCreature, state.ID);
				}
			}
			else if (state.SpecialTarget == TutorialState.SpecialTargetEnum.BestCreatureCards)
			{
				TutorialObject.Attach(Singleton<EditDeckController>.Instance.GetBestCreatureCardObject(), state.ID);
			}
			else if (state.SpecialTarget == TutorialState.SpecialTargetEnum.UnusedCardInList)
			{
				TutorialObject.Attach(Singleton<CardEquipController>.Instance.GetUnusedCard(), state.ID);
			}
			else if (state.SpecialTarget == TutorialState.SpecialTargetEnum.HighestLeague)
			{
				TutorialObject.Attach(Singleton<QuestSelectController>.Instance.GetHighestLeagueObject(), state.ID);
			}
			else if (state.SpecialTarget == TutorialState.SpecialTargetEnum.HighestQuest)
			{
				TutorialObject.Attach(Singleton<QuestSelectController>.Instance.GetHighestQuestObject(), state.ID);
			}
			else if (state.SpecialTarget == TutorialState.SpecialTargetEnum.HelperCreature)
			{
				TutorialObject.Attach(Singleton<PreMatchHelperSelectController>.Instance.GetHelperCreatureObject(), state.ID);
			}
			else if (state.SpecialTarget == TutorialState.SpecialTargetEnum.ZoomedCardText)
			{
				CardPrefabScript zoomedCard = Singleton<HandCardController>.Instance.GetZoomedCard();
				if (zoomedCard == null)
				{
					zoomedCard = Singleton<CreatureInfoPopup>.Instance.GetZoomedCard();
				}
				if (zoomedCard != null)
				{
					TutorialObject.Attach(zoomedCard.CardTextCollider.gameObject, state.ID);
				}
			}
			else if (state.SpecialTarget == TutorialState.SpecialTargetEnum.TapNewestCreatureInList)
			{
				GameObject newestCreature = Singleton<EditDeckController>.Instance.GetNewestCreature();
				if (newestCreature != null)
				{
					TutorialObject.Attach(newestCreature, state.ID);
				}
			}
			else if (state.SpecialTarget == TutorialState.SpecialTargetEnum.TapNewestEVOCreatureInList)
			{
				GameObject newestCreature2 = Singleton<EvoScreenController>.Instance.GetNewestCreature();
				if (newestCreature2 != null)
				{
					TutorialObject.Attach(newestCreature2, state.ID);
				}
			}
			else if (state.SpecialTarget == TutorialState.SpecialTargetEnum.AddNewestCreatureInList)
			{
				GameObject newestCreature3 = Singleton<EditDeckController>.Instance.GetNewestCreature();
				if (newestCreature3 != null)
				{
					TutorialObject.Attach(newestCreature3, state.ID);
				}
			}
		}
	}

	public bool CheckTutorialTriggers()
	{
		if (!IsBlockComplete("Q1") && Singleton<PlayerInfoScript>.Instance.SaveData.TopCompletedQuestId >= 1)
		{
			TutorialDataManager.Instance.GetBlock("Q1").Completed = true;
		}
		if (!IsBlockComplete("Q2") && Singleton<PlayerInfoScript>.Instance.SaveData.TopCompletedQuestId >= 2)
		{
			TutorialDataManager.Instance.GetBlock("Q2").Completed = true;
		}
		if (!IsBlockComplete("BuyHero") && Singleton<PlayerInfoScript>.Instance.RankData.Level >= 8)
		{
			StartTutorialBlock("BuyHero");
			PlayerSaveData saveData = Singleton<PlayerInfoScript>.Instance.SaveData;
			if (!saveData.TutorialsDone.Contains("BuyHero"))
			{
				saveData.TutorialsDone.Add("BuyHero");
			}
			TutorialDataManager.TutorialBlock block = TutorialDataManager.Instance.GetBlock("BuyHero");
			if (block != null)
			{
				block.Completed = true;
			}
			Singleton<PlayerInfoScript>.Instance.Save();
			return true;
		}
		foreach (TutorialState item in TutorialDataManager.Instance.GetDatabase())
		{
			if (!(item.Trigger == string.Empty) && !(item.Block == string.Empty) && !IsBlockComplete(item.Block) && (item.Trigger == "FTUE" || Singleton<PlayerInfoScript>.Instance.IsFeatureUnlocked(item.Trigger)))
			{
				StartTutorialBlock(item.Block);
				return true;
			}
		}
		return false;
	}

	public void CheckTutorialForceDraw(CreatureState creature)
	{
		TutorialState activeState = GetActiveState();
		if (activeState != null)
		{
			TutorialState.ForceAction forceAction = activeState.ForceActions.Find((TutorialState.ForceAction m) => m.Action == TutorialState.ForceActionEnum.Draw);
			if (forceAction != null)
			{
				List<CardData> list = new List<CardData>();
				list.Add(CardDataManager.Instance.GetData(forceAction.Targets[0]));
				creature.OverrideDrawPile = list;
			}
		}
	}

	public void ClearTutorialForceDraw(CreatureState creature)
	{
		creature.OverrideDrawPile = null;
	}

	public bool ForceCrit()
	{
		TutorialState activeState = GetActiveState();
		if (activeState == null)
		{
			return false;
		}
		return activeState.ForceActions.Find((TutorialState.ForceAction m) => m.Action == TutorialState.ForceActionEnum.Crit) != null;
	}

	public bool DisableLeaderBar()
	{
		return IsBlockActive("IntroBattle");
	}

	public bool DisableOpponentLeaderBar()
	{
		return IsBlockActive("IntroBattle");
	}

	public bool AutoFillLeaderBar()
	{
		TutorialState activeState = GetActiveState();
		if (activeState == null)
		{
			return false;
		}
		if (activeState.ForceActions.Find((TutorialState.ForceAction m) => m.Action == TutorialState.ForceActionEnum.FillLeaderBar) == null)
		{
			return manulSetAutoFillMeterBar;
		}
		return true;
	}

	public string CheckTutorialLeaderCardDraw()
	{
		return GetActiveState()?.ForceActions.Find((TutorialState.ForceAction m) => m.Action == TutorialState.ForceActionEnum.Draw)?.Targets[0];
	}

	public List<string> GetCurrentCardOverrides(PlayerType player, CreatureFaction faction)
	{
		TutorialState activeState = GetActiveState();
		if (activeState == null)
		{
			return null;
		}
		TutorialDataManager.TutorialBlock block = TutorialDataManager.Instance.GetBlock(activeState.Block);
		if (player == PlayerType.User)
		{
			return block.CardOverrides[(int)faction];
		}
		return block.EnemyCardOverrides[(int)faction];
	}

	public List<string> GetAllCurrentCardOverrides(PlayerType player)
	{
		TutorialState activeState = GetActiveState();
		if (activeState == null)
		{
			return null;
		}
		List<string> list = new List<string>();
		TutorialDataManager.TutorialBlock block = TutorialDataManager.Instance.GetBlock(activeState.Block);
		if (player == PlayerType.User)
		{
			List<string>[] cardOverrides = block.CardOverrides;
			foreach (List<string> list2 in cardOverrides)
			{
				if (list2 == null)
				{
					continue;
				}
				foreach (string item in list2)
				{
					list.Add(item);
				}
			}
		}
		else
		{
			List<string>[] cardOverrides = block.EnemyCardOverrides;
			foreach (List<string> list3 in cardOverrides)
			{
				if (list3 == null)
				{
					continue;
				}
				foreach (string item2 in list3)
				{
					list.Add(item2);
				}
			}
		}
		if (list.Count > 0)
		{
			return list;
		}
		return null;
	}

	public List<string> GetCurrentForcedDrops()
	{
		return GetActiveState()?.ForceActions.Find((TutorialState.ForceAction m) => m.Action == TutorialState.ForceActionEnum.Drop)?.Targets;
	}

	public List<string> GetCurrentForcedStartingHand(PlayerType player)
	{
		TutorialState activeState = GetActiveState();
		if (activeState == null)
		{
			return null;
		}
		TutorialState.ForceActionEnum action = ((player != PlayerType.User) ? TutorialState.ForceActionEnum.StartHandEnemy : TutorialState.ForceActionEnum.StartHand);
		TutorialState.ForceAction forceAction = activeState.ForceActions.Find((TutorialState.ForceAction m) => m.Action == action);
		if (forceAction == null)
		{
			return null;
		}
		if (Singleton<TutorialController>.Instance.IsBlockActive("Q1"))
		{
			string value = getChosenTeamFactionName().ToUpper() + "_";
			List<string> list = new List<string>();
			{
				foreach (string target in forceAction.Targets)
				{
					if (target.Contains(value))
					{
						list.Add(target);
					}
				}
				return list;
			}
		}
		return forceAction.Targets;
	}

	public int GetStartAP(PlayerType player)
	{
		int result = -1;
		TutorialState activeState = GetActiveState();
		if (activeState == null)
		{
			return result;
		}
		TutorialState.ForceActionEnum action = ((player != PlayerType.User) ? TutorialState.ForceActionEnum.StartAPEnemy : TutorialState.ForceActionEnum.StartAP);
		TutorialState.ForceAction forceAction = activeState.ForceActions.Find((TutorialState.ForceAction m) => m.Action == action);
		if (forceAction == null || forceAction.Targets.Count != 1)
		{
			return result;
		}
		return int.Parse(forceAction.Targets[0]);
	}

	public TutorialBoardData GetTutorialBoard()
	{
		string tutorialBoardID = Singleton<PlayerInfoScript>.Instance.StateData.tutorialBoardID;
		if (tutorialBoardID == null)
		{
			return null;
		}
		return TutorialBoardDataManager.Instance.GetData(tutorialBoardID);
	}

	public bool OverrideAIInCurrentState()
	{
		TutorialState activeState = GetActiveState();
		if (activeState == null)
		{
			return false;
		}
		return activeState.ForceActions.Count > 0;
	}

	public void BuildAIPlan()
	{
		mAIPlan.Clear();
		AIOverridden = true;
		TutorialState activeState = GetActiveState();
		activeState.ForceActions.FindAll((TutorialState.ForceAction m) => m.Action == TutorialState.ForceActionEnum.Attack);
		TutorialState.ForceAction action;
		foreach (TutorialState.ForceAction forceAction in activeState.ForceActions)
		{
			action = forceAction;
			if (action.Action == TutorialState.ForceActionEnum.Attack)
			{
				DWBattleLaneObject laneObject = Singleton<DWBattleLane>.Instance.GetLaneObject(PlayerType.Opponent, action.Targets[0]);
				_ = laneObject == null;
				DWBattleLaneObject laneObject2 = Singleton<DWBattleLane>.Instance.GetLaneObject(PlayerType.User, action.Targets[1]);
				_ = laneObject2 == null;
				if (laneObject == null || laneObject2 == null)
				{
					return;
				}
				AIDecision aIDecision = new AIDecision();
				aIDecision.Seed = KFFRandom.Seed;
				aIDecision.IsAttack = true;
				aIDecision.LaneIndex1 = laneObject.Creature.Lane.Index;
				aIDecision.LaneIndex2 = laneObject2.Creature.Lane.Index;
				aIDecision.TutorialForcedCardDraw = action.CardDrawOverride;
				mAIPlan.Add(aIDecision);
			}
			else if (action.Action == TutorialState.ForceActionEnum.PlayAttackCard)
			{
				DWBattleLaneObject laneObject3 = Singleton<DWBattleLane>.Instance.GetLaneObject(PlayerType.Opponent, action.Targets[1]);
				DWBattleLaneObject laneObject4 = Singleton<DWBattleLane>.Instance.GetLaneObject(PlayerType.User, action.Targets[2]);
				AIDecision aIDecision2 = new AIDecision();
				aIDecision2.Seed = KFFRandom.Seed;
				aIDecision2.Card = CardDataManager.Instance.GetData(action.Targets[0]);
				aIDecision2.TargetPlayer = PlayerType.Opponent;
				if (laneObject3 != null)
				{
					aIDecision2.LaneIndex1 = laneObject3.Creature.Lane.Index;
				}
				if (laneObject4 != null)
				{
					aIDecision2.LaneIndex2 = laneObject4.Creature.Lane.Index;
				}
				Singleton<DWGame>.Instance.SetTarget(PlayerType.Opponent, laneObject4.Creature);
				mAIPlan.Add(aIDecision2);
			}
			else if (action.Action == TutorialState.ForceActionEnum.PlayCreatureCard)
			{
				PlayerState playerState = Singleton<DWGame>.Instance.CurrentBoardState.GetPlayerState(PlayerType.Opponent);
				CreatureState creatureState = playerState.DeploymentList.Find((CreatureState m) => m.Data.Form.ID == action.Targets[0] || m.Data.Form.Faction.ClassName() == action.Targets[0]);
				if (creatureState == null)
				{
					return;
				}
				AIDecision aIDecision3 = new AIDecision();
				aIDecision3.Seed = KFFRandom.Seed;
				aIDecision3.IsDeploy = true;
				aIDecision3.Creature = creatureState.Data;
				aIDecision3.LaneIndex1 = playerState.GetCreatureCount();
				mAIPlan.Add(aIDecision3);
			}
		}
		AIDecision aIDecision4 = new AIDecision();
		aIDecision4.EndTurn = true;
		mAIPlan.Add(aIDecision4);
	}

	public AIDecision GetAIDecision()
	{
		if (mAIPlan.Count == 0)
		{
			return null;
		}
		AIDecision result = mAIPlan[0];
		mAIPlan.RemoveAt(0);
		return result;
	}

	public bool DisallowPlayerLoss()
	{
		if (!IsBlockActive("IntroBattle"))
		{
			return IsFTUETutorialActive();
		}
		return true;
	}

	public bool IgnoreEnergyCosts()
	{
		return IsBlockActive("IntroBattle");
	}

	public int GetTutorialCritRateAdjust(PlayerType player, int baseCrit)
	{
		if (IsBlockActive("IntroBattle"))
		{
			return -999;
		}
		if (!IsFTUETutorialActive())
		{
			return 0;
		}
		if (player == PlayerType.User)
		{
			PlayerState playerState = Singleton<DWGame>.Instance.CurrentBoardState.GetPlayerState(PlayerType.User);
			int deadCreatureCount = playerState.GetDeadCreatureCount();
			int num = deadCreatureCount + playerState.GetCreatureCount() + playerState.DeploymentList.Count;
			return (int)(Mathf.Pow((float)deadCreatureCount / (float)(num - 1), 2f) * (float)(100 - baseCrit));
		}
		return -999;
	}

	public bool OnNonEnemyTurnTutorialState()
	{
		TutorialState activeState = GetActiveState();
		if (activeState == null)
		{
			return false;
		}
		if (activeState.Target != TutorialState.TargetEnum.EnemyTurn)
		{
			return activeState.Target != TutorialState.TargetEnum.Conditionals;
		}
		return false;
	}

	public bool BlockCardDragging()
	{
		TutorialState activeState = GetActiveState();
		if (activeState == null)
		{
			return false;
		}
		if (activeState.Target != TutorialState.TargetEnum.CardPlay && activeState.Target != TutorialState.TargetEnum.CreatureCardPlay && activeState.Target != TutorialState.TargetEnum.CardPlayOnEnemy)
		{
			return activeState.Target != TutorialState.TargetEnum.Conditionals;
		}
		return false;
	}

	public void AdvanceTutorialState(bool backwards = false)
	{
		GameStateData stateData = Singleton<PlayerInfoScript>.Instance.StateData;
		if (stateData.ActiveConditionalState != null)
		{
			if (!stateData.ActiveConditionalState.Passed)
			{
				PassCurrentState();
			}
			TutorialState tutorialState = TutorialDataManager.Instance.GetData(stateData.ActiveConditionalState.Index + 1);
			if (tutorialState == null || tutorialState.Block != stateData.ActiveConditionalState.Block || tutorialState.ConditionType != TutorialState.ConditionTypeEnum.None)
			{
				tutorialState = null;
			}
			EnterConditionalState(tutorialState);
		}
		else if (stateData.ActiveTutorialState != null)
		{
			if (!stateData.ActiveTutorialState.Passed)
			{
				PassCurrentState();
			}
			TutorialDataManager.TutorialBlock block = TutorialDataManager.Instance.GetBlock(stateData.ActiveTutorialState.Block);
			if (!backwards && stateData.ActiveTutorialState.EndState)
			{
				block.Completed = true;
				Singleton<PlayerInfoScript>.Instance.Save();
			}
			int num = ((!backwards) ? 1 : (-1));
			TutorialState tutorialState2 = TutorialDataManager.Instance.GetData(stateData.ActiveTutorialState.Index + num);
			if (tutorialState2 == null || (tutorialState2.Block != block.ID && !stateData.ActiveTutorialState.ContinueThrough))
			{
				tutorialState2 = null;
			}
			EnterTutorialState(tutorialState2);
		}
	}

	public void PassCurrentState()
	{
		TutorialState activeState = GetActiveState();
		if (activeState == null)
		{
			return;
		}
		FullScreenCollider.SetActive(value: false);
		UICamera.ColliderRestrictionList.Clear();
		UICamera.OnlyAllowDrag = false;
		if (!activeState.HideArrow())
		{
			PointerObject.gameObject.SetActive(value: false);
		}
		if (activeState.Text != string.Empty)
		{
			HidePopupTween.Play();
		}
		if (activeState.DragTargetList.Count > 0 || activeState.DragDirection() != TutorialState.DragDirectionEnum.None)
		{
			HideDragTween.Play();
		}
		if (activeState.VOEvent != null)
		{
			Singleton<SLOTAudioManager>.Instance.StopSound(activeState.VOEvent);
		}
		mDisplayedState = null;
		activeState.Passed = true;
		foreach (string exitFunctionCall in activeState.ExitFunctionCalls)
		{
			SendMessage(exitFunctionCall);
		}
	}

	public void PassIfOnState(string stateId)
	{
		if (IsStateActive(stateId))
		{
			PassCurrentState();
		}
	}

	public void AdvanceIfOnState(string stateId)
	{
		if (IsStateActive(stateId))
		{
			AdvanceTutorialState();
		}
	}

	public void BackUpIfOnState(string stateId)
	{
		if (IsStateActive(stateId))
		{
			AdvanceTutorialState(backwards: true);
		}
	}

	public void AdvanceIfNextStateIs(string stateId)
	{
		TutorialState activeState = GetActiveState();
		if (activeState != null && !(TutorialDataManager.Instance.GetData(activeState.Index + 1).ID != stateId))
		{
			AdvanceTutorialState();
		}
	}

	public void AdvanceIfTargetingBuilding(string buildingId)
	{
		TutorialState activeState = GetActiveState();
		_ = Singleton<PlayerInfoScript>.Instance.StateData;
		if (activeState != null && activeState.Target == TutorialState.TargetEnum.TapBuilding && activeState.TargetId(0) == buildingId)
		{
			AdvanceTutorialState();
		}
	}

	public void TiltOffIfTargetingBuilding()
	{
		if (Singleton<TutorialController>.Instance.IsAnyTutorialActive())
		{
			TutorialState activeState = GetActiveState();
			_ = Singleton<PlayerInfoScript>.Instance.StateData;
			if (activeState != null && (activeState.Target == TutorialState.TargetEnum.TapBuilding || activeState.Target == TutorialState.TargetEnum.PointBuilding))
			{
				Singleton<MouseOrbitCamera>.Instance.EnableTiltCam(enabled: false);
			}
			else
			{
				Singleton<MouseOrbitCamera>.Instance.CheckTiltCamSettingBeforeTutorial();
			}
		}
	}

	public void PassIfOnAttack()
	{
		TutorialState activeState = GetActiveState();
		if (activeState != null && activeState.Target == TutorialState.TargetEnum.CreatureAttack)
		{
			PassCurrentState();
		}
	}

	public void AdvanceIfOnAttack()
	{
		TutorialState activeState = GetActiveState();
		if (activeState != null && activeState.Target == TutorialState.TargetEnum.CreatureAttack)
		{
			AdvanceTutorialState();
		}
	}

	public void PassIfOnCardPlay()
	{
		TutorialState activeState = GetActiveState();
		if (activeState != null && (activeState.Target == TutorialState.TargetEnum.CardPlay || activeState.Target == TutorialState.TargetEnum.CardPlayOnEnemy))
		{
			PassCurrentState();
		}
	}

	public void AdvanceIfOnCardPlay(bool targeting)
	{
		TutorialState activeState = GetActiveState();
		if (activeState != null && (activeState.Target == TutorialState.TargetEnum.CardPlay || activeState.Target == TutorialState.TargetEnum.CardPlayOnEnemy || activeState.Target == TutorialState.TargetEnum.CreatureCardPlay || activeState.Target == TutorialState.TargetEnum.AdvanceIfNotTargetingCard))
		{
			AdvanceTutorialState();
			TutorialState activeState2 = GetActiveState();
			if (activeState2 != null && activeState2.Target == TutorialState.TargetEnum.AdvanceIfNotTargetingCard && !targeting)
			{
				AdvanceTutorialState();
			}
		}
	}

	public void AdvanceIfOnEnemyTurn()
	{
		mAIPlan.Clear();
		AIOverridden = false;
		TutorialState activeState = GetActiveState();
		if (activeState != null && activeState.Target == TutorialState.TargetEnum.EnemyTurn)
		{
			AdvanceTutorialState();
		}
	}

	public void AdvanceIfTargetingAttack()
	{
		TutorialState activeState = GetActiveState();
		if (activeState != null && activeState.Target == TutorialState.TargetEnum.TapEnemyCreature)
		{
			AdvanceTutorialState();
		}
	}

	public void AdvanceIfDraggingTile()
	{
		TutorialState activeState = GetActiveState();
		if (activeState != null && activeState.Target == TutorialState.TargetEnum.Special && (activeState.SpecialTarget == TutorialState.SpecialTargetEnum.UnusedCreatureInList || activeState.SpecialTarget == TutorialState.SpecialTargetEnum.BestCreatureInList || activeState.SpecialTarget == TutorialState.SpecialTargetEnum.TutorialFuseCreature || activeState.SpecialTarget == TutorialState.SpecialTargetEnum.TutorialEnhanceCreature || activeState.SpecialTarget == TutorialState.SpecialTargetEnum.TutorialEvoCreature || activeState.SpecialTarget == TutorialState.SpecialTargetEnum.AddNewestCreatureInList || activeState.SpecialTarget == TutorialState.SpecialTargetEnum.TutorialCardCreature || activeState.SpecialTarget == TutorialState.SpecialTargetEnum.UnusedCardInList || activeState.SpecialTarget == TutorialState.SpecialTargetEnum.HelperCreature))
		{
			AdvanceTutorialState();
		}
	}

	public void AdvanceToState(string stateId)
	{
		if (Singleton<PlayerInfoScript>.Instance.StateData.ActiveConditionalState != null)
		{
			PassCurrentState();
			Singleton<PlayerInfoScript>.Instance.StateData.ActiveConditionalState = null;
		}
		TutorialState activeState = GetActiveState();
		if (activeState != null)
		{
			if (activeState.ID == stateId)
			{
				return;
			}
			if (!activeState.Passed)
			{
				PassCurrentState();
			}
			if (activeState.EndState)
			{
				CompleteActiveBlock();
			}
		}
		TutorialState data = TutorialDataManager.Instance.GetData(stateId);
		EnterTutorialState(data);
	}

	public void CompleteActiveBlock()
	{
		TutorialState activeState = GetActiveState();
		TutorialDataManager.Instance.GetBlock(activeState.Block).Completed = true;
		Singleton<PlayerInfoScript>.Instance.Save();
	}

	public bool IsAnyTutorialActive()
	{
		return GetActiveState() != null;
	}

	public bool IsFTUETutorialActive()
	{
		TutorialState activeState = GetActiveState();
		if (activeState == null)
		{
			return false;
		}
		if (!(activeState.Block == "Q1"))
		{
			return activeState.Block == "Q2";
		}
		return true;
	}

	public bool IsFTUETutorialComplete()
	{
		return IsBlockComplete("Q2");
	}

	public bool IsGachaTutorialActive()
	{
		TutorialState activeState = GetActiveState();
		if (activeState == null)
		{
			return false;
		}
		if (!(activeState.Block == "UseGacha") && !(activeState.Block == "EquipGacha"))
		{
			return activeState.Block == "Bank";
		}
		return true;
	}

	public bool IsStateActive(string stateId)
	{
		TutorialState activeState = GetActiveState();
		if (activeState != null)
		{
			return activeState.ID == stateId;
		}
		return false;
	}

	public bool IsStateActiveAndNotPassed(string stateId)
	{
		TutorialState activeState = GetActiveState();
		if (activeState != null && !activeState.Passed)
		{
			return activeState.ID == stateId;
		}
		return false;
	}

	public bool IsBlockActive(string blockId)
	{
		TutorialState activeTutorialState = Singleton<PlayerInfoScript>.Instance.StateData.ActiveTutorialState;
		if (activeTutorialState != null)
		{
			return activeTutorialState.Block == blockId;
		}
		return false;
	}

	public bool IsBlockComplete(string blockId)
	{
		return TutorialDataManager.Instance.GetBlock(blockId)?.Completed ?? true;
	}

	public string getChosenTeamFactionName()
	{
		string result = string.Empty;
		switch (getChosenTeamCreatureFaction())
		{
		case CreatureFaction.Red:
			result = "Corn";
			break;
		case CreatureFaction.Blue:
			result = "Plains";
			break;
		case CreatureFaction.Light:
			result = "Nice";
			break;
		}
		return result;
	}

	public CreatureFaction getChosenTeamCreatureFaction()
	{
		Loadout loadout = Singleton<PlayerInfoScript>.Instance.SaveData.Loadouts[Singleton<PlayerInfoScript>.Instance.SaveData.SelectedLoadout];
		if (loadout.CreatureCount() > 0)
		{
			return loadout.CreatureSet[0].Creature.Form.Faction;
		}
		return Singleton<PlayerInfoScript>.Instance.SaveData.SelectedLoadout switch
		{
			0 => CreatureFaction.Red, 
			1 => CreatureFaction.Blue, 
			2 => CreatureFaction.Light, 
			_ => CreatureFaction.Red, 
		};
	}

	public void DebugCompleteAllTutorials(bool ftueOnly = false)
	{
		TutorialDataManager.Instance.DebugCompleteAllTutorials(ftueOnly);
		if (Singleton<PlayerInfoScript>.Instance.StateData.ActiveTutorialState != null)
		{
			PassCurrentState();
		}
		Singleton<PlayerInfoScript>.Instance.StateData.ActiveTutorialState = null;
		Singleton<PlayerInfoScript>.Instance.StateData.ActiveConditionalState = null;
	}

	public void SkipAllTutorials()
	{
		PlayerInfoScript instance = Singleton<PlayerInfoScript>.Instance;
		DebugCompleteAllTutorials();
		int xpToReachRank = instance.GetXpToReachRank(3);
		if (xpToReachRank > instance.SaveData.RankXP)
		{
			instance.SaveData.RankXP = xpToReachRank;
			instance.RefreshRankXpData();
		}
		instance.UnlockFeature("TBuilding_EditDeck");
		instance.UnlockFeature("TBuilding_Gacha");
		instance.UnlockFeature("TBuilding_Store");
		instance.Save();
	}

	public bool AskNameIfSkipped(Action then)
	{
		if (mAskingName || !IsFTUETutorialComplete() || IsAnyTutorialActive() || !string.IsNullOrEmpty(Singleton<PlayerInfoScript>.Instance.SaveData.MultiplayerPlayerName))
		{
			return false;
		}
		mAskingName = true;
		StartCoroutine(NameEntryCo(delegate
		{
			mAskingName = false;
			if (then != null)
			{
				then();
			}
		}));
		return true;
	}

	public void DebugTutorialFromGacha()
	{
		TutorialState data = TutorialDataManager.Instance.GetData("GA_ShowGacha");
		TutorialDataManager.Instance.DebugCompleteAllBlocksExcept(new List<string> { data.Block });
		Singleton<PlayerInfoScript>.Instance.Save();
	}

	public static bool IsValidDragTarget(GameObject obj)
	{
		if (obj == null)
		{
			return false;
		}
		TutorialState activeState = GetActiveState();
		if (activeState == null || activeState.DragTargetList.Count == 0)
		{
			return true;
		}
		if (new List<TutorialDragObject>(obj.GetComponents<TutorialDragObject>()).Find((TutorialDragObject match) => match.TutorialState == activeState.ID) == null)
		{
			return false;
		}
		return true;
	}

	private static TutorialState GetActiveState()
	{
		if (Singleton<PlayerInfoScript>.Instance.StateData.ActiveConditionalState != null)
		{
			return Singleton<PlayerInfoScript>.Instance.StateData.ActiveConditionalState;
		}
		return Singleton<PlayerInfoScript>.Instance.StateData.ActiveTutorialState;
	}

	private Vector3 GetObjectPosition(GameObject tutObject, bool useBoardOffset = false)
	{
		if (tutObject.layer == mLayerCardHand)
		{
			Vector2 vector = Singleton<DWGameCamera>.Instance.Battle3DUICam.WorldToScreenPoint(tutObject.transform.position);
			return Singleton<DWGameCamera>.Instance.BattleUICam.ScreenToWorldPoint(vector);
		}
		if (tutObject.layer == mLayerBattleBoard)
		{
			Vector2 vector2 = Singleton<DWGameCamera>.Instance.MainCam.WorldToScreenPoint(tutObject.transform.position);
			Vector3 result = Singleton<DWGameCamera>.Instance.BattleUICam.ScreenToWorldPoint(vector2);
			if (useBoardOffset)
			{
				result.y += BoardYPosAdjust;
			}
			return result;
		}
		if (tutObject.layer == mLayerTown)
		{
			Vector2 vector3 = m3DCam.WorldToScreenPoint(tutObject.transform.position);
			return mUICam.ScreenToWorldPoint(vector3);
		}
		return tutObject.transform.position;
	}

	private void Update()
	{
		TutorialState tutorialState = GetActiveState();
		if (tutorialState != null)
		{
			if (UICamera.IsInputLocked() && mDisplayedState == null && mStartDelayTimer == -1f)
			{
				tutorialState = null;
			}
			else if (tutorialState.Passed)
			{
				tutorialState = null;
			}
			else if (!tutorialState.HideArrow() && !tutorialState.PositionSet && tutorialState.ObjectList.Count == 0)
			{
				tutorialState = null;
			}
		}
		if (tutorialState != null && tutorialState != mDisplayedState)
		{
			if (tutorialState.StartDelay > 0f)
			{
				if (mStartDelayTimer == -1f)
				{
					UICamera.LockInput();
					mStartDelayTimer = 0f;
				}
				if (mStartDelayTimer < tutorialState.StartDelay)
				{
					mStartDelayTimer += Time.deltaTime;
					return;
				}
				UICamera.UnlockInput();
				mStartDelayTimer = -1f;
			}
			DisplayState(tutorialState);
		}
		if (mDisplayedState == null)
		{
			return;
		}
		if (!mDisplayedState.HideArrow())
		{
			if (mDisplayedState.ObjectList.Count > 0)
			{
				bool useBoardOffset = mDisplayedState.Target == TutorialState.TargetEnum.PointCreature || mDisplayedState.Target == TutorialState.TargetEnum.PointEnemyCreature || mDisplayedState.Target == TutorialState.TargetEnum.TapEnemyCreature || mDisplayedState.Target == TutorialState.TargetEnum.CreatureAttack || mDisplayedState.Target == TutorialState.TargetEnum.CardPlay || mDisplayedState.Target == TutorialState.TargetEnum.CardPlayOnEnemy || mDisplayedState.Target == TutorialState.TargetEnum.CreatureCardPlay;
				TutorialObject tutorialObject = mDisplayedState.ObjectList[0];
				PointerObject.position = GetObjectPosition(tutorialObject.gameObject, useBoardOffset);
			}
			Vector3 position = PointerObject.position;
			position.z = 0f;
			PointerObject.position = position;
			float num = ((!(PointerObject.position == Vector3.zero)) ? Vector3.Angle(new Vector3(1f, 0f, 0f), PointerObject.position) : 360f);
			if (PointerObject.position.y < 0f)
			{
				num = 360f - num;
			}
			num -= 90f;
			PointerObject.rotation = Quaternion.Euler(0f, 0f, num);
		}
		if (mDisplayedState.Text != string.Empty)
		{
			if (mDisplayedState.PositionSet)
			{
				PopupObject.localPosition = mDisplayedState.PopupPos;
			}
			else
			{
				Vector3 localPosition = PointerObject.localPosition;
				localPosition.z = 0f;
				Vector3 normalized = localPosition.normalized;
				Vector3 vector = normalized * PopupArrowSpacing;
				if (normalized.x != 0f && normalized.y != 0f)
				{
					float num2 = 1f / (1f - Mathf.Abs(normalized.x * normalized.y)) - 1f;
					num2 *= PopupSpacingCornerFactor;
					normalized *= 1f + num2;
				}
				Vector3 zero = Vector3.zero;
				zero.x = PointerObject.localPosition.x - vector.x - normalized.x * (float)PopupBackground.width / 2f;
				zero.y = PointerObject.localPosition.y - vector.y - normalized.y * (float)PopupBackground.height / 2f;
				PopupObject.localPosition = zero;
			}
			if (CharacterTexture.gameObject.activeInHierarchy)
			{
				Vector3 localPosition2 = PopupBackground.transform.localPosition;
				localPosition2.x -= PopupBackground.width / 2;
				localPosition2.y += PopupBackground.height / 2;
				localPosition2.x += CharacterTextureOffset.x;
				localPosition2.y += CharacterTextureOffset.y;
				CharacterTexture.transform.localPosition = localPosition2;
			}
		}
		if (mDisplayedState.DragTargetList.Count == 1)
		{
			UpdateDragArrowPosition();
		}
	}

	private void UpdateDragArrowPosition()
	{
		TweenPosition component = ShowDragTween.GetComponent<TweenPosition>();
		component.worldSpace = true;
		component.from = GetObjectPosition(mDisplayedState.ObjectList[0].gameObject);
		component.to = GetObjectPosition(mDisplayedState.DragTargetList[0].gameObject);
		float num = (float)Math.Atan2(component.to.y - component.from.y, component.to.x - component.from.x);
		num *= 180f / MathF.PI;
		num -= 90f;
		DragArrow.localRotation = Quaternion.Euler(new Vector3(0f, 0f, num));
	}

	private void SetColliderRestrictions(TutorialState state)
	{
		if (state.UseFullScreenCollider())
		{
			if (!UICamera.ColliderRestrictionList.Contains(FullScreenCollider))
			{
				UICamera.ColliderRestrictionList.Add(FullScreenCollider);
			}
			return;
		}
		foreach (TutorialObject @object in state.ObjectList)
		{
			if (@object != null && !UICamera.ColliderRestrictionList.Contains(@object.gameObject))
			{
				UICamera.ColliderRestrictionList.Add(@object.gameObject);
			}
		}
	}

	private void DisplayState(TutorialState stateToDisplay)
	{
		mDisplayedState = stateToDisplay;
		if (mDisplayedState.UseFullScreenCollider())
		{
			FullScreenCollider.SetActive(value: true);
		}
		SetColliderRestrictions(stateToDisplay);
		if (!mDisplayedState.HideArrow())
		{
			ShowArrowTween.Play();
			ArrowPointer.SetActive(mDisplayedState.UseFullScreenCollider());
			TapPointer.SetActive(!mDisplayedState.UseFullScreenCollider());
			TapIndicator.SetActive(mDisplayedState.UseFullScreenCollider());
		}
		else
		{
			TapIndicator.SetActive(value: true);
		}
		if (mDisplayedState.Text != string.Empty)
		{
			ShowPopupTween.Play();
			PopupText.width = MinimumWidth;
			PopupText.text = mBaseColorString + mDisplayedState.Text.Replace("[/]", mBaseColorString);
			if (mDisplayedState.TextTitle != string.Empty)
			{
				PopupTextTitle.gameObject.SetActive(value: true);
				PopupTextTitle.text = mDisplayedState.TextTitle;
			}
			else
			{
				PopupTextTitle.gameObject.SetActive(value: false);
			}
			int num = 0;
			if (num > _BackgroundColors.Length - 1)
			{
				num = 0;
			}
			_HexBgSprite.color = _BackgroundColors[num];
			if (mPopupTextReplacement != null)
			{
				PopupText.text = PopupText.text.Replace("<val1>", mPopupTextReplacement);
				mPopupTextReplacement = null;
			}
			while ((float)PopupText.width < (float)PopupText.height * WidthRatio)
			{
				PopupText.width += 40;
			}
			if (mDisplayedState.MaxHeight > 0)
			{
				while (PopupText.height > mDisplayedState.MaxHeight)
				{
					PopupText.width += 40;
					PopupText.ProcessText();
				}
			}
		}
		if (mDisplayedState.VOEvent != null)
		{
			Singleton<SLOTAudioManager>.Instance.PlaySound("vo/tutorial/" + mDisplayedState.VOEvent, useaudiofolder: false, SLOTAudioManager.AudioType.VO);
		}
		if (mDisplayedState.DragDirection() != TutorialState.DragDirectionEnum.None)
		{
			TapIndicator.SetActive(value: false);
			if (!mDisplayedState.UseFullScreenCollider())
			{
				UICamera.OnlyAllowDrag = true;
			}
			TweenPosition component = ShowDragFadeTween.GetComponent<TweenPosition>();
			component.worldSpace = true;
			Vector3 zero = Vector3.zero;
			foreach (TutorialObject @object in mDisplayedState.ObjectList)
			{
				zero += GetObjectPosition(@object.gameObject);
			}
			zero /= (float)mDisplayedState.ObjectList.Count;
			component.from = zero;
			Vector3 to = component.from;
			if (mDisplayedState.DragDirection() == TutorialState.DragDirectionEnum.Up)
			{
				to.y += GeneralDragDistance;
			}
			else if (mDisplayedState.DragDirection() == TutorialState.DragDirectionEnum.Right)
			{
				to.x += GeneralDragDistance;
			}
			component.to = to;
			ShowDragFadeTween.Play();
			float num2 = (float)Math.Atan2(component.to.y - component.from.y, component.to.x - component.from.x);
			num2 *= 180f / MathF.PI;
			num2 -= 90f;
			DragArrow.localRotation = Quaternion.Euler(new Vector3(0f, 0f, num2));
		}
		else if (mDisplayedState.DragTargetList.Count > 0)
		{
			TapIndicator.SetActive(value: false);
			if (!mDisplayedState.UseFullScreenCollider())
			{
				UICamera.OnlyAllowDrag = true;
			}
			UpdateDragArrowPosition();
			ShowDragTween.Play();
		}
		if (mDisplayedState.CharacterTexture != string.Empty)
		{
			CharacterTexture.gameObject.SetActive(value: true);
			CharacterTexture.ReplaceTexture(mDisplayedState.CharacterTexture);
		}
		else
		{
			CharacterTexture.gameObject.SetActive(value: false);
			CharacterTexture.UnloadTexture();
		}
		if (mDisplayedState.ShouldHavePositionSet())
		{
			_ = mDisplayedState.PositionSet;
		}
		StartCoroutine(BriefInputDisable());
	}

	private IEnumerator BriefInputDisable()
	{
		UICamera.LockInput();
		yield return new WaitForSeconds(0.2f);
		UICamera.UnlockInput();
	}

	public bool IsShowingText()
	{
		TutorialState activeState = GetActiveState();
		if (activeState == null)
		{
			return false;
		}
		if (activeState.Block == "IntroBattle")
		{
			return true;
		}
		if (!activeState.Passed)
		{
			return activeState.Text != string.Empty;
		}
		return false;
	}

	private void UnzoomCard()
	{
		if (DetachedSingleton<SceneFlowManager>.Instance.InBattleScene())
		{
			Singleton<HandCardController>.Instance.UnzoomCard();
		}
	}

	private void LeaveToFrontend()
	{
		Singleton<BattleResultsController>.Instance.OnClickTown();
	}

	private void FlashP1Attack()
	{
		Singleton<DWBattleLane>.Instance.BattleLaneObjects[0][0].HealthBar.FlashAttackValueTween.Play();
	}

	private void StopFlashP1Attack()
	{
		Singleton<DWBattleLane>.Instance.BattleLaneObjects[0][0].HealthBar.FlashAttackValueTween.StopAndReset();
	}

	private void FlashP2Attack()
	{
		Singleton<DWBattleLane>.Instance.BattleLaneObjects[1][1].HealthBar.FlashMagicValueTween.Play();
	}

	private void StopFlashP2Attack()
	{
		Singleton<DWBattleLane>.Instance.BattleLaneObjects[1][1].HealthBar.FlashMagicValueTween.StopAndReset();
	}

	private void ShowIntroSale()
	{
		Singleton<StoreScreenController>.Instance.ShowIntroSalePurchasePrompt();
	}

	private void ShowRateAppPopup()
	{
	}

	private void ShowArenaTeamDescriptionBox()
	{
		if (!(Singleton<PreMatchController>.Instance == null) && (IsBlockActive("Q1") || IsFTUETutorialActive()))
		{
			Singleton<PreMatchController>.Instance.TeamDescriptionBox.gameObject.SetActive(value: true);
		}
	}

	private void HideArenaTeamDescriptionBox()
	{
		if (!(Singleton<PreMatchController>.Instance == null) && (IsBlockActive("Q1") || IsFTUETutorialActive()))
		{
			Singleton<PreMatchController>.Instance.TeamDescriptionBox.gameObject.SetActive(value: false);
		}
	}

	private void RemoveUnchosenTeam()
	{
		PlayerSaveData saveData = Singleton<PlayerInfoScript>.Instance.SaveData;
		int creaturesOnTeam = MiscParams.CreaturesOnTeam;
		List<List<CreatureItem>> list = new List<List<CreatureItem>>();
		for (int i = 0; i < 3; i++)
		{
			List<CreatureItem> list2 = new List<CreatureItem>();
			foreach (InventorySlotItem item2 in saveData.Loadouts[i].CreatureSet)
			{
				if (item2 != null && list2.Count < creaturesOnTeam)
				{
					list2.Add(item2.Creature);
				}
			}
			list.Add(list2);
		}
		saveData.ClearInventory();
		saveData.Loadouts[0].CreatureSet.Clear();
		saveData.Loadouts[1].CreatureSet.Clear();
		saveData.Loadouts[2].CreatureSet.Clear();
		for (int j = 0; j < 3; j++)
		{
			foreach (CreatureItem item3 in list[j])
			{
				InventorySlotItem item = saveData.AddCreature(item3);
				saveData.Loadouts[j].CreatureSet.Add(item);
			}
		}
	}

	private void AddHeroBuyHardCurrency()
	{
		Singleton<PlayerInfoScript>.Instance.AddHardCurrency2(0, MiscParams.TutorialHero.BuyCost, "tutorial hero purchase", -1, string.Empty);
	}

	private void FacebookLogin()
	{
		AdvanceTutorialState();
	}

	private void DeclineFacebookLogin()
	{
		AdvanceTutorialState();
	}

	private void NameEntry()
	{
		if (string.IsNullOrEmpty(Singleton<PlayerInfoScript>.Instance.SaveData.MultiplayerPlayerName))
		{
			StartCoroutine(NameEntryCo());
		}
		else
		{
			AdvanceTutorialState();
		}
	}

	private IEnumerator NameEntryCo(Action then = null)
	{
		PlayerSaveData saveData = Singleton<PlayerInfoScript>.Instance.SaveData;
		string mpName = string.Empty;
		bool popupOpen3 = false;
		while (true)
		{
			popupOpen3 = true;
			Singleton<SimplePopupController>.Instance.ShowInputNoCancel(KFFLocalization.Get("!!MY_NAME_IS"), delegate
			{
				mpName = Singleton<SimplePopupController>.Instance.GetInputValue();
				popupOpen3 = false;
			});
			while (popupOpen3)
			{
				yield return null;
			}
			if (!IsValidPlayerName(mpName))
			{
				yield return StartCoroutine(ShowMessageCoroutine(KFFLocalization.Get("!!NAME_REQUIRED"), KFFLocalization.Get("!!NAME_REQUIRED_MESSAGE")));
				continue;
			}
			popupOpen3 = true;
			bool nameConfirmed = false;
			Singleton<SimplePopupController>.Instance.ShowPrompt(string.Empty, KFFLocalization.Get("!!CONFIRM_NAME").Replace("<val1>", mpName), delegate
			{
				nameConfirmed = true;
				popupOpen3 = false;
			}, delegate
			{
				nameConfirmed = false;
				popupOpen3 = false;
			});
			while (popupOpen3)
			{
				yield return null;
			}
			if (!nameConfirmed)
			{
				continue;
			}
			if (ProfanityFilterDataManager.Instance.ContainsProfanity(mpName))
			{
				yield return StartCoroutine(ShowMessageCoroutine(string.Empty, KFFLocalization.Get("!!NAME_PROFANITY_ERROR")));
				continue;
			}
			saveData.MultiplayerPlayerName = mpName;
			saveData.HasAuthenticated = true;
			Singleton<BusyIconPanelController>.Instance.Show();
			ResponseFlag lastResponse = ResponseFlag.None;
			global::Multiplayer.Multiplayer.CreateMultiplayerUser(SessionManager.Instance.theSession, Singleton<PlayerInfoScript>.Instance, delegate(MultiplayerData data, ResponseFlag response)
			{
				lastResponse = response;
			});
			while (lastResponse == ResponseFlag.None)
			{
				yield return null;
			}
			Singleton<BusyIconPanelController>.Instance.Hide();
			if (lastResponse == ResponseFlag.Success)
			{
				break;
			}
			yield return StartCoroutine(ShowMessageCoroutine(string.Empty, KFFLocalization.Get("!!MP_CREATE_ERROR_MESSAGE")));
		}
		Singleton<PlayerInfoScript>.Instance.Save();
		if (then != null)
		{
			then();
		}
		else
		{
			AdvanceTutorialState();
		}
	}

	private bool IsValidPlayerName(string name)
	{
		if (string.IsNullOrEmpty(name))
		{
			return false;
		}
		if (name.Length > 16)
		{
			return false;
		}
		return true;
	}

	private IEnumerator ShowMessageCoroutine(string title, string message)
	{
		bool popupOpen = true;
		Singleton<SimplePopupController>.Instance.ShowMessage(title, message, delegate
		{
			popupOpen = false;
		});
		while (popupOpen)
		{
			yield return null;
		}
	}

	public bool PauseToShowCard()
	{
		if (!IsStateActive("IN_DrawFloop"))
		{
			return InConditionalState("Q1C_HeroCard");
		}
		return true;
	}

	private void AddReviveHardCurrency()
	{
		Singleton<PlayerInfoScript>.Instance.AddHardCurrency2(0, MiscParams.ReviveCost, "tutorial revive", -1, string.Empty);
	}

	private void ZoomToLab()
	{
		Singleton<TownController>.Instance.GetBuildingObject("TBuilding_Lab").GetComponent<TownBuildingScript>().SendMessage("OnClick");
	}

	private void SetPlayerIDString()
	{
		mPopupTextReplacement = Singleton<PlayerInfoScript>.Instance.GetFormattedPlayerCode();
	}

	private void CameraToP1()
	{
		Singleton<DWGameCamera>.Instance.RenderP1Character(render: true);
		Singleton<DWGameCamera>.Instance.MoveCameraToP1Winner();
	}

	private void CameraToP2()
	{
		Singleton<DWGameCamera>.Instance.MoveCameraToP2Setup();
	}

	private void CheckCardOnHand()
	{
		foreach (CardData item in Singleton<DWGame>.Instance.GetHand(PlayerType.User))
		{
			DetachedSingleton<ConditionalTutorialController>.Instance.OnCardDrawn(item);
		}
	}

	private void ShowBattleHudDeployTween()
	{
		Singleton<BattleHudController>.Instance.ShowDeployTween.Play();
	}

	private void ShowHud()
	{
		Singleton<HandCardController>.Instance.ShowHand();
		Singleton<HandCardController>.Instance.ShowOpponentTween.Play();
		Singleton<BattleHudController>.Instance.ShowHudAfterInfoPopupTween.Play();
		Singleton<DWGameCamera>.Instance.MoveCameraToP1Setup();
	}

	private void AgeEntry()
	{
		StartCoroutine(AgeEntryCo());
	}

	private IEnumerator AgeEntryCo()
	{
		bool done = false;
		Singleton<SimplePopupController>.Instance.ShowDateEntryPrompt(KFFLocalization.Get("!!AGE_ENTRY_PROMPT") + "\n \n \n \n \n", delegate
		{
			done = true;
		});
		while (!done)
		{
			yield return null;
		}
		uint dateOfBirth = (uint)(Singleton<SimplePopupController>.Instance.GetEnteredDate() + TFUtils.GetServerTimeDiff()).Subtract(new DateTime(1950, 1, 1)).TotalSeconds;
		Singleton<PlayerInfoScript>.Instance.SaveData.DateOfBirth = dateOfBirth;
		AdvanceTutorialState();
	}

	private void RemoveAwakenedCreature()
	{
		Singleton<EvoScreenController>.Instance.RemoveCreature(destroy: true);
	}
}
