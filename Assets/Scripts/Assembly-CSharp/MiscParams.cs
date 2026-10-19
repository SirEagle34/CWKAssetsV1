using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

public class MiscParams : DataManager<DummyData>
{
	private int _SecondsPerStamina;

	private int _StaminaRefillCost;

	private int _MaxPvpStamina;

	private int _PvpStaminaMatchCost;

	private int _SecondsPerPvpStamina;

	private int _StartingLoadoutCount;

	private float _TownGridSpacing;

	private float _TownBuildingCollisionHeight;

	private int _TownAutoPlacementPadding;

	private int _ReviveCost;

	private int _StartingInventorySpace;

	private int _MaxInventorySpace;

	private int _InventorySpacePurchaseCost;

	private int _InventorySpacePerPurchase;

	private int _StartingActionPoints;

	private int _StartingStaminaPoints;

	private int _MaxActionPoints;

	private int _ExtensionCardWeight;

	private int _ActionPointIncrement;

	private int _HelpRewardForExplorer;

	private int _HelpRewardForAlly;

	private int _StartingAllyBoxSpace;

	private int _MaxAllyBoxSpace;

	private int _AllyBoxPurchaseCost;

	private int _AllyBoxPerPurchase;

	private int _CardsToDraw;

	private int _CriticalPercent;

	private bool _DrawToHandSize;

	private int _HandSize;

	private int _ActionPointsPerDiscard;

	private int _ActionPointsPerHeldCard;

	private int _SpecialQuestPreviewDays;

	private int _LeagueBonusPreviewHours;

	private CreatureData _TutorialCreature;

	private CreatureData _TutorialEVOCreature;

	private LeaderData _TutorialHero;

	private List<CardData> _TutorialCards = new List<CardData>();

	private float _PvpEntryCostPercent;

	private int _PvPCompatibilityVersion;

	private int _PvPExtremeCompatibilityVersion;

	private int _ChatCompatibilityVersion;

	private int _ChatSegmentingMinutes;

	private bool _ChatSeparateRegeon;

	private bool _PvpEnable;

	private bool _ChatEnable;

	private int _StarThreshold3;

	private int _StarThreshold2;

	private int _StarThreshold1;

	private bool _ForceDisableDebug;

	private bool _UseGooglePlayKFFKey;

	private int _MultiplayerUnrankedSearchRange;

	private int _MultiplayerUnrankedSearchRange2nd;

	private int _MultiplayerUnrankedSearchRange3rd;

	private int _MultiplayerExtremeSearchRange;

	private int _MultiplayerExtremeSearchRange2nd;

	private int _MultiplayerExtremeSearchRange3rd;

	private int _MultiplayerRankedSearchRange;

	private int _MultiplayerRankedSearchRange2nd;

	private int _MultiplayerRankedSearchRange3rd;

	private int _MultiplayerTimeLimitStart;

	private int _MultiplayerTimeLimitPerTurn;

	private int _MultiplayerTimeLimitShowAt;

	private int _MultiplayerNormalizedLevel;

	private List<int> _NotificationReminderHours;

	private int _MaxMatchesInHistory;

	private string _iTunesStoreURL;

	private string _marketStoreURL;

	private bool _ShowInviteButtons;

	private float _GachaXpLevelsPerPlayerRank;

	private int _MaxGachaXpLevel;

	private bool _DisableChat;

	private bool _DisablePvp;

	private bool _AndroidUsesDifferentFBAccount;

	private int _PvPHackThreshold;

	private float _PvPReconnectTimeoutHost;

	private float _PvPReconnectTimeoutClient;

	private float _PvPMessageAckTimeout;

	private int _PvPReconnectRetryCountHost;

	private int _PvPReconnectRetryCountClient;

	private float _PvPHeartBeat;

	private float _PvPResendDelay;

	private float _PvPLeaveDelay;

	private int _PvPPauseAllowMilliseconds;

	private int _PvPAllyMatchWaitLimitMilliseconds;

	private bool _HarderHashCheck;

	private string _CompassSupportURL;

	private bool _UseGameServerStats;

	private float _ThrottlePointsPerMessage;

	private float _ThrottleMultiplierPerMessage;

	private float _ThrottleDupeMultiplier;

	private float _ThrottlePointDecay;

	private float _ThrottleMultiplierDecay;

	private int _StandardGachaMultiPullLimit;

	private int _PremiumGachaMultiPullLimit;

	private int _GachaChatAnnounceRarity;

