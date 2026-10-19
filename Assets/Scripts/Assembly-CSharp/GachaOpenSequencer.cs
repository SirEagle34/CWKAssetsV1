using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GachaOpenSequencer : Singleton<GachaOpenSequencer>
{
	public float CreatureHeightMod = 1f;

	public int BattleCameraDepth;

	public UITweenController FadeToBlackTween;

	public UITweenController FadeBackInTween;

	public UITweenController CreatureInfoHideTween;

	public UITweenController CreatureChestOpenTween;

	public UITweenController ShowInfoPanelCloseButtonTween;

	public CreatureStatsPanel StatsPanel;

	public Transform ChestNode;

	public Camera ChestCamera;

	public Camera CreatureCamera;

	public List<Transform> CardNodes;

	public GameObject tipsPanel;

	public GameObject VFX_CardSwap_Prefab;

	public GameObject CreatureNode;

	private InventorySlotItem mCreature;

	private GameObject mLoadedModelInstance;

	public GameObject[] RarityToppers;

	public GameObject[] RarityGlows;

	public GameObject[] RarityPlatforms;

	private bool mClicked;

	public bool Showing { get; private set; }

	public void ShowGachaSequence(InventorySlotItem item, Action onOpened = null)
	{
		StartCoroutine(ShowGachaSequenceCo(item, onOpened));
		if (DetachedSingleton<SceneFlowManager>.Instance.InBattleScene())
		{
			Singleton<BattleResultsController>.Instance.HideRewardsDuringGacha.Play();
		}
	}

	private IEnumerator ShowGachaSequenceCo(InventorySlotItem item, Action onOpened)
	{
		Showing = true;
		if (DetachedSingleton<SceneFlowManager>.Instance.InBattleScene())
		{
			ChestCamera.depth = BattleCameraDepth;
			CreatureCamera.depth = BattleCameraDepth;
		}
		ChestCamera.enabled = true;
		CreatureCamera.enabled = true;
		bool isCreature = item.SlotType == InventorySlotType.Creature;
		if (isCreature)
		{
			mCreature = item;
		}
		GameObject gameObject = ChestNode.InstantiateAsChild(Singleton<PrefabReferences>.Instance.GachaChests[item.Rarity - 1]);
		gameObject.ChangeLayerToParent();
		ChestSkins.Apply(gameObject);
		ChestNode.gameObject.SetActive(value: false);
		GachaChestObject chest = gameObject.GetComponent<GachaChestObject>();
		if (chest == null)
		{
			chest = gameObject.GetComponentInChildren<GachaChestObject>();
		}
		CardPrefabScript spawnedCard = chest.CardNode.InstantiateAsChild(Singleton<PrefabReferences>.Instance.Card).GetComponent<CardPrefabScript>();
		spawnedCard.gameObject.ChangeLayerToParent();
		spawnedCard.transform.localPosition = Vector3.zero;
		spawnedCard.transform.localRotation = Quaternion.identity;
		spawnedCard.transform.localScale = Vector3.one;
		spawnedCard.InGacha = true;
		spawnedCard.Mode = CardPrefabScript.CardMode.GeneralFrontEnd;
		spawnedCard.CardCollider.enabled = false;
		if (item.SlotType == InventorySlotType.Card)
		{
			spawnedCard.Populate(item.Card.Form);
		}
		else
		{
			spawnedCard.Populate(item);
		}
		spawnedCard.gameObject.SetActive(value: false);
		if (isCreature)
		{
			StatsPanel.Populate(item);
			for (int i = 0; i < item.Creature.Form.ActionCards.Count; i++)
			{
				CardData cardData = item.Creature.Form.ActionCards[i];
				if (cardData != null)
				{
					CardPrefabScript component = CardNodes[i].InstantiateAsChild(Singleton<PrefabReferences>.Instance.Card).GetComponent<CardPrefabScript>();
					component.gameObject.ChangeLayerToParent();
					component.Mode = CardPrefabScript.CardMode.GeneralFrontEnd;
					component.Populate(cardData);
					component.AdjustDepth(i + 1);
				}
			}
		}
		FadeToBlackTween.Play();
		yield return new WaitForSeconds(0.3f);
		ChestNode.gameObject.SetActive(value: true);
		chest.Animator.Play("Intro");
		chest.EnableChestVFXObj(isEnable: true);
		bool flag = true;
		while (flag)
		{
			if (UICamera.IsInputLocked())
			{
				UICamera.UnlockInput();
			}
			flag = UICamera.IsInputLocked();
		}
		Singleton<SLOTAudioManager>.Instance.PlaySound("gacha/SFX_Gacha_ChestShow");
		Singleton<SLOTAudioManager>.Instance.PlaySound("gacha/SFX_LootChestBounce");
		Singleton<SLOTAudioManager>.Instance.PlaySound("gacha/SFX_LootChestStarLoop");
		mClicked = false;
		while (!mClicked)
		{
			yield return null;
		}
		onOpened?.Invoke();
		chest.Animator.Play("Tap");
		Singleton<SLOTAudioManager>.Instance.StopSound("gacha/SFX_LootChestBounce");
		Singleton<SLOTAudioManager>.Instance.StopSound("gacha/SFX_LootChestStarLoop");
		Singleton<SLOTAudioManager>.Instance.PlaySound("gacha/SFX_Gacha_ChestOpen");
		if (tipsPanel != null)
		{
			tipsPanel.SetActive(value: false);
		}
		chest.EnableChestVFXObj(isEnable: false);
		spawnedCard.gameObject.SetActive(value: true);
		yield return new WaitForSeconds(0.4f);
		if (!isCreature)
		{
			chest.EnableCardVFXObj(isEnable: true);
		}
		yield return new WaitForSeconds(0.5f);
		Singleton<SLOTAudioManager>.Instance.PlaySound("gacha/SFX_LootChestShimmer");
		if (Singleton<TutorialController>.Instance.IsBlockActive("Q2"))
		{
			Singleton<TutorialController>.Instance.AdvanceTutorialState();
		}
		if (isCreature)
		{
			CreatureChestOpenTween.Play();
			chest.CardNode.InstantiateAsChild(VFX_CardSwap_Prefab).transform.SetParent(StatsPanel.transform.parent.transform);
			if (DetachedSingleton<SceneFlowManager>.Instance.InBattleScene())
			{
				Singleton<DWGameCamera>.Instance.Battle3DUICam.gameObject.SetActive(value: false);
			}
			StartCoroutine(LoadModelCo());
			while (mLoadedModelInstance == null)
			{
				yield return null;
			}
			CardPrefabScript componentInChildren = chest.CardNode.GetComponentInChildren<CardPrefabScript>();
			if (componentInChildren != null)
			{
				UIWidget component2 = componentInChildren.GetComponent<UIWidget>();
				if (component2 != null)
				{
					component2.alpha = 0f;
				}
			}
			for (int j = 0; j < RarityToppers.Length; j++)
			{
				if (j == mCreature.Creature.Form.Rarity - 1)
				{
					RarityToppers[j].gameObject.SetActive(value: true);
					RarityGlows[j].gameObject.SetActive(value: true);
					RarityPlatforms[j].gameObject.SetActive(value: true);
				}
				else
				{
					RarityToppers[j].gameObject.SetActive(value: false);
					RarityGlows[j].gameObject.SetActive(value: false);
					RarityPlatforms[j].gameObject.SetActive(value: false);
				}
			}
		}
		mClicked = false;
		while (!mClicked)
		{
			yield return null;
		}
		chest.EnableCardVFXObj(isEnable: false);
		chest.EnableChestVFXObj(isEnable: false);
		chest.Animator.Play("Outro");
		Singleton<SLOTAudioManager>.Instance.PlaySound("gacha/SFX_Gacha_ChestClose");
		Singleton<SLOTAudioManager>.Instance.StopSound("gacha/SFX_LootChestShimmer");
		ChestNode.gameObject.SetActive(value: false);
		if (isCreature)
		{
			CreatureInfoHideTween.Play();
		}
		yield return StartCoroutine(FadeBackInTween.PlayAsCoroutine());
		if (DetachedSingleton<SceneFlowManager>.Instance.InBattleScene())
		{
			Singleton<BattleResultsController>.Instance.ShowRewardsAfterGacha.Play();
		}
		ChestNode.DestroyAllChildren();
		foreach (Transform cardNode in CardNodes)
		{
			cardNode.DestroyAllChildren();
		}
		CreatureCamera.enabled = false;
		Showing = false;
	}

	public void OnClickNextObject()
	{
		mClicked = true;
	}

	public void OutroComplete()
	{
		if (DetachedSingleton<SceneFlowManager>.Instance.InBattleScene())
		{
			Singleton<DWGameCamera>.Instance.Battle3DUICam.gameObject.SetActive(value: true);
		}
	}

	private IEnumerator LoadModelCo()
	{
		UnloadModel();
		yield return StartCoroutine(Singleton<SLOTResourceManager>.Instance.LoadCreatureResources(mCreature.Creature.Form, delegate
		{
			GameObject original = SLOTResourceManager.Load("Creatures/" + mCreature.Creature.Form.Prefab + "/" + mCreature.Creature.Form.Prefab, typeof(GameObject)) as GameObject;
			Texture2D loadedTexture = SLOTResourceManager.Load("Creatures/" + mCreature.Creature.Form.Prefab + "/Textures/" + mCreature.Creature.Form.Faction.ToString() + "/" + mCreature.Creature.Form.PrefabTexture, typeof(Texture2D)) as Texture2D;
			CreatureNode.transform.localRotation = Quaternion.Euler(Vector3.zero);
			mLoadedModelInstance = CreatureNode.InstantiateAsChild(original);
			mCreature.Creature.Form.SwapCreatureTexture(mLoadedModelInstance, loadedTexture, addSpin: true);
			SetToIdle(mLoadedModelInstance);
			float num = Mathf.Max(6f, mCreature.Creature.Form.Height);
			mLoadedModelInstance.transform.localScale *= 8f / num;
		}));
	}

	private void SetToIdle(GameObject creature)
	{
		BattleCreatureAnimState componentInChildren = creature.GetComponentInChildren<BattleCreatureAnimState>();
		if (componentInChildren != null && componentInChildren.anim != null)
		{
			componentInChildren.anim.Play("Idle");
		}
	}

	private void UnloadModel()
	{
		if (mLoadedModelInstance != null)
		{
			UnityEngine.Object.Destroy(mLoadedModelInstance);
			mLoadedModelInstance = null;
			Resources.UnloadUnusedAssets();
		}
	}
}
