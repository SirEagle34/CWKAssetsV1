using System;
using System.Collections.Generic;

namespace Multiplayer
{
	public class RecordData
	{
		public string name;

		public string icon;

		public int outcome;

		public bool won;

		public DateTime datetime;

		public RecordData(Dictionary<string, object> dict)
		{
			name = dict["name"].ToString();
			icon = dict["icon"].ToString();
			outcome = TFUtils.LoadInt(dict, "outcome");
			won = TFUtils.LoadBool(dict, "won", defaultValue: false);
			datetime = DateTime.Parse(dict["date"].ToString());
		}

		public static List<RecordData> ProcessList(List<object> data)
		{
			List<RecordData> list = new List<RecordData>(data.Count);
			foreach (Dictionary<string, object> datum in data)
			{
				list.Add(new RecordData(datum));
			}
			return list;
		}
	}
}
