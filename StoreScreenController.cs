using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

public class StoreScreenController : Singleton<StoreScreenController>
{
	public enum StoreKPITracking
	{
		BuyHeroes = 0,
		ReplenishStamina = 1,
		IncreaseInventory = 2,
		ExpandAllyList = 3,
		BuyProduct = 4
	}

	public class FoundProductData
	{
		public CurrencyPackageData PackageData;
	}

	private enum NextAction
	{
		NONE = 0,
		WAITING = 1,
		PROCEED = 2,
		ERROR = 3,
		RETRY = 4
	}

	private const string IntroPackageName = "DW_Special_Sale";

	public int BlankLeaderSlots;

	public float HardCurrencyTickTime;

	public GameObject MainPanel;

	public GameObject StoreEntryPrefab;

	public GameObject HeroPrefab;

	public GameObject[] StoreEntryVFXPrefabs = new GameObject[6];

	public UITweenController ShowTween;

	public UITweenController HideTween;

	public UITweenController HardCurrencyGainTween;

	public UILabel TitleLabel;

	public UILabel StaminaRefillCost;

	public UILabel InventoryIncreaseCost;

	public UILabel InventoryIncreaseAmount;

	public UILabel AllyIncreaseCost;

	public UILabel AllyIncreaseAmount;

	public UILabel HardCurrencyAvailable;

	public UILabel CustomizationCurrencyAvailable;

	public UIStreamingGrid HeroGrid;

	private UIStreamingGridDataSource<LeaderData> mHeroGridDataSource = new UIStreamingGridDataSource<LeaderData>();

	public UILabel HeroName;

	public UILabel HeroAge;

	public UILabel HeroHeight;

	public UILabel HeroWeight;

	public UILabel HeroSpecies;

	public UITexture HeroImage;

	public GameObject HeroCardNodeParent;

	private List<GameObject> HeroCardNodes = new List<GameObject>();

	public UILabel HeroCost;

	public GameObject HeroPurchaseButton;

	public UIStreamingGrid StoreGrid;

	private UIStreamingGridDataSource<FoundProductData> mStoreGridDataSource = new UIStreamingGridDataSource<FoundProductData>();

	public GameObject HeroButtonLock;

	public Collider HeroButtonCollider;

	public GameObject CustomizationButtonLock;

	public Collider CustomizationButtonCollider;

	public UIStreamingGrid SkinGrid;

	private UIStreamingGridDataSource<LeaderData> mSkinGridDataSource = new UIStreamingGridDataSource<LeaderData>();

	public UIGrid CustomizationPurchaseGrid;

	public UILabel SkinName;

	public UITexture SkinImage;

	public UILabel SkinCost;

	public UILabel SkinButtonLabel;

	public GameObject BuySkinButton;

	public GameObject SkinEquippedParent;

	public UIGrid TabsGrid;

	public UIToggle HeroTabButton;

	public UIToggle HardCurrencyTabButton;

	public UIToggle CustomizationTabButton;

	public UIToggle ActionCardTabButton;

	public UIToggle CreatureTabButton;

	public UIToggle CardBackTabButton;

	public UIToggle EvoMatTabButton;

	private GameObject StoreBuyItemPrefab;

	public GameObject SaleButton;

	public GameObject SaleButton2;

	public GameObject SaleButton3;

	public UILabel HeroAvailableLabel;

	public UILabel HeroLevelRequirementLabel;

	[Header("Loc Strings for UIToggle Titles")]
	public string SaleTitleText = "!!SALE";

	public string GemsTitleText = "!!CURRENCY_HARD";

	public string ExtrasTitleText = "!!EXTRAS";

	public string HeroesTitleText = "!!HEROES";

	public string CustomizeTitleText = "!!CUSTOMIZE";

	public string ActionCardTitleText = "!!SPELL";

	public string CreaturesTitleText = "!!CREATURES";

	public string CardBackTitleText = "!!CARDBACK";

	public string EvoMatTitleText = "Ingredient";

	private static string mProductIdBeingPurchased;

	private List<FoundProductData> mFetchedList;

	private List<FoundProductData> mHiddenFetchedList;

	private static bool mSubscribedToEvent;

	private LeaderData mHighlightedLeader;

	private List<CardPrefabScript> mSpawnedCards = new List<CardPrefabScript>();

	private LeaderData mHighlightedSkin;

	private float mDisplayedHardCurrency;

	private float mHardCurrencyTickRate;

	private List<LeaderData> mHeroList;

	private List<LeaderData> mSkinList;

	private bool mWaitForUpsight;

	private float mUpsightWaitTimer = -1f;

	private static bool mWaitForGrantProduct;

	private static string mGrantProductId = string.Empty;

	private static bool mSkipHardCurrencyTick;

	private static bool mWaitForGrantSaleProduct;

	private static SpecialSaleData mSaleData;

	private static bool mShowResultsNow;

	private static bool mWaitForCancelPurchase;

	private bool mWaitForUserAction;

	private NextAction mUserActionProceed;

	private PlayerSaveData.ProceedNextStep mNextFunction;

	private List<string> mActiveActionCardIds = new List<string>();

	private List<string> mSpecialActionCardIds = new List<string>();

	private List<string> mActiveEvoMaterialIds = new List<string>();

	private List<string> mSpecialEvoMaterialIds = new List<string>();

	private Dictionary<string, int> mActionCardBuyCounts = new Dictionary<string, int>();

	private Dictionary<string, int> mEvoMaterialBuyCounts = new Dictionary<string, int>();

	private bool mMonthlyStoreLoaded;

	private bool mMonthlyStorePurchasePending;

	private bool mMonthlyStoreLoading;

	private StoreBuyCreatureData mPendingCreaturePurchase;

	private StoreBuyActionCardData mPendingActionCardPurchase;

	private StoreBuyEvoMaterialData mPendingEvoMaterialPurchase;

	private StoreBuyCardBackData mPendingCardBackPurchase;

	private int mPendingStoreBuyCost;

	[SerializeField]
	private UIGrid CreatureStoreGrid;

	[SerializeField]
	private UIGrid ActionCardStoreGrid;

	[SerializeField]
	private GameObject StoreBuyCreaturePrefab;

	[SerializeField]
	private GameObject StoreBuyActionCardPrefab;

	[SerializeField]
	private UIGrid EvoMaterialStoreGrid;

	[SerializeField]
	private GameObject StoreBuyEvoMaterialPrefab;

	[SerializeField]
	public UIGrid CardBackStoreGrid;

	[SerializeField]
	public GameObject StoreBuyCardBackPrefab;

	private static HashSet<string> sStoreHidden;

	private static string sStoreHiddenRaw;

	private readonly HashSet<UIToggle> mClosedByServer = new HashSet<UIToggle>();

	public bool OpenedFromTown { get; set; }

	public bool OpenToHardCurrency { get; set; }

	public bool OpenToCustomization { get; set; }

	public bool OpenToActionCard { get; set; }

	public bool OpenToCreature { get; set; }

	public bool OpenToEvoMat { get; set; }

	public bool OpenToCardBack { get; set; }

	public bool OpenToHeroes { get; set; }

	public bool isProcessingPurchase { get; set; }

	public void Awake()
	{
		isProcessingPurchase = false;
		if (!mSubscribedToEvent)
		{
			Singleton<PurchaseManager>.Instance.ProcessOldPurchases();
			mSubscribedToEvent = true;
		}
		for (int i = 0; i < 5; i++)
		{
			HeroCardNodes.Add(HeroCardNodeParent.FindInChildren("ActionCardSpawn_0" + (i + 1)));
		}
		Singleton<PurchaseManager>.Instance.ProcessOldPurchases();
	}

	public void Populate()
	{
		OpenedFromTown = true;
		OpenToHeroes = true;
		HardCurrencyTabButton.gameObject.SetActive(value: false);
		ApplyStoreSwitches();
		mDisplayedHardCurrency = Singleton<PlayerInfoScript>.Instance.SaveData.HardCurrency;
		mHardCurrencyTickRate = 9999f;
		StaminaRefillCost.text = MiscParams.StaminaRefillCost.ToString();
		InventoryIncreaseCost.text = MiscParams.InventorySpacePurchaseCost.ToString();
		InventoryIncreaseAmount.text = "+" + MiscParams.InventorySpacePerPurchase;
		AllyIncreaseCost.text = MiscParams.AllyBoxPurchaseCost.ToString();
		AllyIncreaseAmount.text = "+" + MiscParams.AllyBoxPerPurchase;
		PopulateHeroList();
		PopulateSkinList();
		bool flag = Singleton<TutorialController>.Instance.IsBlockActive("BuyHero");
		LeaderData leaderData = null;
		foreach (LeaderData mHero in mHeroList)
		{
			if (flag)
			{
				if (mHero == MiscParams.TutorialHero)
				{
					leaderData = mHero;
					break;
				}
				continue;
			}
			if (leaderData == null)
			{
				leaderData = mHero;
			}
			if (!Singleton<PlayerInfoScript>.Instance.IsLeaderUnlocked(mHero))
			{
				leaderData = mHero;
				break;
			}
		}
		if (leaderData == null)
		{
			leaderData = mHeroList[0];
		}
		PopulateLeader(leaderData);
		bool flag2 = Singleton<PlayerInfoScript>.Instance.CanBuyLeaders();
		HeroButtonLock.SetActive(!flag2);
		HeroButtonCollider.enabled = flag2;
		bool flag3 = flag2 && mSkinList.Count > 0;
		CustomizationButtonLock.SetActive(!flag3);
		CustomizationButtonCollider.enabled = flag3;
		if (flag3)
		{
			if (mHighlightedSkin != null)
			{
				PopulateSkin(mHighlightedSkin);
			}
			else
			{
				PopulateSkin(SkinGrid.transform.GetChild(0).GetComponent<StoreHeroPrefab>().Leader);
			}
		}
		if (DetachedSingleton<SceneFlowManager>.Instance.InFrontEnd())
		{
			Singleton<VersionChecker>.Instance.CheckVersion(OnVersionCheckComplete);
		}
		else
		{
			OnVersionCheckComplete();
		}
	}

	private void OnVersionCheckComplete()
	{
		Singleton<BusyIconPanelController>.Instance.Show();
		PopulateDummyData();
	}

	public void SetTitleLabel(string inLabelText)
	{
		if (TitleLabel != null)
		{
			TitleLabel.text = KFFLocalization.Get("!!BUILDING_STORE") + ": " + KFFLocalization.Get(inLabelText);
		}
	}

	private void PopulateHeroList()
	{
		mHeroList = new List<LeaderData>(LeaderDataManager.Instance.GetDatabase().FindAll((LeaderData m) => m.ShowInSaleList()));
		List<LeaderData> list = mHeroList.FindAll((LeaderData m) => !StoreHidden(m.ID) || m == MiscParams.TutorialHero);
		if (list.Count > 0)
		{
			mHeroList = list;
		}
		foreach (LeaderData mHero in mHeroList)
		{
			Debug.Log("Leader in Hero List: " + mHero.Name + ", ID: " + mHero.ID);
		}
		mHeroGridDataSource.Init(HeroGrid, HeroPrefab, mHeroList);
	}

