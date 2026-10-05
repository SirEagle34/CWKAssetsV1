using System;
using System.Collections.Generic;
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
				creatureDrop.SelectedDropType = inventorySlotItem.Creature.QuestLoadoutEntryData.RandomizeDropType(canLootCards, canLootRunes, true);
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
			for (int j = 0; j < item.Count; j++)
			{
				if (j != num)
				{
					item[j].SelectedDropType = item[j].CreatureOnBoard.QuestLoadoutEntryData.RandomizeDropType(canLootCards, canLootRunes, false);
					if (item[j].SelectedDropType == DropTypeEnum.None)
					{
						item[j].SelectedDropType = DropTypeEnum.SoftCurrency;
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
			if (num2 < mRemainingXPToLevelUp)
			{
				num2 = mRemainingXPToLevelUp;
			}
		}
		questRewards.XP = num2;
		if (addToSaveData)
		{
			saveData.SoftCurrency += questRewards.SoftCurrency;
			saveData.PvPCurrency += questRewards.SocialCurrency;
			Singleton<PlayerInfoScript>.Instance.AddHardCurrency2(0, questRewards.HardCurrency, "loot drop", -1, string.Empty);
			saveData.RankXP += questRewards.XP;
			int mCurrentLevel2 = Singleton<PlayerInfoScript>.Instance.RankXpLevelData.mCurrentLevel;
			if (mCurrentLevel2 > mCurrentLevel)
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
		code = code.Trim().ToUpper();

		mRedeemCodeCallback = null;

		PlayerSaveData saveData =
			Singleton<PlayerInfoScript>.Instance.SaveData;

		// ============================================================
		// USED REDEEM CODES
		// ============================================================

		if (saveData.UsedRedeemCodes == null)
		{
			saveData.UsedRedeemCodes = new List<string>();
		}

		if (saveData.UsedRedeemCodes.Contains(code))
		{
			callback(ResponseFlag.Error);
			return;
		}

		// ============================================================
		// EXISTING LOCAL REDEEM CODES
		// ============================================================

		if (redeemCodes.TryGetValue(code, out Action action))
		{
			action?.Invoke();

			saveData.UsedRedeemCodes.Add(code);

			Singleton<PlayerInfoScript>.Instance.Save();

			callback(ResponseFlag.Success);
			return;
		}

		// ============================================================
		// SERVER / ADMIN PANEL REDEEM CODE
		// ============================================================

		mRedeemCodeCallback = callback;

		SessionManager.Instance.theSession.Server.RedeemcodeCheck(
			code,
			RedeemServerCallback
		);
	}

	private static void RedeemServerCallback(
		Dictionary<string, object> response,
		System.Net.HttpStatusCode status
	)
	{
		var callback = mRedeemCodeCallback;
		mRedeemCodeCallback = null;

		// ============================================================
		// DEBUG SERVER RESPONSE
		// ============================================================

		Debug.Log(
			"[RedeemCode] Server status: " +
			status
		);

		if (response == null)
		{
			Debug.LogError(
				"[RedeemCode] Server response is NULL!"
			);

			callback?.Invoke(ResponseFlag.Error);
			return;
		}

		foreach (
			KeyValuePair<string, object> pair
			in response)
		{
			Debug.Log(
				"[RedeemCode] KEY = " +
				pair.Key +
				" | TYPE = " +
				(
					pair.Value != null
						? pair.Value.GetType().FullName
						: "NULL"
				) +
				" | VALUE = " +
				(
					pair.Value != null
						? pair.Value.ToString()
						: "NULL"
				)
			);
		}

		// Server başarısız
		if (!GetBool(response, "success"))
		{
			Debug.LogError(
				"[RedeemCode] Server returned success=false."
			);

			callback?.Invoke(ResponseFlag.Error);
			return;
		}

		PlayerSaveData saveData =
			Singleton<PlayerInfoScript>.Instance.SaveData;

		if (saveData.UsedRedeemCodes == null)
		{
			saveData.UsedRedeemCodes =
				new List<string>();
		}

		// Server aynı kodu tekrar gönderdiyse
		// client tarafında da engelle
		string code =
			GetString(
				response,
				"code"
			);

		if (string.IsNullOrEmpty(code))
		{
			Debug.LogError(
				"[RedeemCode] Server did not return code."
			);

			callback?.Invoke(ResponseFlag.Error);
			return;
		}

		code =
			code.Trim().ToUpper();

		if (saveData.UsedRedeemCodes.Contains(code))
		{
			Debug.LogWarning(
				"[RedeemCode] Code already used: " +
				code
			);

			callback?.Invoke(ResponseFlag.Error);
			return;
		}

		// ============================================================
		// REWARDS
		// ============================================================

		bool rewardAdded = false;

		// ------------------------------------------------------------
		// SOFT CURRENCY / COINS
		// ------------------------------------------------------------

		int softCurrency =
			GetInt(
				response,
				"soft_currency"
			);

		if (softCurrency > 0)
		{
			saveData.SoftCurrency +=
				softCurrency;

			rewardAdded = true;

			Debug.Log(
				"[RedeemCode] Coins received: " +
				softCurrency
			);
		}

		// ------------------------------------------------------------
		// FREE HARD CURRENCY / GEMS
		// ------------------------------------------------------------

		int freeHardCurrency =
			GetInt(
				response,
				"free_hard_currency"
			);

		if (freeHardCurrency > 0)
		{
			saveData.ModifyFreeHardCurrency(
				freeHardCurrency
			);

			rewardAdded = true;

			Debug.Log(
				"[RedeemCode] Gems received: " +
				freeHardCurrency
			);
		}

		// ============================================================
		// CREATURES
		// ============================================================

		object creaturesObject;

		if (
			response.TryGetValue(
				"creatures",
				out creaturesObject
			))
		{
			List<object> creatures =
				creaturesObject as List<object>;

			if (creatures != null)
			{
				foreach (
					object creatureObject
					in creatures)
				{
					Debug.Log(
						"[RedeemCode] CREATURE RAW = " +
						(
							creatureObject != null
								? creatureObject.ToString()
								: "NULL"
						)
					);

					Dictionary<string, object>
						creatureData =
							creatureObject
								as Dictionary<string, object>;

					if (creatureData == null)
					{
						continue;
					}

					string creatureID =
						GetString(
							creatureData,
							"id"
						);

					if (string.IsNullOrEmpty(
						creatureID))
					{
						continue;
					}

					CreatureItem creatureItem =
						new CreatureItem(
							creatureID
						);

					if (
						creatureData.ContainsKey(
							"star_rating"
						))
					{
						creatureItem.StarRating =
							GetInt(
								creatureData,
								"star_rating"
							);
					}

					if (
						creatureData.ContainsKey(
							"level"
						))
					{
						int level =
							GetInt(
								creatureData,
								"level"
							);

						if (level > 0)
						{
							creatureItem.Xp =
								creatureItem
									.XPTable
									.GetXpToReachLevel(
										level
									);
						}
					}

					saveData.AddCreature(
						creatureItem
					);

					rewardAdded = true;

					Debug.Log(
						"[RedeemCode] Creature received: " +
						creatureID
					);
				}
			}
		}

		// ============================================================
		// ACTION CARDS / EXCARDS
		// ============================================================

		object actionCardsObject;

		if (
			response.TryGetValue(
				"action_cards",
				out actionCardsObject
			))
		{
			List<object> actionCards =
				actionCardsObject as List<object>;

			if (actionCards != null)
			{
				foreach (
					object actionCardObject
					in actionCards)
				{
					Debug.Log(
						"[RedeemCode] ACTION CARD RAW = " +
						(
							actionCardObject != null
								? actionCardObject.ToString()
								: "NULL"
						)
					);

					string actionCardID =
						actionCardObject as string;

					if (
						string.IsNullOrEmpty(
							actionCardID))
					{
						Dictionary<string, object>
							cardData =
								actionCardObject
									as Dictionary<string, object>;

						if (cardData != null)
						{
							actionCardID =
								GetString(
									cardData,
									"id"
								);
						}
					}

					if (
						string.IsNullOrEmpty(
							actionCardID))
					{
						continue;
					}

					CardItem cardItem =
						new CardItem(
							actionCardID
						);

					saveData.AddExCard(
						cardItem
					);

					rewardAdded = true;

					Debug.Log(
						"[RedeemCode] Action Card received: " +
						actionCardID
					);
				}
			}
		}

		// ============================================================
		// CODE MUST HAVE AT LEAST ONE REWARD
		// ============================================================

		if (!rewardAdded)
		{
			Debug.LogError(
				"[RedeemCode] Server accepted the code, " +
				"but no recognized reward was found."
			);

			callback?.Invoke(
				ResponseFlag.Error
			);

			return;
		}

		// ============================================================
		// MARK CODE AS USED
		// ============================================================

		saveData.UsedRedeemCodes.Add(
			code
		);

		Singleton<PlayerInfoScript>.Instance.Save();

		Debug.Log(
			"[RedeemCode] Successfully redeemed: " +
			code
		);

		callback?.Invoke(
			ResponseFlag.Success
		);
	}

	private static bool GetBool(
		Dictionary<string, object> data,
		string key)
	{
		object value;

		if (!data.TryGetValue(key, out value) || value == null)
		{
			return false;
		}

		if (value is bool)
		{
			return (bool)value;
		}

		bool result;

		if (bool.TryParse(
			value.ToString(),
			out result))
		{
			return result;
		}

		return false;
	}

	private static int GetInt(
		Dictionary<string, object> data,
		string key)
	{
		object value;

		if (!data.TryGetValue(key, out value) || value == null)
		{
			return 0;
		}

		if (value is int)
		{
			return (int)value;
		}

		if (value is long)
		{
			return (int)(long)value;
		}

		if (value is double)
		{
			return (int)(double)value;
		}

		int result;

		if (int.TryParse(
			value.ToString(),
			out result))
		{
			return result;
		}

		return 0;
	}

	private static string GetString(
		Dictionary<string, object> data,
		string key)
	{
		object value;

		if (!data.TryGetValue(key, out value) || value == null)
		{
			return string.Empty;
		}

		return value.ToString();
	}


	private static void RedeemCallback(ResponseFlag flag)
	{
		var cb = mRedeemCodeCallback;
		mRedeemCodeCallback = null; // 🔥 önemli: leak + overwrite fix

		cb?.Invoke(flag);
	}

	private static Dictionary<string, Action> redeemCodes = new Dictionary<string, Action>()
	{
		{ "P1NKTR33", () => {
			GiveCreature50LvL("SnuggleTree_Awaken");
		}},
		{ "CORNCRAZY", () => {
			GiveCreature50LvL("CornRonin_Awaken");
		}},
		{ "JUSTSACK", () => {
			GiveCreature50LvL("SackOfPain_Awaken");
		}},
		{ "EPICDARKMAGICTHEDARKPOWER", () => {
			Singleton<PlayerInfoScript>.Instance.AddHardCurrency2(0, 75000, "redeem", -1, "");
		}},
		{ "FREEGEMS29", () => {
			Singleton<PlayerInfoScript>.Instance.AddHardCurrency2(0, 29000, "redeem", -1, "");
		}},
		{ "SPECIALCARD_01", () => {
			GiveActionCard("Leader_Finn_06");
		}},
		{ "SPECIALCARD_02", () => {
			GiveActionCard("Leader_Jake_08");
		}},
		{ "SPECIALCARD_03", () => {
			GiveActionCard("Leader_FlamePrincess_06");
		}},
		{ "SPECIALCARD_04", () => {
			GiveActionCard("Leader_IceKing_06");
		}},
		{ "SPECIALCARD_05", () => {
			GiveActionCard("Leader_LumpySpacePrincess_06");
		}},
		{ "SPECIALCARD_06", () => {
			GiveActionCard("Leader_GrandPrixe_06");
		}},
		{ "SPECIALCARD_07", () => {
			GiveActionCard("Leader_GrandPrixe_07");
		}},
		{ "SPECIALCARD_08", () => {
			GiveActionCard("Leader_Marceline_06");
		}},
		{ "SPECIALCARD_09", () => {
			GiveActionCard("Leader_EarlOfLemongrab_05");
		}},
		{ "SPECIALCARD_10", () => {
			GiveActionCard("Leader_BMO_06");
		}},
		{ "SPECIALCARD_11", () => {
			GiveActionCard("Leader_PeppermintButler_05");
		}},
		{ "SPECIALCARD_12", () => {
			GiveActionCard("Leader_Hunson_03");
		}},
		{ "SPECIALCARD_13", () => {
			GiveActionCard("Leader_Banana_02");
		}},
		{ "SPECIALCARD_14", () => {
			GiveActionCard("Leader_Jake_06");
		}}
	};

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
		PlayerSaveData saveData = Singleton<PlayerInfoScript>.Instance.SaveData;

		CreatureItem creatureItem = new CreatureItem(creatureID);
		creatureItem.StarRating = 5;
        creatureItem.Xp = 43075;
        creatureItem.PassiveSkillLevel = 1;

		saveData.AddCreature(creatureItem);

		Singleton<PlayerInfoScript>.Instance.Save();
	}

	private static void GiveActionCard(string actionCardID)
	{
		PlayerSaveData saveData = Singleton<PlayerInfoScript>.Instance.SaveData;

		CardItem cardItem = new CardItem(actionCardID);

		saveData.AddExCard(cardItem);

		Singleton<PlayerInfoScript>.Instance.Save();
	}

	private static void GiveLeader(string heroID)
	{
		PlayerSaveData saveData = Singleton<PlayerInfoScript>.Instance.SaveData;

		// Aynı leader zaten var mı kontrol (opsiyonel ama önerilir)
		if (saveData.Leaders.Exists(l => l.Form.ID == heroID))
			return;

		LeaderItem item = new LeaderItem(heroID);

		saveData.Leaders.Add(item);

		Singleton<PlayerInfoScript>.Instance.Save();
	}

	private static void GiveLeaderSkin(string leaderID, string skinID, bool autoSelect = true)
	{
		PlayerSaveData saveData = Singleton<PlayerInfoScript>.Instance.SaveData;

		LeaderData leaderData = LeaderDataManager.Instance.GetData(leaderID);
		LeaderData skinData = LeaderDataManager.Instance.GetData(skinID);

		if (leaderData == null || skinData == null)
			return;

		// Leader var mı?
		LeaderItem leaderItem = saveData.Leaders.Find(l => l.Form.ID == leaderID);

		// Yoksa oluştur
		if (leaderItem == null)
		{
			leaderItem = new LeaderItem(leaderID);
			saveData.Leaders.Add(leaderItem);
		}

		// Skin zaten var mı?
		if (!leaderItem.OwnedSkins.Exists(s => s.ID == skinID))
		{
			leaderItem.OwnedSkins.Add(skinData);
		}

		// İstersen otomatik seç
		if (autoSelect)
		{
			leaderItem.SelectedSkin = skinData;
		}

		Singleton<PlayerInfoScript>.Instance.Save();
	}

    private static void GiveCardBack(string cardBackID)
    {
        var playerInfo = Singleton<PlayerInfoScript>.Instance;
        var saveData = playerInfo.SaveData;

        var data = CardBackDataManager.Instance.GetData(cardBackID);
        if (data == null)
        {
            Debug.LogError($"CardBack not found: {cardBackID}");
            return;
        }

        // Zaten varsa tekrar ekleme
        if (!saveData.UnlockedCardBacks.Contains(data))
        {
            saveData.UnlockedCardBacks.Add(data);
        }

        playerInfo.Save();
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
