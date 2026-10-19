using System.Collections.Generic;
using System.IO;

public class CustomAIDataManager : DataManager<CustomAIData>
{
	private static CustomAIDataManager _instance;

	public static CustomAIDataManager Instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = new CustomAIDataManager(Path.Combine(SQSettings.CDN_URL, "Blueprints", "db_CustomAI.json"));
			}
			return _instance;
		}
	}

	public CustomAIDataManager(string path)
	{
		base.FilePath = path;
	}

	protected override void ParseRows(List<object> jlist)
	{
		CustomAIData customAIData = null;
		foreach (object item in jlist)
		{
			SafeRow(item, delegate
			{
				Dictionary<string, object> dictionary = (Dictionary<string, object>)item;
				string text = TFUtils.LoadString(dictionary, "ID", string.Empty);
				if (customAIData == null || text != "^")
				{
					customAIData = new CustomAIData();
					customAIData.Populate(dictionary);
					Database.Add(customAIData.ID, customAIData);
					DatabaseArray.Add(customAIData);
				}
				else
				{
					customAIData.AddRow(dictionary);
				}
			});
		}
	}
}