	private void PopulateSkinList()
	{
		mSkinList = new List<LeaderData>();
		foreach (LeaderData mHero in mHeroList)
		{
			if (mHero == null)
			{
				continue;
			}
			LeaderItem leaderItem = Singleton<PlayerInfoScript>.Instance.GetLeaderItem(mHero.ID);
			foreach (LeaderData alternateSkin in mHero.AlternateSkins)
			{
				bool flag = leaderItem?.OwnedSkins.Contains(alternateSkin) ?? false;
				if ((alternateSkin.BuyableSkin && !StoreHidden(alternateSkin.ID)) || flag)
				{
					mSkinList.Add(alternateSkin);
				}
			}
		}
		mSkinGridDataSource.Init(SkinGrid, HeroPrefab, mSkinList);
	}

	private StoreHeroPrefab FindHeroTile(LeaderData leader)
	{
		for (int i = 0; i < HeroGrid.transform.childCount; i++)
		{
			StoreHeroPrefab component = HeroGrid.transform.GetChild(i).GetComponent<StoreHeroPrefab>();
			if (component.Leader == leader)
			{
				return component;
			}
		}
		return null;
	}

	public void RefreshSaleButtons()
	{
		_ = Singleton<PlayerInfoScript>.Instance;
		SpecialSaleData saleToDisplay = GetSaleToDisplay(SpecialSaleData.SaleMode.SpecialSale01);
		SaleButton.SetActive(saleToDisplay != null);
		SpecialSaleData saleToDisplay2 = GetSaleToDisplay(SpecialSaleData.SaleMode.SpecialSale02);
		SaleButton2.SetActive(saleToDisplay2 != null);
		SpecialSaleData saleToDisplay3 = GetSaleToDisplay(SpecialSaleData.SaleMode.SpecialSale03);
		SaleButton3.SetActive(saleToDisplay3 != null);
		TabsGrid.Reposition();
	}

	private void Update()
	{
		RefreshCurrency();
		if (mWaitForUpsight)
		{
			bool flag = true;
			mUpsightWaitTimer -= Time.deltaTime;
			if (mUpsightWaitTimer <= 0f)
			{
				mUpsightWaitTimer = -1f;
				flag = true;
			}
			if (flag)
			{
				mWaitForUpsight = false;
				Singleton<BusyIconPanelController>.Instance.Hide();
				if (DetachedSingleton<SceneFlowManager>.Instance.InFrontEnd())
				{
					if (GetSaleToDisplay(SpecialSaleData.SaleMode.SpecialSale01) != null)
					{
						SaleButton.SetActive(value: false);
						SaleButton2.SetActive(value: false);
						SaleButton3.SetActive(value: false);
						TabsGrid.Reposition();
					}
					else
					{
						AdvanceTutorialPastSale();
					}
				}
			}
		}
		if (mWaitForUserAction)
		{
			if (mUserActionProceed == NextAction.PROCEED)
			{
				Singleton<BusyIconPanelController>.Instance.Hide();
				mWaitForUserAction = false;
				mUserActionProceed = NextAction.NONE;
				if (mNextFunction != null)
				{
					mNextFunction();
				}
				mUserActionProceed = NextAction.NONE;
			}
			if (mUserActionProceed == NextAction.ERROR)
			{
				mWaitForUserAction = false;
				mUserActionProceed = NextAction.NONE;
				Singleton<BusyIconPanelController>.Instance.Hide();
				Singleton<SimplePopupController>.Instance.ShowMessage(string.Empty, KFFLocalization.Get("!!SERVER_ERROR_MESSAGE"), OnCloseServerAccessErrorPopup);
			}
		}
		if (mWaitForGrantProduct)
		{
			mWaitForGrantProduct = false;
			ExecGrantProduct(mGrantProductId, mSkipHardCurrencyTick);
		}
		if (mWaitForGrantSaleProduct)
		{
			mWaitForGrantSaleProduct = false;
			ExecGrantSaleProduct(mSaleData, mShowResultsNow);
		}
		if (mWaitForCancelPurchase)
		{
			mWaitForCancelPurchase = false;
			Singleton<BusyIconPanelController>.Instance.Hide();
		}
	}

	private void OnCloseServerAccessErrorPopup()
	{
	}

	private void AdvanceTutorialPastSale()
	{
		Singleton<TutorialController>.Instance.AdvanceIfOnState("BA_ExplainBank");
		Singleton<TutorialController>.Instance.AdvanceIfOnState("BA_InSale");
	}

	public static bool StoreHidden(string id)
	{
		string text = MiscParams.GetString("StoreHidden", string.Empty) ?? string.Empty;
		if (sStoreHidden == null || text != sStoreHiddenRaw)
		{
			sStoreHiddenRaw = text;
			sStoreHidden = new HashSet<string>();
			string[] array = text.Split(',');
			foreach (string text2 in array)
			{
				if (text2.Trim().Length > 0)
				{
					sStoreHidden.Add(text2.Trim());
				}
			}
		}
		if (!string.IsNullOrEmpty(id))
		{
			return sStoreHidden.Contains(id);
		}
		return false;
	}

	private void ApplyStoreSwitches()
	{
		bool flag = Singleton<TutorialController>.Instance != null && Singleton<TutorialController>.Instance.IsBlockActive("BuyHero");
		SetTabOpen(HeroTabButton, flag || LiveChannel.IsOn("Store_Heroes"));
		SetTabOpen(CustomizationTabButton, LiveChannel.IsOn("Store_Skins"));
		SetTabOpen(ActionCardTabButton, LiveChannel.IsOn("Store_Cards"));
		SetTabOpen(CreatureTabButton, LiveChannel.IsOn("Store_Creatures"));
		SetTabOpen(EvoMatTabButton, LiveChannel.IsOn("Store_EvoMats"));
		SetTabOpen(CardBackTabButton, LiveChannel.IsOn("Store_CardBacks"));
		if (!TabOpen(HeroTabButton) && !TabOpen(CustomizationTabButton) && !TabOpen(ActionCardTabButton) && !TabOpen(CreatureTabButton) && !TabOpen(EvoMatTabButton) && !TabOpen(CardBackTabButton))
		{
			SetTabOpen(HeroTabButton, open: true);
		}
		if (TabsGrid != null)
		{
			LiftTabBar();
			TabsGrid.Reposition();
		}
	}

	private void LiftTabBar()
	{
		if (!(TabsGrid.GetComponent<UIPanel>() != null))
		{
			UIPanel uIPanel = UIPanel.Find(TabsGrid.transform.parent);
			TabsGrid.gameObject.AddComponent<UIPanel>().depth = ((!(uIPanel != null)) ? 1 : (uIPanel.depth + 1));
			NGUITools.MarkParentAsChanged(TabsGrid.gameObject);
		}
	}

	private void SetTabOpen(UIToggle tab, bool open)
	{
		if (!(tab == null))
		{
			if (!open && tab.gameObject.activeSelf)
			{
				tab.gameObject.SetActive(value: false);
				mClosedByServer.Add(tab);
			}
			else if (open && mClosedByServer.Remove(tab))
			{
				tab.gameObject.SetActive(value: true);
			}
		}
	}

	private static bool TabOpen(UIToggle tab)
	{
		if (tab != null)
		{
			return tab.gameObject.activeSelf;
		}
		return false;
	}

	private void RedirectFromClosedTab()
	{
		if ((OpenToHeroes && !TabOpen(HeroTabButton)) || (OpenToCustomization && !TabOpen(CustomizationTabButton)) || (OpenToActionCard && !TabOpen(ActionCardTabButton)) || (OpenToCreature && !TabOpen(CreatureTabButton)) || (OpenToEvoMat && !TabOpen(EvoMatTabButton)) || (OpenToCardBack && !TabOpen(CardBackTabButton)) || (!OpenToHeroes && !OpenToCustomization && !OpenToActionCard && !OpenToCreature && !OpenToEvoMat && !OpenToCardBack && !TabOpen(HeroTabButton)))
		{
			bool flag = (OpenToCardBack = false);
			bool flag3 = (OpenToEvoMat = flag);
			bool flag5 = (OpenToCreature = flag3);
			bool flag7 = (OpenToActionCard = flag5);
			bool openToHeroes = (OpenToCustomization = flag7);
			OpenToHeroes = openToHeroes;
			if (TabOpen(HeroTabButton))
			{
				OpenToHeroes = true;
			}
			else if (TabOpen(CustomizationTabButton))
			{
				OpenToCustomization = true;
			}
			else if (TabOpen(ActionCardTabButton))
			{
				OpenToActionCard = true;
			}
			else if (TabOpen(CreatureTabButton))
			{
				OpenToCreature = true;
			}
			else if (TabOpen(EvoMatTabButton))
			{
				OpenToEvoMat = true;
			}
			else if (TabOpen(CardBackTabButton))
			{
				OpenToCardBack = true;
			}
		}
	}

	private IEnumerator JumpToTab()
	{
		yield return new WaitForEndOfFrame();
		RedirectFromClosedTab();
		if (OpenToHeroes)
		{
			OpenToHeroes = false;
			HeroTabButton.gameObject.SendMessage("OnClick");
		}
		else if (OpenToCustomization)
		{
			OpenToCustomization = false;
			CustomizationTabButton.gameObject.SendMessage("OnClick");
		}
		else if (OpenToActionCard)
		{
			OpenToActionCard = false;
			SetTitleLabel(ActionCardTitleText);
			ActionCardTabButton.gameObject.SendMessage("OnClick");
		}
		else if (OpenToCreature)
		{
			OpenToCreature = false;
			SetTitleLabel(CreaturesTitleText);
			CreatureTabButton.gameObject.SendMessage("OnClick");
		}
		else if (OpenToEvoMat)
		{
			OpenToEvoMat = false;
			SetTitleLabel(EvoMatTitleText);
			EvoMatTabButton.gameObject.SendMessage("OnClick");
		}
		else if (OpenToCardBack)
		{
			OpenToCardBack = false;
			SetTitleLabel(CardBackTitleText);
			CardBackTabButton.gameObject.SendMessage("OnClick");
		}
		OpenToHardCurrency = false;
	}

	private void RetryConnection()
	{
		Singleton<BusyIconPanelController>.Instance.Show();
		PopulateDummyData();
	}

	private void CancelConnection()
	{
		if (OpenedFromTown)
		{
			Singleton<TownHudController>.Instance.ReturnToTownView();
		}
		if (Singleton<TutorialController>.Instance.IsStateActive("BA_TapBank"))
		{
			for (int i = 0; i < 3; i++)
			{
				Singleton<TutorialController>.Instance.AdvanceTutorialState();
			}
		}
		else if (Singleton<TutorialController>.Instance.IsStateActive("HR_TapStore"))
		{
			if (!Singleton<PlayerInfoScript>.Instance.SaveData.Leaders.Contains((LeaderItem m) => m.Form == MiscParams.TutorialHero))
			{
				Singleton<PlayerInfoScript>.Instance.SaveData.Leaders.Add(new LeaderItem(MiscParams.TutorialHero));
			}
			for (int num = 0; num < 6; num++)
			{
				Singleton<TutorialController>.Instance.AdvanceTutorialState();
			}
		}
	}

	public void ShowIntroSalePurchasePrompt()
	{
		mUpsightWaitTimer = 5f;
		mWaitForUpsight = true;
		Singleton<BusyIconPanelController>.Instance.Show();
		UpsightRequester.RequestContent("store_menu");
	}

	private void ReloadFromServer()
	{
		SessionManager.Instance.theSession.WebFileServer.DeleteETagFile();
		SessionManager.Instance.theSession.ReloadGame();
	}

