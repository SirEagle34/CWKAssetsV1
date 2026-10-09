using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DWBattleLane : Singleton<DWBattleLane>
{
	public float AttackDragStartDistance;

	public float HologramDisappearDelay;

	public GameObject EnvironmentObj;

	public GameObject BoardObj;

	public GameObject BoardHologram;

	public Transform P1CharacterPos;

	public Transform P2CharacterPos;

	private GameObject[] mCharacterObjects = new GameObject[2];

	public Transform[] LaneObjParents;

	public Transform DeployObjParent;

	public List<DWBattleLaneObject>[] BattleLaneObjects = new List<DWBattleLaneObject>[2];

	public GameObject LaneObjectPrefab;

	public GameObject DeployVFXPrefab;

	public GameObject DragIndicatorPrefab;

	public Transform TargetIndicatorsParent;

	public CardPrefabScript CurrentCard;

	public PlayerType CurrentActionCasterOwner;

	public DWBattleLaneObject CurrentActionTarget;

	public GameObject RedDeathPrefab;

	public GameObject GreenDeathPrefab;

	public GameObject BlueDeathPrefab;

	public GameObject PurpleDeathPrefab;

	public GameObject YellowDeathPrefab;

	public GameObject CyanDeathPrefab;

	public GameObject OrangeDeathPrefab;

	public GameObject BrownDeathPrefab;

	public GameObject BlackDeathPrefab;

	public GameObject MagentaDeathPrefab;

	public GameObject SoftCurrencyDropPrefab;

	public GameObject SocialCurrencyDropPrefab;

	public GameObject HardCurrencyDropPrefab;

	public GameObject DropLabelPrefab;

	public GameObject VanishFXAfterDeath;

	public Material CreatureDeathMaterial;

	public List<DWBattleLootObject> CurrentBattleLootInventoryObjects = new List<DWBattleLootObject>();

	public List<DWBattleLootObject> CurrentBattleLootCurrencyObjects = new List<DWBattleLootObject>();

	public List<CreatureState> CurrentBattleTomestoneCreature = new List<CreatureState>();

	public Transform DragArrowTransform;

	public Transform DragArrowHeadTransform;

	public Transform DragArrowBodyTransform;

	public MeshFilter DragArrowMesh;

	public float DragArrowAnimateSpeed;

	public float DragArrowTextureScale;

	public float DragArrowCurveExponent;

	public float DragArrowStartOffset;

	public float DragArrowVerticalOffset;

	public float DragArrowLengthOffset;

	public float DragArrowHeadHighestAngle;

	public float DragArrowHeadLowestAngle;

	public float DragArrowAiSpacing;

	public Color ValidTargetColor;

	public Color InvalidTargetColor;

	public Dictionary<CreatureItem, GameObject> CreaturePool = new Dictionary<CreatureItem, GameObject>();

	public Dictionary<string, GameObject> CreatureVFXPool = new Dictionary<string, GameObject>();

	private Dictionary<CreatureItem, DWBattleLaneObject> LaneObjectPool = new Dictionary<CreatureItem, DWBattleLaneObject>();

	private GameObject[] mTargetIndicators;

	private Dictionary<GameObject, Material> mTargetIndicatorRenderers = new Dictionary<GameObject, Material>();

	private List<Renderer> mMeshRenderers = new List<Renderer>();

	private Material mDragArrowMeshMat;

	private bool mInitialized;

	public bool LootCollectAuto;

	private bool mP2ThinkingDelay;

	public float DeathRepositionTime;

	public float CreatureOffsetX;

	public float Amplitude;

	public float CreatureSpacing;

	public float MinimumCreatureWidth;

	public float MinCreatureHeight = 5f;

	public int DebugIgnoreCreaturesInCam;

	public float MoveScaledUpCreaturesBackFactor;

	public bool UpdateCreaturePosition;

	private Transform mDragArrowAiTarget;

	private Vector2 mUVOffset = Vector2.zero;

	public bool EnableFreeCam;

	public float FreeCamSensitivity;

	public float FreeCamSnapSpeed;

	public float FreeCamMinHeight;

	public float FreeCamMaxHeight;

	private bool mFreeCam;

	private Vector2 mFreeCamRotation;

	private Vector3 mCamLookAtForward;

	private float mCamDistance;

	private Vector3 mTargetCamPos;

	public bool ShowingAiAttackArrow { get; set; }

	public GameObject BuildCreature(CreatureItem creature)
	{
		if (creature == null || creature.Form == null)
		{
			Debug.LogError("[BuildCreature] Invalid creature.");
			return null;
		}

		string prefabPath = "Creatures/" + creature.Form.Prefab + "/" + creature.Form.Prefab;

		Debug.Log("[BuildCreature] Loading: " + creature.Form.ID + " -> " + prefabPath);

		GameObject prefab = SLOTResourceManager.Load<GameObject>(prefabPath);
		if (prefab == null)
		{
			Debug.LogError("[BuildCreature] Prefab not found: " + creature.Form.ID + " | Path: " + prefabPath);
			return null;
		}

		GameObject instance = UnityEngine.Object.Instantiate(prefab);
		if (instance == null)
		{
			Debug.LogError("[BuildCreature] Instantiate failed: " + creature.Form.ID);
			return null;
		}

		instance.SetActive(false);
		CreaturePool[creature] = instance;

		Debug.Log("[BuildCreature] CreaturePool ADDED: " + creature.Form.ID);
		return instance;
	}

	public bool LootObjectsToCollect()
	{
		return CurrentBattleLootInventoryObjects.Count > 0;
	}

	public bool CurrencyObjectsToCollect()
	{
		return CurrentBattleLootCurrencyObjects.Count > 0;
	}

	private void Awake()
	{
		BattleLaneObjects[0] = new List<DWBattleLaneObject>();
		BattleLaneObjects[1] = new List<DWBattleLaneObject>();
		if (DragArrowMesh != null)
		{
			mDragArrowMeshMat = DragArrowMesh.GetComponent<Renderer>().material;
		}
	}

	private void Initialize()
	{
		mTargetIndicators = new GameObject[MiscParams.CreaturesOnBoard];
		for (int i = 0; i < mTargetIndicators.Length; i++)
		{
			mTargetIndicators[i] = TargetIndicatorsParent.InstantiateAsChild(DragIndicatorPrefab);
			mTargetIndicators[i].SetActive(value: false);
			mTargetIndicatorRenderers[mTargetIndicators[i]] = mTargetIndicators[i].FindInChildren("Mesh").GetComponent<Renderer>().material;
		}
		mInitialized = true;
	}

	public void PoolLaneObjects()
	{
		LaneObjectPool.Clear();
		for (int i = 0; i < Singleton<DWGame>.Instance.UserLoadout.CreatureSet.Count; i++)
		{
			if (Singleton<DWGame>.Instance.UserLoadout.CreatureSet[i] != null)
			{
				GameObject obj = LaneObjParents[0].InstantiateAsChild(LaneObjectPrefab);
				DWBattleLaneObject component = obj.GetComponent<DWBattleLaneObject>();
				if (component.ShadowBlob != null)
				{
					component.ShadowBlob.PrepareCache(Singleton<DWGame>.Instance.UserLoadout.CreatureSet[i].Creature);
				}
				LaneObjectPool[Singleton<DWGame>.Instance.UserLoadout.CreatureSet[i].Creature] = component;
				obj.SetActive(value: false);
			}
		}
		for (int j = 0; j < Singleton<DWGame>.Instance.OpLoadout.CreatureSet.Count; j++)
		{
			if (Singleton<DWGame>.Instance.OpLoadout.CreatureSet[j] != null)
			{
				GameObject obj2 = LaneObjParents[1].InstantiateAsChild(LaneObjectPrefab);
				DWBattleLaneObject component2 = obj2.GetComponent<DWBattleLaneObject>();
				if (component2.ShadowBlob != null)
				{
					component2.ShadowBlob.PrepareCache(Singleton<DWGame>.Instance.OpLoadout.CreatureSet[j].Creature);
				}
				LaneObjectPool[Singleton<DWGame>.Instance.OpLoadout.CreatureSet[j].Creature] = component2;
				obj2.SetActive(value: false);
			}
		}
		VanishFXAfterDeath = SLOTResourceManager.Load("VFX/Actions/VFX_Action_Death", typeof(GameObject)) as GameObject;
	}

	public void HideTargetIndicators()
	{
		GameObject[] array = mTargetIndicators;
		foreach (GameObject gameObject in array)
		{
			if (gameObject != null)
			{
				gameObject.SetActive(value: false);
			}
		}
	}

	public void SetTargetIndicator(Vector3 position, bool validTarget, int index = 0)
	{
		GameObject gameObject = mTargetIndicators[index];
		gameObject.SetActive(value: true);
		gameObject.transform.position = position;
		Color value = ((!validTarget) ? InvalidTargetColor : ValidTargetColor);
		if (mTargetIndicatorRenderers.ContainsKey(gameObject))
		{
			mTargetIndicatorRenderers[gameObject].SetColor("_TintColor", value);
		}
	}

	public void SetTargetIndicators(CardData card, DWBattleLaneObject hoveredLane, bool attackTarget = false)
	{
		HideTargetIndicators();
		List<CreatureState> creatureRange = PlayerState.GetCreatureRange(hoveredLane.Creature.Owner, hoveredLane.Creature, card.TargetGroup);
		for (int i = 0; i < creatureRange.Count; i++)
		{
			CreatureState creatureState = creatureRange[i];
			bool validTarget = true;
			if (!attackTarget)
			{
				validTarget = Singleton<DWGame>.Instance.CanPlay(PlayerType.User, card, creatureState.Owner.Type, creatureState.Lane.Index) == PlayerState.CanPlayResult.CanPlay;
			}
			DWBattleLaneObject laneObject = GetLaneObject(creatureState);
			SetTargetIndicator(laneObject.transform.position, validTarget, i);
		}
	}

	public GameObject GetCharacterObj(PlayerType player)
	{
		return mCharacterObjects[player.IntValue];
	}

	public void StoreCharacterObj(int n, GameObject obj)
	{
		mCharacterObjects[n] = obj;
	}

	public void CollectAlphaTintMaterials()
	{
		Shader shader = Shader.Find("Unlit/Alpha With Tint");
		Renderer[] componentsInChildren = GetComponentsInChildren<Renderer>(includeInactive: true);
		foreach (Renderer renderer in componentsInChildren)
		{
			if (renderer.material.shader == shader)
			{
				mMeshRenderers.Add(renderer);
			}
		}
	}

	public void OnLaneColliderClicked(PlayerType player, int n, CreatureItem data)
	{
	}

	public Vector3 RemoveCreatureObj(CreatureState creature, GameMessage passiveTriggered, bool showDeath = true)
	{
		if (creature == null || creature.Owner == null)
		{
			return Vector3.zero;
		}
		int intValue = creature.Owner.Type.IntValue;
		if (intValue < 0 || intValue >= BattleLaneObjects.Length)
		{
			return Vector3.zero;
		}
		List<DWBattleLaneObject> list = BattleLaneObjects[intValue];
		if (list == null)
		{
			return Vector3.zero;
		}
		DWBattleLaneObject dWBattleLaneObject = list.Find((DWBattleLaneObject match) => match != null && match.Creature == creature);
		if (dWBattleLaneObject == null)
		{
			return Vector3.zero;
		}
		Vector3 position = dWBattleLaneObject.transform.position;
		if (passiveTriggered != null && dWBattleLaneObject.CreatureObject != null)
		{
			Vector3 position2 = dWBattleLaneObject.CreatureObject.transform.position;
			StartCoroutine(PlayPassiveTriggerEffectAfterDelay(position2, passiveTriggered));
		}
		if (dWBattleLaneObject.ShadowBlob != null)
		{
			dWBattleLaneObject.ShadowBlob.Despawn();
		}
		CreatureHPBar hPBar = Singleton<BattleHudController>.Instance.GetHPBar(creature);
		if (hPBar != null)
		{
			UnityEngine.Object.Destroy(hPBar.gameObject);
		}
		CreatureBuffBar buffBar = Singleton<BattleHudController>.Instance.GetBuffBar(creature);
		if (buffBar != null)
		{
			UnityEngine.Object.Destroy(buffBar.gameObject);
		}
		list.Remove(dWBattleLaneObject);
		StartCoroutine(PlayDerez(dWBattleLaneObject, creature, revive: false, showDeath));
		return position;
	}

	public void FullReplayReset()
	{
		Singleton<DWBattleLane>.Instance.StopAllCoroutines();
		Singleton<DWGameMessageHandler>.Instance.StopAllCoroutines();
		Singleton<DWGameMessageHandler>.Instance.ResetForReplay();
		for (int i = 0; i < BattleLaneObjects.Length; i++)
		{
			BattleLaneObjects[i].Clear();
		}
		CreatureHPBar[] array = UnityEngine.Object.FindObjectsOfType<CreatureHPBar>();
		for (int j = 0; j < array.Length; j++)
		{
			UnityEngine.Object.Destroy(array[j].gameObject);
		}
		CreatureBuffBar[] array2 = UnityEngine.Object.FindObjectsOfType<CreatureBuffBar>();
		for (int j = 0; j < array2.Length; j++)
		{
			UnityEngine.Object.Destroy(array2[j].gameObject);
		}
	}

	public void ResetBattleLane()
	{
		for (int i = 0; i < BattleLaneObjects.Length; i++)
		{
			List<DWBattleLaneObject> list = BattleLaneObjects[i];
			for (int num = list.Count - 1; num >= 0; num--)
			{
				DWBattleLaneObject dWBattleLaneObject = list[num];
				if (dWBattleLaneObject != null)
				{
					dWBattleLaneObject.DyingThisFrame = false;
					dWBattleLaneObject.Creature = null;
					dWBattleLaneObject.CreatureObject = null;
					dWBattleLaneObject.InitialPositionSet = false;
					dWBattleLaneObject.gameObject.SetActive(value: false);
				}
			}
			list.Clear();
		}
	}

	public void ResetBattleHud()
	{
		BattleHudController instance = Singleton<BattleHudController>.Instance;
		if (!(instance == null))
		{
			CreatureHPBar[] componentsInChildren = instance.GetComponentsInChildren<CreatureHPBar>(includeInactive: true);
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				UnityEngine.Object.Destroy(componentsInChildren[i].gameObject);
			}
			CreatureBuffBar[] componentsInChildren2 = instance.GetComponentsInChildren<CreatureBuffBar>(includeInactive: true);
			for (int i = 0; i < componentsInChildren2.Length; i++)
			{
				UnityEngine.Object.Destroy(componentsInChildren2[i].gameObject);
			}
		}
	}

	public void PlayDefeatedAnim(PlayerType player)
	{
		Singleton<CharacterAnimController>.Instance.HideHandCards(hide: true);
		Singleton<CharacterAnimController>.Instance.PlayHeroAnim(player, CharAnimType.Lose);
	}

	public void PlayWinnerAnim(PlayerType player)
	{
		Singleton<CharacterAnimController>.Instance.PlayHeroAnim(player, CharAnimType.Win);
	}

	private IEnumerator PlayPassiveTriggerEffectAfterDelay(Vector3 lanePos, GameMessage passiveTriggered)
	{
		yield return new WaitForSeconds(0.5f);
		Singleton<DWGameMessageHandler>.Instance.PlayPassiveTriggerEffect(lanePos, null, passiveTriggered);
	}

	public IEnumerator PlayDerez(DWBattleLaneObject laneObject, CreatureState creature, bool revive, bool showDeath)
	{
		if (showDeath)
		{
			Animator animator = ((!laneObject.IsBunny) ? laneObject.CreatureObject.GetComponentInChildren<Animator>() : laneObject.SwappedCreatureObject.GetComponentInChildren<Animator>());
			if (animator != null)
			{
				animator.speed = 0f;
			}
			laneObject.AssignFadeMatToCreature();
			List<SkinnedMeshRenderer> list = new List<SkinnedMeshRenderer>();
			SkinnedMeshRenderer[] componentsInChildren = laneObject.CreatureObject.GetComponentsInChildren<SkinnedMeshRenderer>();
			foreach (SkinnedMeshRenderer item in componentsInChildren)
			{
				list.Add(item);
			}
			if (laneObject.SwappedCreatureObject != null)
			{
				componentsInChildren = laneObject.SwappedCreatureObject.GetComponentsInChildren<SkinnedMeshRenderer>();
				foreach (SkinnedMeshRenderer item2 in componentsInChildren)
				{
					list.Add(item2);
				}
			}
			foreach (SkinnedMeshRenderer item3 in list)
			{
				item3.gameObject.AddComponent<MeshColorAnimateScript>().TriggerAnim = true;
			}
			yield return new WaitForSeconds(0.5f);
			if (CurrentBattleTomestoneCreature.Contains(creature))
			{
				CurrentBattleTomestoneCreature.Remove(creature);
			}
			bool flag = false;
			if (RewardManager.GetCreatureDropInfo(creature, out var dropType, out var rarity))
			{
				SpawnLootDropObject(laneObject.CreatureObject.transform, dropType, rarity, creature.Data);
				flag = rarity != -1;
			}
			if (!flag)
			{
				SLOTGame.InstantiateFX(VanishFXAfterDeath, laneObject.transform.position, laneObject.transform.rotation);
			}
		}
		laneObject.RemoveAllStatusEffectObjFromCreature();
		if (!revive)
		{
			if (laneObject.IsBunny)
			{
				laneObject.UntransmogrifyCreature();
			}
			laneObject.CreatureObject.SetActive(value: false);
			laneObject.CreatureObject.transform.parent = null;
			laneObject.CreatureObject = null;
			laneObject.InitialPositionSet = false;
			laneObject.gameObject.SetActive(value: false);
		}
		else
		{
			Singleton<DWGame>.Instance.TriggerSpawnCreatureEffects(creature);
		}
	}

	private IEnumerator FlyCoinObject()
	{
		yield return new WaitForSeconds(0.5f);
	}

	public void TriggerBattleLootCollect()
	{
		Singleton<TutorialController>.Instance.AdvanceIfNextStateIs("Q1_Drop1");
		ResetLaneColliders(enable: false);
		CurrentBattleLootInventoryObjects[0].GetComponent<Collider>().enabled = true;
		if (LootCollectAuto)
		{
			StartCoroutine(AutoLootCollect());
		}
		else
		{
			Singleton<DWGameCamera>.Instance.MoveCameraToLootCollect(CurrentBattleLootInventoryObjects[0]);
		}
	}

	public GameObject GetLootObject()
	{
		if (CurrentBattleLootInventoryObjects.Count > 0)
		{
			return CurrentBattleLootInventoryObjects[0].gameObject;
		}
		return null;
	}

	private IEnumerator AutoLootCollect()
	{
		for (int i = 0; i < CurrentBattleLootInventoryObjects.Count; i++)
		{
			DWBattleLootObject dWBattleLootObject = CurrentBattleLootInventoryObjects[i];
			dWBattleLootObject.GetComponent<Collider>().enabled = false;
			dWBattleLootObject.SendMessage("OnClick");
			yield return new WaitForSeconds(0.5f);
		}
	}

	public DWBattleLaneObject GetLaneObject(CreatureState creature)
	{
		if (creature == null)
		{
			return null;
		}
		return BattleLaneObjects[creature.Owner.Type.IntValue].Find((DWBattleLaneObject match) => match != null && match.Creature == creature);
	}

	public DWBattleLaneObject GetLaneObject(PlayerType player, string creatureOrFactionId)
	{
		return BattleLaneObjects[player.IntValue].Find((DWBattleLaneObject match) => match.Creature.Data.Form.ID == creatureOrFactionId || match.Creature.Data.Form.Faction.ClassName() == creatureOrFactionId);
	}

	public DWBattleLaneObject GetLaneObject(PlayerType player, Predicate<DWBattleLaneObject> creatureMatch)
	{
		return BattleLaneObjects[player.IntValue].Find(creatureMatch);
	}

	public GameObject GetCreatureObject(CreatureState creature)
	{
		DWBattleLaneObject laneObject = GetLaneObject(creature);
		if (laneObject != null)
		{
			if (laneObject.IsBunny)
			{
				if (laneObject.SwappedCreatureObject != null)
				{
					return laneObject.SwappedCreatureObject;
				}
				return laneObject.CreatureObject;
			}
			return laneObject.CreatureObject;
		}
		return null;
	}

	public List<GameObject> GetCreatureObjects(CreatureState creature)
	{
		List<GameObject> list = new List<GameObject>();
		DWBattleLaneObject laneObject = GetLaneObject(creature);
		if (laneObject != null)
		{
			list.Add(laneObject.CreatureObject);
			if (laneObject.IsBunny && laneObject.SwappedCreatureObject != null)
			{
				list.Add(laneObject.SwappedCreatureObject);
			}
		}
		return list;
	}

	public DWBattleLaneObject GetEndLane(PlayerType player, bool rightEnd)
	{
		List<DWBattleLaneObject> list = BattleLaneObjects[player.IntValue];
		if (list.Count == 0)
		{
			return null;
		}
		if (rightEnd)
		{
			return list[list.Count - 1];
		}
		return list[0];
	}

	public DWBattleLaneObject GetCurrentLane(Vector3 worldPos)
	{
		for (int i = 0; i < 2; i++)
		{
			for (int j = 0; j < BattleLaneObjects[i].Count; j++)
			{
				DWBattleLaneObject dWBattleLaneObject = BattleLaneObjects[i][j];
				if (dWBattleLaneObject.GetComponent<Collider>().bounds.Contains(worldPos))
				{
					return dWBattleLaneObject;
				}
			}
		}
		return null;
	}

	public DWBattleLaneObject GetLowestHealthCreatureLane(PlayerType player)
	{
		List<DWBattleLaneObject> obj = BattleLaneObjects[player.IntValue];
		DWBattleLaneObject dWBattleLaneObject = null;
		int num = 0;
		foreach (DWBattleLaneObject item in obj)
		{
			int hP = item.Creature.HP;
			if (dWBattleLaneObject == null || hP < num)
			{
				dWBattleLaneObject = item;
				num = hP;
			}
		}
		return dWBattleLaneObject;
	}

	public DWBattleLaneObject GetDebuffedCreatureLane(PlayerType player)
	{
		return BattleLaneObjects[player.IntValue].Find((DWBattleLaneObject m) => m.Creature.HasDebuff);
	}

	public void StartTargetSelection()
	{
		UICamera.OnlyAllowDrag = false;
		CurrentCard.SetTargetSelection(CurrentActionTarget.CreatureObject);
		Singleton<BattleHudController>.Instance.OnP1SelectLaneStart(CurrentCard.Card);
		HideAvailableActionIndicators();
		bool flag = false;
		List<DWBattleLaneObject> list = new List<DWBattleLaneObject>();
		foreach (DWBattleLaneObject item in BattleLaneObjects[1])
		{
			if ((bool)item.Creature.HasBravery && !flag)
			{
				list.Clear();
				flag = true;
			}
			if (!flag || (bool)item.Creature.HasBravery)
			{
				list.Add(item);
			}
		}
		foreach (DWBattleLaneObject item2 in list)
		{
			item2.HealthBar.TargetableIndicator.SetActive(value: true);
		}
		Singleton<DWGame>.Instance.SelectingLane = true;
		Singleton<TutorialController>.Instance.AdvanceIfOnCardPlay(targeting: true);
	}

	public void CancelTargetSelection()
	{
		CurrentCard.UndoTargetSelection();
		EndTargetSelection();
		Singleton<DWBattleLane>.Instance.HideTargetIndicators();
		Singleton<DWBattleLane>.Instance.HideDamagePredictions();
		Singleton<DWGame>.Instance.SelectingLane = false;
	}

	public void EndTargetSelection()
	{
		Singleton<BattleHudController>.Instance.OnP1SelectLaneEnd();
		Singleton<HandCardController>.Instance.ShowHand();
		foreach (DWBattleLaneObject item in BattleLaneObjects[1])
		{
			item.HealthBar.TargetableIndicator.SetActive(value: false);
		}
	}

	public void ResetLaneColliders(bool enable)
	{
		for (int i = 0; i < 2; i++)
		{
			for (int j = 0; j < BattleLaneObjects[i].Count; j++)
			{
				BattleLaneObjects[i][j].GetComponent<Collider>().enabled = enable;
			}
		}
	}

	public void PlayBlinkColorOnLaneMesh(DWBattleLaneObject obj, Color color)
	{
	}

	public void StopBlinkOnLane(DWBattleLaneObject obj)
	{
	}

	public void StopBlinkAllLanes()
	{
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

	public Transform GetChairObj(string name)
	{
		if (EnvironmentObj != null)
		{
			return FindInChildren(EnvironmentObj.transform, name);
		}
		return null;
	}

	public void SetObjectMaterialColor(Color color)
	{
		foreach (Renderer mMeshRenderer in mMeshRenderers)
		{
			mMeshRenderer.material.SetColor("_TintColor", color);
		}
	}

	public void SetVFXFactionColor(GameObject obj, CreatureFaction faction)
	{
		Color startColor = Color.white;
		switch (faction)
		{
		case CreatureFaction.Red:
			startColor = Color.red;
			break;
		case CreatureFaction.Blue:
			startColor = Color.blue;
			break;
		case CreatureFaction.Green:
			startColor = Color.green;
			break;
		case CreatureFaction.Light:
			startColor = Color.yellow;
			break;
		case CreatureFaction.Dark:
			startColor = Color.Lerp(Color.red, Color.blue, 0.5f);
			break;
		case CreatureFaction.White:
			startColor = Color.cyan;
			break;
		case CreatureFaction.Orange:
			startColor = Color.Lerp(Color.red, Color.yellow, 0.5f);
			break;
		case CreatureFaction.Black:
			startColor = Color.Lerp(Color.red, Color.cyan, 0.5f);
			break;
		case CreatureFaction.Yellow:
			startColor = Color.Lerp(Color.green, Color.red, 0.5f);
			break;
		case CreatureFaction.Purple:
			startColor = Color.Lerp(Color.blue, Color.magenta, 0.5f);
			break;
		}
		ParticleSystem[] componentsInChildren = obj.GetComponentsInChildren<ParticleSystem>(includeInactive: true);
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].startColor = startColor;
		}
	}

	public void StartP2ThinkingDelay()
	{
		StartCoroutine(StartP2ThinkingDelayCo());
	}

	private IEnumerator StartP2ThinkingDelayCo()
	{
		yield return new WaitForSeconds(2.5f);
		mP2ThinkingDelay = false;
	}

	public void SkipP2ThinkingDelay()
	{
		mP2ThinkingDelay = false;
	}

	public IEnumerator ReadP2Actions()
	{
		mP2ThinkingDelay = true;
		while (mP2ThinkingDelay)
		{
			yield return null;
		}
		bool lookingAtOpponent;
		if (Singleton<PlayerInfoScript>.Instance.StateData.MultiplayerMode || Singleton<TutorialController>.Instance.IsBlockActive("IntroBattle"))
		{
			Singleton<BattleHudController>.Instance.OnOpponentActionTaken();
			lookingAtOpponent = false;
		}
		else
		{
			lookingAtOpponent = true;
		}
		while (true)
		{
			if (Singleton<TutorialController>.Instance.IsBlockActive("IntroBattle") || Singleton<TutorialController>.Instance.IsFTUETutorialActive())
			{
				while (Singleton<TutorialController>.Instance.OnNonEnemyTurnTutorialState())
				{
					yield return null;
				}
			}
			while (Singleton<AIManager>.Instance.IsPlanning())
			{
				yield return null;
			}
			yield return null;
			while (!Singleton<DWGameMessageHandler>.Instance.IsEffectDone())
			{
				yield return null;
			}
			AIDecision CurrentChoice2 = null;
			while (true)
			{
				if (Singleton<DWGame>.Instance.IsGameOver())
				{
					yield break;
				}
				CurrentChoice2 = GetNextDecision();
				if (CurrentChoice2 != null)
				{
					break;
				}
				yield return null;
			}
			if (lookingAtOpponent)
			{
				Singleton<BattleHudController>.Instance.OnOpponentActionTaken();
				lookingAtOpponent = false;
			}
			while (true)
			{
				CustomAIScripts.Dialog dialog = DetachedSingleton<CustomAIManager>.Instance.PopDialog();
				if (dialog == null)
				{
					break;
				}
				yield return StartCoroutine(Singleton<BattleHudController>.Instance.ShowAIDialog(dialog.Text, dialog.Time));
			}
			KFFRandom.Seed = CurrentChoice2.Seed;
			if (CurrentChoice2.EndTurn)
			{
				break;
			}
			if (CurrentChoice2.Card != null || CurrentChoice2.IsDeploy)
			{
				_ = CurrentChoice2.Card;
				PlayActionCardAnimOnHero(PlayerType.Opponent, CurrentChoice2.Card);
				LeaderData selectedSkin = Singleton<DWGame>.Instance.GetLeader(PlayerType.Opponent).SelectedSkin;
				float cardRemoveAnimTime = selectedSkin.CardRemoveAnimTime;
				float cardPlayTime = selectedSkin.CardPlayAnimTime - selectedSkin.CardRemoveAnimTime;
				float bigCardExtraTime = selectedSkin.BigCardPlayAnimTime - selectedSkin.CardPlayAnimTime;
				yield return new WaitForSeconds(cardRemoveAnimTime);
				if (!CurrentChoice2.IsDeploy)
				{
					Singleton<CharacterAnimController>.Instance.Remove3DHoldCard(PlayerType.Opponent);
				}
				if (cardPlayTime > 0f)
				{
					yield return new WaitForSeconds(cardPlayTime);
				}
				if (Singleton<CharacterAnimController>.Instance.IsOpponentPlayingBigCard() && bigCardExtraTime > 0f)
				{
					yield return new WaitForSeconds(bigCardExtraTime);
				}
				if (CurrentChoice2.IsDeploy)
				{
					yield return StartCoroutine(Singleton<HandCardController>.Instance.ShowOpponentPlayAnim(CurrentChoice2.Creature, CurrentChoice2.LaneIndex1));
					StartCoroutine(Singleton<BattleLogController>.Instance.LogCreaturePlay(CurrentChoice2.Creature));
					Singleton<DWGame>.Instance.DeployCreatureFromCard(PlayerType.Opponent, CurrentChoice2.Creature, CurrentChoice2.LaneIndex1);
					yield return new WaitForSeconds(0.4f);
					if (Singleton<PlayerInfoScript>.Instance.StateData.MultiplayerMode && Singleton<DWGame>.Instance.InDeploymentPhase())
					{
						yield return null;
						break;
					}
					continue;
				}
				GameObject creatureTarget = null;
				CreatureState creatureStateTarget = null;
				if (CurrentChoice2.LaneIndex1 != -1)
				{
					DWBattleLaneObject dWBattleLaneObject = BattleLaneObjects[CurrentChoice2.TargetPlayer.IntValue].Find((DWBattleLaneObject match) => match.Creature.Lane.Index == CurrentChoice2.LaneIndex1);
					if (dWBattleLaneObject != null)
					{
						creatureStateTarget = dWBattleLaneObject.Creature;
						creatureTarget = dWBattleLaneObject.CreatureObject;
					}
				}
				yield return StartCoroutine(Singleton<HandCardController>.Instance.ShowOpponentPlayAnim(CurrentChoice2.Card, creatureTarget));
				StartCoroutine(Singleton<BattleLogController>.Instance.LogCardPlay(CurrentChoice2.Card, creatureStateTarget));
				PlayOpponentActionCard(CurrentChoice2);
			}
			else
			{
				if (!CurrentChoice2.IsAttack)
				{
					continue;
				}
				CreatureState creature = BattleLaneObjects[1].Find((DWBattleLaneObject match) => match.Creature.Lane.Index == CurrentChoice2.LaneIndex1).Creature;
				CreatureState target = null;
				if (CurrentChoice2.LaneIndex2 != -1)
				{
					target = BattleLaneObjects[0].Find((DWBattleLaneObject match) => match.Creature.Lane.Index == CurrentChoice2.LaneIndex2).Creature;
				}
				if (CurrentChoice2.TutorialForcedCardDraw != null)
				{
					creature.OverrideDrawPile = new List<CardData> { CardDataManager.Instance.GetData(CurrentChoice2.TutorialForcedCardDraw) };
				}
				Singleton<DWGame>.Instance.DragAttack(PlayerType.Opponent, CurrentChoice2.LaneIndex1, CurrentChoice2.LaneIndex2);
				creature.OverrideDrawPile = null;
				StartCoroutine(Singleton<BattleLogController>.Instance.LogDragAttack(creature, target));
			}
		}
		while (!Singleton<DWGameMessageHandler>.Instance.IsEffectDone())
		{
			yield return null;
		}
		if (!Singleton<DWGame>.Instance.IsGameOver())
		{
			if (Singleton<PlayerInfoScript>.Instance.StateData.MultiplayerMode)
			{
				Singleton<MultiplayerMessageHandler>.Instance.GetNextOpponentMove();
			}
			Singleton<DWGame>.Instance.SetGameState(GameState.P2EndTurn);
		}
	}

	private AIDecision GetNextDecision()
	{
		if (Singleton<TutorialController>.Instance.AIOverridden)
		{
			return Singleton<TutorialController>.Instance.GetAIDecision();
		}
		if (Singleton<PlayerInfoScript>.Instance.StateData.MultiplayerMode)
		{
			return Singleton<MultiplayerMessageHandler>.Instance.GetNextOpponentMove();
		}
		return Singleton<AIManager>.Instance.GetAIDecision();
	}

	public IEnumerator ReadP1Actions()
	{
		yield return new WaitForSeconds(0.5f);
		while (true)
		{
			if (Singleton<AIManager>.Instance.IsPlanning())
			{
				yield return null;
				continue;
			}
			yield return null;
			while (!Singleton<DWGameMessageHandler>.Instance.IsEffectDone())
			{
				yield return null;
			}
			AIDecision CurrentChoice2 = null;
			while (true)
			{
				if (Singleton<DWGame>.Instance.IsGameOver())
				{
					yield break;
				}
				CurrentChoice2 = Singleton<AIManager>.Instance.GetAIDecision();
				if (CurrentChoice2 != null)
				{
					break;
				}
				yield return null;
			}
			while (UICamera.IsInputLocked())
			{
				yield return null;
			}
			while (Singleton<PauseController>.Instance.IsInputBlocked())
			{
				yield return null;
			}
			KFFRandom.Seed = CurrentChoice2.Seed;
			if (CurrentChoice2.EndTurn)
			{
				break;
			}
			if (CurrentChoice2.Card != null || CurrentChoice2.IsDeploy)
			{
				_ = CurrentChoice2.Card;
				PlayActionCardAnimOnHero(PlayerType.User, CurrentChoice2.Card);
				LeaderData selectedSkin = Singleton<DWGame>.Instance.GetLeader(PlayerType.User).SelectedSkin;
				float cardRemoveAnimTime = selectedSkin.CardRemoveAnimTime;
				float cardPlayTime = selectedSkin.CardPlayAnimTime - selectedSkin.CardRemoveAnimTime;
				yield return new WaitForSeconds(cardRemoveAnimTime);
				if (!CurrentChoice2.IsDeploy)
				{
					Singleton<CharacterAnimController>.Instance.Remove3DHoldCard(PlayerType.User);
				}
				if (cardPlayTime > 0f)
				{
					yield return new WaitForSeconds(cardPlayTime);
				}
				if (CurrentChoice2.IsDeploy)
				{
					yield return StartCoroutine(Singleton<HandCardController>.Instance.ShowUserCardPlayAnimation(CurrentChoice2.Creature));
					Singleton<DWGame>.Instance.DeployCreatureFromCard(PlayerType.User, CurrentChoice2.Creature, CurrentChoice2.LaneIndex1);
					yield return new WaitForSeconds(0.4f);
					continue;
				}
				if (CurrentChoice2.LaneIndex1 != -1)
				{
					DWBattleLaneObject dWBattleLaneObject = BattleLaneObjects[CurrentChoice2.TargetPlayer.IntValue].Find((DWBattleLaneObject match) => match.Creature.Lane.Index == CurrentChoice2.LaneIndex1);
					if (dWBattleLaneObject != null)
					{
						_ = dWBattleLaneObject.Creature;
						_ = dWBattleLaneObject.CreatureObject;
					}
				}
				yield return StartCoroutine(Singleton<HandCardController>.Instance.ShowUserCardPlayAnimation(CurrentChoice2.Card));
				PlayUserActionCard(CurrentChoice2);
			}
			else if (CurrentChoice2.IsAttack)
			{
				CreatureState creature = BattleLaneObjects[0].Find((DWBattleLaneObject match) => match.Creature.Lane.Index == CurrentChoice2.LaneIndex1).Creature;
				Singleton<DWGame>.Instance.DragAttack(PlayerType.User, CurrentChoice2.LaneIndex1, CurrentChoice2.LaneIndex2);
				creature.OverrideDrawPile = null;
			}
		}
		while (!Singleton<DWGameMessageHandler>.Instance.IsEffectDone())
		{
			yield return null;
		}
		if (!Singleton<DWGame>.Instance.IsGameOver())
		{
			Singleton<DWGame>.Instance.SetGameState(GameState.P1EndTurn);
		}
	}

	public void PlayActionCardAnimOnHero(PlayerType player, CardData card)
	{
		CharAnimType animType = ((card == null || (int)card.Cost < 40) ? CharAnimType.PlayCard : CharAnimType.PlayBigCard);
		Singleton<CharacterAnimController>.Instance.PlayHeroAnim(player, animType);
	}

	public void PlayUserActionCard(CardPrefabScript cardPrefab, DWBattleLaneObject targetLaneObj)
	{
		Singleton<DWGame>.Instance.turnActions.Add(DWGame.TurnActions.PlayCard);
		CardData card = cardPrefab.Card;
		Singleton<TutorialController>.Instance.PassIfOnCardPlay();
		if (card.TargetType1 == SelectionType.Lane)
		{
			CurrentCard = cardPrefab;
			CurrentActionCasterOwner = PlayerType.User;
			CurrentActionTarget = targetLaneObj;
			if (card.RequiresTargetSelection())
			{
				DWBattleLaneObject autoTargetForCard = GetAutoTargetForCard(card);
				if (autoTargetForCard != null)
				{
					Singleton<DWGame>.Instance.SetTarget(PlayerType.User, autoTargetForCard.Creature);
					Singleton<DWGame>.Instance.PlayActionCard(PlayerType.User, card, targetLaneObj.Creature.Owner.Type, targetLaneObj.Creature.Lane.Index);
				}
				else
				{
					StartTargetSelection();
				}
			}
			else
			{
				Singleton<DWGame>.Instance.PlayActionCard(PlayerType.User, card, targetLaneObj.Creature.Owner.Type, targetLaneObj.Creature.Lane.Index);
			}
		}
		else
		{
			CurrentCard = cardPrefab;
			CurrentActionCasterOwner = PlayerType.User;
			CurrentActionTarget = null;
			Singleton<DWGame>.Instance.PlayActionCard(PlayerType.User, card, PlayerType.User);
		}
		PlayActionCardAnimOnHero(PlayerType.User, card);
	}

	public void PlayUserActionCard(AIDecision CurrentChoice)
	{
		CurrentActionCasterOwner = PlayerType.User;
		Singleton<DWGame>.Instance.PlayActionCard(PlayerType.User, CurrentChoice.Card, CurrentChoice.TargetPlayer, CurrentChoice.LaneIndex1, CurrentChoice.ShapeShiftPick);
	}

	public void PlayOpponentActionCard(AIDecision CurrentChoice)
	{
		CurrentActionCasterOwner = PlayerType.Opponent;
		Singleton<DWGame>.Instance.PlayActionCard(PlayerType.Opponent, CurrentChoice.Card, CurrentChoice.TargetPlayer, CurrentChoice.LaneIndex1, CurrentChoice.ShapeShiftPick);
	}

	public void SetLaneColliders(bool enable)
	{
		for (int i = 0; i < 2; i++)
		{
			for (int j = 0; j < BattleLaneObjects[i].Count; j++)
			{
				BattleLaneObjects[i][j].SetColliders(enable);
			}
		}
	}

	private DWBattleLaneObject CreateLaneObjectForPool(
		CreatureItem creature,
		int ownerIndex)
	{
		if (creature == null)
		{
			Debug.LogError(
				"[LaneObjectPool] CreatureItem is NULL!"
			);
			return null;
		}

		if (creature.Form == null)
		{
			Debug.LogError(
				"[LaneObjectPool] Creature.Form is NULL!"
			);
			return null;
		}

		if (LaneObjectPrefab == null)
		{
			Debug.LogError(
				"[LaneObjectPool] LaneObjectPrefab is NULL!"
			);
			return null;
		}

		if (LaneObjParents == null ||
			ownerIndex < 0 ||
			ownerIndex >= LaneObjParents.Length)
		{
			Debug.LogError(
				"[LaneObjectPool] Invalid owner index: " +
				ownerIndex
			);
			return null;
		}

		GameObject obj =
			LaneObjParents[ownerIndex]
				.InstantiateAsChild(LaneObjectPrefab);

		if (obj == null)
		{
			Debug.LogError(
				"[LaneObjectPool] Failed to instantiate object for: " +
				creature.Form.ID
			);
			return null;
		}

		DWBattleLaneObject component =
			obj.GetComponent<DWBattleLaneObject>();

		if (component == null)
		{
			Debug.LogError(
				"[LaneObjectPool] DWBattleLaneObject missing on LaneObjectPrefab!"
			);

			UnityEngine.Object.Destroy(obj);

			return null;
		}

		if (component.ShadowBlob != null)
		{
			component.ShadowBlob.PrepareCache(creature);
		}

		LaneObjectPool[creature] = component;

		obj.SetActive(false);

		Debug.Log(
			"[LaneObjectPool] ADDED: " +
			creature.Form.ID
		);

		return component;
	}

	public void RepositionLaneObjects()
	{
		float num = 0f;
		if (BattleLaneObjects[0].Count > 0)
		{
			float num2 = 0f;
			float[] array = new float[BattleLaneObjects[0].Count];
			for (int i = 0; i < BattleLaneObjects[0].Count; i++)
			{
				array[i] = Mathf.Max(BattleLaneObjects[0][i].Creature.Data.Form.Width, MinimumCreatureWidth);
			}
			for (int j = 0; j < array.Length - 1; j++)
			{
				float b = (array[j] + array[j + 1]) / 2f;
				num2 = Mathf.Max(num2, b);
			}
			if (num2 < MinimumCreatureWidth)
			{
				num2 = MinimumCreatureWidth;
			}
			num = num2 * (float)BattleLaneObjects[0].Count;
			num += CreatureSpacing * (float)(BattleLaneObjects[0].Count - 1);
			float num3 = num / 2f;
			float num4 = num2 / 2f;
			for (int k = 0; k < BattleLaneObjects[0].Count; k++)
			{
				num3 -= num4;
				Vector3 zero = Vector3.zero;
				zero.y = BattleLaneObjects[0][k].transform.position.y;
				zero.z = num3;
				zero.x = Amplitude * (zero.z * zero.z) + CreatureOffsetX;
				MoveLaneObjToPos(BattleLaneObjects[0][k], zero, 90f);
				num3 -= num4;
				num3 -= CreatureSpacing;
			}
		}
		Vector3 tallestEnemyTop = new Vector3(0f - CreatureOffsetX, MinCreatureHeight, 0f);
		float num5 = 0f;
		if (BattleLaneObjects[1].Count > 0)
		{
			float[] array2 = new float[BattleLaneObjects[1].Count];
			for (int l = 0; l < BattleLaneObjects[1].Count; l++)
			{
				float num6 = 1f;
				if (BattleLaneObjects[1][l].Creature.Data.QuestLoadoutEntryData != null)
				{
					num6 = BattleLaneObjects[1][l].Creature.Data.QuestLoadoutEntryData.CreatureScale;
				}
				array2[l] = num6 * Mathf.Max(BattleLaneObjects[1][l].Creature.Data.Form.Width, MinimumCreatureWidth);
				num5 += array2[l];
			}
			num5 += CreatureSpacing * (float)(BattleLaneObjects[1].Count - 1);
			float num7 = num5 / 2f;
			for (int m = 0; m < BattleLaneObjects[1].Count; m++)
			{
				num7 -= array2[m] / 2f;
				Vector3 zero2 = Vector3.zero;
				zero2.y = BattleLaneObjects[1][m].transform.position.y;
				zero2.z = num7;
				zero2.x = Amplitude * (zero2.z * zero2.z) + CreatureOffsetX;
				zero2.x *= -1f;
				MoveLaneObjToPos(BattleLaneObjects[1][m], zero2, -90f);
				num7 -= array2[m] / 2f;
				num7 -= CreatureSpacing;
				Vector3 vector = zero2;
				vector.y = BattleLaneObjects[1][m].Creature.Data.Form.Height;
				if (vector.y > tallestEnemyTop.y)
				{
					tallestEnemyTop = vector;
				}
			}
		}
		Singleton<DWGameCamera>.Instance.UpdateSetupCamPositions(tallestEnemyTop, num, num5);
	}

	private void MoveLaneObjToPos(DWBattleLaneObject laneObj, Vector3 pos, float rotation)
	{
		if (laneObj.InitialPositionSet)
		{
			iTween.MoveTo(laneObj.gameObject, iTween.Hash("position", pos, "time", DeathRepositionTime, "easetype", iTween.EaseType.easeOutCubic));
		}
		else
		{
			laneObj.transform.position = pos;
			laneObj.transform.localEulerAngles = new Vector3(0f, rotation, 0f);
			laneObj.InitialPositionSet = true;
		}
	}

	private void Update()
	{
		if (!mInitialized && SessionManager.Instance.IsLoadDataDone())
		{
			Initialize();
		}
		if (UpdateCreaturePosition)
		{
			RepositionLaneObjects();
			Singleton<DWGameCamera>.Instance.MoveCameraToP1Setup(instant: true);
		}
		UpdateDragArrow();
		if (mFreeCam)
		{
			Singleton<DWGameCamera>.Instance.MainCam.transform.position = Vector3.Lerp(Singleton<DWGameCamera>.Instance.MainCam.transform.position, mTargetCamPos, Time.deltaTime * FreeCamSnapSpeed);
		}
	}

	public void SpawnLootDropInventoryItems(Transform spawnPoint, CreatureData dropCreature)
	{
	}

	public void SpawnLootDropObject(Transform spawnPoint, DropTypeEnum dropType, int rarity, CreatureItem creature)
	{
		DWBattleLootObject component = UnityExtensions.InstantiateAsChild(original: dropType switch
		{
			DropTypeEnum.SoftCurrency => SoftCurrencyDropPrefab, 
			DropTypeEnum.SocialCurrency => SocialCurrencyDropPrefab, 
			DropTypeEnum.HardCurrency => HardCurrencyDropPrefab, 
			_ => Singleton<PrefabReferences>.Instance.LootChests[rarity - 1], 
		}, parent: spawnPoint.transform.parent.parent).GetComponent<DWBattleLootObject>();
		component.transform.position = spawnPoint.transform.position;
		if (component.PickUp)
		{
			CurrentBattleLootInventoryObjects.Add(component);
			component.transform.localRotation = Quaternion.Euler(new Vector3(0f, -90f, 0f));
		}
		else
		{
			CurrentBattleLootCurrencyObjects.Add(component);
			StartCoroutine(SpawnDropAmountLabel(component.MainParticle, spawnPoint, dropType, creature, component));
		}
		component.AlignTexToCamera(Singleton<DWGameCamera>.Instance.MainCam);
	}

	private IEnumerator SpawnDropAmountLabel(ParticleSystem ps, Transform spawnPoint, DropTypeEnum dropType, CreatureItem creature, DWBattleLootObject lootObj)
	{
		Vector3 pos = spawnPoint.transform.position;
		pos.y += 1f;
		Color color = Color.yellow;
		string str2 = "+ ";
		QuestLoadoutEntry.DropInfoClass dropInfo = creature.QuestLoadoutEntryData.DropInfo;
		str2 = dropType switch
		{
			DropTypeEnum.SoftCurrency => str2 + dropInfo.SoftCurrency, 
			DropTypeEnum.SocialCurrency => str2 + dropInfo.SocialCurrency, 
			_ => str2 + "1", 
		};
		if (ps != null)
		{
			while (ps.IsAlive())
			{
				yield return null;
			}
		}
		Singleton<BattleHudController>.Instance.transform.InstantiateAsChild(DropLabelPrefab).GetComponent<ShowEffectDisplayPopup>().Init(null, str2, pos, color, Color.black, 30);
		yield return new WaitForSeconds(1f);
		CurrentBattleLootCurrencyObjects.Remove(lootObj);
	}

	private List<CreatureState> GetFocusCreatures(List<GameMessage> messages)
	{
		List<GameMessage> list = messages.FindAll((GameMessage m) => m.Action == GameEvent.CREATURE_ATTACKED);
		List<CreatureState> list2 = new List<CreatureState>();
		foreach (GameMessage item in list)
		{
			if (!list2.Contains(item.Creature))
			{
				list2.Add(item.Creature);
			}
			if (!list2.Contains(item.SecondCreature))
			{
				list2.Add(item.SecondCreature);
			}
		}
		foreach (GameMessage item2 in messages.FindAll((GameMessage m) => m.Action == GameEvent.DAMAGE_CREATURE))
		{
			if (!list2.Contains(item2.Creature))
			{
				list2.Add(item2.Creature);
			}
		}
		foreach (GameMessage message in messages)
		{
			if (message.Action.ToString().StartsWith("ENABLE"))
			{
				list2.Add(message.Creature);
			}
		}
		return list2;
	}

	public void ShowDragArrow(GameObject sourceCreature, GameObject targetCreature = null)
	{
		DragArrowTransform.gameObject.SetActive(value: true);
		Vector3 position = sourceCreature.transform.position;
		position.y = 0f;
		position.x += ((!(targetCreature == null)) ? (0f - DragArrowStartOffset) : DragArrowStartOffset);
		DragArrowTransform.position = position;
		mDragArrowAiTarget = ((!(targetCreature != null)) ? null : targetCreature.transform);
		UpdateDragArrow();
	}

	public void HideDragArrow()
	{
		if (DragArrowTransform != null && DragArrowTransform.gameObject != null)
		{
			DragArrowTransform.gameObject.SetActive(value: false);
		}
	}

	private void UpdateDragArrow()
	{
		if (DragArrowTransform.gameObject.activeInHierarchy)
		{
			Vector3 vector = ((!(mDragArrowAiTarget != null)) ? BattleHudController.GetWorldPos(Input.mousePosition, DragArrowVerticalOffset) : mDragArrowAiTarget.position);
			vector.y = 0f;
			Vector3 vector2 = vector - DragArrowTransform.position;
			DragArrowTransform.LookAt(vector);
			float num = vector2.magnitude + DragArrowLengthOffset;
			if (mDragArrowAiTarget != null)
			{
				num -= DragArrowAiSpacing;
			}
			DragArrowBodyTransform.SetLocalScaleZ(0f - num);
			DragArrowHeadTransform.SetLocalPositionZ(num);
			float num2 = Mathf.Pow(num, DragArrowCurveExponent);
			Vector2 value = new Vector2(1f, num2 / DragArrowTextureScale);
			mDragArrowMeshMat.SetTextureScale("_MainTex", value);
			mUVOffset.y += Time.deltaTime * DragArrowAnimateSpeed;
			mUVOffset.y %= 256f;
			mDragArrowMeshMat.SetTextureOffset("_MainTex", -mUVOffset);
			float num3 = 1f - 1f / num;
			float num4 = DragArrowHeadHighestAngle + (DragArrowHeadLowestAngle - DragArrowHeadHighestAngle) * num3;
			if (num4 > DragArrowHeadHighestAngle)
			{
				num4 = DragArrowHeadHighestAngle;
			}
			DragArrowHeadTransform.localRotation = Quaternion.Euler(new Vector3(num4, 0f, 0f));
		}
	}

	public IEnumerator ShowAiAttackArrow(GameObject sourceCreature, GameObject targetCreature)
	{
		ShowingAiAttackArrow = true;
		ShowDragArrow(sourceCreature, targetCreature);
		yield return new WaitForSeconds(0.7f);
		HideDragArrow();
		ShowingAiAttackArrow = false;
	}

	public void ShowDamagePredictions(List<int> damageAmounts)
	{
		for (int i = 0; i < BattleLaneObjects[1].Count; i++)
		{
			BattleLaneObjects[1][i].HealthBar.SetPredictedDamage(damageAmounts[i]);
		}
	}

	public void HideDamagePredictions()
	{
		for (int i = 0; i < BattleLaneObjects[1].Count; i++)
		{
			if (BattleLaneObjects[1][i] != null && BattleLaneObjects[1][i].HealthBar != null)
			{
				BattleLaneObjects[1][i].HealthBar.SetPredictedDamage(0f);
			}
		}
	}

	public void UpdateHPBarAttackValues()
	{
		for (int i = 0; i < 2; i++)
		{
			foreach (DWBattleLaneObject item in BattleLaneObjects[i])
			{
				if (item != null)
				{
					item.HealthBar.SetAttackValues(DamageType.Physical);
				}
			}
		}
	}

	public void UpdateAvailableActionIndicators()
	{
		int actionPoints = Singleton<DWGame>.Instance.CurrentBoardState.GetPlayerState(PlayerType.User).ActionPoints;
		bool flag = false;
		foreach (DWBattleLaneObject item in BattleLaneObjects[0])
		{
			if (!item.Creature.IsFrozen && item.Creature.AttackCost <= actionPoints && !Singleton<TutorialController>.Instance.IsBlockActive("IntroBattle"))
			{
				flag = true;
				item.HealthBar.CanAttackIndicator.SetActive(value: true);
			}
			else
			{
				item.HealthBar.CanAttackIndicator.SetActive(value: false);
			}
		}
		bool flag2 = BattleLaneObjects[1].Count > 0;
		if (flag && !flag2)
		{
			if (!Singleton<BattleHudController>.Instance.DragToDrawBannerTween.AnyTweenPlaying())
			{
				Singleton<BattleHudController>.Instance.DragToDrawBannerTween.Play();
			}
		}
		else
		{
			Singleton<BattleHudController>.Instance.DragToDrawBannerTween.StopAndReset();
		}
		foreach (CardPrefabScript handCard in Singleton<HandCardController>.Instance.GetHandCards())
		{
			if (handCard.CanPlay())
			{
				flag = true;
				handCard.SetPlayableIndicator(visible: true);
			}
			else
			{
				handCard.SetPlayableIndicator(visible: false);
			}
		}
		if (flag)
		{
			Singleton<BattleHudController>.Instance.EndTurnAvailableTween.StopAndReset();
			Singleton<BattleHudController>.Instance.EndTurnGlow.SetActive(value: false);
		}
		else if (!Singleton<DWGame>.Instance.IsGameOver() && Singleton<DWGame>.Instance.GetCurrentGameState() != GameState.EndGameWait)
		{
			Singleton<BattleHudController>.Instance.EndTurnGlow.SetActive(value: true);
			Singleton<BattleHudController>.Instance.EndTurnAvailableTween.Play();
			if (Singleton<TutorialController>.Instance.IsBlockActive("IntroBattle"))
			{
				Singleton<BattleHudController>.Instance.EndTurnFinger.SetActive(value: false);
			}
		}
	}

	public void HideAvailableActionIndicators()
	{
		foreach (DWBattleLaneObject item in BattleLaneObjects[0])
		{
			item.HealthBar.CanAttackIndicator.SetActive(value: false);
		}
		foreach (CardPrefabScript handCard in Singleton<HandCardController>.Instance.GetHandCards())
		{
			handCard.SetPlayableIndicator(visible: false);
		}
		Singleton<BattleHudController>.Instance.EndTurnAvailableTween.StopAndReset();
		Singleton<BattleHudController>.Instance.EndTurnGlow.SetActive(value: false);
		Singleton<BattleHudController>.Instance.DragToDrawBannerTween.StopAndReset();
	}

	private void OnDrawGizmosSelected()
	{
		if (!Application.isPlaying)
		{
			return;
		}
		Gizmos.color = Color.yellow;
		for (int i = 0; i < 2; i++)
		{
			if (BattleLaneObjects[i] == null)
			{
				continue;
			}
			foreach (DWBattleLaneObject item in BattleLaneObjects[i])
			{
				if (item != null && item.Creature != null)
				{
					Vector3 size = new Vector3(item.Creature.Data.Form.Width, item.Creature.Data.Form.Height, item.Creature.Data.Form.Width);
					float num = 1f;
					if (i == 1 && item.Creature.Data.QuestLoadoutEntryData != null)
					{
						num = item.Creature.Data.QuestLoadoutEntryData.CreatureScale;
					}
					size *= num;
					Vector3 position = item.transform.position;
					position.y += size.y / 2f;
					if (size.z < MinimumCreatureWidth)
					{
						Gizmos.color = Color.red;
						Gizmos.DrawWireCube(position, size);
						Gizmos.color = Color.yellow;
						size.z = MinimumCreatureWidth;
						size.x = MinimumCreatureWidth;
					}
					Gizmos.DrawWireCube(position, size);
				}
			}
		}
	}

	public DWBattleLaneObject GetAutoTargetForCard(CardData card)
	{
		List<DWBattleLaneObject> list = BattleLaneObjects[1];
		DWBattleLaneObject dWBattleLaneObject = null;
		foreach (DWBattleLaneObject item in list)
		{
			if ((bool)item.Creature.HasBravery)
			{
				if (!(dWBattleLaneObject == null))
				{
					dWBattleLaneObject = null;
					break;
				}
				dWBattleLaneObject = item;
			}
		}
		if (dWBattleLaneObject != null)
		{
			return dWBattleLaneObject;
		}
		if (card.Target2Group == AttackRange.All)
		{
			return list[list.Count / 2];
		}
		if (card.Target2Group == AttackRange.Triple)
		{
			if (list.Count <= 3)
			{
				return list[list.Count / 2];
			}
		}
		else if (card.Target2Group == AttackRange.Double)
		{
			if (list.Count <= 2)
			{
				return list[0];
			}
		}
		else if (card.Target2Group == AttackRange.Single && list.Count <= 1)
		{
			return list[0];
		}
		return null;
	}

	public void PulsePlayerAttackValues(AttackBase type)
	{
		foreach (DWBattleLaneObject item in BattleLaneObjects[0])
		{
			if (type == AttackBase.STR || type == AttackBase.Both)
			{
				if (!item.HealthBar.FlashAttackValueTween.AnyTweenPlaying())
				{
					item.HealthBar.FlashAttackValueTween.Play();
				}
			}
			else
			{
				item.HealthBar.FlashAttackValueTween.StopAndReset();
			}
			if (type == AttackBase.INT || type == AttackBase.Both)
			{
				if (!item.HealthBar.FlashMagicValueTween.AnyTweenPlaying())
				{
					item.HealthBar.FlashMagicValueTween.Play();
				}
			}
			else
			{
				item.HealthBar.FlashMagicValueTween.StopAndReset();
			}
		}
	}

	private void ApplyColorToParticle(GameObject obj, Color color)
	{
		ParticleSystem[] componentsInChildren = obj.GetComponentsInChildren<ParticleSystem>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].startColor = color;
		}
	}

	private Color GetEvoMaterialColor(Texture2D tex)
	{
		if (tex.format == TextureFormat.RGBA32)
		{
			Color[] pixels = tex.GetPixels(tex.width / 4, tex.height / 4, tex.width / 2, tex.height / 2);
			float num = 0f;
			float num2 = 0f;
			float num3 = 0f;
			for (int i = 0; i < pixels.Length; i++)
			{
				if (pixels[i].a != 0f)
				{
					num += pixels[i].r;
					num2 += pixels[i].g;
					num3 += pixels[i].b;
				}
			}
			return new Color(num / (float)pixels.Length, num2 / (float)pixels.Length, num3 / (float)pixels.Length, 1f);
		}
		return Color.white;
	}

	private void OnPress(bool pressed)
	{
		if (EnableFreeCam && mFreeCam && !pressed)
		{
			EndFreeCam();
		}
	}

	public void EndFreeCam()
	{
		if (mFreeCam)
		{
			mFreeCam = false;
			Singleton<DWGameCamera>.Instance.RenderP1Character(render: false);
			Singleton<DWGameCamera>.Instance.MoveCameraToP1Setup(instant: true);
		}
	}

	private void OnDrag(Vector2 delta)
	{
		if (EnableFreeCam && Singleton<DWGame>.Instance.GetCurrentGameState().IsP1Turn() && CurrentBattleLootInventoryObjects.Count <= 0)
		{
			if (!mFreeCam)
			{
				mFreeCam = true;
				Vector3 position = Singleton<DWGameCamera>.Instance.MainCam.transform.position;
				Vector3 forward = Singleton<DWGameCamera>.Instance.MainCam.transform.forward;
				Plane plane = new Plane(Vector3.right, Vector3.zero);
				Ray ray = new Ray(position, forward);
				plane.Raycast(ray, out mCamDistance);
				Vector3 vector = position + forward * mCamDistance;
				Singleton<DWGameCamera>.Instance.MainCamLookAt.transform.position = vector;
				mCamLookAtForward = (position - vector).normalized;
				mFreeCamRotation = Vector2.zero;
				Singleton<DWGameCamera>.Instance.RenderP1Character(render: true);
			}
			delta.y *= -1f;
			mFreeCamRotation += delta * FreeCamSensitivity;
			mFreeCamRotation.y = Mathf.Clamp(mFreeCamRotation.y, FreeCamMinHeight, FreeCamMaxHeight);
			Vector3 vector2 = Quaternion.AngleAxis(mFreeCamRotation.x, Vector3.up) * mCamLookAtForward;
			Vector3 axis = Vector3.Cross(vector2, Vector3.up);
			vector2 = Quaternion.AngleAxis(mFreeCamRotation.y, axis) * vector2;
			mTargetCamPos = Singleton<DWGameCamera>.Instance.MainCamLookAt.transform.position + vector2 * mCamDistance;
		}
	}

	public void ClearPooledData()
	{
		CreaturePool.Clear();
		CreatureVFXPool.Clear();
		LaneObjectPool.Clear();
	}

	public void CancelDragAttack()
	{
		foreach (DWBattleLaneObject item in BattleLaneObjects[0])
		{
			item.HideDragAttackInfo(atTimeUp: true);
		}
	}

	public void ShowEnvVFXObj(bool toogleEnvVFX)
	{
		GameObject gameObject = null;
		if (EnvironmentObj != null)
		{
			gameObject = EnvironmentObj.FindInChildren("FX_BG");
		}
		if (gameObject != null)
		{
			gameObject.SetActive(toogleEnvVFX);
		}
	}

	public IEnumerator ShowBoardAnim()
	{
		ShowEnvVFXObj(toogleEnvVFX: true);
		BoardObj.SetActive(value: true);
		yield return new WaitForSeconds(HologramDisappearDelay);
		BoardHologram.SetActive(value: false);
	}
}
