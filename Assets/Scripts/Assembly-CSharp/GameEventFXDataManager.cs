using System.IO;

public class GameEventFXDataManager : DataManager<GameEventFXData>
{
	private static GameEventFXDataManager _instance;

	public static GameEventFXDataManager Instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = new GameEventFXDataManager(Path.Combine(SQSettings.CDN_URL, "Blueprints", "db_GameEventFX.json"));
			}
			return _instance;
		}
	}

	public GameEventFXDataManager(string path)
	{
		base.FilePath = path;
	}
}
