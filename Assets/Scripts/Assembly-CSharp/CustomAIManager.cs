using System;
using System.Collections.Generic;
using System.Reflection;
using MoonSharp.Interpreter;

public class CustomAIManager : DetachedSingleton<CustomAIManager>
{
	public class ParsedRow
	{
		public CustomAIData.Row Data;

		public int RunCount;

		public List<ParsedCondition> Conditions = new List<ParsedCondition>();

		public List<ParsedAction> Actions = new List<ParsedAction>();

		public static ParsedRow Parse(CustomAIData.Row row)
		{
			ParsedRow parsedRow = new ParsedRow();
			parsedRow.Data = row;
			foreach (CustomAIData.FunctionCall condition in row.Conditions)
			{
				ParsedCondition parsedCondition = ParsedCondition.Parse(condition);
				if (parsedCondition != null)
				{
					parsedRow.Conditions.Add(parsedCondition);
				}
			}
			foreach (CustomAIData.FunctionCall action in row.Actions)
			{
				ParsedAction parsedAction = ParsedAction.ParseType(action);
				if (parsedAction != null)
				{
					parsedRow.Actions.Add(parsedAction);
				}
			}
			return parsedRow;
		}

		public PlanNode TryToExecute(PlanNode node)
		{
			bool flag = true;
			foreach (ParsedAction action in Actions)
			{
				if (action.TryToExecute(node, out var nextNode))
				{
					node = nextNode;
				}
				else
				{
					flag = false;
				}
			}
			if (flag)
			{
				OnExecuted();
			}
			return node;
		}

		public bool CheckConditions(BoardState state)
		{
			if (RunCount > 0 && Data.RepeatCooldown == -99)
			{
				return false;
			}
			foreach (ParsedCondition condition in Conditions)
			{
				if (!condition.CheckCondition(state))
				{
					return false;
				}
			}
			return true;
		}

		public void OnExecuted()
		{
			RunCount++;
		}

		public bool AtGameStart()
		{
			bool flag = false;
			bool flag2 = false;
			foreach (ParsedAction action in Actions)
			{
				if (action.AtGameStart)
				{
					flag = true;
				}
				else
				{
					flag2 = true;
				}
			}
			if (flag && flag2)
			{
				return false;
			}
			return flag;
		}

		public void AddForbids(bool conditionPassed, List<string> creatures, List<string> cards, List<string> creatureTargets)
		{
			foreach (ParsedAction action in Actions)
			{
				action.AddForbids(conditionPassed, creatures, cards, creatureTargets);
			}
		}

		public void ApplySpecialRules(BoardState state, PlayerType playerTurn)
		{
			foreach (ParsedAction action in Actions)
			{
				action.ApplySpecialRules(state, playerTurn);
			}
		}
	}

	public class ParsedCondition
	{
		private MethodInfo mMethodInfo;

		private object[] mParams;

		private bool mNegated;

		private LuaEffects.LuaScript mLua;

		private List<string> mLuaParams;

		public static ParsedCondition Parse(CustomAIData.FunctionCall data)
		{
			ParsedCondition parsedCondition = new ParsedCondition();
			parsedCondition.mMethodInfo = typeof(CustomAIScripts).GetMethod(data.FunctionName);
			if (parsedCondition.mMethodInfo == null)
			{
				LuaEffects.LuaScript luaScript = LuaEffects.Get(data.FunctionName);
				if (luaScript == null || !luaScript.Has("check"))
				{
					return null;
				}
				parsedCondition.mLua = luaScript;
				parsedCondition.mLuaParams = new List<string>();
				foreach (string parameter in data.Parameters)
				{
					parsedCondition.mLuaParams.Add(Convert.ToString((object)parameter));
				}
				parsedCondition.mNegated = data.Negated;
				return parsedCondition;
			}
			if (parsedCondition.mMethodInfo.ReturnType != typeof(bool))
			{
				return null;
			}
			ParameterInfo[] parameters = parsedCondition.mMethodInfo.GetParameters();
			if (parameters.Length - 1 != data.Parameters.Count)
			{
				return null;
			}
			parsedCondition.mParams = new object[data.Parameters.Count + 1];
			for (int i = 0; i < data.Parameters.Count; i++)
			{
				try
				{
					parsedCondition.mParams[i + 1] = Convert.ChangeType(data.Parameters[i], parameters[i + 1].ParameterType);
				}
				catch (Exception)
				{
				}
				if (parsedCondition.mParams[i + 1] == null)
				{
					return null;
				}
			}
			parsedCondition.mNegated = data.Negated;
			return parsedCondition;
		}

