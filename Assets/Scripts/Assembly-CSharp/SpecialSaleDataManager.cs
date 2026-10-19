using System.IO;
using System.Collections.Generic;

public class SpecialSaleDataManager : DataManager<SpecialSaleData>
{
	private static SpecialSaleDataManager _instance;

	public static SpecialSaleDataManager Instance
	{
		get
		{
			if (_instance == null)
			{
				string path = Path.Combine(SQSettings.CDN_URL, "Blueprints", "db_Offers.json");
				_instance = new SpecialSaleDataManager(path);
			}
			return _instance;
		}
	}

	public SpecialSaleDataManager(string path)
	{
		base.FilePath = path;
		AddDependency(CreatureDataManager.Instance);
		AddDependency(CardDataManager.Instance);
		AddDependency(EvoMaterialDataManager.Instance);
		AddDependency(GachaWeightDataManager.Instance);
	}

	protected override void PostLoad()
	{
		foreach (SpecialSaleData item in DatabaseArray)
		{
			item.PostLoad();
		}
	}

	public List<SpecialSaleData> GetAllSales()
	{
		return DatabaseArray;
	}
}
