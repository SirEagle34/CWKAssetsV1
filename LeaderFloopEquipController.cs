using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LeaderFloopEquipController
	: Singleton<LeaderFloopEquipController>
{
	public delegate void Callback();

	public UITweenController ShowTween;

	public GameObject MainPanel;

	public UILabel LeaderName;

	public UITexture Portrait;
	public UILabel Age;
	public UILabel Height;
	public UILabel Weight;
	public UILabel Species;

	public UIStreamingGrid CardGrid;

	private UIStreamingGridDataSource<InventorySlotItem> mCardGridDataSource = new UIStreamingGridDataSource<InventorySlotItem>();

	public Transform[] EquippedSlots = new Transform[5];

	public List<Transform> ReserveSlots = new List<Transform>();

	public InventoryTile[] EquippedTiles =
		new InventoryTile[5];

	private List<CardPrefabScript> mSpawnedNativeCards = new List<CardPrefabScript>();

	public InventoryBarController inventoryBar;

	public Transform SortButton;

	private LeaderData mSelectedLeader;

	private LeaderFloopSave mSave;

	public float CardScaleInGrid = 0.5f;

	private Callback mFinishedCallback;

	private void Awake()
	{
		inventoryBar = GetComponent<InventoryBarController>();

		if (inventoryBar == null)
		{
			Debug.LogError(
				"LeaderFloopEquipController: InventoryBarController not found on the same GameObject."
			);
		}
	}
	
	public void Show(
		LeaderData leader,
		Callback finishedCallback = null)
	{
		if (leader == null)
			return;

		// Paneli yeniden initialize ettir.
		base.gameObject.SetActive(false);
		base.gameObject.SetActive(true);

		ShowTween.Play();

		mFinishedCallback = finishedCallback;
		mSelectedLeader = leader;

		LeaderName.text = leader.Name;
		Age.text = leader.FlvAge;
		Height.text = leader.FlvHeight;
		Weight.text = leader.FlvWeight;
		Species.text = leader.FlvSpecies;

		RefreshLeaderTexture();

		mSave =
			Singleton<PlayerInfoScript>
				.Instance
				.SaveData
				.EnsureLeaderFloops(leader);

		InventoryBarController.onDoFilterNull();
		InventoryBarController.onDoFilter += PopulateCardList;

		inventoryBar.SetFilters(
			true,
			true,
			false,
			true,
			true
		);

		inventoryBar.UpdateInventoryCounter();

		RefreshAll();

		// Grid'i oluştur.
		PopulateCardList();

		// Grid oluşturulduktan SONRA scroll'u güncelle.
		if (CardGrid != null &&
			CardGrid.transform.parent != null)
		{
			UIScrollView scrollView =
				CardGrid.transform.parent.GetComponent<UIScrollView>();

			if (scrollView != null)
			{
				scrollView.ResetPosition();
				scrollView.UpdateScrollbars();
			}
		}
	}


	private void RefreshAll()
	{
		RefreshEquipped();
	}


	private void ClearTile(
		InventoryTile[] tiles,
		int index)
	{
		if (tiles[index] != null)
		{
			NGUITools.Destroy(
				tiles[index].gameObject
			);

			tiles[index] = null;
		}
	}


	private void RefreshEquipped()
	{
		for (int i = 0; i < EquippedTiles.Length; i++)
		{
			ClearTile(EquippedTiles, i);

			if (mSave == null)
				continue;

			string cardID = mSave.Equipped[i];

			if (!IsValidLeaderFloop(cardID))
			{
				if (!string.IsNullOrEmpty(cardID))
					mSave.Equipped[i] = string.Empty;

				continue;
			}

			CardData data =
				CardDataManager.Instance.GetData(cardID);

			InventorySlotItem item =
				new InventorySlotItem(data);

			InventoryTile tile =
				EquippedSlots[i]
					.InstantiateAsChild(
						Singleton<PrefabReferences>
							.Instance
							.InventoryTile
					)
					.GetComponent<InventoryTile>();

			tile.gameObject.ChangeLayer(
				gameObject.layer
			);

			tile.AssignedSlot = i;

			tile.Populate(item);

			tile.SpawnCard();

			EquippedTiles[i] = tile;
		}
	}


	public void PopulateCardList()
	{
		InventoryTile.SetDelegates(
			InventorySlotType.Card,
			TileDraggable,
			null,
			OnTileDropped,
			RefreshTileOverlay,
			PopulateCardList,
			OnPopupClosed,
			TileClickable
		);

		Singleton<PlayerInfoScript>
			.Instance
			.SaveData
			.SortInventory(InventorySlotType.Card);

		List<InventorySlotItem> inventory =
			inventoryBar.GetFilteredInventory()
			.Where(CanEquipLeaderFloop)
			.ToList();

		mCardGridDataSource.Init(
			CardGrid,
			Singleton<PrefabReferences>.Instance.InventoryTile,
			inventory
		);
	}

	private void OnPopupClosed(
		InventorySlotItem creature)
	{
		PopulateCardList();
	}


	private bool TileDraggable(
		InventoryTile tile)
	{
		if (tile == null ||
			tile.InventoryItem == null)
		{
			return false;
		}

		if (tile.IsAttachedToTarget())
		{
			RemoveFromSlot(tile.AssignedSlot);
			return false;
		}

		if (!InventoryTile.IsUpwardsDrag())
			return false;

		InventorySlotItem item =
			tile.InventoryItem;

		if (!CanEquipLeaderFloop(item))
			return false;

		return true;
	}


	private bool TileClickable(
		InventoryTile tile)
	{
		if (tile == null ||
			tile.InventoryItem == null ||
			tile.InventoryItem.Card == null ||
			tile.InventoryItem.Card.Form == null)
		{
			return false;
		}

		return tile.InventoryItem.SlotType == InventorySlotType.Card &&
			tile.InventoryItem.Card.Form.IsLeaderCard;
	}


	private bool OnTileDropped(
		InventoryTile tile,
		int slotIndex)
	{
		if (slotIndex == -1)
			return false;

		if (tile == null ||
			tile.InventoryItem == null)
		{
			return false;
		}

		if (!CanEquipLeaderFloop(
			tile.InventoryItem))
		{
			ShowLeaderFloopBlockPopup(
				tile.InventoryItem
			);

			return false;
		}

		EquipCard(
			tile.InventoryItem,
			slotIndex
		);

		return false;
	}


	private void RefreshTileOverlay(
		InventoryTile tile)
	{
		if (tile == null ||
			tile.InventoryItem == null ||
			tile.InventoryItem.Card == null ||
			tile.InventoryItem.Card.Form == null)
		{
			return;
		}

		CardData card = tile.InventoryItem.Card.Form;

		// Normal kartlar Floop Equip ekranında kullanılamaz.
		// Gri göster.
		if (!card.IsLeaderCard)
		{
			tile.SetOverlayStatus(
				InventoryTile.TileStatus.SelectedButUnavailable
			);

			return;
		}

		string cardID = card.ID;

		if (IsCardEquipped(cardID) ||
			IsCardReserved(cardID))
		{
			tile.SetOverlayStatus(
				InventoryTile.TileStatus.SelectedAsTarget
			);
		}
		else
		{
			tile.SetOverlayStatus(
				InventoryTile.TileStatus.NotSelected,
				false
			);
		}
	}

	private void EquipCard(
		InventorySlotItem cardItem,
		int slotIndex)
	{
		if (mSave == null)
		{
			return;
		}

		if (slotIndex < 0 ||
			slotIndex >= LeaderFloopSave.EQUIPPED_COUNT)
		{
			return;
		}

		if (!CanEquipLeaderFloop(cardItem))
		{
			return;
		}

		string cardID = cardItem.Card.Form.ID;

		if (IsCardEquipped(cardID) ||
			IsCardReserved(cardID))
		{
			return;
		}

		mSave.Equipped[slotIndex] = cardID;

		RefreshAll();
		PopulateCardList();
	}

	private bool CanEquipLeaderFloop(InventorySlotItem cardItem)
	{
		if (cardItem == null ||
			cardItem.Card == null ||
			cardItem.Card.Form == null)
		{
			return false;
		}

		CardData card = cardItem.Card.Form;

		if (!card.IsLeaderCard)
			return false;

		string cardID = card.ID;

		// Leader özel Floop kontrolü
		if (mSelectedLeader != null &&
			mSelectedLeader.FloopAllowOnly != null &&
			mSelectedLeader.FloopAllowOnly.Count > 0)
		{
			if (!mSelectedLeader.FloopAllowOnly.Contains(cardID))
			{
				return false;
			}
		}

		return true;
	}

	public void RemoveFromSlot(
		int equippedSlot)
	{
		if (mSave == null)
			return;

		if (equippedSlot < 0 ||
			equippedSlot >= 5)
		{
			return;
		}

		mSave.Equipped[equippedSlot] =
			string.Empty;

		RefreshAll();
		PopulateCardList();
	}


	private bool IsCardEquipped(
		string cardID)
	{
		if (mSave == null ||
			string.IsNullOrEmpty(cardID))
		{
			return false;
		}

		for (int i = 0; i < mSave.Equipped.Length; i++)
		{
			if (mSave.Equipped[i] == cardID)
				return true;
		}

		return false;
	}


	private bool IsCardReserved(
		string cardID)
	{
		if (mSave == null ||
			string.IsNullOrEmpty(cardID))
		{
			return false;
		}

		for (int i = 0; i < mSave.Reserve.Length; i++)
		{
			if (mSave.Reserve[i] == cardID)
				return true;
		}

		return false;
	}


	private bool IsValidLeaderFloop(
		string cardID)
	{
		if (string.IsNullOrEmpty(cardID))
			return false;

		CardData data =
			CardDataManager.Instance.GetData(cardID);

		if (data == null)
			return false;

		return data.IsLeaderCard;
	}


	private void ShowLeaderFloopBlockPopup(
		InventorySlotItem cardItem)
	{
		if (cardItem == null ||
			cardItem.Card == null ||
			cardItem.Card.Form == null)
		{
			return;
		}

		Singleton<SimplePopupController>
			.Instance
			.ShowMessage(
				"Card Cannot Be Equipped",
				"Only Leader Floop cards can be equipped here.",
				true
			);
	}


	public void OnClickClose()
	{
		if (mFinishedCallback != null)
		{
			mFinishedCallback();
			mFinishedCallback = null;
		}
	}


	public void Unload()
	{
		if (Portrait != null)
		{
			Portrait.UnloadTexture();
		}

		mCardGridDataSource.Clear();
		for (int i = 0; i < mSpawnedNativeCards.Count; i++)
		{
			if (mSpawnedNativeCards[i] != null)
			{
				NGUITools.Destroy(mSpawnedNativeCards[i].gameObject);
				mSpawnedNativeCards[i] = null;
			}
		}
	}

	public GameObject GetUnusedCard()
	{
		InventorySlotItem dataItem = Singleton<PlayerInfoScript>.Instance.SaveData.FindExCard((CardItem m) => m.CreatureUID == 0);
		return mCardGridDataSource.FindPrefab(dataItem);
	}

	public void OnClickSort()
	{
		Singleton<SortPopupController>
			.Instance
			.Show(
				SortPopupController.Category.Cards,
				SortButton,
				PopulateCardList
			);
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

	public void OnClickSlot5()
	{
	}


	private void RefreshLeaderTexture()
	{
		if (mSelectedLeader == null || Portrait == null)
			return;

		Singleton<SLOTResourceManager>.Instance.QueueUITextureLoad(
			mSelectedLeader.PortraitTexture,
			"FTUEBundle",
			"UI/UI/LoadingPlaceholder",
			Portrait
		);
	}

	private bool CanShowFloop(InventorySlotItem item)
	{
		if (item == null ||
			item.Card == null ||
			item.Card.Form == null)
		{
			return false;
		}

		CardData card = item.Card.Form;

		// Sadece Leader Floop kartları
		if (!card.IsLeaderCard)
			return false;


		// Seçili Leader'ın izin verdiği Floop mu?
		if (mSelectedLeader != null &&
			!mSelectedLeader.IsFloopAllowed(card.ID))
		{
			return false;
		}

		return true;
	}
}
