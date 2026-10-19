using System.Collections.Generic;
using UnityEngine;

public static class ShapeShiftAdvisor
{
	public class Option
	{
		public string Id;

		public string Role;

		public float Score;

		public string Why;
	}

	private static readonly string[][] Roles = new string[6][]
	{
		new string[5] { "cleanse", "RemoveDebuff", "RemoveNeg", "RemoveNag", "RemoveBlindBerserk" },
		new string[3] { "purge", "RemoveBuff", "EraseBravery" },
		new string[2] { "heal", "Heal" },
		new string[3] { "finish", "InstantKill", "LowestHP" },
		new string[3] { "guard", "WeakenStr", "ManyEnemyDragAttacks" },
		new string[2] { "breaker", "DealDamageHighest" }
	};

	public static List<Option> Choose(CreatureState shifter, List<string> pool, int count)
	{
		List<string> list = new List<string>();
		List<CreatureItem> list2 = new List<CreatureItem>();
		foreach (string item in pool)
		{
			CreatureData data = CreatureDataManager.Instance.GetData(item);
			if (data != null && !list.Contains(item) && (shifter.Data == null || shifter.Data.Form != data))
			{
				CreatureItem creatureItem = new CreatureItem(data);
				if (shifter.Data != null)
				{
					creatureItem.Xp = shifter.Data.Xp;
					creatureItem.StarRating = shifter.Data.StarRating;
				}
				list.Add(item);
				list2.Add(creatureItem);
			}
		}
		return Rank(shifter.Owner, shifter, list, list2, shifter.HPPct, count);
	}

	public static List<Option> Rank(PlayerState me, CreatureState skip, List<string> keys, List<CreatureItem> items, float hpPct, int count)
	{
		List<CreatureState> list = Alive(me?.Opponent, null);
		List<CreatureState> list2 = Alive(me, skip);
		int num = 0;
		float num2 = 0f;
		foreach (CreatureState item2 in list2)
		{
			num += item2.GetStatusTypeCount(StatusType.Debuff);
			num2 += 1f - item2.HPPct;
		}
		int num3 = 0;
		int num4 = 0;
		CreatureState creatureState = null;
		CreatureState creatureState2 = null;
		CreatureState creatureState3 = null;
		foreach (CreatureState item3 in list)
		{
			num3 += item3.GetStatusTypeCount(StatusType.Buff);
			num4 += ((item3.HPPct < 0.65f) ? 1 : 0);
			creatureState = ((creatureState == null || item3.STR > creatureState.STR) ? item3 : creatureState);
			creatureState2 = ((creatureState2 == null || item3.HP > creatureState2.HP) ? item3 : creatureState2);
			creatureState3 = ((creatureState3 == null || item3.HPPct < creatureState3.HPPct) ? item3 : creatureState3);
		}
		List<Option> list3 = new List<Option>();
		List<float> list4 = new List<float>();
		List<float> list5 = new List<float>();
		List<float> list6 = new List<float>();
		List<CreatureState> list7 = new List<CreatureState>();
		List<int> list8 = new List<int>();
		for (int i = 0; i < keys.Count; i++)
		{
			string id = keys[i];
			CreatureItem creatureItem = items[i];
			CreatureData form = creatureItem.Form;
			float num5 = (float)creatureItem.HP * hpPct;
			float num6 = (float)creatureItem.STR * (1f + (float)creatureItem.DEX / 200f);
			int num7 = Mathf.Max(1, form.AttackCost);
			float num8 = 0f;
			CreatureState creatureState4 = null;
			int num9 = 0;
			foreach (CreatureState item4 in list)
			{
				int num10 = Mathf.CeilToInt((float)item4.HP / Mathf.Max(1f, num6 * (1f - Mathf.Clamp(item4.DEF, 0f, 90f) / 100f)));
				num8 += (item4.STR + 1f) / (float)(num10 * num7);
				if (creatureState4 == null || num10 < num9 || (num10 == num9 && item4.STR > creatureState4.STR))
				{
					creatureState4 = item4;
					num9 = num10;
				}
			}
			string text = RoleOf(form);
			float item = 0f;
			switch (text)
			{
			case "cleanse":
				item = Mathf.Clamp01((float)num / 3f);
				break;
			case "purge":
				item = Mathf.Clamp01((float)num3 / 3f);
				break;
			case "heal":
				item = Mathf.Clamp01(num2 / 1.2f);
				break;
			case "finish":
				item = ((list.Count == 0) ? 0f : ((float)num4 / (float)list.Count));
				break;
			case "guard":
				item = ((creatureState == null) ? 0f : Mathf.Clamp01((creatureState.STR / Mathf.Max(1f, num5) - 0.2f) / 0.3f));
				break;
			case "breaker":
				item = ((creatureState2 == null) ? 0f : Mathf.Clamp01(((float)creatureState2.HP / Mathf.Max(1f, num6) - 2f) / 3f));
				break;
			}
			list3.Add(new Option
			{
				Id = id,
				Role = text
			});
			list4.Add(num8);
			list5.Add(num5 / Mathf.Max(1f, creatureState?.STR ?? 1f));
			list6.Add(item);
			list7.Add(creatureState4);
			list8.Add(num9);
		}
		float num11 = Mathf.Max(0.0001f, Max(list4));
		float num12 = Mathf.Max(0.0001f, Max(list5));
		for (int j = 0; j < list3.Count; j++)
		{
			float num13 = list4[j] / num11;
			float num14 = list5[j] / num12;
			Option option = list3[j];
			option.Score = num13 + 0.7f * num14 + 0.9f * list6[j] + ((list.Count == 0) ? num14 : 0f);
			if (list6[j] >= 0.5f)
			{
				option.Why = RoleWhy(option.Role, (option.Role == "finish") ? creatureState3 : ((option.Role == "breaker") ? creatureState2 : creatureState));
			}
			else if (list7[j] != null && (num13 >= num14 || list8[j] <= 2))
			{
				option.Why = Text((list8[j] == 1) ? "KILL1" : "KILL", "Kills {0} in {1} hits", list7[j].Data.Form.Name, list8[j]);
			}
			else if (creatureState != null)
			{
				option.Why = Text("TANK", "Survives {1} hits from {0}", creatureState.Data.Form.Name, Mathf.FloorToInt(list5[j]));
			}
			else
			{
				option.Why = Text("STRONG", "Strongest form");
			}
			if (list6[j] < 0.5f)
			{
				option.Role = "stats";
			}
		}
		List<Option> list9 = new List<Option>();
		Dictionary<string, int> dictionary = new Dictionary<string, int>();
		while (list9.Count < count && list3.Count > 0)
		{
			Option option2 = null;
			float num15 = float.MinValue;
			foreach (Option item5 in list3)
			{
				dictionary.TryGetValue(item5.Role, out var value);
				float num16 = item5.Score - 0.35f * (float)value;
				if (num16 > num15)
				{
					option2 = item5;
					num15 = num16;
				}
			}
			list3.Remove(option2);
			list9.Add(option2);
			dictionary.TryGetValue(option2.Role, out var value2);
			dictionary[option2.Role] = value2 + 1;
		}
		string text2 = "[Advisor] offer:";
		foreach (Option item6 in list9)
		{
			text2 = text2 + " " + item6.Id + "(" + item6.Role + " " + item6.Score.ToString("0.00") + " \"" + item6.Why + "\")";
		}
		Debug.Log(text2);
		return list9;
	}

