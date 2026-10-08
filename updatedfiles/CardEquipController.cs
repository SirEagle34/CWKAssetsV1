using System.Collections.Generic;
using UnityEngine;

public class CardEquipController : Singleton<CardEquipController>
{
	public delegate void Callback();

	public UITweenController ShowTween;

	public UITweenController ShowPassiveTween;

	public UITweenController HidePassiveTween;

	public GameObject MainPanel;

	public CreatureStatsPanel StatsPanel;

	public UIStreamingGrid CardGrid;

	private UIStreamingGridDataSource<InventorySlotItem> mCardGridDataSource = new UIStreamingGridDataSource<InventorySlotItem>();

	public UILabel CreatureName;

	public Transform[] EquippedCardSlots = new Transform[4];

	public GameObject[] ExtraCardSlotLocks = new GameObject[4];

	public UILabel SoftCurrencyLabel;

	public UILabel HardCurrencyLabel;

	public Transform SortButton;

	public Transform[] NativeCardSlots;

	private List<CardPrefabScript> mSpawnedNativeCards = new List<CardPrefabScript>();

	private CreatureItem mSelectedCreature;

	public InventoryTile[] mEquippedCardTiles = new InventoryTile[4];

	private Callback mFinishedCallback;

	public Transform ZoomPosition;

	public Collider ZoomCollider;

	public float CardScaleInGrid = 0.5f;

	public InventoryBarController inventoryBar;

	public GameObject CardParentToHide;

	public void Show(InventorySlotItem creatureSlot, Callback finishedCallback)
	{
		if (Singleton<TutorialController>.Instance.IsBlockActive("CardEquip"))
		{
			Singleton<TutorialController>.Instance.AddCardToEquip(creatureSlot.Creature.Faction);
		}
		ShowTween.Play();
		base.gameObject.SetActive(value: false);
		base.gameObject.SetActive(value: true);
		mFinishedCallback = finishedCallback;
		mSelectedCreature = creatureSlot.Creature;
		CreatureName.text = mSelectedCreature.Form.Name;
		for (int i = 0; i < 6; i++)
		{
			if (mSelectedCreature.ActionCards[i] != null)
			{
				GameObject obj = NativeCardSlots[i].InstantiateAsChild(Singleton<PrefabReferences>.Instance.Card);
				obj.ChangeLayer(NativeCardSlots[i].gameObject.layer);
				CardPrefabScript component = obj.GetComponent<CardPrefabScript>();
				component.Mode = CardPrefabScript.CardMode.GeneralFrontEnd;
				component.Populate(mSelectedCreature.Form.ActionCards[i]);
				component.AdjustDepth(i + 1);
				component.SetCardState(CardPrefabScript.HandCardState.InHand);
				mSpawnedNativeCards.Add(component);
			}
		}
		InventoryBarController.onDoFilterNull();
		InventoryBarController.onDoFilter += PopulateCardList;
		inventoryBar.SetFilters(creatureFilterActive: true, cakeFilterActive: true, cardFilterActive: false, ingredientFilterActive: true, shardFilterActive: true);
		RefreshLockedSlots();
		for (int j = 0; j < mEquippedCardTiles.Length; j++)
		{
			if (mSelectedCreature.ExCards[j] != null)
			{
				InventoryTile component2 = EquippedCardSlots[j].InstantiateAsChild(Singleton<PrefabReferences>.Instance.InventoryTile).GetComponent<InventoryTile>();
				component2.gameObject.ChangeLayer(base.gameObject.layer);
				component2.AssignedSlot = j;
				component2.Populate(mSelectedCreature.ExCards[j]);
				component2.SpawnCard();
				mEquippedCardTiles[j] = component2;
			}
		}
		StatsPanel.Populate(creatureSlot);
		if (creatureSlot.Creature.Form.HasPassiveAbility())
		{
			ShowPassiveTween.Play();
		}
		else
		{
			HidePassiveTween.Play();
		}
		RefreshCurrency();
		base.gameObject.GetComponent<InventoryBarController>().UpdateInventoryCounter();
		CardGrid.transform.parent.GetComponent<UIScrollView>().ResetPosition();
		CardGrid.transform.parent.GetComponent<UIScrollView>().UpdateScrollbars();
		Singleton<TutorialController>.Instance.AdvanceIfOnState("CQ_TapCardSlots");
	}

	private void RefreshLockedSlots()
	{
		for (int i = 0; i < mEquippedCardTiles.Length; i++)
		{
			ExtraCardSlotLocks[i].SetActive(i >= mSelectedCreature.ExCardSlotsUnlocked);
		}
	}

	public void OnClickClose()
	{
		if (mFinishedCallback != null)
		{
			mFinishedCallback();
			mFinishedCallback = null;
		}
		if (CardParentToHide != null)
		{
			CardParentToHide.SetActive(value: true);
			CardParentToHide = null;
		}
	}

	public void Unload()
	{
		mCardGridDataSource.Clear();
		for (int i = 0; i < mSpawnedNativeCards.Count; i++)
		{
			if (mSpawnedNativeCards[i] != null)
			{
				NGUITools.Destroy(mSpawnedNativeCards[i].gameObject);
				mSpawnedNativeCards[i] = null;
			}
		}
		for (int j = 0; j < mEquippedCardTiles.Length; j++)
		{
			if (mEquippedCardTiles[j] != null)
			{
				NGUITools.Destroy(mEquippedCardTiles[j].gameObject);
				mEquippedCardTiles[j] = null;
			}
		}
		StatsPanel.Unload();
	}