	public void RefreshCurrency()
	{
		if (HardCurrencyAvailable.gameObject.activeInHierarchy)
		{
			int hardCurrency = Singleton<PlayerInfoScript>.Instance.SaveData.HardCurrency;
			if ((float)hardCurrency > mDisplayedHardCurrency)
			{
				mDisplayedHardCurrency = mDisplayedHardCurrency.TickTowards(hardCurrency, mHardCurrencyTickRate);
			}
			else
			{
				mDisplayedHardCurrency = hardCurrency;
			}
			HardCurrencyAvailable.text = CurrencyUI.Format((int)(mDisplayedHardCurrency + 0.001f));
			CustomizationCurrencyAvailable.text = CurrencyUI.Format(Singleton<PlayerInfoScript>.Instance.SaveData.SoftCurrency);
		}
	}

	public void TriggerHardCurrencyGainTick()
	{
		Singleton<StoreScreenController>.Instance.StartHardCurrencyTick();
	}

	private void StartHardCurrencyTick()
	{
		float num = (float)Singleton<PlayerInfoScript>.Instance.SaveData.HardCurrency - mDisplayedHardCurrency;
		if (num > 0f)
		{
			HardCurrencyGainTween.Play();
			if (num > 2f)
			{
				mHardCurrencyTickRate = num / HardCurrencyTickTime;
			}
			else
			{
				mDisplayedHardCurrency = Singleton<PlayerInfoScript>.Instance.SaveData.HardCurrency;
			}
		}
		else
		{
			mDisplayedHardCurrency = Singleton<PlayerInfoScript>.Instance.SaveData.HardCurrency;
		}
	}

	public void Unload()
	{
		mHeroList.Clear();
		mSkinList.Clear();
		mStoreGridDataSource.Clear();
		mHeroGridDataSource.Clear();
		mSkinGridDataSource.Clear();
		HeroImage.UnloadTexture();
		SkinImage.UnloadTexture();
		CustomizationPurchaseGrid.transform.DestroyAllChildren();
		UnloadCards();
		if (OpenedFromTown)
		{
			Singleton<TownHudController>.Instance.ReturnToTownView();
		}
	}

	private void UnloadCards()
	{
		foreach (CardPrefabScript mSpawnedCard in mSpawnedCards)
		{
			NGUITools.Destroy(mSpawnedCard.gameObject);
		}
		mSpawnedCards.Clear();
	}

	public static void GrantOldProduct(string productId)
	{
		SpecialSaleData specialSaleData = SpecialSaleDataManager.Instance.GetDatabase().Find((SpecialSaleData m) => m.ID == productId);
		if (specialSaleData != null)
		{
			GrantSaleProduct(specialSaleData);
		}
		else
		{
			GrantProduct(productId);
		}
	}

	public static void GrantSaleProduct(SpecialSaleData sale, bool showResultsNow = false)
	{
		mSaleData = sale;
		mShowResultsNow = showResultsNow;
		mWaitForGrantSaleProduct = true;
	}

	public static void CancelPurchese()
	{
		mWaitForCancelPurchase = true;
	}

	public static void GrantProduct(string productId, bool skipHardCurrencyTick = false)
	{
		mGrantProductId = productId;
		mSkipHardCurrencyTick = skipHardCurrencyTick;
		mWaitForGrantProduct = true;
	}

	private static void ExecGrantProduct(string productId, bool skipHardCurrencyTick)
	{
		CurrencyPackageData data = CurrencyPackageDataManager.Instance.GetData(productId);
		if (data == null)
		{
			Debug.LogError("ExecGrantProduct: Missing productId = " + productId);
			Singleton<BusyIconPanelController>.Instance.Hide();
			return;
		}
		PlayerSaveData saveData = Singleton<PlayerInfoScript>.Instance.SaveData;
		_ = mProductIdBeingPurchased;
		mProductIdBeingPurchased = null;
		int getLastHandle = Singleton<PurchaseManager>.Instance.getLastHandle;
		string price = ((data.SocialCurrency > 0) ? "PvPCurrency" : "HardCurrency");
		Singleton<PlayerInfoScript>.Instance.AddHardCurrency2(data.PaidHardCurrency, data.FreeHardCurrency, productId, getLastHandle, price);
		saveData.PvPCurrency += data.SocialCurrency;
		saveData.SoftCurrency += data.SoftCurrency;
		saveData.EvolutionShards += data.EvolutionShards;
		if (!skipHardCurrencyTick)
		{
			Singleton<StoreScreenController>.Instance.TriggerHardCurrencyGainTick();
		}
		Singleton<PlayerInfoScript>.Instance.Save();
		Singleton<BusyIconPanelController>.Instance.Hide();
	}

	public static void ExecGrantSaleProduct(SpecialSaleData sale, bool showResultsNow = false)
	{
		GeneralReward.GrantAll(sale.Items, "sale", sale.ID);
	}

	private void PopulateDummyData()
	{
		Debug.Log("========== PopulateDummyData START ==========");
		mFetchedList = new List<FoundProductData>();
		foreach (CurrencyPackageData item in CurrencyPackageDataManager.Instance.GetDatabase())
		{
			FoundProductData foundProductData = new FoundProductData();
			foundProductData.PackageData = item;
			if (item.ShowInStore && !StoreHidden(item.ID))
			{
				mFetchedList.Add(foundProductData);
			}
		}
		ShowTween.Play();
		StartCoroutine(JumpToTab());
		mStoreGridDataSource.Init(StoreGrid, StoreEntryPrefab, mFetchedList);
		Debug.Log("Calling PopulateCreatureStore...");
		PopulateCreatureStore();
		PopulateCardBackStore();
		Debug.Log("Calling LoadMonthlyStore...");
		LoadMonthlyStore();
		Debug.Log("========== PopulateDummyData END ==========");
		Singleton<BusyIconPanelController>.Instance.Hide();
	}

	private void LoadMonthlyStore()
	{
		if (mMonthlyStoreLoading)
		{
			Debug.LogWarning("[Monthly Store] LoadMonthlyStore ignored - already loading.");
			return;
		}
		if (SessionManager.Instance == null)
		{
			Debug.LogError("[Store] SessionManager.Instance is NULL!");
			return;
		}
		if (SessionManager.Instance.theSession == null)
		{
			Debug.LogError("[Store] SessionManager.theSession is NULL!");
			return;
		}
		SQServer server = SessionManager.Instance.theSession.Server;
		if (server == null)
		{
			Debug.LogError("[Store] Session.Server is NULL!");
			return;
		}
		string playerID = SessionManager.Instance.PlayerID;
		if (string.IsNullOrEmpty(playerID))
		{
			Debug.LogError("[Store] PlayerID is NULL or EMPTY!");
			return;
		}
		mMonthlyStoreLoading = true;
		Debug.Log("[Monthly Store] ===============================");
		Debug.Log("[Monthly Store] LOAD START");
		Debug.Log("[Monthly Store] PlayerID: " + playerID);
		server.GetMonthlyStore(playerID, OnMonthlyStoreReceived);
	}

	private void OnMonthlyStoreReceived(Dictionary<string, object> response, HttpStatusCode status)
	{
		mMonthlyStoreLoading = false;
		Debug.Log("[Monthly Store] LOAD RESPONSE RECEIVED: " + status);
		if (response == null)
		{
			Debug.LogError("[Store] Monthly store response is NULL.");
			return;
		}
		if (response.ContainsKey("success") && response["success"] is bool && !(bool)response["success"])
		{
			Debug.LogError("[Store] Monthly store request failed: " + (response.ContainsKey("error") ? response["error"].ToString() : "Unknown error"));
			return;
		}
		mActiveActionCardIds = ParseStringList(response, "action_cards");
		mSpecialActionCardIds = ParseStringList(response, "action_card_special");
		mActiveEvoMaterialIds = ParseStringList(response, "evo_materials");
		mSpecialEvoMaterialIds = ParseStringList(response, "evo_special");
		Debug.Log("[Store] Monthly Action Cards: " + string.Join(", ", mActiveActionCardIds.ToArray()));
		Debug.Log("[Store] Special Action Cards: " + string.Join(", ", mSpecialActionCardIds.ToArray()));
		Debug.Log("[Store] Monthly Evo Materials: " + string.Join(", ", mActiveEvoMaterialIds.ToArray()));
		Debug.Log("[Store] Special Evo Materials: " + string.Join(", ", mSpecialEvoMaterialIds.ToArray()));
		mActionCardBuyCounts = ParseBuyCountDictionary(response, "buy_counts", "action");
		mEvoMaterialBuyCounts = ParseBuyCountDictionary(response, "buy_counts", "evo");
		mMonthlyStoreLoaded = true;
		PopulateActionCardStore();
		PopulateEvoMaterialStore();
	}

	private List<string> ParseStringList(Dictionary<string, object> response, string key)
	{
		List<string> list = new List<string>();
		if (!response.ContainsKey(key))
		{
			return list;
		}
		if (!(response[key] is List<object> list2))
		{
			return list;
		}
		foreach (object item in list2)
		{
			if (item != null)
			{
				string text = item.ToString();
				if (!string.IsNullOrEmpty(text))
				{
					list.Add(text);
				}
			}
		}
		return list;
	}

	private Dictionary<string, int> ParseBuyCountDictionary(Dictionary<string, object> response, string parentKey, string storeType)
	{
		Dictionary<string, int> dictionary = new Dictionary<string, int>();
		if (!response.ContainsKey(parentKey))
		{
			return dictionary;
		}
		if (!(response[parentKey] is Dictionary<string, object> dictionary2))
		{
			return dictionary;
		}
		if (!dictionary2.ContainsKey(storeType))
		{
			return dictionary;
		}
		if (!(dictionary2[storeType] is Dictionary<string, object> dictionary3))
		{
			return dictionary;
		}
		foreach (KeyValuePair<string, object> item in dictionary3)
		{
			if (item.Value != null && int.TryParse(item.Value.ToString(), out var result))
			{
				dictionary[item.Key] = result;
			}
		}
		return dictionary;
	}

	public void OnClickRefillStamina()
	{
		if (!isProcessingPurchase)
		{
			isProcessingPurchase = true;
			if (DetachedSingleton<StaminaManager>.Instance.GetStamina(StaminaType.Quests) >= Singleton<PlayerInfoScript>.Instance.RankData.Stamina && DetachedSingleton<StaminaManager>.Instance.GetStamina(StaminaType.Pvp) >= MiscParams.MaxPvpStamina)
			{
				Singleton<SimplePopupController>.Instance.ShowMessage(string.Empty, KFFLocalization.Get("!!STAMINA_ALREADY_FULL"));
				isProcessingPurchase = false;
			}
			else if (Singleton<PlayerInfoScript>.Instance.SaveData.HardCurrency < MiscParams.StaminaRefillCost)
			{
				Singleton<SimplePopupController>.Instance.ShowMessage(string.Empty, KFFLocalization.Get("!!STAMINA_NEED_STONES"));
				isProcessingPurchase = false;
			}
			else
			{
				Singleton<SimplePopupController>.Instance.ShowPurchasePrompt(KFFLocalization.Get("!!STAMINA_BUY_PROMPT"), string.Empty, MiscParams.StaminaRefillCost, ConfirmRefillStamina, cancelPopup);
			}
		}
	}

	private void ConfirmRefillStamina()
	{
		mWaitForUserAction = true;
		mUserActionProceed = NextAction.WAITING;
		Singleton<BusyIconPanelController>.Instance.Show();
		Singleton<PlayerInfoScript>.Instance.SaveData.Purchase("stamina", string.Empty, 1, UserActionCallback);
		mNextFunction = StaminaRefillExecute;
		isProcessingPurchase = false;
	}

