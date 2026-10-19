using System.Collections.Generic;
using System.Net;
using System.Text;
using Allies;
using UnityEngine;

public class TradeOfferController : Singleton<TradeOfferController>
{
	private enum Mode
	{
		PickRecipient = 0,
		PickCreatures = 1
	}

	private enum PickSide
	{
		Mine = 0,
		Theirs = 1
	}

	private struct WantItem
	{
		public string CreatureId;

		public int Star;

		public WantItem(string creatureId, int star)
		{
			CreatureId = creatureId;
			Star = star;
		}
	}

	public GameObject Offer_Panel;

	public UIGrid ItemGrid;

	public GameObject ItemPrefab;

	public UILabel TitleLabel;

	public UILabel ListHeaderLabel;

	public UILabel EmptyLabel;

	public GameObject LeftTabs;

	public UILabel MineTabLabel;

	public UILabel TheirsTabLabel;

	public GameObject DealPanel;

	public GameObject[] GiveSlots;

	public GameObject[] WantSlots;

	public GameObject RightColumn;

	public GameObject RightFrame;

	public GameObject GuidePanel;

	public UILabel GiveCoinLabel;

	public UILabel WantCoinLabel;

	private const int CoinStep = 100;

	private const int WantCoinMax = 1000000;

	private const int MaxPerSide = 3;

	private Mode mMode;

	private PickSide mPickSide;

	private bool mCameFromFriendRow;

	private bool mListingMode;

	private string mRecipientId;

	private string mRecipientName;

	private readonly Dictionary<int, CreatureItem> mGive = new Dictionary<int, CreatureItem>();

	private readonly List<int> mGiveOrder = new List<int>();

	private readonly Dictionary<int, WantItem> mWant = new Dictionary<int, WantItem>();

	private readonly List<int> mWantOrder = new List<int>();

	private int mGiveCoins;

	private int mWantCoins;

	private int mCoinBalance;

	private static readonly Color TabActive = new Color(1f, 0.93f, 0.45f);

	private static readonly Color TabInactive = new Color(0.62f, 0.66f, 0.72f);

	private readonly Dictionary<Transform, Vector3> mOrigPos = new Dictionary<Transform, Vector3>();

	private readonly Dictionary<Transform, Vector3> mOrigScale = new Dictionary<Transform, Vector3>();

	private UIInput mGiveInput;

	private UIInput mWantInput;

	private bool mTyping;

	private bool mBusy;

	private float mBusySince = -1f;

	private const float kBusyMaxSeconds = 35f;

	private SQServer Server => SessionManager.Instance.theSession.Server;

	public void OpenFor(string recipientId, string recipientName)
	{
		if (EnsurePanel())
		{
			mCameFromFriendRow = true;
			mRecipientId = recipientId;
			mRecipientName = recipientName;
			ResetDeal();
			Offer_Panel.SetActive(value: true);
			EnterCreatureMode();
		}
	}

	public void OpenPickRecipient()
	{
		if (EnsurePanel())
		{
			mCameFromFriendRow = false;
			mRecipientId = null;
			mRecipientName = null;
			ResetDeal();
			Offer_Panel.SetActive(value: true);
			EnterRecipientMode();
		}
	}

	public void OpenForListing()
	{
		if (EnsurePanel())
		{
			mCameFromFriendRow = false;
			mRecipientId = null;
			mRecipientName = null;
			ResetDeal();
			mListingMode = true;
			mWantCoins = 100;
			Offer_Panel.SetActive(value: true);
			EnterCreatureMode();
		}
	}

	public void Close()
	{
		if (Offer_Panel != null)
		{
			Offer_Panel.SetActive(value: false);
		}
		if (mCameFromFriendRow)
		{
			return;
		}
		TradePanelController[] array = Resources.FindObjectsOfTypeAll<TradePanelController>();
		foreach (TradePanelController tradePanelController in array)
		{
			if (tradePanelController != null && tradePanelController.gameObject.scene.IsValid())
			{
				tradePanelController.Open();
				break;
			}
		}
	}

	public void OnClickBack()
	{
		if (mMode == Mode.PickCreatures && !mCameFromFriendRow)
		{
			EnterRecipientMode();
		}
		else
		{
			Close();
		}
	}

