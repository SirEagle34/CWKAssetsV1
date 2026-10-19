using System;
using System.Collections;
using System.Collections.Generic;
using CodeStage.AntiCheat.ObscuredTypes;
using Multiplayer;
using UnityEngine;

public class PvPModeSelectController : Singleton<PvPModeSelectController>
{
	private const int LeaderboardPageSize = 50;

	[Header("Tab to Click on Init")]
	public GameObject InitTab;

	public UILabel NameLabel;

	public UILabel RankNumberLabel;

	public UILabel CurrentLeagueLabel;

	public LeagueBadge Badge;

	public UILabel SeasonLabel;

	public UILabel SeasonName;

	public UILabel BlackListLabel;

	public UILabel BlackList1;

	public UILabel BlackList2;

	public UILabel BlackList3;

	public UILabel BlackList4;

	public UILabel BlackList5;

	public UILabel BlackList6;

	public UILabel BlackList7;

	public UILabel BlackList8;

	public UILabel SeasonRulesLabel;

	public UILabel Rules1;

	public UILabel Rules2;

	public UILabel RulesUsage;

	public UILabel RulesUsage2;

	public UILabel RulesUsage3;

	public UITexture SeasonBanner;

	public SeasonProgressBar SeasonProgressBar;

	public GameObject RankedParent;

	public GameObject NotRankedParent;

	public GameObject PromotionMatchLabel;

	public UIStreamingGrid LeaderboardGrid;

	public GameObject LeaderboardListPrefab;

	public SeasonLabelColorPallette[] SeasonLabelColors = new SeasonLabelColorPallette[0];

	[Header("Tweens")]
	public UITweenController ShowTween;

	public UITweenController HideTween;

	public UITweenController PromotionMatchTween;

	public UITweenController FetchingLeaderboardsTween;

	public UITweenController HideFetchingLeaderboardsTween;

	public UITweenController HideElementsTween;

	public UITweenController ShowElementsTween;

	private UIStreamingGridDataSource<LeaderboardData> mLeaderboardGridDataSource = new UIStreamingGridDataSource<LeaderboardData>();

	private bool mInPromotionMatch;

	private bool mShowing;

	private PvpSeasonData _ActiveSeason;

	[Header("Optional References")]
	[SerializeField]
	private PVPPrepScreenController PrepScreenController;

	private const string KnownPrepRootName = "UI_FrontEnd_PVP_Vs";

	public PvpMode CurrentSelectedMode { get; private set; } = PvpMode.Unranked;

	public void Populate()
	{
		LeaderboardManager componentInChildren = GetComponentInChildren<LeaderboardManager>(includeInactive: true);
		if (componentInChildren != null && SessionManager.Instance != null && SessionManager.Instance.theSession != null)
		{
			componentInChildren.LoadLeaderboard();
		}
		Singleton<PlayerInfoScript>.Instance.CheckPvpSeasonStatus();
		PlayerSaveData saveData = Singleton<PlayerInfoScript>.Instance.SaveData;
		PvpSeasonData pvpSeasonData = (_ActiveSeason = Singleton<PlayerInfoScript>.Instance.SaveData.ActivePvpSeason);
		if (pvpSeasonData.IsExpired())
		{
			SeasonLabel.text = string.Empty;
		}
		else
		{
			SeasonLabel.text = string.Format(Language.Get("!!SEASON_ENDS"), pvpSeasonData.GetDurationString());
		}
		SeasonName.text = pvpSeasonData.Name;
		BlackListLabel.text = pvpSeasonData.BlackListLabel;
		BlackList1.text = pvpSeasonData.BlackList;
		BlackList2.text = pvpSeasonData.BlackList2;
		BlackList3.text = pvpSeasonData.BlackList3;
		BlackList4.text = pvpSeasonData.BlackList4;
		BlackList5.text = pvpSeasonData.BlackList5;
		BlackList6.text = pvpSeasonData.BlackList6;
		BlackList7.text = pvpSeasonData.BlackList7;
		BlackList8.text = pvpSeasonData.BlackList8;
		SeasonRulesLabel.text = pvpSeasonData.SeasonRulesLabel;
		Rules1.text = pvpSeasonData.Rules;
		Rules2.text = pvpSeasonData.Rules2;
		RulesUsage.text = pvpSeasonData.RulesUsage;
		RulesUsage2.text = pvpSeasonData.RulesUsage2;
		RulesUsage3.text = pvpSeasonData.RulesUsage3;
		ColorSeasonNameLabel(pvpSeasonData.BannerTextureNameOnly);
		Singleton<SLOTResourceManager>.Instance.QueueResourceLoad(pvpSeasonData.BannerTexture, "GeneralBundle", delegate(UnityEngine.Object loadedResouce)
		{
			SeasonBanner.UnloadTexture();
			SeasonBanner.mainTexture = loadedResouce as Texture;
			SafePlayWithCallback(ShowTween, ShowTweenDone);
		});
		if (saveData.PlayedFirstBattleInPvpSeason)
		{
			RankedParent.SetActive(value: true);
			NotRankedParent.SetActive(value: false);
			string arg = PvpSeasonDataManager.Instance.RankName(saveData.MultiplayerLevel);
			CurrentLeagueLabel.text = string.Format(Language.Get("!!DYNAMIC_LEAGUE"), arg);
			RankNumberLabel.text = saveData.MultiplayerLevel.ToString();
			Badge.PopulateMyData();
			PvpRankData rank = PvpRankDataManager.Instance.GetRank(saveData.MultiplayerLevel);
			if (rank == null)
			{
				saveData.MultiplayerLevel = PvpRankDataManager.Instance.GetMaxRank();
				rank = PvpRankDataManager.Instance.GetRank(saveData.MultiplayerLevel);
				RankNumberLabel.text = saveData.MultiplayerLevel.ToString();
			}
			ObscuredInt pointsInMultiplayerLevel = saveData.PointsInMultiplayerLevel;
			int pointsToAdvance = rank.PointsToAdvance;
			SeasonProgressBar.AnimateTo(pointsInMultiplayerLevel, pointsInMultiplayerLevel, pointsToAdvance);
			SeasonProgressBar.gameObject.SetActive(rank.Rank > 1);
			PromotionMatchLabel.SetActive(value: false);
			mInPromotionMatch = saveData.MultiplayerLevel != 1 && (int)pointsInMultiplayerLevel == pointsToAdvance;
		}
		else
		{
			RankedParent.SetActive(value: false);
			NotRankedParent.SetActive(value: true);
			NameLabel.text = Singleton<PlayerInfoScript>.Instance.SaveData.MultiplayerPlayerName;
		}
		mShowing = true;
		Singleton<TutorialController>.Instance.AdvanceIfTargetingBuilding("TBuilding_PVP");
		if (!Singleton<TutorialController>.Instance.IsAnyTutorialActive())
		{
			UpsightRequester.RequestContent("enter_pvp");
		}
		Invoke("GoToInitTab", 0.1f);
	}

