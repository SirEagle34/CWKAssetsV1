using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreatureDetailsController : Singleton<CreatureDetailsController>
{
	public delegate void ClosedDelegate(InventorySlotItem creature);

	public int NoGemsPanelShrinkage;

	[Header("Awaken Preview Materials")]
	public Material DarkBlueMaterial;

	public Material BlackMaterial;

	public Camera PreviewCamera;

	public GameObject PreviewMonitorPanel;

	[Header("Awaken Preview")]
	public GameObject PreviewAwakenButton;

	public Transform AwakenPreviewSpawnPoint;

	public GameObject VFX_UI_Awaken_Prefab;

	[Header("Awaken Preview Exclusions")]
	[Tooltip("List of creature IDs that should NOT show awaken preview")]
	public List<string> ExcludedCreatureIDs = new List<string> { "CoolDoggo_Awaken" };

	private GameObject mAwakenPreviewModel;

	private GameObject mAwakenFXPoint;

	private bool mShowingAwakenPreview;

	public UITweenController ShowTween;

	public UITweenController HideTween;

	public UITweenController ShowCardsTween;

	public UITweenController ShowCreatureTween;

	public UITweenController HideCreatureTween;

	public UITweenController ShowPassiveTween;

	public UITweenController HidePassiveTween;

	public GameObject MainPanel;

	public CreatureStatsPanel StatsPanel;

	public Transform MainModelParent;

	public Transform SpawnNodeScaler;

	public UIStreamingGrid MainCreatureGrid;

	private UIStreamingGridDataSource<InventorySlotItem> mMainCreatureGridDataSource = new UIStreamingGridDataSource<InventorySlotItem>();

	public Transform SkillCardNodeParent;

	public Collider ZoomCollider;

	public Transform ZoomPosition;

	public GameObject GemsParent;

	public Transform StatsPanelTransform;

	public Transform StatsButtonTransform;

	public Transform WeightButtonTransform;

	public UISprite StatsPanelBackground;

	public GameObject FavoriteParent;

	public GameObject FavoriteCheckmark;

	public GameObject ExCardsParent;

	public Camera CreatureCamera;

	private InventorySlotItem mCreature;

	private GameObject mLoadedModelInstance;

	private bool isFirstTimeShow;

	private GameObject[] SkillCardNodes = new GameObject[6];

	private List<CardPrefabScript> mSpawnedActionCards = new List<CardPrefabScript>();

	public Transform[] ExtraCardSlots = new Transform[4];

	public GameObject[] ExtraCardSlotLocks = new GameObject[4];

	private List<CardPrefabScript> mSpawnedExCards = new List<CardPrefabScript>();

	private ClosedDelegate mClosedDelegate;

	private Vector3 mBaseStatsPanelPos;

	private int mBaseStatsPanelBGHeight;

	private bool mEnableCardEditing;

	private bool mZoomSoundPending;

	private bool isCreatureModelShown;

	private bool isActionCardShown;

	private string shownActionCardName;

	private bool isClosing;

	private readonly Dictionary<UIPanel, int> mLiftedPanels = new Dictionary<UIPanel, int>();

	public void Awake()
	{
		mBaseStatsPanelPos = StatsPanelTransform.localPosition;
		mBaseStatsPanelBGHeight = StatsPanelBackground.height;
		for (int i = 0; i < 6; i++)
		{
			SkillCardNodes[i] = SkillCardNodeParent.Find("ActionCardSpawn_0" + (i + 1)).gameObject;
		}
	}

	public void Update()
	{
		if (!isCreatureModelShown)
		{
			bool flag = true;
			if (isActionCardShown)
			{
				flag = false;
			}
			if (Singleton<SimplePopupController>.Instance.MainPanel.activeInHierarchy)
			{
				flag = false;
			}
			if (DetachedSingleton<SceneFlowManager>.Instance.InFrontEnd() && Singleton<CardEquipController>.Instance.MainPanel.activeInHierarchy)
			{
				flag = false;
			}
			if (isClosing)
			{
				flag = false;
			}
			if (flag)
			{
				ShowCreatureModel();
			}
		}
		else
		{
			AdjustCameraDepth();
		}
	}

	private void DarkenCreature(GameObject creatureObject, bool inShouldUseBlue = true)
	{
		Material material = ((!inShouldUseBlue) ? BlackMaterial : DarkBlueMaterial);
		Renderer[] componentsInChildren = creatureObject.GetComponentsInChildren<Renderer>();
		Debug.Log($"Found {componentsInChildren.Length} total renderers to darken");
		Renderer[] array = componentsInChildren;
		foreach (Renderer renderer in array)
		{
			if (renderer != null)
			{
				Debug.Log("Darkening renderer: " + renderer.name);
				renderer.material = material;
			}
		}
		BattleCreatureAnimState componentInChildren = creatureObject.GetComponentInChildren<BattleCreatureAnimState>();
		if (componentInChildren?.orignalMeshes == null)
		{
			return;
		}
		foreach (SkinnedMeshRenderer orignalMesh in componentInChildren.orignalMeshes)
		{
			if (orignalMesh != null)
			{
				orignalMesh.material = material;
			}
		}
	}

	private void RestoreCreatureMaterials(GameObject creatureObject)
	{
		BattleCreatureAnimState componentInChildren = creatureObject.GetComponentInChildren<BattleCreatureAnimState>();
		if (componentInChildren.orignalMeshes == null)
		{
			return;
		}
		for (int i = 0; i < componentInChildren.orignalMeshes.Count; i++)
		{
			if (i < componentInChildren.originalMats.Count)
			{
				componentInChildren.orignalMeshes[i].material = componentInChildren.originalMats[i];
			}
		}
	}

	private void LiftAboveOtherPanels()
	{
		RestorePanelDepths();
		UIPanel[] componentsInChildren = GetComponentsInChildren<UIPanel>(includeInactive: true);
		if (componentsInChildren.Length == 0)
		{
			return;
		}
		HashSet<UIPanel> hashSet = new HashSet<UIPanel>(componentsInChildren);
		int num = int.MaxValue;
		int num2 = int.MinValue;
		int num3 = int.MinValue;
		UIPanel[] array = componentsInChildren;
		foreach (UIPanel uIPanel in array)
		{
			num = Mathf.Min(num, uIPanel.depth);
			num2 = Mathf.Max(num2, uIPanel.depth);
		}
		foreach (UIPanel item in UIPanel.list)
		{
			if (!(item == null) && !hashSet.Contains(item) && item.gameObject.activeInHierarchy && item.depth < 100)
			{
				num3 = Mathf.Max(num3, item.depth);
			}
		}
		if (num3 < num)
		{
			return;
		}
		int num4 = Mathf.Min(num3 + 1 - num, 99 - num2);
		if (num4 > 0)
		{
			array = componentsInChildren;
			foreach (UIPanel uIPanel2 in array)
			{
				mLiftedPanels[uIPanel2] = uIPanel2.depth;
				uIPanel2.depth += num4;
			}
		}
	}

	private void RestorePanelDepths()
	{
		foreach (KeyValuePair<UIPanel, int> mLiftedPanel in mLiftedPanels)
		{
			if (mLiftedPanel.Key != null)
			{
				mLiftedPanel.Key.depth = mLiftedPanel.Value;
			}
		}
		mLiftedPanels.Clear();
	}

	public void ShowCreature(InventorySlotItem creature, ClosedDelegate onClosed, bool enableCardEditing)
	{
		ShowTween.Play();
		LiftAboveOtherPanels();
		mCreature = creature;
		mClosedDelegate = onClosed;
		mEnableCardEditing = enableCardEditing;
		mZoomSoundPending = creature.Creature.Form.ZoomSound != null;
		PopulateCurrentCreature();
	}

	public void ShowCollectionCreature(CreatureData data, ClosedDelegate onClosed)
	{
		InventorySlotItem creature = new InventorySlotItem(new CreatureItem(data)
		{
			IsCollectionDummy = true,
			Xp = 0
		});
		ShowCreature(creature, onClosed, enableCardEditing: false);
	}

	private void PopulateCurrentCreature()
	{
		isCreatureModelShown = true;
		isActionCardShown = false;
		shownActionCardName = null;
		isClosing = false;
		SpawnNodeScaler.localScale = Vector3.one;
		StatsPanel.Populate(mCreature);
		if (PreviewAwakenButton != null)
		{
			bool flag = !string.IsNullOrEmpty(mCreature.Creature.Form.EvolvesTo);
			bool flag2 = ExcludedCreatureIDs.Contains(mCreature.Creature.Form.ID);
			PreviewAwakenButton.SetActive(flag && !mCreature.Creature.IsCollectionDummy && !flag2);
			UILabel componentInChildren = PreviewAwakenButton.GetComponentInChildren<UILabel>();
			if (componentInChildren != null)
			{
				componentInChildren.text = (mShowingAwakenPreview ? KFFLocalization.Get("!!PREVIEW_AWAKEN_SHOW_BASE") : KFFLocalization.Get("!!PREVIEW_AWAKEN_BUTTON"));
			}
		}
		if (mShowingAwakenPreview)
		{
			UnloadAwakenPreview();
			mShowingAwakenPreview = false;
		}
		if (mCreature.Creature.Form.HasPassiveAbility())
		{
			ShowPassiveTween.Play();
		}
		else
		{
			HidePassiveTween.Play();
		}
		ExCardsParent.SetActive(mEnableCardEditing && DetachedSingleton<SceneFlowManager>.Instance.InFrontEnd() && Singleton<PlayerInfoScript>.Instance.CanEquipExCards());
		MainModelParent.transform.localRotation = Quaternion.Euler(new Vector3(0f, 180f, 0f));
		for (int i = 0; i < mCreature.Creature.Form.ActionCards.Count && i < SkillCardNodes.Length; i++)
		{
			if (mCreature.Creature.ActionCards != null && i < mCreature.Creature.ActionCards.Count && mCreature.Creature.ActionCards[i] != null)
			{
				GameObject gameObject = SkillCardNodes[i].InstantiateAsChild(Singleton<PrefabReferences>.Instance.Card);
				gameObject.ChangeLayer(SkillCardNodes[i].layer);
				CardPrefabScript cardScript2 = gameObject.GetComponent<CardPrefabScript>();
				cardScript2.Mode = CardPrefabScript.CardMode.GeneralFrontEnd;
				cardScript2.OpponentInfoPopup = mCreature.Creature.EnemyLoadoutCreature;
				cardScript2.Populate(mCreature.Creature.Form.ActionCards[i]);
				cardScript2.AdjustDepth(i + 1);
				cardScript2.SetCardState(CardPrefabScript.HandCardState.InHand);
				mSpawnedActionCards.Add(cardScript2);
				cardScript2.GetComponent<UIEventTrigger>().onClick.Add(new EventDelegate(delegate
				{
					HideCreatureModel(calledByActionCard: true, cardScript2.Card.Name);
				}));
			}
		}
		for (int num = 0; num < 4; num++)
		{
			if (num < mCreature.Creature.ExCardSlotsUnlocked)
			{
				ExtraCardSlotLocks[num].SetActive(value: false);
				if (mCreature.Creature.ExCards[num] != null)
				{
					ExtraCardSlots[num].gameObject.SetActive(value: true);
					GameObject gameObject2 = ExtraCardSlots[num].InstantiateAsChild(Singleton<PrefabReferences>.Instance.Card);
					gameObject2.ChangeLayer(ExtraCardSlots[num].gameObject.layer);
					CardPrefabScript cardScript3 = gameObject2.GetComponent<CardPrefabScript>();
					cardScript3.Mode = CardPrefabScript.CardMode.GeneralFrontEnd;
					cardScript3.OpponentInfoPopup = mCreature.Creature.EnemyLoadoutCreature;
					cardScript3.Populate(mCreature.Creature.ExCards[num].Card.Form);
					cardScript3.AdjustDepth(num + 1);
					cardScript3.SetCardState(CardPrefabScript.HandCardState.InHand);
					mSpawnedExCards.Add(cardScript3);
					cardScript3.GetComponent<UIEventTrigger>().onClick.Add(new EventDelegate(delegate
					{
						HideCreatureModel(calledByActionCard: true, cardScript3.Card.Name);
					}));
				}
			}
			else
			{
				ExtraCardSlotLocks[num].SetActive(value: true);
			}
		}
		StartCoroutine(LoadModelCo());
		if (Singleton<PlayerInfoScript>.Instance.SaveData.FindCreature((InventorySlotItem m) => m == mCreature) != null)
		{
			FavoriteParent.SetActive(value: true);
			FavoriteCheckmark.SetActive(mCreature.Creature.Favorite);
		}
		else
		{
			FavoriteParent.SetActive(value: false);
		}
	}

	private void AdjustCameraDepth()
	{
		if (!CreatureCamera.isActiveAndEnabled)
		{
			return;
		}
		if (mShowingAwakenPreview && mAwakenPreviewModel != null)
		{
			CreatureCamera.fieldOfView = 30f;
		}
		else
		{
			if (mLoadedModelInstance == null)
			{
				return;
			}
			Renderer[] componentsInChildren = mLoadedModelInstance.GetComponentsInChildren<Renderer>();
			bool flag = false;
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				if (componentsInChildren[i] != null && componentsInChildren[i].isVisible)
				{
					flag = true;
				}
			}
			if (flag)
			{
				CreatureCamera.fieldOfView = 30f;
			}
			else
			{
				CreatureCamera.fieldOfView = 45f;
			}
		}
	}

	private IEnumerator LoadModelCo()
	{
		UnloadModel();
		yield return StartCoroutine(Singleton<SLOTResourceManager>.Instance.LoadCreatureResources(mCreature.Creature.Form, delegate(GameObject objData, Texture2D texture)
		{
			mLoadedModelInstance = MainModelParent.InstantiateAsChild(objData);
			mLoadedModelInstance.ChangeLayerToParent();
			mCreature.Creature.Form.SwapCreatureTexture(mLoadedModelInstance, texture, addSpin: true);
			float num = Mathf.Max(4f, mCreature.Creature.Form.Height);
			float a = Mathf.Min(1f, 5f / num);
			float num2 = Mathf.Max(4f, mCreature.Creature.Form.Width);
			float b = Mathf.Min(1f, 5f / num2);
			float num3 = Mathf.Min(a, b);
			mLoadedModelInstance.transform.localScale = Vector3.one * num3;
		}));
		if (mZoomSoundPending)
		{
			mZoomSoundPending = false;
			if (!isClosing && mCreature.Creature.Form.ZoomSound != null)
			{
				Singleton<SLOTAudioManager>.Instance.PlaySound("creature/" + mCreature.Creature.Form.ZoomSound);
			}
		}
		if (mCreature.Creature.Form.InitNumOfLoad > 0)
		{
			isFirstTimeShow = mCreature.Creature.Form.InitNumOfLoad <= 1;
			isCreatureModelShown = false;
			ShowCreatureModel();
		}
		else
		{
			isFirstTimeShow = true;
			isCreatureModelShown = true;
		}
		if (DetachedSingleton<SceneFlowManager>.Instance.InBattleScene())
		{
			Camera componentInChildren = GetComponentInChildren<Camera>();
			if (componentInChildren != null)
			{
				componentInChildren.depth = 32f;
			}
		}
	}

	private void UnloadModel()
	{
		if (mLoadedModelInstance != null)
		{
			Object.Destroy(mLoadedModelInstance);
			mLoadedModelInstance = null;
			Resources.UnloadUnusedAssets();
		}
	}

	public void OnClickClose()
	{
		RestorePanelDepths();
		InventorySlotItem creature = mCreature;
		mCreature = null;
		if (mClosedDelegate != null)
		{
			ClosedDelegate closedDelegate = mClosedDelegate;
			mClosedDelegate = null;
			closedDelegate(creature);
		}
		isClosing = true;
		UnloadAwakenPreview();
		isClosing = true;
		HideCreatureModel();
	}

	public void Unload()
	{
		for (int i = 0; i < mSpawnedActionCards.Count; i++)
		{
			if (mSpawnedActionCards[i] != null)
			{
				NGUITools.Destroy(mSpawnedActionCards[i].gameObject);
				mSpawnedActionCards[i] = null;
			}
		}
		for (int j = 0; j < mSpawnedExCards.Count; j++)
		{
			if (mSpawnedExCards[j] != null)
			{
				NGUITools.Destroy(mSpawnedExCards[j].gameObject);
				mSpawnedExCards[j] = null;
			}
		}
		mMainCreatureGridDataSource.Clear();
		UnloadModel();
		StatsPanel.Unload();
		UnloadAwakenPreview();
		mShowingAwakenPreview = false;
	}

	public void OnClickEquipGems()
	{
	}

	public void OnCreatureClicked(InventorySlotItem creature)
	{
		mCreature = creature;
		PopulateCurrentCreature();
		if (ShowCardsTween != null)
		{
			ShowCardsTween.Play();
		}
	}

	public void UnzoomCard()
	{
		foreach (CardPrefabScript mSpawnedActionCard in mSpawnedActionCards)
		{
			if (mSpawnedActionCard != null)
			{
				mSpawnedActionCard.Unzoom();
			}
		}
		foreach (CardPrefabScript mSpawnedExCard in mSpawnedExCards)
		{
			if (mSpawnedExCard != null)
			{
				mSpawnedExCard.Unzoom();
			}
		}
	}

	public void OnClickFavorite()
	{
		mCreature.Creature.Favorite = !mCreature.Creature.Favorite;
		FavoriteCheckmark.SetActive(mCreature.Creature.Favorite);
	}

	public void OnClickExCards()
	{
		HideTween.Play();
		isClosing = true;
		HideCreatureModel();
		Singleton<CardEquipController>.Instance.Show(mCreature, OnCloseCardEquip);
	}

	private void OnCloseCardEquip()
	{
		ShowTween.Play();
		PopulateCurrentCreature();
	}

	public void OnClickStatsDetails()
	{
		Singleton<StatsDescriptionPopup>.Instance.ShowPanelAtTransformPosition(StatsButtonTransform);
	}

	public void OnClickWeight()
	{
		Singleton<StatsDescriptionPopup>.Instance.ShowWeightPanelAtTransformPosition(WeightButtonTransform);
	}

	public void HideCreatureModel(bool calledByActionCard = false, string actionCardName = null)
	{
		if (calledByActionCard)
		{
			isActionCardShown = true;
			if (shownActionCardName != null && actionCardName != null && shownActionCardName == actionCardName)
			{
				isActionCardShown = false;
				shownActionCardName = null;
				return;
			}
			if (actionCardName != null)
			{
				shownActionCardName = actionCardName;
			}
		}
		if (isCreatureModelShown)
		{
			HideCreatureTween.Play();
			isCreatureModelShown = false;
		}
	}

	public void ShowCreatureModel(bool calledByActionCard = false)
	{
		if (calledByActionCard)
		{
			isActionCardShown = false;
		}
		else if (!isCreatureModelShown)
		{
			ShowCreatureTween.Play();
			isCreatureModelShown = true;
			shownActionCardName = null;
		}
	}

	public void FinishedShowCreatureModel()
	{
		if (mLoadedModelInstance != null && !isFirstTimeShow)
		{
			isFirstTimeShow = true;
			isCreatureModelShown = false;
			ShowCreatureModel();
		}
	}

	public void OnClickPreviewAwaken()
	{
		if (mShowingAwakenPreview)
		{
			UnloadAwakenPreview();
			ShowCreatureModel();
			mShowingAwakenPreview = false;
			if (mLoadedModelInstance != null)
			{
				mLoadedModelInstance.SetActive(value: true);
			}
			ClearAllCards();
			RepopulateBaseCards();
			StatsPanel.Populate(mCreature);
			if (PreviewAwakenButton != null)
			{
				UILabel componentInChildren = PreviewAwakenButton.GetComponentInChildren<UILabel>();
				if (componentInChildren != null)
				{
					componentInChildren.text = KFFLocalization.Get("!!PREVIEW_AWAKEN_BUTTON");
				}
			}
			return;
		}
		HideCreatureModel();
		if (mLoadedModelInstance != null)
		{
			mLoadedModelInstance.SetActive(value: false);
		}
		ClearAllCards();
		StartCoroutine(LoadAwakenPreviewCo());
		mShowingAwakenPreview = true;
		if (CreatureCamera != null)
		{
			CreatureCamera.fieldOfView = 30f;
		}
		if (PreviewAwakenButton != null)
		{
			UILabel componentInChildren2 = PreviewAwakenButton.GetComponentInChildren<UILabel>();
			if (componentInChildren2 != null)
			{
				componentInChildren2.text = KFFLocalization.Get("!!PREVIEW_AWAKEN_SHOW_BASE");
			}
		}
	}

	private IEnumerator LoadAwakenPreviewCo()
	{
		if (mCreature?.Creature?.Form == null || string.IsNullOrEmpty(mCreature.Creature.Form.EvolvesTo))
		{
			yield break;
		}
		UnloadAwakenPreview();
		CreatureData awakenedForm = CreatureDataManager.Instance.GetData(mCreature.Creature.Form.EvolvesTo);
		if (awakenedForm == null)
		{
			yield break;
		}
		yield return StartCoroutine(Singleton<SLOTResourceManager>.Instance.LoadCreatureResources(awakenedForm, delegate(GameObject objData, Texture2D texture)
		{
			mAwakenPreviewModel = MainModelParent.InstantiateAsChild(objData);
			mAwakenPreviewModel.ChangeLayerToParent();
			awakenedForm.SwapCreatureTexture(mAwakenPreviewModel, texture, addSpin: true);
			float num = Mathf.Max(4f, awakenedForm.Height);
			float a = Mathf.Min(1f, 5f / num);
			float num2 = Mathf.Max(4f, awakenedForm.Width);
			float b = Mathf.Min(1f, 5f / num2);
			float num3 = Mathf.Min(a, b);
			mAwakenPreviewModel.transform.localScale = Vector3.one * num3;
			if (mLoadedModelInstance != null)
			{
				mAwakenPreviewModel.transform.localRotation = mLoadedModelInstance.transform.localRotation;
				Debug.Log("Using natural awakened position with base rotation");
			}
			mAwakenPreviewModel.SetActive(value: true);
			Debug.Log("Attempting to darken awakened creature...");
			DarkenCreature(mAwakenPreviewModel);
			Debug.Log("Darkening complete!");
			if (CreatureCamera != null)
			{
				CreatureCamera.fieldOfView = 30f;
				Debug.Log("Locked camera FOV to 30 for awakened preview");
			}
		}));
		AddSparkleEffect();
		if (awakenedForm.HasPassiveAbility())
		{
			ShowPassiveTween.Play();
		}
		CreatureItem creatureItem = new CreatureItem(awakenedForm);
		creatureItem.IsCollectionDummy = mCreature.Creature.IsCollectionDummy;
		creatureItem.Xp = mCreature.Creature.Xp;
		creatureItem.StarRating = mCreature.Creature.StarRating;
		creatureItem.PassiveSkillLevel = mCreature.Creature.PassiveSkillLevel;
		creatureItem.GetLevelData();
		Debug.Log($"Awakened preview - Base Level: {mCreature.Creature.Level}, Awakened Level: {creatureItem.Level}, XP: {creatureItem.Xp}");
		InventorySlotItem creatureSlot = new InventorySlotItem(creatureItem);
		StatsPanel.Populate(creatureSlot);
		ShowAwakenedCards(awakenedForm);
	}

	private void AddSparkleEffect()
	{
		if (VFX_UI_Awaken_Prefab == null || mAwakenPreviewModel == null)
		{
			return;
		}
		if (mAwakenFXPoint == null)
		{
			mAwakenFXPoint = new GameObject("AwakenFXPoint");
			mAwakenFXPoint.transform.SetParent(mAwakenPreviewModel.transform);
			Vector3 vector = Vector3.up * 2.5f;
			mAwakenFXPoint.transform.localPosition = vector;
			mAwakenFXPoint.transform.localRotation = Quaternion.identity;
			if (mAwakenFXPoint.transform.childCount < 1)
			{
				mAwakenFXPoint.InstantiateAsChild(VFX_UI_Awaken_Prefab);
			}
			Debug.Log($"Positioned sparkles at height offset: {vector}");
		}
		mAwakenFXPoint.SetActive(value: true);
	}

	private void ShowAwakenedCards(CreatureData awakenedForm)
	{
		for (int i = 0; i < awakenedForm.ActionCards.Count && i < SkillCardNodes.Length; i++)
		{
			if (awakenedForm.ActionCards[i] != null)
			{
				GameObject gameObject = SkillCardNodes[i].InstantiateAsChild(Singleton<PrefabReferences>.Instance.Card);
				gameObject.ChangeLayer(SkillCardNodes[i].layer);
				CardPrefabScript cardScript = gameObject.GetComponent<CardPrefabScript>();
				cardScript.Mode = CardPrefabScript.CardMode.GeneralFrontEnd;
				cardScript.Populate(awakenedForm.ActionCards[i]);
				cardScript.AdjustDepth(i + 1);
				cardScript.SetCardState(CardPrefabScript.HandCardState.InHand);
				mSpawnedActionCards.Add(cardScript);
				cardScript.GetComponent<UIEventTrigger>().onClick.Add(new EventDelegate(delegate
				{
					HideCreatureModel(calledByActionCard: true, cardScript.Card.Name);
				}));
			}
		}
	}

	private void ClearAllCards()
	{
		for (int i = 0; i < mSpawnedActionCards.Count; i++)
		{
			if (mSpawnedActionCards[i] != null)
			{
				NGUITools.Destroy(mSpawnedActionCards[i].gameObject);
			}
		}
		mSpawnedActionCards.Clear();
		for (int j = 0; j < mSpawnedExCards.Count; j++)
		{
			if (mSpawnedExCards[j] != null)
			{
				NGUITools.Destroy(mSpawnedExCards[j].gameObject);
			}
		}
		mSpawnedExCards.Clear();
	}

	private void RepopulateBaseCards()
	{
		for (int i = 0; i < mCreature.Creature.ActionCards.Count && i < SkillCardNodes.Length; i++)
		{
			if (mCreature.Creature.ActionCards[i] != null)
			{
				GameObject gameObject = SkillCardNodes[i].InstantiateAsChild(Singleton<PrefabReferences>.Instance.Card);
				gameObject.ChangeLayer(SkillCardNodes[i].layer);
				CardPrefabScript cardScript = gameObject.GetComponent<CardPrefabScript>();
				cardScript.Mode = CardPrefabScript.CardMode.GeneralFrontEnd;
				cardScript.OpponentInfoPopup = mCreature.Creature.EnemyLoadoutCreature;
				cardScript.Populate(mCreature.Creature.Form.ActionCards[i]);
				cardScript.AdjustDepth(i + 1);
				cardScript.SetCardState(CardPrefabScript.HandCardState.InHand);
				mSpawnedActionCards.Add(cardScript);
				cardScript.GetComponent<UIEventTrigger>().onClick.Add(new EventDelegate(delegate
				{
					HideCreatureModel(calledByActionCard: true, cardScript.Card.Name);
				}));
			}
		}
		if (!mEnableCardEditing || !DetachedSingleton<SceneFlowManager>.Instance.InFrontEnd() || !Singleton<PlayerInfoScript>.Instance.CanEquipExCards())
		{
			return;
		}
		for (int num = 0; num < 4; num++)
		{
			if (num < mCreature.Creature.ExCardSlotsUnlocked && mCreature.Creature.ExCards[num] != null)
			{
				ExtraCardSlots[num].gameObject.SetActive(value: true);
				GameObject gameObject2 = ExtraCardSlots[num].InstantiateAsChild(Singleton<PrefabReferences>.Instance.Card);
				gameObject2.ChangeLayer(ExtraCardSlots[num].gameObject.layer);
				CardPrefabScript cardScript2 = gameObject2.GetComponent<CardPrefabScript>();
				cardScript2.Mode = CardPrefabScript.CardMode.GeneralFrontEnd;
				cardScript2.OpponentInfoPopup = mCreature.Creature.EnemyLoadoutCreature;
				cardScript2.Populate(mCreature.Creature.ExCards[num].Card.Form);
				cardScript2.AdjustDepth(num + 1);
				cardScript2.SetCardState(CardPrefabScript.HandCardState.InHand);
				mSpawnedExCards.Add(cardScript2);
				cardScript2.GetComponent<UIEventTrigger>().onClick.Add(new EventDelegate(delegate
				{
					HideCreatureModel(calledByActionCard: true, cardScript2.Card.Name);
				}));
			}
		}
	}

	private void UnloadAwakenPreview()
	{
		if (mAwakenPreviewModel != null)
		{
			RestoreCreatureMaterials(mAwakenPreviewModel);
			Object.Destroy(mAwakenPreviewModel);
			mAwakenPreviewModel = null;
		}
		if (mAwakenFXPoint != null)
		{
			Object.Destroy(mAwakenFXPoint);
			mAwakenFXPoint = null;
		}
		if (PreviewMonitorPanel != null)
		{
			PreviewMonitorPanel.SetActive(value: false);
		}
	}
}
