using System.IO;

public class JokerPoolDataManager : DataManager<JokerPoolData>
{
	private static JokerPoolDataManager _instance;

	public static JokerPoolDataManager Instance
	{
		get
		{
			if (_instance == null)
			{
				string path = Path.Combine(
					SQSettings.CDN_URL,
					"Blueprints",
					"db_JokerPool.json"
				);

				_instance = new JokerPoolDataManager(path);
			}

			return _instance;
		}
	}

	public JokerPoolDataManager(string path)
	{
		base.FilePath = path;
	}
}