	public void UserActionCallback(PlayerSaveData.ActionResult result)
	{
		if (result.success)
		{
			mUserActionProceed = NextAction.PROCEED;
		}
		else
		{
			mUserActionProceed = NextAction.ERROR;
		}
	}

	private void StaminaRefillExecute()
	{
		DetachedSingleton<StaminaManager>.Instance.RefillStamina();
		Singleton<PlayerInfoScript>.Instance.Save();
		Singleton<BuyStaminaPopupController>.Instance.Show();
	}

	public void OnClickExpandInventory()
	{
		if (Singleton<PlayerInfoScript>.Instance.SaveData.HardCurrency < MiscParams.InventorySpacePurchaseCost)
		{
			Singleton<SimplePopupController>.Instance.ShowMessage(string.Empty, KFFLocalization.Get("!!INVENTORY_SLOTS_NOBUY"));
			isProcessingPurchase = false;
		}
		else
		{
			Singleton<SimplePopupController>.Instance.ShowPurchasePrompt(KFFLocalization.Get("!!INVENTORY_SLOTS_CONFIRM").Replace("<val2>", MiscParams.InventorySpacePerPurchase.ToString()), string.Empty, MiscParams.InventorySpacePurchaseCost, ConfirmExpandInventory, cancelPopup);
		}
	}

	private void OnClickConfirmPurchaseSlots()
	{
		PlayerSaveData saveData = Singleton<PlayerInfoScript>.Instance.SaveData;
		mWaitForUserAction = true;
		mUserActionProceed = NextAction.WAITING;
		Singleton<BusyIconPanelController>.Instance.Show();
		saveData.Purchase("inventory", string.Empty, 1, UserActionCallback);
		mNextFunction = InventorySpaceExecute;
	}

	private void cancelPopup()
	{
		isProcessingPurchase = false;
	}

	private void ConfirmExpandInventory()
	{
		PlayerSaveData saveData = Singleton<PlayerInfoScript>.Instance.SaveData;
		mWaitForUserAction = true;
		mUserActionProceed = NextAction.WAITING;
		Singleton<BusyIconPanelController>.Instance.Show();
		saveData.Purchase("inventory", string.Empty, 1, UserActionCallback);
		mNextFunction = InventorySpaceExecute;
		isProcessingPurchase = false;
	}

	private void InventorySpaceExecute()
	{
		Singleton<PlayerInfoScript>.Instance.SaveData.AddEmptyInventorySlots(MiscParams.InventorySpacePerPurchase);
		Singleton<PlayerInfoScript>.Instance.Save();
		Singleton<BuyInventoryPopupController>.Instance.Show();
	}

	public void OnClickExpandAllyList()
	{
		if (!isProcessingPurchase)
		{
			isProcessingPurchase = true;
			if (Singleton<PlayerInfoScript>.Instance.SaveData.HardCurrency < MiscParams.AllyBoxPurchaseCost)
			{
				Singleton<SimplePopupController>.Instance.ShowMessage(string.Empty, KFFLocalization.Get("!!ALLY_LIST_NEED_STONES"));
				isProcessingPurchase = false;
			}
			else
			{
				Singleton<SimplePopupController>.Instance.ShowPurchasePrompt(KFFLocalization.Get("!!ALLY_SLOTS_CONFIRM").Replace("<val2>", MiscParams.AllyBoxPerPurchase.ToString()), string.Empty, MiscParams.AllyBoxPurchaseCost, ConfirmExpandAllyList, cancelPopup);
			}
		}
	}

	private void ConfirmExpandAllyList()
	{
		PlayerSaveData saveData = Singleton<PlayerInfoScript>.Instance.SaveData;
		mWaitForUserAction = true;
		mUserActionProceed = NextAction.WAITING;
		Singleton<BusyIconPanelController>.Instance.Show();
		saveData.Purchase("ally", string.Empty, 1, UserActionCallback);
		mNextFunction = AllySpaceExecute;
		isProcessingPurchase = false;
	}

	private void AllySpaceExecute()
	{
		PlayerSaveData saveData = Singleton<PlayerInfoScript>.Instance.SaveData;
		saveData.AddEmptyAllySlots(MiscParams.AllyBoxPerPurchase);
		Singleton<PlayerInfoScript>.Instance.Save();
		Singleton<SimplePopupController>.Instance.ShowMessage(string.Empty, KFFLocalization.Get("!!ALLY_SLOTS_BOUGHT").Replace("<val1>", saveData.AllyBoxSpace.ToString()));
	}

	public void OnClickBuySoftCurrency()
	{
		if (!isProcessingPurchase)
		{
			isProcessingPurchase = true;
			if (Singleton<PlayerInfoScript>.Instance.SaveData.HardCurrency < MiscParams.BuySoftCurrencyCost)
			{
				Singleton<SimplePopupController>.Instance.ShowMessage(string.Empty, KFFLocalization.Get("!!BUY_GOLD_NEED_STONES"));
				isProcessingPurchase = false;
			}
			else
			{
				Singleton<SimplePopupController>.Instance.ShowPurchasePrompt(KFFLocalization.Get("!!BUY_GOLD_CONFIRM").Replace("<val2>", MiscParams.BuySoftCurrencyAmount.ToString()), string.Empty, MiscParams.BuySoftCurrencyCost, ConfirmBuySoftCurrency, cancelPopup);
			}
		}
	}

	private void ConfirmBuySoftCurrency()
	{
		Singleton<PlayerInfoScript>.Instance.BuyGold(1);
	}

	public void OnLeaderClicked(StoreHeroPrefab heroTile)
	{
		PopulateLeader(heroTile.Leader);
	}

	public void OnClickBuyEvolutionShards()
	{
		if (!isProcessingPurchase)
		{
			isProcessingPurchase = true;
			if (Singleton<PlayerInfoScript>.Instance.SaveData.HardCurrency < MiscParams.BuyEvolutionShardsCost)
			{
				Singleton<SimplePopupController>.Instance.ShowMessage(string.Empty, KFFLocalization.Get("!!BUY_EVOLUTION_SHARDS_NEED_STONES"));
				isProcessingPurchase = false;
			}
			else
			{
				Singleton<SimplePopupController>.Instance.ShowPurchasePrompt(KFFLocalization.Get("!!BUY_EVOLUTION_SHARDS_CONFIRM").Replace("<val2>", MiscParams.BuyEvolutionShardsAmount.ToString()), string.Empty, MiscParams.BuyEvolutionShardsCost, ConfirmBuyEvolutionShards, cancelPopup);
			}
		}
	}

	private void ConfirmBuyEvolutionShards()
	{
		mWaitForUserAction = true;
		mUserActionProceed = NextAction.WAITING;
		Singleton<BusyIconPanelController>.Instance.Show();
		Singleton<PlayerInfoScript>.Instance.SaveData.ClaimCurrency("evolution shards purchase", string.Empty, 0, -MiscParams.BuyEvolutionShardsCost, 0, MiscParams.BuyEvolutionShardsAmount, UserActionCallback);
		mNextFunction = EvolutionShardsExecute;
		isProcessingPurchase = false;
	}

	//public void OnClickBuyCraftCoins()
	//{
	//	if (!isProcessingPurchase)
	//	{
	//		int buyCraftCurrencyCost = MiscParams.BuyCraftCurrencyCost;
	//		int buyCraftCurrencyAmount = MiscParams.BuyCraftCurrencyAmount;
	//		if (Singleton<PlayerInfoScript>.Instance.SaveData.HardCurrency < buyCraftCurrencyCost)
	//		{
	//			Singleton<SimplePopupController>.Instance.ShowMessage(string.Empty, "Not enough gems for a Craft Coin.");
	//			return;
	//		}
	//		isProcessingPurchase = true;
	//		Singleton<SimplePopupController>.Instance.ShowPurchasePrompt((buyCraftCurrencyAmount == 1) ? "Buy 1 Craft Coin?" : ("Buy " + buyCraftCurrencyAmount + " Craft Coins?"), string.Empty, buyCraftCurrencyCost, ConfirmBuyCraftCoins, cancelPopup);
	//	}
	//}

	//private void ConfirmBuyCraftCoins()
	//{
	//	mWaitForUserAction = true;
	//	mUserActionProceed = NextAction.WAITING;
	//	Singleton<BusyIconPanelController>.Instance.Show();
	//	Singleton<PlayerInfoScript>.Instance.SaveData.ClaimCurrency("craft coin purchase", string.Empty, 0, -MiscParams.BuyCraftCurrencyCost, 0, 0, UserActionCallback);
	//	mNextFunction = CraftCoinsExecute;
	//	isProcessingPurchase = false;
	//}

	//private void CraftCoinsExecute()
	//{
	//	Singleton<PlayerInfoScript>.Instance.Save();
	//	int buyCraftCurrencyAmount = MiscParams.BuyCraftCurrencyAmount;
	//	Singleton<SimplePopupController>.Instance.ShowMessage(string.Empty, (buyCraftCurrencyAmount == 1) ? "You got 1 Craft Coin!" : ("You got " + buyCraftCurrencyAmount + " Craft Coins!"));
	//}

	private void EvolutionShardsExecute()
	{
		Singleton<PlayerInfoScript>.Instance.Save();
		Singleton<SimplePopupController>.Instance.ShowMessage(string.Empty, KFFLocalization.Get("!!BUY_EVOLUTION_SHARDS_SUCCESS").Replace("<val1>", MiscParams.BuyEvolutionShardsAmount.ToString()));
	}

	private void PopulatePeppermintButlerActionCards(LeaderData leader)
	{
		Debug.Log("Populating PeppermintButler ActionCards for Leader: " + leader.Name + ", ID: " + leader.ID);
		UnloadCards();
		for (int i = 0; i < leader.ActionCards.Count; i++)
		{
			CardPrefabScript component = HeroCardNodes[i].InstantiateAsChild(Singleton<PrefabReferences>.Instance.Card).GetComponent<CardPrefabScript>();
			component.gameObject.ChangeLayer(base.gameObject.layer);
			component.Mode = CardPrefabScript.CardMode.GeneralFrontEnd;
			Debug.Log("Populating PeppermintButler Card: " + leader.ActionCards[i].Name + ", Texture Path: " + leader.ActionCards[i].UITexture);
			component.Populate(leader.ActionCards[i]);
			component.AdjustDepth(1);
			mSpawnedCards.Add(component);
		}
	}

