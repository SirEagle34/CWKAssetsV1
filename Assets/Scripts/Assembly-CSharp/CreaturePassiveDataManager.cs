using System.IO;

public class CreaturePassiveDataManager : DataManager<CreaturePassiveData>
{
	private static CreaturePassiveDataManager _instance;

	public static CreaturePassiveDataManager Instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = new CreaturePassiveDataManager(Path.Combine(SQSettings.CDN_URL, "Blueprints", "db_CreaturePassives.json"));
			}
			return _instance;
		}
	}

	public CreaturePassiveDataManager(string path)
	{
		base.FilePath = path;
	}
}
