using System.IO;

public class CurrencyPackageDataManager : DataManager<CurrencyPackageData>
{
	private static CurrencyPackageDataManager _instance;

	public static CurrencyPackageDataManager Instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = new CurrencyPackageDataManager(Path.Combine(SQSettings.CDN_URL, "Blueprints", "db_CurrencyPackages.json"));
			}
			return _instance;
		}
	}

	public CurrencyPackageDataManager(string path)
	{
		base.FilePath = path;
	}
}
