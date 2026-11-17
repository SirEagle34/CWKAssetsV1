using System.Collections.Generic;
using UnityEngine;

public class CustomPreMatchController : MonoBehaviour
{
	public GameObject Panel;
	public UITweenController ShowTween;
	public UITweenController ExceededTeamCostTween;
	public UITexture[] PIPIconTargets = new UITexture[2];
	public UITexture[] PlayerBackgroundTextures = new UITexture[2];
	public Transform[] MyCreatureNodes;
	public Transform[] EnemyCreatureNodes;
	public Transform[] RewardNodes;
	public UILabel QuestName;
	public UILabel TeamName;
	public UILabel TeamCost;
	public UILabel PlayerStamina;
	public UILabel PlayerStaminaTimer;
	public UILabel StaminaCost;
	public UILabel[] LeaderNames = new UILabel[2];
	[HideInInspector]
	public UITexture PlayerLeaderPortrait;
	[HideInInspector]
	public UITexture PlayerBackgroundTexture;
	public GameObject EditDeckButton;
	public GameObject StartButton;

	private readonly List<GameObject> mMyTiles = new List<GameObject>();
	private readonly List<GameObject> mEnemyTiles = new List<GameObject>();
	private readonly List<GameObject> mRewardTiles = new List<GameObject>();
	private CustomQuestController.QuestEntry mDungeon;
	private bool mShowingPortrait;

	public bool Show(CustomQuestController.QuestEntry dungeon)
	{
		if (PIPIconTargets != null && PIPIconTargets.Length > 0 && PIPIconTargets[0] == null)
			PIPIconTargets[0] = PlayerLeaderPortrait;
		if (PlayerBackgroundTextures != null && PlayerBackgroundTextures.Length > 0 && PlayerBackgroundTextures[0] == null)
			PlayerBackgroundTextures[0] = PlayerBackgroundTexture;
		PlayerInfoScript player = Singleton<PlayerInfoScript>.Instance;
		Loadout loadout = player != null ? player.GetCurrentLoadout() : null;
		GameObject tilePrefab = Singleton<PrefabReferences>.Instance != null ? Singleton<PrefabReferences>.Instance.InventoryTile : null;
		InventoryTile inventoryTile = tilePrefab != null ? tilePrefab.GetComponent<InventoryTile>() : null;
		if (Panel == null || QuestName == null || TeamName == null || TeamCost == null ||
			LeaderNames == null || LeaderNames.Length < 2 || LeaderNames[0] == null || LeaderNames[1] == null ||
			PIPIconTargets == null || PIPIconTargets.Length != 2 || PIPIconTargets[0] == null || PIPIconTargets[1] == null ||
			PIPIconTargets[0] == PIPIconTargets[1] || Singleton<FrontEndPIPController>.Instance == null ||
			PlayerBackgroundTextures == null || PlayerBackgroundTextures.Length != 2 ||
			PlayerBackgroundTextures[0] == null || PlayerBackgroundTextures[1] == null ||
			PlayerBackgroundTextures[0] == PlayerBackgroundTextures[1] ||
			dungeon == null || dungeon.Creatures == null || dungeon.Creatures.Count != 12 ||
			dungeon.Rewards == null || dungeon.Rewards.Count < 1 || dungeon.Rewards.Count > 6 ||
			loadout == null || loadout.Leader == null || loadout.Leader.Form == null ||
			loadout.Leader.SelectedSkin == null || player.RankData == null ||
			!ValidNodes(MyCreatureNodes, loadout.CreatureSet.Count, false) ||
			!ValidNodes(EnemyCreatureNodes, 12, true) || !ValidNodes(RewardNodes, 6, true) ||
			inventoryTile == null || inventoryTile.CurrencyParent == null || inventoryTile.CurrencySprite == null ||
			inventoryTile.CurrencyAmount == null || inventoryTile.PortraitTexture == null)
		{
			Debug.LogError("[Olympic] Custom pre-match data or scene references are invalid.");
			Singleton<SimplePopupController>.Instance.ShowMessage("Olympic Dungeon", "The pre-match screen is not configured correctly.");
			return false;
		}
		Unload();
		mDungeon = dungeon;
		Panel.SetActive(true);
		QuestName.text = dungeon.Name;
		LeaderNames[1].text = dungeon.OwnerLeader != null ? dungeon.OwnerLeader.Name : dungeon.OwnerName;
		if (StaminaCost != null) StaminaCost.text = string.Empty;
		if (EditDeckButton != null) EditDeckButton.SetActive(player.CanEditDeck());
		if (StartButton != null) StartButton.SetActive(true);
		else Debug.LogWarning("[Olympic] Assign the Start Button in CustomPreMatchController.");
		if (Singleton<FrontEndPIPController>.Instance != null)
		{
			Singleton<FrontEndPIPController>.Instance.HideModelPortrait();
		}
		RefreshMyLoadout();
		RefreshEnemyAndRewards();
		UpdateTimers();
		if (ShowTween != null) ShowTween.Play();
		return true;
	}