	public void Hide()
	{
		SafePlay(HideTween);
	}

	private void GoToInitTab()
	{
		ColorSeasonNameLabel(_ActiveSeason.BannerTextureNameOnly);
		if (InitTab != null)
		{
			InitTab.SendMessage("OnClick");
		}
		InitTab = null;
	}

	private void ShowTweenDone()
	{
		if (mInPromotionMatch)
		{
			SafePlay(PromotionMatchTween);
		}
	}

	private IEnumerator PopulateLeaderboard()
	{
		List<LeaderboardData> fetchedEntries = null;
		bool done = false;
		SafePlay(FetchingLeaderboardsTween);
		global::Multiplayer.Multiplayer.TopLeaderboard(SessionManager.Instance.theSession, delegate(List<LeaderboardData> leaderboardEntries)
		{
			fetchedEntries = leaderboardEntries;
			done = true;
		});
		while (!done)
		{
			yield return null;
		}
		SafePlay(HideFetchingLeaderboardsTween);
		if (mShowing)
		{
			if (fetchedEntries == null)
			{
				Debug.LogWarning("[PvP] leaderboard fetch failed; showing empty board.");
				fetchedEntries = new List<LeaderboardData>();
			}
			mLeaderboardGridDataSource.Init(LeaderboardGrid, LeaderboardListPrefab, fetchedEntries);
		}
	}

	public void OnClickRankedMatch()
	{
		if (MiscParams.DisablePvp)
		{
			Singleton<SimplePopupController>.Instance.ShowMessage(string.Empty, KFFLocalization.Get("!!PVP_DISABLED"));
		}
		else if (LiveChannel.Allowed("PvP_Ranked"))
		{
			CurrentSelectedMode = PvpMode.Ranked;
			Singleton<PlayerInfoScript>.Instance.PvPData.RankedMode = true;
			PVPPrepScreenController pVPPrepScreenController = FindPrepController();
			if (pVPPrepScreenController != null)
			{
				pVPPrepScreenController.Show(PvpMode.Ranked, null, inShouldShowBackButton: true);
			}
			else
			{
				Singleton<SimplePopupController>.Instance.ShowMessage(string.Empty, "PvP Screen not available. Please try again.");
			}
		}
	}

	public void OnClickUnrankedMatch()
	{
		if (MiscParams.DisablePvp)
		{
			Singleton<SimplePopupController>.Instance.ShowMessage(string.Empty, KFFLocalization.Get("!!PVP_DISABLED"));
		}
		else if (LiveChannel.Allowed("PvP_Unranked"))
		{
			CurrentSelectedMode = PvpMode.Unranked;
			Singleton<PlayerInfoScript>.Instance.PvPData.RankedMode = false;
			PVPPrepScreenController pVPPrepScreenController = FindPrepController();
			if (pVPPrepScreenController != null)
			{
				pVPPrepScreenController.Show(PvpMode.Unranked, null);
			}
			else
			{
				Singleton<SimplePopupController>.Instance.ShowMessage(string.Empty, "PvP Screen not available. Please try again.");
			}
		}
	}

