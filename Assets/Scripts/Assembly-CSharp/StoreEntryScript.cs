using UnityEngine;

public class StoreEntryScript : UIStreamingGridListItem
{
	public enum ExtrasType
	{
		None = 0,
		RefillStamina = 1,
		ExpandInventory = 2,
		ExpandAllyList = 3,
		SoftCurrency = 4,
		EvolutionShards = 5,
		CraftCurrency = 6
	}

	private static bool sCloning;

	public ExtrasType Extra;

	public UILabel Name;

	public UILabel Cost;

	public UILabel CurrencyAmount;

	public UITexture Image;

	private StoreScreenController.FoundProductData mProductData;

	private void Awake()
	{
		if (Extra == ExtrasType.SoftCurrency)
		{
			Name.text = KFFLocalization.Get("!!BUY_GOLD").Replace("<val1>", MiscParams.BuySoftCurrencyAmount.ToString());
			Cost.text = MiscParams.BuySoftCurrencyCost.ToString();
			CurrencyAmount.text = "+" + MiscParams.BuySoftCurrencyAmount;
		}
		else if (Extra == ExtrasType.EvolutionShards)
		{
			Name.text = KFFLocalization.Get("!!BUY_EVOLUTION_SHARDS").Replace("<val1>", MiscParams.BuyEvolutionShardsAmount.ToString());
			Cost.text = MiscParams.BuyEvolutionShardsCost.ToString();
			CurrencyAmount.text = "+" + MiscParams.BuyEvolutionShardsAmount;
			//if (!sCloning)
			//{
			//	AddCraftCoinTile();
			//}
		}
		//else if (Extra == ExtrasType.CraftCurrency)
		//{
		//	ShowCraftCoinTile();
		//}
	}

	//private void AddCraftCoinTile()
	//{
	//	Transform parent = base.transform.parent;
	//	if (!(parent == null) && !(parent.Find("UI_All_StoreTile (Craft Coins)") != null))
	//	{
	//		sCloning = true;
	//		GameObject obj = Object.Instantiate(base.gameObject, parent, worldPositionStays: false);
	//		sCloning = false;
	//		obj.name = "UI_All_StoreTile (Craft Coins)";
	//		CurrencyUI.RemoveCopiedShadows(obj);
	//		obj.transform.SetSiblingIndex(base.transform.GetSiblingIndex() + 1);
	//		StoreEntryScript component = obj.GetComponent<StoreEntryScript>();
	//		component.Extra = ExtrasType.CraftCurrency;
	//		component.ShowCraftCoinTile();
	//		UIGrid component2 = parent.GetComponent<UIGrid>();
	//		if (component2 != null)
	//		{
	//			component2.Reposition();
	//		}
	//	}
	//}

	//private void ShowCraftCoinTile()
	//{
	//	int buyCraftCurrencyAmount = MiscParams.BuyCraftCurrencyAmount;
	//	Name.text = ((buyCraftCurrencyAmount == 1) ? "Buy 1 Craft Coin" : ("Buy " + buyCraftCurrencyAmount + " Craft Coins"));
	//	Cost.text = MiscParams.BuyCraftCurrencyCost.ToString();
	//	CurrencyAmount.text = "+" + buyCraftCurrencyAmount;
	//	UISprite[] componentsInChildren = GetComponentsInChildren<UISprite>(includeInactive: true);
	//	foreach (UISprite uISprite in componentsInChildren)
	//	{
	//		if (uISprite.spriteName != null && uISprite.spriteName.IndexOf("EvoShard") >= 0)
	//		{
	//			CurrencyUI.ReplaceWithCraftIcon(uISprite);
	//		}
	//	}
	//}

	public override void Populate(object dataObj)
	{
		mProductData = dataObj as StoreScreenController.FoundProductData;
		Name.text = mProductData.PackageData.DisplayName;
		CurrencyPackageData packageData = mProductData.PackageData;
		if (packageData.SocialCurrency > 0)
		{
			Cost.text = packageData.SocialCurrency + " PvP";
		}
		else
		{
			Cost.text = packageData.PaidHardCurrency + " Gems";
		}
		if (packageData.CustomizationCurrency > 0)
		{
			CurrencyAmount.text = packageData.CustomizationCurrency.ToString();
		}
		else
		{
			CurrencyAmount.text = packageData.TotalHardCurrency.ToString();
		}
		Image.ReplaceTexture(packageData.UITexture);
		if (packageData.VFXIndex != -1)
		{
			base.transform.InstantiateAsChild(Singleton<StoreScreenController>.Instance.StoreEntryVFXPrefabs[packageData.VFXIndex]);
		}
	}

	public void OnEntryClicked()
	{
		if (Extra == ExtrasType.RefillStamina)
		{
			Singleton<StoreScreenController>.Instance.OnClickRefillStamina();
		}
		else if (Extra == ExtrasType.ExpandInventory)
		{
			Singleton<StoreScreenController>.Instance.OnClickExpandInventory();
		}
		else if (Extra == ExtrasType.ExpandAllyList)
		{
			Singleton<StoreScreenController>.Instance.OnClickExpandAllyList();
		}
		else if (Extra == ExtrasType.SoftCurrency)
		{
			Singleton<StoreScreenController>.Instance.OnClickBuySoftCurrency();
		}
		else if (Extra == ExtrasType.EvolutionShards)
		{
			Singleton<StoreScreenController>.Instance.OnClickBuyEvolutionShards();
		}
		//else if (Extra == ExtrasType.CraftCurrency)
		//{
		//	Singleton<StoreScreenController>.Instance.OnClickBuyCraftCoins();
		//}
		else
		{
			StoreScreenController.GrantOldProduct(mProductData.PackageData.ID);
		}
	}
}
