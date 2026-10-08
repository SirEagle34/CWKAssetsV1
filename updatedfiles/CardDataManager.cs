using System.IO;

public class CardDataManager : DataManager<CardData>
{
	private static CardDataManager _instance;

	public static CardDataManager Instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = new CardDataManager(Path.Combine(SQSettings.CDN_URL, "Blueprints", "db_ActionCards.json"));
			}
			return _instance;
		}
	}

	public CardDataManager(string path)
	{
		base.FilePath = path;
		AddDependency(StatusDataManager.Instance);
	}
}
