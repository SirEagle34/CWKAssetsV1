using System.IO;

public class StatusDataManager : DataManager<StatusData>
{
	private static StatusDataManager _instance;

	public static StatusDataManager Instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = new StatusDataManager(Path.Combine(SQSettings.CDN_URL, "Blueprints", "db_StatusEffects.json"));
			}
			return _instance;
		}
	}

	public StatusDataManager(string path)
	{
		base.FilePath = path;
		AddDependency(GameEventFXDataManager.Instance);
	}
}
