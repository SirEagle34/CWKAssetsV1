using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

public static class BattleRules
{
	private static string Mode()
	{
		PlayerInfoScript instance = Singleton<PlayerInfoScript>.Instance;
		if (instance != null && instance.StateData != null && instance.StateData.MultiplayerMode)
		{
			if (instance.PvPData == null || !instance.PvPData.ExtremeMode)
			{
				return "PvP";
			}
			return "Extreme";
		}
		return "PvE";
	}

	private static List<string> Ids(string csv)
	{
		List<string> list = new List<string>();
		string[] array = (csv ?? string.Empty).Split(',');
		foreach (string text in array)
		{
			if (text.Trim().Length > 0)
			{
				list.Add(text.Trim());
			}
		}
		return list;
	}

	public static void AddTo(CreatureState creature, List<AbilityState> abilities)
	{
		List<string> list = Ids(MiscParams.GetString("BattleRules_All", string.Empty));
		list.AddRange(Ids(MiscParams.GetString("BattleRules_" + Mode(), string.Empty)));
		foreach (string item in list)
		{
			CreaturePassiveData data = CreaturePassiveDataManager.Instance.GetData(item);
			if (data == null || data.ScriptName != "DataAbility")
			{
				Debug.LogWarning("[BattleRules] skipped '" + item + "' (not a DataAbility passive)");
				continue;
			}
			AbilityState abilityState = AbilityState.Create(new BattleRuleSource(data));
			abilityState.Owner = creature;
			abilities.Add(abilityState);
		}
	}

	public static bool Held(CreatureState c)
	{
		if (c != null && (bool)c.IsFrozen)
		{
			return MiscParams.GetString("FrozenNoAutoAttack", string.Empty) == "1";
		}
		return false;
	}

	public static bool IsRule(AbilityState ability)
	{
		if (ability != null)
		{
			return ability.Source is BattleRuleSource;
		}
		return false;
	}

	public static string PvpFingerprint()
	{
		string text = MiscParams.GetString("BattleRules_All", string.Empty) + "|" + MiscParams.GetString("BattleRules_PvP", string.Empty) + "|" + MiscParams.GetString("BattleRules_Extreme", string.Empty);
		if (text == "||")
		{
			return string.Empty;
		}
		using MD5 mD = MD5.Create();
		byte[] array = mD.ComputeHash(Encoding.UTF8.GetBytes(text));
		return "_r" + BitConverter.ToString(array, 0, 3).Replace("-", string.Empty).ToLowerInvariant();
	}
}
