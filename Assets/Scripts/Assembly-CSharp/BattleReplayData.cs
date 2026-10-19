using System;
using System.Collections.Generic;

[Serializable]
public class BattleReplayData
{
	[Serializable]
	public enum ActionType
	{
		CreaturePlay,
		CardPlay,
		Attack,
		TurnStart,
		TurnEnd,
		BattleStart,
		BattleEnd
	}

	[Serializable]
	public class ReplayAction
	{
		public ActionType actionType;
		public float timestamp;
		public int playerIndex;
		public string creatureID;
		public string cardID;
		public string targetCreatureID;
		public int lane;
		public int damage;
		public Dictionary<string, object> additionalData;

		public ReplayAction()
		{
			additionalData = new Dictionary<string, object>();
		}

		public Dictionary<string, object> ToJSONDictionary()
		{
			Dictionary<string, object> dict = new Dictionary<string, object>();
			dict.Add("actionType", (int)actionType);
			dict.Add("timestamp", timestamp);
			dict.Add("playerIndex", playerIndex);
			if (!string.IsNullOrEmpty(creatureID))
				dict.Add("creatureID", creatureID);
			if (!string.IsNullOrEmpty(cardID))
				dict.Add("cardID", cardID);
			if (!string.IsNullOrEmpty(targetCreatureID))
				dict.Add("targetCreatureID", targetCreatureID);
			dict.Add("lane", lane);
			dict.Add("damage", damage);
			if (additionalData.Count > 0)
				dict.Add("additionalData", additionalData);
			return dict;
		}

		public static ReplayAction FromJSONDictionary(Dictionary<string, object> dict)
		{
			ReplayAction action = new ReplayAction();
			action.actionType = (ActionType)(int)dict["actionType"];
			action.timestamp = float.Parse(dict["timestamp"].ToString());
			action.playerIndex = int.Parse(dict["playerIndex"].ToString());
			if (dict.ContainsKey("creatureID"))
				action.creatureID = (string)dict["creatureID"];
			if (dict.ContainsKey("cardID"))
				action.cardID = (string)dict["cardID"];
			if (dict.ContainsKey("targetCreatureID"))
				action.targetCreatureID = (string)dict["targetCreatureID"];
			action.lane = int.Parse(dict["lane"].ToString());
			action.damage = int.Parse(dict["damage"].ToString());
			if (dict.ContainsKey("additionalData"))
			{
				action.additionalData = (Dictionary<string, object>)dict["additionalData"];
			}
			return action;
		}
	}

	public string replayID;
	public string playerName;
	public string opponentName;
	public uint battleTimestamp;
	public bool playerWon;
	public float battleDuration;
	public List<ReplayAction> actions;

	public List<string> playerCreatures;
	public List<string> opponentCreatures;
	public string playerLeader;
	public string opponentLeader;

	public BattleReplayData()
	{
		actions = new List<ReplayAction>();
		playerCreatures = new List<string>();
		opponentCreatures = new List<string>();
		replayID = Guid.NewGuid().ToString();
		battleTimestamp = DateTime.UtcNow.UnixTimestamp();
	}

	public Dictionary<string, object> ToJSONDictionary()
	{
		Dictionary<string, object> dict = new Dictionary<string, object>();
		dict.Add("replayID", replayID);
		dict.Add("playerName", playerName);
		dict.Add("opponentName", opponentName);
		dict.Add("battleTimestamp", battleTimestamp);
		dict.Add("playerWon", playerWon);
		dict.Add("battleDuration", battleDuration);

		List<object> actionsList = new List<object>();
		foreach (ReplayAction action in actions)
		{
			actionsList.Add(action.ToJSONDictionary());
		}
		dict.Add("actions", actionsList);

		dict.Add("playerCreatures", playerCreatures);
		dict.Add("opponentCreatures", opponentCreatures);
		dict.Add("playerLeader", playerLeader);
		dict.Add("opponentLeader", opponentLeader);

		return dict;
	}

	public static BattleReplayData FromJSONDictionary(Dictionary<string, object> dict)
	{
		BattleReplayData replay = new BattleReplayData();
		replay.replayID = (string)dict["replayID"];
		replay.playerName = (string)dict["playerName"];
		replay.opponentName = (string)dict["opponentName"];
		replay.battleTimestamp = uint.Parse(dict["battleTimestamp"].ToString());
		replay.playerWon = (bool)dict["playerWon"];
		replay.battleDuration = float.Parse(dict["battleDuration"].ToString());

		if (dict.ContainsKey("actions"))
		{
			object[] actionsArray = (object[])dict["actions"];
			foreach (object actionObj in actionsArray)
			{
				Dictionary<string, object> actionDict = (Dictionary<string, object>)actionObj;
				replay.actions.Add(ReplayAction.FromJSONDictionary(actionDict));
			}
		}

		replay.playerCreatures = new List<string>((string[])dict["playerCreatures"]);
		replay.opponentCreatures = new List<string>((string[])dict["opponentCreatures"]);
		replay.playerLeader = (string)dict["playerLeader"];
		replay.opponentLeader = (string)dict["opponentLeader"];

		return replay;
	}
}
