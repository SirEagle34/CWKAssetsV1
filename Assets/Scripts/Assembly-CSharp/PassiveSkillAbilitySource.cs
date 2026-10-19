using CodeStage.AntiCheat.ObscuredTypes;

public class PassiveSkillAbilitySource : IAbilitySource
{
	private string _ScriptName;

	private ObscuredInt[] _CurrentValues;

	private ObscuredInt[] _EnhanceValues;

	public string ScriptName => _ScriptName;

	public AbilitySourceType AbilitySourceType => AbilitySourceType.Creature;

	public string ValID { get; private set; }

	public CreaturePassiveData Data { get; private set; }

	public int Val1 => GetValue(0);

	public int Val2 => GetValue(1);

	public int Val3 => GetValue(2);

	public int Val4 => GetValue(3);

	public int EnhanceVal1 => GetEnhanceValue(0);

	public int EnhanceVal2 => GetEnhanceValue(1);

	public int EnhanceVal3 => GetEnhanceValue(2);

	public int EnhanceVal4 => GetEnhanceValue(3);

	public PassiveSkillAbilitySource(CreaturePassiveData effectData, int level, bool isEnhanced)
	{
		_ScriptName = effectData.ScriptName;
		Data = effectData;
		ValID = effectData.ValID;
		_CurrentValues = effectData.GetValuesAtLevel(level);
		_EnhanceValues = effectData.GetEnhanceValuesAtLevel(level);
	}

	private int GetValue(int index)
	{
		if (_CurrentValues == null || index >= _CurrentValues.Length)
		{
			return 0;
		}
		return _CurrentValues[index];
	}

	private int GetEnhanceValue(int index)
	{
		if (_EnhanceValues == null || index >= _EnhanceValues.Length)
		{
			return 0;
		}
		return _EnhanceValues[index];
	}
}