	public void OnClickExtremeMatch()
	{
		if (MiscParams.DisablePvp)
		{
			Singleton<SimplePopupController>.Instance.ShowMessage(string.Empty, KFFLocalization.Get("!!PVP_DISABLED"));
		}
		else if (LiveChannel.Allowed("PvP_Extreme"))
		{
			CurrentSelectedMode = PvpMode.Extreme;
			Singleton<PlayerInfoScript>.Instance.PvPData.RankedMode = false;
			PVPPrepScreenController pVPPrepScreenController = FindPrepController();
			if (pVPPrepScreenController != null)
			{
				pVPPrepScreenController.Show(PvpMode.Extreme, null);
			}
			else
			{
				Singleton<SimplePopupController>.Instance.ShowMessage(string.Empty, "PvP Screen not available. Please try again.");
			}
		}
	}

	public void OnClickAllyMatch()
	{
		if (MiscParams.DisablePvp)
		{
			Singleton<SimplePopupController>.Instance.ShowMessage(string.Empty, KFFLocalization.Get("!!PVP_DISABLED"));
		}
		else if (LiveChannel.Allowed("PvP_Ally"))
		{
			SafePlay(HideElementsTween);
			Singleton<PvPAllySelectController>.Instance.Populate();
		}
	}

	public void OnClickBattleHistory()
	{
		SafePlay(HideElementsTween);
		BattleHistoryController.Instance.Show();
	}

	public void OnClickRewards()
	{
		SafePlay(HideElementsTween);
		Singleton<SeasonRewardsScreen>.Instance.Show();
	}

	public void OnClickClose()
	{
		mShowing = false;
	}

	public void Unload()
	{
		SeasonBanner.UnloadTexture();
		mLeaderboardGridDataSource.Clear();
		Singleton<TownHudController>.Instance.ReturnToTownView();
	}

	public void ShowElements()
	{
		SafePlay(ShowElementsTween);
	}

	private void ColorSeasonNameLabel(string inBannerTextureNameOnly)
	{
		int num = 0;
		switch (inBannerTextureNameOnly)
		{
		case "PVP_Season_Cinnamon":
			num = 1;
			break;
		case "PVP_Season_Mint":
			num = 2;
			break;
		case "PVP_Season_Nutmeg":
			num = 3;
			break;
		case "PVP_Season_ToeSalt":
			num = 4;
			break;
		case "PVP_Season_SkullKing":
			num = 5;
			break;
		}
		SeasonLabelColorPallette seasonLabelColorPallette = SeasonLabelColors[num];
		SeasonName.color = seasonLabelColorPallette.TextColor;
		SeasonName.effectColor = seasonLabelColorPallette.OutlineColor;
		if (SeasonName.LabelShadow != null)
		{
			SeasonName.LabelShadow.ShadowTextColor = seasonLabelColorPallette.ShadowTextColor;
			SeasonName.LabelShadow.ShadowEffectColor = seasonLabelColorPallette.ShadowOutlineColor;
			SeasonName.LabelShadow.RefreshShadowLabel();
		}
	}

	private void SafePlay(UITweenController tween)
	{
		if (tween == null)
		{
			return;
		}
		try
		{
			tween.Play();
		}
		catch (Exception ex)
		{
			Debug.LogWarning("[PvPModeSelectController] SafePlay failed: " + ex.Message);
		}
	}

	private void SafeStopAndReset(UITweenController tween)
	{
		if (tween == null)
		{
			return;
		}
		try
		{
			tween.StopAndReset();
		}
		catch (Exception ex)
		{
			Debug.LogWarning("[PvPModeSelectController] SafeStopAndReset failed: " + ex.Message);
		}
	}

	private void SafePlayWithCallback(UITweenController tween, EventDelegate.Callback callback)
	{
		if (tween == null)
		{
			callback?.Invoke();
			return;
		}
		try
		{
			tween.PlayWithCallback(callback);
		}
		catch (Exception ex)
		{
			Debug.LogWarning("[PvPModeSelectController] SafePlayWithCallback failed: " + ex.Message);
			callback?.Invoke();
		}
	}

	private PVPPrepScreenController FindPrepController()
	{
		if (PrepScreenController != null)
		{
			return PrepScreenController;
		}
		PVPPrepScreenController instance = Singleton<PVPPrepScreenController>.Instance;
		if (instance != null)
		{
			return instance;
		}
		PVPPrepScreenController pVPPrepScreenController = UnityEngine.Object.FindObjectOfType<PVPPrepScreenController>(includeInactive: true);
		if (pVPPrepScreenController != null)
		{
			return pVPPrepScreenController;
		}
		GameObject gameObject = GameObject.Find("UI_FrontEnd_PVP_Vs");
		if (gameObject != null)
		{
			PVPPrepScreenController componentInChildren = gameObject.GetComponentInChildren<PVPPrepScreenController>(includeInactive: true);
			if (componentInChildren != null)
			{
				return componentInChildren;
			}
		}
		return null;
	}
}