	private void PopulateLeader(LeaderData leader)
	{
		mHighlightedLeader = leader;
		Debug.Log("Populating Leader: " + leader.Name + ", ID: " + leader.ID);
		HeroName.text = leader.Name;
		HeroAge.text = leader.FlvAge;
		HeroHeight.text = leader.FlvHeight;
		HeroWeight.text = leader.FlvWeight;
		HeroSpecies.text = leader.FlvSpecies;
		HeroCost.text = leader.BuyCost.ToString();
		Singleton<SLOTResourceManager>.Instance.QueueUITextureLoad(leader.PortraitTexture, "FTUEBundle", "UI/UI/LoadingPlaceholder", HeroImage);
		if (leader.ID == "Leader_PeppermintButler")
		{
			PopulatePeppermintButlerActionCards(leader);
		}
		else
		{
			UnloadCards();
			for (int i = 0; i < Mathf.Min(5, leader.ActionCards.Count); i++)
			{
				CardPrefabScript component = HeroCardNodes[i].InstantiateAsChild(Singleton<PrefabReferences>.Instance.Card).GetComponent<CardPrefabScript>();
				component.gameObject.ChangeLayer(base.gameObject.layer);
				component.Mode = CardPrefabScript.CardMode.GeneralFrontEnd;
				Debug.Log("Populating Card: " + leader.ActionCards[i].Name + ", Texture Path: " + leader.ActionCards[i].UITexture);
				component.Populate(leader.ActionCards[i]);
				component.AdjustDepth(1);
				mSpawnedCards.Add(component);
			}
		}
		bool flag = Singleton<PlayerInfoScript>.Instance.IsLeaderUnlocked(leader);
		bool flag2 = CanBuyableLeader(leader);
		HeroPurchaseButton.SetActive(!flag && flag2);
		if (!flag2)
		{
			HeroLevelRequirementLabel.gameObject.SetActive(value: true);
			HeroLevelRequirementLabel.text = "Unlock " + leader.Name + " at Level " + leader.RequiredLevel;
			HeroCost.text = KFFLocalization.Get("!!LOCKED");
		}
		else
		{
			HeroLevelRequirementLabel.gameObject.SetActive(value: false);
			HeroCost.text = leader.BuyCost.ToString();
		}
		mHeroGridDataSource.RepopulateObjects();
		if (!leader.AlwaysAvailable && !flag)
		{
			StartEndDate activeAvailablePeriod = leader.GetActiveAvailablePeriod();
			if (activeAvailablePeriod != null)
			{
				TimeSpan timeSpan = activeAvailablePeriod.EndDate - TFUtils.ServerTime;
				HeroAvailableLabel.text = KFFLocalization.Get("!!AVAILABLE_FOR_X").Replace("<val1>", PlayerInfoScript.FormatTimeString((int)timeSpan.TotalSeconds));
			}
			else
			{
				HeroAvailableLabel.text = string.Empty;
			}
		}
		else
		{
			HeroAvailableLabel.text = string.Empty;
		}
	}

	public void OnClickPurchaseHero()
	{
		if (!CanBuyableLeader(mHighlightedLeader))
		{
			Singleton<SimplePopupController>.Instance.ShowMessage(string.Empty, "Requires Level " + mHighlightedLeader.RequiredLevel);
		}
		else if (Singleton<TutorialController>.Instance.IsBlockActive("BuyHero"))
		{
			LeaderPurchaseExecute();
		}
		else
		{
			Singleton<SimplePopupController>.Instance.ShowPurchasePrompt(KFFLocalization.Get("!!HIRE_HERO_CONFIRM").Replace("<val2>", mHighlightedLeader.Name), KFFLocalization.Get("!!HIRE_HERO_NOBUY").Replace("<val2>", mHighlightedLeader.Name), mHighlightedLeader.BuyCost, ConfirmHeroPurchase, null, takeToBank: false);
		}
	}

	private void ConfirmHeroPurchase()
	{
		mWaitForUserAction = true;
		mUserActionProceed = NextAction.WAITING;
		Singleton<BusyIconPanelController>.Instance.Show();
		Singleton<PlayerInfoScript>.Instance.SaveData.Purchase("leader", mHighlightedLeader.ID, 1, UserActionCallback);
		mNextFunction = LeaderPurchaseExecute;
	}

	private void LeaderPurchaseExecute()
	{
		Singleton<PlayerInfoScript>.Instance.SaveData.Leaders.Add(new LeaderItem(mHighlightedLeader.ID));
		Singleton<PlayerInfoScript>.Instance.Save();
		PopulateHeroList();
		PopulateLeader(mHighlightedLeader);
		mSkinGridDataSource.RepopulateObjects();
		PopulateSkin(mHighlightedSkin ?? mHighlightedLeader);
		BuyHeroPopupController instance = Singleton<BuyHeroPopupController>.Instance;
		instance.Show(mHighlightedLeader);
		instance.transform.SetAsLastSibling();
		UIPanel componentInChildren = instance.GetComponentInChildren<UIPanel>();
		if (componentInChildren != null)
		{
			componentInChildren.depth = 1000;
		}
	}

	public void OnSkinClicked(StoreHeroPrefab skinTile)
	{
		PopulateSkin(skinTile.Leader);
	}

	private void PopulateSkin(LeaderData skin)
	{
		mHighlightedSkin = skin;
		SkinName.text = skin.Name;
		Singleton<SLOTResourceManager>.Instance.QueueUITextureLoad(skin.PortraitTexture, "FTUEBundle", "UI/UI/LoadingPlaceholder", SkinImage);
		SkinCost.transform.SetParentActive(value: false);
		SkinButtonLabel.transform.SetParentActive(value: false);
		BuySkinButton.SetActive(value: false);
		SkinEquippedParent.SetActive(value: false);
		LeaderItem skinOwnerItem = GetSkinOwnerItem(skin);
		if (skinOwnerItem != null)
		{
			if (skinOwnerItem.OwnedSkins.Contains(skin))
			{
				if (skinOwnerItem.SelectedSkin == skin)
				{
					SkinEquippedParent.SetActive(value: true);
				}
				else
				{
					BuySkinButton.SetActive(value: true);
					SkinButtonLabel.transform.SetParentActive(value: true);
					SkinButtonLabel.text = KFFLocalization.Get("!!EQUIP");
				}
			}
			else
			{
				BuySkinButton.SetActive(value: true);
				SkinCost.transform.SetParentActive(value: true);
				SkinCost.text = skin.SkinBuyCost.ToString();
			}
		}
		else
		{
			BuySkinButton.SetActive(value: true);
			SkinButtonLabel.transform.SetParentActive(value: true);
			SkinButtonLabel.text = KFFLocalization.Get("!!BUY_HERO");
		}
		mSkinGridDataSource.RepopulateObjects();
	}

	public void OnClickSkinButton()
	{
		if (mHighlightedSkin == null)
		{
			return;
		}
		LeaderItem skinOwnerItem = GetSkinOwnerItem(mHighlightedSkin);
		if (skinOwnerItem == null)
		{
			if (mHighlightedSkin.SkinParentLeader != null)
			{
				PopulateLeader(mHighlightedSkin.SkinParentLeader);
				OnClickPurchaseHero();
			}
		}
		else if (skinOwnerItem.IsSkinOwned(mHighlightedSkin))
		{
			skinOwnerItem.SelectedSkin = mHighlightedSkin;
			PopulateSkin(mHighlightedSkin);
		}
		else
		{
			Singleton<SimplePopupController>.Instance.ShowCustomizationPurchasePrompt(KFFLocalization.Get("!!BUY_SKIN_PROMPT"), KFFLocalization.Get("!!BUY_SKIN_PROMPT_CANT_AFFORD"), mHighlightedSkin.SkinBuyCost, ConfirmSkinPurchase, takeToBank: false);
		}
	}

	private void ConfirmSkinPurchase()
	{
		Singleton<PlayerInfoScript>.Instance.Save(delegate(bool success)
		{
			if (!success)
			{
				Singleton<SimplePopupController>.Instance.ShowMessage(string.Empty, KFFLocalization.Get("!!RELOAD_DATA_PROMPT"), ReloadFromServer, SimplePopupController.PopupPriority.ServerError);
			}
			else
			{
				mWaitForUserAction = true;
				mUserActionProceed = NextAction.WAITING;
				Singleton<BusyIconPanelController>.Instance.Show();
				Singleton<PlayerInfoScript>.Instance.SaveData.Purchase("skin", mHighlightedSkin.ID, 1, UserActionCallback);
				mNextFunction = SkinPurchaseExecute;
			}
		});
	}

	private void SkinPurchaseExecute()
	{
		LeaderItem skinOwnerItem = GetSkinOwnerItem(mHighlightedSkin);
		skinOwnerItem.OwnedSkins.Add(mHighlightedSkin);
		skinOwnerItem.SelectedSkin = mHighlightedSkin;
		Singleton<PlayerInfoScript>.Instance.Save();
		PopulateSkinList();
		PopulateSkin(mHighlightedSkin);
	}

	public bool IsHeroHighlighted(LeaderData leader)
	{
		return mHighlightedLeader == leader;
	}

	public bool IsSkinHighlighted(LeaderData skin)
	{
		return mHighlightedSkin == skin;
	}

	private IEnumerator SkipStoreTutorial()
	{
		Singleton<TutorialController>.Instance.AdvanceIfTargetingBuilding("TBuilding_Store");
		while (true)
		{
			Singleton<TutorialController>.Instance.AdvanceIfOnState("BA_InSale");
			if (Singleton<TutorialController>.Instance.IsStateActive("BA_CloseBank"))
			{
				break;
			}
			yield return null;
		}
		Singleton<TutorialController>.Instance.AdvanceTutorialState();
		Singleton<TownHudController>.Instance.ReturnToTownView();
	}

	private SpecialSaleData GetSaleToDisplay(SpecialSaleData.SaleMode mode)
	{
		foreach (SpecialSaleData allSale in SpecialSaleDataManager.Instance.GetAllSales())
		{
			if (allSale.Mode == mode)
			{
				SpecialSaleItem sale = Singleton<PlayerInfoScript>.Instance.GetSale(allSale);
				if (sale == null || sale.IsAvailable())
				{
					return allSale;
				}
			}
		}
		return null;
	}

	public void OnClickSaleButton()
	{
		SpecialSaleData saleToDisplay = GetSaleToDisplay(SpecialSaleData.SaleMode.SpecialSale01);
		if (saleToDisplay == null)
		{
			SaleButton.SetActive(value: false);
			TabsGrid.Reposition();
		}
		else
		{
			Singleton<SalePopupController>.Instance.Show(saleToDisplay, SalePopupController.ShowLocation.StoreScreen);
		}
	}

	public void OnClickSale2Button()
	{
		SpecialSaleData saleToDisplay = GetSaleToDisplay(SpecialSaleData.SaleMode.SpecialSale02);
		if (saleToDisplay == null)
		{
			SaleButton2.SetActive(value: false);
			TabsGrid.Reposition();
		}
		else
		{
			Singleton<SalePopupController>.Instance.Show(saleToDisplay, SalePopupController.ShowLocation.StoreScreen);
		}
	}

	public void OnClickSale3Button()
	{
		SpecialSaleData saleToDisplay = GetSaleToDisplay(SpecialSaleData.SaleMode.SpecialSale03);
		if (saleToDisplay == null)
		{
			SaleButton3.SetActive(value: false);
			TabsGrid.Reposition();
		}
		else
		{
			Singleton<SalePopupController>.Instance.Show(saleToDisplay, SalePopupController.ShowLocation.StoreScreen);
		}
	}

	public void OnClickClose()
	{
		if (DetachedSingleton<SceneFlowManager>.Instance.InFrontEnd())
		{
			Singleton<MouseOrbitCamera>.Instance.CheckTiltCamSettingBeforeTutorial();
		}
		if (DetachedSingleton<SceneFlowManager>.Instance.InBattleScene())
		{
			Singleton<PauseController>.Instance.HideIfShowing();
		}
		HideTween.Play();
	}

	private int GetPlayerLevel()
	{
		return Singleton<PlayerInfoScript>.Instance.RankData.Level;
	}

	private bool CanBuyableLeader(LeaderData leader)
	{
		return GetPlayerLevel() >= leader.RequiredLevel;
	}

	public bool CanShowLevelSale(SpecialSaleData sale)
	{
		return GetPlayerLevel() >= sale.RequiredLevel;
	}