	private CalendarTable _ActiveCalendar;

	private CalendarTable _RepeatingCalendar;

	private int _GlobalSaleCooldownMinutes;

	private float _GachaChatAnnounceCooldown;

	private int _CreaturesOnBoard;

	private int _CreaturesOnTeam;

	private bool _AllowCreatureFusion;

	private int _StandardGachaCost;

	private int _PremiumGachaCost;

	private int _fbAgeGate;

	private int _chatAgeGate;

	private string _defaultTutCreature_Plains;

	private string _defaultTutCreature_Corn;

	private string _defaultTutCreature_Nice;

	private int _DailyRouletteLevelRequired;

	private int _CardCraftingLevelRequired;

	private int _BuySoftCurrencyCost;

	private int _BuySoftCurrencyAmount;

	private int _BuyEvolutionShardsCost;

	private int _BuyCraftCurrencyCost = 6500;

	private int _BuyCraftCurrencyAmount = 1;

	private int _BuyEvolutionShardsAmount;

	private int _UnlockAllDailyDungeonsGemCost;

	private DateTime _AwakenEventStartDate;

	private DateTime _AwakenEventEndDate;

	private int _AwakenEventBonus;

	private bool _CoinflipLoserGetsEnergyBonus;

	private bool _CoinflipLoserGetsAugmentCard;

	private bool _CoinflipLoserGetsEquippedAugment;

	private bool _CoinflipLoserGetsExodiaAugment;

	private bool _ForceUserCoinflipLoser;

	private static MiscParams _instance;

	private static Dictionary<string, object> sRaw = new Dictionary<string, object>();

	public static bool ForceUserCoinflipLoser => Instance._ForceUserCoinflipLoser;

	public static int SecondsPerStamina => Instance._SecondsPerStamina;

	public static int StaminaRefillCost => Instance._StaminaRefillCost;

	public static int MaxPvpStamina => Instance._MaxPvpStamina;

	public static int PvpStaminaMatchCost => Instance._PvpStaminaMatchCost;

	public static int SecondsPerPvpStamina => Instance._SecondsPerPvpStamina;

	public static int StartingLoadoutCount => Instance._StartingLoadoutCount;

	public static float TownGridSpacing => Instance._TownGridSpacing;

	public static float TownBuildingCollisionHeight => Instance._TownBuildingCollisionHeight;

	public static int TownAutoPlacementPadding => Instance._TownAutoPlacementPadding;

	public static int ReviveCost => Instance._ReviveCost;

	public static int StartingInventorySpace => Instance._StartingInventorySpace;

	public static int MaxInventorySpace => Instance._MaxInventorySpace;

	public static int InventorySpacePurchaseCost => Instance._InventorySpacePurchaseCost;

	public static int InventorySpacePerPurchase => Instance._InventorySpacePerPurchase;

	public static int StartingActionPoints => Instance._StartingActionPoints;

	public static int StartingStaminaPoints => Instance._StartingStaminaPoints;

	public static int MaxActionPoints => Instance._MaxActionPoints;

	public static int ActionPointIncrement => Instance._ActionPointIncrement;

	public static int HelpPointForExplorer => Instance._HelpRewardForExplorer;

	public static int HelpPointForAlly => Instance._HelpRewardForAlly;

	public static int StartingAllyBoxSpace => Instance._StartingAllyBoxSpace;

	public static int MaxAllyBoxSpace => Instance._MaxAllyBoxSpace;

	public static int AllyBoxPurchaseCost => Instance._AllyBoxPurchaseCost;

	public static int AllyBoxPerPurchase => Instance._AllyBoxPerPurchase;

	public static int ExtensionCardWeight => Instance._ExtensionCardWeight;

	public static int CardsToDraw => Instance._CardsToDraw;

	public static int CriticalPercent => Instance._CriticalPercent;

	public static bool DrawToHandSize => Instance._DrawToHandSize;

	public static int HandSize => Instance._HandSize;

	public static int ActionPointsPerDiscard => Instance._ActionPointsPerDiscard;

	public static int ActionPointsPerHeldCard => Instance._ActionPointsPerHeldCard;

	public static int SpecialQuestPreviewDays => Instance._SpecialQuestPreviewDays;

	public static int LeagueBonusPreviewHours => Instance._LeagueBonusPreviewHours;

	public static CreatureData TutorialCreature => Instance._TutorialCreature;

	public static CreatureData TutorialEVOCreature => Instance._TutorialEVOCreature;

