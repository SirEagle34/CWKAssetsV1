using System;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

public class TradePanelController : Singleton<TradePanelController>
{
	public GameObject Trade_Panel;

	public UIGrid ListingGrid;

	public GameObject ListingItemPrefab;

	public UILabel CoinsLabel;

	public UILabel PlayerCoinsLabel;

	public UILabel PlayerGemsLabel;

	public UIGrid OffersGrid;

	public UILabel ListingsEmptyLabel;

	public UILabel OffersEmptyLabel;

	public GameObject BuyCoinsPanel;

	private const string ScenePlaceholderNumber = "2371400";

	private int mShownCoins = int.MinValue;

	private int mShownGems = int.MinValue;

	private bool mBusy;

	private float mBusySince = -1f;

	private const float kBusyMaxSeconds = 35f;

	private static readonly Dictionary<int, long[]> Packs = new Dictionary<int, long[]>
	{
		{
			100,
			new long[2] { 7000L, 700000L }
		},
		{
			500,
			new long[2] { 21500L, 2500000L }
		},
		{
			1000,
			new long[2] { 40000L, 4500000L }
		}
	};

	private SQServer Server => SessionManager.Instance.theSession.Server;

	public void Open()
	{
		Debug.Log("[Trade] OPEN");
		if (Trade_Panel == null)
		{
			Debug.LogError("[Trade] Trade_Panel is NULL.");
			return;
		}
		Trade_Panel.SetActive(value: true);
		ClearUnboundPlaceholders();
		Populate();
	}

