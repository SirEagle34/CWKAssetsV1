using System;
using System.Collections.Generic;
using MoonSharp.Interpreter;
using UnityEngine;

public static class LuaEffects
{
	public class LuaScript
	{
		public string Name;

		public Script Script;

		public Table Hooks;

		public bool Dead;

		public bool Has(string hook)
		{
			if (!Dead && Hooks != null)
			{
				return Hooks.Get(hook).Type == DataType.Function;
			}
			return false;
		}

		public DynValue Call(string hook, params DynValue[] args)
		{
			if (!Has(hook))
			{
				return null;
			}
			try
			{
				DynValue dynValue = Script.CreateCoroutine(Hooks.Get(hook));
				dynValue.Coroutine.AutoYieldCounter = 200000L;
				DynValue result = dynValue.Coroutine.Resume(args);
				if (dynValue.Coroutine.State != CoroutineState.Dead)
				{
					Kill(hook, "ran more than " + 200000 + " steps");
					return null;
				}
				return result;
			}
			catch (Exception ex)
			{
				Kill(hook, (ex is InterpreterException) ? ((InterpreterException)ex).DecoratedMessage : ex.Message);
				return null;
			}
		}

		private void Kill(string hook, string why)
		{
			Dead = true;
			Debug.LogError("[Lua] " + Name + "." + hook + " disabled: " + why);
		}
	}

	public const int StepBudget = 200000;

	private static readonly Dictionary<string, LuaScript> sScripts = new Dictionary<string, LuaScript>();

	private static readonly List<CreatureState> sCreatures = new List<CreatureState>();

	private static readonly Dictionary<CreatureState, int> sCreatureIds = new Dictionary<CreatureState, int>();

	public static LuaScript Get(string name)
	{
		if (string.IsNullOrEmpty(name))
		{
			return null;
		}
		if (sScripts.TryGetValue(name, out var value))
		{
			if (!value.Dead)
			{
				return value;
			}
			return null;
		}
		value = new LuaScript
		{
			Name = name
		};
		sScripts[name] = value;
		if (!LiveContent.Scripts.TryGetValue(name, out var value2) || string.IsNullOrEmpty(value2))
		{
			value.Dead = true;
			Debug.LogWarning("[Lua] no script named " + name);
			return null;
		}
		try
		{
			value.Script = new Script(CoreModules.Preset_SoftSandbox);
			InstallApi(value.Script);
			DynValue dynValue = value.Script.DoString(value2, null, name);
			value.Hooks = ((dynValue.Type == DataType.Table) ? dynValue.Table : null);
			if (value.Hooks == null)
			{
				value.Dead = true;
				Debug.LogError("[Lua] " + name + " must return a table of hooks");
			}
		}
		catch (Exception ex)
		{
			value.Dead = true;
			Debug.LogError("[Lua] " + name + " failed to load: " + ((ex is InterpreterException) ? ((InterpreterException)ex).DecoratedMessage : ex.Message));
		}
		if (!value.Dead)
		{
			return value;
		}
		return null;
	}

	public static DynValue Wrap(CreatureState c)
	{
		if (c == null)
		{
			return DynValue.Nil;
		}
		if (!sCreatureIds.TryGetValue(c, out var value))
		{
			if (sCreatures.Count > 2000)
			{
				sCreatures.Clear();
				sCreatureIds.Clear();
			}
			value = sCreatures.Count + 1;
			sCreatures.Add(c);
			sCreatureIds[c] = value;
		}
		return DynValue.NewNumber(value);
	}

	private static CreatureState Unwrap(DynValue v)
	{
		if (v == null || v.Type != DataType.Number)
		{
			return null;
		}
		int num = (int)v.Number - 1;
		if (num < 0 || num >= sCreatures.Count)
		{
			return null;
		}
		return sCreatures[num];
	}

	public static Table Context(Script script, CreatureState self, CreatureState other, float value, string status, Table state)
	{
		return new Table(script)
		{
			["self"] = Wrap(self),
			["other"] = Wrap(other),
			["value"] = value,
			["status"] = status ?? string.Empty,
			["state"] = state ?? new Table(script)
		};
	}

	private static float Stat(CreatureState c, string name)
	{
		return (name ?? string.Empty).ToUpperInvariant() switch
		{
			"STR" => c.STR, 
			"INT" => c.INT, 
			"DEF" => c.DEF, 
			"RES" => c.RES, 
			"DEX" => c.DEX, 
			"HP" => c.HP, 
			"MAXHP" => c.MaxHP, 
			_ => 0f, 
		};
	}