	private bool EnsurePanel()
	{
		if (Offer_Panel == null)
		{
			Debug.LogError("[TradeOffer] Offer_Panel is NULL.");
			return false;
		}
		return true;
	}

	private void ResetDeal()
	{
		mGive.Clear();
		mGiveOrder.Clear();
		mWant.Clear();
		mWantOrder.Clear();
		mGiveCoins = 0;
		mWantCoins = 0;
		mCoinBalance = 0;
		mPickSide = PickSide.Mine;
		mListingMode = false;
	}

	private void EnterRecipientMode()
	{
		TradePanelController.FillWalletCards(Offer_Panel);
		mMode = Mode.PickRecipient;
		if (TitleLabel != null)
		{
			TitleLabel.text = "Choose a friend to trade with";
		}
		if (ListHeaderLabel != null)
		{
			ListHeaderLabel.text = "Your Friends";
			ListHeaderLabel.gameObject.SetActive(value: true);
		}
		if (LeftTabs != null)
		{
			LeftTabs.SetActive(value: false);
		}
		if (RightColumn != null)
		{
			RightColumn.SetActive(value: false);
		}
		if (RightFrame != null)
		{
			RightFrame.SetActive(value: true);
		}
		if (GuidePanel != null)
		{
			GuidePanel.SetActive(value: true);
		}
		if (DealPanel != null)
		{
			DealPanel.SetActive(value: false);
		}
		if (ItemGrid != null)
		{
			ItemGrid.transform.DestroyAllChildren();
		}
		if (EmptyLabel != null)
		{
			EmptyLabel.text = "Loading friends...";
			EmptyLabel.gameObject.SetActive(value: true);
		}
		FetchFriends();
	}

	private void FetchFriends()
	{
		Ally.GetAlliesList(SessionManager.Instance.theSession, delegate(List<AllyData> allies, ResponseFlag flag)
		{
			PopulateRecipientsFromAllies(allies);
		});
	}

	private void PopulateRecipientsFromAllies(List<AllyData> allies)
	{
		if (ItemGrid == null || ItemPrefab == null)
		{
			Debug.LogError("[TradeOffer] ItemGrid/ItemPrefab NULL.");
			return;
		}
		ItemGrid.transform.DestroyAllChildren();
		int num = allies?.Count ?? 0;
		if (EmptyLabel != null)
		{
			EmptyLabel.text = "No friends yet — add friends first, then trade with them.";
			EmptyLabel.gameObject.SetActive(num == 0);
		}
		if (allies != null)
		{
			foreach (AllyData ally in allies)
			{
				if (ally == null || string.IsNullOrEmpty(ally.Id))
				{
					continue;
				}
				GameObject gameObject = ItemGrid.transform.InstantiateAsChild(ItemPrefab);
				if (!(gameObject == null))
				{
					gameObject.ChangeLayer(ItemGrid.gameObject.layer);
					TradeListItem component = gameObject.GetComponent<TradeListItem>();
					if (component != null)
					{
						component.PopulateRecipient(ally, this);
					}
				}
			}
		}
		ItemGrid.Reposition();
	}

	public void PickRecipient(string userId, string name)
	{
		mRecipientId = userId;
		mRecipientName = name;
		ResetDeal();
		EnterCreatureMode();
	}

	private void EnterCreatureMode()
	{
		mMode = Mode.PickCreatures;
		if (LeftTabs != null)
		{
			LeftTabs.SetActive(value: true);
		}
		if (RightColumn != null)
		{
			RightColumn.SetActive(value: true);
		}
		if (RightFrame != null)
		{
			RightFrame.SetActive(value: true);
		}
		if (GuidePanel != null)
		{
			GuidePanel.SetActive(value: false);
		}
		if (DealPanel != null)
		{
			DealPanel.SetActive(value: true);
		}
		ApplyCreatureChrome();
		EnsureCoinInputs();
		TradePanelController.FillWalletCards(Offer_Panel);
		FetchBalance();
		ShowPickSide(PickSide.Mine);
		RefreshDeal();
	}

