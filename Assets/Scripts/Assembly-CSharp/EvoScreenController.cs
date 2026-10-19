using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EvoScreenController : Singleton<EvoScreenController>
{
	public enum ScreenState
	{
		NULL = 0,
		EMPTY = 1,
		SHARD = 2,
		AWAKEN = 3,
		PASSIVE_UP = 4
	}

	[Header("General")]
	public BoxCollider EvoSlot;

	public GameObject EvoSlotSprite;

	public GameObject ShardSlotSprite;

	public BoxCollider ResultSlot;

	public UILabel SoftCurrencyLabel;

	public UILabel HardCurrencyLabel;

	public UILabel EvolutionShardsLabel;

	public UILabel CostLabel;

	public GameObject EvoPrepCreatureNameGroup;

	public UILabel EvoPrepCreatureName;

	public Transform RecipeNodeParent;

	public Transform[] RecipeNodes;

	public GameObject RecipeGroup;

	public GameObject QuestionObj;

	public GameObject QuestionVFX;

	private GameObject mQuestionVFXObj;

	public Transform SortButton;

	public CreatureStatsPanel StatsPanel;

	public GameObject StatsPanelRoot;

	public GameObject PreviewMonitorRoot;

	public GameObject AwakenMaterialsParent;

	public GameObject PassiveUpParent;

	public Transform PassiveUpTileSlot;

	public UILabel PassiveUpMatQuantity;

	public UILabel PassiveUpBonusQuantity;

	public UILabel PassiveName;

	public UILabel PassiveDescription;

	public UISprite PassiveExpBar;

	public UILabel PassiveExpAmount;

	public UILabel PassiveCostLabel;

	public GameObject PassiveUpVFXSpawnParent;

	public GameObject PassiveShardSpawnNode;

	public Transform PassiveShardDestinationNode;

	public AnimationCurve PassivePopInCurve;

	public AnimationCurve PassiveEaseInOutCurve;

	public AnimationCurve ModelPuffUpCurve;

	public AnimationCurve EaseInOutCurve;

	public GameObject ShardParent;

	public BoxCollider ShardSlot;

	public GameObject ShardResultParent;

	public GameObject ShardResultNode;

	public UILabel ShardResultQuantity;

	public GameObject ShardSequenceRoot;

	public List<GameObject> ShardRewardSpawnPoints;

	public List<GameObject> ShardRewardMovers;

	public LaboratorySequence RevealSequenceScript;

	public UIStreamingGrid CreatureGrid;

	public InventoryBarController inventoryBar;

	public Transform[] CreatureSpawnPoints;

	public Transform[] CreatureScalers;

	public GameObject VFX_UI_Awaken_Prefab;

	public GameObject AwakenFXPoint;

	public GameObject VFX_Shard_Burst_Prefab;

	public GameObject VFX_Shard_Burst_PassiveUp_Prefab;

	public GameObject VFX_Inventory_Pulse;

	public GameObject shardTarget;

	[Header("Debug Button")]
	public bool testSequence;

	public bool PlayFlashTween;

	[Header("Highlights")]
	public GameObject[] SlotHighlights = new GameObject[5];

	public GameObject[] PlatformLights = new GameObject[5];

	public GameObject[] PlatformRightLights = new GameObject[5];

	[Header("Evo Materials")]
	public Material FlashMaterial;

	public Material BlackMaterial;

	public Material DarkBlueMaterial;

	[Header("Cameras")]
	public Camera CreatureCamera_Masked;

	public Camera CreatureCamera_Full;

	public Camera CreatureCameraAfter_Masked;

	public Camera CreatureCameraAfter_Full;

	public Camera CreatureCameraAfter_Monitor;

	[Header("Tweens")]
	public UITweenController ShowTween;

	public UITweenController HideTween;

	public UITweenController ShowPlatformTween;

	public UITweenController ShowShardPlatformTween;

	public UITweenController HideShardPlatformTween;

	public UITweenController ShowSlotsTween;

	public UITweenController HideSlotsTween;

	public UITweenController CantAffordTween;

	public UITweenController ShowBlackoutTween;

	public UITweenController HideBlackoutTween;

	public UITweenController WhiteoutFlashTween;

	public UITweenController ShowGuages;

	public UITweenController HideGuages;

	public UITweenController ResetForSequenceTween;

	public UITweenController MovePlatformsForAwaken;

	public UITweenController MovePlatformsAndPop;

	public UITweenController MovePlatformsAndSwap;

	public UITweenController ShakeStartLoopTween;

	public UITweenController ShakeStopTween;

	public UITweenController ShowStatsPanel;

	public UITweenController HideStatsPanel;

	public UITweenController ShowMonitorPanel;

	public UITweenController HideMonitorPanel;

	public UITweenController ShowScaleRightCreature;

	public UITweenController HideScaleRightCreature;

	public UITweenController ShardShowResultCounterTween;

	public UITweenController ShardHideResultCounterTween;

	public UITweenController ShardSequenceStartTween;

	public UITweenController ShardSequenceEndTween;

	public UITweenController ShardInvPopTween;

	public List<UITweenController> ShardPopRewardTween;

	public UITweenController PlatformLightsPinkTween;

	public UITweenController PlatformLightsGreenTween;

	[Header("Evolution Shards")]
	public GameObject EvolutionShardsPanel;

	public UILabel EvolutionShardsCostLabel;

	public UILabel EvolutionShardsOwnedLabel;

	public UIButton EvolutionShardsButton;

	public UILabel EvolutionShardsButtonLabel;

	private int mMaxPassiveFeeds;

	private int mCurrentPassiveFeeds;

	private int mBuyingGoldPacks;

	private GameObject mEgg;

	private InventoryTile mShardCreature;

	private InventoryTile mEvoCreature;

	private InventoryTile mEvoResultCreature;

	private InventoryTile[] mRecipeEvoMaterials = new InventoryTile[5];

	private UIStreamingGridDataSource<InventorySlotItem> mCreatureGridDataSource = new UIStreamingGridDataSource<InventorySlotItem>();

	private List<GameObject> mCreatureModelInstances = new List<GameObject>();

	private ScreenState currentScreenState = ScreenState.EMPTY;

	private ScreenState nextScreenState;

	private ScreenState queuedScereenState;

	private bool isTransitionOutComplete;

	private bool isTransitionInComplete;

	private bool isShardSequence;

	private bool mFollowFlag;

	private GameObject mEggBoneToFollow;

	private Quaternion mEvoCamOriginalRot;

	private Vector3 mEvoCamOriginalPos;

	public UITweenController HideMainTween;

	public InventoryTile GetEvoCreature()
	{
		return mEvoCreature;
	}

	private void SwitchState(ScreenState nextScreen)
	{
		if (nextScreenState == ScreenState.NULL)
		{
			StartCoroutine(HandleStateSwitch(nextScreen));
		}
		else if (queuedScereenState != nextScreen)
		{
			queuedScereenState = nextScreen;
		}
		else
		{
			queuedScereenState = ScreenState.NULL;
		}
	}

	private IEnumerator HandleStateSwitch(ScreenState nextScreen)
	{
		nextScreenState = nextScreen;
		queuedScereenState = ScreenState.NULL;
		AwakenFXPoint.SetActive(value: false);
		if (currentScreenState != nextScreenState)
		{
			isTransitionOutComplete = false;
			if (currentScreenState == ScreenState.EMPTY)
			{
				if (nextScreenState == ScreenState.SHARD)
				{
					isTransitionOutComplete = true;
				}
				else
				{
					ShardSlot.gameObject.SetActive(value: false);
					HideShardPlatformTween.PlayWithCallback(OnTransitionOutComplete);
				}
			}
			else if (currentScreenState == ScreenState.SHARD)
			{
				if (nextScreenState == ScreenState.EMPTY)
				{
					if (isShardSequence)
					{
						isTransitionOutComplete = true;
					}
					else
					{
						isShardSequence = true;
						ShardHideResultCounterTween.PlayWithCallback(OnTransitionOutComplete);
					}
				}
				else
				{
					ShardSlotSprite.SetActive(value: false);
					ShardSlot.gameObject.SetActive(value: false);
					ShardHideResultCounterTween.Play();
					HideShardPlatformTween.PlayWithCallback(OnTransitionOutComplete);
				}
			}
			else if (currentScreenState == ScreenState.AWAKEN)
			{
				HideSlotsTween.Play();
				HideMonitorPanel.PlayWithCallback(OnTransitionOutComplete);
			}
			else if (currentScreenState == ScreenState.PASSIVE_UP)
			{
				HideStatsPanel.PlayWithCallback(OnTransitionOutComplete);
			}
			while (!isTransitionOutComplete)
			{
				yield return null;
			}
			isTransitionInComplete = false;
			if (nextScreenState == ScreenState.EMPTY)
			{
				PlatformLightsGreenTween.StopAndReset();
				ResetPlatformRightLights(turnOn: false);
				ShardSlotSprite.SetActive(value: true);
				ShardSlot.gameObject.SetActive(value: true);
				if (currentScreenState == ScreenState.SHARD)
				{
					isTransitionInComplete = true;
				}
				else
				{
					ShowShardPlatformTween.PlayWithCallback(OnTransitionInComplete);
				}
			}
			else if (nextScreenState == ScreenState.SHARD)
			{
				ShardSlot.gameObject.SetActive(value: true);
				ShardShowResultCounterTween.PlayWithCallback(OnTransitionInComplete);
			}
			else if (nextScreenState == ScreenState.AWAKEN)
			{
				AwakenMaterialsParent.SetActive(value: true);
				ShowSlotsTween.Play();
				ShowMonitorPanel.PlayWithCallback(OnTransitionInComplete);
			}
			else if (nextScreenState == ScreenState.PASSIVE_UP)
			{
				ShowStatsPanel.PlayWithCallback(OnTransitionInComplete);
			}
			while (!isTransitionInComplete)
			{
				yield return null;
			}
			if (nextScreenState == ScreenState.EMPTY)
			{
				DisplayModelCamerasOff();
			}
			else if (nextScreenState == ScreenState.SHARD)
			{
				DisplayModelCamerasShard();
			}
			else if (nextScreenState == ScreenState.AWAKEN)
			{
				DisplayModelCamerasMonitor();
			}
			else if (nextScreenState == ScreenState.PASSIVE_UP)
			{
				DisplayModelCameraPassive();
			}
		}
		currentScreenState = nextScreenState;
		nextScreenState = ScreenState.NULL;
		if (queuedScereenState != ScreenState.NULL)
		{
			SwitchState(queuedScereenState);
		}
		UpdateScreenStateVisbility();
	}

	private void OnTransitionOutComplete()
	{
		isTransitionOutComplete = true;
	}

	private void OnTransitionInComplete()
	{
		isTransitionInComplete = true;
	}

	private void UpdateScreenStateVisbility()
	{
		if (currentScreenState != ScreenState.SHARD)
		{
			ShardResultParent.SetActive(value: false);
		}
	}

	public void Populate()
	{
		if (Singleton<TutorialController>.Instance.IsBlockActive("Evo"))
		{
			Singleton<TutorialController>.Instance.AddMaterialsForEvo();
		}
		ShowTween.Play();
		InventoryBarController.onDoFilterNull();
		InventoryBarController.onDoFilter += PopulateCreatureList;
		inventoryBar.SetFilters(creatureFilterActive: false, cakeFilterActive: true, cardFilterActive: true, ingredientFilterActive: false, shardFilterActive: false);
		RefreshCurrency();
		InventoryTile.ResetForNewScreen();
		RecipeGroup.SetActive(value: false);
		base.gameObject.GetComponent<InventoryBarController>().UpdateInventoryCounter();
		CreatureGrid.transform.parent.GetComponent<UIScrollView>().ResetPosition();
		CreatureGrid.transform.parent.GetComponent<UIScrollView>().UpdateScrollbars();
		currentScreenState = ScreenState.EMPTY;
		nextScreenState = ScreenState.NULL;
		queuedScereenState = ScreenState.NULL;
		AwakenMaterialsParent.SetActive(value: false);
		PassiveUpParent.SetActive(value: false);
		PlatformLightsGreenTween.StopAndReset();
		ResetPlatformRightLights(turnOn: false);
		ResetForSequenceTween.Play();
		ShowPlatformTween.Play();
		ShowShardPlatformTween.Play();
		ShardResultParent.SetActive(value: false);
		ShardParent.SetActive(value: true);
		ShardSlotSprite.SetActive(value: true);
		SetShardCreature(null);
		if (AwakenFXPoint.transform.childCount < 1)
		{
			AwakenFXPoint.InstantiateAsChild(VFX_UI_Awaken_Prefab);
		}
		AwakenFXPoint.SetActive(value: false);
	}

	public void Unload()
	{
		mCreatureGridDataSource.Clear();
		if (mEvoCreature != null)
		{
			NGUITools.Destroy(mEvoCreature.gameObject);
			mEvoCreature = null;
			if (mEvoResultCreature != null)
			{
				NGUITools.Destroy(mEvoResultCreature.gameObject);
			}
			mEvoResultCreature = null;
		}
		ClearRecipe();
		UnloadModel();
		EvoSlotSprite.SetActive(value: true);
		QuestionObj.SetActive(value: false);
		if (mQuestionVFXObj != null)
		{
			mQuestionVFXObj.SetActive(value: false);
		}
		PassiveUpTileSlot.DestroyAllChildren();
	}

	private void Update()
	{
		RefreshCurrency();
		if (testSequence)
		{
			testSequence = false;
			StartCoroutine(AwakenSequence());
		}
		if (PlayFlashTween)
		{
			PlayFlashTween = false;
			WhiteoutFlashTween.Play();
		}
	}

	public void PopulateCreatureList()
	{
		InventoryTile.SetDelegates(InventorySlotType.None, TileDraggable, OnTileDragBeginFromSlot, OnTileDropped, RefreshTileOverlay, PopulateCreatureList, OnPopupClosed);
		Singleton<PlayerInfoScript>.Instance.SaveData.SortInventory(InventorySlotType.EvoMaterial);
		mCreatureGridDataSource.Init(CreatureGrid, Singleton<PrefabReferences>.Instance.InventoryTile, inventoryBar.GetFilteredInventory());
		base.gameObject.GetComponent<InventoryBarController>().ResetScrollView();
	}

	private void OnPopupClosed(InventorySlotItem creature)
	{
		PopulateCreatureList();
	}

	private bool TileDraggable(InventoryTile tile)
	{
		if (tile.InventoryItem.SlotType != InventorySlotType.Creature)
		{
			return false;
		}
		if (tile.IsAttachedToTarget())
		{
			return true;
		}
		if (!InventoryTile.IsUpwardsDrag())
		{
			return false;
		}
		if (IsCreatureSelected(tile) || IsCreatureSelectedToShard(tile))
		{
			return false;
		}
		return true;
	}

	private void OnTileDragBeginFromSlot(InventoryTile tile)
	{
		if (tile.AssignedSlot == 0)
		{
			RemoveCreature(destroy: false);
		}
		else if (tile.AssignedSlot == 3)
		{
			ShardResultNode.transform.DestroyAllChildren();
			RemoveCreature(destroy: false);
		}
	}

	private bool OnTileDropped(InventoryTile tile, int slotIndex)
	{
		switch (slotIndex)
		{
		case 0:
			if (!tile.InventoryItem.Creature.CanEverEvo())
			{
				Singleton<SimplePopupController>.Instance.ShowMessage(string.Empty, KFFLocalization.Get("!!NO_AWAKEN_MESSAGE"));
				return false;
			}
			if (mShardCreature != null)
			{
				SetShardCreature(null);
			}
			SetCreature(tile);
			return true;
		case 3:
			if (tile.InventoryItem.Creature.Form.TurnsIntoMaterial == null)
			{
				Singleton<SimplePopupController>.Instance.ShowMessage(string.Empty, KFFLocalization.Get("!!NO_SHARD_MESSAGE"));
				return false;
			}
			if (tile.InventoryItem.InUse() || tile.InventoryItem.IsHelper() || tile.InventoryItem.IsFavorite())
			{
				return false;
			}
			SetShardCreature(tile);
			return true;
		default:
			return false;
		}
	}

	private void RefreshTileOverlay(InventoryTile tile)
	{
		if (tile.InventoryItem.SlotType == InventorySlotType.Creature)
		{
			if (IsCreatureSelected(tile))
			{
				tile.SetOverlayStatus(InventoryTile.TileStatus.SelectedAsTarget, fadeIfInUse: false);
				return;
			}
			if (IsCreatureSelectedToShard(tile))
			{
				tile.SetOverlayStatus(InventoryTile.TileStatus.SelectedAsMaterial, fadeIfInUse: false);
				return;
			}
			if (tile.InventoryItem.Creature.CanEverEvo() || tile.InventoryItem.Creature.Form.TurnsIntoMaterial != null)
			{
				tile.SetOverlayStatus(InventoryTile.TileStatus.NotSelected, fadeIfInUse: false);
				return;
			}
			tile.ClearOverlays();
			tile.FadeGroup.SetActive(value: true);
		}
		else if (tile.InventoryItem.SlotType == InventorySlotType.EvoMaterial)
		{
			if (tile.EvoMatch == InventoryTile.EvoMatchType.NotSet)
			{
				if (IsEvoMatInSelectedRecipe(tile.InventoryItem))
				{
					tile.SetOverlayStatus(InventoryTile.TileStatus.SelectedAsMaterial, fadeIfInUse: false);
				}
				else
				{
					tile.SetOverlayStatus(InventoryTile.TileStatus.NotSelected, fadeIfInUse: false);
				}
			}
			else if (tile.EvoMatch == InventoryTile.EvoMatchType.Good)
			{
				tile.SetOverlayStatus(InventoryTile.TileStatus.SelectedAsMaterial, fadeIfInUse: false);
				tile.FadeGroup.SetActive(value: false);
			}
			else if (tile.EvoMatch == InventoryTile.EvoMatchType.NoMatch)
			{
				tile.SetOverlayStatus(InventoryTile.TileStatus.SelectedButUnavailable, fadeIfInUse: false);
			}
		}
		else
		{
			tile.ClearOverlays();
			tile.FadeGroup.SetActive(value: true);
		}
	}

	private void RefreshCurrency()
	{
		if (SoftCurrencyLabel.gameObject.activeInHierarchy)
		{
			SoftCurrencyLabel.text = Singleton<PlayerInfoScript>.Instance.SaveData.SoftCurrency.ToString();
			HardCurrencyLabel.text = Singleton<PlayerInfoScript>.Instance.SaveData.HardCurrency.ToString();
			EvolutionShardsLabel.text = Singleton<PlayerInfoScript>.Instance.SaveData.EvolutionShards.ToString();
		}
	}

	public void SetCreature(InventoryTile creature)
	{
		PassiveUpTileSlot.DestroyAllChildren();
		if (mEvoCreature != null)
		{
			NGUITools.Destroy(mEvoCreature.gameObject);
		}
		if (mEvoResultCreature != null)
		{
			NGUITools.Destroy(mEvoResultCreature.gameObject);
		}
		mEvoCreature = creature;
		mEvoCreature.transform.parent = EvoSlot.transform;
		mEvoCreature.transform.localPosition = Vector3.zero;
		mEvoCreature.AssignedSlot = 0;
		EvoSlotSprite.SetActive(value: false);
		mEvoCreature.MatchToCollider(EvoSlot);
		StartCoroutine(PopulateStatPanelDelay(creature.InventoryItem, 0.3f));
		if (creature.InventoryItem.Creature.CanEverEvo())
		{
			PassiveUpParent.SetActive(value: false);
			RecipeGroup.SetActive(value: true);
			List<CreatureData> list = new List<CreatureData>();
			list.Add(creature.InventoryItem.Creature.Form);
			GameObject gameObject = ResultSlot.transform.InstantiateAsChild(Singleton<PrefabReferences>.Instance.InventoryTile);
			mEvoResultCreature = gameObject.GetComponent<InventoryTile>();
			mEvoResultCreature.AssignedSlot = 2;
			mEvoResultCreature.Populate(CreatureDataManager.Instance.GetData(creature.InventoryItem.Creature.Form.EvolvesTo));
			list.Add(mEvoResultCreature.InventoryItem.DisplayCreature);
			StartCoroutine(LoadModelForPrepEvoCo(list, forEvo: true));
			QuestionObj.SetActive(value: true);
			PopulateSelectedCreature(forEvo: true);
			Singleton<TutorialController>.Instance.AdvanceIfOnState("EV_DragEvoTarget");
			SwitchState(ScreenState.AWAKEN);
		}
		else
		{
			StartCoroutine(LoadModelForPrepEvoCo(new List<CreatureData> { mEvoCreature.InventoryItem.Creature.Form }, forEvo: false));
			PopulatePassiveInfo();
			SwitchState(ScreenState.PASSIVE_UP);
		}
	}

	private void PopulatePassiveInfo()
	{
		AwakenMaterialsParent.SetActive(value: false);
		PassiveUpParent.SetActive(value: true);
		PopulateSelectedCreature(forEvo: false);
		InventorySlotItem dataObj = new InventorySlotItem(mEvoCreature.InventoryItem.Creature.Form.PassiveUpMaterial);
		InventoryTile component = PassiveUpTileSlot.InstantiateAsChild(Singleton<PrefabReferences>.Instance.InventoryTile).GetComponent<InventoryTile>();
		component.gameObject.ChangeLayer(PassiveUpTileSlot.gameObject.layer);
		component.gameObject.GetComponent<Collider>().enabled = false;
		component.Populate(dataObj);
		mCurrentPassiveFeeds = 0;
		CalculateMaxPassiveFeeds();
		UpdatePassiveInfo();
	}

	private void CalculateMaxPassiveFeeds()
	{
		mMaxPassiveFeeds = Singleton<PlayerInfoScript>.Instance.SaveData.FindAllEvoMaterials((InventorySlotItem m) => m.EvoMaterial == mEvoCreature.InventoryItem.Creature.Form.PassiveUpMaterial).Count;
	}

	public IEnumerator PopulateStatPanelDelay(InventorySlotItem invSlotItem, float numSec)
	{
		yield return new WaitForSeconds(numSec);
		StatsPanel.Populate(invSlotItem);
		mEvoCreature.MatchToCollider(EvoSlot);
	}

	public void RemoveCreature(bool destroy)
	{
		EvoSlotSprite.SetActive(value: true);
		if (mEvoCreature != null)
		{
			if (destroy)
			{
				NGUITools.Destroy(mEvoCreature.gameObject);
			}
			else
			{
				mEvoCreature.MatchToCollider(null);
			}
			mEvoCreature = null;
		}
		if (mShardCreature != null)
		{
			if (destroy)
			{
				NGUITools.Destroy(mShardCreature.gameObject);
			}
			else
			{
				mShardCreature.MatchToCollider(null);
			}
			mShardCreature = null;
		}
		if (mEvoResultCreature != null)
		{
			NGUITools.Destroy(mEvoResultCreature.gameObject);
			mEvoResultCreature = null;
		}
		PassiveUpTileSlot.DestroyAllChildren();
		ClearRecipe();
		UnloadModel();
		QuestionObj.SetActive(value: false);
		if (mQuestionVFXObj != null)
		{
			mQuestionVFXObj.SetActive(value: false);
		}
		AwakenMaterialsParent.SetActive(value: false);
		PassiveUpParent.SetActive(value: false);
		SwitchState(ScreenState.EMPTY);
	}

	private void ClearRecipe()
	{
		for (int i = 0; i < mRecipeEvoMaterials.Length; i++)
		{
			if (mRecipeEvoMaterials[i] != null)
			{
				NGUITools.Destroy(mRecipeEvoMaterials[i].gameObject);
				mRecipeEvoMaterials[i] = null;
			}
		}
	}

	private void PopulateSelectedCreature(bool forEvo)
	{
		Debug.Log($"PopulateSelectedCreature called with forEvo = {forEvo}");
		ClearRecipe();
		CreatureData creatureData = mEvoCreature.InventoryItem.Creature.Form;
		EvoPrepCreatureName.text = creatureData.Name;
		if (!forEvo)
		{
			EvolutionShardsPanel.SetActive(value: false);
			return;
		}
		bool flag = mEvoCreature.InventoryItem.Creature.CanEverEvo();
		Debug.Log($"Creature {creatureData.Name} can evolve: {flag}");
		if (flag)
		{
			Debug.Log("Creature can evolve, enabling Evolution Shards panel");
			EvolutionShardsPanel.SetActive(value: true);
			UpdateEvolutionShardsUI();
		}
		else
		{
			Debug.Log("Creature cannot evolve, hiding Evolution Shards panel");
			EvolutionShardsPanel.SetActive(value: false);
		}
		CostLabel.text = creatureData.EvolveCost.ToString();
		if (creatureData.EvolveCost > Singleton<PlayerInfoScript>.Instance.SaveData.SoftCurrency)
		{
			CantAffordTween.Play();
		}
		else
		{
			CantAffordTween.StopAndReset();
		}
		for (int i = 0; i < 5; i++)
		{
			InventorySlotItem inventorySlotItem = Singleton<PlayerInfoScript>.Instance.SaveData.FindEvoMaterial((InventorySlotItem m) => m.EvoMaterial == creatureData.AwakenMaterial && !IsEvoMatInSelectedRecipe(m));
			InventoryTile.EvoMatchType evoMatch;
			if (inventorySlotItem == null)
			{
				inventorySlotItem = new InventorySlotItem(EvoMaterialDataManager.Instance.GetData(creatureData.AwakenMaterial.ID));
				evoMatch = InventoryTile.EvoMatchType.NoMatch;
			}
			else
			{
				evoMatch = InventoryTile.EvoMatchType.Good;
			}
			RecipeNodes[i].transform.SetLocalScaleX(1f);
			RecipeNodes[i].transform.SetLocalScaleY(1f);
			GameObject gameObject = RecipeNodes[i].InstantiateAsChild(Singleton<PrefabReferences>.Instance.InventoryTile);
			gameObject.ChangeLayer(RecipeNodes[i].gameObject.layer);
			gameObject.GetComponent<Collider>().enabled = false;
			RecipeNodes[i].transform.SetLocalScaleX(0.7f);
			RecipeNodes[i].transform.SetLocalScaleY(0.7f);
			mRecipeEvoMaterials[i] = gameObject.GetComponent<InventoryTile>();
			mRecipeEvoMaterials[i].AssignedSlot = 1;
			mRecipeEvoMaterials[i].EvoMatch = evoMatch;
			mRecipeEvoMaterials[i].Populate(inventorySlotItem);
		}
	}

	public bool IsCreatureSelected(InventoryTile creature)
	{
		if (mEvoCreature != null)
		{
			return mEvoCreature.InventoryItem.Creature == creature.InventoryItem.Creature;
		}
		return false;
	}

	public bool IsCreatureSelectedToShard(InventoryTile creature)
	{
		if (mShardCreature != null)
		{
			return mShardCreature.InventoryItem.Creature == creature.InventoryItem.Creature;
		}
		return false;
	}

	public bool IsEvoMatInSelectedRecipe(InventorySlotItem evoMaterial)
	{
		InventoryTile[] array = mRecipeEvoMaterials;
		foreach (InventoryTile inventoryTile in array)
		{
			if (inventoryTile != null && inventoryTile.EvoMatch == InventoryTile.EvoMatchType.Good && inventoryTile.InventoryItem == evoMaterial)
			{
				return true;
			}
		}
		return false;
	}

	public void OnClickEvo()
	{
		if (!mEvoCreature.InventoryItem.Creature.CanEverEvo())
		{
			OnClickPassiveFeed();
			return;
		}
		if (mEvoCreature == null)
		{
			string body = KFFLocalization.Get("!!ERROR_EVO_NO_CREATURE_SELECTED");
			Singleton<SimplePopupController>.Instance.ShowMessage(string.Empty, body);
			return;
		}
		PlayerSaveData saveData = Singleton<PlayerInfoScript>.Instance.SaveData;
		CreatureItem creature = mEvoCreature.InventoryItem.Creature;
		if (saveData.SoftCurrency < creature.Form.EvolveCost)
		{
			Singleton<PlayerInfoScript>.Instance.CalculateGoldPacksNeeded(creature.Form.EvolveCost, out mBuyingGoldPacks, out var totalGold, out var totalGemCost);
			string confirmString = string.Format(KFFLocalization.Get("!!ERROR_EVO_NOT_ENOUGH_CURRENCY_BUY"), creature.Form.EvolveCost, totalGold);
			string insufficientString = string.Format(KFFLocalization.Get("!!ERROR_EVO_NOT_ENOUGH_CURRENCY_NOBUY"), creature.Form.EvolveCost);
			Singleton<SimplePopupController>.Instance.ShowPurchasePrompt(confirmString, insufficientString, totalGemCost, OnClickConfirmBuyGold);
			return;
		}
		InventoryTile[] array = mRecipeEvoMaterials;
		foreach (InventoryTile inventoryTile in array)
		{
			if (!(inventoryTile == null) && inventoryTile.EvoMatch != InventoryTile.EvoMatchType.Good)
			{
				string body2 = KFFLocalization.Get("!!ERROR_EVO_DONT_HAVE_ALL_CREATURE");
				Singleton<SimplePopupController>.Instance.ShowMessage(string.Empty, body2);
				return;
			}
		}
		saveData.SoftCurrency -= creature.Form.EvolveCost;
		List<EvoMaterialData> list = new List<EvoMaterialData>();
		array = mRecipeEvoMaterials;
		foreach (InventoryTile inventoryTile2 in array)
		{
			if (inventoryTile2 != null)
			{
				list.Add(inventoryTile2.InventoryItem.EvoMaterial);
			}
		}
		saveData.RemoveEvoMaterials(list);
		creature.Evolve();
		StartCoroutine(AwakenSequence());
		Singleton<PlayerInfoScript>.Instance.Save();
		Singleton<PlayerInfoScript>.Instance.UpdateBadgeCount(BadgeEnum.Evo);
		mEvoCreature.Populate(mEvoCreature.InventoryItem);
		if (mEvoResultCreature != null)
		{
			NGUITools.Destroy(mEvoResultCreature.gameObject);
			mEvoResultCreature = null;
		}
		ClearRecipe();
		RefreshCurrency();
		inventoryBar.UpdateInventoryCounter();
	}

	private void TurnOffHighlights()
	{
		for (int i = 0; i < SlotHighlights.Length; i++)
		{
			SlotHighlights[i].SetActive(value: false);
		}
		for (int j = 0; j < PlatformLights.Length; j++)
		{
			PlatformLights[j].SetActive(value: false);
		}
	}

	private void OnClickConfirmBuyGold()
	{
		Singleton<PlayerInfoScript>.Instance.BuyGold(mBuyingGoldPacks, delegate
		{
			if (mEvoCreature.InventoryItem.Creature.Form.EvolveCost <= Singleton<PlayerInfoScript>.Instance.SaveData.SoftCurrency)
			{
				CantAffordTween.StopAndReset();
			}
		});
	}

	public void ResetEvoResult()
	{
		mEvoResultCreature = null;
		PopulateCreatureList();
		UnloadModel();
		Singleton<EvoStatsPanelController>.Instance.HideStats();
		Singleton<EvoResultController>.Instance.HideCreatureBackgroundTween.Play();
		ShowTween.Play();
		Singleton<EvoResultController>.Instance.HideTween.Play();
		EvoSlotSprite.SetActive(value: true);
		QuestionObj.SetActive(value: false);
		if (mQuestionVFXObj != null)
		{
			mQuestionVFXObj.SetActive(value: false);
		}
		mFollowFlag = false;
	}

	public void DisplayModelCameraPassive()
	{
		CreatureCamera_Masked.gameObject.SetActive(value: true);
		CreatureCamera_Full.gameObject.SetActive(value: false);
		CreatureCameraAfter_Masked.gameObject.SetActive(value: false);
		CreatureCameraAfter_Full.gameObject.SetActive(value: false);
		CreatureCameraAfter_Monitor.gameObject.SetActive(value: false);
		CreatureScalers[0].localScale = Vector3.one;
		CreatureScalers[1].localScale = Vector3.zero;
	}

	public void DisplayModelCamerasMonitor()
	{
		PreviewMonitorRoot.SetActive(value: true);
		CreatureCamera_Masked.gameObject.SetActive(value: true);
		CreatureCamera_Full.gameObject.SetActive(value: false);
		CreatureCameraAfter_Masked.gameObject.SetActive(value: false);
		CreatureCameraAfter_Full.gameObject.SetActive(value: false);
		CreatureCameraAfter_Monitor.gameObject.SetActive(value: true);
		CreatureScalers[0].localScale = Vector3.one;
		ShowScaleRightCreature.Play();
		if (mCreatureModelInstances.Count > 1 && mCreatureModelInstances[1] != null)
		{
			CreatureScalers[1].localScale = Vector3.one;
			DarkenCreature(mCreatureModelInstances[1], inShouldUseBlue: false);
		}
		AwakenFXPoint.SetActive(value: true);
	}

	public void DisplayModelCamerasSequence()
	{
		CreatureCamera_Masked.gameObject.SetActive(value: false);
		CreatureCamera_Full.gameObject.SetActive(value: true);
		CreatureCameraAfter_Masked.gameObject.SetActive(value: false);
		CreatureCameraAfter_Full.gameObject.SetActive(value: true);
		CreatureCameraAfter_Monitor.gameObject.SetActive(value: false);
		CreatureScalers[0].localScale = Vector3.one;
		ShowScaleRightCreature.Play();
		AwakenFXPoint.SetActive(value: true);
	}

	public void DisplayModelCamerasPostSequence()
	{
		CreatureCamera_Masked.gameObject.SetActive(value: true);
		CreatureCamera_Full.gameObject.SetActive(value: false);
		CreatureCameraAfter_Masked.gameObject.SetActive(value: false);
		CreatureCameraAfter_Full.gameObject.SetActive(value: true);
		CreatureCameraAfter_Monitor.gameObject.SetActive(value: false);
		CreatureScalers[0].localScale = Vector3.zero;
		CreatureScalers[1].localScale = Vector3.one;
	}

	public void DisplayModelCamerasShard()
	{
		CreatureCamera_Masked.gameObject.SetActive(value: false);
		CreatureCamera_Full.gameObject.SetActive(value: false);
		CreatureCameraAfter_Masked.gameObject.SetActive(value: true);
		CreatureCameraAfter_Full.gameObject.SetActive(value: false);
		CreatureCameraAfter_Monitor.gameObject.SetActive(value: false);
		CreatureScalers[0].localScale = Vector3.zero;
		CreatureScalers[1].localScale = Vector3.one;
	}

	public void DisplayModelCamerasOff()
	{
		CreatureCamera_Masked.gameObject.SetActive(value: false);
		CreatureCamera_Full.gameObject.SetActive(value: false);
		CreatureCameraAfter_Masked.gameObject.SetActive(value: false);
		CreatureCameraAfter_Full.gameObject.SetActive(value: false);
		CreatureCameraAfter_Monitor.gameObject.SetActive(value: false);
		CreatureScalers[0].localScale = Vector3.zero;
		CreatureScalers[1].localScale = Vector3.zero;
	}

	private IEnumerator AwakenSequence()
	{
		ResetForSequenceTween.Play();
		DarkenCreature(mCreatureModelInstances[1]);
		CreatureScalers[1].localScale = Vector3.zero;
		if (PreviewMonitorRoot.activeInHierarchy)
		{
			HideMonitorPanel.PlayWithCallback(DisplayModelCamerasSequence);
			HideScaleRightCreature.Play();
		}
		else
		{
			DisplayModelCamerasSequence();
		}
		if (StatsPanelRoot.activeInHierarchy)
		{
			HideStatsPanel.Play();
		}
		List<UITexture> list = new List<UITexture>();
		InventoryTile[] array = mRecipeEvoMaterials;
		foreach (InventoryTile inventoryTile in array)
		{
			if (inventoryTile != null)
			{
				list.Add(inventoryTile.PortraitTexture);
			}
		}
		_ = mEvoCreature.InventoryItem.Creature;
		for (int j = 0; j < CreatureSpawnPoints.Length; j++)
		{
			CreatureSpawnPoints[j].transform.localRotation = Quaternion.Euler(new Vector3(0f, 180f, 0f));
		}
		HideGuages.Play();
		HideSlotsTween.Play();
		MovePlatformsForAwaken.Play();
		CreatureScalers[1].localScale = Vector3.zero;
		DisplayModelCamerasSequence();
		LaboratorySequence.onElectricStart += OnElectricStart;
		LaboratorySequence.onSequencePop += OnSequencePop;
		LaboratorySequence.onSequenceComplete += OnClickCloseResult;
		TurnOffHighlights();
		PlatformLightsGreenTween.StopAndReset();
		ResetPlatformRightLights(turnOn: true);
		PlatformLightsPinkTween.Play();
		if (RevealSequenceScript == null)
		{
			RevealSequenceScript = base.gameObject.GetComponentInChildren<LaboratorySequence>();
		}
		if (RevealSequenceScript != null)
		{
			RevealSequenceScript.StartModuleSequence(list);
		}
		else
		{
			OnClickCloseResult();
		}
		yield return new WaitForSeconds(0.5f);
		ClearRecipe();
	}

	public void OnElectricStart()
	{
		TurnOffHighlights();
		ShakeStartLoopTween.Play();
		AwakenFXPoint.SetActive(value: false);
	}

	public void OnSequencePop()
	{
		TurnOffHighlights();
		ShakeStartLoopTween.End();
		ShakeStopTween.Play();
		StartCoroutine(FlashCreatureCo());
		StartCoroutine(WhiteFlashCo());
		MovePlatformsAndPop.Play();
	}

	public IEnumerator FlashCreatureCo()
	{
		yield return new WaitForSeconds(0.01f);
		DarkenCreature(mCreatureModelInstances[0]);
		FlashCreature(mCreatureModelInstances[1]);
		yield return new WaitForSeconds(0.08f);
		UnFlashCreature(mCreatureModelInstances[0]);
		DarkenCreature(mCreatureModelInstances[1]);
		yield return new WaitForSeconds(0.07f);
		DarkenCreature(mCreatureModelInstances[0]);
		FlashCreature(mCreatureModelInstances[1]);
		yield return new WaitForSeconds(0.08f);
		UnFlashCreature(mCreatureModelInstances[0]);
		DarkenCreature(mCreatureModelInstances[1]);
		yield return new WaitForSeconds(0.1f);
		DarkenCreature(mCreatureModelInstances[0]);
		FlashCreature(mCreatureModelInstances[1]);
		yield return new WaitForSeconds(0.13f);
		UnFlashCreature(mCreatureModelInstances[0]);
		DarkenCreature(mCreatureModelInstances[1]);
		yield return new WaitForSeconds(0.3f);
		DarkenCreature(mCreatureModelInstances[0]);
		FlashCreature(mCreatureModelInstances[1]);
		yield return new WaitForSeconds(0.13f);
		UnFlashCreature(mCreatureModelInstances[0]);
		UnFlashCreature(mCreatureModelInstances[1]);
	}

	public IEnumerator WhiteFlashCo()
	{
		yield return new WaitForSeconds(0.85f);
		WhiteoutFlashTween.Play();
	}

	public void OnClickCloseResult()
	{
		StartCoroutine(ShowMainAfterAwaken());
	}

	private IEnumerator ShowMainAfterAwaken()
	{
		MovePlatformsAndSwap.PlayWithCallback(SwapToMaskedCamera);
		TurnOffHighlights();
		PlatformLightsGreenTween.StopAndReset();
		ResetPlatformRightLights(turnOn: false);
		PopulateCreatureList();
		PopulatePassiveInfo();
		StatsPanel.Populate(mEvoCreature.InventoryItem);
		SwitchState(ScreenState.PASSIVE_UP);
		UICamera.UnlockInput();
		yield return null;
	}

	public void SwapToMaskedCamera()
	{
		DisplayModelCamerasPostSequence();
		ResetForSequenceTween.Play();
		mCreatureModelInstances[1].transform.SetParent(CreatureSpawnPoints[0]);
		mCreatureModelInstances[1].transform.localPosition = mCreatureModelInstances[0].transform.localPosition;
		mCreatureModelInstances[1].transform.localRotation = mCreatureModelInstances[0].transform.localRotation;
		mCreatureModelInstances[1].transform.localScale = mCreatureModelInstances[0].transform.localScale;
		mCreatureModelInstances[0].SetActive(value: false);
	}

	public void FlashCreature(GameObject creatureObject)
	{
		BattleCreatureAnimState componentInChildren = creatureObject.GetComponentInChildren<BattleCreatureAnimState>();
		if (componentInChildren.orignalMeshes == null)
		{
			return;
		}
		foreach (SkinnedMeshRenderer orignalMesh in componentInChildren.orignalMeshes)
		{
			orignalMesh.material = FlashMaterial;
		}
	}

	public void UnFlashCreature(GameObject creatureObject)
	{
		BattleCreatureAnimState componentInChildren = creatureObject.GetComponentInChildren<BattleCreatureAnimState>();
		for (int i = 0; i < componentInChildren.orignalMeshes.Count; i++)
		{
			componentInChildren.orignalMeshes[i].material = componentInChildren.originalMats[i];
		}
	}

	private void DarkenCreature(GameObject creatureObject, bool inShouldUseBlue = true)
	{
		Material material = ((!inShouldUseBlue) ? BlackMaterial : DarkBlueMaterial);
		BattleCreatureAnimState componentInChildren = creatureObject.GetComponentInChildren<BattleCreatureAnimState>();
		if (componentInChildren.orignalMeshes != null)
		{
			foreach (SkinnedMeshRenderer orignalMesh in componentInChildren.orignalMeshes)
			{
				orignalMesh.material = material;
			}
		}
		if (componentInChildren.PropsRenderer != null && componentInChildren.PropsBlackoutMaterial != null)
		{
			componentInChildren.PropsRenderer.material = componentInChildren.PropsBlackoutMaterial;
		}
	}

	private IEnumerator LoadMonitorOnMonitor()
	{
		yield return 0;
		DisplayModelCamerasMonitor();
		yield return new WaitForSeconds(0.2f);
		if (mCreatureModelInstances.Count > 1 && mCreatureModelInstances[1] != null)
		{
			CreatureScalers[1].localScale = Vector3.one;
			DarkenCreature(mCreatureModelInstances[1], inShouldUseBlue: false);
		}
	}

	private IEnumerator EvoSequence(CreatureItem creature)
	{
		Singleton<TutorialController>.Instance.AdvanceIfOnState("EV_PerformEvo");
		yield break;
	}

	private Vector3 PowerUpCreatureScale(float formHeight)
	{
		float num = Mathf.Max(4f, formHeight);
		float num2 = Mathf.Min(1f, 5f / num);
		return Vector3.one * num2;
	}

	public IEnumerator LoadModelForPrepEvoCo(List<CreatureData> creatures, bool forEvo)
	{
		UnloadModel();
		List<CreatureData> creatures2 = creatures;
		for (int i = 0; i < CreatureSpawnPoints.Length; i++)
		{
			Transform spawnPoint = CreatureSpawnPoints[i];
			GameObject creatureObj = null;
			yield return StartCoroutine(Singleton<SLOTResourceManager>.Instance.LoadCreatureResources(creatures[i], delegate(GameObject objData, Texture2D texture)
			{
				creatureObj = spawnPoint.InstantiateAsChild(objData);
				creatures2[i].SwapCreatureTexture(creatureObj, texture);
				creatureObj.transform.localScale = PowerUpCreatureScale(creatures2[i].Height);
				mCreatureModelInstances.Add(creatureObj);
				if (i == 1)
				{
					CreatureScalers[1].localScale = Vector3.zero;
					if (Singleton<TutorialController>.Instance.IsBlockActive("Evo"))
					{
						PreviewMonitorRoot.SetActive(value: false);
					}
					if (!creatures2[i].AlreadyCollected)
					{
						if (mQuestionVFXObj == null)
						{
							mQuestionVFXObj = spawnPoint.InstantiateAsChild(QuestionVFX);
							_ = (UILabel)(mQuestionVFXObj.GetComponent<VFXRenderQueueSorter>().mTarget = QuestionObj.GetComponent<UILabel>());
						}
						else
						{
							mQuestionVFXObj.SetActive(value: true);
						}
					}
					else if (mQuestionVFXObj != null)
					{
						mQuestionVFXObj.SetActive(value: false);
					}
					if (nextScreenState == ScreenState.NULL)
					{
						StartCoroutine(LoadMonitorOnMonitor());
					}
				}
			}));
			if (!forEvo)
			{
				break;
			}
		}
	}

	public void UnloadModel(int atIndex = -1)
	{
		int num = 0;
		int num2 = mCreatureModelInstances.Count;
		if (atIndex != -1 && atIndex < num2)
		{
			num = atIndex;
			num2 = 1;
		}
		for (int i = num; i < num2; i++)
		{
			if (mCreatureModelInstances[i] != null)
			{
				UnityEngine.Object.Destroy(mCreatureModelInstances[i]);
				mCreatureModelInstances[i] = null;
			}
		}
		mCreatureModelInstances.Clear();
		Resources.UnloadUnusedAssets();
	}

	public IEnumerator LoadModelForSharding(CreatureData creatureData)
	{
		UnloadModel();
		Transform spawnPoint = CreatureSpawnPoints[1];
		GameObject creatureObj = null;
		CreatureData creatureData2 = creatureData;
		yield return StartCoroutine(Singleton<SLOTResourceManager>.Instance.LoadCreatureResources(creatureData, delegate(GameObject objData, Texture2D texture)
		{
			spawnPoint.transform.localRotation = Quaternion.Euler(new Vector3(0f, 180f, 0f));
			spawnPoint.transform.localPosition = new Vector3(0f, -84f, 0f);
			CreatureScalers[1].localPosition = new Vector3(214f, -209f, 0f);
			creatureObj = spawnPoint.InstantiateAsChild(objData);
			creatureData2.SwapCreatureTexture(creatureObj, texture);
			creatureObj.transform.localScale = PowerUpCreatureScale(creatureData2.Height);
			mCreatureModelInstances = new List<GameObject>();
			mCreatureModelInstances.Add(null);
			mCreatureModelInstances.Add(creatureObj);
		}));
	}

	public void OnClickSort()
	{
		Singleton<SortPopupController>.Instance.Show(SortPopupController.Category.Creatures, SortButton, PopulateCreatureList);
	}

	public GameObject GetTutorialEvoCreature()
	{
		InventorySlotItem dataItem = Singleton<PlayerInfoScript>.Instance.SaveData.FindCreature((InventorySlotItem m) => m.Creature.GivenForEvoTutorial);
		return mCreatureGridDataSource.FindPrefab(dataItem);
	}

	public GameObject GetNewestCreature()
	{
		InventorySlotItem newestCreature = Singleton<PlayerInfoScript>.Instance.SaveData.GetNewestCreature();
		return mCreatureGridDataSource.FindPrefab(newestCreature);
	}

	public void OnClickBack()
	{
		SwitchState(ScreenState.EMPTY);
		HideScaleRightCreature.Play();
		HideTween.Play();
	}

	public void OnClickBackToTown()
	{
		SwitchState(ScreenState.EMPTY);
		HideScaleRightCreature.Play();
		Singleton<MouseOrbitCamera>.Instance.CheckTiltCamSettingBeforeTutorial();
		HideTween.Play();
		Singleton<LabBuildingController>.Instance.Hide();
	}

	public void OnClickPassiveUpArrow()
	{
		OnClickPassiveArrow(1);
	}

	public void OnClickPassiveDownArrow()
	{
		OnClickPassiveArrow(-1);
	}

	private void OnClickPassiveArrow(int direction)
	{
		mCurrentPassiveFeeds += direction;
		mCurrentPassiveFeeds = Mathf.Clamp(mCurrentPassiveFeeds, 0, mMaxPassiveFeeds);
		UpdatePassiveInfo();
	}

	private void UpdatePassiveInfo()
	{
		PassiveUpMatQuantity.text = mCurrentPassiveFeeds.ToString();
		int num = BonusPassiveFeeds();
		if (num > 0)
		{
			PassiveUpBonusQuantity.gameObject.SetActive(value: true);
			PassiveUpBonusQuantity.text = KFFLocalization.Get("!!PLUS_BONUS").Replace("<val1>", num.ToString());
			PassiveUpMatQuantity.transform.localPosition = new Vector3(-128f, -194f, 0f);
			PassiveUpMatQuantity.height = 38;
		}
		else
		{
			PassiveUpBonusQuantity.gameObject.SetActive(value: false);
			PassiveUpMatQuantity.transform.localPosition = new Vector3(-128f, -205f, 0f);
			PassiveUpMatQuantity.height = 44;
		}
		CalculatePassiveGains(out var feedsGained, out var levelsGained);
		CreatureItem creature = mEvoCreature.InventoryItem.Creature;
		int overrideLevel = creature.PassiveSkillLevel + levelsGained;
		PassiveName.text = creature.PassiveLevelDetailFormatString(overrideLevel);
		PassiveDescription.text = creature.BuildPassiveDescriptionString(isBattle: false, overrideLevel);
		if (creature.Form.PassiveData != null && creature.Form.PassiveData.FeedsPerLevel > 0)
		{
			int feedsPerLevel = creature.Form.PassiveData.FeedsPerLevel;
			int num2 = ((int)creature.PassiveFeeds + feedsGained) % feedsPerLevel;
			PassiveExpAmount.text = num2 + "/" + feedsPerLevel;
			PassiveExpBar.fillAmount = (float)num2 / (float)feedsPerLevel;
			PassiveCostLabel.text = (mCurrentPassiveFeeds * creature.Form.CostPerPassiveFeed).ToString();
		}
	}

	private int BonusPassiveFeeds()
	{
		int num = mCurrentPassiveFeeds / 5;
		if (num > 0)
		{
			DateTime serverTime = TFUtils.ServerTime;
			if (MiscParams.AwakenEventStartDate != DateTime.MinValue && MiscParams.AwakenEventEndDate != DateTime.MinValue && serverTime >= MiscParams.AwakenEventStartDate && serverTime < MiscParams.AwakenEventEndDate)
			{
				return num * (1 + MiscParams.AwakenEventBonus);
			}
			return num;
		}
		return 0;
	}

	private void CalculatePassiveGains(out int feedsGained, out int levelsGained)
	{
		CreatureItem creature = mEvoCreature.InventoryItem.Creature;
		feedsGained = mCurrentPassiveFeeds + BonusPassiveFeeds();
		if (creature.Form.PassiveData != null && creature.Form.PassiveData.FeedsPerLevel > 0)
		{
			int num = (int)creature.PassiveFeeds + creature.PassiveSkillLevel * creature.Form.PassiveData.FeedsPerLevel;
			int num2 = creature.Form.PassiveData.FeedsPerLevel * creature.Form.PassiveData.MaxLevel;
			feedsGained = Mathf.Min(feedsGained, num2 - num);
			levelsGained = ((int)creature.PassiveFeeds + feedsGained) / creature.Form.PassiveData.FeedsPerLevel;
		}
		else
		{
			levelsGained = 0;
		}
	}

	public void OnClickPassiveFeed()
	{
		StartCoroutine(PassiveFeedCo());
	}

	private IEnumerator PassiveFeedCo()
	{
		if (mCurrentPassiveFeeds == 0)
		{
			yield break;
		}
		PlayerSaveData saveData = Singleton<PlayerInfoScript>.Instance.SaveData;
		CreatureItem creature = mEvoCreature.InventoryItem.Creature;
		int num = mCurrentPassiveFeeds * creature.Form.CostPerPassiveFeed;
		if (saveData.SoftCurrency < num)
		{
			Singleton<PlayerInfoScript>.Instance.CalculateGoldPacksNeeded(num, out mBuyingGoldPacks, out var totalGold, out var totalGemCost);
			string confirmString = string.Format(KFFLocalization.Get("!!ERROR_POWERUP_NOT_ENOUGH_CURRENCY_BUY"), num, totalGold);
			string insufficientString = string.Format(KFFLocalization.Get("!!ERROR_POWERUP_NOT_ENOUGH_CURRENCY_NOBUY"), num);
			Singleton<SimplePopupController>.Instance.ShowPurchasePrompt(confirmString, insufficientString, totalGemCost, OnClickConfirmBuyGold);
			yield break;
		}
		saveData.SoftCurrency -= num;
		List<EvoMaterialData> list = new List<EvoMaterialData>(mCurrentPassiveFeeds);
		for (int i = 0; i < mCurrentPassiveFeeds; i++)
		{
			list.Add(creature.Form.PassiveUpMaterial);
		}
		saveData.RemoveEvoMaterials(list);
		CalculatePassiveGains(out var feedsGained, out var _);
		creature.GrantPassiveFeeds(feedsGained);
		StartCoroutine(SpawnPassiveUpShardsCo());
		mCurrentPassiveFeeds = 0;
		CalculateMaxPassiveFeeds();
		UpdatePassiveInfo();
	}

	private IEnumerator SpawnPassiveUpShardsCo()
	{
		int numShards = mCurrentPassiveFeeds;
		TweenScale creatureTweenScale2 = null;
		if (mCreatureModelInstances.Count > 0)
		{
			GameObject gameObject = mCreatureModelInstances[0];
			creatureTweenScale2 = gameObject.GetComponent<TweenScale>();
			if (creatureTweenScale2 == null)
			{
				creatureTweenScale2 = gameObject.AddComponent<TweenScale>();
			}
			creatureTweenScale2.from = gameObject.transform.localScale;
			creatureTweenScale2.to = gameObject.transform.localScale * 1.01f;
			creatureTweenScale2.duration = 0.2f;
			creatureTweenScale2.animationCurve = ModelPuffUpCurve;
			creatureTweenScale2.enabled = false;
		}
		yield return new WaitForSeconds(0.2f);
		List<GameObject> shards = new List<GameObject>();
		for (int i = 0; i < numShards; i++)
		{
			GameObject gameObject2 = PassiveShardSpawnNode.InstantiateAsChild(Singleton<PrefabReferences>.Instance.InventoryTile);
			gameObject2.ChangeLayerToParent();
			gameObject2.transform.localPosition = new Vector3(i * 27, i * 40, 0f);
			gameObject2.SetActive(value: true);
			shards.Add(gameObject2);
			InventoryTile component = gameObject2.GetComponent<InventoryTile>();
			component.AdjustDepth((i + 1) * 10);
			CreatureData form = mEvoCreature.InventoryItem.Creature.Form;
			component.Populate(new InventorySlotItem(form.TurnsIntoMaterial));
			TweenScale tweenScale = gameObject2.AddComponent<TweenScale>();
			tweenScale.duration = 0.2f;
			tweenScale.from = Vector3.one * 0.4f;
			tweenScale.to = Vector3.one * 0.8f;
			tweenScale.PlayForward();
			TweenPosition tweenPosition = gameObject2.AddComponent<TweenPosition>();
			tweenPosition.duration = 0.3f;
			tweenPosition.delay = 0.3f;
			tweenPosition.from = gameObject2.transform.localPosition;
			tweenPosition.to = PassiveShardDestinationNode.localPosition;
			tweenPosition.animationCurve = PassiveEaseInOutCurve;
			tweenPosition.AddOnFinished(new EventDelegate(delegate
			{
				PassiveShardDestinationNode.InstantiateAsChild(VFX_Shard_Burst_PassiveUp_Prefab);
				Singleton<SLOTAudioManager>.Instance.PlaySound("ui/SFX_Shards");
				UnityEngine.Object.Destroy(shards[0]);
				shards.RemoveAt(0);
				creatureTweenScale2.enabled = true;
				creatureTweenScale2.ResetToBeginning();
				creatureTweenScale2.PlayForward();
			}));
			tweenPosition.PlayForward();
			yield return new WaitForSeconds(0.15f);
		}
	}

	private void SetShardCreature(InventoryTile creature)
	{
		if (mShardCreature != null)
		{
			NGUITools.Destroy(mShardCreature.gameObject);
			mShardCreature = null;
		}
		ShardResultNode.transform.DestroyAllChildren();
		if (creature != null)
		{
			mShardCreature = creature;
			mShardCreature.transform.parent = ShardSlot.transform;
			mShardCreature.transform.localPosition = Vector3.zero;
			mShardCreature.AssignedSlot = 3;
			ShardSlotSprite.SetActive(value: false);
			creature.MatchToCollider(ShardSlot);
			GameObject obj = ShardResultNode.InstantiateAsChild(Singleton<PrefabReferences>.Instance.InventoryTile);
			obj.ChangeLayerToParent();
			CreatureData form = mShardCreature.InventoryItem.Creature.Form;
			InventoryTile component = obj.GetComponent<InventoryTile>();
			component.AssignedSlot = 3;
			component.AdjustDepth(1);
			component.Populate(new InventorySlotItem(form.TurnsIntoMaterial));
			ShardResultQuantity.text = "x" + form.TurnsIntoMaterialCount;
			StartCoroutine(LoadModelForSharding(form));
			ResetPlatformRightLights(turnOn: true);
			PlatformLightsGreenTween.Play();
			SwitchState(ScreenState.SHARD);
		}
		else
		{
			UnloadModel();
			SwitchState(ScreenState.EMPTY);
		}
	}

	private void ResetPlatformRightLights(bool turnOn)
	{
		GameObject[] platformRightLights = PlatformRightLights;
		for (int i = 0; i < platformRightLights.Length; i++)
		{
			platformRightLights[i].SetActive(turnOn);
		}
	}

	public void OnClickShard()
	{
		StartCoroutine(ShardCreatureCo());
	}

	private IEnumerator ShardCreatureCo()
	{
		if (mShardCreature == null)
		{
			yield break;
		}
		isShardSequence = true;
		CreatureData creatureData = mShardCreature.InventoryItem.Creature.Form;
		string body = KFFLocalization.Get("!!SHARD_CREATURE_CONFIRM").Replace("<val1>", creatureData.Name);
		int choice = -1;
		Singleton<SimplePopupController>.Instance.ShowPrompt(string.Empty, body, delegate
		{
			choice = 1;
		}, delegate
		{
			choice = 0;
		});
		while (true)
		{
			switch (choice)
			{
			case -1:
				yield return null;
				continue;
			case 0:
				yield break;
			}
			PlayerSaveData saveData = Singleton<PlayerInfoScript>.Instance.SaveData;
			int uniqueId = mShardCreature.InventoryItem.Creature.UniqueId;
			saveData.RemoveCreature(mShardCreature.InventoryItem);
			PlayerSaveData.BeginGrant();
			for (int num = 0; num < creatureData.TurnsIntoMaterialCount; num++)
			{
				saveData.AddEvoMaterial(creatureData.TurnsIntoMaterial);
			}
			saveData.ClaimCurrency("convert", creatureData.ID + ":" + uniqueId, 0, 0, 0, 0, null, PlayerSaveData.EndGrant());
			NGUITools.Destroy(mShardCreature.gameObject);
			mShardCreature = null;
			ShardSequenceRoot.SetActive(value: true);
			for (int num2 = 0; num2 < ShardRewardMovers.Count; num2++)
			{
				ShardRewardMovers[num2].SetActive(value: true);
				ShardRewardMovers[num2].transform.localPosition = Vector3.zero;
				ShardRewardMovers[num2].transform.localRotation = Quaternion.Euler(Vector3.zero);
				ShardRewardMovers[num2].transform.localScale = Vector3.one;
			}
			ShardHideResultCounterTween.Play();
			ShardSequenceStartTween.Play();
			Singleton<SLOTAudioManager>.Instance.PlaySound("gacha/SFX_AuraLoop");
			yield return new WaitForSeconds(0.2f);
			int numRewards = creatureData.TurnsIntoMaterialCount;
			GameObject rewardObj = ShardResultNode.transform.GetChild(0).gameObject;
			rewardObj.name = "Source Shard(clone)";
			for (int i = 0; i < numRewards; i++)
			{
				GameObject obj = ShardRewardSpawnPoints[i].InstantiateAsChild(rewardObj);
				obj.SetActive(value: true);
				obj.name = "Flying Shard(clone)";
				obj.ChangeLayerToParent();
				ShardPopRewardTween[i].Play();
				obj.InstantiateAsChild(VFX_Shard_Burst_Prefab);
				Singleton<SLOTAudioManager>.Instance.PlaySound("ui/SFX_Shards");
				yield return new WaitForSeconds(0.2f);
			}
			yield return new WaitForSeconds(0.5f);
			rewardObj.SetActive(value: false);
			ShardSequenceEndTween.Play();
			StartCoroutine(InvPopCo(numRewards));
			yield return new WaitForSeconds(0.8f);
			Singleton<SLOTAudioManager>.Instance.StopSound("SFX_AuraLoop");
			SetShardCreature(null);
			PopulateCreatureList();
			yield break;
		}
	}

	private IEnumerator InvPopCo(int numPops)
	{
		yield return new WaitForSeconds(0.2f);
		for (int i = 0; i < numPops; i++)
		{
			ShardInvPopTween.StopAndReset();
			ShardInvPopTween.Play();
			shardTarget.InstantiateAsChild(VFX_Inventory_Pulse);
			yield return new WaitForSeconds(0.1f);
		}
		yield return new WaitForSeconds(1.2f);
		foreach (GameObject shardRewardSpawnPoint in ShardRewardSpawnPoints)
		{
			if (shardRewardSpawnPoint.transform.childCount > 0)
			{
				UnityEngine.Object.Destroy(shardRewardSpawnPoint.transform.GetChild(0).gameObject);
			}
		}
	}

	public bool CanEvolveWithShards(CreatureItem creature)
	{
		return creature?.CanCurrentlyEvo() ?? false;
	}

	public int GetEvolutionShardCost(CreatureItem creature)
	{
		if (creature == null)
		{
			return 0;
		}
		return creature.Form.Rarity switch
		{
			1 => 1, 
			2 => 1, 
			3 => 1, 
			_ => 1, 
		};
	}

	private void UpdateEvolutionShardsUI()
	{
		Debug.Log("UpdateEvolutionShardsUI called");
		if (mEvoCreature == null)
		{
			Debug.Log("mEvoCreature is null, hiding panel");
			EvolutionShardsPanel.SetActive(value: false);
			return;
		}
		bool flag = mEvoCreature.InventoryItem.Creature.CanEverEvo();
		Debug.Log($"Creature {mEvoCreature.InventoryItem.Creature.Form.Name} CanEverEvo: {flag}");
		if (!flag)
		{
			Debug.Log("Creature cannot ever evolve, hiding panel");
			EvolutionShardsPanel.SetActive(value: false);
			return;
		}
		Debug.Log("Setting EvolutionShardsPanel to active");
		EvolutionShardsPanel.SetActive(value: true);
		int evolutionShardCost = GetEvolutionShardCost(mEvoCreature.InventoryItem.Creature);
		int evolutionShards = Singleton<PlayerInfoScript>.Instance.SaveData.EvolutionShards;
		Debug.Log($"Evolution Shards - Cost: {evolutionShardCost}, Owned: {evolutionShards}");
		EvolutionShardsCostLabel.text = evolutionShardCost.ToString();
		EvolutionShardsOwnedLabel.text = evolutionShards.ToString();
		bool flag2 = evolutionShards >= evolutionShardCost;
		EvolutionShardsButton.isEnabled = flag2;
		if (flag2)
		{
			EvolutionShardsButtonLabel.text = KFFLocalization.Get("!!EVOLVE_WITH_SHARDS");
			EvolutionShardsButtonLabel.color = Color.white;
		}
		else
		{
			EvolutionShardsButtonLabel.text = KFFLocalization.Get("!!NOT_ENOUGH_SHARDS");
			EvolutionShardsButtonLabel.color = Color.red;
		}
		Debug.Log($"EvolutionShardsPanel.activeInHierarchy: {EvolutionShardsPanel.activeInHierarchy}");
	}

	public void OnClickEvolveWithShards()
	{
		if (mEvoCreature == null)
		{
			Singleton<SimplePopupController>.Instance.ShowMessage(string.Empty, KFFLocalization.Get("!!ERROR_EVO_NO_CREATURE_SELECTED"));
			return;
		}
		CreatureItem creature = mEvoCreature.InventoryItem.Creature;
		int evolutionShardCost = GetEvolutionShardCost(creature);
		if (Singleton<PlayerInfoScript>.Instance.SaveData.EvolutionShards < evolutionShardCost)
		{
			Singleton<SimplePopupController>.Instance.ShowMessage(string.Empty, KFFLocalization.Get("!!NOT_ENOUGH_EVOLUTION_SHARDS"));
			return;
		}
		string body = KFFLocalization.Get("!!CONFIRM_EVOLVE_WITH_SHARDS").Replace("<creature>", creature.Form.Name).Replace("<cost>", evolutionShardCost.ToString());
		Singleton<SimplePopupController>.Instance.ShowPrompt(string.Empty, body, ConfirmEvolutionShardsEvolution, null, KFFLocalization.Get("!!EVOLVE"), KFFLocalization.Get("!!CANCEL"));
	}

	private void ConfirmEvolutionShardsEvolution()
	{
		CreatureItem creature = mEvoCreature.InventoryItem.Creature;
		int evolutionShardCost = GetEvolutionShardCost(creature);
		Singleton<PlayerInfoScript>.Instance.SaveData.EvolutionShards -= evolutionShardCost;
		creature.Evolve();
		StartCoroutine(AwakenSequence());
		Singleton<PlayerInfoScript>.Instance.Save();
		Singleton<PlayerInfoScript>.Instance.UpdateBadgeCount(BadgeEnum.Evo);
		mEvoCreature.Populate(mEvoCreature.InventoryItem);
		if (mEvoResultCreature != null)
		{
			NGUITools.Destroy(mEvoResultCreature.gameObject);
			mEvoResultCreature = null;
		}
		ClearRecipe();
		RefreshCurrency();
		inventoryBar.UpdateInventoryCounter();
		UpdateEvolutionShardsUI();
	}
}