	public static bool AddStat(CreatureState c, string name, float amount)
	{
		switch ((name ?? string.Empty).ToUpperInvariant())
		{
		case "STR":
			c.STR += amount;
			return true;
		case "INT":
			c.INT += amount;
			return true;
		case "DEF":
			c.DEF += amount;
			return true;
		case "RES":
			c.RES += amount;
			return true;
		case "DEX":
			c.DEX += amount;
			return true;
		default:
			return false;
		}
	}

	public static Table CardTable(Script script, CardData card)
	{
		return new Table(script)
		{
			["id"] = card.ID,
			["floop"] = card.IsLeaderCard,
			["faction"] = card.Faction.ToString(),
			["cost"] = (int)card.Cost
		};
	}

	public static bool CardAllows(CardData card, string hook, PlayerState player, CreatureState target)
	{
		LuaScript luaScript = ((card == null) ? null : Get(card.PlayScript));
		if (luaScript == null || !luaScript.Has(hook))
		{
			return true;
		}
		Table table = new Table(luaScript.Script);
		table["card"] = CardTable(luaScript.Script, card);
		table["target"] = Wrap(target);
		table["mine"] = Lanes(luaScript.Script, player);
		table["theirs"] = Lanes(luaScript.Script, player?.Opponent);
		table["dead"] = player?.RaisableDead().Count ?? 0;
		DynValue dynValue = luaScript.Call(hook, DynValue.NewTable(table));
		if (dynValue != null && dynValue.Type == DataType.Boolean)
		{
			return dynValue.Boolean;
		}
		return true;
	}

	private static Table Lanes(Script script, PlayerState p)
	{
		Table table = new Table(script);
		if (p != null)
		{
			foreach (LaneState lane in p.Lanes)
			{
				if (lane.Creature != null)
				{
					table.Append(Wrap(lane.Creature));
				}
			}
		}
		return table;
	}

	private static DynValue RemoveRandom(CreatureState c, int n, StatusRemovalData what)
	{
		StatusData data = StatusDataManager.Instance.GetData("RemoveStatus");
		if (c != null && n > 0 && data != null)
		{
			c.ApplyStatus(data, n, null, null, what);
		}
		return DynValue.Nil;
	}