	public static LeaderData TutorialHero => Instance._TutorialHero;

	public static List<CardData> TutorialCards => Instance._TutorialCards;

	public static float PvpEntryCostPercent => Instance._PvpEntryCostPercent;

	public static int PvPCompatibilityVersion => Instance._PvPCompatibilityVersion;

	public static int PvPExtremeCompatibilityVersion => Instance._PvPExtremeCompatibilityVersion;

	public static int ChatCompatibilityVersion => Instance._ChatCompatibilityVersion;

	public static int ChatSegmentingMinutes => Instance._ChatSegmentingMinutes;

	public static bool ChatSeparateRegeon => Instance._ChatSeparateRegeon;

	public static bool PvpEnable => Instance._PvpEnable;

	public static bool ChatEnable => Instance._ChatEnable;

	public static float PvPReconnectTimeoutHost => Instance._PvPReconnectTimeoutHost;

	public static float PvPReconnectTimeoutClient => Instance._PvPReconnectTimeoutClient;

	public static float PvPMessageAckTimeout => Instance._PvPMessageAckTimeout;

	public static int PvPReconnectRetryCountHost => Instance._PvPReconnectRetryCountHost;

	public static int PvPReconnectRetryCountClient => Instance._PvPReconnectRetryCountClient;

	public static float PvPHeartBeat => Instance._PvPHeartBeat;

	public static float PvPResendDelay => Instance._PvPResendDelay;

	public static float PvPLeaveDelay => Instance._PvPLeaveDelay;

	public static int PvPPauseAllowMilliseconds => Instance._PvPPauseAllowMilliseconds;

	public static int PvPAllyMatchWaitLimitMilliseconds => Instance._PvPAllyMatchWaitLimitMilliseconds;

	public static bool HarderHashCheck => Instance._HarderHashCheck;

	public static bool UseGameServerStats => Instance._UseGameServerStats;

	public static int StarThreshold3 => Instance._StarThreshold3;

	public static int StarThreshold2 => Instance._StarThreshold2;

	public static int StarThreshold1 => Instance._StarThreshold1;

	public static bool ForceDisableDebug => Instance._ForceDisableDebug;

	public static bool UseGooglePlayKFFKey => Instance._UseGooglePlayKFFKey;

	public static int MultiplayerUnrankedSearchRange => Instance._MultiplayerUnrankedSearchRange;

	public static int MultiplayerUnrankedSearchRange2nd => Instance._MultiplayerUnrankedSearchRange2nd;

	public static int MultiplayerUnrankedSearchRange3rd => Instance._MultiplayerUnrankedSearchRange3rd;

	public static int MultiplayerRankedSearchRange => Instance._MultiplayerRankedSearchRange;

	public static int MultiplayerRankedSearchRange2nd => Instance._MultiplayerRankedSearchRange2nd;

	public static int MultiplayerRankedSearchRange3rd => Instance._MultiplayerRankedSearchRange3rd;

	public static int MultiplayerExtremeSearchRange => Instance._MultiplayerExtremeSearchRange;

	public static int MultiplayerExtremeSearchRange2nd => Instance._MultiplayerExtremeSearchRange2nd;

	public static int MultiplayerExtremeSearchRange3rd => Instance._MultiplayerExtremeSearchRange3rd;

	public static int MultiplayerTimeLimitStart => Instance._MultiplayerTimeLimitStart;

	public static int MultiplayerTimeLimitPerTurn => Instance._MultiplayerTimeLimitPerTurn;

	public static int MultiplayerTimeLimitShowAt => Instance._MultiplayerTimeLimitShowAt;

	public static int MultiplayerNormalizedLevel => Instance._MultiplayerNormalizedLevel;

	public static List<int> NotificationReminderHours => Instance._NotificationReminderHours;

	public static int MaxMatchesInHistory => Instance._MaxMatchesInHistory;

	public static string iTunesStoreURL => Instance._iTunesStoreURL;

	public static string marketStoreURL => Instance._marketStoreURL;

	public static string CompassSupportURL => Instance._CompassSupportURL;

	public static bool ShowInviteButtons => Instance._ShowInviteButtons;

	public static float GachaXpLevelsPerPlayerRank => Instance._GachaXpLevelsPerPlayerRank;

	public static int MaxGachaXpLevel => Instance._MaxGachaXpLevel;

	public static bool DisableChat => Instance._DisableChat;

	public static bool DisablePvp => Instance._DisablePvp;

	public static int PvPHackThreshold => Instance._PvPHackThreshold;

