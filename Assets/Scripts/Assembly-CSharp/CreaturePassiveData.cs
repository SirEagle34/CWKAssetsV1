using System.Collections.Generic;
using CodeStage.AntiCheat.ObscuredTypes;

public class CreaturePassiveData : ILoadableData
{
	public string ID { get; private set; }

	public string LevelGroup { get; private set; }

	public ObscuredInt[] MinValues { get; private set; }

	public ObscuredInt[] MaxValues { get; private set; }

	public string Description { get; private set; }

	public string ScriptName { get; private set; }

	public int MaxLevel { get; private set; }

	public string DisplayName { get; private set; }

	public int FeedsPerLevel { get; private set; }

	public string ValID { get; private set; }

	public IAbilitySource Source { get; set; }

	public CreatureState Owner { get; set; }

	public Dictionary<string, object> Raw { get; private set; }

	public void Populate(Dictionary<string, object> dict)
	{
		Raw = dict;
		ID = TFUtils.LoadLocalizedString(dict, "ID", string.Empty);
		LevelGroup = TFUtils.LoadLocalizedString(dict, "LevelGroup", string.Empty);
		Description = TFUtils.LoadLocalizedString(dict, "Description", string.Empty);
		ScriptName = TFUtils.LoadString(dict, "ScriptName", string.Empty);
		MaxLevel = TFUtils.LoadInt(dict, "MaxLevel", 1);
		DisplayName = TFUtils.LoadLocalizedString(dict, "DisplayName", string.Empty);
		FeedsPerLevel = TFUtils.LoadInt(dict, "FeedsPerLevel", 3);
		ValID = TFUtils.LoadString(dict, "ValID", null);
		List<ObscuredInt> list = new List<ObscuredInt>();
		List<ObscuredInt> list2 = new List<ObscuredInt>();
		int num = 1;
		while (true)
		{
			int num2 = TFUtils.LoadInt(dict, "MinVal" + num, int.MinValue);
			int num3 = TFUtils.LoadInt(dict, "MaxVal" + num, int.MinValue);
			if (num2 == int.MinValue || num3 == int.MinValue)
			{
				break;
			}
			list.Add(num2);
			list2.Add(num3);
			num++;
		}
		MinValues = list.ToArray();
		MaxValues = list2.ToArray();
	}

	public ObscuredInt[] GetValuesAtLevel(int level)
	{
		if (Owner != null && Owner.SourceItem != null && (bool)Owner.SourceItem.EnhancePassive)
		{
			return GetEnhanceValuesAtLevel(level);
		}
		return GetNormalValuesAtLevel(level);
	}

	private ObscuredInt[] GetNormalValuesAtLevel(int level)
	{
		ObscuredInt[] array = new ObscuredInt[MinValues.Length];
		for (int i = 0; i < array.Length; i++)
		{
			float num = ((MaxLevel <= 0) ? 1f : (1E-05f + (float)level / (float)MaxLevel));
			array[i] = (int)MinValues[i] + (int)((float)((int)MaxValues[i] - (int)MinValues[i]) * num);
		}
		return array;
	}

	public ObscuredInt[] GetEnhanceValuesAtLevel(int level)
	{
		ObscuredInt[] normalValuesAtLevel = GetNormalValuesAtLevel(level);
		ObscuredInt[] array = new ObscuredInt[normalValuesAtLevel.Length];
		for (int i = 0; i < normalValuesAtLevel.Length; i++)
		{
			int num = normalValuesAtLevel[i];
			if (num >= 1 && num <= 9)
			{
				num++;
			}
			else if (num >= 10 && num <= 29)
			{
				num += 15;
			}
			else if (num >= 30 && num <= 49)
			{
				num += 20;
			}
			else if (num >= 50 && num <= 74)
			{
				num += 30;
			}
			else if (num >= 75)
			{
				num += 50;
			}
			array[i] = num;
		}
		return array;
	}

	public string BuildDescriptionString(int level, bool isEnhanced = false)
	{
		string text = Description;
		ObscuredInt[] valuesAtLevel = GetValuesAtLevel(level);
		for (int i = 0; i < valuesAtLevel.Length; i++)
		{
			text = text.Replace("<val" + (i + 1) + ">", valuesAtLevel[i].ToString());
		}
		if (ValID != null)
		{
			CreatureData data = CreatureDataManager.Instance.GetData(ValID);
			if (data != null)
			{
				text = text.Replace("<valID>", data.Name);
			}
		}
		return text;
	}
}
