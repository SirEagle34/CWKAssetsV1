using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Multiplayer;
using UnityEngine;

public class RewardManager
{
	public class CreatureDrop
	{
		public CreatureItem CreatureOnBoard;

		public DropTypeEnum SelectedDropType;

		public bool Earned;

		public string OverrideID;
	}

	public class QuestRewards
	{
		public int SoftCurrency;

		public int SocialCurrency;

		public int HardCurrency;

		public int XP;

		public List<InventorySlotItem> CreaturesLooted = new List<InventorySlotItem>();

		public List<InventorySlotItem> EvoMatsLooted = new List<InventorySlotItem>();

		public List<InventorySlotItem> XPMatsLooted = new List<InventorySlotItem>();

		public List<InventorySlotItem> CardsLooted = new List<InventorySlotItem>();

		public List<GachaSlotData> KeysLooted = new List<GachaSlotData>();

		public int Stars;
	}

	public delegate void RedeemCodeCallback(ResponseFlag flag);

	private static QuestData mCurrentQuest;

	private static List<CreatureDrop> mSelectedDrops = new List<CreatureDrop>();

	public static int CreatureDropsPickedUp;

	private static bool mTriedOnce = false;

	private static RedeemCodeCallback mRedeemCodeCallback;

	public static string LastRedeemReason = "ERROR";

	public static string LastRedeemRewards = string.Empty;

	private static string mRedeemCode;

	private static readonly Dictionary<string, string> redeemItems = new Dictionary<string, string>
	{
		{ "P1NKTR33", "CR:SnuggleTree_Awaken" },
		{ "APPLE11", "CR:ApplePieclops_Awaken" },
		{ "RAINBOW44", "CR:RainbowBarfer_Awaken" },
		{ "SHIELDANDDRAW", "CA:PLAINS_0065" },
		{ "CORNPOWER", "CA:CORN_0017" },
		{ "THORNPROTECT", "CA:SAND_0006" },
		{ "SMELLYTEA", "CA:SWAMP_0027" },
		{ "SWEETCANDY", "CA:NICE_0007" },
		{ "BURSTERCOINS", "CR:ChestBurster_Base" },
		{ "TEASMELLY", "CA:SWAMP_0027" },
		{ "GHOSTINDUSTY", "CA:SPOOKY_0016" },
		{ "CURSETOWER", "CA:SPOOKY_0022" },
		{ "BLANKEYEDGIRL", "CR:BlankEyedGirl_Special" },
		{ "FREAKDEER", "CR:FreakDeer_Special" }
	};

	private static Dictionary<string, Action> redeemCodes = new Dictionary<string, Action>
	{
		{
			"P1NKTR33",
			delegate
			{
				GiveCreature50LvL("SnuggleTree_Awaken");
			}
		},
		{
			"APPLE11",
			delegate
			{
				GiveCreature50LvL("ApplePieclops_Awaken");
			}
		},
		{
			"RAINBOW44",
			delegate
			{
				GiveCreature50LvL("RainbowBarfer_Awaken");
			}
		},
		{
			"AUTUMNGEMS2026",
			delegate
			{
			}
		},
		{
			"SHIELDANDDRAW",
			delegate
			{
				GiveActionCard("PLAINS_0065");
			}
		},
		{
			"CORNPOWER",
			delegate
			{
				GiveActionCard("CORN_0017");
			}
		},
		{
			"THORNPROTECT",
			delegate
			{
				GiveActionCard("SAND_0006");
			}
		},
		{
			"SMELLYTEA",
			delegate
			{
				GiveActionCard("SWAMP_0027");
			}
		},
		{
			"SWEETCANDY",
			delegate
			{
				GiveActionCard("NICE_0007");
			}
		},
		{
			"BURSTERCOINS",
			delegate
			{
				GiveCreature50LvL("ChestBurster_Base");
			}
		},
		{
			"HALLOWEENORGEMS",
			delegate
			{
			}
		},
		{
			"TEASMELLY",
			delegate
			{
				GiveActionCard("SWAMP_0027");
			}
		},
		{
			"GHOSTINDUSTY",
			delegate
			{
				GiveActionCard("SPOOKY_0016");
			}
		},
		{
			"CURSETOWER",
			delegate
			{
				GiveActionCard("SPOOKY_0022");
			}
		},
		{
			"BLANKEYEDGIRL",
			delegate
			{
				GiveCreature50LvL("BlankEyedGirl_Special");
			}
		},
		{
			"FREAKDEER",
			delegate
			{
				GiveCreature50LvL("FreakDeer_Special");
			}
		}
	};

