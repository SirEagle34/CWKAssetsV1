using System.Collections.Generic;

namespace Multiplayer
{
	public class LeaderboardData
	{
		public int rank;

		public int previousRank;

		public string name;

		public string icon;

		public string leader;

		public int leaderLevel;

		public int trophies;

		public int wins;

		public int losses;

		public LeaderboardData()
		{
		}

		public LeaderboardData(Dictionary<string, object> dict)
		{
			rank = TFUtils.LoadInt(dict, "ranking", TFUtils.LoadInt(dict, "rank", 0));
			previousRank = TFUtils.LoadInt(dict, "prev_rank", 0);
			name = TFUtils.LoadString(dict, "playername", TFUtils.LoadString(dict, "player_name", "Unknown"));
			icon = TFUtils.LoadString(dict, "sprite_name", TFUtils.LoadString(dict, "icon", string.Empty));
			leader = TFUtils.LoadString(dict, "rank_name", TFUtils.LoadString(dict, "leader", string.Empty));
			leaderLevel = TFUtils.LoadInt(dict, "multiplayer_level", TFUtils.LoadInt(dict, "leader_level", 0));
			trophies = TFUtils.LoadInt(dict, "score", TFUtils.LoadInt(dict, "points", TFUtils.LoadInt(dict, "trophies", 0)));
			wins = TFUtils.LoadInt(dict, "wins", 0);
			losses = TFUtils.LoadInt(dict, "losses", 0);
		}

		public static List<LeaderboardData> ProcessList(List<object> data)
		{
			List<LeaderboardData> list = new List<LeaderboardData>(data.Count);
			foreach (Dictionary<string, object> datum in data)
			{
				list.Add(new LeaderboardData(datum));
			}
			return list;
		}
	}
}
