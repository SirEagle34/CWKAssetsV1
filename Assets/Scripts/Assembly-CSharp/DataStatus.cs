using System;
using System.Collections.Generic;
using MoonSharp.Interpreter;
using UnityEngine;

public class DataStatus : StatusState
{
	private class Settings
	{
		public bool Freeze;

		public bool DisablePassive;

		public bool AutoMiss;

		public bool AttackRandom;

		public bool BlockDraw;

		public bool GuaranteeCrit;

		public readonly Dictionary<string, float> Stats = new Dictionary<string, float>();

		public float DamageTakenPct;

		public float DamageGivenPct;

		public float DodgePct;

		public int CritAdd;

		public int BonusEnergy;

		public int ExtraCards;

		public int BonusDragAttacks;

		public int AttackDiscount;

		public float DotPct;

		public float HotPct;

		public int EnergyPerTurn;

		public string Script;
	}

	private static readonly string[] StatNames = new string[5] { "STR", "INT", "DEF", "RES", "DEX" };

	private Table mState;

	private CreatureState mStateTarget;

	private Settings S => Get(base.Data);

	public bool Freezes
	{
		get
		{
			if (base.Data != null)
			{
				return S.Freeze;
			}
			return false;
		}
	}

	public bool DisablesPassive
	{
		get
		{
			if (base.Data != null)
			{
				return S.DisablePassive;
			}
			return false;
		}
	}

	private LuaEffects.LuaScript Lua
	{
		get
		{
			if (!string.IsNullOrEmpty(S.Script))
			{
				return LuaEffects.Get(S.Script);
			}
			return null;
		}
	}

	private static Settings Get(StatusData d)
	{
		if (d.DataParams is Settings result)
		{
			return result;
		}
		Dictionary<string, object> dictionary = d.Raw ?? new Dictionary<string, object>();
		Settings settings = new Settings
		{
			Freeze = TFUtils.LoadBool(dictionary, "Freeze", defaultValue: false),
			DisablePassive = TFUtils.LoadBool(dictionary, "DisablePassive", defaultValue: false),
			AutoMiss = TFUtils.LoadBool(dictionary, "AutoMiss", defaultValue: false),
			AttackRandom = TFUtils.LoadBool(dictionary, "AttackRandom", defaultValue: false),
			BlockDraw = TFUtils.LoadBool(dictionary, "BlockDraw", defaultValue: false),
			GuaranteeCrit = TFUtils.LoadBool(dictionary, "GuaranteeCrit", defaultValue: false),
			DamageTakenPct = TFUtils.LoadFloat(dictionary, "DamageTakenPct", 0f),
			DamageGivenPct = TFUtils.LoadFloat(dictionary, "DamageGivenPct", 0f),
			DodgePct = TFUtils.LoadFloat(dictionary, "DodgePct", 0f),
			CritAdd = TFUtils.LoadInt(dictionary, "CritAdd", 0),
			BonusEnergy = TFUtils.LoadInt(dictionary, "BonusEnergy", 0),
			ExtraCards = TFUtils.LoadInt(dictionary, "ExtraCards", 0),
			BonusDragAttacks = TFUtils.LoadInt(dictionary, "BonusDragAttacks", 0),
			AttackDiscount = TFUtils.LoadInt(dictionary, "AttackDiscount", 0),
			DotPct = TFUtils.LoadFloat(dictionary, "DotPct", 0f),
			HotPct = TFUtils.LoadFloat(dictionary, "HotPct", 0f),
			EnergyPerTurn = TFUtils.LoadInt(dictionary, "EnergyPerTurn", 0),
			Script = TFUtils.LoadString(dictionary, "Script", string.Empty)
		};
		string[] statNames = StatNames;
		foreach (string key in statNames)
		{
			float num = TFUtils.LoadFloat(dictionary, key, 0f);
			if (num != 0f)
			{
				settings.Stats[key] = num;
			}
		}
		d.DataParams = settings;
		return settings;
	}

	public static GameEventFXData EventFX(GameMessage m)
	{
		if (m != null && m.Action == GameEvent.ENABLE_DATASTATUS && m.Status != null)
		{
			return m.Status.FXData;
		}
		if (m != null)
		{
			return GameEventFXDataManager.Instance.GetData(m.Action.ToString());
		}
		return null;
	}

