using UnityEngine;

public class CardCraftingIngredientItem : MonoBehaviour
{
	public UILabel Name;

	public UILabel RequiredCount;

	public UILabel OwnedCount;

	public GameObject OwnedMark;

	public GameObject UnavailableGroup;

	public Transform InventoryTileNode;

	private CardCraftingIngredient mData;

	public void Populate(CardCraftingIngredient data)
	{
		if (data == null)
		{
			Debug.LogError("[CardCraftingIngredientItem] Ingredient data is NULL.");
			return;
		}
		if (InventoryTileNode == null)
		{
			Debug.LogError("[CardCraftingIngredientItem] InventoryTileNode is NULL.");
			return;
		}
		mData = data;
		string ingredientName = GetIngredientName(data);
		if (Name != null)
		{
			Name.text = ingredientName;
		}
		int cost = data.Cost;
		if (RequiredCount != null)
		{
			RequiredCount.text = cost.ToString();
		}
		int ownedCount = GetOwnedCount(data);
		if (OwnedCount != null)
		{
			OwnedCount.text = ownedCount.ToString();
		}
		bool flag = ownedCount >= cost;
		if (OwnedMark != null)
		{
			OwnedMark.SetActive(flag);
		}
		if (UnavailableGroup != null)
		{
			UnavailableGroup.SetActive(!flag);
		}
		InventoryTileNode.DestroyAllChildren();
		GameObject inventoryTile = Singleton<PrefabReferences>.Instance.InventoryTile;
		if (inventoryTile == null)
		{
			Debug.LogError("[CardCraftingIngredientItem] InventoryTile prefab is NULL.");
			return;
		}
		GameObject gameObject = InventoryTileNode.InstantiateAsChild(inventoryTile);
		if (!(gameObject == null))
		{
			UIWidget[] componentsInChildren = gameObject.GetComponentsInChildren<UIWidget>(includeInactive: true);
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				componentsInChildren[i].depth += 100;
			}
			InventoryTile component = gameObject.GetComponent<InventoryTile>();
			if (component == null)
			{
				Debug.LogError("[CardCraftingIngredientItem] InventoryTile component missing.");
				NGUITools.Destroy(gameObject);
			}
			else
			{
				component.AssignedSlot = -1;
				component.SetAsDisplayOnly();
				PopulateInventoryTile(component, data);
			}
		}
	}

	private void PopulateInventoryTile(InventoryTile tile, CardCraftingIngredient data)
	{
		if (tile == null || data == null)
		{
			return;
		}
		if (data.Item == "Card")
		{
			CardData data2 = CardDataManager.Instance.GetData(data.ItemData);
			if (data2 == null)
			{
				Debug.LogError("[CardCraftingIngredientItem] Card not found: " + data.ItemData);
				return;
			}
			InventorySlotItem dataObj = new InventorySlotItem(data2);
			tile.Populate(dataObj);
		}
		else if (data.Item == "EvoMaterial")
		{
			EvoMaterialData data3 = EvoMaterialDataManager.Instance.GetData(data.ItemData);
			if (data3 == null)
			{
				Debug.LogError("[CardCraftingIngredientItem] EvoMaterial not found: " + data.ItemData);
				return;
			}
			InventorySlotItem dataObj2 = new InventorySlotItem(data3);
			tile.Populate(dataObj2);
		}
		else if (data.Item == "Creature")
		{
			CreatureData data4 = CreatureDataManager.Instance.GetData(data.ItemData);
			if (data4 == null)
			{
				Debug.LogError("[CardCraftingIngredientItem] Creature not found: " + data.ItemData);
				return;
			}
			Debug.Log("[CardCraftingIngredientItem] Displaying CREATURE: " + data.ItemData);
			tile.PopulateAndForceDisplay(data4);
		}
	}

	private string GetIngredientName(CardCraftingIngredient data)
	{
		if (data.Item == "Card")
		{
			CardData data2 = CardDataManager.Instance.GetData(data.ItemData);
			if (data2 == null)
			{
				return data.ItemData;
			}
			return data2.Name;
		}
		if (data.Item == "EvoMaterial")
		{
			EvoMaterialData data3 = EvoMaterialDataManager.Instance.GetData(data.ItemData);
			if (data3 == null)
			{
				return data.ItemData;
			}
			return data3.Name;
		}
		if (data.Item == "Creature")
		{
			CreatureData data4 = CreatureDataManager.Instance.GetData(data.ItemData);
			if (data4 == null)
			{
				return data.ItemData;
			}
			return data4.Name;
		}
		return data.ItemData;
	}

	private InventorySlotItem FindInventoryItem(CardCraftingIngredient data)
	{
		PlayerSaveData saveData = Singleton<PlayerInfoScript>.Instance.SaveData;
		if (saveData == null)
		{
			return null;
		}
		if (data.Item == "Card")
		{
			return saveData.FindExCard((CardItem card) => card != null && card.Form != null && card.Form.ID == data.ItemData);
		}
		if (data.Item == "EvoMaterial")
		{
			return saveData.FindEvoMaterial((InventorySlotItem item) => item != null && item.EvoMaterial != null && item.EvoMaterial.ID == data.ItemData);
		}
		if (data.Item == "Creature")
		{
			return saveData.FindCreature((InventorySlotItem item) => item != null && item.Creature != null && item.Creature.Form != null && item.Creature.Form.ID == data.ItemData);
		}
		return null;
	}

	private int GetOwnedCount(CardCraftingIngredient data)
	{
		if (data != null)
		{
			return CardCraftingController.CountUsable(data.Item, data.ItemData);
		}
		return 0;
	}
}
