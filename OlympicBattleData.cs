using System;
using System.Collections;
using System.Net;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class OlympicBattleData
{
	public static CustomQuestController.QuestEntry Dungeon { get; private set; }

	private static bool mVictoryPending;
	private static bool mProcessingResult;

	public static bool IsActive
	{
		get { return Dungeon != null; }
	}

	public static void Begin(CustomQuestController.QuestEntry dungeon)
	{
		Dungeon = dungeon;
		mVictoryPending = false;
		mProcessingResult = false;
	}

	public static Loadout BuildOpponentLoadout()
	{
		if (Dungeon == null || Dungeon.Creatures == null || Dungeon.Creatures.Count != 12 ||
			Dungeon.OwnerLeader == null)
		{
			return null;
		}

		LeaderItem leader = new LeaderItem(Dungeon.OwnerLeader.ID);
		if (leader.Form == null)
		{
			return null;
		}

		leader.SelectedSkin = Dungeon.OwnerLeaderSkin != null
			? Dungeon.OwnerLeaderSkin
			: Dungeon.OwnerLeader;

		Loadout loadout = new Loadout();
		loadout.Leader = leader;

		foreach (object value in Dungeon.Creatures)
		{
			Dictionary<string, object> data = value as Dictionary<string, object>;
			if (data == null)
			{
				return null;
			}

			CreatureItem creature = new CreatureItem(data, true);
			if (creature.Form == null)
			{
				return null;
			}

			loadout.CreatureSet.Add(new InventorySlotItem(creature));
		}

		return loadout.CreatureSet.Count == 12 ? loadout : null;
	}

	public static void MarkVictory()
	{
		if (Dungeon != null)
		{
			mVictoryPending = true;
		}
	}

	public static void Clear()
	{
		Dungeon = null;
		mVictoryPending = false;
		mProcessingResult = false;
	}

	public static void TryProcessFrontEndResult(PlayerInfoScript player)
	{
		if (!mVictoryPending || mProcessingResult || Dungeon == null || player == null ||
			player.SaveData == null)
		{
			return;
		}

		string frontEndScene = DetachedSingleton<SceneFlowManager>.Instance.GetNameFromScene(
			SceneFlowManager.Scene.FrontEnd);
		if (SceneManager.GetActiveScene().name != frontEndScene)
		{
			return;
		}

		mProcessingResult = true;
		int dungeonId = Dungeon.Id;

		if (player.SaveData.ClaimedOlympicDungeonRewards.Contains(dungeonId))
		{
			Clear();
			return;
		}

		player.StartCoroutine(GrantRewardsCo(player, Dungeon));
	}

	private static IEnumerator GrantRewardsCo(PlayerInfoScript player, CustomQuestController.QuestEntry dungeon)
	{
		int tradeAmount = 0;
		foreach (CustomQuestController.RewardEntry reward in dungeon.Rewards)
		{
			if (reward != null && reward.Type == "trade")
			{
				checked { tradeAmount += reward.Amount; }
			}
		}

		if (tradeAmount > 0)
		{
			bool requestDone = false;
			bool requestSucceeded = false;
			HttpStatusCode responseStatus = HttpStatusCode.ServiceUnavailable;

			SessionManager.Instance.theSession.Server.OlympicClaimTradeReward(
				dungeon.Id,
				delegate(Dictionary<string, object> response, HttpStatusCode status)
				{
					responseStatus = status;
					object successValue;
					requestSucceeded = status == HttpStatusCode.OK && response != null &&
						response.TryGetValue("success", out successValue) && successValue is bool && (bool)successValue;
					requestDone = true;
				});

			while (!requestDone) yield return null;

			if (!requestSucceeded)
			{
				Debug.LogError("[Olympic] Trade reward claim failed: " + responseStatus);
				Singleton<SimplePopupController>.Instance.ShowMessage(
					"Olympic Dungeon",
					"Your trade currency reward could not be claimed. Please try the dungeon again.");
				Clear();
				yield break;
			}
		}

		try
		{
			foreach (CustomQuestController.RewardEntry reward in dungeon.Rewards)
			{
				if (reward == null || reward.Amount <= 0)
				{
					continue;
				}

				switch (reward.Type)
				{
				case "trade":
					break;

				case "craft":
					player.SaveData.CraftCurrency += reward.Amount;
					break;

				case "pvp":
					player.SaveData.PvPCurrency += reward.Amount;
					break;

				case "cake":
					if (reward.Cake == null)
					{
						throw new InvalidOperationException("Olympic cake reward data is missing.");
					}
				for (int i = 0; i < reward.Amount; i++)
				{
					player.SaveData.AddXPMaterial(reward.Cake);
				}
				break;

				case "shard":
					if (reward.Shard == null)
					{
						throw new InvalidOperationException("Olympic shard reward data is missing.");
					}
				for (int i = 0; i < reward.Amount; i++)
				{
					player.SaveData.AddEvoMaterial(reward.Shard);
				}
				break;

				case "key":
					if (reward.Key == null)
					{
						throw new InvalidOperationException("Olympic key reward data is missing.");
					}
				player.AddGachaKey(reward.Key, reward.Amount);
				break;

				default:
					throw new InvalidOperationException("Unsupported Olympic reward type: " + reward.Type);
				}
			}
		}
		catch (Exception exception)
		{
			Debug.LogError("[Olympic] Reward grant failed: " + exception);
			Singleton<SimplePopupController>.Instance.ShowMessage(
				"Olympic Dungeon",
				"Some rewards could not be added. Please contact support before replaying this dungeon.");
			Clear();
			yield break;
		}

		int dungeonId = dungeon.Id;
		if (!player.SaveData.ClaimedOlympicDungeonRewards.Contains(dungeonId))
		{
			player.SaveData.ClaimedOlympicDungeonRewards.Add(dungeonId);
		}

		player.Save(delegate(bool success)
		{
			if (success)
			{
				Singleton<SimplePopupController>.Instance.ShowMessage(
					"Olympic Dungeon",
					"You completed the dungeon and earned your rewards!");
			}
			else
			{
				Singleton<SimplePopupController>.Instance.ShowMessage(
					"Olympic Dungeon",
					"Your rewards were added locally, but the server save could not be confirmed.");
			}

			Clear();
		});
	}
}
