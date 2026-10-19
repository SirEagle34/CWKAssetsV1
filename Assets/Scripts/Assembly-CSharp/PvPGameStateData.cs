using System;
using UnityEngine;

[Serializable]
public class PvPGameStateData
{
	public enum MatchType
	{
		Unranked = 0,
		Ranked = 1,
		Extreme = 2
	}

	public Loadout OpponentLoadout;

	public Loadout CurrentMatchLoadout;

	public bool AmIPrimary;

	public bool RankedMode;

	public bool ExtremeMode;

	public MatchType CurrentMatchType;

	public bool WonInitialCoinFlip;

	public string MatchId;

	public string OpponentName;

	public string OpponentID;

	public int OpponentLevel;

	public int OpponentBestLevel;

	public string OpponentFBID;

	public Texture2D OpponentPortrait;

	public string PlayerName;

	public string PlayerRank;

	public int TotalTrophies;

	public CardBackData OpponentCardBack;

	public PlayerPortraitData OpponentPortraitData;

	public BattleHistory HistoryEntryForThisMatch;

	public bool OpponentIsExtreme;
}
