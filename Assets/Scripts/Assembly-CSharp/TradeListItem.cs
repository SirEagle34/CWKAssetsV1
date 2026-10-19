using System.Collections.Generic;
using Allies;
using UnityEngine;

public class TradeListItem : MonoBehaviour
{
	public UILabel NameLabel;

	public UILabel PriceLabel;

	public Transform InventoryTileNode;

	public GameObject SelectedGroup;

	private int mListingId;

	private string mSellerId;

	private string mSellerName;

	private bool mIsOffer;

	private string mName;

	private int mPrice;

	private string mGiveSummary;

	private string mWantSummary;

	private bool mWantMissing;

	private bool mIsOwnedSelect;

	private int mOwnedUniqueId;

	private CreatureItem mOwnedCreature;

	private string mOwnedStarText;

	private Color mPriceBaseColor = Color.white;

	private TradeOfferController mOwner;

	private bool mIsWantSelect;

	private string mWantCreatureId;

	private int mWantStar;

	private string mSelectedText = "✓ In offer";

	private bool mIsBasket;

	private bool mHighlightLifted;

	private bool mIsRecipient;

	private string mRecipientId;

	public void Populate(object dataObj)
	{
		if (!(dataObj is Dictionary<string, object> dictionary))
		{
			Debug.LogError("TradeListItem: data is NULL.");
			return;
		}
		string text;
		int num;
		if (dictionary.ContainsKey("offer_id"))
		{
			mListingId = ToInt(Get(dictionary, "offer_id"));
			mSellerId = Get(dictionary, "from_id") as string;
			mSellerName = Get(dictionary, "from_name") as string;
			text = null;
			num = 0;
			Dictionary<string, object> dictionary2 = Get(dictionary, "they_give") as Dictionary<string, object>;
			if (dictionary2 != null)
			{
				num = ToInt(Get(dictionary2, "coins"));
				if (Get(dictionary2, "creatures") is List<object> { Count: >0 } list && list[0] is Dictionary<string, object> d)
				{
					text = Get(d, "creature_id") as string;
				}
			}
			mIsOffer = true;
			mGiveSummary = DescribeGive(dictionary2);
			mWantSummary = DescribeWant(Get(dictionary, "they_want") as Dictionary<string, object>);
		}
		else
		{
			mListingId = ToInt(Get(dictionary, "listing_id"));
			mSellerId = Get(dictionary, "seller_id") as string;
			mSellerName = Get(dictionary, "seller_name") as string;
			num = ToInt(Get(dictionary, "price"));
			text = Get(dictionary, "creature_id") as string;
			mIsOffer = false;
		}
		CreatureData creatureData = null;
		if (!string.IsNullOrEmpty(text) && CreatureDataManager.Instance != null)
		{
			creatureData = CreatureDataManager.Instance.GetData(text);
		}
		mName = ((creatureData != null) ? creatureData.Name : ((text != null) ? text : "?"));
		mPrice = num;
		if (NameLabel != null)
		{
			NameLabel.text = mName;
		}
		if (PriceLabel != null)
		{
			PriceLabel.text = (mIsOffer ? ("wants: " + mWantSummary) : (num + " coins"));
		}
		if (!(InventoryTileNode != null))
		{
			return;
		}
		InventoryTileNode.DestroyAllChildren();
		GameObject inventoryTile = Singleton<PrefabReferences>.Instance.InventoryTile;
		if (!(inventoryTile != null) || creatureData == null)
		{
			return;
		}
		GameObject gameObject = InventoryTileNode.InstantiateAsChild(inventoryTile);
		if (gameObject != null)
		{
			gameObject.ChangeLayer(InventoryTileNode.gameObject.layer);
			UIWidget[] componentsInChildren = gameObject.GetComponentsInChildren<UIWidget>(includeInactive: true);
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				componentsInChildren[i].depth += 100;
			}
			InventoryTile component = gameObject.GetComponent<InventoryTile>();
			if (component != null)
			{
				component.PopulateAndForceDisplay(creatureData);
			}
		}
	}

	public void PopulateOwned(CreatureItem cre, TradeOfferController owner)
	{
		mIsOwnedSelect = true;
		mIsWantSelect = false;
		mIsBasket = false;
		mOwner = owner;
		mIsOffer = false;
		mWantMissing = false;
		mWantCreatureId = null;
		mSelectedText = "✓ In offer";
		if (cre != null)
		{
			mOwnedCreature = cre;
			mOwnedUniqueId = cre.UniqueId;
			CreatureData form = cre.Form;
			mName = ((form != null) ? form.Name : "?");
			if (NameLabel != null)
			{
				NameLabel.text = mName;
			}
			if (PriceLabel != null)
			{
				PriceLabel.text = ((cre.StarRating > 0) ? ("Star " + cre.StarRating) : string.Empty);
				mOwnedStarText = PriceLabel.text;
				mPriceBaseColor = PriceLabel.color;
			}
			BuildCreatureArt(form);
			SetOwnedSelected(owner != null && owner.IsCreatureSelected(mOwnedUniqueId));
		}
	}

	private void BuildCreatureArt(CreatureData data)
	{
		if (InventoryTileNode == null)
		{
			return;
		}
		InventoryTileNode.DestroyAllChildren();
		GameObject inventoryTile = Singleton<PrefabReferences>.Instance.InventoryTile;
		if (inventoryTile == null || data == null)
		{
			return;
		}
		GameObject gameObject = InventoryTileNode.InstantiateAsChild(inventoryTile);
		if (!(gameObject == null))
		{
			gameObject.ChangeLayer(InventoryTileNode.gameObject.layer);
			UIWidget[] componentsInChildren = gameObject.GetComponentsInChildren<UIWidget>(includeInactive: true);
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				componentsInChildren[i].depth += 100;
			}
			InventoryTile component = gameObject.GetComponent<InventoryTile>();
			if (component != null)
			{
				component.PopulateAndForceDisplay(data);
			}
		}
	}

	public void SetOwnedSelected(bool on)
	{
		if ((mIsOwnedSelect || mIsWantSelect) && PriceLabel != null)
		{
			PriceLabel.text = (on ? mSelectedText : mOwnedStarText);
			PriceLabel.color = (on ? new Color(0.3f, 0.85f, 0.3f) : mPriceBaseColor);
		}
		if (!(SelectedGroup != null))
		{
			return;
		}
		if (on && !mHighlightLifted)
		{
			UIWidget[] componentsInChildren = SelectedGroup.GetComponentsInChildren<UIWidget>(includeInactive: true);
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				componentsInChildren[i].depth += 150;
			}
			mHighlightLifted = true;
		}
		SelectedGroup.SetActive(on);
	}

	public void RefreshOwnedSelected()
	{
		if (!(mOwner == null))
		{
			if (mIsOwnedSelect)
			{
				SetOwnedSelected(mOwner.IsCreatureSelected(mOwnedUniqueId));
			}
			else if (mIsWantSelect)
			{
				SetOwnedSelected(mOwner.IsWantSelected(mOwnedUniqueId));
			}
		}
	}

	public void PopulateBasket(CreatureItem cre, TradeOfferController owner)
	{
		PopulateOwned(cre, owner);
		mIsBasket = true;
		mIsOwnedSelect = false;
		SetOwnedSelected(on: false);
		if (PriceLabel != null)
		{
			PriceLabel.text = "tap to remove";
			PriceLabel.color = mPriceBaseColor;
		}
		LiftAboveBasketFrame();
	}

	public void PopulateWant(int uniqueId, string creatureId, int star, TradeOfferController owner)
	{
		mIsWantSelect = true;
		mIsOwnedSelect = false;
		mIsBasket = false;
		mOwner = owner;
		mIsOffer = false;
		mWantMissing = false;
		mSelectedText = "✓ Requested";
		mOwnedUniqueId = uniqueId;
		mWantCreatureId = creatureId;
		mWantStar = star;
		CreatureData creatureData = ((!string.IsNullOrEmpty(creatureId) && CreatureDataManager.Instance != null) ? CreatureDataManager.Instance.GetData(creatureId) : null);
		mName = ((creatureData != null) ? creatureData.Name : creatureId);
		if (NameLabel != null)
		{
			NameLabel.text = mName;
		}
		if (PriceLabel != null)
		{
			PriceLabel.text = ((star > 0) ? ("Star " + star) : string.Empty);
			mOwnedStarText = PriceLabel.text;
			mPriceBaseColor = PriceLabel.color;
		}
		BuildCreatureArt(creatureData);
		SetOwnedSelected(owner != null && owner.IsWantSelected(uniqueId));
	}

	public void PopulateWantBasket(int uniqueId, string creatureId, int star, TradeOfferController owner)
	{
		PopulateWant(uniqueId, creatureId, star, owner);
		mIsBasket = true;
		mIsWantSelect = false;
		SetOwnedSelected(on: false);
		if (PriceLabel != null)
		{
			PriceLabel.text = "tap to remove";
			PriceLabel.color = mPriceBaseColor;
		}
		mWantCreatureId = creatureId;
		LiftAboveBasketFrame();
	}

	private void LiftAboveBasketFrame()
	{
		UIWidget[] componentsInChildren = GetComponentsInChildren<UIWidget>(includeInactive: true);
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].depth += 400;
		}
	}

	public void PopulateDealThumb(int uniqueId, CreatureData data, string creatureId, int star, bool isWant, TradeOfferController owner)
	{
		mIsOwnedSelect = false;
		mIsWantSelect = false;
		mIsRecipient = false;
		mIsOffer = false;
		mIsBasket = true;
		mOwner = owner;
		mOwnedUniqueId = uniqueId;
		mWantCreatureId = (isWant ? creatureId : null);
		mWantStar = star;
		if (NameLabel != null)
		{
			NameLabel.gameObject.SetActive(value: false);
		}
		if (PriceLabel != null)
		{
			PriceLabel.gameObject.SetActive(value: false);
		}
		if (SelectedGroup != null)
		{
			SelectedGroup.SetActive(value: false);
		}
		BuildCreatureArt(data);
		LiftAboveBasketFrame();
		base.transform.localScale = new Vector3(0.42f, 0.42f, 1f);
	}

	public void PopulateRecipient(AllyData ally, TradeOfferController owner)
	{
		mIsRecipient = true;
		mIsOwnedSelect = false;
		mIsWantSelect = false;
		mIsBasket = false;
		mIsOffer = false;
		mWantMissing = false;
		mOwner = owner;
		mRecipientId = ally.Id;
		mName = (string.IsNullOrEmpty(ally.Name) ? ally.Id : ally.Name);
		if (NameLabel != null)
		{
			NameLabel.gameObject.SetActive(value: true);
			NameLabel.text = mName;
		}
		if (PriceLabel != null)
		{
			PriceLabel.gameObject.SetActive(value: true);
			PriceLabel.text = ((ally.Level > 0) ? ("Level " + ally.Level) : "tap to choose");
			PriceLabel.color = mPriceBaseColor;
		}
		if (InventoryTileNode != null)
		{
			InventoryTileNode.DestroyAllChildren();
		}
		CreatureData creatureData = null;
		try
		{
			PlayerInfoScript instance = Singleton<PlayerInfoScript>.Instance;
			if (instance != null && !string.IsNullOrEmpty(ally.HelperCreature))
			{
				InventorySlotItem inventorySlotItem = instance.DeserializeHelperCreature(ally.HelperCreature);
				if (inventorySlotItem != null && inventorySlotItem.Creature != null)
				{
					creatureData = inventorySlotItem.Creature.Form;
				}
			}
		}
		catch
		{
			creatureData = null;
		}
		if (creatureData != null)
		{
			BuildCreatureArt(creatureData);
		}
		if (SelectedGroup != null)
		{
			SelectedGroup.SetActive(value: false);
		}
	}

	public void OnEntryClicked()
	{
		if (mIsRecipient)
		{
			if (mOwner != null)
			{
				mOwner.PickRecipient(mRecipientId, mName);
			}
			return;
		}
		if (mIsBasket)
		{
			if (mOwner != null)
			{
				if (!string.IsNullOrEmpty(mWantCreatureId))
				{
					mOwner.ToggleWant(mOwnedUniqueId, mWantCreatureId, mWantStar, this);
				}
				else
				{
					mOwner.ToggleCreature(mOwnedUniqueId, null, this);
				}
			}
			return;
		}
		if (mIsWantSelect)
		{
			if (mOwner != null)
			{
				mOwner.ToggleWant(mOwnedUniqueId, mWantCreatureId, mWantStar, this);
			}
			return;
		}
		if (mIsOwnedSelect)
		{
			if (mOwner != null)
			{
				mOwner.ToggleCreature(mOwnedUniqueId, mOwnedCreature, this);
			}
			return;
		}
		TradePanelController ctrl = Singleton<TradePanelController>.Instance;
		if (ctrl == null)
		{
			return;
		}
		SimplePopupController instance = Singleton<SimplePopupController>.Instance;
		if (mIsOffer)
		{
			if (mWantMissing)
			{
				if (instance != null)
				{
					instance.ShowMessage("Trade Offer", "You no longer have the creature they asked for, so this trade can't be completed.");
				}
				return;
			}
			string text = ((!string.IsNullOrEmpty(mSellerName)) ? mSellerName : "This player");
			string body = text + " gives:\n" + mGiveSummary + "\nYou give:\n" + mWantSummary;
			if (instance != null)
			{
				instance.ShowPrompt("Trade Offer", body, delegate
				{
					ctrl.OnClickAcceptOffer(mListingId);
				}, delegate
				{
					ctrl.OnClickDeclineOffer(mListingId);
				}, "Accept", "Decline");
			}
			else
			{
				ctrl.OnClickAcceptOffer(mListingId);
			}
			return;
		}
		string body2 = "Buy " + (string.IsNullOrEmpty(mName) ? "this creature" : mName) + " for " + mPrice + " coins?";
		if (instance != null)
		{
			instance.ShowPrompt("Confirm Purchase", body2, delegate
			{
				ctrl.OnClickBuy(mListingId);
			}, null, "Buy", "Cancel");
		}
		else
		{
			ctrl.OnClickBuy(mListingId);
		}
	}

	public void OnClickBuy()
	{
		if (!(Singleton<TradePanelController>.Instance != null))
		{
			return;
		}
		if (mIsOffer)
		{
			if (!mWantMissing)
			{
				Singleton<TradePanelController>.Instance.OnClickAcceptOffer(mListingId);
			}
		}
		else
		{
			Singleton<TradePanelController>.Instance.OnClickBuy(mListingId);
		}
	}

	public int GetListingId()
	{
		return mListingId;
	}

	public string GetSellerId()
	{
		return mSellerId;
	}

	public void SetSelected(bool selected)
	{
		if (SelectedGroup != null)
		{
			SelectedGroup.SetActive(selected);
		}
	}

	private static string DescribeGive(Dictionary<string, object> give)
	{
		if (give == null)
		{
			return "nothing";
		}
		List<string> list = new List<string>();
		if (Get(give, "creatures") is List<object> list2)
		{
			foreach (object item in list2)
			{
				if (item is Dictionary<string, object> d)
				{
					string text = Get(d, "creature_id") as string;
					CreatureData creatureData = ((!string.IsNullOrEmpty(text) && CreatureDataManager.Instance != null) ? CreatureDataManager.Instance.GetData(text) : null);
					list.Add((creatureData != null) ? creatureData.Name : ((text != null) ? text : "?"));
				}
			}
		}
		int num = ToInt(Get(give, "coins"));
		if (num > 0)
		{
			list.Add(num + " coins");
		}
		if (list.Count <= 0)
		{
			return "nothing";
		}
		return string.Join(" + ", list.ToArray());
	}

	private string DescribeWant(Dictionary<string, object> want)
	{
		mWantMissing = false;
		if (want == null)
		{
			return "nothing";
		}
		List<string> list = new List<string>();
		if (Get(want, "unique_ids") is List<object> list2)
		{
			PlayerInfoScript instance = Singleton<PlayerInfoScript>.Instance;
			PlayerSaveData playerSaveData = ((instance != null) ? instance.SaveData : null);
			foreach (object item in list2)
			{
				int uniqueId = ToInt(item);
				InventorySlotItem inventorySlotItem = playerSaveData?.GetCreatureItem(uniqueId);
				if (inventorySlotItem != null && inventorySlotItem.Creature != null && inventorySlotItem.Creature.Form != null)
				{
					list.Add(inventorySlotItem.Creature.Form.Name);
					continue;
				}
				mWantMissing = true;
				list.Add("(creature you no longer have)");
			}
		}
		int num = ToInt(Get(want, "coins"));
		if (num > 0)
		{
			list.Add(num + " coins");
		}
		if (list.Count <= 0)
		{
			return "nothing";
		}
		return string.Join(" + ", list.ToArray());
	}

	private static object Get(Dictionary<string, object> d, string key)
	{
		if (!d.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
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

	public void OnClickDecline()
	{
		if (mIsOffer && Singleton<TradePanelController>.Instance != null)
		{
			Singleton<TradePanelController>.Instance.OnClickDeclineOffer(mListingId);
		}
	}
}
