using System.IO;

public class PvpExtremeTeamDataManager : DataManager<PvpExtremeTeamData>
{
	private static PvpExtremeTeamDataManager _instance;

	public static PvpExtremeTeamDataManager Instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = new PvpExtremeTeamDataManager(Path.Combine(SQSettings.CDN_URL, "Blueprints", "db_PVPExtremeList.json"));
			}
			return _instance;
		}
	}

	public PvpExtremeTeamDataManager(string path)
	{
		base.FilePath = path;
	}
}
