using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardCraftingController : Singleton<CardCraftingController>
{
	public GameObject CardCreation_Panel;

	public UIGrid CraftingGrid;

	public UIGrid IngredientsGrid;

	public UILabel EvolutionShardsCostLabel;

	public UIButton CraftButton;

	private bool mCraftingListInitialized;

	public GameObject CraftingListItemPrefab;

	public GameObject IngredientItemPrefab;

	private CardCraftingData mSelectedRecipe;

	private CardCraftingIngredientItem[] mIngredientItems;

	private bool mCrafting;

	public void Populate()
	{
		Debug.Log("[CardCrafting] Populate");
		InventoryTile.ResetForNewScreen();
		mSelectedRecipe = null;
		ClearIngredients();
		UpdateEvolutionShardsUI();
		PopulateCraftingList();
	}

	private void PopulateCraftingList()
	{
		if (CraftingGrid == null)
		{
			Debug.LogError("[CardCrafting] CraftingGrid is NULL.");
			return;
		}
		if (CraftingListItemPrefab == null)
		{
			Debug.LogError("[CardCrafting] CraftingListItemPrefab is NULL.");
			return;
		}
		List<CardCraftingData> database = CardCraftingDataManager.Instance.GetDatabase();
		if (database == null || database.Count == 0)
		{
			Debug.LogWarning("[CardCrafting] No crafting recipes found.");
			return;
		}
		if (mCraftingListInitialized)
		{
			Debug.Log("[CardCrafting] Crafting list already initialized.");
			if (mSelectedRecipe == null && database.Count > 0)
			{
				SelectRecipe(database[0]);
			}
			ResetScroll(CraftingGrid);
			return;
		}
		Debug.Log("[CardCrafting] Creating crafting list: " + database.Count);
		for (int i = 0; i < database.Count; i++)
		{
			CardCraftingData cardCraftingData = database[i];
			if (cardCraftingData == null)
			{
				continue;
			}
			GameObject gameObject = CraftingGrid.transform.InstantiateAsChild(CraftingListItemPrefab);
			if (gameObject == null)
			{
				Debug.LogError("[CardCrafting] Failed to create recipe item.");
				continue;
			}
			CardCraftingListItem component = gameObject.GetComponent<CardCraftingListItem>();
			if (component == null)
			{
				Debug.LogError("[CardCrafting] CraftingListItemPrefab does not contain CardCraftingListItem.");
			}
			else
			{
				component.Populate(cardCraftingData);
			}
		}
		CraftingGrid.Reposition();
		ResetScroll(CraftingGrid);
		mCraftingListInitialized = true;
		SelectRecipe(database[0]);
	}

	public void SelectRecipe(CardCraftingData recipe)
	{
		if (recipe == null)
		{
			return;
		}
		mSelectedRecipe = recipe;
		for (int i = 0; i < CraftingGrid.transform.childCount; i++)
		{
			CardCraftingListItem component = CraftingGrid.transform.GetChild(i).GetComponent<CardCraftingListItem>();
			if (!(component == null))
			{
				component.SetSelected(component.GetData() == recipe);
			}
		}
		Debug.Log("[CardCrafting] Selected recipe: " + recipe.ID);
		RefreshRecipe();
	}

	private void RefreshRecipe()
	{
		if (mSelectedRecipe == null)
		{
			ClearIngredients();
			UpdateEvolutionShardsUI();
			UpdateCraftButton();
		}
		else
		{
			RefreshIngredients();
			UpdateEvolutionShardsUI();
			UpdateCraftButton();
		}
	}

	private void RefreshIngredients()
	{
		ClearIngredients();
		if (mSelectedRecipe == null)
		{
			return;
		}
		if (IngredientsGrid == null)
		{
			Debug.LogError("[CardCrafting] IngredientsGrid is NULL.");
			return;
		}
		if (IngredientItemPrefab == null)
		{
			Debug.LogError("[CardCrafting] IngredientItemPrefab is NULL.");
			return;
		}
		List<CardCraftingIngredient> ingredients = mSelectedRecipe.Ingredients;
		if (ingredients == null || ingredients.Count == 0)
		{
			return;
		}
		mIngredientItems = new CardCraftingIngredientItem[ingredients.Count];
		for (int i = 0; i < ingredients.Count; i++)
		{
			CardCraftingIngredient cardCraftingIngredient = ingredients[i];
			if (cardCraftingIngredient == null)
			{
				continue;
			}
			GameObject gameObject = IngredientsGrid.transform.InstantiateAsChild(IngredientItemPrefab);
			if (gameObject == null)
			{
				Debug.LogError("[CardCrafting] Failed to create ingredient item.");
				continue;
			}
			gameObject.ChangeLayer(IngredientsGrid.gameObject.layer);
			CardCraftingIngredientItem component = gameObject.GetComponent<CardCraftingIngredientItem>();
			if (component == null)
			{
				Debug.LogError("[CardCrafting] IngredientItemPrefab does not contain CardCraftingIngredientItem.");
				NGUITools.Destroy(gameObject);
			}
			else
			{
				component.Populate(cardCraftingIngredient);
				mIngredientItems[i] = component;
			}
		}
		IngredientsGrid.Reposition();
		ResetScroll(IngredientsGrid);
	}

	private void ResetScroll(UIGrid grid)
	{
		if (grid != null && base.gameObject.activeInHierarchy)
		{
			StartCoroutine(ResetScrollNextFrame(grid));
		}
	}

	private IEnumerator ResetScrollNextFrame(UIGrid grid)
	{
		yield return null;
		if (!(grid == null))
		{
			grid.Reposition();
			UIScrollView uIScrollView = NGUITools.FindInParents<UIScrollView>(grid.gameObject);
			if (uIScrollView != null)
			{
				uIScrollView.ResetPosition();
			}
		}
	}

	private void ClearIngredients()
	{
		if (IngredientsGrid == null)
		{
			mIngredientItems = null;
			return;
		}
		IngredientsGrid.transform.DestroyAllChildren();
		mIngredientItems = null;
		IngredientsGrid.Reposition();
	}

	private void UpdateEvolutionShardsUI()
	{
		int num = ((mSelectedRecipe != null) ? mSelectedRecipe.CraftCurrencyCost : 0);
		if (EvolutionShardsCostLabel != null)
		{
			EvolutionShardsCostLabel.text = num.ToString();
			CurrencyUI.UseCraftIcon(EvolutionShardsCostLabel);
		}
		UpdateHeaderBalances();
	}

	private void UpdateHeaderBalances()
	{
		if (CardCreation_Panel == null)
		{
			return;
		}
		PlayerSaveData saveData = Singleton<PlayerInfoScript>.Instance.SaveData;
		if (saveData == null)
		{
			return;
		}
		Transform transform = CardCreation_Panel.transform.Find("TopBar/Currency/Offset/SoftCurrency");
		if (transform != null)
		{
			Transform transform2 = transform.Find("Icon_Currency");
			if (transform2 != null && transform.Find("Icon_Currency_Craft") == null)
			{
				UISprite component = transform2.GetComponent<UISprite>();
				if (component != null)
				{
					UITexture uITexture = CurrencyUI.ReplaceWithCraftIcon(component);
					uITexture.transform.parent = transform;
					uITexture.transform.localPosition = transform2.localPosition;
				}
				UISprite[] componentsInChildren = transform2.GetComponentsInChildren<UISprite>(includeInactive: true);
				for (int i = 0; i < componentsInChildren.Length; i++)
				{
					componentsInChildren[i].enabled = false;
				}
			}
			SetLabel(transform, "Val_SoftCurrencyCount", saveData.CraftCurrency);
		}
		Transform transform3 = CardCreation_Panel.transform.Find("TopBar/Currency/Offset/HardCurrency");
		if (transform3 != null)
		{
			SetLabel(transform3, "Val_HardCurrencyCount", saveData.HardCurrency);
		}
	}

	private static void SetLabel(Transform parent, string name, int value)
	{
		Transform transform = parent.Find(name);
		UILabel uILabel = ((transform != null) ? transform.GetComponent<UILabel>() : null);
		if (uILabel != null)
		{
			uILabel.text = CurrencyUI.Format(value);
		}
	}

	public void OnClickCraft()
	{
		if (mSelectedRecipe != null && !mCrafting)
		{
			PlayerSaveData saveData = Singleton<PlayerInfoScript>.Instance.SaveData;
			if (saveData != null && saveData.CraftCurrency < mSelectedRecipe.CraftCurrencyCost)
			{
				Singleton<SimplePopupController>.Instance.ShowMessage("CRAFT", "You need more Craft Coins for this. Get them in the Store.");
			}
			else
			{
				Singleton<SimplePopupController>.Instance.ShowPrompt("CRAFT", "Craft this item? It uses the Craft Coins and materials shown.", ConfirmCraft, CancelCraft);
			}
		}
	}

	private void ConfirmCraft()
	{
		if (mSelectedRecipe != null && !mCrafting)
		{
			StartCoroutine(CraftSequence());
		}
	}

	private void CancelCraft()
	{
	}

	private IEnumerator CraftSequence()
	{
		mCrafting = true;
		if (CraftButton != null)
		{
			CraftButton.isEnabled = false;
		}
		yield return null;
		CardCraftingData cardCraftingData = mSelectedRecipe;
		PlayerSaveData saveData = Singleton<PlayerInfoScript>.Instance.SaveData;
		if (cardCraftingData == null || saveData == null || !PickIngredients(saveData, cardCraftingData, out var cards, out var mats, out var creatures))
		{
			Singleton<SimplePopupController>.Instance.ShowMessage("CRAFT", "You don't have everything this needs. Items on your team, equipped cards, your helper and favourites are never used.");
			mCrafting = false;
			RefreshRecipe();
			yield break;
		}
		if (cardCraftingData.Ingredients.Count == 0 && saveData.IsInventorySpaceFull())
		{
			Singleton<SimplePopupController>.Instance.ShowMessage("CRAFT", "Your inventory is full.");
			mCrafting = false;
			RefreshRecipe();
			yield break;
		}
		saveData.RemoveExCards(cards);
		saveData.RemoveEvoMaterials(mats);
		saveData.RemoveCreatures(creatures);
		saveData.CraftCurrency -= cardCraftingData.CraftCurrencyCost;
		PlayerSaveData.BeginGrant();
		bool flag = AddCraftingResult(cardCraftingData);
		string items = PlayerSaveData.EndGrant();
		if (flag)
		{
			saveData.ClaimCurrency("craft", cardCraftingData.ID, 0, 0, 0, 0, null, items);
		}
		else
		{
			Debug.LogError("[CardCrafting] Craft result could not be added: " + cardCraftingData.CraftItem);
		}
		Singleton<PlayerInfoScript>.Instance.Save();
		mCrafting = false;
		RefreshRecipe();
		Singleton<SimplePopupController>.Instance.ShowMessage(flag ? "CRAFT SUCCESS" : "CRAFT", flag ? "Crafting completed successfully." : "Something went wrong. Please restart the game.");
	}

	public void Unload()
	{
		mSelectedRecipe = null;
		ClearIngredients();
	}

	public void Open()
	{
		if (CardCreation_Panel == null)
		{
			Debug.LogError("[CardCrafting] CardCreation_Panel is NULL.");
			return;
		}
		CardCreation_Panel.SetActive(value: true);
		Populate();
	}

	public void Close()
	{
		if (!(CardCreation_Panel == null))
		{
			Unload();
			CardCreation_Panel.SetActive(value: false);
		}
	}

	public void OnClickBack()
	{
		Close();
	}

	private void UpdateCraftButton()
	{
		if (!(CraftButton == null))
		{
			CraftButton.isEnabled = !mCrafting && mSelectedRecipe != null && HasEnoughCraftingMaterials(mSelectedRecipe);
		}
	}

	private bool HasEnoughCraftingMaterials(CardCraftingData recipe)
	{
		PlayerSaveData saveData = Singleton<PlayerInfoScript>.Instance.SaveData;
		List<InventorySlotItem> cards;
		List<EvoMaterialData> mats;
		List<InventorySlotItem> creatures;
		if (recipe != null && saveData != null)
		{
			return PickIngredients(saveData, recipe, out cards, out mats, out creatures);
		}
		return false;
	}

	private static bool Usable(InventorySlotItem slot)
	{
		if (slot != null && !slot.InUse() && !slot.IsHelper())
		{
			return !slot.IsFavorite();
		}
		return false;
	}

	private static bool Matches(InventorySlotItem slot, string item, string id)
	{
		switch (item)
		{
		case "Card":
			if (slot.SlotType == InventorySlotType.Card && slot.Card != null && slot.Card.Form != null)
			{
				return slot.Card.Form.ID == id;
			}
			return false;
		case "EvoMaterial":
			if (slot.SlotType == InventorySlotType.EvoMaterial && slot.EvoMaterial != null)
			{
				return slot.EvoMaterial.ID == id;
			}
			return false;
		case "Creature":
			if (slot.SlotType == InventorySlotType.Creature && slot.Creature != null && slot.Creature.Form != null)
			{
				return slot.Creature.Form.ID == id;
			}
			return false;
		default:
			return false;
		}
	}

	public static int CountUsable(string item, string id)
	{
		PlayerSaveData saveData = Singleton<PlayerInfoScript>.Instance.SaveData;
		if (saveData == null)
		{
			return 0;
		}
		int num = 0;
		foreach (InventorySlotItem inventorySlot in saveData.InventorySlots)
		{
			if (Matches(inventorySlot, item, id) && Usable(inventorySlot))
			{
				num++;
			}
		}
		return num;
	}

	private static bool PickIngredients(PlayerSaveData saveData, CardCraftingData recipe, out List<InventorySlotItem> cards, out List<EvoMaterialData> mats, out List<InventorySlotItem> creatures)
	{
		cards = new List<InventorySlotItem>();
		mats = new List<EvoMaterialData>();
		creatures = new List<InventorySlotItem>();
		if (saveData.CraftCurrency < recipe.CraftCurrencyCost)
		{
			return false;
		}
		Dictionary<string, int> dictionary = new Dictionary<string, int>();
		foreach (CardCraftingIngredient ingredient in recipe.Ingredients)
		{
			if (ingredient != null)
			{
				if (ingredient.Item != "Card" && ingredient.Item != "EvoMaterial" && ingredient.Item != "Creature")
				{
					return false;
				}
				string key = ingredient.Item + "|" + ingredient.ItemData;
				dictionary[key] = (dictionary.ContainsKey(key) ? dictionary[key] : 0) + ingredient.Cost;
			}
		}
		foreach (KeyValuePair<string, int> item in dictionary)
		{
			string[] array = item.Key.Split('|');
			int num = item.Value;
			foreach (InventorySlotItem inventorySlot in saveData.InventorySlots)
			{
				if (num <= 0)
				{
					break;
				}
				if (Matches(inventorySlot, array[0], array[1]) && Usable(inventorySlot))
				{
					if (array[0] == "Card")
					{
						cards.Add(inventorySlot);
					}
					else if (array[0] == "Creature")
					{
						creatures.Add(inventorySlot);
					}
					else
					{
						mats.Add(inventorySlot.EvoMaterial);
					}
					num--;
				}
			}
			if (num > 0)
			{
				return false;
			}
		}
		return true;
	}

	private bool AddCraftingResult(CardCraftingData recipe)
	{
		if (recipe == null)
		{
			return false;
		}
		PlayerSaveData saveData = Singleton<PlayerInfoScript>.Instance.SaveData;
		if (saveData == null)
		{
			return false;
		}
		if (recipe.CraftItemData == "CardData")
		{
			CardData cardData = recipe.Card;
			if (cardData == null)
			{
				cardData = CardDataManager.Instance.GetData(recipe.CraftItem);
			}
			if (cardData == null)
			{
				Debug.LogError("[CardCrafting] Card result not found: " + recipe.CraftItem);
				return false;
			}
			CardItem card = new CardItem(cardData);
			if (saveData.AddExCard(card) == null)
			{
				return false;
			}
			Debug.Log("[CardCrafting] Added CARD result: " + cardData.ID);
			return true;
		}
		if (recipe.CraftItemData == "CreatureData")
		{
			CreatureData creatureData = recipe.Creature;
			if (creatureData == null)
			{
				creatureData = CreatureDataManager.Instance.GetData(recipe.CraftItem);
			}
			if (creatureData == null)
			{
				Debug.LogError("[CardCrafting] Creature result not found: " + recipe.CraftItem);
				return false;
			}
			CreatureItem creature = new CreatureItem(creatureData);
			if (saveData.AddCreature(creature) == null)
			{
				return false;
			}
			Debug.Log("[CardCrafting] Added CREATURE result: " + creatureData.ID);
			return true;
		}
		Debug.LogError("[CardCrafting] Unknown result type: " + recipe.CraftItemData);
		return false;
	}
}
