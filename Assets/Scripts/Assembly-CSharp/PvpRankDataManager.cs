using System.IO;

public class PvpRankDataManager : DataManager<PvpRankData>
{
	private static PvpRankDataManager _instance;

	public static PvpRankDataManager Instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = new PvpRankDataManager(Path.Combine(SQSettings.CDN_URL, "Blueprints", "db_PVPRanks.json"));
			}
			return _instance;
		}
	}

	public PvpRankDataManager(string path)
	{
		base.FilePath = path;
	}

	public PvpRankData GetRank(int rank)
	{
		if (rank < 1 || rank > DatabaseArray.Count)
		{
			return null;
		}
		return DatabaseArray[rank - 1];
	}

	public int GetMaxRank()
	{
		return DatabaseArray.Count;
	}
}
