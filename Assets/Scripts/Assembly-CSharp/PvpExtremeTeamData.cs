using System.Collections.Generic;

public class PvpExtremeTeamData : ILoadableData
{
	public string ID { get; private set; }

	public string HeroID { get; private set; }

	public List<string> CreatureIDs { get; private set; }

	public int StarRating { get; private set; }

	public int Level { get; private set; }

	public int PassiveLevel { get; private set; }

	public int RotateDays { get; private set; }

	public void Populate(Dictionary<string, object> dict)
	{
		ID = TFUtils.LoadString(dict, "ID", string.Empty);
		HeroID = TFUtils.LoadString(dict, "HeroID", string.Empty);
		CreatureIDs = new List<string>();
		for (int i = 1; i <= 10; i++)
		{
			string text = TFUtils.LoadString(dict, "CreatureID" + i.ToString("00"), string.Empty);
			if (!string.IsNullOrEmpty(text))
			{
				CreatureIDs.Add(text);
			}
		}
		StarRating = TFUtils.LoadInt(dict, "StarRating", 5);
		Level = TFUtils.LoadInt(dict, "Level", 0);
		PassiveLevel = TFUtils.LoadInt(dict, "PassiveLevel", 1);
		RotateDays = TFUtils.LoadInt(dict, "RotateDays", 7);
	}
}