	private void ApplyCreatureChrome()
	{
		Transform transform = FindInPanel("TS_GiveHdr");
		Transform transform2 = FindInPanel("TS_WantHdr");
		Transform transform3 = FindInPanel("TS_TheirsTab");
		Transform transform4 = FindInPanel("TS_GiveCoinRow");
		if (transform4 == null && GiveCoinLabel != null)
		{
			transform4 = GiveCoinLabel.transform.parent;
		}
		if (transform4 != null && ((DealPanel != null && transform4.gameObject == DealPanel) || (WantCoinLabel != null && WantCoinLabel.transform.IsChildOf(transform4))))
		{
			transform4 = null;
		}
		Transform transform5 = ((GiveSlots != null && GiveSlots.Length != 0 && GiveSlots[0] != null) ? GiveSlots[0].transform : null);
		Transform transform6 = FindInPanel("TS_WantCoinRow");
		float x = ((transform != null) ? transform.localPosition.x : 330f);
		if (mListingMode)
		{
			if (TitleLabel != null)
			{
				TitleLabel.text = "List a creature for sale";
			}
			SetText(transform, "SELLING");
			SetText(transform2, "ASKING PRICE");
			SetSendLabel("List for Sale");
			if (GiveSlots != null)
			{
				for (int i = 0; i < GiveSlots.Length; i++)
				{
					if (GiveSlots[i] != null)
					{
						GiveSlots[i].SetActive(i == 0);
					}
				}
			}
			if (WantSlots != null)
			{
				GameObject[] wantSlots = WantSlots;
				foreach (GameObject gameObject in wantSlots)
				{
					if (gameObject != null)
					{
						gameObject.SetActive(value: false);
					}
				}
			}
			if (transform4 != null)
			{
				transform4.gameObject.SetActive(value: false);
			}
			if (transform3 != null)
			{
				transform3.gameObject.SetActive(value: false);
			}
			if (LeftTabs != null)
			{
				LeftTabs.SetActive(value: false);
			}
			if (ListHeaderLabel != null)
			{
				ListHeaderLabel.text = "Your Creatures";
				ListHeaderLabel.gameObject.SetActive(value: true);
			}
			Remember(transform);
			Remember(transform5);
			Remember(transform2);
			Remember(transform6);
			if (transform != null)
			{
				transform.localPosition = new Vector3(x, 175f, 0f);
			}
			if (transform5 != null)
			{
				transform5.localPosition = new Vector3(x, 55f, 0f);
				transform5.localScale = new Vector3(1.4f, 1.4f, 1f);
			}
			if (transform2 != null)
			{
				transform2.localPosition = new Vector3(x, -55f, 0f);
			}
			if (transform6 != null)
			{
				transform6.localPosition = new Vector3(0f, 40f, 0f);
			}
			return;
		}
		if (TitleLabel != null)
		{
			TitleLabel.text = "Trading with " + DisplayName();
		}
		SetText(transform, "YOU GIVE");
		SetText(transform2, "YOU WANT");
		SetSendLabel("Send");
		if (GiveSlots != null)
		{
			GameObject[] wantSlots = GiveSlots;
			foreach (GameObject gameObject2 in wantSlots)
			{
				if (gameObject2 != null)
				{
					gameObject2.SetActive(value: true);
				}
			}
		}
		if (WantSlots != null)
		{
			GameObject[] wantSlots = WantSlots;
			foreach (GameObject gameObject3 in wantSlots)
			{
				if (gameObject3 != null)
				{
					gameObject3.SetActive(value: true);
				}
			}
		}
		if (transform4 != null)
		{
			transform4.gameObject.SetActive(value: true);
		}
		if (transform3 != null)
		{
			transform3.gameObject.SetActive(value: true);
		}
		if (LeftTabs != null)
		{
			LeftTabs.SetActive(value: true);
		}
		if (ListHeaderLabel != null)
		{
			ListHeaderLabel.gameObject.SetActive(value: false);
		}
		Restore(transform);
		Restore(transform5);
		Restore(transform2);
		Restore(transform6);
	}

	private void Remember(Transform t)
	{
		if (!(t == null) && !mOrigPos.ContainsKey(t))
		{
			mOrigPos[t] = t.localPosition;
			mOrigScale[t] = t.localScale;
		}
	}

	private void Restore(Transform t)
	{
		if (!(t == null) && mOrigPos.ContainsKey(t))
		{
			t.localPosition = mOrigPos[t];
			t.localScale = mOrigScale[t];
		}
	}

