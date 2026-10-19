using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class BattleReplayRecorder : Singleton<BattleReplayRecorder>
{
	private BattleReplayData currentReplay;
	private float battleStartTime;
	private bool isRecording;
	private string replayFilePath;

	public bool IsRecording
	{
		get { return isRecording; }
	}

	// Auto-create instance if it doesn't exist
	public static BattleReplayRecorder GetOrCreateInstance()
	{
		if (Instance == null)
		{
			GameObject recorderObj = new GameObject("BattleReplayRecorder");
			BattleReplayRecorder recorder = recorderObj.AddComponent<BattleReplayRecorder>();
			DontDestroyOnLoad(recorderObj);
			Debug.Log("[BattleReplayRecorder] Instance created automatically");
			return recorder;
		}
		return Instance;
	}

	public string CurrentReplayID
	{
		get { return currentReplay != null ? currentReplay.replayID : string.Empty; }
	}

	public void StartRecording()
	{
		if (isRecording)
		{
			Debug.LogWarning("[BattleReplayRecorder] Already recording a battle!");
			return;
		}

		currentReplay = new BattleReplayData();
		battleStartTime = Time.time;
		isRecording = true;

		// Set player information
		currentReplay.playerName = Singleton<PlayerInfoScript>.Instance.SaveData.MultiplayerPlayerName;
		
		if (Singleton<PlayerInfoScript>.Instance.PvPData != null)
		{
			currentReplay.opponentName = Singleton<PlayerInfoScript>.Instance.PvPData.OpponentName;
			
			// Store player loadout
			Loadout playerLoadout = Singleton<PlayerInfoScript>.Instance.GetCurrentLoadout();
			if (playerLoadout != null)
			{
				currentReplay.playerLeader = playerLoadout.Leader.Form.ID;
				foreach (var creatureSet in playerLoadout.CreatureSet)
				{
					if (creatureSet != null && creatureSet.Creature != null)
					{
						currentReplay.playerCreatures.Add(creatureSet.Creature.Form.ID);
					}
				}
			}

			// Store opponent loadout
			Loadout opponentLoadout = Singleton<PlayerInfoScript>.Instance.PvPData.OpponentLoadout;
			if (opponentLoadout != null)
			{
				currentReplay.opponentLeader = opponentLoadout.Leader.Form.ID;
				foreach (var creatureSet in opponentLoadout.CreatureSet)
				{
					if (creatureSet != null && creatureSet.Creature != null)
					{
						currentReplay.opponentCreatures.Add(creatureSet.Creature.Form.ID);
					}
				}
			}
		}

		RecordAction(BattleReplayData.ActionType.BattleStart, 0, string.Empty, string.Empty, string.Empty, 0, 0);
		Debug.Log("[BattleReplayRecorder] Recording started. Replay ID: " + currentReplay.replayID);
	}

	public void StopRecording(bool playerWon)
	{
		if (!isRecording)
		{
			Debug.LogWarning("[BattleReplayRecorder] No battle is being recorded!");
			return;
		}

		currentReplay.playerWon = playerWon;
		currentReplay.battleDuration = Time.time - battleStartTime;
		RecordAction(BattleReplayData.ActionType.BattleEnd, 0, string.Empty, string.Empty, string.Empty, 0, 0);

		SaveReplayToFile();
		isRecording = false;

		Debug.Log("[BattleReplayRecorder] Recording stopped. Duration: " + currentReplay.battleDuration + "s, Actions: " + currentReplay.actions.Count);
	}

	public void RecordCreaturePlay(int playerIndex, string creatureID, int lane)
	{
		if (!isRecording) return;
		RecordAction(BattleReplayData.ActionType.CreaturePlay, playerIndex, creatureID, string.Empty, string.Empty, lane, 0);
	}

	public void RecordCardPlay(int playerIndex, string cardID, string targetCreatureID, int lane)
	{
		if (!isRecording) return;
		RecordAction(BattleReplayData.ActionType.CardPlay, playerIndex, string.Empty, cardID, targetCreatureID, lane, 0);
	}

	public void RecordAttack(int playerIndex, string attackerID, string targetID, int damage)
	{
		if (!isRecording) return;
		RecordAction(BattleReplayData.ActionType.Attack, playerIndex, attackerID, string.Empty, targetID, 0, damage);
	}

	public void RecordTurnStart(int playerIndex)
	{
		if (!isRecording) return;
		RecordAction(BattleReplayData.ActionType.TurnStart, playerIndex, string.Empty, string.Empty, string.Empty, 0, 0);
	}

	public void RecordTurnEnd(int playerIndex)
	{
		if (!isRecording) return;
		RecordAction(BattleReplayData.ActionType.TurnEnd, playerIndex, string.Empty, string.Empty, string.Empty, 0, 0);
	}

	private void RecordAction(BattleReplayData.ActionType actionType, int playerIndex, string creatureID, string cardID, string targetCreatureID, int lane, int damage)
	{
		if (!isRecording) return;

		BattleReplayData.ReplayAction action = new BattleReplayData.ReplayAction();
		action.actionType = actionType;
		action.timestamp = Time.time - battleStartTime;
		action.playerIndex = playerIndex;
		action.creatureID = creatureID;
		action.cardID = cardID;
		action.targetCreatureID = targetCreatureID;
		action.lane = lane;
		action.damage = damage;

		currentReplay.actions.Add(action);
	}

	private void SaveReplayToFile()
	{
		try
		{
			string replayDirectory = Path.Combine(Application.persistentDataPath, "BattleReplays");
			
			// Create directory if it doesn't exist
			if (!Directory.Exists(replayDirectory))
			{
				Directory.CreateDirectory(replayDirectory);
			}

			string fileName = string.Format("replay_{0}_{1}.json", 
				currentReplay.battleTimestamp, 
				currentReplay.replayID.Substring(0, 8));
			replayFilePath = Path.Combine(replayDirectory, fileName);

			// Convert to JSON
			Dictionary<string, object> replayDict = currentReplay.ToJSONDictionary();
			string jsonString = MiniJSON.Json.Serialize(replayDict);

			// Save to file
			File.WriteAllText(replayFilePath, jsonString);

			Debug.Log("[BattleReplayRecorder] Replay saved to: " + replayFilePath);
		}
		catch (Exception ex)
		{
			Debug.LogError("[BattleReplayRecorder] Error saving replay: " + ex.Message);
			replayFilePath = string.Empty;
		}
	}

	public string GetLastReplayFilePath()
	{
		return replayFilePath;
	}

	public string GetLastReplayID()
	{
		return currentReplay != null ? currentReplay.replayID : string.Empty;
	}

	public static BattleReplayData LoadReplayFromFile(string filePath)
	{
		try
		{
			if (!File.Exists(filePath))
			{
				Debug.LogError("[BattleReplayRecorder] Replay file not found: " + filePath);
				return null;
			}

			string jsonString = File.ReadAllText(filePath);
			Dictionary<string, object> replayDict = MiniJSON.Json.Deserialize(jsonString) as Dictionary<string, object>;

			if (replayDict != null)
			{
				return BattleReplayData.FromJSONDictionary(replayDict);
			}
		}
		catch (Exception ex)
		{
			Debug.LogError("[BattleReplayRecorder] Error loading replay: " + ex.Message);
		}

		return null;
	}

	public static bool ReplayFileExists(string replayID)
	{
		if (string.IsNullOrEmpty(replayID)) return false;

		// Backwards/forwards compatibility:
		// - Some replays are saved with only the first 8 chars of the replayID in the filename.
		// - MatchHistory stores the full replayID.
		string replayIDShort = (replayID.Length >= 8) ? replayID.Substring(0, 8) : replayID;

		string replayDirectory = Path.Combine(Application.persistentDataPath, "BattleReplays");
		if (!Directory.Exists(replayDirectory)) return false;

		string[] files = Directory.GetFiles(replayDirectory, "*.json");
		foreach (string file in files)
		{
			string fileName = Path.GetFileName(file);
			if (fileName.Contains(replayID) || fileName.Contains(replayIDShort))
			{
				return true;
			}
		}

		return false;
	}

	public static string GetReplayFilePath(string replayID)
	{
		if (string.IsNullOrEmpty(replayID)) return string.Empty;

		string replayIDShort = (replayID.Length >= 8) ? replayID.Substring(0, 8) : replayID;

		string replayDirectory = Path.Combine(Application.persistentDataPath, "BattleReplays");
		if (!Directory.Exists(replayDirectory)) return string.Empty;

		string[] files = Directory.GetFiles(replayDirectory, "*.json");
		// Prefer exact/full replayID match first.
		foreach (string file in files)
		{
			if (Path.GetFileName(file).Contains(replayID))
			{
				return file;
			}
		}

		// Fallback to short-id match for older filenames.
		foreach (string file in files)
		{
			if (Path.GetFileName(file).Contains(replayIDShort))
			{
				return file;
			}
		}

		return string.Empty;
	}
}
