using System.Collections.Generic;
using UnityEngine;

public class CardCraftingData : ILoadableData
{
	private string _ID;

	private string _CraftItem;

	private string _CraftItemData;

	private int _EvolutionShardsCost;

	private CardData _Card;

	private CreatureData _Creature;

	private List<CardCraftingIngredient> _Ingredients = new List<CardCraftingIngredient>();

	public string ID => _ID;

	public string CraftItem => _CraftItem;

	public string CraftItemData => _CraftItemData;

	public CardData Card => _Card;

	public CreatureData Creature => _Creature;

	public int CraftCurrencyCost => _EvolutionShardsCost;

	public List<CardCraftingIngredient> Ingredients => _Ingredients;

	public void Populate(Dictionary<string, object> dict)
	{
		_ID = TFUtils.LoadString(dict, "ID", string.Empty);
		_CraftItem = TFUtils.LoadString(dict, "CraftItem", string.Empty);
		_CraftItemData = TFUtils.LoadString(dict, "CraftItemData", string.Empty);
		_EvolutionShardsCost = TFUtils.LoadInt(dict, "CraftCurrencyCost", TFUtils.LoadInt(dict, "EvolutionShardsCost", 0));
		if (_CraftItemData == "CardData")
		{
			_Card = CardDataManager.Instance.GetData(_CraftItem);
			if (_Card == null)
			{
				Debug.LogError("CardCraftingData: Card not found. CraftItem: " + _CraftItem);
			}
		}
		else if (_CraftItemData == "CreatureData")
		{
			_Creature = CreatureDataManager.Instance.GetData(_CraftItem);
			if (_Creature == null)
			{
				Debug.LogError("CardCraftingData: Creature not found. CraftItem: " + _CraftItem);
			}
		}
		else
		{
			Debug.LogError("CardCraftingData: Unknown CraftItemData: " + _CraftItemData);
		}
		_Ingredients.Clear();
		int num = 1;
		while (true)
		{
			string text = "Item" + num;
			if (dict.ContainsKey(text))
			{
				string text2 = TFUtils.LoadString(dict, text, string.Empty);
				string itemData = TFUtils.LoadString(dict, text + "Data", string.Empty);
				int cost = TFUtils.LoadInt(dict, text + "Cost", 0);
				if (!string.IsNullOrEmpty(text2))
				{
					CardCraftingIngredient item = new CardCraftingIngredient(text2, itemData, cost);
					_Ingredients.Add(item);
				}
				num++;
				continue;
			}
			break;
		}
	}
}
