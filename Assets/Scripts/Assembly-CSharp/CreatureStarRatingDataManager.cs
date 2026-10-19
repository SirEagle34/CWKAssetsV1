using System.IO;

public class CreatureStarRatingDataManager : DataManager<CreatureStarRatingData>
{
	private static CreatureStarRatingDataManager _instance;

	public static CreatureStarRatingDataManager Instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = new CreatureStarRatingDataManager(Path.Combine(SQSettings.CDN_URL, "Blueprints", "db_CreatureStarRatings.json"));
			}
			return _instance;
		}
	}

	public CreatureStarRatingDataManager(string path)
	{
		base.FilePath = path;
	}

	public CreatureStarRatingData GetDataByRating(int rating)
	{
		return DatabaseArray[rating - 1];
	}

	public int MaxStarRating()
	{
		return DatabaseArray.Count;
	}
}
