public class CardCraftingIngredient
{
	private string _Item;

	private string _ItemData;

	private int _Cost;

	public string Item => _Item;

	public string ItemData => _ItemData;

	public int Cost => _Cost;

	public CardCraftingIngredient()
	{
	}

	public CardCraftingIngredient(string item, string itemData, int cost)
	{
		_Item = item;
		_ItemData = itemData;
		_Cost = cost;
	}
}
