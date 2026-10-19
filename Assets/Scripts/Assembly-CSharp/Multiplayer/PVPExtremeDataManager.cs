using System;
using System.Collections.Generic;
using System.IO;

public class PVPExtremeDataManager : DataManager<PVPExtremeData>
{
	private static PVPExtremeDataManager _instance;

	public static PVPExtremeDataManager Instance
	{
		get
		{
			if (_instance == null)
			{
				string path = Path.Combine(SQSettings.CDN_URL, "Blueprints", "db_PVPExtremeList.json");
				_instance = new PVPExtremeDataManager(path);
			}
			return _instance;
		}
	}

	public PVPExtremeDataManager(string path)
	{
		base.FilePath = path;
	}

	protected override void ParseRows(List<object> jlist)
	{
		foreach (object obj in jlist)
		{
			Dictionary<string, object> dict = obj as Dictionary<string, object>;
			if (dict == null)
				continue;

			PVPExtremeData data = new PVPExtremeData();
			data.Populate(dict);

			if (data.ID != string.Empty && !Database.ContainsKey(data.ID))
			{
				Database.Add(data.ID, data);
			}

			DatabaseArray.Add(data);
		}
	}
}