	private static bool ValidNodes(Transform[] nodes, int required, bool exact)
	{
		if (nodes == null || nodes.Length < required || (exact && nodes.Length != required))
		{
			return false;
		}
		for (int i = 0; i < required; i++)
		{
			if (nodes[i] == null) return false;
		}
		return true;
	}

	public void RefreshMyLoadout()
	{
		if (mDungeon == null) return;
		PlayerInfoScript player = Singleton<PlayerInfoScript>.Instance;
		Loadout loadout = player.GetCurrentLoadout();
		if (loadout == null || loadout.Leader == null || loadout.Leader.Form == null || loadout.Leader.SelectedSkin == null ||
			!ValidNodes(MyCreatureNodes, loadout.CreatureSet.Count, false))
		{
			Debug.LogError("[Olympic] Player loadout or creature nodes are unavailable.");
			Singleton<SimplePopupController>.Instance.ShowMessage("Olympic Dungeon", "The selected team cannot be displayed.");
			return;
		}
		ClearTiles(mMyTiles);
		InventoryTile.ClearDelegates(true);
		for (int i = 0; i < loadout.CreatureSet.Count; i++)
		{
			InventorySlotItem creature = loadout.CreatureSet[i];
			if (creature == null) continue;
			GameObject tile = MyCreatureNodes[i].InstantiateAsChild(Singleton<PrefabReferences>.Instance.InventoryTile);
			mMyTiles.Add(tile);
			tile.ChangeLayer(MyCreatureNodes[i].gameObject.layer);
			tile.transform.localScale = Vector3.one * 0.9f;
			tile.GetComponent<InventoryTile>().Populate(creature);
		}
		TeamName.text = player.GetTeamName(player.SaveData.SelectedLoadout);
		int cost = loadout.GetTeamCost();
		int maxCost = player.RankData.TeamCost;
		TeamCost.text = KFFLocalization.Get("!!TEAM_WEIGHT") + "  " + cost + " / " + maxCost;
		LeaderNames[0].text = loadout.Leader.Form.Name;
		PlayerBackgroundTextures[0].ReplaceTexture(loadout.Leader.Form.VSScreenBackground);
		if (mDungeon.OwnerLeader != null)
		{
			PlayerBackgroundTextures[1].ReplaceTexture(mDungeon.OwnerLeader.VSScreenBackground);
		}
		else
		{
			PlayerBackgroundTextures[1].UnloadTexture();
		}
		if (ExceededTeamCostTween != null)
		{
			if (cost > maxCost) ExceededTeamCostTween.Play();
			else ExceededTeamCostTween.StopAndReset();
		}
		if (Singleton<FrontEndPIPController>.Instance != null)
		{
			Singleton<FrontEndPIPController>.Instance.ShowModelPortraits(
				new LeaderData[] { loadout.Leader.SelectedSkin, mDungeon.OwnerLeaderSkin }, PIPIconTargets);
			mShowingPortrait = true;
		}
	}