	public void OnSale01Purchased(SpecialSaleData sale)
	{
		PlayerSaveData saveData = Singleton<PlayerInfoScript>.Instance.SaveData;
		if (saveData.SelectedSale == sale)
		{
			saveData.SelectedSale = null;
		}
		SaleButton.SetActive(value: false);
		TabsGrid.Reposition();
	}

	public void OnSale02Purchased(SpecialSaleData sale)
	{
		PlayerSaveData saveData = Singleton<PlayerInfoScript>.Instance.SaveData;
		if (saveData.SelectedSale == sale)
		{
			saveData.SelectedSale = null;
		}
		SaleButton2.SetActive(value: false);
		TabsGrid.Reposition();
	}

	public void OnSale03Purchased(SpecialSaleData sale)
	{
		PlayerSaveData saveData = Singleton<PlayerInfoScript>.Instance.SaveData;
		if (saveData.SelectedSale == sale)
		{
			saveData.SelectedSale = null;
		}
		SaleButton3.SetActive(value: false);
		TabsGrid.Reposition();
	}

	public bool IsSaleVisible(SpecialSaleData data, SpecialSaleItem item, int playerLevel)
	{
		if (data == null)
		{
			return false;
		}
		if (item != null && item.ConsumedOnce && data.OneShot)
		{
			return false;
		}
		if (data.Mode == SpecialSaleData.SaleMode.SpecialSale01)
		{
			DateTime serverTime = TFUtils.ServerTime;
			if (data.AvailableDates == null || data.AvailableDates.Count == 0)
			{
				return false;
			}
			foreach (StartEndDate availableDate in data.AvailableDates)
			{
				if (serverTime >= availableDate.StartDate && serverTime <= availableDate.EndDate)
				{
					return true;
				}
			}
			return false;
		}
		if (data.Mode == SpecialSaleData.SaleMode.SpecialSale02)
		{
			DateTime serverTime2 = TFUtils.ServerTime;
			if (data.AvailableDates == null || data.AvailableDates.Count == 0)
			{
				return false;
			}
			foreach (StartEndDate availableDate2 in data.AvailableDates)
			{
				if (serverTime2 >= availableDate2.StartDate && serverTime2 <= availableDate2.EndDate)
				{
					return true;
				}
			}
			return false;
		}
		if (data.Mode == SpecialSaleData.SaleMode.SpecialSale03)
		{
			DateTime serverTime3 = TFUtils.ServerTime;
			if (data.AvailableDates == null || data.AvailableDates.Count == 0)
			{
				return false;
			}
			foreach (StartEndDate availableDate3 in data.AvailableDates)
			{
				if (serverTime3 >= availableDate3.StartDate && serverTime3 <= availableDate3.EndDate)
				{
					return true;
				}
			}
			return false;
		}
		return false;
	}

	public void ShowSaleButton2(SpecialSaleData sale)
	{
		bool flag = sale != null && sale.Mode == SpecialSaleData.SaleMode.SpecialSale02;
		SaleButton2.SetActive(flag);
		if (flag)
		{
			TabsGrid?.Reposition();
		}
	}

	public void ShowSaleButton3(SpecialSaleData sale)
	{
		bool flag = sale != null && sale.Mode == SpecialSaleData.SaleMode.SpecialSale03;
		SaleButton3.SetActive(flag);
		if (flag)
		{
			TabsGrid?.Reposition();
		}
	}

	private LeaderItem GetSkinOwnerItem(LeaderData skin)
	{
		PlayerInfoScript instance = Singleton<PlayerInfoScript>.Instance;
		if (skin.SkinParentLeader != null)
		{
			LeaderItem leaderItem = instance.GetLeaderItem(skin.SkinParentLeader);
			if (leaderItem != null)
			{
				return leaderItem;
			}
		}
		if (skin.SkinParentLeaderLevel2 != null)
		{
			LeaderItem leaderItem2 = instance.GetLeaderItem(skin.SkinParentLeaderLevel2);
			if (leaderItem2 != null)
			{
				return leaderItem2;
			}
		}
		if (skin.SkinParentLeaderLevel3 != null)
		{
			LeaderItem leaderItem3 = instance.GetLeaderItem(skin.SkinParentLeaderLevel3);
			if (leaderItem3 != null)
			{
				return leaderItem3;
			}
		}
		return null;
	}

	public void OnClickBuyCreature(StoreBuyCreatureData data)
	{
		if (data == null)
		{
			Debug.LogError("OnClickBuyCreature: data is NULL.");
			return;
		}
		if (data.Creature == null)
		{
			Debug.LogError("OnClickBuyCreature: Creature is NULL. " + data.CreatureID);
			return;
		}
		if (Singleton<PlayerInfoScript>.Instance.SaveData.HardCurrency < data.BuyCost)
		{
			Singleton<SimplePopupController>.Instance.ShowMessage(string.Empty, "Not enough Gems.");
			return;
		}
		mPendingCreaturePurchase = data;
		mPendingActionCardPurchase = null;
		mPendingCardBackPurchase = null;
		mMonthlyStorePurchasePending = false;
		mPendingStoreBuyCost = data.BuyCost;
		Singleton<SimplePopupController>.Instance.ShowPurchasePrompt("Buy " + data.Creature.Name + "?", "Not enough currency.", data.BuyCost, ConfirmStorePurchase, cancelPopup);
	}

	public void OnClickBuyActionCard(StoreBuyActionCardData data)
	{
		if (data == null)
		{
			Debug.LogError("OnClickBuyActionCard: data is NULL.");
			return;
		}
		if (data.Card == null)
		{
			Debug.LogError("OnClickBuyActionCard: Card is NULL. " + data.CardID);
			return;
		}
		int actionCardBuyCount = GetActionCardBuyCount(data.ID);
		Debug.Log("[Monthly Store] Action Card Buy Check: " + data.ID + " / " + actionCardBuyCount + "/" + 3);
		if (actionCardBuyCount >= 3)
		{
			Debug.LogWarning("[Monthly Store] Action Card BUY LIMIT REACHED locally: " + data.ID + " / " + actionCardBuyCount + "/" + 3);
			if (Singleton<SimplePopupController>.Instance != null)
			{
				Singleton<SimplePopupController>.Instance.ShowMessage(string.Empty, "You have reached the purchase limit.");
			}
		}
		else if (Singleton<PlayerInfoScript>.Instance.SaveData.HardCurrency < data.BuyCost)
		{
			Singleton<SimplePopupController>.Instance.ShowMessage(string.Empty, "Not enough Gems.");
		}
		else
		{
			mPendingActionCardPurchase = data;
			mPendingCreaturePurchase = null;
			mPendingEvoMaterialPurchase = null;
			mPendingCardBackPurchase = null;
			mMonthlyStorePurchasePending = true;
			mPendingStoreBuyCost = data.BuyCost;
			Singleton<SimplePopupController>.Instance.ShowPurchasePrompt("Buy " + data.Card.Name + "?", "Not enough currency.", data.BuyCost, ConfirmStorePurchase, cancelPopup);
		}
	}

	public void OnClickBuyEvoMaterial(StoreBuyEvoMaterialData data)
	{
		if (data == null)
		{
			Debug.LogError("OnClickBuyEvoMaterial: data is NULL.");
			return;
		}
		if (data.EvoMaterial == null)
		{
			Debug.LogError("OnClickBuyEvoMaterial: EvoMaterial is NULL. " + data.EvoMaterialID);
			return;
		}
		int evoMaterialBuyCount = GetEvoMaterialBuyCount(data.ID);
		int num = 1;
		Debug.Log("[Evo Material] Buy attempt: " + data.ID + " BuyCount: " + evoMaterialBuyCount + "/" + num);
		if (evoMaterialBuyCount >= num)
		{
			Debug.LogWarning("[Evo Material] BUY LIMIT REACHED: " + data.ID + " " + evoMaterialBuyCount + "/" + num);
			if (Singleton<SimplePopupController>.Instance != null)
			{
				Singleton<SimplePopupController>.Instance.ShowMessage(string.Empty, "Buy limit reached.");
			}
		}
		else if (Singleton<PlayerInfoScript>.Instance.SaveData.PvPCurrency < data.BuyCost)
		{
			Debug.LogWarning("[Evo Material] Not enough Wishbones. Required: " + data.BuyCost + " Available: " + Singleton<PlayerInfoScript>.Instance.SaveData.PvPCurrency);
			Singleton<SimplePopupController>.Instance.ShowMessage(string.Empty, "Not enough Wishbones.");
		}
		else
		{
			mPendingEvoMaterialPurchase = data;
			mPendingCreaturePurchase = null;
			mPendingActionCardPurchase = null;
			mPendingCardBackPurchase = null;
			mMonthlyStorePurchasePending = true;
			mPendingStoreBuyCost = data.BuyCost;
			Debug.Log("[Evo Material] Starting purchase: " + data.ID + " BuyCount: " + evoMaterialBuyCount + "/" + num);
			Singleton<SimplePopupController>.Instance.ShowPurchasePrompt("Buy " + data.EvoMaterial.Name + "?", "Not enough currency.", data.BuyCost, ConfirmEvoMaterialPurchase, cancelPopup);
		}
	}

	private void ConfirmStorePurchase()
	{
		if (mMonthlyStorePurchasePending)
		{
			ConfirmMonthlyStorePurchase();
			return;
		}
		mWaitForUserAction = true;
		mUserActionProceed = NextAction.WAITING;
		Singleton<BusyIconPanelController>.Instance.Show();
		Singleton<PlayerInfoScript>.Instance.SaveData.Purchase((mPendingCreaturePurchase != null) ? "creature" : "cardback", (mPendingCreaturePurchase != null) ? mPendingCreaturePurchase.ID : mPendingCardBackPurchase.ID, 1, UserActionCallback);
		mNextFunction = StorePurchaseExecute;
	}

	private void ConfirmEvoMaterialPurchase()
	{
		ConfirmMonthlyStorePurchase();
	}

	private void ConfirmMonthlyStorePurchase()
	{
		if (SessionManager.Instance == null)
		{
			Debug.LogError("[Monthly Store] SessionManager is NULL.");
			return;
		}
		string text;
		string iD;
		if (mPendingActionCardPurchase != null)
		{
			text = "action";
			iD = mPendingActionCardPurchase.ID;
		}
		else
		{
			if (mPendingEvoMaterialPurchase == null)
			{
				Debug.LogError("[Monthly Store] No pending monthly purchase.");
				return;
			}
			text = "evo";
			iD = mPendingEvoMaterialPurchase.ID;
		}
		Debug.Log("[Monthly Store] Sending purchase request: " + text + " / " + iD);
		Singleton<BusyIconPanelController>.Instance.Show();
		SessionManager.Instance.theSession.Server.BuyMonthlyStore(SessionManager.Instance.PlayerID, text, iD, OnMonthlyStoreBuyReceived);
	}