	private static List<CreatureState> Alive(PlayerState p, CreatureState skip)
	{
		List<CreatureState> list = new List<CreatureState>();
		if (p != null)
		{
			foreach (LaneState lane in p.Lanes)
			{
				if (lane.Creature != null && lane.Creature.HP > 0 && lane.Creature != skip && lane.Creature.Data != null)
				{
					list.Add(lane.Creature);
				}
			}
		}
		return list;
	}

	private static string RoleOf(CreatureData form)
	{
		string text = ((form.PassiveData == null) ? string.Empty : (form.PassiveData.ScriptName ?? string.Empty));
		string[][] roles = Roles;
		foreach (string[] array in roles)
		{
			for (int j = 1; j < array.Length; j++)
			{
				if (text.Contains(array[j]))
				{
					return array[0];
				}
			}
		}
		return "stats";
	}

	private static string RoleWhy(string role, CreatureState target)
	{
		string text = ((target == null) ? string.Empty : target.Data.Form.Name);
		return role switch
		{
			"cleanse" => Text("CLEANSE", "Cleanses your team"), 
			"purge" => Text("PURGE", "Strips enemy buffs"), 
			"heal" => Text("HEAL", "Heals your team"), 
			"finish" => Text("FINISH", "Finishes off {0}", text), 
			"guard" => Text("GUARD", "Punishes {0}'s attacks", text), 
			_ => Text("BREAK", "Hurts {0} every turn", text), 
		};
	}

	private static string Text(string key, string fallback, params object[] args)
	{
		string text = KFFLocalization.Get("!!SPOOKY_0050_WHY_" + key);
		return string.Format((string.IsNullOrEmpty(text) || text.Contains("SPOOKY_0050_WHY")) ? fallback : text, args);
	}

	private static float Max(List<float> list)
	{
		float num = 0f;
		foreach (float item in list)
		{
			num = Mathf.Max(num, item);
		}
		return num;
	}
}
