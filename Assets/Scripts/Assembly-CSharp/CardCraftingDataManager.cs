using System.IO;

public class CardCraftingDataManager : DataManager<CardCraftingData>
{
	private static CardCraftingDataManager _instance;

	public static CardCraftingDataManager Instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = new CardCraftingDataManager(Path.Combine(SQSettings.CDN_URL, "Blueprints", "db_CardCrafting.json"));
			}
			return _instance;
		}
	}

	public CardCraftingDataManager(string path)
	{
		base.FilePath = path;
		AddDependency(CardDataManager.Instance);
		AddDependency(CreatureDataManager.Instance);
		AddDependency(EvoMaterialDataManager.Instance);
	}
}