	private DynValue CallLua(string hook, CreatureState other, params DynValue[] extra)
	{
		LuaEffects.LuaScript lua = Lua;
		if (lua == null || !lua.Has(hook) || base.Target == null)
		{
			return null;
		}
		if (mStateTarget != base.Target || mState == null)
		{
			mState = new Table(lua.Script);
			mStateTarget = base.Target;
		}
		Table table = LuaEffects.Context(lua.Script, base.Target, other, GetStackedAmount(), base.Data.ID, mState);
		DynValue[] array = new DynValue[extra.Length + 1];
		array[0] = DynValue.NewTable(table);
		Array.Copy(extra, 0, array, 1, extra.Length);
		return lua.Call(hook, array);
	}

	protected override void OnEnable()
	{
		Settings s = S;
		if (s.Freeze)
		{
			base.Target.IsFrozen = true;
		}
		if (s.DisablePassive)
		{
			base.Target.IsParalyzed = true;
		}
		foreach (KeyValuePair<string, float> stat in s.Stats)
		{
			LuaEffects.AddStat(base.Target, stat.Key, stat.Value);
		}
		CallLua("onEnable", null);
	}

	protected override void OnDisable()
	{
		Settings s = S;
		foreach (KeyValuePair<string, float> stat in s.Stats)
		{
			LuaEffects.AddStat(base.Target, stat.Key, 0f - stat.Value);
		}
		CallLua("onDisable", null);
		if (s.Freeze || s.DisablePassive)
		{
			base.Target.RefreshControlFlags(this);
		}
	}

	public override void ProcessMessage(GameMessage Message)
	{
		if (base.Target != null && Message.Action == GameEvent.START_TURN && Message.WhichPlayer == base.Target.Owner)
		{
			Settings s = S;
			if (s.DotPct > 0f)
			{
				base.Target.DealDamage(Mathf.CeilToInt((float)base.Target.MaxHP * s.DotPct / 100f), AttackBase.None, IsDirect: false, null, this);
			}
			if (base.Target != null && s.HotPct > 0f)
			{
				base.Target.Heal(s.HotPct / 100f);
			}
			if (base.Target != null && s.EnergyPerTurn > 0)
			{
				base.Target.Owner.AddActionPoints(s.EnergyPerTurn);
			}
			else if (base.Target != null && s.EnergyPerTurn < 0)
			{
				base.Target.Owner.LoseActionPoints(-s.EnergyPerTurn);
			}
			if (base.Target != null)
			{
				CallLua("onStartTurn", null);
			}
			if (base.Target != null && base.Data.StackRule == StackType.Count && base.Count > 0)
			{
				TickCount();
			}
		}
	}

	public override float InteruptDamageTaken(float damage)
	{
		damage *= 1f + S.DamageTakenPct / 100f;
		DynValue dynValue = CallLua("damageTaken", null, DynValue.NewNumber(damage));
		if (dynValue == null || dynValue.Type != DataType.Number)
		{
			return damage;
		}
		return (float)dynValue.Number;
	}

	public override float InteruptDamageGiven(float damage)
	{
		damage *= 1f + S.DamageGivenPct / 100f;
		DynValue dynValue = CallLua("damageGiven", null, DynValue.NewNumber(damage));
		if (dynValue == null || dynValue.Type != DataType.Number)
		{
			return damage;
		}
		return (float)dynValue.Number;
	}

	public override int AdjustCritChance(int chance)
	{
		return chance + S.CritAdd;
	}

	public override int AttackDiscount()
	{
		return S.AttackDiscount;
	}

	public override bool BlockDraw()
	{
		return S.BlockDraw;
	}

	public override bool PreventAttack()
	{
		if (S.Freeze)
		{
			return true;
		}
		return CallLua("preventAttack", null)?.CastToBool() ?? false;
	}

	public override int BonusDragAttacks()
	{
		return S.BonusDragAttacks;
	}

	public override bool GuaranteeCrit()
	{
		return S.GuaranteeCrit;
	}

	public override bool AutoMiss()
	{
		return S.AutoMiss;
	}

	public override bool Dodge(AttackCause attackCause)
	{
		if (S.DodgePct > 0f && KFFRandom.Percent((int)S.DodgePct))
		{
			return true;
		}
		return CallLua("dodge", null, DynValue.NewString(attackCause.ToString()))?.CastToBool() ?? false;
	}

	public override bool AttackRandom()
	{
		return S.AttackRandom;
	}

	public override int GetExtraCardCount()
	{
		return S.ExtraCards;
	}

	public override int BonusEnergy()
	{
		return S.BonusEnergy;
	}
}
