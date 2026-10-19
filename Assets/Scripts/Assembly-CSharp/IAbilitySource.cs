public interface IAbilitySource
{
	AbilitySourceType AbilitySourceType { get; }

	string ScriptName { get; }

	int Val1 { get; }

	int Val2 { get; }

	int Val3 { get; }

	int Val4 { get; }

	int EnhanceVal1 { get; }

	int EnhanceVal2 { get; }

	int EnhanceVal3 { get; }

	int EnhanceVal4 { get; }

	string ValID { get; }
}
