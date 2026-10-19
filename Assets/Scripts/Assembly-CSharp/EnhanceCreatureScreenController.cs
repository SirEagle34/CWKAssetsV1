using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnhanceCreatureScreenController : Singleton<EnhanceCreatureScreenController>
{
	private const int SlotCount = 5;

	public GameObject EnhanceFX;

	public GameObject StarUpFX;

	public UITweenController ShowTween;

	public UITweenController HideMainTween;

	public UITweenController ShowSlotsTween;

	public UITweenController HideSlotsTween;

	public UITweenController ShowAfterEnhanceTween;

	public UITweenController HideForEnhanceTween;

	public UITweenController CantAffordTween;

	public UITweenController ShowPowerUpGroup;

	public UITweenController HidePowerUpGroup;

	public UITweenController ShowGuages;

	public UITweenController HideGuages;

	public UITweenController MovePlatformToCenterTween;

	public UITweenController DropPlatformTween;

	public UITweenController PopCreatureTween;

	public UITweenController MovePlatformBackTween;

	public UITweenController ShakeStartLoopTween;

	public UITweenController ShakeStopTween;

	public UIStreamingGrid CreatureGrid;

	private UIStreamingGridDataSource<InventorySlotItem> mCreatureGridDataSource = new UIStreamingGridDataSource<InventorySlotItem>();

	public BoxCollider PowerUpSlot;

	public GameObject PowerUpSlotSprite;

	public GameObject CreatureInfoGroup;

	public CreatureStatsPanel StatsPanel;

	public UILabel SoftCurrencyLabel;

	public UILabel HardCurrencyLabel;

	public UILabel CostLabel;

	public GameObject PowerUpGroup;

	public GameObject GuagesGroup;

	public Transform SortButton;

	public Transform[] RecipeNodes = new Transform[5];

	public GameObject[] SlotHighlights = new GameObject[5];

	public GameObject[] PlatformLights = new GameObject[5];

	public Material FlashMaterial;

	public GameObject upgradeGlow;

	public LaboratorySequence RevealSequenceScript;

	public Camera CreatureCamera_Masked;

	public Camera CreatureCamera_Full;

	public InventoryBarController inventoryBar;

	public UILabel EvolutionShardsLabel;

	public GameObject EvolutionShardsPanel;

	public UILabel EvolutionShardsCostLabel;

	public UILabel EvolutionShardsOwnedLabel;

	public UIButton EvolutionShardsButton;

	private InventoryTile mEnhanceCreature;

	private InventoryTile[] mRecipeEvoMaterials = new InventoryTile[5];

	private int mBuyingGoldPacks;

	public Transform CreatureSpawnPoint;

	private GameObject mCreatureModelInstance;

	public void Populate()
	{
		if (Singleton<TutorialController>.Instance.IsBlockActive("Enhance"))
		{
			Singleton<TutorialController>.Instance.AddMaterialsForEnhance();
		}
		ShowTween.Play();
		CreatureInfoGroup.SetActive(value: false);
		PowerUpGroup.SetActive(value: false);
		GuagesGroup.SetActive(value: false);
		TurnOffHighlights();
		InventoryBarController.onDoFilterNull();
		InventoryBarController.onDoFilter += PopulateContents;
		inventoryBar.SetFilters(creatureFilterActive: false, cakeFilterActive: false, cardFilterActive: true, ingredientFilterActive: true, shardFilterActive: false);
		base.gameObject.GetComponent<InventoryBarController>().UpdateInventoryCounter();
		CreatureGrid.transform.parent.GetComponent<UIScrollView>().ResetPosition();
		CreatureGrid.transform.parent.GetComponent<UIScrollView>().UpdateScrollbars();
		UpdateEvolutionShardsUI();
	}

	private void PopulateContents()
	{
		PopulateCreatureList();
		RefreshCurrency();
		InventoryTile.ResetForNewScreen();
	}

	public void Unload()
	{
		mCreatureGridDataSource.Clear();
		UnloadSelectedCreature();
		if (EvolutionShardsPanel != null)
		{
			EvolutionShardsPanel.SetActive(value: false);
		}
	}

	private void UnloadSelectedCreature()
	{
		if (mEnhanceCreature != null)
		{
			NGUITools.Destroy(mEnhanceCreature.gameObject);
			mEnhanceCreature = null;
		}
		ClearRecipe();
		PowerUpSlotSprite.SetActive(value: true);
		UnloadModel();
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
		if (tile.InventoryItem.Creature.IsMaxStarRating())
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
		if (IsCreatureSelected(tile.InventoryItem))
		{
			return false;
		}
		return true;
	}

	private void OnTileDragBeginFromSlot(InventoryTile tile)
	{
		RemoveTile(tile, tile.AssignedSlot);
	}

	private bool OnTileDropped(InventoryTile tile, int slotIndex)
	{
		if (slotIndex != 0)
		{
			return false;
		}
		if (!tile.InventoryItem.Creature.IsMaxStarRating())
		{
			tile.EquipTween.Play();
			AddCreature(tile, slotIndex);
			return true;
		}
		return false;
	}

	private void RefreshTileOverlay(InventoryTile tile)
	{
		if (tile.InventoryItem.SlotType == InventorySlotType.Creature)
		{
			if (IsCreatureSelected(tile.InventoryItem))
			{
				tile.SetOverlayStatus(InventoryTile.TileStatus.SelectedAsTarget, fadeIfInUse: false);
			}
			else
			{
				tile.SetOverlayStatus(InventoryTile.TileStatus.NotSelected, fadeIfInUse: false);
			}
			if (tile.InventoryItem.Creature.IsMaxStarRating())
			{
				tile.FadeGroup.SetActive(value: true);
			}
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
		}
	}

	public void AddCreature(InventoryTile creature, int slot)
	{
		if (slot == 0)
		{
			if (mEnhanceCreature != null)
			{
				if (mEnhanceCreature.InventoryItem.Creature.IsMaxStarRating())
				{
					UnloadSelectedCreature();
				}
				else
				{
					ClearRecipe();
				}
			}
			creature.transform.parent = PowerUpSlot.transform;
			creature.transform.localPosition = Vector3.zero;
			mEnhanceCreature = creature;
			CreatureInfoGroup.SetActive(value: true);
			StatsPanel.Unload();
			StatsPanel.Populate(mEnhanceCreature.InventoryItem);
			PowerUpGroup.SetActive(value: true);
			GuagesGroup.SetActive(value: true);
			ShowPowerUpGroup.Play();
			StartCoroutine(LoadModelForPrepPowerUpCo());
			PowerUpSlotSprite.SetActive(value: false);
			creature.MatchToCollider(PowerUpSlot);
			PopulateRecipe();
			if (mEnhanceCreature.InventoryItem.Creature.Level == mEnhanceCreature.InventoryItem.Creature.MaxLevel && mEnhanceCreature.InventoryItem.Creature.StarRatingData.CostToEnhance < Singleton<PlayerInfoScript>.Instance.SaveData.SoftCurrency)
			{
				StatsPanel.PreviewStarUpgrade();
			}
			else
			{
				StatsPanel.StopPreviewStarUpgrade();
			}
			upgradeGlow.SetActive(mEnhanceCreature.InventoryItem.Creature.Level == mEnhanceCreature.InventoryItem.Creature.MaxLevel);
			Singleton<TutorialController>.Instance.AdvanceIfOnState("EN_DragEnhanceTarget");
			UpdateEvolutionShardsUI();
		}
		creature.AssignedSlot = slot;
	}

	private void PopulateRecipe()
	{
		CreatureItem creature = mEnhanceCreature.InventoryItem.Creature;
		int costToEnhance = creature.StarRatingData.CostToEnhance;
		CostLabel.text = costToEnhance.ToString();
		if (costToEnhance > Singleton<PlayerInfoScript>.Instance.SaveData.SoftCurrency)
		{
			CantAffordTween.Play();
		}
		else
		{
			CantAffordTween.StopAndReset();
		}
		TurnOffHighlights();
		int num = 0;
		List<EvoMaterialData> recipe = creature.Form.GetEnhanceRecipe(mEnhanceCreature.InventoryItem.Creature.StarRating);
		int i;
		for (i = 0; i < mRecipeEvoMaterials.Length; i++)
		{
			if (i < recipe.Count && recipe[i] != null)
			{
				InventorySlotItem inventorySlotItem = Singleton<PlayerInfoScript>.Instance.SaveData.FindEvoMaterial((InventorySlotItem m) => m.EvoMaterial == recipe[i] && !IsEvoMatInSelectedRecipe(m));
				InventoryTile.EvoMatchType evoMatch;
				if (inventorySlotItem == null)
				{
					inventorySlotItem = new InventorySlotItem(EvoMaterialDataManager.Instance.GetData(recipe[i].ID));
					evoMatch = InventoryTile.EvoMatchType.NoMatch;
				}
				else
				{
					evoMatch = InventoryTile.EvoMatchType.Good;
					SlotHighlights[i].SetActive(value: true);
					num++;
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
		for (int num2 = 0; num2 < PlatformLights.Length; num2++)
		{
			if (num > num2)
			{
				PlatformLights[num2].SetActive(value: true);
			}
		}
	}

	private void TurnOnHighlights()
	{
		GameObject[] slotHighlights = SlotHighlights;
		for (int i = 0; i < slotHighlights.Length; i++)
		{
			slotHighlights[i].SetActive(value: true);
		}
		GameObject[] platformLights = PlatformLights;
		for (int j = 0; j < platformLights.Length; j++)
		{
			platformLights[j].SetActive(value: true);
		}
	}

	private void TurnOffHighlights()
	{
		GameObject[] slotHighlights = SlotHighlights;
		for (int i = 0; i < slotHighlights.Length; i++)
		{
			slotHighlights[i].SetActive(value: false);
		}
		GameObject[] platformLights = PlatformLights;
		for (int j = 0; j < platformLights.Length; j++)
		{
			platformLights[j].SetActive(value: false);
		}
	}

	public void RemoveTile(InventoryTile tile, int slot)
	{
		if (slot == 0)
		{
			PowerUpSlotSprite.SetActive(value: true);
			if (mEnhanceCreature != null)
			{
				mEnhanceCreature.MatchToCollider(null);
			}
			mEnhanceCreature = null;
			HidePowerUpGroup.Play();
			CreatureInfoGroup.SetActive(value: false);
			UnloadModel();
			ClearRecipe();
		}
	}

	public void OnClickEnhance()
	{
		Debug.Log($"[Enhance] OnClickEnhance called. mEnhanceCreature is null: {mEnhanceCreature == null}");
		if (mEnhanceCreature == null)
		{
			string text = KFFLocalization.Get("!!ERROR_POWERUP_NO_CREATURE_SELECTED");
			Debug.Log("[Enhance] Error message: " + text);
			Singleton<SimplePopupController>.Instance.ShowMessage(string.Empty, text, playErrorSound: true);
			return;
		}
		CreatureItem creature = mEnhanceCreature.InventoryItem.Creature;
		if (creature.Level < creature.MaxLevel)
		{
			Singleton<SimplePopupController>.Instance.ShowMessage(string.Empty, KFFLocalization.Get("!!ERROR_EVO_NOT_MAXLEVEL"));
			return;
		}
		InventoryTile[] array = mRecipeEvoMaterials;
		foreach (InventoryTile inventoryTile in array)
		{
			if (!(inventoryTile == null) && inventoryTile.EvoMatch != InventoryTile.EvoMatchType.Good)
			{
				string body = KFFLocalization.Get("!!ERROR_EVO_DONT_HAVE_ALL_CREATURE");
				Singleton<SimplePopupController>.Instance.ShowMessage(string.Empty, body);
				return;
			}
		}
		int costToEnhance = creature.StarRatingData.CostToEnhance;
		if (Singleton<PlayerInfoScript>.Instance.SaveData.SoftCurrency < costToEnhance)
		{
			Singleton<PlayerInfoScript>.Instance.CalculateGoldPacksNeeded(costToEnhance, out mBuyingGoldPacks, out var totalGold, out var totalGemCost);
			string confirmString = string.Format(KFFLocalization.Get("!!ERROR_ENHANCE_NOT_ENOUGH_CURRENCY_BUY"), costToEnhance, totalGold);
			string insufficientString = string.Format(KFFLocalization.Get("!!ERROR_ENHANCE_NOT_ENOUGH_CURRENCY_NOBUY"), costToEnhance);
			Singleton<SimplePopupController>.Instance.ShowPurchasePrompt(confirmString, insufficientString, totalGemCost, OnClickConfirmBuyGold);
		}
		else
		{
			StartCoroutine(EnhanceSequence());
			inventoryBar.UpdateInventoryCounter();
		}
	}

	private void OnClickConfirmBuyGold()
	{
		Singleton<PlayerInfoScript>.Instance.BuyGold(mBuyingGoldPacks);
	}

	private IEnumerator EnhanceSequence()
	{
		PlayerSaveData saveData = Singleton<PlayerInfoScript>.Instance.SaveData;
		int costToEnhance = mEnhanceCreature.InventoryItem.Creature.StarRatingData.CostToEnhance;
		saveData.SoftCurrency -= costToEnhance;
		List<UITexture> list = new List<UITexture>();
		List<EvoMaterialData> list2 = new List<EvoMaterialData>();
		InventoryTile[] array = mRecipeEvoMaterials;
		foreach (InventoryTile inventoryTile in array)
		{
			if (inventoryTile != null)
			{
				list2.Add(inventoryTile.InventoryItem.EvoMaterial);
				list.Add(inventoryTile.PortraitTexture);
			}
		}
		saveData.RemoveEvoMaterials(list2);
		mEnhanceCreature.InventoryItem.Creature.EnhanceStarRating();
		Singleton<PlayerInfoScript>.Instance.Save();
		CreatureSpawnPoint.transform.localRotation = Quaternion.Euler(new Vector3(0f, 180f, 0f));
		HideForEnhanceTween.Play();
		HidePowerUpGroup.Play();
		HideGuages.Play();
		HideSlotsTween.Play();
		MovePlatformToCenterTween.Play();
		CreatureCamera_Masked.gameObject.SetActive(value: false);
		CreatureCamera_Full.gameObject.SetActive(value: true);
		LaboratorySequence.onElectricStart += OnElectricStart;
		LaboratorySequence.onSequencePop += OnSequencePop;
		LaboratorySequence.onSequenceComplete += OnClickCloseResult;
		TurnOffHighlights();
		if (RevealSequenceScript == null)
		{
			RevealSequenceScript = base.gameObject.GetComponentInChildren<LaboratorySequence>();
		}
		if (RevealSequenceScript != null)
		{
			RevealSequenceScript.numStars = mEnhanceCreature.InventoryItem.Creature.StarRating;
			RevealSequenceScript.StartModuleSequence(list);
		}
		else
		{
			OnClickCloseResult();
		}
		yield return new WaitForSeconds(0.5f);
		ClearRecipe();
	}

	private bool IsCreatureFullyMaxed(CreatureItem creature)
	{
		if (creature == null)
		{
			return false;
		}
		if (creature.StarRating >= 5)
		{
			return creature.Level >= 50;
		}
		return false;
	}

	private void UpdateEvolutionShardsUI()
	{
		if (EvolutionShardsLabel != null)
		{
			EvolutionShardsLabel.text = Singleton<PlayerInfoScript>.Instance.SaveData.EvolutionShards.ToString();
		}
		if (mEnhanceCreature == null)
		{
			if (EvolutionShardsPanel != null)
			{
				EvolutionShardsPanel.SetActive(value: false);
			}
			return;
		}
		CreatureItem creature = mEnhanceCreature.InventoryItem.Creature;
		if (IsCreatureFullyMaxed(creature))
		{
			if (EvolutionShardsPanel != null)
			{
				EvolutionShardsPanel.SetActive(value: false);
			}
			return;
		}
		if (EvolutionShardsPanel != null)
		{
			EvolutionShardsPanel.SetActive(value: true);
		}
		int num = 1;
		int evolutionShards = Singleton<PlayerInfoScript>.Instance.SaveData.EvolutionShards;
		if (EvolutionShardsCostLabel != null)
		{
			EvolutionShardsCostLabel.text = num.ToString();
		}
		if (EvolutionShardsOwnedLabel != null)
		{
			EvolutionShardsOwnedLabel.text = evolutionShards.ToString();
		}
		bool isEnabled = evolutionShards >= num;
		if (EvolutionShardsButton != null)
		{
			EvolutionShardsButton.isEnabled = isEnabled;
		}
	}

	public void OnClickMaxWithShards()
	{
		Debug.Log("[EnhanceCreature] OnClickMaxWithShards called");
		Debug.Log($"[EnhanceCreature] mEnhanceCreature is null: {mEnhanceCreature == null}");
		if (mEnhanceCreature == null)
		{
			Singleton<SimplePopupController>.Instance.ShowMessage(string.Empty, "Please select a creature to enhance!");
			return;
		}
		Debug.Log("[EnhanceCreature] Creature: " + mEnhanceCreature.InventoryItem.Creature.Form.Name);
		CreatureItem creature = mEnhanceCreature.InventoryItem.Creature;
		if (IsCreatureFullyMaxed(creature))
		{
			Singleton<SimplePopupController>.Instance.ShowMessage(string.Empty, KFFLocalization.Get("!!ERROR_ALREADY_MAXED"));
			return;
		}
		int num = 1;
		if (Singleton<PlayerInfoScript>.Instance.SaveData.EvolutionShards < num)
		{
			Singleton<SimplePopupController>.Instance.ShowMessage(string.Empty, KFFLocalization.Get("!!NOT_ENOUGH_EVOLUTION_SHARDS"));
			return;
		}
		string body = KFFLocalization.Get("!!CONFIRM_MAX_WITH_SHARDS").Replace("<creature>", creature.Form.Name).Replace("<cost>", num.ToString());
		Singleton<SimplePopupController>.Instance.ShowPrompt(string.Empty, body, ConfirmMaxWithShards, null, KFFLocalization.Get("!!MAX_CREATURE"), KFFLocalization.Get("!!CANCEL"));
	}

	private void ConfirmMaxWithShards()
	{
		PlayerSaveData saveData = Singleton<PlayerInfoScript>.Instance.SaveData;
		CreatureItem creature = mEnhanceCreature.InventoryItem.Creature;
		int num = 1;
		saveData.EvolutionShards -= num;
		while (creature.StarRating < 5)
		{
			creature.EnhanceStarRating();
		}
		creature.Xp = creature.XPTable.GetXpToReachLevel(50);
		Singleton<PlayerInfoScript>.Instance.Save();
		StartCoroutine(MaxWithShardsSequence());
	}

	private IEnumerator MaxWithShardsSequence()
	{
		List<UITexture> fodderTiles = new List<UITexture>();
		inventoryBar.UpdateInventoryCounter();
		CreatureSpawnPoint.transform.localRotation = Quaternion.Euler(new Vector3(0f, 180f, 0f));
		HideForEnhanceTween.Play();
		HidePowerUpGroup.Play();
		HideGuages.Play();
		HideSlotsTween.Play();
		MovePlatformToCenterTween.Play();
		CreatureCamera_Masked.gameObject.SetActive(value: false);
		CreatureCamera_Full.gameObject.SetActive(value: true);
		LaboratorySequence.onElectricStart += OnElectricStart;
		LaboratorySequence.onSequencePop += OnSequencePop;
		LaboratorySequence.onSequenceComplete += OnClickCloseResult;
		TurnOffHighlights();
		if (RevealSequenceScript == null)
		{
			RevealSequenceScript = base.gameObject.GetComponentInChildren<LaboratorySequence>();
		}
		if (RevealSequenceScript != null)
		{
			RevealSequenceScript.numStars = 5;
			RevealSequenceScript.StartModuleSequence(fodderTiles);
		}
		else
		{
			OnClickCloseResult();
		}
		yield return new WaitForSeconds(0.5f);
		yield return StartCoroutine(LoadCreatureModel());
		UpdateEvolutionShardsUI();
	}

	private IEnumerator LoadCreatureModel()
	{
		yield return StartCoroutine(Singleton<SLOTResourceManager>.Instance.LoadCreatureResources(mEnhanceCreature.InventoryItem.Creature.Form, delegate(GameObject objData, Texture2D texture)
		{
			if (mCreatureModelInstance != null)
			{
				Object.Destroy(mCreatureModelInstance);
			}
			mCreatureModelInstance = CreatureSpawnPoint.InstantiateAsChild(objData);
			mEnhanceCreature.InventoryItem.Creature.Form.SwapCreatureTexture(mCreatureModelInstance, texture);
			mCreatureModelInstance.transform.localScale = PowerUpCreatureScale();
		}));
	}

	public void OnElectricStart()
	{
		TurnOffHighlights();
		ShakeStartLoopTween.Play();
	}

	public void OnSequencePop()
	{
		PopCreatureTween.Play();
		TurnOffHighlights();
		ShakeStartLoopTween.End();
		ShakeStopTween.Play();
		StartCoroutine(FlashCreatureCo());
	}

	public IEnumerator FlashCreatureCo()
	{
		yield return new WaitForSeconds(0.01f);
		FlashCreature();
		yield return new WaitForSeconds(0.08f);
		UnFlashCreature();
		yield return new WaitForSeconds(0.07f);
		FlashCreature();
		yield return new WaitForSeconds(0.08f);
		UnFlashCreature();
		yield return new WaitForSeconds(0.1f);
		FlashCreature();
		yield return new WaitForSeconds(0.13f);
		UnFlashCreature();
		yield return new WaitForSeconds(0.3f);
		FlashCreature();
		yield return new WaitForSeconds(0.23f);
		UnFlashCreature();
	}

	public void OnClickCloseResult()
	{
		StartCoroutine(ShowMainAfterEnhance());
	}

	private IEnumerator ShowMainAfterEnhance()
	{
		ShowAfterEnhanceTween.Play();
		ShowSlotsTween.Play();
		MovePlatformBackTween.PlayWithCallback(SwapToMaskedCamera);
		TurnOffHighlights();
		ShowPowerUpGroup.Play();
		ShowGuages.Play();
		PopulateContents();
		yield return new WaitForSeconds(1f);
		if (mEnhanceCreature.InventoryItem.Creature.IsMaxStarRating())
		{
			yield return new WaitForSeconds(1f);
			UnloadSelectedCreature();
			RemoveTile(null, 0);
		}
		else
		{
			PopulateRecipe();
		}
		PopulateCreatureList();
		if (mEnhanceCreature != null)
		{
			upgradeGlow.SetActive(mEnhanceCreature.InventoryItem.Creature.Level == mEnhanceCreature.InventoryItem.Creature.MaxLevel);
		}
		Singleton<PlayerInfoScript>.Instance.UpdateBadgeCount(BadgeEnum.Evo);
		StatsPanel.Unload();
		if (mEnhanceCreature != null)
		{
			StatsPanel.Populate(mEnhanceCreature.InventoryItem);
			StatsPanel.StopPreviewStarUpgrade();
		}
		UpdateEvolutionShardsUI();
		UICamera.UnlockInput();
	}

	public void SwapToMaskedCamera()
	{
		CreatureCamera_Masked.gameObject.SetActive(value: true);
		CreatureCamera_Full.gameObject.SetActive(value: false);
	}

	private bool IsCreatureSelected(InventorySlotItem slot)
	{
		if (slot.SlotType == InventorySlotType.Creature && mEnhanceCreature != null && mEnhanceCreature.InventoryItem.Creature == slot.Creature)
		{
			return true;
		}
		return false;
	}

	private bool IsEvoMatInSelectedRecipe(InventorySlotItem slot)
	{
		InventoryTile[] array = mRecipeEvoMaterials;
		foreach (InventoryTile inventoryTile in array)
		{
			if (inventoryTile != null && inventoryTile.EvoMatch == InventoryTile.EvoMatchType.Good && inventoryTile.InventoryItem == slot)
			{
				return true;
			}
		}
		return false;
	}

	private void Update()
	{
		RefreshCurrency();
	}

	public IEnumerator LoadModelForPrepPowerUpCo()
	{
		UnloadModel();
		yield return StartCoroutine(Singleton<SLOTResourceManager>.Instance.LoadCreatureResources(mEnhanceCreature.InventoryItem.Creature.Form, delegate(GameObject objData, Texture2D texture)
		{
			CreatureSpawnPoint.transform.localRotation = Quaternion.Euler(new Vector3(0f, 180f, 0f));
			mCreatureModelInstance = CreatureSpawnPoint.InstantiateAsChild(objData);
			mEnhanceCreature.InventoryItem.Creature.Form.SwapCreatureTexture(mCreatureModelInstance, texture);
			mCreatureModelInstance.transform.localScale = PowerUpCreatureScale();
		}));
	}

	private Vector3 PowerUpCreatureScale()
	{
		float num = Mathf.Max(4f, mEnhanceCreature.InventoryItem.Creature.Form.Height);
		float num2 = Mathf.Min(1f, 5f / num);
		return Vector3.one * num2;
	}

	public void UnloadModel()
	{
		if (mCreatureModelInstance != null)
		{
			Object.Destroy(mCreatureModelInstance);
			mCreatureModelInstance = null;
		}
		Resources.UnloadUnusedAssets();
	}

	public void OnClickSort()
	{
		Singleton<SortPopupController>.Instance.Show(SortPopupController.Category.Creatures, SortButton, PopulateCreatureList);
	}

	public void OnClickBackToTown()
	{
		if (Singleton<TutorialController>.Instance.IsAnyTutorialActive())
		{
			Singleton<TutorialController>.Instance.AdvanceTutorialState();
		}
		Singleton<MouseOrbitCamera>.Instance.CheckTiltCamSettingBeforeTutorial();
		HideMainTween.Play();
		Singleton<LabBuildingController>.Instance.Hide();
	}

	public GameObject GetTutorialEnhanceCreature()
	{
		InventorySlotItem inventorySlotItem = Singleton<PlayerInfoScript>.Instance.SaveData.GetBestCreature(notMaxStars: true);
		if (inventorySlotItem == null)
		{
			inventorySlotItem = Singleton<PlayerInfoScript>.Instance.SaveData.GetNewestCreature();
		}
		return mCreatureGridDataSource.FindPrefab(inventorySlotItem);
	}

	public void FlashCreature()
	{
		foreach (SkinnedMeshRenderer orignalMesh in CreatureSpawnPoint.GetChild(0).gameObject.GetComponentInChildren<BattleCreatureAnimState>().orignalMeshes)
		{
			orignalMesh.material = FlashMaterial;
		}
	}

	public void UnFlashCreature()
	{
		BattleCreatureAnimState componentInChildren = CreatureSpawnPoint.GetChild(0).gameObject.GetComponentInChildren<BattleCreatureAnimState>();
		for (int i = 0; i < componentInChildren.orignalMeshes.Count; i++)
		{
			componentInChildren.orignalMeshes[i].material = componentInChildren.originalMats[i];
		}
	}
}