	private void OnMonthlyStoreBuyReceived(Dictionary<string, object> response, HttpStatusCode status)
	{
		Debug.Log("========== MONTHLY BUY CALLBACK START ==========");
		Debug.Log("[Monthly Store] HTTP Status: " + status);
		if (status != HttpStatusCode.OK)
		{
			Debug.LogError("[Monthly Store] Buy request failed. HTTP Status: " + status);
			return;
		}
		if (response == null)
		{
			Debug.LogError("[Monthly Store] Buy response is NULL.");
			return;
		}
		bool flag = false;
		if (response.ContainsKey("success"))
		{
			flag = response["success"] is bool && (bool)response["success"];
		}
		Debug.Log("[Monthly Store] Success parsed: " + flag);
		if (!flag)
		{
			string text = (response.ContainsKey("error") ? response["error"].ToString() : "Unknown error");
			Debug.LogError("[Monthly Store] Buy failed: " + text);
			if (!(text == "BUY_LIMIT_REACHED"))
			{
				return;
			}
			int result = 0;
			int result2 = 0;
			if (response.ContainsKey("buy_count"))
			{
				int.TryParse(response["buy_count"].ToString(), out result);
			}
			if (response.ContainsKey("buy_limit"))
			{
				int.TryParse(response["buy_limit"].ToString(), out result2);
			}
			string text2 = (response.ContainsKey("store_type") ? response["store_type"].ToString() : string.Empty);
			string text3 = (response.ContainsKey("offer_id") ? response["offer_id"].ToString() : string.Empty);
			Debug.LogWarning("[Monthly Store] BUY LIMIT REACHED: " + text2 + " / " + text3 + " / " + result + "/" + result2);
			if (text2 == "action")
			{
				if (mActionCardBuyCounts == null)
				{
					mActionCardBuyCounts = new Dictionary<string, int>();
				}
				mActionCardBuyCounts[text3] = result;
				RefreshActionCardStoreUI(text3);
			}
			else if (text2 == "evo")
			{
				if (mEvoMaterialBuyCounts == null)
				{
					mEvoMaterialBuyCounts = new Dictionary<string, int>();
				}
				mEvoMaterialBuyCounts[text3] = result;
				RefreshEvoMaterialStoreUI(text3);
			}
			Debug.Log("[Monthly Store] Local count updated: " + text2 + " / " + text3 + " = " + result);
			return;
		}
		int result3 = 0;
		int result4 = 0;
		if (response.ContainsKey("buy_count"))
		{
			int.TryParse(response["buy_count"].ToString(), out result3);
		}
		if (response.ContainsKey("buy_limit"))
		{
			int.TryParse(response["buy_limit"].ToString(), out result4);
		}
		string text4 = (response.ContainsKey("store_type") ? response["store_type"].ToString() : string.Empty);
		string text5 = (response.ContainsKey("offer_id") ? response["offer_id"].ToString() : string.Empty);
		Debug.Log("[Monthly Store] PURCHASE SUCCESS: " + text4 + " / " + text5 + " / " + result3 + "/" + result4);
		if (text4 == "action")
		{
			if (mActionCardBuyCounts == null)
			{
				mActionCardBuyCounts = new Dictionary<string, int>();
			}
			mActionCardBuyCounts[text5] = result3;
		}
		else if (text4 == "evo")
		{
			if (mEvoMaterialBuyCounts == null)
			{
				mEvoMaterialBuyCounts = new Dictionary<string, int>();
			}
			mEvoMaterialBuyCounts[text5] = result3;
		}
		if (text4 == "action")
		{
			RefreshActionCardStoreUI(text5);
		}
		if (text4 == "evo")
		{
			RefreshEvoMaterialStoreUI(text5);
		}
		Debug.Log("[Monthly Store] Local buy count updated.");
		mNextFunction = StorePurchaseExecute;
		mWaitForUserAction = true;
		mUserActionProceed = NextAction.WAITING;
		PlayerSaveData saveData = Singleton<PlayerInfoScript>.Instance.SaveData;
		int result5 = 0;
		if (response.ContainsKey("charged"))
		{
			int.TryParse(response["charged"].ToString(), out result5);
		}
		if (text4 == "action" || text4 == "evo")
		{
			if (response.ContainsKey("level1") && response.ContainsKey("level2"))
			{
				int result6 = 0;
				int result7 = 0;
				int.TryParse(response["level1"].ToString(), out result6);
				int.TryParse(response["level2"].ToString(), out result7);
				saveData.SetGemsFromServer(result6, result7);
			}
			string text6 = (response.ContainsKey("currency") ? response["currency"].ToString() : string.Empty);
			if (text6 == "PvpCurrency")
			{
				saveData.PvPCurrency -= result5;
			}
			else if (text6 == "SoftCurrency")
			{
				saveData.SoftCurrency -= result5;
			}
			UserActionCallback(new PlayerSaveData.ActionResult
			{
				success = true
			});
			Debug.Log("========== MONTHLY BUY CALLBACK END ==========");
		}
		else
		{
			Debug.LogError("[Monthly Store] Unknown store type: " + text4);
		}
	}

	private void StorePurchaseExecute()
	{
		Debug.Log("[Monthly Store] ===============================");
		Debug.Log("[Monthly Store] StorePurchaseExecute START");
		PlayerInfoScript instance = Singleton<PlayerInfoScript>.Instance;
		if (instance == null)
		{
			Debug.LogError("[Monthly Store] PlayerInfoScript is NULL.");
			return;
		}
		Debug.Log("[Monthly Store] PlayerInfoScript OK");
		if (mPendingActionCardPurchase != null)
		{
			StoreBuyActionCardData storeBuyActionCardData = mPendingActionCardPurchase;
			Debug.Log("[Monthly Store] Giving Action Card: " + storeBuyActionCardData.CardID + " Quantity: " + storeBuyActionCardData.Quantity);
			BuyActionCardExecute(instance, storeBuyActionCardData);
			Debug.Log("[Monthly Store] BuyActionCardExecute FINISHED");
		}
		else if (mPendingEvoMaterialPurchase != null)
		{
			StoreBuyEvoMaterialData storeBuyEvoMaterialData = mPendingEvoMaterialPurchase;
			Debug.Log("[Monthly Store] Giving Evo Material: " + storeBuyEvoMaterialData.EvoMaterialID + " Quantity: " + storeBuyEvoMaterialData.Quantity);
			BuyEvoMaterialExecute(instance, storeBuyEvoMaterialData);
			Debug.Log("[Monthly Store] BuyEvoMaterialExecute FINISHED");
		}
		else if (mPendingCardBackPurchase != null)
		{
			StoreBuyCardBackData storeBuyCardBackData = mPendingCardBackPurchase;
			Debug.Log("[Monthly Store] Giving Card Back: " + storeBuyCardBackData.CardBackID + " Quantity: " + storeBuyCardBackData.Quantity);
			BuyCardBackExecute(instance, storeBuyCardBackData);
			Debug.Log("[Monthly Store] BuyCardBackExecute FINISHED");
		}
		else
		{
			if (mPendingCreaturePurchase == null)
			{
				Debug.LogError("[Monthly Store] StorePurchaseExecute: No pending store purchase.");
				return;
			}
			Debug.Log("[Monthly Store] Giving Creature");
			BuyCreatureExecute(instance, mPendingCreaturePurchase);
			Debug.Log("[Monthly Store] BuyCreatureExecute FINISHED");
		}
		Debug.Log("[Monthly Store] BEFORE player.Save()");
		instance.Save();
		Debug.Log("[Monthly Store] AFTER player.Save()");
		mPendingCreaturePurchase = null;
		mPendingActionCardPurchase = null;
		mPendingEvoMaterialPurchase = null;
		mPendingCardBackPurchase = null;
		mPendingStoreBuyCost = 0;
		Debug.Log("[Monthly Store] StorePurchaseExecute END");
	}

	private void BuyCreatureExecute(PlayerInfoScript player, StoreBuyCreatureData data)
	{
		if (data.Creature == null)
		{
			Debug.LogError("BuyCreatureExecute: Creature is NULL.");
			return;
		}
		for (int i = 0; i < data.Quantity; i++)
		{
			CreatureItem creature = new CreatureItem(data.Creature);
			player.SaveData.AddCreature(creature);
		}
	}

	private void BuyActionCardExecute(PlayerInfoScript player, StoreBuyActionCardData data)
	{
		if (data.Card == null)
		{
			Debug.LogError("BuyActionCardExecute: Card is NULL.");
			return;
		}
		for (int i = 0; i < data.Quantity; i++)
		{
			CardItem card = new CardItem(data.Card);
			player.SaveData.AddExCard(card);
		}
	}

	private void BuyEvoMaterialExecute(PlayerInfoScript player, StoreBuyEvoMaterialData data)
	{
		if (data.EvoMaterial == null)
		{
			Debug.LogError("BuyEvoMaterialExecute: EvoMaterial is NULL.");
			return;
		}
		for (int i = 0; i < data.Quantity; i++)
		{
			player.SaveData.AddEvoMaterial(data.EvoMaterial);
		}
	}