	private static readonly Dictionary<string, int> redeemGems = new Dictionary<string, int>
	{
		{ "AUTUMNGEMS2026", 36000 },
		{ "HALLOWEENORGEMS", 35000 }
	};

	public static void Init(QuestData questData, Loadout enemyLoadout)
	{
		mCurrentQuest = questData;
		CreatureDropsPickedUp = 0;
		mSelectedDrops.Clear();
		List<string> currentForcedDrops = Singleton<TutorialController>.Instance.GetCurrentForcedDrops();
		bool flag = Singleton<TutorialController>.Instance.IsBlockActive("IntroBattle");
		bool canLootCards = Singleton<PlayerInfoScript>.Instance.CanLootCards();
		bool canLootRunes = Singleton<PlayerInfoScript>.Instance.CanLootRunes();
		List<List<CreatureDrop>> list = new List<List<CreatureDrop>>();
		for (int i = 0; i < enemyLoadout.CreatureSet.Count; i++)
		{
			InventorySlotItem inventorySlotItem = enemyLoadout.CreatureSet[i];
			if (inventorySlotItem == null)
			{
				continue;
			}
			CreatureDrop creatureDrop = new CreatureDrop();
			creatureDrop.CreatureOnBoard = inventorySlotItem.Creature;
			if (Singleton<PlayerInfoScript>.Instance.StateData.MultiplayerMode)
			{
				creatureDrop.SelectedDropType = DropTypeEnum.None;
			}
			else if (currentForcedDrops != null)
			{
				string teamFaction = Singleton<TutorialController>.Instance.getChosenTeamFactionName();
				string text = currentForcedDrops.Find((string m) => m.Contains(teamFaction));
				if (text != null)
				{
					creatureDrop.SelectedDropType = DropTypeEnum.XPMaterial;
					creatureDrop.OverrideID = text;
					currentForcedDrops.Remove(text);
				}
				else
				{
					creatureDrop.SelectedDropType = DropTypeEnum.SoftCurrency;
				}
			}
			else if (flag)
			{
				creatureDrop.SelectedDropType = DropTypeEnum.None;
			}
			else if (Singleton<TutorialController>.Instance.IsFTUETutorialActive())
			{
				creatureDrop.SelectedDropType = DropTypeEnum.SoftCurrency;
			}
			else
			{
				creatureDrop.SelectedDropType = inventorySlotItem.Creature.QuestLoadoutEntryData.RandomizeDropType(canLootCards, canLootRunes, canLootCreatures: true);
				if (creatureDrop.SelectedDropType == DropTypeEnum.Creature)
				{
					string creatureGroup = inventorySlotItem.Creature.QuestLoadoutEntryData.DropInfo.CreatureGroup;
					if (creatureGroup != null)
					{
						List<CreatureDrop> list2 = list.Find((List<CreatureDrop> m) => m[0].CreatureOnBoard.QuestLoadoutEntryData.DropInfo.CreatureGroup == creatureGroup);
						if (list2 == null)
						{
							list2 = new List<CreatureDrop>();
							list.Add(list2);
						}
						list2.Add(creatureDrop);
					}
				}
			}
			mSelectedDrops.Add(creatureDrop);
		}
		foreach (List<CreatureDrop> item in list)
		{
			if (item.Count <= 1)
			{
				continue;
			}
			int num = UnityEngine.Random.Range(0, item.Count);
			for (int num2 = 0; num2 < item.Count; num2++)
			{
				if (num2 != num)
				{
					item[num2].SelectedDropType = item[num2].CreatureOnBoard.QuestLoadoutEntryData.RandomizeDropType(canLootCards, canLootRunes, canLootCreatures: false);
					if (item[num2].SelectedDropType == DropTypeEnum.None)
					{
						item[num2].SelectedDropType = DropTypeEnum.SoftCurrency;
					}
				}
			}
		}
	}