		public bool CheckCondition(BoardState state)
		{
			if (mLua != null)
			{
				return LuaCheck(state) != mNegated;
			}
			mParams[0] = state;
			return (bool)mMethodInfo.Invoke(null, mParams) != mNegated;
		}

		private bool LuaCheck(BoardState state)
		{
			Script script = mLua.Script;
			Table table = new Table(script);
			table["turn"] = state.TurnNumber;
			table["ai"] = Side(script, state.GetPlayerState(PlayerType.Opponent));
			table["user"] = Side(script, state.GetPlayerState(PlayerType.User));
			DynValue[] array = new DynValue[mLuaParams.Count + 1];
			array[0] = DynValue.NewTable(table);
			for (int i = 0; i < mLuaParams.Count; i++)
			{
				array[i + 1] = DynValue.NewString(mLuaParams[i]);
			}
			return mLua.Call("check", array)?.CastToBool() ?? false;
		}

		private static Table Side(Script sc, PlayerState ps)
		{
			Table table = new Table(sc);
			Table table2 = new Table(sc);
			if (ps != null)
			{
				table["lost"] = ps.GetDeadCreatureCount();
				table["hand"] = ps.DeploymentList.Count;
				foreach (LaneState lane in ps.Lanes)
				{
					if (lane != null && lane.Creature != null)
					{
						Table table3 = new Table(sc);
						table3["id"] = lane.Creature.Data.Form.ID;
						table3["hp"] = (double)lane.Creature.HPPct;
						table2.Append(DynValue.NewTable(table3));
					}
				}
			}
			table["creatures"] = table2;
			return table;
		}
	}

	public abstract class ParsedAction
	{
		public virtual bool AtGameStart => false;

		public static ParsedAction ParseType(CustomAIData.FunctionCall action)
		{
			Type type = Type.GetType("CustomAIScripts+" + action.FunctionName);
			if (type == null)
			{
				return null;
			}
			ParsedAction parsedAction = (ParsedAction)Activator.CreateInstance(type);
			try
			{
				parsedAction.Parse(action);
				return parsedAction;
			}
			catch (Exception)
			{
				return null;
			}
		}

		public virtual void Parse(CustomAIData.FunctionCall action)
		{
		}

		public virtual bool TryToExecute(PlanNode node, out PlanNode nextNode)
		{
			nextNode = null;
			return false;
		}

		public virtual void AddForbids(bool conditionPassed, List<string> creatures, List<string> cards, List<string> creatureTargets)
		{
		}

		public virtual void ApplySpecialRules(BoardState state, PlayerType playerTurn)
		{
		}
	}

	private List<ParsedRow> mParsedRows;

	private Dictionary<string, object> mForbiddenCreatures = new Dictionary<string, object>();

	private Dictionary<string, object> mForbiddenCards = new Dictionary<string, object>();

	private Dictionary<string, object> mForbiddenCreatureTargets = new Dictionary<string, object>();

	private List<CustomAIScripts.Dialog> mDialogToShow = new List<CustomAIScripts.Dialog>();

	public int OverrideEnergy;

	public int OverrideHandSize;

	public int NoCardDrawForPlayer;

	public int OnlyMagicAttacksDamageForPlayer;

	public int OnlyCritsDamageForPlayer;

	public int SwapAttackStatsForPlayer;

	public int OverrideDragAttackCost;

	public int OverrideDragAttackCostForPlayer;

	public bool CheckNoCardDrawForPlayer(PlayerType player)
	{
		if (NoCardDrawForPlayer != 2)
		{
			return NoCardDrawForPlayer == (int)player;
		}
		return true;
	}

	public bool CheckOnlyMagicAttacksDamageForPlayer(PlayerType player)
	{
		if (OnlyMagicAttacksDamageForPlayer != 2)
		{
			return OnlyMagicAttacksDamageForPlayer == (int)player;
		}
		return true;
	}