	public static bool AndroidUsesDifferentFBAccount => Instance._AndroidUsesDifferentFBAccount;

	public static float ThrottlePointsPerMessage => Instance._ThrottlePointsPerMessage;

	public static float ThrottleMultiplierPerMessage => Instance._ThrottleMultiplierPerMessage;

	public static float ThrottleDupeMultiplier => Instance._ThrottleDupeMultiplier;

	public static float ThrottlePointDecay => Instance._ThrottlePointDecay;

	public static float ThrottleMultiplierDecay => Instance._ThrottleMultiplierDecay;

	public static int StandardGachaMultiPullLimit => Instance._StandardGachaMultiPullLimit;

	public static int PremiumGachaMultiPullLimit => Instance._PremiumGachaMultiPullLimit;

	public static int GachaChatAnnounceRarity => Instance._GachaChatAnnounceRarity;

	public static CalendarTable ActiveCalendar => Instance._ActiveCalendar;

	public static CalendarTable RepeatingCalendar => Instance._RepeatingCalendar;

	public static int GlobalSaleCooldownMinutes => Instance._GlobalSaleCooldownMinutes;

	public static float GachaChatAnnounceCooldown => Instance._GachaChatAnnounceCooldown;

	public static int CreaturesOnBoard => Instance._CreaturesOnBoard;

	public static int CreaturesOnTeam => Instance._CreaturesOnTeam;

	public static bool AllowCreatureFusion => Instance._AllowCreatureFusion;

	public static int fbAgeGate => Instance._fbAgeGate;

	public static int chatAgeGate => Instance._chatAgeGate;

	public static string defaultTutCreature_Plains => Instance._defaultTutCreature_Plains;

	public static string defaultTutCreature_Corn => Instance._defaultTutCreature_Corn;

	public static string defaultTutCreature_Nice => Instance._defaultTutCreature_Nice;

	public static int DailyRouletteLevelRequired => Instance._DailyRouletteLevelRequired;

	public static int CardCraftingLevelRequired => Instance._CardCraftingLevelRequired;

	public static int BuySoftCurrencyCost => Instance._BuySoftCurrencyCost;

	public static int BuySoftCurrencyAmount => Instance._BuySoftCurrencyAmount;

	public static int BuyCraftCurrencyCost => Instance._BuyCraftCurrencyCost;

	public static int BuyCraftCurrencyAmount => Instance._BuyCraftCurrencyAmount;

	public static int BuyEvolutionShardsCost => Instance._BuyEvolutionShardsCost;

	public static int UnlockAllDailyDungeonsGemCost => Instance._UnlockAllDailyDungeonsGemCost;

	public static int BuyEvolutionShardsAmount => Instance._BuyEvolutionShardsAmount;

	public static DateTime AwakenEventStartDate => Instance._AwakenEventStartDate;

	public static DateTime AwakenEventEndDate => Instance._AwakenEventEndDate;

	public static int AwakenEventBonus => Instance._AwakenEventBonus;

	public static bool CoinflipLoserGetsEnergyBonus => Instance._CoinflipLoserGetsEnergyBonus;

	public static bool CoinflipLoserGetsAugmentCard => Instance._CoinflipLoserGetsAugmentCard;

	public static bool CoinflipLoserGetsEquippedAugment => Instance._CoinflipLoserGetsEquippedAugment;

	public static bool CoinflipLoserGetsExodiaAugment => Instance._CoinflipLoserGetsExodiaAugment;