	public static bool GetCreatureDropInfo(CreatureState creature, out DropTypeEnum dropType, out int rarity)
	{
		CreatureDrop creatureDrop = mSelectedDrops.Find((CreatureDrop m) => m.CreatureOnBoard == creature.Data);
		if (creatureDrop == null || creatureDrop.Earned || creatureDrop.SelectedDropType == DropTypeEnum.None)
		{
			dropType = DropTypeEnum.None;
			rarity = -1;
			return false;
		}
		creatureDrop.Earned = true;
		dropType = creatureDrop.SelectedDropType;
		QuestLoadoutEntry.DropInfoClass dropInfo = creature.Data.QuestLoadoutEntryData.DropInfo;
		switch (dropType)
		{
		case DropTypeEnum.Creature:
			rarity = dropInfo.Creature.Rarity;
			break;
		case DropTypeEnum.Card:
			rarity = dropInfo.Card.Rarity;
			break;
		case DropTypeEnum.EvoMaterial:
			rarity = dropInfo.EvoMaterial.Rarity;
			break;
		case DropTypeEnum.XPMaterial:
			rarity = dropInfo.XPMaterial.Rarity;
			break;
		case DropTypeEnum.GachaKey:
			rarity = dropInfo.GachaKey.KeyRarity;
			break;
		default:
			rarity = -1;
			break;
		}
		return true;
	}

