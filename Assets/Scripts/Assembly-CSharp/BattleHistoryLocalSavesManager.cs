using System;
using System.Collections.Generic;
using UnityEngine;

public class BattleHistoryLocalSavesManager
{
	private string cachedOpponentFlag;

	private static BattleHistoryLocalSavesManager _instance;

	public static BattleHistoryLocalSavesManager Instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = new BattleHistoryLocalSavesManager();
			}
			return _instance;
		}
	}

	public string CachedOpponentFlag
	{
		get
		{
			return cachedOpponentFlag;
		}
		set
		{
			cachedOpponentFlag = value;
		}
	}

	public BattleHistory SaveNewBattleHistory()
	{
		PlayerInfoScript instance = Singleton<PlayerInfoScript>.Instance;
		if (instance == null)
		{
			Debug.LogError("[BattleHistory] PlayerInfoScript.Instance is null.");
			return null;
		}
		if (instance.PvPData == null)
		{
			Debug.LogError("[BattleHistory] PvPData is null.");
			return null;
		}
		Loadout opponentLoadout = instance.PvPData.OpponentLoadout;
		if (opponentLoadout == null)
		{
			Debug.LogError("[BattleHistory] OpponentLoadout is null.");
			return null;
		}
		if (opponentLoadout.CreatureSet == null)
		{
			Debug.LogError("[BattleHistory] OpponentLoadout.CreatureSet is null.");
			return null;
		}
		PvpSeasonData pvpSeasonData = null;
		if ((instance.PvPData.RankedMode || instance.PvPData.ExtremeMode) && instance.SaveData != null)
		{
			pvpSeasonData = instance.SaveData.ActivePvpSeason;
		}
		string opponentName = instance.PvPData.OpponentName ?? string.Empty;
		string empty = string.Empty;
		string flag = string.Empty;
		if (Singleton<OnlinePvPManager>.Instance != null)
		{
			flag = Singleton<OnlinePvPManager>.Instance.GetOpponentCountryCode();
		}
		string season = ((pvpSeasonData != null) ? pvpSeasonData.ID : string.Empty);
		List<string> list = new List<string>();
		foreach (InventorySlotItem item in opponentLoadout.CreatureSet)
		{
			if (item == null)
			{
				Debug.LogError("[BattleHistory] OpponentLoadout contains null InventorySlotItem.");
				return null;
			}
			if (item.Creature == null)
			{
				Debug.LogError("[BattleHistory] OpponentLoadout contains InventorySlotItem with null Creature.");
				return null;
			}
			if (item.Creature.Form == null)
			{
				Debug.LogError("[BattleHistory] OpponentLoadout contains Creature with null Form. Creature UniqueId: " + item.Creature.UniqueId);
				return null;
			}
			if (string.IsNullOrEmpty(item.Creature.Form.ID))
			{
				Debug.LogError("[BattleHistory] Opponent Creature Form.ID is null or empty.");
				return null;
			}
			list.Add(item.Creature.Form.ID);
		}
		int opponentLevel = instance.PvPData.OpponentLevel;
		int opponentBestLevel = instance.PvPData.OpponentBestLevel;
		int bestLeagueSeasonID = 0;
		uint recordTime = DateTime.UtcNow.UnixTimestamp();
		string opponentId = instance.PvPData.OpponentID ?? string.Empty;
		string opponentFBId = instance.PvPData.OpponentFBID ?? string.Empty;
		string opponentLeader = string.Empty;
		if (opponentLoadout.Leader != null && opponentLoadout.Leader.SelectedSkin != null && !string.IsNullOrEmpty(opponentLoadout.Leader.SelectedSkin.ID))
		{
			opponentLeader = opponentLoadout.Leader.SelectedSkin.ID;
		}
		else
		{
			Debug.LogWarning("[BattleHistory] Opponent Leader or SelectedSkin is null.");
		}
		BattleHistory battleHistory = new BattleHistory(opponentName, empty, flag, season, _youWon: false, list, opponentLevel, opponentBestLevel, bestLeagueSeasonID, recordTime, opponentId, opponentFBId, opponentLeader);
		if (instance.SaveData == null)
		{
			Debug.LogError("[BattleHistory] SaveData is null.");
			return battleHistory;
		}
		if (instance.SaveData.BattleHistoryList == null)
		{
			instance.SaveData.BattleHistoryList = new List<BattleHistory>();
		}
		while (instance.SaveData.BattleHistoryList.Count >= MiscParams.MaxMatchesInHistory)
		{
			instance.SaveData.BattleHistoryList.RemoveAt(0);
		}
		instance.SaveData.BattleHistoryList.Add(battleHistory);
		return battleHistory;
	}

	public void FinalizeHistoryEntry(BattleHistory entry, bool youWon)
	{
		entry.youWon = youWon;
		if (Singleton<BattleReplayRecorder>.Instance != null)
		{
			string lastReplayID = Singleton<BattleReplayRecorder>.Instance.GetLastReplayID();
			if (!string.IsNullOrEmpty(lastReplayID))
			{
				entry.battleVideo = lastReplayID;
				Debug.Log("[BattleHistoryLocalSavesManager] Replay ID saved to history: " + lastReplayID);
			}
			else
			{
				Debug.LogWarning("[BattleHistoryLocalSavesManager] Replay ID is empty, replay not saved");
			}
		}
		else
		{
			Debug.LogWarning("[BattleHistoryLocalSavesManager] BattleReplayRecorder instance not found");
		}
	}

	public List<BattleHistory> getRankedMatches()
	{
		return Singleton<PlayerInfoScript>.Instance.SaveData.BattleHistoryList.FindAll((BattleHistory m) => m.season != string.Empty);
	}

	public List<BattleHistory> getExtremeMatches()
	{
		return Singleton<PlayerInfoScript>.Instance.SaveData.BattleHistoryList.FindAll((BattleHistory m) => m.season != string.Empty);
	}

	public List<BattleHistory> getUnrankedMatches()
	{
		return Singleton<PlayerInfoScript>.Instance.SaveData.BattleHistoryList.FindAll((BattleHistory m) => m.season == string.Empty);
	}
}