	private Transform FindInPanel(string name)
	{
		if (Offer_Panel == null)
		{
			return null;
		}
		Transform[] componentsInChildren = Offer_Panel.GetComponentsInChildren<Transform>(includeInactive: true);
		foreach (Transform transform in componentsInChildren)
		{
			if (transform != null && transform.name == name)
			{
				return transform;
			}
		}
		return null;
	}

	private static void SetText(Transform t, string text)
	{
		if (!(t == null))
		{
			UILabel component = t.GetComponent<UILabel>();
			if (component != null)
			{
				component.text = text;
			}
		}
	}

	private void SetSendLabel(string text)
	{
		if (Offer_Panel == null)
		{
			return;
		}
		UIEventTrigger[] componentsInChildren = Offer_Panel.GetComponentsInChildren<UIEventTrigger>(includeInactive: true);
		foreach (UIEventTrigger uIEventTrigger in componentsInChildren)
		{
			if (uIEventTrigger == null || uIEventTrigger.onClick == null)
			{
				continue;
			}
			foreach (EventDelegate item in uIEventTrigger.onClick)
			{
				if (item != null && item.methodName == "OnClickSend")
				{
					UILabel componentInChildren = uIEventTrigger.GetComponentInChildren<UILabel>(includeInactive: true);
					if (componentInChildren != null)
					{
						componentInChildren.text = text;
					}
					return;
				}
			}
		}
	}

	private string DisplayName()
	{
		if (!string.IsNullOrEmpty(mRecipientName))
		{
			return mRecipientName;
		}
		return mRecipientId;
	}

	private void FetchBalance()
	{
		Server.TradeGetBalance(delegate(Dictionary<string, object> resp, HttpStatusCode status)
		{
			mCoinBalance = ReadInt(GetDataObject(resp), "coins", 0);
			if (mGiveCoins > mCoinBalance)
			{
				mGiveCoins = mCoinBalance;
			}
			RefreshDeal();
		});
	}

	public void OnClickMineTab()
	{
		ShowPickSide(PickSide.Mine);
	}

	public void OnClickTheirsTab()
	{
		ShowPickSide(PickSide.Theirs);
	}

	private void ShowPickSide(PickSide side)
	{
		if (!mListingMode && ListHeaderLabel != null)
		{
			ListHeaderLabel.gameObject.SetActive(value: false);
		}
		mPickSide = side;
		if (MineTabLabel != null)
		{
			MineTabLabel.color = ((side == PickSide.Mine) ? TabActive : TabInactive);
		}
		if (TheirsTabLabel != null)
		{
			TheirsTabLabel.color = ((side == PickSide.Theirs) ? TabActive : TabInactive);
		}
		if (side == PickSide.Mine)
		{
			PopulateMyCreatures();
		}
		else
		{
			PopulateFriendCreatures();
		}
	}

	private void PopulateMyCreatures()
	{
		if (ItemGrid == null || ItemPrefab == null)
		{
			return;
		}
		ItemGrid.transform.DestroyAllChildren();
		int num = 0;
		PlayerInfoScript instance = Singleton<PlayerInfoScript>.Instance;
		if (instance != null && instance.SaveData != null)
		{
			foreach (InventorySlotItem inventorySlot in instance.SaveData.InventorySlots)
			{
				if (!IsTradeable(inventorySlot, instance))
				{
					continue;
				}
				GameObject gameObject = ItemGrid.transform.InstantiateAsChild(ItemPrefab);
				if (!(gameObject == null))
				{
					gameObject.ChangeLayer(ItemGrid.gameObject.layer);
					TradeListItem component = gameObject.GetComponent<TradeListItem>();
					if (!(component == null))
					{
						component.PopulateOwned(inventorySlot.Creature, this);
						num++;
					}
				}
			}
		}
		if (EmptyLabel != null)
		{
			EmptyLabel.text = "You have no tradeable creatures.";
			EmptyLabel.gameObject.SetActive(num == 0);
		}
		ItemGrid.Reposition();
	}