	public static QuestRewards AccumulateRewards(bool addToSaveData)
	{
		PlayerSaveData saveData = Singleton<PlayerInfoScript>.Instance.SaveData;
		GameStateData stateData = Singleton<PlayerInfoScript>.Instance.StateData;
		QuestRewards questRewards = new QuestRewards();
		int deadCreatureCount = Singleton<DWGame>.Instance.CurrentBoardState.GetPlayerState(PlayerType.User).GetDeadCreatureCount();
		if (deadCreatureCount <= MiscParams.StarThreshold3)
		{
			questRewards.Stars = 3;
		}
		else if (deadCreatureCount <= MiscParams.StarThreshold2)
		{
			questRewards.Stars = 2;
		}
		else if (deadCreatureCount <= MiscParams.StarThreshold1)
		{
			questRewards.Stars = 1;
		}
		else
		{
			questRewards.Stars = 0;
		}
		if (addToSaveData)
		{
			if (mCurrentQuest.League.QuestLine == QuestLineEnum.Main)
			{
				int intQuestId = mCurrentQuest.GetIntQuestId();
				if (intQuestId != -1)
				{
					if (intQuestId == 5 && saveData.TopCompletedQuestId < 5)
					{
						Singleton<KFFSocialManager>.Instance.ReportAchievement(KFFSocialManager.AchievementIDs.DW_QUEST_5);
					}
					else if (intQuestId == 10 && saveData.TopCompletedQuestId < 10)
					{
						Singleton<KFFSocialManager>.Instance.ReportAchievement(KFFSocialManager.AchievementIDs.DW_QUEST_10);
					}
					else if (intQuestId == 50 && saveData.TopCompletedQuestId < 50)
					{
						Singleton<KFFSocialManager>.Instance.ReportAchievement(KFFSocialManager.AchievementIDs.DW_QUEST_50);
					}
					if (saveData.TopCompletedQuestId < intQuestId)
					{
						saveData.TopCompletedQuestId = intQuestId;
					}
					Singleton<PlayerInfoScript>.Instance.SetQuestStars(intQuestId, questRewards.Stars);
				}
			}
			else if (mCurrentQuest.League.QuestLine == QuestLineEnum.Special)
			{
				Singleton<PlayerInfoScript>.Instance.OnSpecialQuestCompleted(mCurrentQuest.ID);
				if (!mCurrentQuest.OneTime)
				{
					Singleton<PlayerInfoScript>.Instance.SetDungeonStars(mCurrentQuest.ID, questRewards.Stars);
				}
			}
		}
		if (addToSaveData)
		{
			PlayerSaveData.BeginGrant();
		}
		foreach (CreatureDrop mSelectedDrop in mSelectedDrops)
		{
			if ((!addToSaveData && !mSelectedDrop.Earned) || mSelectedDrop.CreatureOnBoard.QuestLoadoutEntryData == null)
			{
				continue;
			}
			QuestLoadoutEntry.DropInfoClass dropInfo = mSelectedDrop.CreatureOnBoard.QuestLoadoutEntryData.DropInfo;
			if (mSelectedDrop.SelectedDropType == DropTypeEnum.SoftCurrency)
			{
				int num = dropInfo.SoftCurrency;
				if (stateData.ActiveQuestBonus == QuestBonusType.BonusSoftCurrencyQuantity)
				{
					num *= 2;
				}
				questRewards.SoftCurrency += num;
			}
			else if (mSelectedDrop.SelectedDropType == DropTypeEnum.SocialCurrency)
			{
				questRewards.SocialCurrency += dropInfo.SocialCurrency;
			}
			else if (mSelectedDrop.SelectedDropType == DropTypeEnum.HardCurrency)
			{
				questRewards.HardCurrency++;
			}
			else if (mSelectedDrop.SelectedDropType == DropTypeEnum.Creature)
			{
				if (addToSaveData)
				{
					CreatureItem creatureItem = new CreatureItem(dropInfo.Creature.ID);
					creatureItem.Xp = creatureItem.XPTable.GetXpToReachLevel(dropInfo.CreatureLevel);
					InventorySlotItem item = saveData.AddCreature(creatureItem);
					questRewards.CreaturesLooted.Add(item);
				}
				else
				{
					questRewards.CreaturesLooted.Add(new InventorySlotItem(dropInfo.Creature));
				}
			}
			else if (mSelectedDrop.SelectedDropType == DropTypeEnum.EvoMaterial)
			{
				if (addToSaveData)
				{
					InventorySlotItem item2 = saveData.AddEvoMaterial(dropInfo.EvoMaterial);
					questRewards.EvoMatsLooted.Add(item2);
				}
				else
				{
					questRewards.EvoMatsLooted.Add(new InventorySlotItem(dropInfo.EvoMaterial));
				}
			}
			else if (mSelectedDrop.SelectedDropType == DropTypeEnum.XPMaterial)
			{
				XPMaterialData xpMaterial = ((mSelectedDrop.OverrideID != null) ? XPMaterialDataManager.Instance.GetData(mSelectedDrop.OverrideID) : dropInfo.XPMaterial);
				if (addToSaveData)
				{
					InventorySlotItem item3 = saveData.AddXPMaterial(xpMaterial);
					questRewards.XPMatsLooted.Add(item3);
				}
				else
				{
					questRewards.XPMatsLooted.Add(new InventorySlotItem(xpMaterial));
				}
			}
			else if (mSelectedDrop.SelectedDropType == DropTypeEnum.Card)
			{
				if (addToSaveData)
				{
					CardItem card = new CardItem(dropInfo.Card);
					InventorySlotItem item4 = saveData.AddExCard(card);
					questRewards.CardsLooted.Add(item4);
				}
				else
				{
					questRewards.CardsLooted.Add(new InventorySlotItem(dropInfo.Card));
				}
			}
			else if (mSelectedDrop.SelectedDropType == DropTypeEnum.GachaKey)
			{
				if (addToSaveData)
				{
					Singleton<PlayerInfoScript>.Instance.AddGachaKey(dropInfo.GachaKey, 1);
				}
				questRewards.KeysLooted.Add(dropInfo.GachaKey);
			}
		}
		if (addToSaveData)
		{
			DetachedSingleton<MissionManager>.Instance.OnCreaturesCollected(questRewards.CreaturesLooted.Count);
			DetachedSingleton<MissionManager>.Instance.OnRunesCollected(questRewards.EvoMatsLooted.Count);
			DetachedSingleton<MissionManager>.Instance.OnXPMaterialsCollected(questRewards.XPMatsLooted.Count);
			DetachedSingleton<MissionManager>.Instance.OnCardsCollected(questRewards.CardsLooted.Count);
			DetachedSingleton<MissionManager>.Instance.OnCoinsCollected(questRewards.SoftCurrency);
			DetachedSingleton<MissionManager>.Instance.OnTeethCollected(questRewards.SocialCurrency);
		}
		int mCurrentLevel = Singleton<PlayerInfoScript>.Instance.RankXpLevelData.mCurrentLevel;
		int num2 = mCurrentQuest.XPReward;
		if (stateData.ActiveQuestBonus == QuestBonusType.BonusXp)
		{
			num2 = (int)((float)num2 * 1.5f);
		}
		if (Singleton<TutorialController>.Instance.IsFTUETutorialActive())
		{
			int mRemainingXPToLevelUp = Singleton<PlayerInfoScript>.Instance.RankXpLevelData.mRemainingXPToLevelUp;
			if (num2 < mRemainingXPToLevelUp && mRemainingXPToLevelUp != int.MaxValue)
			{
				num2 = mRemainingXPToLevelUp;
			}
		}
		questRewards.XP = num2;
		if (addToSaveData)
		{
			string ctx = ((mCurrentQuest.League != null && mCurrentQuest.League.ID == "DailyRandom") ? "RANDOM" : mCurrentQuest.ID);
			saveData.ClaimCurrency("quest reward", ctx, questRewards.SoftCurrency, questRewards.HardCurrency, questRewards.SocialCurrency, 0, null, PlayerSaveData.EndGrant());
			saveData.RankXP += questRewards.XP;
			if (Singleton<PlayerInfoScript>.Instance.RankXpLevelData.mCurrentLevel > mCurrentLevel)
			{
				DetachedSingleton<StaminaManager>.Instance.RefillStamina();
				DetachedSingleton<MissionManager>.Instance.AssignGlobalMissions();
			}
			Singleton<PlayerInfoScript>.Instance.Save();
		}
		return questRewards;
	}

