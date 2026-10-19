using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SalePopupController : Singleton<SalePopupController>
{
	public enum ShowLocation
	{
		TownIntro,
		GachaScreen,
		StoreScreen
	}

	public GameObject MainPanel1;

	public GameObject MainPanel2;

	public GameObject ItemListEntry;

	public GameObject GrantedItemListEntry;

	public UITweenController ShowTween;

	public UITweenController HideTween;

	public UITweenController ShowGrantedTween;

	public UITweenController HideGrantedTween;

	public UITweenController GrantedTapToContinueTween;

	public UILabel SaleName;

	public UILabel SaleDesc;

	public UILabel Price;

	public UISprite CurrencyIcon;

	public UILabel PackTitleLabel;

	public UILabel BadgeLabel;

	public UIGrid ItemGrid;

	public UIScrollView ListScrollview;

	public UILabel TimeLeft;

	public UITexture BackgroundTexture;

	public UITexture BadgeTexture;

	public UIGrid GrantedItemGrid;

	public UIScrollView GrantedListScrollview;

	public float GrantRewardDelay;

	private SpecialSaleData mSale;

	private bool mWaitingForTap;

	private ShowLocation mShowLocation;

	private bool mDataLoaded;

	private static SpecialSaleData mGrantedSale;

	private static bool mWaitForPurchaseComplete;

	private static bool mWaitForCancelPurchase;

	public void Show(SpecialSaleData sale, ShowLocation showLocation)
	{
		mSale = sale;
		mShowLocation = showLocation;

		Singleton<BusyIconPanelController>.Instance.Show();

		mDataLoaded = false;

		ShowAfterDataFetched();
	}

	private void Update()
	{
		if (mWaitForPurchaseComplete)
		{
			mWaitForPurchaseComplete = false;
			ExecOnPurchaseComplete();
		}
		if (mWaitForCancelPurchase)
		{
			mWaitForCancelPurchase = false;
			Singleton<BusyIconPanelController>.Instance.Hide();
		}
	}


	private void OnError()
	{
		Singleton<BusyIconPanelController>.Instance.Hide();
		OnFullyFinished();
		OnClosed();
	}

	private void ShowAfterDataFetched()
	{
		if (mDataLoaded)
		{
			return;
		}
		mDataLoaded = true;
		bool flag = mSale.BadgeTexture != "UI/GeneralBundle/SaleArt/";
		BadgeTexture.gameObject.SetActive(flag);
		if (flag)
		{
			Singleton<SLOTResourceManager>.Instance.QueueResourceLoad(mSale.BadgeTexture, "GeneralBundle", delegate(Object loadedBadgeResouce)
			{
				BadgeTexture.UnloadTexture();
				BadgeTexture.mainTexture = loadedBadgeResouce as Texture;
			});
		}
		Singleton<SLOTResourceManager>.Instance.QueueResourceLoad(mSale.Texture, "GeneralBundle", delegate(Object loadedResouce)
		{
			Singleton<BusyIconPanelController>.Instance.Hide();
			ShowTween.Play();
			Singleton<SLOTAudioManager>.Instance.PlaySound("ui/SFX_SalePopupShow");
			BackgroundTexture.UnloadTexture();
			BackgroundTexture.mainTexture = loadedResouce as Texture;
			Singleton<PlayerInfoScript>.Instance.OnSaleShown(mSale);
			SaleName.text = mSale.Name;
			SaleDesc.text = mSale.Description;
			switch (mSale.PurchaseCurrency)
			{
				case SpecialSaleData.CurrencyType.HardCurrency:
					Price.text = mSale.CurrencyCost.ToString();
					CurrencyIcon.spriteName = "Icon_Currency_Hard";
					break;

				case SpecialSaleData.CurrencyType.PvPCurrency:
					Price.text = mSale.CurrencyCost.ToString();
					CurrencyIcon.spriteName = "Icon_Currency_PVPCurrency";
					break;
			}
			PackTitleLabel.text = mSale.PackTitle;
			BadgeLabel.text = mSale.BadgeText;
			uint num = TFUtils.ServerTime.UnixTimestamp();
			if (Singleton<PlayerInfoScript>.Instance.SaveData.SaleEndTime > num + 60)
			{
				uint totalSeconds = Singleton<PlayerInfoScript>.Instance.SaveData.SaleEndTime - num;
				TimeLeft.text = KFFLocalization.Get("!!SALE_ENDING_IN_X").Replace("<val1>", PlayerInfoScript.FormatTimeString((int)totalSeconds));
			}
			else
			{
				TimeLeft.text = KFFLocalization.Get("!!SALE_ENDING_NOW");
			}
			InventoryTile.ClearDelegates(false);
			foreach (GeneralReward item in mSale.Items)
			{
				SalePopupListEntry component = ItemGrid.transform.InstantiateAsChild(ItemListEntry).GetComponent<SalePopupListEntry>();
				component.gameObject.SetActive(true);
				item.PopulateUI(component.Label, component.Sprite, component.Texture, component.BackgroundSprite, component.TileNode);
			}
			ItemGrid.Reposition();
			ListScrollview.ResetPosition();
		});
	}

	public void OnClickPurchase()
	{
		PlayerInfoScript player =
			Singleton<PlayerInfoScript>.Instance;

		string currencyText = "";

		switch (mSale.PurchaseCurrency)
		{
			case SpecialSaleData.CurrencyType.HardCurrency:
				currencyText = "gems";
				break;

			case SpecialSaleData.CurrencyType.PvPCurrency:
				currencyText = "PvP currency";
				break;
		}

		Singleton<SimplePopupController>.Instance.ShowPurchasePrompt(
			$"Do you want to buy this offer for {mSale.CurrencyCost} {currencyText}?",
			"",
			mSale.CurrencyCost,
			ConfirmPurchase,
			CancelPurchase
		);
	}

	private void ConfirmPurchase()
	{
		var player = Singleton<PlayerInfoScript>.Instance;

		// 💰 currency switch
		switch (mSale.PurchaseCurrency)
		{
			case SpecialSaleData.CurrencyType.HardCurrency:
				player.SaveData.HardCurrency -= mSale.CurrencyCost;
				break;

			case SpecialSaleData.CurrencyType.PvPCurrency:
				player.SaveData.PvPCurrency -= mSale.CurrencyCost;
				break;
		}

		// 🎯 KRİTİK FIX: sale state update
		var item = player.GetSale(mSale);
		item.OnPurchased();

		// 💾 SAVE (EN ÖNEMLİ SATIR)
		player.Save();

		CompleteCurrencyPurchase();

		// 🎯 UI switch
		switch (mSale.Mode)
		{
			case SpecialSaleData.SaleMode.SpecialSale01:
				StoreScreenController.Instance.OnSale01Purchased(mSale);
				break;

			case SpecialSaleData.SaleMode.SpecialSale02:
				StoreScreenController.Instance.OnSale02Purchased(mSale);
				break;
			
			case SpecialSaleData.SaleMode.SpecialSale03:
				StoreScreenController.Instance.OnSale03Purchased(mSale);
				break;
		}
	}

	private void CancelPurchase()
	{
		// hiçbir şey yapma
	}

	private void CompleteCurrencyPurchase()
	{
		Singleton<PlayerInfoScript>.Instance.OnSpecialSalePurchased(mSale);

		StoreScreenController.GrantSaleProduct(
			mSale,
			true
		);

		HideTween.Play();
	}
	private void ReloadFromServer()
	{
		OnFullyFinished();
		SessionManager.Instance.theSession.WebFileServer.DeleteETagFile();
		SessionManager.Instance.theSession.ReloadGame();
	}

	private void PurchaseCallback(PurchaseManager.ProductPurchaseResult result)
	{
		if (result == PurchaseManager.ProductPurchaseResult.Success)
		{
			OnPurchaseComplete();
		}
		else
		{
			CancelPurchese();
		}
	}

	public static void CancelPurchese()
	{
		mWaitForCancelPurchase = true;
	}

	private void OnPurchaseComplete()
	{
		mWaitForPurchaseComplete = true;
	}

	private void ExecOnPurchaseComplete()
	{
		Singleton<PlayerInfoScript>.Instance.OnSpecialSalePurchased(mSale);
		StoreScreenController.GrantSaleProduct(
			mSale,
			true
		);
		HideTween.Play();
	}

	public void OnClickClose()
	{
		HideTween.Play();
		OnFullyFinished();
	}

	public void OnClosed()
	{
		Singleton<BusyIconPanelController>.Instance.Hide();
		ItemGrid.transform.DestroyAllChildren();
		BackgroundTexture.UnloadTexture();
		BadgeTexture.UnloadTexture();
	}

	public static void SetGrantedSale(SpecialSaleData sale, bool showResultsNow)
	{
		if (showResultsNow)
		{
			Singleton<SalePopupController>.Instance.StartCoroutine(Singleton<SalePopupController>.Instance.ShowGrantedSale(sale));
		}
		else
		{
			mGrantedSale = sale;
		}
	}

	public bool CheckGrantedSale()
	{
		if (mGrantedSale != null)
		{
			StartCoroutine(ShowGrantedSale(mGrantedSale));
			mGrantedSale = null;
			return true;
		}
		return false;
	}

	private IEnumerator ShowGrantedSale(SpecialSaleData sale)
	{
		UICamera.LockInput();
		yield return StartCoroutine(ShowGrantedTween.PlayAsCoroutine());
		List<SalePopupListEntry> entries = new List<SalePopupListEntry>();
		List<GeneralReward> gachaRewards = new List<GeneralReward>();
		foreach (GeneralReward item in sale.Items)
		{
			if (item.RewardType == GeneralReward.TypeEnum.GachaTable)
			{
				gachaRewards.Add(item);
				continue;
			}
			SalePopupListEntry newEntry = GrantedItemGrid.transform.InstantiateAsChild(GrantedItemListEntry).GetComponent<SalePopupListEntry>();
			newEntry.gameObject.SetActive(true);
			item.PopulateUI(newEntry.Label, newEntry.Sprite, newEntry.Texture, newEntry.BackgroundSprite, newEntry.TileNode);
			entries.Add(newEntry);
		}
		GrantedItemGrid.Reposition();
		GrantedListScrollview.ResetPosition();
		foreach (SalePopupListEntry entry2 in entries)
		{
			entry2.gameObject.SetActive(false);
		}
		yield return new WaitForSeconds(0.3f);
		foreach (SalePopupListEntry entry in entries)
		{
			entry.gameObject.SetActive(true);
			entry.GrantTween.Play();
			Singleton<SLOTAudioManager>.Instance.StopSound("ui/SFX_SalePopupItem");
			Singleton<SLOTAudioManager>.Instance.PlaySound("ui/SFX_SalePopupItem");
			yield return new WaitForSeconds(GrantRewardDelay);
		}
		UICamera.UnlockInput();
		mWaitingForTap = true;
		GrantedTapToContinueTween.Play();
		while (mWaitingForTap)
		{
			yield return null;
		}
		HideGrantedTween.Play();
		if (gachaRewards.Count > 0)
		{
			if (mShowLocation == ShowLocation.StoreScreen)
			{
				Singleton<StoreScreenController>.Instance.OnClickClose();
			}
			if (gachaRewards.Count > 1)
			{
				List<InventorySlotItem> creatures = new List<InventorySlotItem>();
				foreach (GeneralReward spin in gachaRewards)
				{
					creatures.Add(spin.GrantedInventoryItem);
				}
				Singleton<GachaMultiResultController>.Instance.ShowMultiEggPanel(creatures);
				while (Singleton<GachaMultiResultController>.Instance.Showing)
				{
					yield return null;
				}
			}
			else
			{
				Singleton<GachaOpenSequencer>.Instance.ShowGachaSequence(gachaRewards[0].GrantedInventoryItem);
				while (Singleton<GachaOpenSequencer>.Instance.Showing)
				{
					yield return null;
				}
			}
		}
		OnFullyFinished();
	}

	public void OnGrantedScreenClosed()
	{
		GrantedItemGrid.transform.DestroyAllChildren();
		Singleton<BusyIconPanelController>.Instance.Hide();
	}

	private void OnFullyFinished()
	{
		if (!Singleton<TownController>.Instance.IsIntroDone())
		{
			Singleton<TownController>.Instance.AdvanceIntroState();
		}
		Singleton<TutorialController>.Instance.AdvanceIfOnState("BA_InSale");
	}

	public void OnGrantedScreenTapped()
	{
		mWaitingForTap = false;
	}
}