	private void PopulateFriendCreatures()
	{
		if (ItemGrid == null || ItemPrefab == null)
		{
			return;
		}
		ItemGrid.transform.DestroyAllChildren();
		if (EmptyLabel != null)
		{
			EmptyLabel.text = "Loading their creatures...";
			EmptyLabel.gameObject.SetActive(value: true);
		}
		Server.TradeGetInventory(mRecipientId, delegate(Dictionary<string, object> resp, HttpStatusCode status)
		{
			if (mPickSide == PickSide.Theirs && !(ItemGrid == null))
			{
				ItemGrid.transform.DestroyAllChildren();
				List<object> list = ReadList(GetDataObject(resp), "creatures");
				int num = 0;
				if (list != null)
				{
					foreach (object item in list)
					{
						if (item is Dictionary<string, object> d)
						{
							int num2 = ReadInt(d, "unique_id", 0);
							string text = ReadStr(d, "creature_id");
							int star = ReadInt(d, "star", 0);
							if (num2 != 0 && !string.IsNullOrEmpty(text))
							{
								GameObject gameObject = ItemGrid.transform.InstantiateAsChild(ItemPrefab);
								if (!(gameObject == null))
								{
									gameObject.ChangeLayer(ItemGrid.gameObject.layer);
									TradeListItem component = gameObject.GetComponent<TradeListItem>();
									if (!(component == null))
									{
										component.PopulateWant(num2, text, star, this);
										num++;
									}
								}
							}
						}
					}
				}
				if (EmptyLabel != null)
				{
					EmptyLabel.text = "This friend has no creatures to trade.";
					EmptyLabel.gameObject.SetActive(num == 0);
				}
				ItemGrid.Reposition();
			}
		});
	}

	private static bool IsTradeable(InventorySlotItem slot, PlayerInfoScript info)
	{
		if (slot == null || slot.SlotType != InventorySlotType.Creature || slot.Creature == null)
		{
			return false;
		}
		if (slot.IsFavorite() || slot.IsHelper())
		{
			return false;
		}
		if (info != null && info.IsCreatureInAnyLoadout(slot.Creature))
		{
			return false;
		}
		return true;
	}

	public void ToggleCreature(int uniqueId, CreatureItem cre, TradeListItem tile)
	{
		if (mGive.ContainsKey(uniqueId))
		{
			mGive.Remove(uniqueId);
			mGiveOrder.Remove(uniqueId);
			if (tile != null)
			{
				tile.SetOwnedSelected(on: false);
			}
		}
		else
		{
			if (cre == null)
			{
				return;
			}
			if (mListingMode)
			{
				mGive.Clear();
				mGiveOrder.Clear();
			}
			else if (mGiveOrder.Count >= 3)
			{
				TooMany();
				return;
			}
			mGive[uniqueId] = cre;
			mGiveOrder.Add(uniqueId);
			if (tile != null)
			{
				tile.SetOwnedSelected(on: true);
			}
		}
		RefreshDeal();
		RefreshLeftHighlights();
	}

	public void ToggleWant(int uniqueId, string creatureId, int star, TradeListItem tile)
	{
		if (mWant.ContainsKey(uniqueId))
		{
			mWant.Remove(uniqueId);
			mWantOrder.Remove(uniqueId);
			if (tile != null)
			{
				tile.SetOwnedSelected(on: false);
			}
		}
		else
		{
			if (string.IsNullOrEmpty(creatureId))
			{
				return;
			}
			if (mWantOrder.Count >= 3)
			{
				TooMany();
				return;
			}
			mWant[uniqueId] = new WantItem(creatureId, star);
			mWantOrder.Add(uniqueId);
			if (tile != null)
			{
				tile.SetOwnedSelected(on: true);
			}
		}
		RefreshDeal();
		RefreshLeftHighlights();
	}

	private static void TooMany()
	{
		SimplePopupController instance = Singleton<SimplePopupController>.Instance;
		if (instance != null)
		{
			instance.ShowMessage("Trade Offer", "You can put up to " + 3 + " creatures on each side of a trade.");
		}
	}

	public void OnGiveSlotTapped(int index)
	{
		if (index >= 0 && index < mGiveOrder.Count)
		{
			ToggleCreature(mGiveOrder[index], null, null);
		}
	}