	private void PopulateCreatureStore()
	{
		if (CreatureStoreGrid == null)
		{
			Debug.LogError("PopulateCreatureStore: CreatureStoreGrid is NULL.");
			return;
		}
		if (StoreBuyCreaturePrefab == null)
		{
			Debug.LogError("PopulateCreatureStore: StoreBuyCreaturePrefab is NULL.");
			return;
		}
		CreatureStoreGrid.transform.DestroyAllChildren();
		foreach (StoreBuyCreatureData item in StoreBuyCreatureDataManager.Instance.GetDatabase())
		{
			if (item == null || StoreHidden(item.ID))
			{
				continue;
			}
			if (item.Creature == null)
			{
				Debug.LogWarning("Skipping creature store item. Creature is NULL. CreatureID: " + item.CreatureID);
				continue;
			}
			GameObject gameObject = CreatureStoreGrid.transform.InstantiateAsChild(StoreBuyCreaturePrefab);
			if (gameObject == null)
			{
				Debug.LogError("Failed to instantiate StoreBuyCreaturePrefab.");
				continue;
			}
			gameObject.transform.localScale = Vector3.one;
			gameObject.SetActive(value: true);
			UIWidget[] componentsInChildren = gameObject.GetComponentsInChildren<UIWidget>(includeInactive: true);
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				componentsInChildren[i].depth += 100;
			}
			StoreBuyCreatureScript componentInChildren = gameObject.GetComponentInChildren<StoreBuyCreatureScript>(includeInactive: true);
			if (componentInChildren == null)
			{
				Debug.LogError("StoreBuyCreaturePrefab does not contain StoreBuyCreatureScript.");
				NGUITools.Destroy(gameObject);
			}
			else
			{
				componentInChildren.Populate(item);
			}
		}
		CreatureStoreGrid.Reposition();
		UIScrollView componentInParent = CreatureStoreGrid.GetComponentInParent<UIScrollView>();
		if (componentInParent != null)
		{
			componentInParent.UpdateScrollbars();
			componentInParent.RestrictWithinBounds(instant: true);
		}
	}

	private void PopulateActionCardStore()
	{
		Debug.Log("[Monthly Store] PopulateActionCardStore START");
		if (ActionCardStoreGrid == null)
		{
			Debug.LogError("PopulateActionCardStore: ActionCardStoreGrid is NULL.");
			return;
		}
		if (StoreBuyActionCardPrefab == null)
		{
			Debug.LogError("PopulateActionCardStore: StoreBuyActionCardPrefab is NULL.");
			return;
		}
		ActionCardStoreGrid.transform.DestroyAllChildren();
		List<StoreBuyActionCardData> activeMonthlyOffers = StoreBuyActionCardDataManager.Instance.GetActiveMonthlyOffers(mActiveActionCardIds, mSpecialActionCardIds);
		if (activeMonthlyOffers == null)
		{
			Debug.LogError("PopulateActionCardStore: activeOffers is NULL.");
			return;
		}
		foreach (StoreBuyActionCardData item in activeMonthlyOffers)
		{
			if (item == null || StoreHidden(item.ID))
			{
				continue;
			}
			if (item.Card == null)
			{
				Debug.LogWarning("Skipping action card store item. Card is NULL. CardID: " + item.CardID);
				continue;
			}
			GameObject gameObject = ActionCardStoreGrid.transform.InstantiateAsChild(StoreBuyActionCardPrefab);
			if (gameObject == null)
			{
				Debug.LogError("Failed to instantiate StoreBuyActionCardPrefab.");
				continue;
			}
			gameObject.transform.localScale = Vector3.one;
			gameObject.SetActive(value: true);
			UIWidget[] componentsInChildren = gameObject.GetComponentsInChildren<UIWidget>(includeInactive: true);
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				componentsInChildren[i].depth += 100;
			}
			StoreBuyActionCardScript componentInChildren = gameObject.GetComponentInChildren<StoreBuyActionCardScript>(includeInactive: true);
			if (componentInChildren == null)
			{
				Debug.LogError("StoreBuyActionCardPrefab does not contain StoreBuyActionCardScript.");
				NGUITools.Destroy(gameObject);
			}
			else
			{
				componentInChildren.Populate(item);
			}
		}
		ActionCardStoreGrid.Reposition();
		UIScrollView componentInParent = ActionCardStoreGrid.GetComponentInParent<UIScrollView>();
		if (componentInParent != null)
		{
			componentInParent.UpdateScrollbars();
			componentInParent.RestrictWithinBounds(instant: true);
		}
	}

	private void PopulateEvoMaterialStore()
	{
		if (EvoMaterialStoreGrid == null)
		{
			Debug.LogError("PopulateEvoMaterialStore: EvoMaterialStoreGrid is NULL.");
			return;
		}
		if (StoreBuyEvoMaterialPrefab == null)
		{
			Debug.LogError("PopulateEvoMaterialStore: StoreBuyEvoMaterialPrefab is NULL.");
			return;
		}
		EvoMaterialStoreGrid.transform.DestroyAllChildren();
		List<StoreBuyEvoMaterialData> activeMonthlyOffers = StoreBuyEvoMaterialDataManager.Instance.GetActiveMonthlyOffers(mActiveEvoMaterialIds, mSpecialEvoMaterialIds);
		if (activeMonthlyOffers == null)
		{
			Debug.LogError("PopulateEvoMaterialStore: activeOffers is NULL.");
			return;
		}
		foreach (StoreBuyEvoMaterialData item in activeMonthlyOffers)
		{
			if (item == null || StoreHidden(item.ID))
			{
				continue;
			}
			if (item.EvoMaterial == null)
			{
				Debug.LogWarning("Skipping evo material store item. EvoMaterial is NULL. ID: " + item.EvoMaterialID);
				continue;
			}
			GameObject gameObject = EvoMaterialStoreGrid.transform.InstantiateAsChild(StoreBuyEvoMaterialPrefab);
			if (gameObject == null)
			{
				Debug.LogError("Failed to instantiate StoreBuyEvoMaterialPrefab.");
				continue;
			}
			gameObject.transform.localScale = Vector3.one;
			gameObject.SetActive(value: true);
			UIWidget[] componentsInChildren = gameObject.GetComponentsInChildren<UIWidget>(includeInactive: true);
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				componentsInChildren[i].depth += 100;
			}
			StoreBuyEvoMaterialScript componentInChildren = gameObject.GetComponentInChildren<StoreBuyEvoMaterialScript>(includeInactive: true);
			if (componentInChildren == null)
			{
				Debug.LogError("StoreBuyEvoMaterialPrefab does not contain StoreBuyEvoMaterialScript.");
				NGUITools.Destroy(gameObject);
			}
			else
			{
				componentInChildren.Populate(item);
			}
		}
		EvoMaterialStoreGrid.Reposition();
		UIScrollView componentInParent = EvoMaterialStoreGrid.GetComponentInParent<UIScrollView>();
		if (componentInParent != null)
		{
			componentInParent.UpdateScrollbars();
			componentInParent.RestrictWithinBounds(instant: true);
		}
	}

	public int GetActionCardBuyCount(string offerId)
	{
		if (mActionCardBuyCounts == null)
		{
			return 0;
		}
		if (mActionCardBuyCounts.TryGetValue(offerId, out var value))
		{
			return value;
		}
		return 0;
	}

	public int GetEvoMaterialBuyCount(string offerId)
	{
		if (mEvoMaterialBuyCounts == null)
		{
			return 0;
		}
		if (mEvoMaterialBuyCounts.TryGetValue(offerId, out var value))
		{
			return value;
		}
		return 0;
	}

	private void RefreshActionCardStoreUI(string offerId)
	{
		if (ActionCardStoreGrid == null)
		{
			return;
		}
		StoreBuyActionCardScript[] componentsInChildren = ActionCardStoreGrid.GetComponentsInChildren<StoreBuyActionCardScript>(includeInactive: true);
		foreach (StoreBuyActionCardScript storeBuyActionCardScript in componentsInChildren)
		{
			if (!(storeBuyActionCardScript == null))
			{
				storeBuyActionCardScript.RefreshBuyCount();
			}
		}
	}

	private void RefreshEvoMaterialStoreUI(string offerId)
	{
		if (EvoMaterialStoreGrid == null)
		{
			return;
		}
		StoreBuyEvoMaterialScript[] componentsInChildren = EvoMaterialStoreGrid.GetComponentsInChildren<StoreBuyEvoMaterialScript>(includeInactive: true);
		foreach (StoreBuyEvoMaterialScript storeBuyEvoMaterialScript in componentsInChildren)
		{
			if (!(storeBuyEvoMaterialScript == null))
			{
				storeBuyEvoMaterialScript.RefreshBuyCount();
			}
		}
	}

	private void PopulateCardBackStore()
	{
		if (CardBackStoreGrid == null)
		{
			Debug.LogError("PopulateCardBackStore: CardBackStoreGrid is NULL.");
			return;
		}
		if (StoreBuyCardBackPrefab == null)
		{
			Debug.LogError("PopulateCardBackStore: StoreBuyCardBackPrefab is NULL.");
			return;
		}
		CardBackStoreGrid.transform.DestroyAllChildren();
		foreach (StoreBuyCardBackData item in StoreBuyCardBackDataManager.Instance.GetDatabase())
		{
			if (item == null || StoreHidden(item.ID))
			{
				continue;
			}
			if (item.CardBack == null)
			{
				Debug.LogWarning("Skipping card back store item. CardBack is NULL. CardBackID: " + item.CardBackID);
				continue;
			}
			GameObject gameObject = CardBackStoreGrid.transform.InstantiateAsChild(StoreBuyCardBackPrefab);
			if (gameObject == null)
			{
				Debug.LogError("Failed to instantiate StoreBuyCardBackPrefab.");
				continue;
			}
			gameObject.transform.localScale = Vector3.one;
			gameObject.SetActive(value: true);
			UIWidget[] componentsInChildren = gameObject.GetComponentsInChildren<UIWidget>(includeInactive: true);
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				componentsInChildren[i].depth += 100;
			}
			StoreBuyCardBackScript componentInChildren = gameObject.GetComponentInChildren<StoreBuyCardBackScript>(includeInactive: true);
			if (componentInChildren == null)
			{
				Debug.LogError("StoreBuyCardBackPrefab does not contain StoreBuyCardBackScript.");
				NGUITools.Destroy(gameObject);
			}
			else
			{
				componentInChildren.Populate(item);
			}
		}
		CardBackStoreGrid.Reposition();
		UIScrollView componentInParent = CardBackStoreGrid.GetComponentInParent<UIScrollView>();
		if (componentInParent != null)
		{
			componentInParent.UpdateScrollbars();
			componentInParent.RestrictWithinBounds(instant: true);
		}
	}

	private void BuyCardBackExecute(PlayerInfoScript player, StoreBuyCardBackData data)
	{
		if (player == null)
		{
			Debug.LogError("BuyCardBackExecute: Player is NULL.");
			return;
		}
		if (data == null)
		{
			Debug.LogError("BuyCardBackExecute: data is NULL.");
			return;
		}
		if (data.CardBack == null)
		{
			Debug.LogError("BuyCardBackExecute: CardBack is NULL. CardBackID: " + data.CardBackID);
			return;
		}
		PlayerSaveData saveData = player.SaveData;
		if (saveData.UnlockedCardBacks == null)
		{
			Debug.LogError("BuyCardBackExecute: UnlockedCardBacks is NULL.");
			return;
		}
		for (int i = 0; i < data.Quantity; i++)
		{
			if (!saveData.UnlockedCardBacks.Contains(data.CardBack))
			{
				saveData.UnlockedCardBacks.Add(data.CardBack);
				Debug.Log("CardBack unlocked: " + data.CardBackID);
			}
			else
			{
				Debug.LogWarning("CardBack already unlocked: " + data.CardBackID);
			}
		}
	}

	public void OnClickBuyCardBack(StoreBuyCardBackData data)
	{
		if (data == null)
		{
			Debug.LogError("OnClickBuyCardBack: data is NULL.");
			return;
		}
		if (data.CardBack == null)
		{
			Debug.LogError("OnClickBuyCardBack: CardBack is NULL. " + data.CardBackID);
			return;
		}
		PlayerInfoScript instance = Singleton<PlayerInfoScript>.Instance;
		if (instance == null)
		{
			Debug.LogError("OnClickBuyCardBack: PlayerInfoScript is NULL.");
			return;
		}
		if (instance.SaveData.HardCurrency < data.BuyCost)
		{
			Singleton<SimplePopupController>.Instance.ShowMessage(string.Empty, "Not enough Gems.");
			return;
		}
		mPendingCardBackPurchase = data;
		mPendingCreaturePurchase = null;
		mPendingActionCardPurchase = null;
		mMonthlyStorePurchasePending = false;
		mPendingStoreBuyCost = data.BuyCost;
		Singleton<SimplePopupController>.Instance.ShowPurchasePrompt("Buy Card Back?", "Not enough currency.", data.BuyCost, ConfirmStorePurchase, cancelPopup);
	}

	public int GetCardBackBuyCount(string cardBackId)
	{
		if (string.IsNullOrEmpty(cardBackId))
		{
			Debug.LogWarning("[CardBack Store] CardBack ID is NULL or empty.");
			return 0;
		}
		PlayerInfoScript instance = Singleton<PlayerInfoScript>.Instance;
		if (instance == null)
		{
			Debug.LogError("[CardBack Store] PlayerInfoScript is NULL.");
			return 0;
		}
		if (instance.SaveData == null)
		{
			Debug.LogError("[CardBack Store] Player SaveData is NULL.");
			return 0;
		}
		if (instance.SaveData.UnlockedCardBacks == null)
		{
			Debug.LogError("[CardBack Store] UnlockedCardBacks is NULL.");
			return 0;
		}
		CardBackData data = CardBackDataManager.Instance.GetData(cardBackId);
		if (data == null)
		{
			Debug.LogError("[CardBack Store] CardBackData not found: " + cardBackId);
			return 0;
		}
		if (instance.SaveData.UnlockedCardBacks.Contains(data))
		{
			return 1;
		}
		return 0;
	}

	private void RefreshCardBackStoreUI(string cardBackId)
	{
		if (CardBackStoreGrid == null)
		{
			return;
		}
		StoreBuyCardBackScript[] componentsInChildren = CardBackStoreGrid.GetComponentsInChildren<StoreBuyCardBackScript>(includeInactive: true);
		foreach (StoreBuyCardBackScript storeBuyCardBackScript in componentsInChildren)
		{
			if (!(storeBuyCardBackScript == null))
			{
				storeBuyCardBackScript.RefreshBuyCount();
			}
		}
	}
}
