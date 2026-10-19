using System.Collections.Generic;
using MoonSharp.Interpreter;

public class DataAbility : AbilityState
{
	public const int MaxRules = 4;

	private Dictionary<string, object> Row
	{
		get
		{
			if (base.Source is PassiveSkillAbilitySource { Data: not null } passiveSkillAbilitySource)
			{
				return passiveSkillAbilitySource.Data.Raw;
			}
			return null;
		}
	}

	public override bool ProcessMessage(GameMessage m)
	{
		Dictionary<string, object> row = Row;
		if (row == null || base.Owner == null || m == null)
		{
			return false;
		}
		bool flag = false;
		for (int i = 0; i < 4; i++)
		{
			flag |= RunRule(row, (i == 0) ? string.Empty : (i + 1).ToString(), 1 << i, m);
		}
		return flag;
	}

	private bool RunRule(Dictionary<string, object> r, string sfx, int bit, GameMessage m)
	{
		string text = TFUtils.LoadString(r, "Trigger" + sfx, string.Empty);
		if (text.Length == 0 || !Matches(text, m, out var other))
		{
			return false;
		}
		if (TFUtils.LoadBool(r, "Once" + sfx, defaultValue: false) && (StoredValue & bit) != 0)
		{
			return false;
		}
		int num = Value(TFUtils.LoadString(r, "Chance" + sfx, "100"));
		if (num < 100 && !KFFRandom.Percent(num))
		{
			return false;
		}
		StoredValue |= bit;
		List<CreatureState> targets = Targets(TFUtils.LoadString(r, "Target" + sfx, "Self"), other);
		string[] array = TFUtils.LoadString(r, "Actions" + sfx, string.Empty).Split(';');
		foreach (string text2 in array)
		{
			Do(text2.Trim(), targets);
		}
		LuaEffects.LuaScript luaScript = LuaEffects.Get(TFUtils.LoadString(r, "Script" + sfx, string.Empty));
		if (luaScript != null && luaScript.Has("trigger"))
		{
			Table table = LuaEffects.Context(luaScript.Script, base.Owner, other, base.Val1, base.ScriptName, null);
			table["val1"] = base.Val1;
			table["val2"] = base.Val2;
			table["val3"] = base.Val3;
			table["val4"] = base.Val4;
			table["trigger"] = text;
			if (m.Card != null)
			{
				table["card"] = LuaEffects.CardTable(luaScript.Script, m.Card);
			}
			luaScript.Call("trigger", DynValue.NewTable(table));
		}
		return true;
	}

	private bool Matches(string trigger, GameMessage m, out CreatureState other)
	{
		other = null;
		PlayerState owner = base.Owner.Owner;
		switch (trigger)
		{
		case "Deploy":
			if (m.Action == GameEvent.CREATURE_DEPLOYED)
			{
				return m.Creature == base.Owner;
			}
			return false;
		case "AllyDeployed":
			other = m.Creature;
			if (m.Action == GameEvent.CREATURE_DEPLOYED && m.Creature != base.Owner)
			{
				return m.WhichPlayer == owner;
			}
			return false;
		case "EnemyDeployed":
			other = m.Creature;
			if (m.Action == GameEvent.CREATURE_DEPLOYED && owner != null)
			{
				return m.WhichPlayer == owner.Opponent;
			}
			return false;
		case "StartTurn":
			if (m.Action == GameEvent.START_TURN)
			{
				return m.WhichPlayer == owner;
			}
			return false;
		case "EndTurn":
			if (m.Action == GameEvent.END_TURN)
			{
				return m.WhichPlayer == owner;
			}
			return false;
		case "Attack":
			other = m.SecondCreature;
			if (m.Action == GameEvent.CREATURE_ATTACKED)
			{
				return m.Creature == base.Owner;
			}
			return false;
		case "Kill":
			other = m.SecondCreature;
			if (m.Action == GameEvent.CREATURE_ATTACKED && m.Creature == base.Owner && m.SecondCreature != null)
			{
				return m.SecondCreature.HP <= 0;
			}
			return false;
		case "Attacked":
			other = m.Creature;
			if (m.Action == GameEvent.CREATURE_ATTACKED)
			{
				return m.SecondCreature == base.Owner;
			}
			return false;
		case "Damaged":
			if (m.Action == GameEvent.DAMAGE_CREATURE)
			{
				return m.Creature == base.Owner;
			}
			return false;
		case "Healed":
			if (m.Action == GameEvent.HEAL_CREATURE)
			{
				return m.Creature == base.Owner;
			}
			return false;
		case "Died":
			if (m.Action == GameEvent.CREATURE_DIED)
			{
				return m.Creature == base.Owner;
			}
			return false;
		case "AllyDied":
			other = m.Creature;
			if (m.Action == GameEvent.CREATURE_DIED && m.Creature != null && m.Creature != base.Owner)
			{
				return m.Creature.Owner == owner;
			}
			return false;
		case "EnemyDied":
			other = m.Creature;
			if (m.Action == GameEvent.CREATURE_DIED && m.Creature != null && owner != null)
			{
				return m.Creature.Owner == owner.Opponent;
			}
			return false;
		case "CardDraw":
			if (m.Action == GameEvent.DRAW_CARD)
			{
				return m.WhichPlayer == owner;
			}
			return false;
		case "CardPlayed":
			other = m.Creature;
			if (m.Action == GameEvent.CARD_PLAYED && m.Card != null)
			{
				return m.WhichPlayer == owner;
			}
			return false;
		case "FloopPlayed":
			other = m.Creature;
			if (m.Action == GameEvent.CARD_PLAYED && m.Card != null && m.Card.IsLeaderCard)
			{
				return m.WhichPlayer == owner;
			}
			return false;
		case "EnemyCardPlayed":
			other = m.Creature;
			if (m.Action == GameEvent.CARD_PLAYED && m.Card != null && owner != null)
			{
				return m.WhichPlayer == owner.Opponent;
			}
			return false;
		case "HPBelow":
			if (m.Action == GameEvent.DAMAGE_CREATURE && m.Creature == base.Owner)
			{
				return base.Owner.HPPct <= base.Val1Pct;
			}
			return false;
		default:
			return false;
		}
	}