	public static void UpdatePvPInfo()
	{
		PlayerInfoScript instance = Singleton<PlayerInfoScript>.Instance;
		string empty = string.Empty;
		string landscapes = instance.PvPSerialize();
		global::Multiplayer.Multiplayer.UpdateDeck(SessionManager.Instance.theSession, instance.SaveData.MultiplayerPlayerName, empty, SessionManager.loginOnece ? 1 : 0, landscapes, instance.SerializeHelperCreature(), instance.SaveData.MyHelperCreatureID.ToString(), instance.RankXpLevelData.mCurrentLevel, instance.SaveData.AllyBoxSpace, SuccessCallback);
		SessionManager.loginOnece = false;
	}

	private static void SuccessCallback(ResponseFlag flag)
	{
		if (flag == ResponseFlag.Error && !mTriedOnce)
		{
			PlayerInfoScript instance = Singleton<PlayerInfoScript>.Instance;
			global::Multiplayer.Multiplayer.CreateMultiplayerUser(SessionManager.Instance.theSession, instance, UserCreatedCallback);
		}
	}

	private static void UserCreatedCallback(MultiplayerData data, ResponseFlag flag)
	{
		mTriedOnce = true;
	}

	public static void RedeemCodeReward(string code, RedeemCodeCallback callback)
	{
		code = Regex.Replace(code ?? string.Empty, "[\\s\\-_]", string.Empty).ToUpperInvariant();
		LastRedeemReason = "ERROR";
		LastRedeemRewards = string.Empty;
		mRedeemCodeCallback = null;
		PlayerSaveData saveData = Singleton<PlayerInfoScript>.Instance.SaveData;
		if (saveData.UsedRedeemCodes == null)
		{
			saveData.UsedRedeemCodes = new List<string>();
		}
		Action action;
		if (code.Length == 0)
		{
			LastRedeemReason = "EMPTY";
			callback(ResponseFlag.Invalid);
		}
		else if (saveData.UsedRedeemCodes.Contains(code))
		{
			LastRedeemReason = "USED";
			callback(ResponseFlag.Error);
		}
		else if (redeemCodes.TryGetValue(code, out action))
		{
			int value;
			int gems = (redeemGems.TryGetValue(code, out value) ? value : 0);
			saveData.ClaimCurrency("redeem", code, 0, gems, 0, 0, delegate(PlayerSaveData.ActionResult result)
			{
				if (!result.success)
				{
					LastRedeemReason = ((result.reason == "ALREADY_CLAIMED") ? "USED" : "ERROR");
					callback(ResponseFlag.Error);
				}
				else
				{
					action?.Invoke();
					saveData.UsedRedeemCodes.Add(code);
					Singleton<PlayerInfoScript>.Instance.Save();
					LastRedeemReason = "OK";
					List<string> list = new List<string>();
					if (gems > 0)
					{
						list.Add(RedeemUI.Amount("!!REDEEM_GEMS", "{0} Gems", gems));
					}
					if (redeemItems.TryGetValue(code, out var value2))
					{
						list.Add(ItemName(value2.Substring(0, 2), value2.Substring(3)));
					}
					LastRedeemRewards = string.Join(", ", list.ToArray());
					callback(ResponseFlag.Success);
				}
			});
		}
		else
		{
			mRedeemCodeCallback = callback;
			mRedeemCode = code;
			global::Multiplayer.Multiplayer.LastRedeemReply = null;
			global::Multiplayer.Multiplayer.CheckRedeemCode(SessionManager.Instance.theSession, code, "100", KFFLocalization.Get("!!INVITE_REWARD_TITLE"), string.Concat(Singleton<PlayerInfoScript>.Instance.SaveData.MultiplayerPlayerName, " ", KFFLocalization.Get("!!INVITE_REWARD_MESSAGE")), DateTime.Now, DateTime.Now.AddDays(365.0), 0, 0, RedeemCallback);
		}
	}