	private void RefreshEnemyAndRewards()
	{
		ClearTiles(mEnemyTiles);
		ClearTiles(mRewardTiles);
		InventoryTile.ClearDelegates(false);
		for (int i = 0; i < mDungeon.Creatures.Count; i++)
		{
			Dictionary<string, object> data = (Dictionary<string, object>)mDungeon.Creatures[i];
			GameObject tile = EnemyCreatureNodes[i].InstantiateAsChild(Singleton<PrefabReferences>.Instance.InventoryTile);
			mEnemyTiles.Add(tile);
			tile.ChangeLayer(EnemyCreatureNodes[i].gameObject.layer);
			tile.transform.localScale = Vector3.one * 0.9f;
			InventoryTile inventory = tile.GetComponent<InventoryTile>();
			inventory.Populate(new InventorySlotItem(new CreatureItem(data, true)));
			inventory.DisableCardEditing = true;
			foreach (Collider collider in tile.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
		}
		for (int i = 0; i < mDungeon.Rewards.Count; i++)
		{
			mRewardTiles.Add(CustomQuestController.SpawnRewardTile(RewardNodes[i], RewardNodes[i].gameObject.layer, mDungeon.Rewards[i]));
		}
	}

	private void Update()
	{
		if (mDungeon != null && Panel != null && Panel.activeInHierarchy) UpdateTimers();
	}

	private void UpdateTimers()
	{
		if (PlayerStamina == null || PlayerStaminaTimer == null || !PlayerStamina.gameObject.activeInHierarchy) return;
		int current;
		int maximum;
		int seconds;
		DetachedSingleton<StaminaManager>.Instance.GetStaminaInfo(StaminaType.Quests, out current, out maximum, out seconds);
		PlayerStamina.text = maximum.ToString();
		PlayerStaminaTimer.text = seconds <= 0 ? string.Empty : PlayerInfoScript.BuildTimerString(seconds);
	}

	public void OnClickNextLoadout()
	{
		if (mDungeon == null) return;
		Singleton<PlayerInfoScript>.Instance.GoToNextLoadout();
		RefreshMyLoadout();
	}

	public void OnClickPrevLoadout()
	{
		if (mDungeon == null) return;
		Singleton<PlayerInfoScript>.Instance.GoToPrevLoadout();
		RefreshMyLoadout();
	}

	public void OnClickEditDeck()
	{
		if (mDungeon == null) return;
		Singleton<EditDeckController>.Instance.Show(delegate
		{
			if (this != null && mDungeon != null) RefreshMyLoadout();
		});
	}

	public void OnClickMyLeader()
	{
		if (mDungeon != null)
		{
			Singleton<LeaderDetailsController>.Instance.Show(Singleton<PlayerInfoScript>.Instance.GetCurrentLoadout().Leader,
				delegate { if (this != null && mDungeon != null) RefreshMyLoadout(); });
		}
	}

	public void OnClickOpponentLeader()
	{
		if (mDungeon == null) return;
		LeaderData leader = mDungeon.OwnerLeaderSkin != null ? mDungeon.OwnerLeaderSkin : mDungeon.OwnerLeader;
		if (leader == null)
		{
			Debug.LogWarning("[Olympic] Dungeon creator's leader is unavailable.");
			Singleton<SimplePopupController>.Instance.ShowMessage("Olympic Dungeon", "The opponent's leader is unavailable.");
			return;
		}
		Singleton<LeaderDetailsController>.Instance.ShowOpponent(leader);
	}

	public void OnClickPlay()
	{
		if (mDungeon == null) return;

		PlayerInfoScript player = Singleton<PlayerInfoScript>.Instance;
		Loadout loadout = player != null ? player.GetCurrentLoadout() : null;
		if (player == null || loadout == null || loadout.Leader == null ||
			loadout.CreatureSet == null || !loadout.IsUsable() ||
			mDungeon.OwnerLeader == null || mDungeon.Creatures == null || mDungeon.Creatures.Count != 12)
		{
			Singleton<SimplePopupController>.Instance.ShowMessage("Olympic Dungeon", "The player or opponent team is incomplete.");
			return;
		}

		if (loadout.GetTeamCost() > player.RankData.TeamCost)
		{
			Singleton<SimplePopupController>.Instance.ShowMessage(string.Empty, KFFLocalization.Get("!!EXCEEDS_WEIGHT"));
			return;
		}

		QuestData safeQuest = QuestDataManager.Instance.GetData(0);
		if (safeQuest == null)
		{
			Singleton<SimplePopupController>.Instance.ShowMessage("Olympic Dungeon", "The battle configuration is unavailable.");
			return;
		}

		GameStateData stateData = player.StateData;
		stateData.MultiplayerMode = false;
		stateData.CurrentActiveQuest = safeQuest;
		stateData.CurrentLoadout = loadout.Copy();
		stateData.ActiveQuestBonus = QuestBonusType.None;

		OlympicBattleData.Begin(mDungeon);
		UICamera.LockInput();
		if (Panel != null) Panel.SetActive(false);
		StartCoroutine(PlayOlympicBattle());
	}

	private IEnumerator PlayOlympicBattle()
	{
		if (Singleton<FrontEndPIPController>.Instance != null)
		{
			Singleton<FrontEndPIPController>.Instance.PlayBattleStartAnim();
			yield return new WaitForSeconds(2.5f);
		}

		Singleton<ScreenFadeController>.Instance.ShowLoadScreen(delegate
		{
			DetachedSingleton<SceneFlowManager>.Instance.LoadBattleScene();
		});
	}

	public void OnClickAddHelper()
	{
		Singleton<SimplePopupController>.Instance.ShowMessage("Olympic Dungeon", "Helpers are not available for Olympic pre-match yet.");
	}

	public void OnCloseClicked()
	{
		Unload();
		Singleton<FrontEndPIPController>.Instance.HideModelPortrait();
		if (Panel != null) Panel.SetActive(false);
	}

	public void Unload()
	{
		ClearTiles(mMyTiles);
		ClearTiles(mEnemyTiles);
		ClearTiles(mRewardTiles);
		mDungeon = null;
		if (mShowingPortrait && Singleton<FrontEndPIPController>.Instance != null)
		{
			Singleton<FrontEndPIPController>.Instance.HideModelPortrait();
		}
		if (PlayerBackgroundTextures != null)
		{
			foreach (UITexture background in PlayerBackgroundTextures)
				if (background != null) background.UnloadTexture();
		}
		mShowingPortrait = false;
	}

	private static void ClearTiles(List<GameObject> tiles)
	{
		foreach (GameObject tile in tiles)
		{
			if (tile != null) NGUITools.Destroy(tile);
		}
		tiles.Clear();
	}
}