	public void OnWantSlotTapped(int index)
	{
		if (index >= 0 && index < mWantOrder.Count)
		{
			int num = mWantOrder[index];
			if (mWant.TryGetValue(num, out var value))
			{
				ToggleWant(num, value.CreatureId, value.Star, null);
			}
		}
	}

	public void OnGiveSlot0()
	{
		OnGiveSlotTapped(0);
	}

	public void OnGiveSlot1()
	{
		OnGiveSlotTapped(1);
	}

	public void OnGiveSlot2()
	{
		OnGiveSlotTapped(2);
	}

	public void OnWantSlot0()
	{
		OnWantSlotTapped(0);
	}

	public void OnWantSlot1()
	{
		OnWantSlotTapped(1);
	}

	public void OnWantSlot2()
	{
		OnWantSlotTapped(2);
	}

	public bool IsCreatureSelected(int uniqueId)
	{
		return mGive.ContainsKey(uniqueId);
	}

	public bool IsWantSelected(int uniqueId)
	{
		return mWant.ContainsKey(uniqueId);
	}

	private void RefreshLeftHighlights()
	{
		if (ItemGrid == null || mMode != Mode.PickCreatures)
		{
			return;
		}
		TradeListItem[] componentsInChildren = ItemGrid.GetComponentsInChildren<TradeListItem>(includeInactive: true);
		foreach (TradeListItem tradeListItem in componentsInChildren)
		{
			if (tradeListItem != null)
			{
				tradeListItem.RefreshOwnedSelected();
			}
		}
	}

	private void RefreshDeal()
	{
		FillSlots(GiveSlots, mGiveOrder, wantSide: false);
		FillSlots(WantSlots, mWantOrder, wantSide: true);
		ShowCoins(mGiveInput, GiveCoinLabel, mGiveCoins);
		ShowCoins(mWantInput, WantCoinLabel, mWantCoins);
	}

	private static void ShowCoins(UIInput input, UILabel label, int value)
	{
		if (input != null)
		{
			if (!input.isSelected)
			{
				input.value = value.ToString();
			}
		}
		else if (label != null)
		{
			label.text = value.ToString();
		}
	}

	private void FillSlots(GameObject[] slots, List<int> order, bool wantSide)
	{
		if (slots == null)
		{
			return;
		}
		for (int i = 0; i < slots.Length; i++)
		{
			GameObject gameObject = slots[i];
			if (gameObject == null)
			{
				continue;
			}
			Transform transform = gameObject.transform.Find("Art");
			Transform transform2 = gameObject.transform.Find("Plus");
			if (transform != null)
			{
				transform.DestroyAllChildren();
			}
			CreatureData creatureData = null;
			if (i < order.Count)
			{
				int key = order[i];
				WantItem value2;
				if (!wantSide)
				{
					if (mGive.TryGetValue(key, out var value) && value != null)
					{
						creatureData = value.Form;
					}
				}
				else if (mWant.TryGetValue(key, out value2) && !string.IsNullOrEmpty(value2.CreatureId) && CreatureDataManager.Instance != null)
				{
					creatureData = CreatureDataManager.Instance.GetData(value2.CreatureId);
				}
			}
			if (transform2 != null)
			{
				transform2.gameObject.SetActive(creatureData == null);
			}
			if (creatureData != null && transform != null)
			{
				BuildSlotArt(transform, creatureData);
			}
		}
	}