	private static void RedeemCallback(ResponseFlag flag)
	{
		RedeemCodeCallback redeemCodeCallback = mRedeemCodeCallback;
		mRedeemCodeCallback = null;
		Dictionary<string, object> lastRedeemReply = global::Multiplayer.Multiplayer.LastRedeemReply;
		LastRedeemReason = (string)(((lastRedeemReply != null && lastRedeemReply.ContainsKey("reason")) ? Convert.ToString(lastRedeemReply["reason"]) : null) ?? (flag switch
		{
			ResponseFlag.Invalid => "INVALID", 
			ResponseFlag.Exceed => "USED", 
			ResponseFlag.Success => "OK", 
			_ => "ERROR", 
		}));
		if (flag == ResponseFlag.Success && lastRedeemReply != null)
		{
			try
			{
				ApplyServerRedeem(lastRedeemReply);
			}
			catch (Exception ex)
			{
				Debug.LogWarning("[Redeem] " + ex.Message);
			}
		}
		redeemCodeCallback?.Invoke(flag);
	}

	private static void ApplyServerRedeem(Dictionary<string, object> reply)
	{
		PlayerSaveData saveData = Singleton<PlayerInfoScript>.Instance.SaveData;
		Dictionary<string, object> dictionary = (reply.ContainsKey("fields") ? (reply["fields"] as Dictionary<string, object>) : null);
		if (dictionary != null)
		{
			saveData.ApplyServerClaim(dictionary);
		}
		Dictionary<string, object> dictionary2 = (reply.ContainsKey("rewards") ? (reply["rewards"] as Dictionary<string, object>) : null);
		bool flag = reply.ContainsKey("deliver") && Convert.ToString(reply["deliver"]) == "now";
		List<string> list = new List<string>();
		if (dictionary2 != null)
		{
			int num = (dictionary2.ContainsKey("gems") ? Convert.ToInt32(dictionary2["gems"]) : 0);
			int num2 = (dictionary2.ContainsKey("coins") ? Convert.ToInt32(dictionary2["coins"]) : 0);
			if (num > 0)
			{
				list.Add(RedeemUI.Amount("!!REDEEM_GEMS", "{0} Gems", num));
			}
			if (num2 > 0)
			{
				list.Add(RedeemUI.Amount("!!REDEEM_COINS", "{0} Coins", num2));
			}
			string[] array = new string[2] { "creatures", "cards" };
			foreach (string text in array)
			{
				List<object> list2 = (dictionary2.ContainsKey(text) ? (dictionary2[text] as List<object>) : null);
				if (list2 == null)
				{
					continue;
				}
				foreach (object item in list2)
				{
					string text2 = Convert.ToString(item);
					list.Add(ItemName((text == "creatures") ? "CR" : "CA", text2));
					if (!flag)
					{
						continue;
					}
					if (text == "creatures")
					{
						CreatureItem creatureItem = new CreatureItem(text2);
						if (creatureItem.Form != null)
						{
							creatureItem.StarRating = Mathf.Max(1, creatureItem.Form.Rarity);
							saveData.AddCreature(creatureItem);
						}
					}
					else
					{
						saveData.AddExCard(new CardItem(text2));
					}
				}
			}
		}
		if (!string.IsNullOrEmpty(mRedeemCode))
		{
			saveData.UsedRedeemCodes.Add(mRedeemCode);
		}
		Singleton<PlayerInfoScript>.Instance.Save();
		LastRedeemRewards = string.Join(", ", list.ToArray());
	}

	private static string ItemName(string kind, string id)
	{
		string text = null;
		text = ((!(kind == "CR")) ? CardDataManager.Instance.GetData(id)?.Name : CreatureDataManager.Instance.GetData(id)?.Name);
		if (string.IsNullOrEmpty(text))
		{
			return id;
		}
		if (!text.StartsWith("!!"))
		{
			return text;
		}
		return KFFLocalization.Get(text);
	}