	private static void InstallApi(Script script)
	{
		Table table = new Table(script);
		table["stat"] = DynValue.NewCallback(delegate(ScriptExecutionContext ctx, CallbackArguments a)
		{
			CreatureState creatureState = Unwrap(a[0]);
			return DynValue.NewNumber((creatureState == null) ? 0f : Stat(creatureState, a[1].CastToString()));
		});
		table["id"] = DynValue.NewCallback(delegate(ScriptExecutionContext ctx, CallbackArguments a)
		{
			CreatureState creatureState = Unwrap(a[0]);
			return DynValue.NewString((creatureState == null || creatureState.Data == null) ? "" : creatureState.Data.Form.ID);
		});
		table["addStat"] = DynValue.NewCallback(delegate(ScriptExecutionContext ctx, CallbackArguments a)
		{
			CreatureState creatureState = Unwrap(a[0]);
			return DynValue.NewBoolean(creatureState != null && AddStat(creatureState, a[1].CastToString(), (float)a[2].CastToNumber().GetValueOrDefault()));
		});
		table["applyStatus"] = DynValue.NewCallback(delegate(ScriptExecutionContext ctx, CallbackArguments a)
		{
			CreatureState creatureState = Unwrap(a[0]);
			StatusData statusData = ((creatureState == null) ? null : StatusDataManager.Instance.GetData(a[1].CastToString() ?? string.Empty));
			if (statusData == null)
			{
				return DynValue.False;
			}
			creatureState.ApplyStatus(statusData, (float)(a[2].CastToNumber() ?? 1.0), null);
			return DynValue.True;
		});
		table["removeStatus"] = DynValue.NewCallback(delegate(ScriptExecutionContext ctx, CallbackArguments a)
		{
			Unwrap(a[0])?.CancelStatusEffect(a[1].CastToString() ?? string.Empty);
			return DynValue.Nil;
		});
		table["cleanse"] = DynValue.NewCallback((ScriptExecutionContext ctx, CallbackArguments a) => RemoveRandom(Unwrap(a[0]), (int)a[1].CastToNumber().GetValueOrDefault(), StatusRemovalData.RemoveRandomDebuffs()));
		table["purge"] = DynValue.NewCallback((ScriptExecutionContext ctx, CallbackArguments a) => RemoveRandom(Unwrap(a[0]), (int)a[1].CastToNumber().GetValueOrDefault(), StatusRemovalData.RemoveRandomBuffs()));
		table["statuses"] = DynValue.NewCallback(delegate(ScriptExecutionContext ctx, CallbackArguments a)
		{
			CreatureState creatureState = Unwrap(a[0]);
			Table table3 = new Table(script);
			if (creatureState != null)
			{
				foreach (StatusState statusEffect in creatureState.StatusEffects)
				{
					if (statusEffect.Target != null && statusEffect.Data != null)
					{
						table3.Append(DynValue.NewString(statusEffect.Data.ID));
					}
				}
			}
			return DynValue.NewTable(table3);
		});
		table["hasStatus"] = DynValue.NewCallback(delegate(ScriptExecutionContext ctx, CallbackArguments a)
		{
			CreatureState creatureState = Unwrap(a[0]);
			string n = a[1].CastToString() ?? string.Empty;
			return DynValue.NewBoolean(creatureState?.StatusEffects.Exists((StatusState s) => s.Target != null && s.Data != null && s.Data.ID == n) ?? false);
		});
		table["damage"] = DynValue.NewCallback(delegate(ScriptExecutionContext ctx, CallbackArguments a)
		{
			CreatureState creatureState = Unwrap(a[0]);
			int num = (int)a[1].CastToNumber().GetValueOrDefault();
			if (creatureState != null && num > 0)
			{
				creatureState.DealDamage(num, AttackBase.None, IsDirect: false, null, null);
			}
			return DynValue.Nil;
		});
		table["heal"] = DynValue.NewCallback(delegate(ScriptExecutionContext ctx, CallbackArguments a)
		{
			CreatureState creatureState = Unwrap(a[0]);
			int num = (int)a[1].CastToNumber().GetValueOrDefault();
			if (creatureState != null && num > 0)
			{
				creatureState.Heal(num);
			}
			return DynValue.Nil;
		});
		table["energy"] = DynValue.NewCallback(delegate(ScriptExecutionContext ctx, CallbackArguments a)
		{
			CreatureState creatureState = Unwrap(a[0]);
			int num = (int)a[1].CastToNumber().GetValueOrDefault();
			if (creatureState != null && num > 0)
			{
				creatureState.Owner.AddActionPoints(num);
			}
			else if (creatureState != null && num < 0)
			{
				creatureState.Owner.LoseActionPoints(-num);
			}
			return DynValue.Nil;
		});
		table["draw"] = DynValue.NewCallback(delegate(ScriptExecutionContext ctx, CallbackArguments a)
		{
			CreatureState creatureState = Unwrap(a[0]);
			int num = Math.Min(10, (int)a[1].CastToNumber().GetValueOrDefault());
			int num2 = 0;
			while (creatureState != null && num2 < num)
			{
				creatureState.DrawCard();
				num2++;
			}
			return DynValue.Nil;
		});
		table["allies"] = DynValue.NewCallback(delegate(ScriptExecutionContext ctx, CallbackArguments a)
		{
			CreatureState creatureState = Unwrap(a[0]);
			return DynValue.NewTable(Lanes(script, creatureState?.Owner));
		});
		table["enemies"] = DynValue.NewCallback(delegate(ScriptExecutionContext ctx, CallbackArguments a)
		{
			CreatureState creatureState = Unwrap(a[0]);
			return DynValue.NewTable(Lanes(script, creatureState?.Owner.Opponent));
		});
		DynValue value = (DynValue)(table["random"] = DynValue.NewCallback(delegate(ScriptExecutionContext ctx, CallbackArguments a)
		{
			if (a.Count == 0)
			{
				return DynValue.NewNumber(KFFRandom.Range(0f, 1f));
			}
			int num = ((a.Count < 2) ? 1 : ((int)(a[0].CastToNumber() ?? 1.0)));
			int num2 = (int)(a[(a.Count >= 2) ? 1 : 0].CastToNumber() ?? 1.0);
			return DynValue.NewNumber((num2 < num) ? num : KFFRandom.Range(num, num2));
		}));
		table["chance"] = DynValue.NewCallback((ScriptExecutionContext ctx, CallbackArguments a) => DynValue.NewBoolean(KFFRandom.Percent((int)a[0].CastToNumber().GetValueOrDefault())));
		table["log"] = DynValue.NewCallback(delegate(ScriptExecutionContext ctx, CallbackArguments a)
		{
			Debug.Log("[Lua] " + ((a.Count > 0) ? a[0].ToPrintString() : string.Empty));
			return DynValue.Nil;
		});
		script.Globals["cwk"] = table;
		Table table2 = script.Globals.Get("math").Table;
		if (table2 != null)
		{
			table2["random"] = value;
			table2["randomseed"] = DynValue.NewCallback((ScriptExecutionContext ctx, CallbackArguments a) => DynValue.Nil);
		}
		script.Options.DebugPrint = delegate(string s)
		{
			Debug.Log("[Lua] " + s);
		};
	}
}
