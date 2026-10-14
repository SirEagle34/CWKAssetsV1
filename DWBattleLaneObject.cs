using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DWBattleLaneObject : MonoBehaviour
{
	public float LandscapeLiftLerpSpeed;

	public CreatureState Creature;

	public ShadowBlob ShadowBlob;

	public GameObject CreatureObject;

	public GameObject SwappedCreatureObject;

	public CreatureHPBar HealthBar;

	public BoxCollider LaneCollider;

	public bool IsFrozen;

	public bool IsBunny;

	private bool mHolding;

	private float mHoldTime;

	private Vector3 mHoldStartPos;

	private DWBattleLaneObject mPrevHoveredLane;

	private CardPrefabScript mDraggingToDrawCard;

	private bool mHasLandscapeEffect;

	private static CreatureState mSelectedCardTarget;

	public bool IgnoreBoardTint;

	public bool IsTransfmogrified;

	private List<GameObject> CreatureStatusFXObjects = new List<GameObject>();

	private List<GameEvent> ActiveCreaturePersistentFXList = new List<GameEvent>();

	private List<StatusData> ActiveCreaturePersistentFXStatus = new List<StatusData>();

	private GameObject mFXObjToFadeOut;

	private bool mFadeOutStatus;

	private float mFadeOutTime = 2f;

	public bool InitialPositionSet { get; set; }

	public Vector3 AttackStartPosition { get; set; }

	public bool DyingThisFrame { get; set; }

	public ShowEffectDisplayPopup AttachedEffectPopup { get; set; }

	private static StatusData FXStatusKey(GameMessage ms)
	{
		if (ms.Action != GameEvent.ENABLE_DATASTATUS && ms.Action != GameEvent.DISABLE_DATASTATUS)
		{
			return null;
		}
		return ms.Status;
	}

	private void Awake()
	{
		Transform[] componentsInChildren = base.transform.GetComponentsInChildren<Transform>(includeInactive: true);
		foreach (Transform transform in componentsInChildren)
		{
			if (transform.name == "ShadowBlob")
			{
				ShadowBlob = transform.GetComponent<ShadowBlob>();
			}
		}
		LaneCollider = GetComponent<BoxCollider>();
	}

	private Vector3 GetPosOnCircle(float angle, float radius)
	{
		float z = Mathf.Cos(angle * (MathF.PI / 180f)) * radius;
		return new Vector3(Mathf.Sin(angle * (MathF.PI / 180f)) * radius, 0f, z);
	}

	private List<Color> GetColorArray()
	{
		return new List<Color>
		{
			Color.Lerp(Color.yellow, Color.green, 0.5f),
			Color.yellow,
			Color.Lerp(Color.yellow, Color.red, 0.3f),
			Color.Lerp(Color.yellow, Color.red, 0.7f),
			Color.Lerp(Color.grey, Color.red, 0.5f),
			Color.Lerp(Color.red, Color.white, 0.5f),
			Color.magenta,
			Color.blue,
			Color.cyan,
			Color.green
		};
	}

	private void OnClick()
	{
		if (CanInteract() && !Singleton<DWGame>.Instance.SelectingLane && Creature != null && !Singleton<TutorialController>.Instance.IsStateActive("Q1_BattlePlayCard"))
		{
			ShowCreatureInfoPopup();
		}
	}

	private void OnDragOver(GameObject go)
	{
		if (Singleton<DWGame>.Instance.SelectingLane)
		{
			if (Creature.Owner.Type == PlayerType.Opponent)
			{
				mSelectedCardTarget = Creature;
				Singleton<DWGame>.Instance.SetTarget(PlayerType.User, Creature);
				Singleton<DWBattleLane>.Instance.ShowDamagePredictions(Singleton<DWBattleLane>.Instance.CurrentActionTarget.Creature.PredictCardDamage(Singleton<DWBattleLane>.Instance.CurrentCard.Card));
				Singleton<DWBattleLane>.Instance.SetTargetIndicators(Singleton<DWBattleLane>.Instance.CurrentCard.Card, this, attackTarget: true);
			}
			else
			{
				mSelectedCardTarget = null;
				Singleton<DWBattleLane>.Instance.HideDamagePredictions();
				Singleton<DWBattleLane>.Instance.HideTargetIndicators();
			}
		}
	}

	private void OnPress(bool pressed)
	{
		if (pressed && CanInteract())
		{
			mHolding = true;
			mHoldTime = 0f;
			mHoldStartPos = Input.mousePosition;
			if (Singleton<DWGame>.Instance.SelectingLane)
			{
				OnDragOver(null);
			}
			return;
		}
		mHolding = false;
		if (CanInteract())
		{
			if (Singleton<DWGame>.Instance.SelectingLane)
			{
				if (mSelectedCardTarget != null)
				{
					if (Singleton<DWGame>.Instance.IsCreatureTargetRestricted(mSelectedCardTarget))
					{
						Singleton<BattleHudController>.Instance.ShowErrorReason(KFFLocalization.Get("!!MUST_TARGET_BRAVERY"));
						Singleton<SLOTAudioManager>.Instance.PlaySound("battle/SFX_Announcer_TryAgain");
					}
					else
					{
						StartCoroutine(Singleton<DWBattleLane>.Instance.CurrentCard.ShowPlayAnim());
						Singleton<DWBattleLane>.Instance.EndTargetSelection();
						CreatureState creature = Singleton<DWBattleLane>.Instance.CurrentActionTarget.Creature;
						Singleton<DWGame>.Instance.PlayActionCard(PlayerType.User, Singleton<DWBattleLane>.Instance.CurrentCard.Card, creature.Owner.Type, creature.Lane.Index);
						Singleton<DWGame>.Instance.SelectingLane = false;
					}
				}
			}
			else if ((Input.mousePosition - mHoldStartPos).y > Singleton<DWBattleLane>.Instance.AttackDragStartDistance)
			{
				if (mPrevHoveredLane != null && mPrevHoveredLane.Creature.Owner.Type == PlayerType.Opponent)
				{
					Singleton<BattleHudController>.Instance.HideDragActionCost();
					Singleton<DWBattleLane>.Instance.HideDragArrow();
					HealthBar.FlashAttackCostTween.StopAndReset();
					HealthBar.FlashAttackValueTween.StopAndReset();
					if (!Singleton<TutorialController>.Instance.IsStateActive("Q3C_LeaderBar4"))
					{
						if (Singleton<DWGame>.Instance.IsCreatureTargetRestricted(mPrevHoveredLane.Creature))
						{
							Singleton<BattleHudController>.Instance.ShowErrorReason(KFFLocalization.Get("!!MUST_TARGET_BRAVERY"));
							Singleton<SLOTAudioManager>.Instance.PlaySound("battle/SFX_Announcer_TryAgain");
						}
						else if (Singleton<DWGame>.Instance.GetActionPoints(PlayerType.User) < Creature.AttackCost && !Singleton<TutorialController>.Instance.IgnoreEnergyCosts())
						{
							Singleton<BattleHudController>.Instance.ShowErrorReason(KFFLocalization.Get("!!NOT_ENOUGH_ENERGY"));
							Singleton<SLOTAudioManager>.Instance.PlaySound("battle/SFX_Announcer_OutofEnergy");
						}
						else
						{
							if (Singleton<PlayerInfoScript>.Instance.StateData.MultiplayerMode)
							{
								Singleton<MultiplayerMessageHandler>.Instance.SendDragAttack(Creature.Lane.Index, mPrevHoveredLane.Creature.Lane.Index);
							}
							DetachedSingleton<ConditionalTutorialController>.Instance.OnCreatureAttack(Creature);
							Singleton<TutorialController>.Instance.CheckTutorialForceDraw(Creature);
							Singleton<DWGame>.Instance.DragAttack(PlayerType.User, Creature.Lane.Index, mPrevHoveredLane.Creature.Lane.Index);
							Singleton<TutorialController>.Instance.PassIfOnAttack();
							Singleton<SLOTAudioManager>.Instance.SetVOEventCooldown(VOEvent.Idle);
						}
					}
				}
				else if (mDraggingToDrawCard != null)
				{
					if (Singleton<DWGame>.Instance.GetActionPoints(PlayerType.User) < Creature.AttackCost && !Singleton<TutorialController>.Instance.IgnoreEnergyCosts())
					{
						Singleton<BattleHudController>.Instance.ShowErrorReason(KFFLocalization.Get("!!NOT_ENOUGH_ENERGY"));
						Singleton<SLOTAudioManager>.Instance.PlaySound("battle/SFX_Announcer_OutofEnergy");
						NGUITools.Destroy(mDraggingToDrawCard.gameObject);
					}
					else
					{
						if (Singleton<PlayerInfoScript>.Instance.StateData.MultiplayerMode)
						{
							Singleton<MultiplayerMessageHandler>.Instance.SendDragAttack(Creature.Lane.Index, -1);
						}
						Singleton<DWGameMessageHandler>.Instance.SetDragDrawCard(mDraggingToDrawCard);
						mDraggingToDrawCard.SetTargetAlpha(0f, 8f);
						Singleton<DWGame>.Instance.DragAttack(PlayerType.User, Creature.Lane.Index, -1);
						Singleton<SLOTAudioManager>.Instance.SetVOEventCooldown(VOEvent.Idle);
					}
					mDraggingToDrawCard = null;
				}
			}
		}
		Singleton<DWBattleLane>.Instance.HideTargetIndicators();
		Singleton<DWBattleLane>.Instance.HideDamagePredictions();
		mPrevHoveredLane = null;
	}

	public void CancelPress()
	{
		if (mDraggingToDrawCard != null)
		{
			NGUITools.Destroy(mDraggingToDrawCard.gameObject);
			mDraggingToDrawCard = null;
		}
		mPrevHoveredLane = null;
		HideDragAttackInfo();
		mHolding = false;
	}

	private bool CanInteract()
	{
		if (Singleton<DWGame>.Instance.GetCurrentGameState().IsP1Turn())
		{
			return !Singleton<DWBattleLane>.Instance.LootObjectsToCollect();
		}
		return false;
	}

	private void Update()
	{
		if (mHolding && CanInteract() && !Singleton<DWGame>.Instance.SelectingLane && Creature != null && Creature.Owner.Type == PlayerType.User && !IsFrozen && !Creature.IsFrozen)
		{
			if ((Input.mousePosition - mHoldStartPos).y > Singleton<DWBattleLane>.Instance.AttackDragStartDistance)
			{
				if (Singleton<DWGame>.Instance.CurrentBoardState.GetCreatureCount(PlayerType.Opponent) == 0)
				{
					if (mDraggingToDrawCard == null)
					{
						mDraggingToDrawCard = Singleton<HandCardController>.Instance.CreateDragDrawCard();
					}
				}
				else
				{
					DWBattleLaneObject hoveredLane = BattleHudController.GetHoveredLane();
					if (hoveredLane != mPrevHoveredLane)
					{
						if (hoveredLane != null && hoveredLane.Creature.Owner.Type == PlayerType.Opponent)
						{
							Singleton<BattleHudController>.Instance.ShowDragActionCost(Creature.AttackCost);
							Singleton<DWBattleLane>.Instance.ShowDragArrow(CreatureObject);
							HealthBar.FlashAttackCostTween.Play();
							HealthBar.FlashAttackValueTween.Play();
							bool validTarget;
							if (Singleton<DWGame>.Instance.IsCreatureTargetRestricted(hoveredLane.Creature))
							{
								validTarget = false;
								Singleton<DWBattleLane>.Instance.HideDamagePredictions();
							}
							else
							{
								validTarget = true;
								Singleton<DWBattleLane>.Instance.ShowDamagePredictions(Creature.PredictDragAttackDamage(hoveredLane.Creature));
							}
							Singleton<DWBattleLane>.Instance.SetTargetIndicator(hoveredLane.transform.position, validTarget);
						}
						else
						{
							HideDragAttackInfo();
						}
					}
					mPrevHoveredLane = hoveredLane;
				}
			}
			else
			{
				mPrevHoveredLane = null;
				HideDragAttackInfo();
				if (mDraggingToDrawCard != null)
				{
					NGUITools.Destroy(mDraggingToDrawCard.gameObject);
					mDraggingToDrawCard = null;
				}
			}
		}
		float num = ((!mHasLandscapeEffect) ? 0f : 1f);
		if (base.transform.position.y != num)
		{
			Vector3 position = base.transform.position;
			position.y = num;
			base.transform.position = Vector3.Lerp(base.transform.position, position, LandscapeLiftLerpSpeed * Time.deltaTime);
			if (Mathf.Abs(base.transform.position.y - num) < 0.01f)
			{
				base.transform.position = position;
			}
		}
	}

	public void HideDragAttackInfo(bool atTimeUp = false)
	{
		if (mHolding)
		{
			Singleton<BattleHudController>.Instance.HideDragActionCost();
			Singleton<DWBattleLane>.Instance.HideTargetIndicators();
			Singleton<DWBattleLane>.Instance.HideDragArrow();
			Singleton<DWBattleLane>.Instance.HideDamagePredictions();
			if (HealthBar != null && HealthBar.FlashAttackCostTween != null)
			{
				HealthBar.FlashAttackCostTween.StopAndReset();
			}
			if (HealthBar != null && HealthBar.FlashAttackValueTween != null)
			{
				HealthBar.FlashAttackValueTween.StopAndReset();
			}
			if (atTimeUp)
			{
				mHolding = false;
				mPrevHoveredLane = null;
			}
		}
	}

	private void ShowCreatureInfoPopup()
	{
		Singleton<CreatureInfoPopup>.Instance.Show(Creature);
		Singleton<DWGameCamera>.Instance.RenderP1Character(render: true);
		if (!Singleton<DWGameCamera>.Instance.UseDetailCam)
		{
			Singleton<DWGameCamera>.Instance.MoveCameraToCreatureDetail(Creature, instant: true);
		}
		else
		{
			StartCoroutine(SetCreatureDetailCam());
		}
	}

	private IEnumerator SetCreatureDetailCam()
	{
		yield return new WaitForSeconds(0.3f);
		Singleton<DWGameCamera>.Instance.SetCreatureDetailCam(enable: true, Creature);
	}

	private void OnDragStart()
	{
	}

	private void OnDrag()
	{
	}

	private void OnDragEnd()
	{
	}

	public void SetColliders(bool enable)
	{
		Collider[] componentsInChildren = GetComponentsInChildren<Collider>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].enabled = enable;
		}
	}

	public void FreezeCreature()
	{
		IsFrozen = true;
		StartCoroutine(StopCreatureAnimAfterDelay(0.1f));
		SwapMaterials(Singleton<DWGameMessageHandler>.Instance.FreezeDesatMaterial);
	}

	public void AssignFadeMatToCreature()
	{
		SwapMaterials(Singleton<DWBattleLane>.Instance.CreatureDeathMaterial);
	}

	private void SwapMaterials(Material matToSwap)
	{
		IgnoreBoardTint = true;
		BattleCreatureAnimState[] componentsInChildren = CreatureObject.GetComponentsInChildren<BattleCreatureAnimState>(includeInactive: true);
		if (componentsInChildren == null)
		{
			return;
		}
		List<BattleCreatureAnimState> list = new List<BattleCreatureAnimState>();
		list.Add(componentsInChildren[0]);
		if (SwappedCreatureObject != null)
		{
			BattleCreatureAnimState componentInChildren = SwappedCreatureObject.GetComponentInChildren<BattleCreatureAnimState>();
			list.Add(componentInChildren);
		}
		BattleCreatureAnimState[] array = componentsInChildren;
		foreach (BattleCreatureAnimState battleCreatureAnimState in array)
		{
			for (int j = 0; j < battleCreatureAnimState.orignalMeshes.Count; j++)
			{
				SkinnedMeshRenderer skinnedMeshRenderer = battleCreatureAnimState.orignalMeshes[j];
				Material material = UnityEngine.Object.Instantiate(matToSwap);
				material.mainTexture = skinnedMeshRenderer.material.mainTexture;
				skinnedMeshRenderer.material = material;
			}
		}
	}

	public void RevertMaterials()
	{
		IgnoreBoardTint = false;
		BattleCreatureAnimState[] componentsInChildren = CreatureObject.GetComponentsInChildren<BattleCreatureAnimState>(includeInactive: true);
		if (componentsInChildren != null)
		{
			BattleCreatureAnimState battleCreatureAnimState = componentsInChildren[0];
			for (int i = 0; i < battleCreatureAnimState.orignalMeshes.Count; i++)
			{
				battleCreatureAnimState.orignalMeshes[i].material = battleCreatureAnimState.originalMats[i];
			}
		}
	}

	public void UnfreezeCreature()
	{
		IsFrozen = false;
		RestartCreatureIdle();
		RevertMaterials();
	}

	public void StealthCreature()
	{
		SwapMaterials(Singleton<DWGameMessageHandler>.Instance.StealthMaterial);
	}

	public void UnStealthCreature()
	{
		RevertMaterials();
	}

	public void TransmogrifyCreature()
	{
		IsBunny = true;
		Transform transform = CreatureObject.transform;
		if (SwappedCreatureObject != null)
		{
			return;
		}
		SwappedCreatureObject = base.transform.InstantiateAsChild(Singleton<DWGameMessageHandler>.Instance.TransmogrifyCreature);
		if (IsFrozen)
		{
			Animator componentInChildren = SwappedCreatureObject.GetComponentInChildren<Animator>();
			if (componentInChildren != null)
			{
				componentInChildren.speed = 0f;
			}
		}
		SwappedCreatureObject.transform.rotation = transform.rotation;
		BattleCreatureAnimState componentInChildren2 = SwappedCreatureObject.GetComponentInChildren<BattleCreatureAnimState>();
		for (int i = 0; i < ActiveCreaturePersistentFXList.Count; i++)
		{
			if (ActiveCreaturePersistentFXList[i] == GameEvent.ENABLE_BLIND)
			{
				CreatureStatusFXObjects[i].transform.parent = componentInChildren2.AttachBoneBlindEffect;
			}
			else if (CreatureStatusFXObjects[i].transform.parent == CreatureObject.transform)
			{
				CreatureStatusFXObjects[i].transform.parent = SwappedCreatureObject.transform;
			}
		}
		CreatureObject.SetActive(value: false);
	}

	public void UntransmogrifyCreature()
	{
		IsBunny = false;
		CreatureObject.SetActive(value: true);
		BattleCreatureAnimState componentInChildren = CreatureObject.GetComponentInChildren<BattleCreatureAnimState>();
		for (int i = 0; i < ActiveCreaturePersistentFXList.Count; i++)
		{
			if (ActiveCreaturePersistentFXList[i] == GameEvent.ENABLE_BLIND)
			{
				CreatureStatusFXObjects[i].transform.parent = componentInChildren.AttachBoneBlindEffect;
			}
			else if (CreatureStatusFXObjects[i].transform.parent == SwappedCreatureObject.transform)
			{
				CreatureStatusFXObjects[i].transform.parent = CreatureObject.transform;
			}
		}
		UnityEngine.Object.Destroy(SwappedCreatureObject);
		SwappedCreatureObject = null;
	}

	public void ApplyStatusEffectObjOnCreature(GameMessage ms)
	{
		for (int i = 0; i < ActiveCreaturePersistentFXList.Count; i++)
		{
			if (ActiveCreaturePersistentFXList[i] == ms.Action && ActiveCreaturePersistentFXStatus[i] == FXStatusKey(ms))
			{
				return;
			}
		}
		GameObject creatureObject = Singleton<DWBattleLane>.Instance.GetCreatureObject(Creature);
		Transform transform = creatureObject.transform;
		Vector3 localPosition = Vector3.zero;
		if (ms.Action == GameEvent.ENABLE_BLIND)
		{
			Transform transform2 = Singleton<DWGameMessageHandler>.Instance.FindInChildren(creatureObject.transform, "Puppet_Head");
			BattleCreatureAnimState componentInChildren = creatureObject.GetComponentInChildren<BattleCreatureAnimState>();
			if (componentInChildren.AttachBoneBlindEffect != null)
			{
				transform2 = componentInChildren.AttachBoneBlindEffect;
			}
			localPosition = componentInChildren.BlindEffectLocalOffset;
			if (transform2 != null)
			{
				transform = transform2;
			}
		}
		GameEventFXData gameEventFXData = DataStatus.EventFX(ms);
		if (gameEventFXData == null)
		{
			return;
		}
		GameObject gameObject = SLOTResourceManager.Load("VFX/Actions/" + gameEventFXData.CreatureStatusFXPrefab, typeof(GameObject)) as GameObject;
		if (gameObject != null)
		{
			if (gameEventFXData.DetachStatusFXFromCreature)
			{
				transform = transform.transform.parent;
			}
			GameObject gameObject2 = transform.InstantiateAsChild(gameObject);
			gameObject2.transform.localPosition = localPosition;
			ActiveCreaturePersistentFXList.Add(ms.Action);
			ActiveCreaturePersistentFXStatus.Add(FXStatusKey(ms));
			CreatureStatusFXObjects.Add(gameObject2);
		}
		if (ms.Status != null && ms.Status.IsLandscape)
		{
			mHasLandscapeEffect = true;
		}
	}

	public void RemoveStatusEffectObjFromCreature(GameMessage ms)
	{
		GameEvent disableEventFX = GetDisableEventFX(ms);
		if (disableEventFX == GameEvent.NONE)
		{
			return;
		}
		for (int i = 0; i < ActiveCreaturePersistentFXList.Count; i++)
		{
			if (ActiveCreaturePersistentFXList[i] == disableEventFX && ActiveCreaturePersistentFXStatus[i] == FXStatusKey(ms))
			{
				ActiveCreaturePersistentFXList.RemoveAt(i);
				ActiveCreaturePersistentFXStatus.RemoveAt(i);
				GameObject gameObject = CreatureStatusFXObjects[i];
				CreatureStatusFXObjects.RemoveAt(i);
				if (gameObject != null)
				{
					StartCoroutine(FadeOutStatusEffectFromCreature(gameObject));
				}
				break;
			}
		}
		if (ms.Status != null && ms.Status.IsLandscape)
		{
			mHasLandscapeEffect = false;
		}
	}

	public void RemoveAllStatusEffectObjFromCreature()
	{
		for (int i = 0; i < CreatureStatusFXObjects.Count; i++)
		{
			GameObject gameObject = CreatureStatusFXObjects[i];
			if (gameObject != null)
			{
				UnityEngine.Object.Destroy(gameObject);
			}
		}
	}

	private IEnumerator FadeOutStatusEffectFromCreature(GameObject obj)
	{
		Renderer[] componentsInChildren = obj.GetComponentsInChildren<Renderer>(includeInactive: true);
		foreach (Renderer renderer in componentsInChildren)
		{
			if (renderer.material.HasProperty("_Color"))
			{
				iTween.FadeTo(renderer.gameObject, iTween.Hash("alpha", 0f, "time", 2f));
			}
		}
		ParticleSystem[] componentsInChildren2 = obj.GetComponentsInChildren<ParticleSystem>(includeInactive: true);
		for (int i = 0; i < componentsInChildren2.Length; i++)
		{
			componentsInChildren2[i].loop = false;
		}
		UITexture[] componentsInChildren3 = obj.GetComponentsInChildren<UITexture>(includeInactive: true);
		for (int i = 0; i < componentsInChildren3.Length; i++)
		{
			TweenAlpha tweenAlpha = componentsInChildren3[i].gameObject.AddComponent<TweenAlpha>();
			tweenAlpha.to = 0f;
			tweenAlpha.duration = 2f;
			tweenAlpha.PlayForward();
		}
		yield return new WaitForSeconds(2f);
		UnityEngine.Object.Destroy(obj);
	}

	private IEnumerator StopCreatureAnimAfterDelay(float delay)
	{
		Singleton<DWGameMessageHandler>.Instance.PlayCreatureAnim(Creature, "HitReaction");
		yield return new WaitForSeconds(delay);
		foreach (GameObject creatureObject in Singleton<DWBattleLane>.Instance.GetCreatureObjects(Creature))
		{
			if (creatureObject != null)
			{
				creatureObject.GetComponentsInChildren<Animator>(includeInactive: true)[0].speed = 0f;
			}
		}
	}

	private void RestartCreatureIdle()
	{
		Singleton<DWBattleLane>.Instance.GetCreatureObject(Creature).GetComponentsInChildren<Animator>(includeInactive: true)[0].speed = 1f;
		Singleton<DWGameMessageHandler>.Instance.PlayCreatureAnim(Creature, "ToIdle");
	}

	private GameEvent GetDisableEventFX(GameMessage ms)
	{
		GameEvent result = GameEvent.NONE;
		foreach (StatusData item in StatusDataManager.Instance.GetDatabase())
		{
			if (item.DisableMessage == ms.Action)
			{
				return result = item.EnableMessage;
			}
		}
		return result;
	}
}
