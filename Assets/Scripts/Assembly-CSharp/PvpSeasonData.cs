using System;
using System.Collections.Generic;
using System.Globalization;

public class PvpSeasonData : ILoadableData
{
	public class RankRewards
	{
		public List<GeneralReward> Rewards;
	}

	public class LeaderboardRewards
	{
		public List<GeneralReward> Rewards;

		public int Position;
	}

	private List<RankRewards> mRankRewards;

	private List<LeaderboardRewards> mLeaderboardRewards = new List<LeaderboardRewards>();

	public string ID { get; private set; }

	public DateTime EndDate { get; private set; }

	public PvpSeasonData PreviousSeason { get; set; }

	public string Name { get; private set; }

	public string BlackListLabel { get; private set; }

	public string BlackList { get; private set; }

	public string BlackList2 { get; private set; }

	public string BlackList3 { get; private set; }

	public string BlackList4 { get; private set; }

	public string BlackList5 { get; private set; }

	public string BlackList6 { get; private set; }

	public string BlackList7 { get; private set; }

	public string BlackList8 { get; private set; }

	public string SeasonRulesLabel { get; private set; }

	public string Rules { get; private set; }

	public string Rules2 { get; private set; }

	public string RulesUsage { get; private set; }

	public string RulesUsage2 { get; private set; }

	public string RulesUsage3 { get; private set; }

	public string BannerTexture { get; private set; }

	public string BannerTextureNameOnly { get; private set; }

	public void Populate(Dictionary<string, object> dict)
	{
		ID = TFUtils.LoadString(dict, "Season", string.Empty);
		EndDate = DateTime.ParseExact(TFUtils.LoadString(dict, "EndDate", string.Empty), "M/d/yyyy", CultureInfo.InvariantCulture);
		int count = PvpRankDataManager.Instance.GetDatabase().Count;
		mRankRewards = new List<RankRewards>(count);
		for (int i = 0; i < count; i++)
		{
			mRankRewards.Add(null);
		}
		Name = TFUtils.LoadLocalizedString(dict, "Name", string.Empty);
		BlackListLabel = TFUtils.LoadLocalizedString(dict, "BlackListLabel", string.Empty);
		BlackList = TFUtils.LoadLocalizedString(dict, "BlackList1", string.Empty);
		BlackList2 = TFUtils.LoadLocalizedString(dict, "BlackList2", string.Empty);
		BlackList3 = TFUtils.LoadLocalizedString(dict, "BlackList3", string.Empty);
		BlackList4 = TFUtils.LoadLocalizedString(dict, "BlackList4", string.Empty);
		BlackList5 = TFUtils.LoadLocalizedString(dict, "BlackList5", string.Empty);
		BlackList6 = TFUtils.LoadLocalizedString(dict, "BlackList6", string.Empty);
		BlackList7 = TFUtils.LoadLocalizedString(dict, "BlackList7", string.Empty);
		BlackList8 = TFUtils.LoadLocalizedString(dict, "BlackList8", string.Empty);
		SeasonRulesLabel = TFUtils.LoadLocalizedString(dict, "SeasonRulesLabel", string.Empty);
		Rules = TFUtils.LoadLocalizedString(dict, "Rules1", string.Empty);
		Rules2 = TFUtils.LoadLocalizedString(dict, "Rules2", string.Empty);
		RulesUsage = TFUtils.LoadLocalizedString(dict, "RulesUsage", string.Empty);
		RulesUsage2 = TFUtils.LoadLocalizedString(dict, "RulesUsage2", string.Empty);
		RulesUsage3 = TFUtils.LoadLocalizedString(dict, "RulesUsage3", string.Empty);
		BannerTexture = "UI/GeneralBundle/SeasonBanners/" + TFUtils.LoadString(dict, "BannerTexture", string.Empty);
		BannerTextureNameOnly = TFUtils.LoadString(dict, "BannerTexture", string.Empty);
		AddRewardRow(dict);
	}

	public void AddRewardRow(Dictionary<string, object> dict)
	{
		int num = TFUtils.LoadInt(dict, "LeaderboardPos", -1);
		if (num != -1)
		{
			LeaderboardRewards leaderboardRewards = new LeaderboardRewards();
			leaderboardRewards.Rewards = GeneralReward.ParseFromJson(dict);
			leaderboardRewards.Position = num;
			mLeaderboardRewards.Add(leaderboardRewards);
			return;
		}
		int num2 = TFUtils.LoadInt(dict, "Rank", 0);
		if (num2 >= 1 && num2 <= mRankRewards.Count)
		{
			_ = mRankRewards[num2 - 1];
			mRankRewards[num2 - 1] = new RankRewards();
			mRankRewards[num2 - 1].Rewards = GeneralReward.ParseFromJson(dict);
		}
	}

	public List<GeneralReward> GetRankRewards(int rank)
	{
		if (mRankRewards[rank - 1] == null)
		{
			return new List<GeneralReward>();
		}
		return mRankRewards[rank - 1].Rewards;
	}

	public List<GeneralReward> GetLeaderboardRewardsForPlacement(int placement)
	{
		List<GeneralReward> list = new List<GeneralReward>();
		foreach (LeaderboardRewards mLeaderboardReward in mLeaderboardRewards)
		{
			if (placement <= mLeaderboardReward.Position)
			{
				list.AddRange(mLeaderboardReward.Rewards);
			}
		}
		return list;
	}

	public string GetEndDateString()
	{
		return EndDate.ToShortDateString();
	}

	public bool IsExpired()
	{
		return TFUtils.ServerTime > EndDate;
	}

	public string GetDurationString()
	{
		return PlayerInfoScript.FormatTimeString((int)(EndDate - TFUtils.ServerTime).TotalSeconds);
	}
}
