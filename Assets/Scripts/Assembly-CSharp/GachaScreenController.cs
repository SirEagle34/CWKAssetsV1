using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GachaScreenController : Singleton<GachaScreenController>
{
	private class QueuedCreatureLoad
	{
		public CreatureData Creature;

		public int ScreenIndex;

		public bool Invalidated;

		public bool Started;

		public void OnComplete(Object loadedObjData, Texture2D loadedTexture)
		{
			if (Invalidated)
			{
				loadedObjData = null;
				loadedTexture = null;
				Resources.UnloadUnusedAssets();
			}
			else
			{
				Singleton<GachaScreenController>.Instance.InstantiateLoadedCreature(loadedObjData, loadedTexture, ScreenIndex);
			}
		}
	}

	private enum NextAction
	{
		NONE = 0,
		WAITING = 1,
		PROCEED = 2,
		ERROR = 3,
		RETRY = 4
	}

	public int TestRolls;

	public float CreatureWaitTime;

	public float RotatingCardScale;

	public float RotatingCardOffset;

	public float InitialKeyMoveSpeed;

	public float InitialKeyRotateSpeed;

	public float KeyReturnSpeed;

	public float KeyLockDragDistance;

	public UITweenController EventActiveTween;

	public UITweenController[] ShowGachaMainTweens;

	public UITweenController HideGachaMainTween;

	public UITweenController RotateCreaturesTween;

	public UITweenController FadeCreatureStatsTween;

	public UITweenController ShowFeaturedCreatureTween;

	public UITweenController HideFeaturedCreatureTween;

	public UITweenController PulseStatsAndTitleTween;

	public UITweenController ShowDragKeyTween;

	public UITweenController ArrowLoopTween;

	public UITweenController HideDragKeyTween;

	public UITweenController DisableDragKeyTween;

	public UILabel GoldAvailable;

	public UILabel SoftCurrencyAvailable;

	public UILabel HardCurrencyAvailable;

	public UILabel KeysAvailable;

	public UITexture KeysAvailableTexture;

	public GameObject KeysAvailableParent;

	public UILabel CostLabel;

	public UILabel OpenLabel;

	public UISprite CostIcon;

	public UITexture CostTexture;

	public GameObject CostParent;

	public UILabel ChestTimer;

	public Transform ChestNode;

	public GameObject MultiPullParent;

	public UILabel MultiPullCostLabel;

	public UILabel MultiPullOpenLabel;

	public UISprite MultiPullCostIcon;

	public UITexture MultiPullCostTexture;

	public Transform MultiPullChestNode;

	public GameObject MultiPullBonusParent;

	public UILabel MultiPullBonusCostLabel;

	public UILabel MultiPullBonusOpenLabel;

	public UISprite MultiPullBonusCostIcon;

	public UITexture MultiPullBonusCostTexture;

	public Transform MultiPullBonusChestNode;

	public GameObject EventParent;

	public UILabel EventName;

	public UILabel EventDesc;

	public UILabel EventTimer;

	public UILabel GachaTypeLabel;

	public UILabel TypeDescription;

	public UILabel EggCountStackLabel;

	public UILabel BonusEggCountStackLabel;

	public UIGrid ButtonGrid;

	public UILabel NextSlotName;

	public UILabel PrevSlotName;

	public UISprite NextSlotChestIcon;

	public UISprite PrevSlotChestIcon;

	public Transform[] RotatingCreatureNodes;

	public Camera CreatureCamera;

	public GameObject RotatingStatsContentsParent;

	public CreatureStatsPanel[] RotatingStatsTypes;

	public Transform FeaturedCreatureNode;

	public CreatureStatsPanel FeaturedCreatureStats;

	public GachaDragKey KeyDragObject;

	public Transform KeyParentObject;

	public Transform KeyEndPosition;

	public Transform Panel3D;

	public GameObject rareFXParent;

	public GameObject algebraicFXParent;

	public string DailyChestID = string.Empty;

	private GachaEventDataManager.EventStatus mCurrentEvent;

	private int mMaxCount;

	private List<GachaSlotData> mAvailableSlots = new List<GachaSlotData>();

	private int mDisplayedSlotIndex;

	private string mFreeString;

	private Coroutine mCreatureRotation;

	private int mRotatingCreatureIndex;

	private InventorySlotItem mCurrentRotatingCreature;

	private GameObject[] mRotatingCreatures = new GameObject[4];

	private bool showSparkleFX = true;

	private bool mInOpenSequence;

	private int currentPurchaseCount;

	private GameObject currentKeyObject;

	private List<QueuedCreatureLoad> mQueuedCreatureLoads = new List<QueuedCreatureLoad>();

	private GachaSlotData DisplayedSlot => mAvailableSlots[mDisplayedSlotIndex];

	private static int AffordableCount(int balance, int cost)
	{
		if (cost <= 0)
		{
			return 1;
		}
		return balance / cost;
	}

	private void Awake()
	{
		KeyDragObject.gameObject.SetActive(value: false);
		StartCoroutine(CreatureLoader());
	}

	public void Populate()
	{
		mFreeString = KFFLocalization.Get("!!FREE_CHEST");
		mAvailableSlots.Clear();
		foreach (GachaSlotData item in GachaSlotDataManager.Instance.GetDatabase())
		{
			if (item.ConditionsMet())
			{
				mAvailableSlots.Add(item);
			}
		}
		if (mAvailableSlots.Count != 0)
		{
			mDisplayedSlotIndex = GetInitialSlot(DailyChestID);
			DailyChestID = string.Empty;
			PopulateData();
			PlayGachaTweenIn();
			Singleton<SLOTAudioManager>.Instance.PlaySound("gacha/SFX_Gacha_Enter2");
			Singleton<SLOTAudioManager>.Instance.PlaySound("gacha/SFX_GachaAmbience");
			EnableSparkleFX(tof: true);
		}
	}

	private int GetInitialSlot(string inChestID)
	{
		if (!string.IsNullOrEmpty(inChestID))
		{
			for (int i = 0; i < mAvailableSlots.Count; i++)
			{
				if (mAvailableSlots[i].ID == inChestID)
				{
					return i;
				}
			}
		}
		return 1;
	}

	public void PopulateData()
	{
		DestroySpawnedObjects();
		CreatureCamera.enabled = true;
		GachaSlotData displayedSlot = DisplayedSlot;
		GachaTypeLabel.text = displayedSlot.Name;
		TypeDescription.text = displayedSlot.Description;
		int index = (mDisplayedSlotIndex + 1).PositiveMod(mAvailableSlots.Count);
		NextSlotName.text = mAvailableSlots[index].Name;
		PackSprite.Apply(NextSlotChestIcon, mAvailableSlots[index].ChestSprite);
		int index2 = (mDisplayedSlotIndex - 1).PositiveMod(mAvailableSlots.Count);
		PrevSlotName.text = mAvailableSlots[index2].Name;
		PackSprite.Apply(PrevSlotChestIcon, mAvailableSlots[index2].ChestSprite);
		string text = null;
		string text2 = null;
		int num = Singleton<PlayerInfoScript>.Instance.GachaKeyCount(displayedSlot);
		if (num > 0)
		{
			mMaxCount = num;
			if (displayedSlot.CurrencyType == DropTypeEnum.SoftCurrency || displayedSlot.CurrencyType == DropTypeEnum.SocialCurrency)
			{
				mMaxCount = Mathf.Min(mMaxCount, MiscParams.StandardGachaMultiPullLimit);
			}
			else if (displayedSlot.CurrencyType == DropTypeEnum.HardCurrency)
			{
				mMaxCount = Mathf.Min(mMaxCount, MiscParams.PremiumGachaMultiPullLimit);
			}
			OpenLabel.text = KFFLocalization.Get("!!OPEN") + " 1";
			CostLabel.text = "1 " + KFFLocalization.Get("!!KEY");
			text2 = displayedSlot.KeyUITexture;
		}
		else if (displayedSlot.CurrencyType == DropTypeEnum.SoftCurrency)
		{
			CostLabel.text = displayedSlot.Cost.ToString();
			text = "Icon_Currency_Soft";
			OpenLabel.text = KFFLocalization.Get("!!OPEN") + " 1";
			mMaxCount = AffordableCount(Singleton<PlayerInfoScript>.Instance.SaveData.SoftCurrency, displayedSlot.Cost);
		}
		else if (displayedSlot.CurrencyType == DropTypeEnum.SocialCurrency)
		{
			CostLabel.text = displayedSlot.Cost.ToString();
			text = "Icon_Currency_PVPCurrency";
			OpenLabel.text = KFFLocalization.Get("!!OPEN") + " 1";
			mMaxCount = AffordableCount(Singleton<PlayerInfoScript>.Instance.SaveData.PvPCurrency, displayedSlot.Cost);
		}
		else if (displayedSlot.CurrencyType == DropTypeEnum.HardCurrency)
		{
			CostLabel.text = displayedSlot.Cost.ToString();
			text = "Icon_Currency_Hard";
			OpenLabel.text = KFFLocalization.Get("!!OPEN") + " 1";
			mMaxCount = AffordableCount(Singleton<PlayerInfoScript>.Instance.SaveData.HardCurrency, displayedSlot.Cost);
			mMaxCount = Mathf.Min(mMaxCount, MiscParams.PremiumGachaMultiPullLimit);
		}
		else
		{
			num = -1;
			mMaxCount = 1;
		}
		RefreshKeys(num, displayedSlot);
		if (text != null)
		{
			CostTexture.gameObject.SetActive(value: false);
			MultiPullCostTexture.gameObject.SetActive(value: false);
			MultiPullBonusCostTexture.gameObject.SetActive(value: false);
			CostIcon.gameObject.SetActive(value: true);
			MultiPullCostIcon.gameObject.SetActive(value: true);
			MultiPullBonusCostIcon.gameObject.SetActive(value: true);
			UISprite costIcon = CostIcon;
			string text3 = text;
			MultiPullBonusCostIcon.spriteName = text3;
			text3 = text3;
			MultiPullCostIcon.spriteName = text3;
			costIcon.spriteName = text3;
		}
		else if (text2 != null)
		{
			CostIcon.gameObject.SetActive(value: false);
			MultiPullCostIcon.gameObject.SetActive(value: false);
			MultiPullBonusCostIcon.gameObject.SetActive(value: false);
			CostTexture.gameObject.SetActive(value: true);
			MultiPullCostTexture.gameObject.SetActive(value: true);
			MultiPullBonusCostTexture.gameObject.SetActive(value: true);
			CostTexture.ReplaceTexture(text2);
			MultiPullCostTexture.ReplaceTexture(text2);
			MultiPullBonusCostTexture.ReplaceTexture(text2);
		}
		else
		{
			CostIcon.gameObject.SetActive(value: false);
			MultiPullCostIcon.gameObject.SetActive(value: false);
			MultiPullBonusCostIcon.gameObject.SetActive(value: false);
			CostTexture.gameObject.SetActive(value: false);
			MultiPullCostTexture.gameObject.SetActive(value: false);
			MultiPullBonusCostTexture.gameObject.SetActive(value: false);
		}
		InstantiateKey(ChestNode, displayedSlot);
		if (mMaxCount > 2)
		{
			int num2 = Mathf.RoundToInt((float)mMaxCount / 2f);
			MultiPullParent.SetActive(value: true);
			EggCountStackLabel.text = "x" + num2;
			if (num > 0)
			{
				MultiPullCostLabel.text = num2 + " " + KFFLocalization.Get("!!KEYS");
			}
			else
			{
				MultiPullCostLabel.text = (num2 * displayedSlot.Cost).ToString();
			}
			MultiPullOpenLabel.text = KFFLocalization.Get("!!OPEN") + " " + num2;
			InstantiateKey(MultiPullChestNode, displayedSlot);
		}
		else
		{
			MultiPullParent.SetActive(value: false);
		}
		if (mMaxCount > 1)
		{
			MultiPullBonusParent.SetActive(value: true);
			BonusEggCountStackLabel.text = "x" + mMaxCount;
			if (num > 0)
			{
				MultiPullBonusCostLabel.text = mMaxCount + " " + KFFLocalization.Get("!!KEYS");
			}
			else
			{
				MultiPullBonusCostLabel.text = (mMaxCount * displayedSlot.Cost).ToString();
			}
			MultiPullBonusOpenLabel.text = KFFLocalization.Get("!!OPEN") + " " + mMaxCount;
			InstantiateKey(MultiPullBonusChestNode, displayedSlot);
		}
		else
		{
			MultiPullBonusParent.SetActive(value: false);
		}
		ButtonGrid.gameObject.SetActive(value: true);
		ButtonGrid.Reposition();
		RefreshCurrencies();
		RefreshEventStatus();
		ButtonGrid.transform.localPosition = new Vector3(0f, ButtonGrid.transform.localPosition.y, 0f);
		if (displayedSlot.FeaturedCreature != null)
		{
			FeaturedCreatureStats.gameObject.SetActive(value: true);
			FeaturedCreatureStats.ShouldTweenInRarityStars = true;
			FeaturedCreatureStats.Populate(new InventorySlotItem(new CreatureItem(displayedSlot.FeaturedCreature)));
			QueueCreatureLoad(displayedSlot.FeaturedCreature, -1);
			ShowFeaturedCreatureTween.Play();
			FeaturedCreatureStats.TweenInRarityStars();
			if (mMaxCount > 1)
			{
				ButtonGrid.transform.localPosition = new Vector3(50f, ButtonGrid.transform.localPosition.y, 0f);
			}
		}
		else if (FeaturedCreatureStats.gameObject.activeSelf)
		{
			HideFeaturedCreatureTween.PlayWithCallback(delegate
			{
				FeaturedCreatureStats.gameObject.SetActive(value: false);
			});
		}
		RotateCreaturesTween.StopAndReset();
		if (mCreatureRotation != null)
		{
			StopCoroutine(mCreatureRotation);
			mCreatureRotation = null;
		}
		if (displayedSlot.RotatingCreatures.Count > 0)
		{
			mRotatingCreatureIndex = Random.Range(0, displayedSlot.RotatingCreatures.Count);
			OnDisplayedCreatureChanged();
			if (mCurrentRotatingCreature == null)
			{
				RotatingStatsContentsParent.SetActive(value: false);
			}
			SwapCreatureStats();
			mCreatureRotation = StartCoroutine(CreatureRotation());
		}
		Singleton<TutorialController>.Instance.AdvanceIfTargetingBuilding("TBuilding_Gacha");
	}

	private void InstantiateKey(Transform node, GachaSlotData slot)
	{
		GameObject obj = node.InstantiateAsChild(Singleton<PrefabReferences>.Instance.GachaKeys[slot.ChestType - 1]);
		obj.ChangeLayer(node.gameObject.layer);
		obj.GetComponent<GachaKeyObject>().IdleFX.ChangeLayer(LayerMask.NameToLayer("Particle"));
	}

	private void Update()
	{
		if (SoftCurrencyAvailable.gameObject.activeInHierarchy)
		{
			RefreshEventStatus();
			RefreshCurrencies();
		}
		if (!showSparkleFX && !Singleton<CreatureDetailsController>.Instance.StatsPanel.isActiveAndEnabled && !Singleton<SimplePopupController>.Instance.MainPanel.activeInHierarchy && !Singleton<BuyInventoryPopupController>.Instance.MainPanel.activeInHierarchy && !Singleton<StoreScreenController>.Instance.MainPanel.activeInHierarchy)
		{
			EnableSparkleFX(tof: true);
		}
	}

	private void RefreshCurrencies()
	{
		PlayerSaveData saveData = Singleton<PlayerInfoScript>.Instance.SaveData;
		GoldAvailable.text = saveData.SoftCurrency.ToString();
		SoftCurrencyAvailable.text = saveData.PvPCurrency.ToString();
		HardCurrencyAvailable.text = saveData.HardCurrency.ToString();
	}

	private void RefreshKeys(int count, GachaSlotData slot)
	{
		if (!string.IsNullOrEmpty(slot.KeyUITexture))
		{
			KeysAvailableParent.SetActive(value: true);
			KeysAvailableTexture.ReplaceTexture(slot.KeyUITexture);
			KeysAvailable.text = count.ToString();
		}
		else
		{
			KeysAvailableParent.SetActive(value: false);
		}
	}

	private void RefreshEventStatus()
	{
		GachaSlotData displayedSlot = DisplayedSlot;
		mCurrentEvent = GachaEventDataManager.Instance.GetCurrentEvent(displayedSlot);
		if (mCurrentEvent != null)
		{
			EventParent.gameObject.SetActive(value: true);
			EventTimer.gameObject.SetActive(value: true);
			EventName.text = mCurrentEvent.Event.Name;
			EventDesc.text = mCurrentEvent.Event.Description;
			EventTimer.text = mCurrentEvent.DisplayText;
			if (mCurrentEvent.Active)
			{
				if (!EventActiveTween.AnyTweenPlaying())
				{
					EventActiveTween.Play();
				}
			}
			else if (EventActiveTween.AnyTweenPlaying())
			{
				EventActiveTween.StopAndReset();
			}
		}
		else
		{
			EventParent.gameObject.SetActive(value: false);
			EventTimer.gameObject.SetActive(value: false);
		}
		int gachaSlotCooldownSeconds = Singleton<PlayerInfoScript>.Instance.GetGachaSlotCooldownSeconds(displayedSlot);
		if (gachaSlotCooldownSeconds > 0)
		{
			CostParent.SetActive(value: false);
			ChestTimer.gameObject.SetActive(value: true);
			ChestTimer.text = PlayerInfoScript.FormatTimeString(gachaSlotCooldownSeconds, showSeconds: true);
		}
		else if (displayedSlot.CurrencyType == DropTypeEnum.SoftCurrency || displayedSlot.CurrencyType == DropTypeEnum.SocialCurrency || displayedSlot.CurrencyType == DropTypeEnum.HardCurrency)
		{
			CostParent.SetActive(value: true);
			ChestTimer.gameObject.SetActive(value: false);
		}
		else
		{
			CostParent.SetActive(value: false);
			ChestTimer.gameObject.SetActive(value: true);
			ChestTimer.text = mFreeString;
		}
	}

	private void ExpandInventory()
	{
		StartCoroutine(ExpandInventoryCo());
	}

	private IEnumerator ExpandInventoryCo()
	{
		PlayerSaveData saveData = Singleton<PlayerInfoScript>.Instance.SaveData;
		Singleton<BusyIconPanelController>.Instance.Show();
		bool waiting = true;
		bool success = false;
		saveData.Purchase("inventory", string.Empty, 1, delegate(PlayerSaveData.ActionResult result)
		{
			waiting = false;
			success = result.success;
		});
		while (waiting)
		{
			yield return null;
		}
		Singleton<BusyIconPanelController>.Instance.Hide();
		if (!success)
		{
			Singleton<SimplePopupController>.Instance.ShowMessage(string.Empty, KFFLocalization.Get("!!SERVER_ERROR_MESSAGE"));
			EnableSparkleFX(tof: false);
			yield break;
		}
		saveData.AddEmptyInventorySlots(MiscParams.InventorySpacePerPurchase);
		Singleton<BuyInventoryPopupController>.Instance.Show();
		PopulateData();
		EnableSparkleFX(tof: false);
	}

	public void OnClickOpen()
	{
		StartCoroutine(StartGacha(Mathf.Min(1, mMaxCount), ChestNode.GetChild(0).gameObject));
	}

	public void OnClickOpenMax()
	{
		StartCoroutine(StartGacha(Mathf.RoundToInt((float)mMaxCount / 2f), MultiPullChestNode.GetChild(0).gameObject));
	}

	public void OnClickOpenMaxBonus()
	{
		StartCoroutine(StartGacha(mMaxCount, MultiPullBonusChestNode.GetChild(0).gameObject));
	}

	private IEnumerator StartGacha(int count, GameObject keyObject)
	{
		currentPurchaseCount = count;
		currentKeyObject = keyObject;
		if (count == 0)
		{
			Singleton<SimplePopupController>.Instance.ShowMessage(string.Empty, KFFLocalization.Get("!!ERROR_GACHA_NOT_ENOUGH_CURRENCY"));
			EnableSparkleFX(tof: false);
			yield break;
		}
		PlayerSaveData saveData = Singleton<PlayerInfoScript>.Instance.SaveData;
		if (saveData.IsInventorySpaceFull())
		{
			Singleton<SimplePopupController>.Instance.ShowPurchasePrompt(KFFLocalization.Get("!!INVENTORY_FULL_BUY").Replace("<val2>", MiscParams.InventorySpacePerPurchase.ToString()), KFFLocalization.Get("!!INVENTORY_FULL_NOBUY"), MiscParams.InventorySpacePurchaseCost, ExpandInventory);
			Singleton<SLOTAudioManager>.Instance.PlayErrorSound();
			EnableSparkleFX(tof: false);
			yield break;
		}
		GachaSlotData displayedSlot = DisplayedSlot;
		if (Singleton<PlayerInfoScript>.Instance.GetGachaSlotCooldownSeconds(displayedSlot) > 0)
		{
			Singleton<SimplePopupController>.Instance.ShowMessage(string.Empty, KFFLocalization.Get("!!ERROR_GACHA_ON_COOLDOWN"));
			EnableSparkleFX(tof: false);
			yield break;
		}
		if (displayedSlot.CooldownHours > 0)
		{
			saveData.GachaSlotCooldowns.Add(displayedSlot, (uint)(TFUtils.ServerTime.UnixTimestamp() + displayedSlot.CooldownHours * 60 * 60));
			ConfirmedPurchase();
			yield break;
		}
		int num = Singleton<PlayerInfoScript>.Instance.GachaKeyCount(DisplayedSlot);
		string text = string.Empty;
		int num2 = 0;
		if (num > 0)
		{
			text = KFFLocalization.Get("!!KEYS");
			num2 = num;
		}
		else if (displayedSlot.CurrencyType == DropTypeEnum.SoftCurrency)
		{
			text = KFFLocalization.Get("!!CURRENCY_SOFT");
			num2 = saveData.SoftCurrency;
		}
		else if (displayedSlot.CurrencyType == DropTypeEnum.SocialCurrency)
		{
			text = KFFLocalization.Get("!!CURRENCY_PVP");
			num2 = saveData.PvPCurrency;
		}
		else if (displayedSlot.CurrencyType == DropTypeEnum.HardCurrency)
		{
			text = KFFLocalization.Get("!!CURRENCY_HARD");
			num2 = saveData.HardCurrency;
		}
		_ = string.Empty;
		string text2 = ((count <= 1) ? KFFLocalization.Get("!!BUY_GACHA_CHEST1") : KFFLocalization.Get("!!BUY_GACHA_CHESTS"));
		text2 = text2.Replace("<val1>", ((num <= 0) ? (displayedSlot.Cost * count) : count).ToString());
		text2 = text2.Replace("<val2>", text);
		text2 = text2.Replace("<val3>", count.ToString());
		string text3 = text2;
		text2 = text3 + "\n\n" + KFFLocalization.Get("!!YOUR_BALANCE") + ": " + num2 + " " + text;
		Singleton<SimplePopupController>.Instance.ShowPrompt(KFFLocalization.Get("!!CONFIRM_PURCHASE"), text2, ConfirmedPurchase, null);
		EnableSparkleFX(tof: false);
	}

	private void ConfirmedPurchase()
	{
		StartCoroutine(ContinueGacha());
	}

	private IEnumerator ContinueGacha()
	{
		GachaSlotData slot = DisplayedSlot;
		PlayerSaveData saveData = Singleton<PlayerInfoScript>.Instance.SaveData;
		int count = currentPurchaseCount;
		GachaKeyObject keyScript = currentKeyObject.GetComponent<GachaKeyObject>();
		string pullSource = "free";
		if (Singleton<PlayerInfoScript>.Instance.GachaKeyCount(slot) > 0)
		{
			pullSource = "key";
			Singleton<PlayerInfoScript>.Instance.ConsumeGachaKey(slot, count);
		}
		else if (slot.CurrencyType == DropTypeEnum.SoftCurrency || slot.CurrencyType == DropTypeEnum.SocialCurrency || slot.CurrencyType == DropTypeEnum.HardCurrency)
		{
			if (slot.CurrencyType != DropTypeEnum.SoftCurrency)
			{
				if (slot.CurrencyType != DropTypeEnum.SocialCurrency)
				{
					_ = saveData.HardCurrency;
				}
				else
				{
					_ = saveData.PvPCurrency;
				}
			}
			else
			{
				_ = saveData.SoftCurrency;
			}
			Singleton<BusyIconPanelController>.Instance.Show();
			bool waiting = true;
			bool success = false;
			pullSource = "paid";
			saveData.Purchase("gacha", slot.ID, count, delegate(PlayerSaveData.ActionResult result)
			{
				waiting = false;
				success = result.success;
			});
			while (waiting)
			{
				yield return null;
			}
			Singleton<BusyIconPanelController>.Instance.Hide();
			if (!success)
			{
				Singleton<SimplePopupController>.Instance.ShowMessage(string.Empty, KFFLocalization.Get("!!SERVER_ERROR_MESSAGE"));
				EnableSparkleFX(tof: false);
				yield break;
			}
		}
		List<InventorySlotItem> results = new List<InventorySlotItem>();
		PlayerSaveData.BeginGrant();
		for (int num = 0; num < count; num++)
		{
			results.Add(SpinGacha(slot));
		}
		saveData.ClaimCurrency("gacha pull", slot.ID + ":" + count + ":" + pullSource, 0, 0, 0, 0, null, PlayerSaveData.EndGrant());
		ChestSkins.Current = slot.ChestSkin;
		Singleton<PlayerInfoScript>.Instance.Save();
		mInOpenSequence = true;
		KeyDragObject.gameObject.SetActive(value: true);
		KeyDragObject.transform.localPosition = Vector3.zero;
		keyScript.transform.SetParent(KeyParentObject);
		keyScript.gameObject.ChangeLayerToParent();
		keyScript.IdleFX.ChangeLayer(LayerMask.NameToLayer("Particle"));
		HideGachaMainTween.Play();
		UICamera.LockInput();
		Singleton<TownController>.Instance.GachaKeyholeEffect.SetActive(value: true);
		StartCoroutine(MoveKeyToStartPos(keyScript.gameObject));
		yield return new WaitForSeconds(0.25f);
		ShowDragKeyTween.Play();
		Singleton<SLOTAudioManager>.Instance.PlaySound("gacha/SFX_GachaKey");
		ArrowLoopTween.Play();
		UICamera.UnlockInput();
		while (!((KeyDragObject.transform.position - KeyEndPosition.position).sqrMagnitude < KeyLockDragDistance * KeyLockDragDistance))
		{
			if (KeyDragObject.Pressed)
			{
				keyScript.DragFX.SetActive(value: true);
			}
			else
			{
				keyScript.DragFX.SetActive(value: false);
				KeyDragObject.transform.localPosition = Vector3.Lerp(KeyDragObject.transform.localPosition, Vector3.zero, KeyReturnSpeed * Time.deltaTime);
			}
			yield return null;
		}
		Singleton<SLOTAudioManager>.Instance.PlaySound("gacha/SFX_GachaKeyUnlock");
		HideDragKeyTween.Play();
		keyScript.DragFX.SetActive(value: false);
		yield return StartCoroutine(Singleton<TownController>.Instance.PlayGachaGateOpenAnim(keyScript.gameObject));
		yield return StartCoroutine(Singleton<TownController>.Instance.SpawnGachaPassByChests(results));
		Singleton<TownController>.Instance.GachaKeyholeEffect.SetActive(value: false);
		KeyDragObject.gameObject.SetActive(value: false);
		Object.Destroy(keyScript.gameObject);
		DisableDragKeyTween.Play();
		Singleton<TownController>.Instance.ResetGachaGate();
		if (results.Count == 1)
		{
			Singleton<GachaOpenSequencer>.Instance.ShowGachaSequence(results[0]);
			while (Singleton<GachaOpenSequencer>.Instance.Showing)
			{
				yield return null;
			}
		}
		else
		{
			Singleton<GachaMultiResultController>.Instance.ShowMultiEggPanel(results);
			while (Singleton<GachaMultiResultController>.Instance.Showing)
			{
				yield return null;
			}
		}
		PlayGachaTweenIn();
		Singleton<SLOTAudioManager>.Instance.PlaySound("gacha/SFX_Gacha_Enter2");
		PopulateData();
		if (Singleton<TutorialController>.Instance.IsBlockActive("UseGacha"))
		{
			Singleton<TutorialController>.Instance.AdvanceTutorialState();
		}
		ChestSkins.Current = null;
		mInOpenSequence = false;
	}

	private IEnumerator MoveKeyToStartPos(GameObject key)
	{
		while (true)
		{
			key.transform.localPosition = Vector3.Lerp(key.transform.localPosition, Vector3.zero, InitialKeyMoveSpeed * Time.deltaTime);
			key.transform.localRotation = Quaternion.Lerp(key.transform.localRotation, Quaternion.identity, InitialKeyRotateSpeed * Time.deltaTime);
			key.transform.localScale = Vector3.Lerp(key.transform.localScale, Vector3.one, InitialKeyMoveSpeed * Time.deltaTime);
			if (key.transform.localPosition.magnitude < 0.001f)
			{
				break;
			}
			yield return null;
		}
		key.transform.localPosition = Vector3.zero;
		key.transform.localRotation = Quaternion.identity;
		key.transform.localScale = Vector3.one;
	}

	private void PlayGachaTweenIn()
	{
		for (int i = 0; i < ShowGachaMainTweens.Length; i++)
		{
			ShowGachaMainTweens[i].Play();
		}
	}

	private InventorySlotItem SpinGacha(GachaSlotData gachaSlot)
	{
		_ = Singleton<PlayerInfoScript>.Instance.SaveData;
		GachaWeightTable gachaWeightTable = null;
		GachaWeightEntry gachaWeightEntry = null;
		if (Singleton<TutorialController>.Instance.IsBlockActive("UseGacha"))
		{
			gachaWeightTable = GachaWeightDataManager.Instance.GetData("Tutorial");
			Singleton<PlayerInfoScript>.Instance.UnlockFeature("TBuilding_Store");
			Singleton<TownController>.Instance.CheckUnlockedBuildings();
			Singleton<TownController>.Instance.RefreshLockedBuildingEffect();
			Singleton<TownHudController>.Instance.PopulatePlayerInfo();
			if (gachaWeightTable == null)
			{
				return null;
			}
			gachaWeightEntry = gachaWeightTable.TutorialSpin();
		}
		else
		{
			gachaWeightTable = ((mCurrentEvent == null || !mCurrentEvent.Active) ? gachaSlot.Table : mCurrentEvent.Event.WeightsTable);
			if (gachaWeightTable == null)
			{
				return null;
			}
			gachaWeightEntry = gachaWeightTable.Spin();
		}
		InventorySlotItem result = null;
		if (CreatureDataManager.Instance.GetData(gachaWeightEntry.DropID) != null)
		{
			CreatureItem creatureItem = new CreatureItem(gachaWeightEntry.DropID);
			if (Singleton<TutorialController>.Instance.IsBlockActive("UseGacha"))
			{
				creatureItem.SetStarRatingFromGacha(1);
			}
			else if (gachaWeightEntry.StarOverride > 0)
			{
				creatureItem.SetStarRatingFromGacha(gachaWeightEntry.StarOverride);
			}
			else
			{
				int num = 0;
				foreach (int starWeight in gachaSlot.StarWeights)
				{
					num += starWeight;
				}
				int num2 = Random.Range(0, num);
				int starRatingFromGacha = 1;
				for (int i = 0; i < gachaSlot.StarWeights.Count; i++)
				{
					num2 -= gachaSlot.StarWeights[i];
					if (num2 < 0)
					{
						starRatingFromGacha = i + 1;
						break;
					}
				}
				creatureItem.SetStarRatingFromGacha(starRatingFromGacha);
			}
			if (mCurrentEvent != null && mCurrentEvent.Active && (mCurrentEvent.Event.BonusClassRestriction == CreatureFaction.Count || mCurrentEvent.Event.BonusClassRestriction == creatureItem.Form.Faction))
			{
				if (mCurrentEvent.Event.BonusLevels > 0)
				{
					int num3 = creatureItem.Level + mCurrentEvent.Event.BonusLevels;
					if (num3 < creatureItem.MaxLevel)
					{
						creatureItem.Xp = creatureItem.XPTable.GetXpToReachLevel(num3);
						int maxExclusive = creatureItem.XPTable.GetXpToReachLevel(num3 + 1) - (int)creatureItem.Xp;
						creatureItem.Xp = (int)creatureItem.Xp + Random.Range(0, maxExclusive);
					}
					else
					{
						creatureItem.Xp = creatureItem.XPTable.GetXpToReachLevel(creatureItem.MaxLevel);
					}
				}
				creatureItem.PassiveSkillLevel += mCurrentEvent.Event.BonusPassiveLevels;
			}
			result = Singleton<PlayerInfoScript>.Instance.SaveData.AddCreature(creatureItem);
		}
		else
		{
			XPMaterialData data = XPMaterialDataManager.Instance.GetData(gachaWeightEntry.DropID);
			if (data != null)
			{
				result = Singleton<PlayerInfoScript>.Instance.SaveData.AddXPMaterial(data);
			}
		}
		Singleton<PlayerInfoScript>.Instance.UpdateBadgeCount(BadgeEnum.Evo);
		return result;
	}

	public void Unload()
	{
		Singleton<TownController>.Instance.ShowGachaOpeningVFX(toogleOn: false);
		Singleton<MouseOrbitCamera>.Instance.CheckTiltCamSettingBeforeTutorial();
		if (mCreatureRotation != null)
		{
			StopCoroutine(mCreatureRotation);
			mCreatureRotation = null;
		}
		DestroySpawnedObjects();
		Resources.UnloadUnusedAssets();
		if (!mInOpenSequence)
		{
			CreatureCamera.enabled = false;
			Singleton<TownHudController>.Instance.ReturnToTownView();
			Singleton<SLOTAudioManager>.Instance.StopSound("SFX_GachaAmbience");
			Singleton<TownController>.Instance.GachaAnimator.Play("Idle");
		}
	}

	private void DestroySpawnedObjects()
	{
		ChestNode.DestroyAllChildren();
		MultiPullChestNode.DestroyAllChildren();
		MultiPullBonusChestNode.DestroyAllChildren();
		FeaturedCreatureNode.DestroyAllChildren();
		for (int i = 0; i < mRotatingCreatures.Length; i++)
		{
			if (mRotatingCreatures[i] != null)
			{
				Object.Destroy(mRotatingCreatures[i]);
				mRotatingCreatures[i] = null;
			}
		}
		mQueuedCreatureLoads.RemoveAll((QueuedCreatureLoad m) => !m.Started);
		foreach (QueuedCreatureLoad mQueuedCreatureLoad in mQueuedCreatureLoads)
		{
			mQueuedCreatureLoad.Invalidated = true;
		}
	}

	public void OnClickPrevSlot()
	{
		OnClickChangeSlot(-1);
	}

	public void OnClickNextSlot()
	{
		OnClickChangeSlot(1);
		Singleton<SLOTAudioManager>.Instance.PlaySound("gacha/SFX_Gacha_Arrow");
	}

	private void OnClickChangeSlot(int direction)
	{
		mDisplayedSlotIndex = (mDisplayedSlotIndex + direction).PositiveMod(mAvailableSlots.Count);
		PulseStatsAndTitleTween.Play();
		PopulateData();
	}

	private IEnumerator CreatureRotation()
	{
		while (true)
		{
			if (DisplayedSlot.RotatingCreatures.Count == 0)
			{
				yield return null;
				continue;
			}
			int num = 1;
			while (true)
			{
				if (mRotatingCreatures[num] == null)
				{
					string iD = DisplayedSlot.RotatingCreatures[(mRotatingCreatureIndex + num) % DisplayedSlot.RotatingCreatures.Count];
					CreatureData data = CreatureDataManager.Instance.GetData(iD);
					if (data != null)
					{
						QueueCreatureLoad(data, num);
					}
					else
					{
						XPMaterialData data2 = XPMaterialDataManager.Instance.GetData(iD);
						if (data2 != null)
						{
							CardPrefabScript component = RotatingCreatureNodes[num].InstantiateAsChild(Singleton<PrefabReferences>.Instance.Card).GetComponent<CardPrefabScript>();
							component.gameObject.ChangeLayerToParent();
							component.transform.localPosition = new Vector3(0f, RotatingCardOffset, 0f);
							component.transform.rotation = Quaternion.identity;
							component.transform.localScale = new Vector3(RotatingCardScale, RotatingCardScale, 1f);
							component.InGachaDisplay = true;
							component.Mode = CardPrefabScript.CardMode.GeneralFrontEnd;
							component.CardCollider.enabled = false;
							component.Populate(data2);
							mRotatingCreatures[num] = component.gameObject;
							if (num == 3)
							{
								mRotatingCreatures[num].SetActive(value: false);
							}
						}
					}
				}
				switch (num)
				{
				case 1:
					num = 0;
					continue;
				case 0:
					num = 2;
					continue;
				case 2:
					num = 3;
					continue;
				}
				break;
			}
			while (mQueuedCreatureLoads.Count > 0)
			{
				yield return null;
			}
			yield return new WaitForSeconds(CreatureWaitTime);
			mRotatingCreatureIndex = (mRotatingCreatureIndex + 1) % DisplayedSlot.RotatingCreatures.Count;
			OnDisplayedCreatureChanged();
			FadeCreatureStatsTween.Play();
			if (mCurrentRotatingCreature == null)
			{
				RotatingStatsContentsParent.SetActive(value: false);
			}
			mRotatingCreatures[mRotatingCreatures.Length - 1].SetActive(value: true);
			SetToIdle(mRotatingCreatures[mRotatingCreatures.Length - 1]);
			bool finished = false;
			RotateCreaturesTween.PlayWithCallback(delegate
			{
				finished = true;
			});
			while (!finished)
			{
				yield return null;
			}
			RotateCreaturesTween.StopAndReset();
			Object.Destroy(mRotatingCreatures[0]);
			for (num = 0; num < mRotatingCreatures.Length - 1; num++)
			{
				Vector3 localPosition = mRotatingCreatures[num + 1].transform.localPosition;
				mRotatingCreatures[num] = mRotatingCreatures[num + 1];
				mRotatingCreatures[num].transform.SetParent(RotatingCreatureNodes[num]);
				mRotatingCreatures[num].transform.localPosition = localPosition;
			}
			mRotatingCreatures[mRotatingCreatures.Length - 1] = null;
		}
	}

	private void OnDisplayedCreatureChanged()
	{
		string iD = DisplayedSlot.RotatingCreatures[(mRotatingCreatureIndex + 1) % DisplayedSlot.RotatingCreatures.Count];
		CreatureData data = CreatureDataManager.Instance.GetData(iD);
		if (data != null)
		{
			mCurrentRotatingCreature = new InventorySlotItem(new CreatureItem(data));
		}
		else
		{
			mCurrentRotatingCreature = null;
		}
	}

	private IEnumerator CreatureLoader()
	{
		while (true)
		{
			if (mQueuedCreatureLoads.Count > 0)
			{
				QueuedCreatureLoad load = mQueuedCreatureLoads[0];
				load.Started = true;
				yield return StartCoroutine(Singleton<SLOTResourceManager>.Instance.LoadCreatureResources(load.Creature, load.OnComplete, lockInput: false));
				mQueuedCreatureLoads.Remove(load);
			}
			else
			{
				yield return null;
			}
		}
	}

	private void QueueCreatureLoad(CreatureData creature, int screenIndex)
	{
		QueuedCreatureLoad queuedCreatureLoad = new QueuedCreatureLoad();
		queuedCreatureLoad.Creature = creature;
		queuedCreatureLoad.ScreenIndex = screenIndex;
		mQueuedCreatureLoads.Add(queuedCreatureLoad);
	}

	public void InstantiateLoadedCreature(Object loadedObjData, Texture2D loadedTexture, int screenIndex)
	{
		if (screenIndex == -1)
		{
			GameObject gameObject = FeaturedCreatureNode.InstantiateAsChild(loadedObjData as GameObject);
			ReplaceTexture(gameObject, loadedTexture);
			gameObject.ChangeLayer(FeaturedCreatureNode.gameObject.layer);
			return;
		}
		GameObject gameObject2 = RotatingCreatureNodes[screenIndex].InstantiateAsChild(loadedObjData as GameObject);
		ReplaceTexture(gameObject2, loadedTexture);
		gameObject2.ChangeLayer(RotatingCreatureNodes[screenIndex].gameObject.layer);
		mRotatingCreatures[screenIndex] = gameObject2;
		if (screenIndex == mRotatingCreatures.Length - 1)
		{
			gameObject2.SetActive(value: false);
		}
		else
		{
			SetToIdle(gameObject2);
		}
	}

	private void ReplaceTexture(GameObject creature, Texture2D texture)
	{
		if (texture != null)
		{
			Renderer[] componentsInChildren = creature.GetComponentsInChildren<Renderer>(includeInactive: true);
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				componentsInChildren[i].material.mainTexture = texture;
			}
		}
	}

	private void SetToIdle(GameObject creature)
	{
		BattleCreatureAnimState componentInChildren = creature.GetComponentInChildren<BattleCreatureAnimState>();
		if (componentInChildren != null && componentInChildren.anim != null)
		{
			componentInChildren.anim.Play("Idle");
		}
	}

	public void SwapCreatureStats()
	{
		if (mCurrentRotatingCreature == null)
		{
			return;
		}
		for (int i = 0; i < RotatingStatsTypes.Length; i++)
		{
			if (i == mCurrentRotatingCreature.Creature.Form.Rarity - 1)
			{
				RotatingStatsTypes[i].gameObject.SetActive(value: true);
				RotatingStatsTypes[i].ShouldTweenInRarityStars = true;
				RotatingStatsTypes[i].Populate(new InventorySlotItem(new CreatureItem(mCurrentRotatingCreature.Creature.Form)));
				RotatingStatsTypes[i].TweenInRarityStars();
				if (mCurrentRotatingCreature.Creature.Form.BattleZoomSound != null)
				{
					Singleton<SLOTAudioManager>.Instance.PlaySound("creature/" + mCurrentRotatingCreature.Creature.Form.BattleZoomSound);
				}
			}
			else
			{
				RotatingStatsTypes[i].gameObject.SetActive(value: false);
			}
		}
	}

	public void ShowFeaturedCreatureInfo()
	{
		Singleton<CreatureDetailsController>.Instance.ShowCreature(FeaturedCreatureStats.CreatureItem, null, enableCardEditing: false);
		EnableSparkleFX(tof: false);
	}

	public void ShowRotatingCreatureInfo()
	{
		if (mCurrentRotatingCreature != null)
		{
			Singleton<CreatureDetailsController>.Instance.ShowCreature(mCurrentRotatingCreature, null, enableCardEditing: false);
			EnableSparkleFX(tof: false);
		}
	}

	public void EnableSparkleFX(bool tof)
	{
		showSparkleFX = tof;
		rareFXParent.SetActive(tof);
		algebraicFXParent.SetActive(tof);
	}
}