	public bool CheckOnlyCritsDamageForPlayer(PlayerType player)
	{
		if (OnlyCritsDamageForPlayer != 2)
		{
			return OnlyCritsDamageForPlayer == (int)player;
		}
		return true;
	}

	public bool CheckSwapAttackStatsForPlayer(PlayerType player)
	{
		if (SwapAttackStatsForPlayer != 2)
		{
			return SwapAttackStatsForPlayer == (int)player;
		}
		return true;
	}

	public bool CheckOverrideDragAttackCostForPlayer(PlayerType player)
	{
		if (OverrideDragAttackCostForPlayer != 2)
		{
			return OverrideDragAttackCostForPlayer == (int)player;
		}
		return true;
	}

	private void ResetSpecialRules(bool forNewGame)
	{
		NoCardDrawForPlayer = -1;
		OnlyMagicAttacksDamageForPlayer = -1;
		OnlyCritsDamageForPlayer = -1;
		SwapAttackStatsForPlayer = -1;
		OverrideDragAttackCostForPlayer = -1;
		if (forNewGame)
		{
			OverrideEnergy = -1;
			OverrideHandSize = -1;
		}
	}

	public void ParseAIData(CustomAIData data)
	{
		ResetSpecialRules(forNewGame: true);
		mParsedRows = new List<ParsedRow>();
		if (data == null)
		{
			return;
		}
		foreach (CustomAIData.Row row in data.Rows)
		{
			ParsedRow item = ParsedRow.Parse(row);
			mParsedRows.Add(item);
		}
	}

	public List<ParsedRow> GetCustomActionsAtGameStart()
	{
		List<ParsedRow> list = new List<ParsedRow>();
		foreach (ParsedRow mParsedRow in mParsedRows)
		{
			if (mParsedRow.AtGameStart())
			{
				list.Add(mParsedRow);
			}
		}
		return list;
	}

	public void SetSpecialRulesThisTurn(BoardState state, PlayerType playerTurn)
	{
		ResetSpecialRules(forNewGame: false);
		foreach (ParsedRow mParsedRow in mParsedRows)
		{
			if (mParsedRow.CheckConditions(state))
			{
				mParsedRow.ApplySpecialRules(state, playerTurn);
			}
		}
	}

	public List<ParsedRow> ReadCustomActionsThisTurn(BoardState state)
	{
		mForbiddenCreatures.Clear();
		mForbiddenCards.Clear();
		mForbiddenCreatureTargets.Clear();
		List<ParsedRow> list = new List<ParsedRow>();
		List<string> list2 = new List<string>();
		List<string> list3 = new List<string>();
		List<string> list4 = new List<string>();
		foreach (ParsedRow mParsedRow in mParsedRows)
		{
			bool flag = mParsedRow.CheckConditions(state);
			mParsedRow.AddForbids(flag, list2, list3, list4);
			if (flag)
			{
				list.Add(mParsedRow);
			}
		}
		foreach (string item in list2)
		{
			if (!mForbiddenCreatures.ContainsKey(item))
			{
				mForbiddenCreatures.Add(item, null);
			}
		}
		foreach (string item2 in list3)
		{
			if (!mForbiddenCards.ContainsKey(item2))
			{
				mForbiddenCards.Add(item2, null);
			}
		}
		foreach (string item3 in list4)
		{
			if (!mForbiddenCreatureTargets.ContainsKey(item3))
			{
				mForbiddenCreatureTargets.Add(item3, null);
			}
		}
		return list;
	}

	public bool IsCreatureForbidden(CreatureItem creature)
	{
		return mForbiddenCreatures.ContainsKey(creature.Form.ID);
	}

	public bool IsCardForbidden(CardData card)
	{
		return mForbiddenCards.ContainsKey(card.ID);
	}

	public bool IsCreatureTargetForbidden(CreatureItem creature)
	{
		return mForbiddenCreatureTargets.ContainsKey(creature.Form.ID);
	}

	public void AddDialogToShow(CustomAIScripts.Dialog dialog)
	{
		mDialogToShow.Add(dialog);
	}

	public CustomAIScripts.Dialog PopDialog()
	{
		if (mDialogToShow.Count == 0)
		{
			return null;
		}
		CustomAIScripts.Dialog result = mDialogToShow[0];
		mDialogToShow.RemoveAt(0);
		return result;
	}
}
