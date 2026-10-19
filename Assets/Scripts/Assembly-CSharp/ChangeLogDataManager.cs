using System;
using System.Collections.Generic;
using System.IO;

public class ChangeLogDataManager : DataManager<ChangeLogData>
{
    private static ChangeLogDataManager _instance;

    public static ChangeLogDataManager Instance
    {
        get
        {
            if (_instance == null)
            {
                string path = Path.Combine(SQSettings.CDN_URL, "Blueprints/db_ChangeLog.json");
                _instance = new ChangeLogDataManager(path);
            }
            return _instance;
        }
    }

    public ChangeLogDataManager(string path)
    {
        FilePath = path;
    }

    protected override void ParseRows(List<object> jlist)
    {
        foreach (object item in jlist)
        {
            var dict = (Dictionary<string, object>)item;

            var data = new ChangeLogData();
            data.Populate(dict);

            Database.Add(data.ID, data);
            DatabaseArray.Add(data);
        }
    }

	public IReadOnlyList<ChangeLogData> GetAll()
	{
		return DatabaseArray;
	}

	public ChangeLogData GetLatest()
	{
		if (DatabaseArray == null || DatabaseArray.Count == 0)
			return null;

		return DatabaseArray[^1];
	}
}