	private List<CreatureState> Targets(string target, CreatureState other)
	{
		List<CreatureState> list = new List<CreatureState>();
		PlayerState owner = base.Owner.Owner;
		switch (target)
		{
		case "Other":
			if (other != null)
			{
				list.Add(other);
			}
			break;
		case "Allies":
		case "AlliesOthers":
		case "RandomAlly":
			AddLanes(list, owner, (target == "AlliesOthers") ? base.Owner : null);
			break;
		case "Enemies":
		case "RandomEnemy":
			AddLanes(list, owner?.Opponent, null);
			break;
		default:
			list.Add(base.Owner);
			break;
		}
		if ((target == "RandomAlly" || target == "RandomEnemy") && list.Count > 1)
		{
			CreatureState item = list[KFFRandom.RandomIndex(list.Count)];
			list.Clear();
			list.Add(item);
		}
		return list;
	}

	private static void AddLanes(List<CreatureState> list, PlayerState p, CreatureState except)
	{
		if (p == null)
		{
			return;
		}
		foreach (LaneState lane in p.Lanes)
		{
			if (lane.Creature != null && lane.Creature != except)
			{
				list.Add(lane.Creature);
			}
		}
	}

	private int Value(string text)
	{
		switch ((text ?? string.Empty).Trim())
		{
		case "{Val1}":
			return base.Val1;
		case "{Val2}":
			return base.Val2;
		case "{Val3}":
			return base.Val3;
		case "{Val4}":
			return base.Val4;
		default:
		{
			if (!int.TryParse(text, out var result))
			{
				return 0;
			}
			return result;
		}
		}
	}

	private void Do(string action, List<CreatureState> targets)
	{
		if (action.Length == 0)
		{
			return;
		}
		string[] array = action.Split(':');
		string text = array[0];
		int num = ((array.Length > 1) ? Value(array[^1]) : 0);
		if (text == "Energy")
		{
			if (num > 0)
			{
				base.Owner.Owner.AddActionPoints(num);
			}
			else if (num < 0)
			{
				base.Owner.Owner.LoseActionPoints(-num);
			}
			return;
		}
		foreach (CreatureState target in targets)
		{
			if (target == null)
			{
				continue;
			}
			switch (text)
			{
			case "Status":
			{
				StatusData statusData = ((array.Length > 2) ? StatusDataManager.Instance.GetData(array[1]) : null);
				if (statusData != null)
				{
					target.ApplyStatus(statusData, num, null, this);
				}
				break;
			}
			case "Cancel":
				if (array.Length > 1)
				{
					target.CancelStatusEffect(array[1]);
				}
				break;
			case "Cleanse":
				if (num > 0)
				{
					ApplyStatus(target, StatusEnum.RemoveStatus, num, StatusRemovalData.RemoveRandomDebuffs());
				}
				break;
			case "Purge":
				if (num > 0)
				{
					ApplyStatus(target, StatusEnum.RemoveStatus, num, StatusRemovalData.RemoveRandomBuffs());
				}
				break;
			case "Damage":
				if (num > 0)
				{
					target.DealDamage(num, AttackBase.None, IsDirect: false, null, null);
				}
				break;
			case "Heal":
				if (num > 0)
				{
					target.Heal(num);
				}
				break;
			case "HealPct":
				if (num > 0)
				{
					target.Heal((float)num / 100f);
				}
				break;
			case "Draw":
			{
				for (int i = 0; i < num && i < 10; i++)
				{
					target.DrawCard();
				}
				break;
			}
			}
		}
	}
}
