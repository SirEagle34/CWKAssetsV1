using UnityEngine;

public class CardCraftingListItem : MonoBehaviour
{
	public UILabel Name;

	public Transform InventoryTileNode;

	private CardCraftingData mData;

	public GameObject SelectedGroup;

	public void Populate(object dataObj)
	{
		if (!(dataObj is CardCraftingData cardCraftingData))
		{
			Debug.LogError("CardCraftingListItem: data is NULL.");
			return;
		}
		if (InventoryTileNode == null)
		{
			Debug.LogError("CardCraftingListItem: InventoryTileNode is NULL.");
			return;
		}
		GameObject inventoryTile = Singleton<PrefabReferences>.Instance.InventoryTile;
		if (inventoryTile == null)
		{
			Debug.LogError("CardCraftingListItem: PrefabReferences.InventoryTile is NULL.");
			return;
		}
		mData = cardCraftingData;
		if (Name != null)
		{
			if (cardCraftingData.CraftItemData == "CardData")
			{
				if (cardCraftingData.Card != null)
				{
					Name.text = cardCraftingData.Card.Name;
				}
				else
				{
					Name.text = cardCraftingData.CraftItem;
				}
			}
			else if (cardCraftingData.CraftItemData == "CreatureData")
			{
				if (cardCraftingData.Creature != null)
				{
					Name.text = cardCraftingData.Creature.Name;
				}
				else
				{
					Name.text = cardCraftingData.CraftItem;
				}
			}
			else
			{
				Name.text = cardCraftingData.CraftItem;
			}
		}
		InventoryTileNode.DestroyAllChildren();
		GameObject gameObject = InventoryTileNode.InstantiateAsChild(inventoryTile);
		if (gameObject == null)
		{
			Debug.LogError("CardCraftingListItem: InventoryTile clone could not be created.");
			return;
		}
		UIWidget[] componentsInChildren = gameObject.GetComponentsInChildren<UIWidget>(includeInactive: true);
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].depth += 100;
		}
		InventoryTile component = gameObject.GetComponent<InventoryTile>();
		if (component == null)
		{
			Debug.LogError("CardCraftingListItem: Created InventoryTile has no InventoryTile component.");
		}
		else if (cardCraftingData.CraftItemData == "CardData")
		{
			if (cardCraftingData.Card == null)
			{
				Debug.LogError("CardCraftingListItem: Crafting Card is NULL: " + cardCraftingData.CraftItem);
				return;
			}
			Debug.Log("[CardCrafting] Displaying CARD: " + cardCraftingData.CraftItem);
			InventorySlotItem dataObj2 = new InventorySlotItem(cardCraftingData.Card);
			component.Populate(dataObj2);
		}
		else if (cardCraftingData.CraftItemData == "CreatureData")
		{
			if (cardCraftingData.Creature == null)
			{
				Debug.LogError("CardCraftingListItem: Crafting Creature is NULL: " + cardCraftingData.CraftItem);
				return;
			}
			Debug.Log("[CardCrafting] Displaying CREATURE: " + cardCraftingData.CraftItem);
			component.PopulateAndForceDisplay(cardCraftingData.Creature);
		}
		else
		{
			Debug.LogError("CardCraftingListItem: Unknown CraftItemData: " + cardCraftingData.CraftItemData);
		}
	}

	public void OnEntryClicked()
	{
		if (mData == null)
		{
			Debug.LogError("CardCraftingListItem: mData is NULL.");
		}
		else if (Singleton<CardCraftingController>.Instance == null)
		{
			Debug.LogError("CardCraftingListItem: CardCraftingController.Instance is NULL.");
		}
		else
		{
			Singleton<CardCraftingController>.Instance.SelectRecipe(mData);
		}
	}

	public CardCraftingData GetData()
	{
		return mData;
	}

	public void SetSelected(bool selected)
	{
		if (SelectedGroup != null)
		{
			SelectedGroup.SetActive(selected);
		}
	}
}