	private static void BuildSlotArt(Transform artNode, CreatureData data)
	{
		PrefabReferences instance = Singleton<PrefabReferences>.Instance;
		GameObject gameObject = ((instance != null) ? instance.InventoryTile : null);
		if (gameObject == null)
		{
			return;
		}
		GameObject gameObject2 = artNode.InstantiateAsChild(gameObject);
		if (!(gameObject2 == null))
		{
			gameObject2.ChangeLayer(artNode.gameObject.layer);
			UIWidget[] componentsInChildren = gameObject2.GetComponentsInChildren<UIWidget>(includeInactive: true);
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				componentsInChildren[i].depth += 700;
			}
			gameObject2.transform.localScale = new Vector3(0.57f, 0.57f, 1f);
			InventoryTile component = gameObject2.GetComponent<InventoryTile>();
			if (component != null)
			{
				component.PopulateAndForceDisplay(data);
			}
		}
	}

	private void EnsureCoinInputs()
	{
		if (mGiveInput == null)
		{
			mGiveInput = MakeCoinInput(GiveCoinLabel, "OnGiveCoinTyped");
		}
		if (mWantInput == null)
		{
			mWantInput = MakeCoinInput(WantCoinLabel, "OnWantCoinTyped");
		}
	}

	private UIInput MakeCoinInput(UILabel label, string onChange)
	{
		if (label == null)
		{
			return null;
		}
		UIInput uIInput = label.GetComponent<UIInput>();
		if (uIInput == null)
		{
			NGUITools.AddWidgetCollider(label.gameObject);
			uIInput = label.gameObject.AddComponent<UIInput>();
			uIInput.label = label;
			uIInput.validation = UIInput.Validation.Integer;
			uIInput.keyboardType = UIInput.KeyboardType.NumberPad;
			uIInput.characterLimit = 7;
			uIInput.activeTextColor = label.color;
		}
		uIInput.onChange.Clear();
		uIInput.onChange.Add(new EventDelegate(this, onChange));
		return uIInput;
	}

	private static int Typed(UIInput input)
	{
		if (!(input != null) || !int.TryParse((input.value ?? string.Empty).Trim(), out var result) || result <= 0)
		{
			return 0;
		}
		return result;
	}

	public void OnGiveCoinTyped()
	{
		mTyping = true;
		SetGiveCoins(Typed(mGiveInput));
	}

	public void OnWantCoinTyped()
	{
		mTyping = true;
		SetWantCoins(Typed(mWantInput));
	}

	private void Update()
	{
		if (mTyping && (!(mGiveInput != null) || !mGiveInput.isSelected) && (!(mWantInput != null) || !mWantInput.isSelected))
		{
			mTyping = false;
			RefreshDeal();
		}
	}

	public void OnGiveCoinPlus()
	{
		SetGiveCoins(mGiveCoins + 100);
	}

	public void OnGiveCoinMinus()
	{
		SetGiveCoins(mGiveCoins - 100);
	}

	public void OnWantCoinPlus()
	{
		SetWantCoins(mWantCoins + 100);
	}

	public void OnWantCoinMinus()
	{
		SetWantCoins(mWantCoins - 100);
	}

	private void SetGiveCoins(int v)
	{
		if (v < 0)
		{
			v = 0;
		}
		if (v > mCoinBalance)
		{
			v = mCoinBalance;
		}
		mGiveCoins = v;
		RefreshDeal();
	}

	private void SetWantCoins(int v)
	{
		if (v < 0)
		{
			v = 0;
		}
		if (v > 1000000)
		{
			v = 1000000;
		}
		mWantCoins = v;
		RefreshDeal();
	}

	public void OnClickSend()
	{
		if (mMode != Mode.PickCreatures)
		{
			return;
		}
		if (mListingMode)
		{
			SubmitListing();
		}
		else
		{
			if (string.IsNullOrEmpty(mRecipientId))
			{
				return;
			}
			SimplePopupController instance = Singleton<SimplePopupController>.Instance;
			if (mGive.Count == 0 && mGiveCoins <= 0)
			{
				if (instance != null)
				{
					instance.ShowMessage("Trade Offer", "Add at least one creature or some trade coins to YOUR side (You Give).");
				}
				return;
			}
			string giveCsv = BuildCsv(mGive.Keys);
			string wantCsv = BuildCsv(mWant.Keys);
			string body = "You give: " + Describe(mGive.Count, mGiveCoins) + "\nYou want: " + Describe(mWant.Count, mWantCoins) + "\n\nSend this offer to " + DisplayName() + "?";
			if (instance != null)
			{
				instance.ShowPrompt("Send Trade Offer", body, delegate
				{
					DoSend(giveCsv, mGiveCoins, wantCsv, mWantCoins);
				}, null, "Send", "Cancel");
			}
			else
			{
				DoSend(giveCsv, mGiveCoins, wantCsv, mWantCoins);
			}
		}
	}

	private void SubmitListing()
	{
		SimplePopupController instance = Singleton<SimplePopupController>.Instance;
		if (mGiveOrder.Count != 1)
		{
			if (instance != null)
			{
				instance.ShowMessage("List for Sale", "Pick one creature to sell.");
			}
			return;
		}
		if (mWantCoins <= 0)
		{
			if (instance != null)
			{
				instance.ShowMessage("List for Sale", "Set an asking price using the + / - stepper.");
			}
			return;
		}
		int uid = mGiveOrder[0];
		int price = mWantCoins;
		CreatureItem value;
		string text = ((mGive.TryGetValue(uid, out value) && value != null && value.Form != null) ? value.Form.Name : "this creature");
		string body = "List " + text + " for " + price + " trade coins?";
		if (instance != null)
		{
			instance.ShowPrompt("List for Sale", body, delegate
			{
				DoListing(uid, price);
			}, null, "List", "Cancel");
		}
		else
		{
			DoListing(uid, price);
		}
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

	private void DoListing(int uniqueId, int price)
	{
		if (!BeginBusy())
		{
			return;
		}
		Server.TradeListItem(uniqueId, price, delegate(Dictionary<string, object> resp, HttpStatusCode status)
		{
			EndBusy();
			SimplePopupController instance = Singleton<SimplePopupController>.Instance;
			if (IsSuccess(resp))
			{
				if (instance != null)
				{
					instance.ShowMessage("Market", "Listed for sale!");
				}
				TradePanelController.SyncTradeChanges(null);
				Close();
			}
			else if (instance != null)
			{
				instance.ShowMessage("Market", GetError(resp));
			}
		});
	}

	private static string Describe(int creatures, int coins)
	{
		List<string> list = new List<string>();
		if (creatures > 0)
		{
			list.Add(creatures + " creature" + ((creatures == 1) ? "" : "s"));
		}
		if (coins > 0)
		{
			list.Add(coins + " trade coins");
		}
		if (list.Count <= 0)
		{
			return "nothing";
		}
		return string.Join(" + ", list.ToArray());
	}

	private void DoSend(string giveCsv, int giveCoins, string wantCsv, int wantCoins)
	{
		SendOfferNow(giveCsv, giveCoins, wantCsv, wantCoins);
	}

	private void SendOfferNow(string giveCsv, int giveCoins, string wantCsv, int wantCoins)
	{
		if (!BeginBusy())
		{
			return;
		}
		Server.TradeSendOffer(mRecipientId, giveCsv, wantCsv, giveCoins, wantCoins, delegate(Dictionary<string, object> resp, HttpStatusCode status)
		{
			EndBusy();
			SimplePopupController instance = Singleton<SimplePopupController>.Instance;
			if (IsSuccess(resp))
			{
				if (instance != null)
				{
					instance.ShowMessage("Trade Offer", "Offer sent!");
				}
				TradePanelController.SyncTradeChanges(null);
				Close();
			}
			else if (instance != null)
			{
				instance.ShowMessage("Trade Offer", GetError(resp));
			}
		});
	}

	private static string BuildCsv(ICollection<int> ids)
	{
		StringBuilder stringBuilder = new StringBuilder();
		foreach (int id in ids)
		{
			if (stringBuilder.Length > 0)
			{
				stringBuilder.Append(',');
			}
			stringBuilder.Append(id);
		}
		return stringBuilder.ToString();
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

	private static Dictionary<string, object> GetDataObject(Dictionary<string, object> resp)
	{
		if (resp != null && resp.TryGetValue("data", out var value))
		{
			return value as Dictionary<string, object>;
		}
		return null;
	}

	private static int ReadInt(Dictionary<string, object> d, string key, int fallback)
	{
		if (d == null || !d.TryGetValue(key, out var value) || value == null)
		{
			return fallback;
		}
		if (value is long)
		{
			return (int)(long)value;
		}
		if (value is double)
		{
			return (int)(double)value;
		}
		if (!int.TryParse(value.ToString(), out var result))
		{
			return fallback;
		}
		return result;
	}

	private static string ReadStr(Dictionary<string, object> d, string key)
	{
		if (d == null || !d.TryGetValue(key, out var value) || value == null)
		{
			return null;
		}
		return value.ToString();
	}

	private static List<object> ReadList(Dictionary<string, object> d, string key)
	{
		if (d == null || !d.TryGetValue(key, out var value))
		{
			return null;
		}
		return value as List<object>;
	}
}