	public void PopulateCardList()
	{
		InventoryTile.SetDelegates(InventorySlotType.Card, TileDraggable, null, OnTileDropped, RefreshTileOverlay, PopulateCardList, OnPopupClosed, TileClickable);
		Singleton<PlayerInfoScript>.Instance.SaveData.SortInventory(InventorySlotType.Card);
		mCardGridDataSource.Init(CardGrid, Singleton<PrefabReferences>.Instance.InventoryTile, inventoryBar.GetFilteredInventory());
	}

	private void OnPopupClosed(InventorySlotItem creature)
	{
		PopulateCardList();
	}

	private bool TileDraggable(InventoryTile tile)
	{
		if (tile.IsAttachedToTarget())
		{
			CardEquipper.ClearCard(mEquippedCardTiles, mSelectedCreature, tile.AssignedSlot, OnCardsChanged);
			return false;
		}
		if (!InventoryTile.IsUpwardsDrag())
		{
			return false;
		}
		if (tile.InventoryItem.Card.CreatureUID == mSelectedCreature.UniqueId)
		{
			return false;
		}
		return true;
	}

	private bool TileClickable(InventoryTile tile)
	{
		return tile.InventoryItem.SlotType != InventorySlotType.Creature;
	}

	private bool OnTileDropped(InventoryTile tile, int slotIndex)
	{
		if (slotIndex == -1)
		{
			return false;
		}
		if (tile == null || tile.InventoryItem == null)
		{
			return false;
		}
		if (!CanEquipCard(tile.InventoryItem))
		{
			ShowCardEquipBlockPopup(tile.InventoryItem);
			return false;
		}
		CardEquipper.SetCard(mEquippedCardTiles, EquippedCardSlots, mSelectedCreature, tile.InventoryItem, slotIndex, OnCardsChanged);
		Singleton<TutorialController>.Instance.AdvanceIfDraggingTile();
		return false;
	}

	private void RefreshTileOverlay(InventoryTile tile)
	{
		if (tile.InventoryItem.Card.CreatureUID == mSelectedCreature.UniqueId)
		{
			tile.SetOverlayStatus(InventoryTile.TileStatus.SelectedAsTarget);
		}
		else
		{
			tile.SetOverlayStatus(InventoryTile.TileStatus.NotSelected, fadeIfInUse: false);
		}
	}

	public bool IsCardOnThisCreature(CardItem card)
	{
		return card.CreatureUID == mSelectedCreature.UniqueId;
	}

	private void RefreshCurrency()
	{
		if (SoftCurrencyLabel.gameObject.activeInHierarchy)
		{
			SoftCurrencyLabel.text = Singleton<PlayerInfoScript>.Instance.SaveData.SoftCurrency.ToString();
			HardCurrencyLabel.text = Singleton<PlayerInfoScript>.Instance.SaveData.HardCurrency.ToString();
		}
	}

	private void Update()
	{
		RefreshCurrency();
	}

	public void OnClickSort()
	{
		Singleton<SortPopupController>.Instance.Show(SortPopupController.Category.Cards, SortButton, PopulateCardList);
	}

	public GameObject GetUnusedCard()
	{
		InventorySlotItem dataItem = Singleton<PlayerInfoScript>.Instance.SaveData.FindExCard((CardItem m) => m.CreatureUID == 0);
		return mCardGridDataSource.FindPrefab(dataItem);
	}

	public void OnClickSlot1()
	{
	}

	public void OnClickSlot2()
	{
	}

	public void OnClickSlot3()
	{
	}

	public void OnClickSlot4()
	{
	}

	private void OnCardsChanged()
	{
		PopulateCardList();
		RefreshLockedSlots();
	}

	private bool CanEquipCard(InventorySlotItem cardItem)
	{
		if (cardItem == null || cardItem.Card == null || cardItem.Card.Form == null || mSelectedCreature == null || mSelectedCreature.Form == null)
		{
			return false;
		}
		int num = mSelectedCreature.Form.AttackCost;
		int rarity = cardItem.Card.Form.Rarity;
		return num >= RequiredDragCost(rarity);
	}

	private static int RequiredDragCost(int rarity)
	{
		return rarity switch
		{
			1 => 0, 
			2 => 3, 
			3 => 4, 
			4 => 8, 
			_ => int.MaxValue, 
		};
	}

	private void ShowCardEquipBlockPopup(InventorySlotItem cardItem)
	{
		if (cardItem != null && cardItem.Card != null && cardItem.Card.Form != null && mSelectedCreature != null && mSelectedCreature.Form != null)
		{
			int num = mSelectedCreature.Form.AttackCost;
			int rarity = cardItem.Card.Form.Rarity;
			int num2 = RequiredDragCost(rarity);
			Singleton<SimplePopupController>.Instance.ShowMessage("Card Cannot Be Equipped", "This card cannot be equipped to this creature.\n\nCard Rarity: " + rarity + "\nRequired Drag Attack Cost: " + num2 + "\nCreature Drag Attack Cost: " + num, playErrorSound: true);
		}
	}
}