	private static void GiveCreature(string creatureID)
	{
		PlayerSaveData saveData = Singleton<PlayerInfoScript>.Instance.SaveData;
		CreatureItem creatureItem = new CreatureItem(creatureID);
		creatureItem.Xp = creatureItem.XPTable.GetXpToReachLevel(1);
		saveData.AddCreature(creatureItem);
		Singleton<PlayerInfoScript>.Instance.Save();
	}

	private static void GiveCreature50LvL(string creatureID)
	{
		Singleton<PlayerInfoScript>.Instance.SaveData.AddCreature(new CreatureItem(creatureID)
		{
			StarRating = 5,
			Xp = 43075,
			PassiveSkillLevel = 1
		});
		Singleton<PlayerInfoScript>.Instance.Save();
	}

	private static void GiveActionCard(string actionCardID)
	{
		PlayerSaveData saveData = Singleton<PlayerInfoScript>.Instance.SaveData;
		CardItem card = new CardItem(actionCardID);
		saveData.AddExCard(card);
		Singleton<PlayerInfoScript>.Instance.Save();
	}

	private static void GiveLeader(string heroID)
	{
		PlayerSaveData saveData = Singleton<PlayerInfoScript>.Instance.SaveData;
		if (!saveData.Leaders.Exists((LeaderItem l) => l.Form.ID == heroID))
		{
			LeaderItem item = new LeaderItem(heroID);
			saveData.Leaders.Add(item);
			Singleton<PlayerInfoScript>.Instance.Save();
		}
	}

	private static void GiveLeaderSkin(string leaderID, string skinID, bool autoSelect = true)
	{
		PlayerSaveData saveData = Singleton<PlayerInfoScript>.Instance.SaveData;
		LeaderData data = LeaderDataManager.Instance.GetData(leaderID);
		LeaderData data2 = LeaderDataManager.Instance.GetData(skinID);
		if (data != null && data2 != null)
		{
			LeaderItem leaderItem = saveData.Leaders.Find((LeaderItem l) => l.Form.ID == leaderID);
			if (leaderItem == null)
			{
				leaderItem = new LeaderItem(leaderID);
				saveData.Leaders.Add(leaderItem);
			}
			if (!leaderItem.OwnedSkins.Exists((LeaderData s) => s.ID == skinID))
			{
				leaderItem.OwnedSkins.Add(data2);
			}
			if (autoSelect)
			{
				leaderItem.SelectedSkin = data2;
			}
			Singleton<PlayerInfoScript>.Instance.Save();
		}
	}

	private static void GiveCardBack(string cardBackID)
	{
		PlayerInfoScript instance = Singleton<PlayerInfoScript>.Instance;
		PlayerSaveData saveData = instance.SaveData;
		CardBackData data = CardBackDataManager.Instance.GetData(cardBackID);
		if (data == null)
		{
			Debug.LogError("CardBack not found: " + cardBackID);
			return;
		}
		if (!saveData.UnlockedCardBacks.Contains(data))
		{
			saveData.UnlockedCardBacks.Add(data);
		}
		instance.Save();
	}

	public static void TransferPvPRewardsToPlayerInfo()
	{
		PlayerSaveData saveData = Singleton<PlayerInfoScript>.Instance.SaveData;
		saveData.PvpBattles++;
		if (saveData.PvpBattles == 10)
		{
			Singleton<KFFSocialManager>.Instance.ReportAchievement(KFFSocialManager.AchievementIDs.DW_BATTLES_4);
		}
		else if (saveData.PvpBattles == 100)
		{
			Singleton<KFFSocialManager>.Instance.ReportAchievement(KFFSocialManager.AchievementIDs.DW_BATTLES_10);
		}
		else if (saveData.PvpBattles == 200)
		{
			Singleton<KFFSocialManager>.Instance.ReportAchievement(KFFSocialManager.AchievementIDs.DW_BATTLES_20);
		}
		else if (saveData.PvpBattles == 500)
		{
			Singleton<KFFSocialManager>.Instance.ReportAchievement(KFFSocialManager.AchievementIDs.DW_BATTLES_40);
		}
		else if (saveData.PvpBattles == 1000)
		{
			Singleton<KFFSocialManager>.Instance.ReportAchievement(KFFSocialManager.AchievementIDs.DW_BATTLES_100);
		}
	}
}