	private void ClearUnboundPlaceholders()
	{
		if (Trade_Panel == null)
		{
			return;
		}
		UILabel[] componentsInChildren = Trade_Panel.GetComponentsInChildren<UILabel>(includeInactive: true);
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			if (componentsInChildren[i] != null && componentsInChildren[i].text == "2371400")
			{
				componentsInChildren[i].text = string.Empty;
			}
		}
		InvalidateWalletLabels();
		if (CoinsLabel == null)
		{
			Debug.LogWarning("[Trade] CoinsLabel is not wired; trade-coin balance cannot be shown.");
		}
		if (PlayerCoinsLabel == null || PlayerGemsLabel == null)
		{
			Debug.LogWarning("[Trade] wallet cards not wired (coins=" + (PlayerCoinsLabel != null) + " gems=" + (PlayerGemsLabel != null) + "); run TradeCoinsUI.Apply.");
		}
	}

	public void Close()
	{
		if (!(Trade_Panel == null))
		{
			Trade_Panel.SetActive(value: false);
		}
	}

	public void OnClickBack()
	{
		Close();
	}

	public void Populate()
	{
		RefreshBalance();
		RefreshPlayerWallet();
		SyncTradeChanges(null);
		RefreshListings();
		RefreshOffers();
	}

	private void Update()
	{
		if (!(Trade_Panel == null) && Trade_Panel.activeInHierarchy)
		{
			RefreshPlayerWallet();
		}
	}

	public void RefreshPlayerWallet()
	{
		PlayerInfoScript instance = Singleton<PlayerInfoScript>.Instance;
		if (!(instance == null) && instance.SaveData != null)
		{
			int softCurrency = instance.SaveData.SoftCurrency;
			int hardCurrency = instance.SaveData.HardCurrency;
			if (PlayerCoinsLabel != null && softCurrency != mShownCoins)
			{
				PlayerCoinsLabel.text = softCurrency.ToString("N0");
				mShownCoins = softCurrency;
			}
			if (PlayerGemsLabel != null && hardCurrency != mShownGems)
			{
				PlayerGemsLabel.text = hardCurrency.ToString("N0");
				mShownGems = hardCurrency;
			}
		}
	}

	private void InvalidateWalletLabels()
	{
		mShownCoins = int.MinValue;
		mShownGems = int.MinValue;
	}

	public void RefreshBalance()
	{
		Server.TradeGetBalance(delegate(Dictionary<string, object> resp, HttpStatusCode status)
		{
			int num = 0;
			Dictionary<string, object> dataDict = GetDataDict(resp);
			if (dataDict != null && dataDict.ContainsKey("coins"))
			{
				num = ToInt(dataDict["coins"]);
			}
			if (CoinsLabel != null)
			{
				CoinsLabel.text = num.ToString();
			}
		});
	}

	public void RefreshListings()
	{
		Server.TradeBrowse(1, 30, delegate(Dictionary<string, object> resp, HttpStatusCode status)
		{
			PopulateListings(GetDataObject(resp) as List<object>);
		});
	}

	private void PopulateListings(List<object> listings)
	{
		if (ListingGrid == null || ListingItemPrefab == null)
		{
			Debug.LogError("[Trade] ListingGrid or ListingItemPrefab is NULL.");
			return;
		}
		ListingGrid.transform.DestroyAllChildren();
		int num = listings?.Count ?? 0;
		if (ListingsEmptyLabel != null)
		{
			if (string.IsNullOrEmpty(ListingsEmptyLabel.text))
			{
				ListingsEmptyLabel.text = "No creatures listed yet.\nCheck back soon!";
			}
			CenterInList(ListingsEmptyLabel);
			ListingsEmptyLabel.gameObject.SetActive(num == 0);
		}
		if (listings != null)
		{
			for (int i = 0; i < listings.Count; i++)
			{
				GameObject gameObject = ListingGrid.transform.InstantiateAsChild(ListingItemPrefab);
				if (!(gameObject == null))
				{
					gameObject.ChangeLayer(ListingGrid.gameObject.layer);
					TradeListItem component = gameObject.GetComponent<TradeListItem>();
					if (component == null)
					{
						Debug.LogError("[Trade] ListingItemPrefab has no TradeListItem.");
					}
					else
					{
						component.Populate(listings[i]);
					}
				}
			}
		}
		ListingGrid.Reposition();
	}

	private bool BeginBusy()
	{
		if (mBusy && mBusySince >= 0f && RealTime.time - mBusySince < 35f)
		{
			return false;
		}
		mBusy = true;
		mBusySince = RealTime.time;
		Singleton<BusyIconPanelController>.Instance.Show();
		return true;
	}

	private void EndBusy()
	{
		mBusy = false;
		mBusySince = -1f;
		Singleton<BusyIconPanelController>.Instance.Hide();
	}

	public static void SyncTradeChanges(Action done)
	{
		SessionManager.Instance.theSession.Server.TradeSync(delegate(Dictionary<string, object> resp, HttpStatusCode status)
		{
			PlayerInfoScript instance = Singleton<PlayerInfoScript>.Instance;
			Dictionary<string, object> dictionary = (IsSuccess(resp) ? GetDataDict(resp) : null);
			bool flag = false;
			if (instance != null && instance.SaveData != null && dictionary != null)
			{
				foreach (object item in (dictionary.ContainsKey("remove") ? (dictionary["remove"] as List<object>) : null) ?? new List<object>())
				{
					if (item is Dictionary<string, object> dictionary2)
					{
						InventorySlotItem creatureItem = instance.SaveData.GetCreatureItem(ToInt(dictionary2["unique_id"]));
						if (creatureItem != null && creatureItem.Creature != null && creatureItem.Creature.Form != null && creatureItem.Creature.Form.ID == dictionary2["creature_id"] as string)
						{
							instance.SaveData.RemoveCreature(creatureItem);
							flag = true;
						}
					}
				}
				foreach (object item2 in (dictionary.ContainsKey("add") ? (dictionary["add"] as List<object>) : null) ?? new List<object>())
				{
					if (item2 is Dictionary<string, object> dictionary3 && !instance.SaveData.IsInventorySpaceFull())
					{
						int num = (dictionary3.ContainsKey("UniqueID") ? ToInt(dictionary3["UniqueID"]) : 0);
						if (num != 0 && instance.SaveData.GetCreatureItem(num) == null)
						{
							CreatureItem creatureItem2 = new CreatureItem(dictionary3);
							if (creatureItem2.Form != null)
							{
								instance.SaveData.AddCreature(creatureItem2);
								flag = true;
							}
						}
					}
				}
			}
			if (flag)
			{
				instance.Save();
			}
			if (done != null)
			{
				done();
			}
		});
	}

	public void OnClickBuy(int listingId)
	{
		if (!BeginBusy())
		{
			return;
		}
		Server.TradeBuy(listingId, delegate(Dictionary<string, object> resp, HttpStatusCode status)
		{
			EndBusy();
			if (IsSuccess(resp))
			{
				Singleton<SimplePopupController>.Instance.ShowMessage("Trade", "Purchased!");
				SyncTradeChanges(null);
				Populate();
			}
			else
			{
				Singleton<SimplePopupController>.Instance.ShowMessage("Trade", GetError(resp));
			}
		});
	}

	public void OpenBuyCoins()
	{
		if (BuyCoinsPanel == null)
		{
			return;
		}
		UIPanel component = BuyCoinsPanel.GetComponent<UIPanel>();
		if (component != null)
		{
			int num = 0;
			foreach (UIPanel item in UIPanel.list)
			{
				if (!(item == null) && !(item == component) && item.gameObject.activeInHierarchy && item.depth > num)
				{
					num = item.depth;
				}
			}
			component.depth = Mathf.Min(num + 10, 90);
		}
		string[] array = new string[5] { "CustomCaption", "CustomAmountBox", "BuyCustomGems", "BuyCustomCoins", "CustomCostLabel" };
		foreach (string text in array)
		{
			Transform[] componentsInChildren = BuyCoinsPanel.GetComponentsInChildren<Transform>(includeInactive: true);
			foreach (Transform transform in componentsInChildren)
			{
				if (transform.name == text)
				{
					transform.gameObject.SetActive(value: false);
				}
			}
		}
		foreach (KeyValuePair<int, long[]> pack in Packs)
		{
			SetButtonText("BuyGem_" + pack.Key, ButtonPrice(pack.Value[0]));
			SetButtonText("BuyCoin_" + pack.Key, ButtonPrice(pack.Value[1]));
		}
		LayoutBuyCoins();
		BuyCoinsPanel.SetActive(value: true);
	}

	private void LayoutBuyCoins()
	{
		Dictionary<string, Transform> dictionary = new Dictionary<string, Transform>();
		Transform[] componentsInChildren = BuyCoinsPanel.GetComponentsInChildren<Transform>(includeInactive: true);
		foreach (Transform transform in componentsInChildren)
		{
			if (!dictionary.ContainsKey(transform.name))
			{
				dictionary[transform.name] = transform;
			}
		}
		if (dictionary.TryGetValue("BuyCoinsTitle", out var value))
		{
			UILabel component = value.GetComponent<UILabel>();
			if (component != null)
			{
				component.pivot = UIWidget.Pivot.Center;
				component.alignment = NGUIText.Alignment.Center;
			}
			value.localPosition = new Vector3(0f, 240f, value.localPosition.z);
		}
		int[] array = new int[3] { 100, 500, 1000 };
		for (int j = 0; j < array.Length; j++)
		{
			float y = 110f - 150f * (float)j;
			string[] array2 = new string[4] { "TCIcon_", "TCAmount_", "BuyGem_", "BuyCoin_" };
			foreach (string text in array2)
			{
				if (dictionary.TryGetValue(text + array[j], out var value2))
				{
					value2.localPosition = new Vector3(value2.localPosition.x, y, value2.localPosition.z);
				}
			}
		}
	}

	private void SetButtonText(string buttonName, string text)
	{
		Transform[] componentsInChildren = BuyCoinsPanel.GetComponentsInChildren<Transform>(includeInactive: true);
		foreach (Transform transform in componentsInChildren)
		{
			if (transform.name != buttonName)
			{
				continue;
			}
			UILabel[] componentsInChildren2 = transform.GetComponentsInChildren<UILabel>(includeInactive: true);
			foreach (UILabel uILabel in componentsInChildren2)
			{
				if (uILabel.name == "Val_PowerUpCost")
				{
					uILabel.text = text;
				}
			}
			break;
		}
	}

	private static void CenterInList(UILabel label)
	{
		UIPanel uIPanel = NGUITools.FindInParents<UIPanel>(label.gameObject);
		if (!(uIPanel == null) && uIPanel.clipping != UIDrawCall.Clipping.None)
		{
			Vector4 baseClipRegion = uIPanel.baseClipRegion;
			Vector3 position = uIPanel.transform.TransformPoint(new Vector3(baseClipRegion.x + uIPanel.clipOffset.x, baseClipRegion.y + uIPanel.clipOffset.y, 0f));
			label.transform.position = position;
			label.pivot = UIWidget.Pivot.Center;
			label.alignment = NGUIText.Alignment.Center;
			label.overflowMethod = UILabel.Overflow.ResizeHeight;
			label.width = Mathf.Max(100, Mathf.RoundToInt(baseClipRegion.z) - 40);
		}
	}

	public static void FillWalletCards(GameObject root)
	{
		PlayerInfoScript instance = Singleton<PlayerInfoScript>.Instance;
		if (root == null || instance == null || instance.SaveData == null)
		{
			return;
		}
		UILabel[] componentsInChildren = root.GetComponentsInChildren<UILabel>(includeInactive: true);
		foreach (UILabel uILabel in componentsInChildren)
		{
			if (uILabel.name == "Val_HardCurrencyCount")
			{
				uILabel.text = instance.SaveData.HardCurrency.ToString("N0");
			}
			else if (uILabel.name == "Val_SoftCurrencyCount")
			{
				uILabel.text = instance.SaveData.SoftCurrency.ToString("N0");
			}
		}
	}

	public void CloseBuyCoins()
	{
		if (BuyCoinsPanel != null)
		{
			BuyCoinsPanel.SetActive(value: false);
		}
	}

	public static long CostFor(int tradeCoins, string currency)
	{
		if (!Packs.TryGetValue(tradeCoins, out var value))
		{
			return 0L;
		}
		if (!(currency == "coins"))
		{
			return value[0];
		}
		return value[1];
	}

	private static string PriceText(int tradeCoins, string currency)
	{
		return CostFor(tradeCoins, currency).ToString("N0") + ((currency == "coins") ? " coins" : " gems");
	}

	private static string ButtonPrice(long v)
	{
		if (v >= 1000000)
		{
			return ((float)v / 1000000f).ToString("0.#") + "M";
		}
		if (v >= 100000)
		{
			return v / 1000 + "K";
		}
		return v.ToString("N0");
	}

	private void BuyAmount(int tradeCoins, string currency)
	{
		CloseBuyCoins();
		BuyPack(tradeCoins, currency, PriceText(tradeCoins, currency));
	}

	public void OnBuy100Gems()
	{
		BuyAmount(100, "gems");
	}

	public void OnBuy500Gems()
	{
		BuyAmount(500, "gems");
	}

	public void OnBuy1000Gems()
	{
		BuyAmount(1000, "gems");
	}

	public void OnBuy100Coins()
	{
		BuyAmount(100, "coins");
	}

	public void OnBuy500Coins()
	{
		BuyAmount(500, "coins");
	}

	public void OnBuy1000Coins()
	{
		BuyAmount(1000, "coins");
	}

	private void BuyPack(int pack, string currency, string priceText)
	{
		SimplePopupController instance = Singleton<SimplePopupController>.Instance;
		string body = "Buy " + pack + " Trade Coins for " + priceText + "?";
		if (instance != null)
		{
			instance.ShowPrompt("Buy Trade Coins", body, delegate
			{
				DoBuyPack(pack, currency);
			}, null, "Buy", "Cancel");
		}
		else
		{
			DoBuyPack(pack, currency);
		}
	}

	private void DoBuyPack(int pack, string currency)
	{
		if (!BeginBusy())
		{
			return;
		}
		Server.TradeBuyPack(pack, currency, delegate(Dictionary<string, object> resp, HttpStatusCode status)
		{
			EndBusy();
			if (IsSuccess(resp))
			{
				Dictionary<string, object> dataDict = GetDataDict(resp);
				int num = ((dataDict != null && dataDict.ContainsKey("spent")) ? ToInt(dataDict["spent"]) : 0);
				PlayerInfoScript instance = Singleton<PlayerInfoScript>.Instance;
				if (instance != null && instance.SaveData != null && num > 0)
				{
					if (currency == "coins")
					{
						instance.SaveData.SoftCurrency -= num;
					}
					else
					{
						instance.SaveData.HardCurrency -= num;
					}
				}
				RefreshBalance();
				RefreshPlayerWallet();
				Singleton<SimplePopupController>.Instance.ShowMessage("Trade", "Purchased " + pack + " Trade Coins!");
			}
			else
			{
				Singleton<SimplePopupController>.Instance.ShowMessage("Trade", GetError(resp));
			}
		});
	}

	public void RefreshOffers()
	{
		Server.TradeOffersReceived(delegate(Dictionary<string, object> resp, HttpStatusCode status)
		{
			if (!IsSuccess(resp))
			{
				Debug.LogWarning("[Trade] offers failed: " + GetError(resp));
			}
			else
			{
				PopulateOffers(GetDataObject(resp) as List<object>);
			}
		});
	}

	private void PopulateOffers(List<object> offers)
	{
		UIGrid uIGrid = ((OffersGrid != null) ? OffersGrid : ListingGrid);
		if (uIGrid == null || ListingItemPrefab == null)
		{
			Debug.LogError("[Trade] offers grid/prefab not wired.");
			return;
		}
		uIGrid.transform.DestroyAllChildren();
		int num = offers?.Count ?? 0;
		if (OffersEmptyLabel != null)
		{
			if (string.IsNullOrEmpty(OffersEmptyLabel.text))
			{
				OffersEmptyLabel.text = "No incoming offers.";
			}
			CenterInList(OffersEmptyLabel);
			OffersEmptyLabel.gameObject.SetActive(num == 0);
		}
		if (offers != null)
		{
			foreach (object offer in offers)
			{
				GameObject obj = uIGrid.transform.InstantiateAsChild(ListingItemPrefab);
				obj.ChangeLayer(uIGrid.gameObject.layer);
				TradeListItem component = obj.GetComponent<TradeListItem>();
				if (component != null)
				{
					component.Populate(offer);
				}
			}
		}
		uIGrid.Reposition();
		Debug.Log("[Trade] offers shown: " + num);
	}

	public void OnClickAcceptOffer(int offerId)
	{
		if (!BeginBusy())
		{
			return;
		}
		Server.TradeAcceptOffer(offerId, delegate(Dictionary<string, object> resp, HttpStatusCode status)
		{
			EndBusy();
			string title = "Trade";
			if (IsSuccess(resp))
			{
				Singleton<SimplePopupController>.Instance.ShowMessage(title, "Trade complete!");
				SyncTradeChanges(null);
				RefreshOffers();
			}
			else
			{
				Singleton<SimplePopupController>.Instance.ShowMessage(title, GetError(resp));
			}
		});
	}

	public void OnClickDeclineOffer(int offerId)
	{
		if (!BeginBusy())
		{
			return;
		}
		Server.TradeDeclineOffer(offerId, delegate(Dictionary<string, object> resp, HttpStatusCode status)
		{
			EndBusy();
			if (IsSuccess(resp))
			{
				RefreshOffers();
			}
			else
			{
				Singleton<SimplePopupController>.Instance.ShowMessage("Trade", GetError(resp));
			}
		});
	}

	public void OnClickNewOffer()
	{
		TradeOfferController tradeOfferController = Singleton<TradeOfferController>.Instance;
		if (tradeOfferController == null)
		{
			TradeOfferController[] array = Resources.FindObjectsOfTypeAll<TradeOfferController>();
			foreach (TradeOfferController tradeOfferController2 in array)
			{
				if (tradeOfferController2 != null && tradeOfferController2.gameObject.scene.IsValid())
				{
					tradeOfferController = tradeOfferController2;
					break;
				}
			}
		}
		if (tradeOfferController != null)
		{
			Close();
			tradeOfferController.OpenPickRecipient();
		}
		else
		{
			Debug.LogWarning("[Trade] TradeOfferController not in scene - run TradeOfferPanelBuilder.");
		}
	}

	public void OnClickSell()
	{
		TradeOfferController tradeOfferController = Singleton<TradeOfferController>.Instance;
		if (tradeOfferController == null)
		{
			TradeOfferController[] array = Resources.FindObjectsOfTypeAll<TradeOfferController>();
			foreach (TradeOfferController tradeOfferController2 in array)
			{
				if (tradeOfferController2 != null && tradeOfferController2.gameObject.scene.IsValid())
				{
					tradeOfferController = tradeOfferController2;
					break;
				}
			}
		}
		if (tradeOfferController != null)
		{
			Close();
			tradeOfferController.OpenForListing();
		}
		else
		{
			Debug.LogWarning("[Trade] TradeOfferController not in scene - run TradeOfferPanelBuilder.");
		}
	}

	public void SendOfferTo(string toPlayerId, string offerUniqueIds, string requestUniqueIds)
	{
		Server.TradeSendOffer(toPlayerId, offerUniqueIds, requestUniqueIds, 0, 0, delegate(Dictionary<string, object> resp, HttpStatusCode status)
		{
			string title = "Trade";
			if (IsSuccess(resp))
			{
				Singleton<SimplePopupController>.Instance.ShowMessage(title, "Offer sent!");
			}
			else
			{
				Singleton<SimplePopupController>.Instance.ShowMessage(title, GetError(resp));
			}
		});
	}

	private static bool IsSuccess(Dictionary<string, object> resp)
	{
		if (resp != null && resp.TryGetValue("success", out var value) && value is bool)
		{
			return (bool)value;
		}
		return false;
	}

	private static string GetError(Dictionary<string, object> resp)
	{
		if (resp != null && resp.TryGetValue("error", out var value) && value != null)
		{
			return TradeErrorText.Friendly(value.ToString());
		}
		return "Couldn't reach the trade server. Check your connection and try again.";
	}

	private static Dictionary<string, object> GetDataDict(Dictionary<string, object> resp)
	{
		if (resp != null && resp.TryGetValue("data", out var value))
		{
			return value as Dictionary<string, object>;
		}
		return null;
	}

	private static object GetDataObject(Dictionary<string, object> resp)
	{
		if (resp != null && resp.TryGetValue("data", out var value))
		{
			return value;
		}
		return null;
	}

	private static int ToInt(object v)
	{
		if (v == null)
		{
			return 0;
		}
		if (v is long)
		{
			return (int)(long)v;
		}
		if (v is double)
		{
			return (int)(double)v;
		}
		if (!int.TryParse(v.ToString(), out var result))
		{
			return 0;
		}
		return result;
	}
}