	public static MiscParams Instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = new MiscParams(Path.Combine(SQSettings.CDN_URL, "Blueprints", "db_MiscParams.json"));
			}
			return _instance;
		}
	}

	public MiscParams(string path)
	{
		base.FilePath = path;
		AddDependency(CreatureDataManager.Instance);
		AddDependency(LeaderDataManager.Instance);
		AddDependency(CalendarGiftDataManager.Instance);
	}

	public static int GetInt(string key, int defaultValue)
	{
		return SafeInt(sRaw, key, defaultValue);
	}

	public static float GetFloat(string key, float defaultValue)
	{
		return SafeFloat(sRaw, key, defaultValue);
	}

	public static bool GetBool(string key, bool defaultValue)
	{
		return SafeBool(sRaw, key, defaultValue);
	}

	public static string GetString(string key, string defaultValue)
	{
		return SafeString(sRaw, key, defaultValue);
	}

	private static int SafeInt(Dictionary<string, object> d, string key, int defaultValue)
	{
		try
		{
			return TFUtils.LoadInt(d, key, defaultValue);
		}
		catch (Exception e)
		{
			BadValue(key, e);
			return defaultValue;
		}
	}

	private static float SafeFloat(Dictionary<string, object> d, string key, float defaultValue)
	{
		try
		{
			return TFUtils.LoadFloat(d, key, defaultValue);
		}
		catch (Exception e)
		{
			BadValue(key, e);
			return defaultValue;
		}
	}

	private static bool SafeBool(Dictionary<string, object> d, string key, bool defaultValue)
	{
		try
		{
			return TFUtils.LoadBool(d, key, defaultValue);
		}
		catch (Exception e)
		{
			BadValue(key, e);
			return defaultValue;
		}
	}

	private static string SafeString(Dictionary<string, object> d, string key, string defaultValue)
	{
		try
		{
			return TFUtils.LoadString(d, key, defaultValue);
		}
		catch (Exception e)
		{
			BadValue(key, e);
			return defaultValue;
		}
	}

	public static bool InDateWindow(DateTime today, string startKey, string startDefault, string endKey, string endDefault)
	{
		DateTime dateTime = MonthDay(today.Year, GetString(startKey, startDefault), startDefault);
		DateTime dateTime2 = MonthDay(today.Year, GetString(endKey, endDefault), endDefault);
		DateTime date = today.Date;
		if (!(dateTime <= dateTime2))
		{
			if (!(date >= dateTime))
			{
				return date < dateTime2;
			}
			return true;
		}
		if (date >= dateTime)
		{
			return date < dateTime2;
		}
		return false;
	}

	private static DateTime MonthDay(int year, string md, string fallback)
	{
		if (DateTime.TryParseExact(year + "-" + md, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
		{
			return result;
		}
		return DateTime.ParseExact(year + "-" + fallback, "yyyy-MM-dd", CultureInfo.InvariantCulture);
	}

	private static void BadValue(string key, Exception e)
	{
		Debug.LogWarning("[DataSkip] db_MiscParams " + key + ": " + e.Message);
	}

	protected override void ParseRows(List<object> jlist)
	{
		if (jlist.Count == 0)
		{
			return;
		}
		Dictionary<string, object> dictionary = new Dictionary<string, object>();
		foreach (Dictionary<string, object> item in jlist)
		{
			if (item.ContainsKey("Value"))
			{
				string key = TFUtils.LoadString(item, "Parameter", string.Empty);
				object value = item["Value"];
				dictionary[key] = value;
			}
		}
		sRaw = dictionary;
		_SecondsPerStamina = SafeInt(dictionary, "SecondsPerStamina", 1);
		_StaminaRefillCost = SafeInt(dictionary, "StaminaRefillCost", 1);
		_MaxPvpStamina = SafeInt(dictionary, "MaxPvpStamina", 1);
		_PvpStaminaMatchCost = SafeInt(dictionary, "PvpStaminaMatchCost", 1);
		_SecondsPerPvpStamina = SafeInt(dictionary, "SecondsPerPvpStamina", 1);
		_StartingLoadoutCount = SafeInt(dictionary, "StartingLoadoutCount", 1);
		_TownGridSpacing = SafeFloat(dictionary, "TownGridSpacing", 1f);
		_TownBuildingCollisionHeight = SafeFloat(dictionary, "TownBuildingCollisionHeight", 1f);
		_TownAutoPlacementPadding = SafeInt(dictionary, "TownAutoPlacementPadding", 1);
		_ReviveCost = SafeInt(dictionary, "ReviveCost", 1);
		_StartingInventorySpace = SafeInt(dictionary, "StartingInventorySpace", 1);
		_MaxInventorySpace = SafeInt(dictionary, "MaxInventorySpace", 1);
		_InventorySpacePurchaseCost = SafeInt(dictionary, "InventorySpacePurchaseCost", 1);
		_InventorySpacePerPurchase = SafeInt(dictionary, "InventorySpacePerPurchase", 1);
		_StartingActionPoints = SafeInt(dictionary, "StartingActionPoints", 50);
		_StartingStaminaPoints = SafeInt(dictionary, "StartingStaminaPoints", 2);
		_MaxActionPoints = SafeInt(dictionary, "MaxActionPoints", 100);
		_ActionPointIncrement = SafeInt(dictionary, "ActionPointIncrement", 5);
		_HelpRewardForExplorer = SafeInt(dictionary, "HelpPointForExplorer", 5);
		_HelpRewardForAlly = SafeInt(dictionary, "HelpPointForAlly", 10);
		_StartingAllyBoxSpace = SafeInt(dictionary, "StartingAllyBoxSpace", 1);
		_MaxAllyBoxSpace = SafeInt(dictionary, "MaxAllyBoxSpace", 1);
		_AllyBoxPurchaseCost = SafeInt(dictionary, "AllyBoxPurchaseCost", 1);
		_AllyBoxPerPurchase = SafeInt(dictionary, "AllyBoxPerPurchase", 1);
		_CardsToDraw = SafeInt(dictionary, "CardsToDraw", 1);
		_CriticalPercent = SafeInt(dictionary, "CriticalPercent", 25);
		_ExtensionCardWeight = SafeInt(dictionary, "ExtensionCardWeight", 10);
		_DrawToHandSize = SafeBool(dictionary, "DrawToHandSize", defaultValue: false);
		_HandSize = SafeInt(dictionary, "HandSize", 5);
		_ActionPointsPerDiscard = SafeInt(dictionary, "ActionPointsPerDiscard", 1);
		_ActionPointsPerHeldCard = SafeInt(dictionary, "ActionPointsPerHeldCard", 10);
		_SpecialQuestPreviewDays = SafeInt(dictionary, "SpecialQuestPreviewDays", 0);
		_LeagueBonusPreviewHours = SafeInt(dictionary, "LeagueBonusPreviewHours", 0);
		_TutorialCreature = CreatureDataManager.Instance.GetData(SafeString(dictionary, "TutorialCreature", string.Empty));
		_TutorialEVOCreature = CreatureDataManager.Instance.GetData(SafeString(dictionary, "TutorialEVOCreature", string.Empty));
		_TutorialHero = LeaderDataManager.Instance.GetData(SafeString(dictionary, "TutorialBuyHero", string.Empty));
		string[] array = SafeString(dictionary, "TutorialCards", string.Empty).Split(',');
		foreach (string text in array)
		{
			CardData data = CardDataManager.Instance.GetData(text.Trim());
			if (data != null)
			{
				_TutorialCards.Add(data);
			}
		}
		_PvpEntryCostPercent = SafeFloat(dictionary, "PvpEntryCostPercent", 0f);
		_PvPCompatibilityVersion = SafeInt(dictionary, "PvPCompatibilityVersion", 110);
		_PvPExtremeCompatibilityVersion = SafeInt(dictionary, "PvPExtremeCompatibilityVersion", 612);
		_ChatCompatibilityVersion = SafeInt(dictionary, "ChatCompatibilityVersion", 100);
		_ChatSegmentingMinutes = SafeInt(dictionary, "ChatSegmentingMinutes", 60);
		_ChatSeparateRegeon = SafeBool(dictionary, "ChatSeparateRegeon", defaultValue: false);
		_PvpEnable = SafeBool(dictionary, "PvpEnable", defaultValue: true);
		_ChatEnable = SafeBool(dictionary, "ChatEnable", defaultValue: true);
		_StarThreshold3 = SafeInt(dictionary, "3StarThreshold", 1);
		_StarThreshold2 = SafeInt(dictionary, "2StarThreshold", 2);
		_StarThreshold1 = SafeInt(dictionary, "1StarThreshold", 3);
		_ForceDisableDebug = SafeBool(dictionary, "ForceDisableDebug", defaultValue: false);
		_UseGooglePlayKFFKey = SafeBool(dictionary, "UseGooglePlayKFFKey", defaultValue: false);
		_MultiplayerUnrankedSearchRange = SafeInt(dictionary, "MultiplayerUnrankedSearchRange", 1);
		_MultiplayerUnrankedSearchRange2nd = SafeInt(dictionary, "MultiplayerUnrankedSearchRange2ndPath", 2);
		_MultiplayerUnrankedSearchRange3rd = SafeInt(dictionary, "MultiplayerUnrankedSearchRange3rdPath", 3);
		_MultiplayerRankedSearchRange = SafeInt(dictionary, "MultiplayerRankedSearchRange", 1);
		_MultiplayerRankedSearchRange2nd = SafeInt(dictionary, "MultiplayerRankedSearchRange2ndPath", 2);
		_MultiplayerRankedSearchRange3rd = SafeInt(dictionary, "MultiplayerRankedSearchRange3rdPath", 3);
		_MultiplayerExtremeSearchRange = SafeInt(dictionary, "MultiplayerExtremeSearchRange", 1);
		_MultiplayerExtremeSearchRange2nd = SafeInt(dictionary, "MultiplayerExtremeSearchRange2ndPath", 2);
		_MultiplayerExtremeSearchRange3rd = SafeInt(dictionary, "MultiplayerExtremeSearchRange3rdPath", 3);
		_MultiplayerTimeLimitStart = SafeInt(dictionary, "MultiplayerTimeLimitStart", 600);
		_MultiplayerTimeLimitPerTurn = SafeInt(dictionary, "MultiplayerTimeLimitPerTurn", 5);
		_MultiplayerTimeLimitShowAt = SafeInt(dictionary, "MultiplayerTimeLimitShowAt", 15);
		_MultiplayerNormalizedLevel = SafeInt(dictionary, "MultiplayerNormalizedLevel", 30);
		_MaxMatchesInHistory = SafeInt(dictionary, "MaxMatchesInHistory", 50);
		_iTunesStoreURL = SafeString(dictionary, "iTunesStoreURL", "itunes.apple.com/us/app/card-wars-kingdom-adventure/id1084805156?ls=1&mt=8&uo=4&at=11lmxx");
		_marketStoreURL = SafeString(dictionary, "marketStoreURL", "com.turner.cardwars2");
		_CompassSupportURL = SafeString(dictionary, "CompassSupportURL", "http://dev4.compasssupport.crooz.jp/");
		_ShowInviteButtons = SafeBool(dictionary, "ShowInviteButtons", defaultValue: false);
		_GachaXpLevelsPerPlayerRank = SafeFloat(dictionary, "GachaXpLevelsPerPlayerRank", 0.1f);
		_MaxGachaXpLevel = SafeInt(dictionary, "MaxGachaXpLevel", 10);
		_DisableChat = SafeBool(dictionary, "DisableChat", defaultValue: false);
		_DisablePvp = SafeBool(dictionary, "DisablePvp", defaultValue: false);
		_PvPHackThreshold = SafeInt(dictionary, "PvPHackThreshold", 9989);
		_PvPReconnectTimeoutHost = SafeFloat(dictionary, "PvPReconnectTimeoutHost", 11f);
		_PvPReconnectTimeoutClient = SafeFloat(dictionary, "PvPReconnectTimeoutClient", 4f);
		_PvPMessageAckTimeout = SafeFloat(dictionary, "PvPMessageAckTimeout", 4f);
		_PvPReconnectRetryCountHost = SafeInt(dictionary, "PvPReconnectRetryCountHost", 3);
		_PvPReconnectRetryCountClient = SafeInt(dictionary, "PvPReconnectRetryCountClient", 7);
		_PvPHeartBeat = SafeFloat(dictionary, "PvPHeartBeat", 5f);
		_PvPResendDelay = SafeFloat(dictionary, "PvPResendDelay", 1f);
		_PvPLeaveDelay = SafeFloat(dictionary, "PvPLeaveDelay", 0.5f);
		_PvPPauseAllowMilliseconds = SafeInt(dictionary, "PvPPauseAllowMilliseconds", 3500);
		_PvPAllyMatchWaitLimitMilliseconds = SafeInt(dictionary, "PvPAllyMatchWaitLimitMilliseconds", 60000);
		_HarderHashCheck = SafeBool(dictionary, "HarderHashCheck", defaultValue: false);
		_UseGameServerStats = SafeBool(dictionary, "UseGameServerStats", defaultValue: false);
		_AndroidUsesDifferentFBAccount = SafeBool(dictionary, "AndroidFBAccount", defaultValue: false);
		_ThrottlePointsPerMessage = SafeFloat(dictionary, "ThrottlePPM", 0f);
		_ThrottleMultiplierPerMessage = SafeFloat(dictionary, "ThrottleMPM", 0f);
		_ThrottlePointDecay = SafeFloat(dictionary, "ThrottlePD", 0f);
		_ThrottleMultiplierDecay = SafeFloat(dictionary, "ThrottleMD", 0f);
		_ThrottleDupeMultiplier = SafeFloat(dictionary, "ThrottleDX", 0f);
		_StandardGachaMultiPullLimit = SafeInt(dictionary, "StandardGachaMultiPullLimit", 30);
		_PremiumGachaMultiPullLimit = SafeInt(dictionary, "PremiumGachaMultiPullLimit", 30);
		_GachaChatAnnounceRarity = SafeInt(dictionary, "GachaChatAnnounceRarity", 5);
		_GachaChatAnnounceCooldown = SafeFloat(dictionary, "GachaChatAnnounceCooldown", 120f);
		_ActiveCalendar = CalendarGiftDataManager.Instance.GetData(SafeString(dictionary, "ActiveCalendar", string.Empty));
		_RepeatingCalendar = CalendarGiftDataManager.Instance.GetData(SafeString(dictionary, "RepeatingCalendar", string.Empty));
		_GlobalSaleCooldownMinutes = SafeInt(dictionary, "GlobalSaleCooldownMinutes", 0);
		_CreaturesOnBoard = SafeInt(dictionary, "CreaturesOnBoard", 4);
		_CreaturesOnTeam = SafeInt(dictionary, "CreaturesOnTeam", 5);
		_AllowCreatureFusion = SafeBool(dictionary, "AllowCreatureFusion", defaultValue: false);
		_StandardGachaCost = SafeInt(dictionary, "StandardGachaCost", 100);
		_PremiumGachaCost = SafeInt(dictionary, "PremiumGachaCost", 500);
		_fbAgeGate = SafeInt(dictionary, "fbAgeGate", 13);
		_chatAgeGate = SafeInt(dictionary, "chatAgeGate", 18);
		_defaultTutCreature_Plains = SafeString(dictionary, "defaultTutCreature_Plains", "AncientScholar_Base");
		_defaultTutCreature_Corn = SafeString(dictionary, "defaultTutCreature_Corn", "HuskerKnight_Base");
		_defaultTutCreature_Nice = SafeString(dictionary, "defaultTutCreature_Nice", "Shepard_Base");
		_DailyRouletteLevelRequired = SafeInt(dictionary, "DailyRouletteLevelRequired", 200);
		_CardCraftingLevelRequired = SafeInt(dictionary, "CardCraftingLevelRequired", 200);
		_BuySoftCurrencyAmount = SafeInt(dictionary, "BuySoftCurrencyAmount", 1000);
		_BuySoftCurrencyCost = SafeInt(dictionary, "BuySoftCurrencyCost", 250);
		_BuyEvolutionShardsAmount = SafeInt(dictionary, "BuyEvolutionShardsAmount", 3);
		_BuyEvolutionShardsCost = SafeInt(dictionary, "BuyEvolutionShardsCost", 100);
		_BuyCraftCurrencyCost = SafeInt(dictionary, "BuyCraftCurrencyCost", 6500);
		_BuyCraftCurrencyAmount = SafeInt(dictionary, "BuyCraftCurrencyAmount", 1);
		_UnlockAllDailyDungeonsGemCost = SafeInt(dictionary, "UnlockAllDailyDungeonsGemCost", 100);
		_NotificationReminderHours = new List<int>();
		string text2 = SafeString(dictionary, "NotificationReminderHours", null);
		if (text2 != null)
		{
			array = text2.Split(',');
			for (int i = 0; i < array.Length; i++)
			{
				if (int.TryParse(array[i].Trim(), out var result))
				{
					_NotificationReminderHours.Add(result);
				}
			}
		}
		string text3 = SafeString(dictionary, "AwakenEventStartDate", null);
		string text4 = SafeString(dictionary, "AwakenEventEndDate", null);
		if (text3 != null && text4 != null && DateTime.TryParse(text3, out var result2) && DateTime.TryParse(text4, out var result3))
		{
			_AwakenEventStartDate = result2;
			_AwakenEventEndDate = result3;
			_AwakenEventBonus = SafeInt(dictionary, "AwakenEventBonus", 0);
		}
		else
		{
			_AwakenEventStartDate = DateTime.MinValue;
			_AwakenEventEndDate = DateTime.MinValue;
		}
		_CoinflipLoserGetsEnergyBonus = SafeBool(dictionary, "CoinflipLoserGetsEnergyBonus", defaultValue: false);
		_CoinflipLoserGetsAugmentCard = SafeBool(dictionary, "CoinflipLoserGetsAugmentCard", defaultValue: false);
		_CoinflipLoserGetsEquippedAugment = SafeBool(dictionary, "CoinflipLoserGetsEquippedAugment", defaultValue: false);
		_CoinflipLoserGetsExodiaAugment = SafeBool(dictionary, "CoinflipLoserGetsExodiaAugment", defaultValue: false);
		_ForceUserCoinflipLoser = SafeBool(dictionary, "ForceUserCoinflipLoser", defaultValue: false);
	}

	protected override void PostLoad()
	{
	}
}
