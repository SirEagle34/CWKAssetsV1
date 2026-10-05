using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using MiniJSON;
using UnityEngine;

public class SLOTResourceManager : Singleton<SLOTResourceManager>
{
	private class LoadAssetBundleQueueInfo
	{
		public string path;
		public Type t;
		public string assetBundleName;
		public Action<object> loadAssetBundleCallback;
		public bool async;
	}

	private class QueuedResourceLoad
	{
		public string AssetBundle;
		public string AssetPath;
		public Action<UnityEngine.Object> Callback;
		public bool PreloadOnly;
		public bool BackgroundLoad;
		public bool SilentFail;
		public bool UseHighResLowRes;
	}

	private class QueuedTextureLoad
	{
		public string AssetBundle;
		public string AssetPath;
		public UITexture TextureObject;
	}

	public enum PreloadBundlesPoint
	{
		IntroBattle,
		Q1,
		Q2
	}

	// ============================================================
	// DEFAULT BUNDLE NAMES
	// ============================================================

	public const string MainResourcesBundle = "MainResourcesBundle";

	// LOCAL ONLY.
	// NEVER DOWNLOAD THIS BUNDLE.
	public const string MainScenesBundle = "MainScenesBundle";

	public const string GeneralUIBundle = "GeneralBundle";
	public const string FtueUIBundle = "FTUEBundle";
	public const string FtueAudioBundle = "FTUEAudioBundle";
	public const string MainAudioBundle = "MainAudioBundle";

	// ============================================================
	// ACTIVE BUNDLE VERSIONS
	// ============================================================

	public string ActiveMainResourcesBundle =
		MainResourcesBundle;

	// Compatibility only.
	// NEVER downloaded.
	public string ActiveMainScenesBundle =
		MainScenesBundle;

	public string ActiveGeneralUIBundle =
		GeneralUIBundle;

	public string ActiveFtueUIBundle =
		FtueUIBundle;

	public string ActiveFtueAudioBundle =
		FtueAudioBundle;

	public string ActiveMainAudioBundle =
		MainAudioBundle;

	private const string LOCAL_TEXTURE_CACHE_DIRECTORY =
		"TextureCache";

	private const string ETAG_EXTENSION =
		".DragonWarsEtag";

	private const int MAX_RETRY_COUNT = 3;

	public const string CreaturePortraitPlaceholder =
		"UI/UI/LoadingPlaceholder";

	public const string CardArtPlaceholder =
		"UI/UI/LoadingPlaceholder";

	public const string LeagueBannerPlaceholder =
		"UI/UI/LoadingPlaceholder";

	public const string HeroPortraitPlaceholder =
		"UI/UI/LoadingPlaceholder";



	// ============================================================
	// CREATURE ASSET BUNDLES
	// ============================================================

	public List<string> CreatureAssetBundles = new List<string>
	{
		"03__card_wars_kingdom",
		"albinoeyebat",
		"alexplainer",
		"alexsander",
		"alexsander_evo",
		"ametistdan",
		"ancientscholar",
		"ancientscholar_evo",
		"angeleye",
		"angelofchocolate",
		"angelofdark",
		"angelofsand",
		"angelofsand_evo",
		"angelofsnow",
		"angelofvanilla",
		"applepieclops",
		"appletree",
		"archerdan",
		"archerdan_evo",
		"armourofbooks",
		"armourofbooks_evo",
		"autoplucker",
		"avalancheetah",
		"badrose",
		"baldingman",
		"baldingman_evo",
		"baldingmanhologram",
		"baldkingthrone",
		"baldmansthrone",
		"baldmansthrone_evo",
		"bananaslimey",
		"bansheprincess",
		"banshequeen",
		"batclops",
		"batclops_evo",
		"beachmum",
		"beachmum_evo",
		"bespectacledogre",
		"blackcat",
		"blackfisherfish",
		"blackpaladin",
		"blankeyedgirl",
		"blueberrypieclops",
		"bluecastle",
		"bluepartyogre",
		"blueplainsdrifter",
		"blueplainsdrifter_evo",
		"bogbansheangel",
		"bogbansheangel_evo",
		"bogbum",
		"bogfrogbomb",
		"bombguy",
		"botanicgolem",
		"braingooey",
		"bull",
		"bunny",
		"burnedcutie",
		"burnedffantry",
		"cactusball",
		"cactusball_evo",
		"cactusthug",
		"cactusthug_evo",
		"cameradude",
		"cameradude_1",
		"captaintaco",
		"caveofsolitude",
		"cedarcrystaldan",
		"cerberus.v2",
		"chadbear",
		"championnapalmer",
		"chargedbulb",
		"chestburster",
		"chestburster_evo",
		"coldsoldier",
		"cooldog",
		"cooldog_evo",
		"cornasaurusrex",
		"cornataur",
		"cornball",
		"cornball_evo",
		"corndog",
		"corndog_evo",
		"corneyebat",
		"corngolem",
		"cornhorn",
		"cornlord",
		"cornlord_evo",
		"cornronin",
		"cornwall",
		"cornwall_evo",
		"cornwall_evo1",
		"cornworm",
		"cottoncutie",
		"cottoncutie_evo.v2",
		"cottoneyebat",
		"cottonpult",
		"cottonsaurusrex",
		"cottonsaurusrex_evo",
		"cottonsnow",
		"cow",
		"cow_evo",
		"cowpurple",
		"cutiepie",
		"cutiepie_evo",
		"cyanparthenon",
		"darren",
		"davebones",
		"demonelf",
		"detectivebobby",
		"detectivesally",
		"devileye",
		"diamonddan",
		"dogboy",
		"dollyllama",
		"dollyllama_evo",
		"donkeykhan",
		"donkeykhan_evo",
		"dragonclaw",
		"dragonfoot",
		"drdeath",
		"drdeath_evo",
		"drghostbear",
		"drstuffenstein",
		"dustbunny",
		"dustbunny_evo",
		"earl",
		"earl_evo",
		"elderbrutewoad",
		"elfchief",
		"elfchief_evo",
		"elfmarauder",
		"embarrassingbard",
		"emeralddan",
		"enlightenedbaldman",
		"enlightenedbaldman_evo",
		"ethanallfire",
		"ethanallfire_evo",
		"eviltree",
		"extraordinaryspider",
		"extraordinaryspider_evo",
		"eyeguy",
		"farmerrecep",
		"farmertom",
		"fatgoat",
		"feedman",
		"feedman_evo",
		"fieldreaper",
		"fireballer",
		"fireballer_evo",
		"firewolf.v2",
		"fisherfish",
		"flameingowarrior",
		"flowercow",
		"fluffantry",
		"flufflapillar",
		"flufflapillar_evo",
		"flygem",
		"flyswatter",
		"freakdeer",
		"freezyj",
		"friedfriday",
		"fummy",
		"fummy_evo",
		"funguy",
		"funguy_evo",
		"furiouschick",
		"furioushen",
		"furiousrooster",
		"ghostbespectacledogre",
		"ghostbull",
		"ghostcottoncutie",
		"ghostEye",
		"ghosthag",
		"ghostjane",
		"ghostlycastle",
		"ghostlytree",
		"ghostninja",
		"ghostpig",
		"ghostrecordthug",
		"ghostrex",
		"ghostsnake",
		"ghoststalker",
		"ghostteenwolf",
		"ghostwindmill",
		"ghostwizardelf",
		"ghostwurst",
		"gnomeyellow",
		"goblinassassin",
		"goldencow",
		"goldendragonfoot",
		"goldeneyebat",
		"grainaryweevil",
		"grapeslimey",
		"grayeyebat",
		"greencow",
		"greenmermaid",
		"greenmermaid_evo",
		"greenmerman",
		"greenpartyogre",
		"greenpartyogre_1",
		"greenpartyogre_evo",
		"greenslimey",
		"greensnakey",
		"grim.v2",
		"hatebird",
		"headphonejerk",
		"heartangel",
		"heartangel_evo",
		"herculeye",
		"herculeye_evo",
		"hoteyebat",
		"hothead",
		"hotvolcano",
		"huskerdragon",
		"huskerdragon_evo",
		"huskerdragon_mix",
		"huskerdragon_white",
		"huskerdragon_yellow",
		"huskergiant",
		"huskergiant_evo",
		"huskerknight",
		"huskerknight_evo",
		"huskertanker",
		"huskertanker_evo",
		"huskervalkyrie",
		"icebaby",
		"icebull",
		"icedevil",
		"icepaladin",
		"icewarriorant",
		"icywall",
		"infinitefigure",
		"ironmaizen",
		"jalapenopizzadude",
		"junkking",
		"kingbaby",
		"kingfluff",
		"kingfluff_evo",
		"kingworm",
		"knifekite",
		"knifekite_evo",
		"knightofobesity",
		"knightofobesity_evo",
		"ladyscarab",
		"ladyscarab_evo",
		"legionofearlings",
		"legionofearlings_evo",
		"logknight",
		"lostgolem",
		"lostgolem_evo",
		"madameseota",
		"magmadog",
		"magmasaurusrex",
		"magmawall",
		"magneticu",
		"maisejane",
		"maisejane_evo",
		"maisewalker",
		"maisewalker_evo",
		"manowarelf",
		"manwitch",
		"manwitch_evo",
		"mausoleum",
		"motherfluffbucket",
		"mouthball",
		"mrfixit",
		"mrfixit_evo",
		"msmummy",
		"msmummy_evo",
		"mushroomlieutenant",
		"mushroompizzadude",
		"mushroomsoldier",
		"musicmallard",
		"mutanteyeguy",
		"narl",
		"nefertiti",
		"nefertiti_evo",
		"NiceBird",
		"nicetoilet",
		"nightbluerex",
		"nightbluerex_evo",
		"nightshinysharkkid",
		"ninja",
		"ninja_evo",
		"obsidianronin",
		"obsidianseahorse",
		"oilwall",
		"palaceofbone",
		"paladim",
		"papercuttiger",
		"patchythepumpkin",
		"peasoupbarfer",
		"petemoss",
		"petemoss_evo",
		"phyllis",
		"pickler",
		"pieclops",
		"pig",
		"pig_evo",
		"pileofcats",
		"pileofcats_evo",
		"piratebear",
		"piratebear_evo",
		"pizzadude",
		"pizzadudeanchovie",
		"plainjane",
		"plainjane_evo",
		"plainsbarfer",
		"plainsdollmonster",
		"plainswalker",
		"poppacorn",
		"poppacorn_evo",
		"porcelainguardian",
		"poultergeist",
		"preteenwolf",
		"preteenwolf_evo",
		"puffycastle",
		"punkcat",
		"punkcat_evo",
		"pyramidthekid",
		"pyramidthekid_evo",
		"Quadurai",
		"ra",
		"ra_evo",
		"rainbowbarfer",
		"rainboweyebat",
		"recordthug",
		"redhorn",
		"redparthenon",
		"robotscarab",
		"rosesoupbarfer",
		"rubydan",
		"ruralearl",
		"sackgeist",
		"sackofpain",
		"sakuraronin",
		"sandasaurusrex",
		"sandasaurusrex_evo",
		"sandeyebat",
		"sandfoot",
		"sandhorn",
		"sandicore",
		"sandjackal",
		"sandknight",
		"sandman",
		"sandman_evo",
		"sandmischievousrextriplets",
		"sandpyramid",
		"sandronin",
		"sandshark",
		"sandsnake",
		"sandsnow",
		"sandwishmonday",
		"sandwishmonday_evo",
		"sandwitch",
		"sandwitch_evo",
		"sciencebrianandhisbrain",
		"sciencebrianandhisbrain_evo",
		"seahorse",
		"sewerstuart",
		"sewerstuart_evo",
		"shamanwoad",
		"shamanwoad_evo",
		"shark",
		"sharkkid",
		"shepard",
		"shepard_evo",
		"shieldmaizen",
		"shieldmaizen_evo",
		"shinnytree",
		"silooftruth",
		"skeletalhand",
		"sludger",
		"sludger_evo",
		"smothergoose",
		"smothergoose_evo",
		"snakeball",
		"snakemint",
		"snakemint_evo",
		"snowball",
		"snowbull",
		"snowdog",
		"snoweyebat",
		"snowpieclops",
		"snowplains",
		"snowsaurusrex",
		"snowsludger",
		"snowycooldog",
		"snowymcsnow",
		"snuggletree",
		"softeyeling",
		"softrock",
		"softrock_evo",
		"spirittower",
		"stalker",
		"stalker_evo",
		"stalkeyesnek",
		"steakchop",
		"stonegolem",
		"stonehenge",
		"struzanjinn",
		"struzanrobot",
		"sunking",
		"sunpyramid",
		"swamphorn",
		"swampsaurusrex",
		"swampsnow",
		"swampytire",
		"swampytire_evo",
		"teethleaf",
		"thebarrel",
		"thecooper",
		"thecooper_evo",
		"thedrooler",
		"thedrooler_evo",
		"theghost",
		"themariachi",
		"theuglytree",
		"thirstythursday",
		"toddlersscholar",
		"tomezooka",
		"tomezooka_evo",
		"tvdude",
		"unicycleknight",
		"unicyclewizard",
		"unicyclops",
		"vampbaldingman",
		"vampbaldingman_evo",
		"vampcactus",
		"vampcorn",
		"vampghost",
		"vampmarshmallow",
		"wallofchocolate",
		"wallofhoney",
		"wallofsand",
		"wallofsand_evo",
		"wallofslime",
		"wallofstrawberry",
		"wallofvanilla",
		"welldressedwolf",
		"whitecat",
		"whiteninja",
		"whiteninja_evo",
		"wintercow",
		"wizardelf",
		"wizardofpoo",
		"wurstwednesday",
		"yellowmermaid",
		"zebracorn",
		"zebrasaurusrex",
		"zombiepieclops",
		"zombierex_evo"
	};


	// ============================================================
	// HERO / CHARACTER ASSET BUNDLES
	// ============================================================

	public List<string> HeroAssetBundles = new List<string>
	{
		"02__bosses",
		"ash",
		"bananaguard",
		"bananaguard_hal",
		"bananaguard_zombie",
		"blueflameprincess",
		"bmo",
		"bmo_beta",
		"bmo_hal",
		"bmo_sweater",
		"charlie",
		"cinnamonbun",
		"cinnamonbun_zombie",
		"cosmicowl",
		"druid",
		"earloflemongrab",
		"earloflemongrab2",
		"earloflemongrab_hal",
		"finn",
		"finn_hal",
		"finn_maxpwr",
		"finn_maxpwr_hal",
		"finn_maxpwr_pjama",
		"finn_maxpwr_summer",
		"finn_maxpwr_sweater",
		"finn_pjama",
		"finn_pwr",
		"finn_pwr_hal",
		"finn_pwr_pjama",
		"finn_pwr_summer",
		"finn_pwr_sweater",
		"finn_summer",
		"finn_sweater",
		"finndavey",
		"finndavey_maxpwr",
		"finndavey_pwr",
		"finndoctor",
		"finndoctor_maxpwr",
		"finndoctor_pwr",
		"flameking",
		"flameprincess",
		"flameprincess_hal",
		"grandprixe",
		"grandprixe_hal",
		"gunter",
		"hunsonabadeer",
		"huntresswizard",
		"iceking",
		"iceking_datenight",
		"iceking_hal",
		"iceking_maxpwr",
		"iceking_pjama",
		"iceking_pwr",
		"iceking_sweater",
		"jake",
		"jake_hal",
		"jake_maxpwr_hal",
		"jake_maxpwr_summer",
		"jake_maxpwr_sweater",
		"jake_maxpwr_zombie",
		"jake_pwr",
		"jake_pwr_hal",
		"jake_pwr_summer",
		"jake_pwr_sweater",
		"jake_pwr_zombie",
		"jake_summer",
		"jake_sweater",
		"jake_zombie",
		"jakeking",
		"jakeking_maxpwr",
		"jakeking_pwr",
		"labprincessbubblegum",
		"ladyrainicorn",
		"lich",
		"lumpyspaceprincess",
		"lumpyspaceprincess_hal",
		"lumpyspaceprincess_masquerade",
		"lumpyspaceprincess_sweater",
		"magicman",
		"marceline",
		"marceline_hal",
		"marceline_maxpwr",
		"marceline_pwr",
		"marceline_stakes",
		"marshalllee",
		"maxpoweredjk",
		"mrcupcake",
		"partygod",
		"peppermintbutler",
		"peppermintbutler_hal",
		"peppermintbutler_maxpwr",
		"peppermintbutler_maxpwr_hal",
		"peppermintbutler_maxpwr_mintcharisma",
		"peppermintbutler_maxpwr_wizardcity",
		"peppermintbutler_maxpwr_zombie",
		"peppermintbutler_mintcharisma",
		"peppermintbutler_pwr",
		"peppermintbutler_pwr_hal",
		"peppermintbutler_pwr_mintcharisma",
		"peppermintbutler_pwr_wizardcity",
		"peppermintbutler_pwr_zombie",
		"peppermintbutler_wizardcity",
		"peppermintbutler_zombie",
		"poweredjk",
		"primefinn",
		"princessbreakfast",
		"princessbubblegum",
		"princessbubblegum_hal",
		"princessbubblegum_sweater",
		"princessbubblegum_zombie",
		"prismo",
		"treasurecat",
		"younggrandprixe"
	};


	public List<string> ExtraAssetBundlesPack = new List<string>
	{
		"extra000001"
	};

	// ============================================================
	// SYSTEM / GENERAL BUNDLES
	// ============================================================

	public List<string> SystemAssetBundles = new List<string>
	{
		"ftuebundle",
		"generalbundle",
		"mainaudiobundle",
		"mainresourcesbundle"
	};


	// ============================================================
	// GAMEBOARD / ENVIRONMENT BUNDLES
	// ============================================================

	public List<string> EnvironmentAssetBundles = new List<string>
	{
		"gameboard_treefort",
		"inn_bg",

		// gerçek environment/gameboard bundle'ları buraya
	};

	

	public string resourcePathPrefix_hires;
	public string resourcePathPrefix_lores;

	public List<UIAtlas> UiAtlases;

	public UIAtlas lowResAtlas;

	private List<AssetBundle> assetBundles =
		new List<AssetBundle>();

	private static AssetBundle mResourcesBundle;
	private static AssetBundle mScenesBundle;

	private List<LoadAssetBundleQueueInfo>
		loadAssetBundleQueue =
			new List<LoadAssetBundleQueueInfo>();

	private List<QueuedResourceLoad>
		mQueuedResourceLoads =
			new List<QueuedResourceLoad>();

	private Coroutine mQueuedResourceLoadCoroutine;

	private int mBundleIndex;
	private int mTotalBundles = -1;

	// Keeps startup downloads in one continuous progress session.
	private bool mKeepResourceLoadProgressAlive;

	private WWW mInProgressWWW;

	private bool mCurrentlyBackgroundLoading;

	private List<QueuedTextureLoad>
		mQueuedTextureLoads =
			new List<QueuedTextureLoad>();

	private Coroutine mQueuedTextureLoadCoroutine;

	// ============================================================
	// PRIMARY BUNDLE FAILURE STATE
	// ============================================================

	private bool mPrimaryBundlesLoadFailed;

	public bool PrimaryBundlesLoadFailed
	{
		get
		{
			return mPrimaryBundlesLoadFailed;
		}
	}

	// ============================================================
	// URL / RESOURCE SETTINGS
	// ============================================================

	public string assetBundleBaseURL { get; set; }

	public string resourcePathPrefix { get; set; }

	public bool ResourceLoadInProgress
	{
		get
		{
			return mQueuedResourceLoadCoroutine != null;
		}
	}

	// ============================================================
	// BUNDLE NAME NORMALIZATION
	// ============================================================

	private string NormalizeBundleName(
		string bundleName)
	{
		if (string.IsNullOrEmpty(bundleName))
		{
			return string.Empty;
		}

		return bundleName
			.Replace("\\", "/")
			.Trim('/')
			.ToLowerInvariant();
	}

	// ============================================================
	// LOCAL MAIN SCENES BUNDLE
	// ============================================================

	public bool IsMainScenesBundle(
		string bundleName)
	{
		if (string.IsNullOrEmpty(bundleName))
		{
			return false;
		}

		return NormalizeBundleName(bundleName)
			== "mainscenesbundle";
	}

	public bool IsLocalBundle(
		string bundleName)
	{
		return IsMainScenesBundle(bundleName);
	}

	// ============================================================
	// ACTIVE BUNDLE HELPERS
	// ============================================================

	private string GetActiveBundleName(
		string activeBundleName,
		string fallback)
	{
		if (string.IsNullOrEmpty(activeBundleName))
		{
			activeBundleName = fallback;
		}

		return NormalizeBundleName(activeBundleName);
	}

	public string GetMainResourcesBundleName()
	{
		return GetActiveBundleName(
			ActiveMainResourcesBundle,
			MainResourcesBundle
		);
	}

	public string GetMainScenesBundleName()
	{
		return NormalizeBundleName(
			MainScenesBundle
		);
	}

	public string GetGeneralUIBundleName()
	{
		return GetActiveBundleName(
			ActiveGeneralUIBundle,
			GeneralUIBundle
		);
	}

	public string GetFtueUIBundleName()
	{
		return GetActiveBundleName(
			ActiveFtueUIBundle,
			FtueUIBundle
		);
	}

	public string GetFtueAudioBundleName()
	{
		return GetActiveBundleName(
			ActiveFtueAudioBundle,
			FtueAudioBundle
		);
	}

	public string GetMainAudioBundleName()
	{
		return GetActiveBundleName(
			ActiveMainAudioBundle,
			MainAudioBundle
		);
	}

	// ============================================================
	// DOWNLOAD STATISTICS
	// ============================================================

	public string CurrentDownloadBundle
	{
		get
		{
			if (
				Singleton<KFFAssetBundleManager>.Instance ==
				null)
			{
				return string.Empty;
			}

			return
				Singleton<KFFAssetBundleManager>.Instance
					.ActiveBundleName;
		}
	}

	public long CurrentDownloadBytes
	{
		get
		{
			if (
				Singleton<KFFAssetBundleManager>.Instance ==
				null)
			{
				return 0;
			}

			return
				Singleton<KFFAssetBundleManager>.Instance
					.ActiveFileBytes;
		}
	}

	public long CurrentDownloadTotalBytes
	{
		get
		{
			if (
				Singleton<KFFAssetBundleManager>.Instance ==
				null)
			{
				return 0;
			}

			return
				Singleton<KFFAssetBundleManager>.Instance
					.ActiveFileTotalBytes;
		}
	}

	public long TotalDownloadedBytes
	{
		get
		{
			if (
				Singleton<KFFAssetBundleManager>.Instance ==
				null)
			{
				return 0;
			}

			return
				Singleton<KFFAssetBundleManager>.Instance
					.TotalDownloadedBytes;
		}
	}

	public long TotalDownloadBytes
	{
		get
		{
			if (
				Singleton<KFFAssetBundleManager>.Instance ==
				null)
			{
				return 0;
			}

			return
				Singleton<KFFAssetBundleManager>.Instance
					.TotalDownloadBytes;
		}
	}

	public float DownloadSpeed
	{
		get
		{
			if (
				Singleton<KFFAssetBundleManager>.Instance ==
				null)
			{
				return 0f;
			}

			return
				Singleton<KFFAssetBundleManager>.Instance
					.DownloadSpeed;
		}
	}

	// ============================================================
	// START
	// ============================================================

	private void Start()
	{
		GetBaseURLs();

		resourcePathPrefix =
			(!KFFLODManager.IsLowEndDevice())
				? resourcePathPrefix_hires
				: resourcePathPrefix_lores;

		Debug.Log(
			"[SLOTResourceManager] Device Resolution Mode: " +
			(
				KFFLODManager.IsLowEndDevice()
					? "LOW"
					: "HIGH"
			)
		);

		Debug.Log(
			"[SLOTResourceManager] Resource Path Prefix: " +
			resourcePathPrefix
		);

		Debug.Log(
			"[SLOTResourceManager] MainScenesBundle is LOCAL. " +
			"No server download will be attempted."
		);
	}

	// ============================================================
	// BASE URL
	// ============================================================

	private void GetBaseURLs()
	{
		string empty = string.Empty;

		string streamingAssetsFile =
			TFUtils.GetStreamingAssetsFile(
				"asset_bundle_settings.json"
			);

		empty =
			(!streamingAssetsFile.Contains("://"))
				? File.ReadAllText(
					streamingAssetsFile
				)
				: SQSettings.getJsonPath(
					streamingAssetsFile
				);

		Dictionary<string, object> dictionary =
			(Dictionary<string, object>)
			Json.Deserialize(empty);

#if UNITY_ANDROID

		assetBundleBaseURL =
			(string)
			dictionary["asset_bundle_url_apk"];

#elif UNITY_IOS

		assetBundleBaseURL =
			(string)
			dictionary["asset_bundle_url_ios"];

#elif UNITY_STANDALONE_WIN

		assetBundleBaseURL =
			(string)
			dictionary["asset_bundle_url_zip"];

#else

		assetBundleBaseURL =
			(string)
			dictionary["asset_bundle_url_apk"];

#endif

		if (!string.IsNullOrEmpty(
			assetBundleBaseURL))
		{
			assetBundleBaseURL =
				assetBundleBaseURL
					.TrimEnd('/') +
				"/";
		}

		Debug.Log(
			"[SLOTResourceManager] AssetBundle Base URL: " +
			assetBundleBaseURL
		);
	}

	// ============================================================
	// RESOURCE NAME
	// ============================================================

	public static string GetResourceName(
		string name)
	{
		return GetResourceName(
			name,
			KFFLODManager.IsLowEndDevice()
		);
	}

	public static string GetResourceName(
		string name,
		bool lowRes)
	{
		if (lowRes)
		{
			if (name.Contains("low_"))
			{
				return name;
			}

			int num =
				name.LastIndexOf("/");

			if (num >= 0)
			{
				string text =
					name.Substring(
						0,
						num
					);

				string text2 =
					name.Substring(
						num + 1
					);

				return
					text +
					"/low_" +
					text2;
			}

			return
				"low_" +
				name;
		}

		return name;
	}

	public bool IsHiLoRezResource(
		string path)
	{
		if (path.Contains("Flags/"))
		{
			return false;
		}

		return true;
	}

	// ============================================================
	// SYNCHRONOUS RESOURCE LOAD
	// ============================================================

	public UnityEngine.Object LoadResource(
		string path,
		Type t = null)
	{
		if (string.IsNullOrEmpty(path))
		{
			return null;
		}

		path =
			path.Replace(
				"low_",
				string.Empty
			);

		path =
			path.Replace(
				"\\",
				"/"
			);

		if (
			IsUsingAssetBundles() &&
			mResourcesBundle != null)
		{
			string resourcePath =
				path.Trim('/');

			if (IsHiLoRezResource(path))
			{
				resourcePath =
					GetResourceName(
						resourcePath
					);
			}

			UnityEngine.Object loadedObject =
				(t == null)
					? mResourcesBundle.LoadAsset(
						resourcePath
					)
					: mResourcesBundle.LoadAsset(
						resourcePath,
						t
					);

			if (loadedObject != null)
			{
				return loadedObject;
			}
		}

		if (
			KFFLODManager.IsLowEndDevice() &&
			IsHiLoRezResource(path))
		{
			string resourceName =
				GetResourceName(path);

			UnityEngine.Object loadedObject =
				(t == null)
					? Resources.Load(
						resourceName
					)
					: Resources.Load(
						resourceName,
						t
					);

			if (loadedObject != null)
			{
				return loadedObject;
			}
		}

		if (t != null)
		{
			return Resources.Load(
				path,
				t
			);
		}

		return Resources.Load(path);
	}

	// ============================================================
	// QUEUED RESOURCE LOAD
	// ============================================================

	public void QueueResourceLoad(
		string path,
		string assetBundleName,
		Action<UnityEngine.Object>
			loadResourceCallback,
		bool isPreload = false,
		bool silentFail = false,
		bool useHighRes = true,
		bool backgroundLoad = false)
	{
		QueuedResourceLoad
			queuedResourceLoad =
				new QueuedResourceLoad();

		queuedResourceLoad.AssetPath =
			path;

		// Legacy Resources.Load callers often do not provide a bundle.
		// Resolve the bundle centrally so every caller uses the same
		// Resources -> AssetBundle mapping.
		queuedResourceLoad.AssetBundle =
			ResolveResourceBundleName(
				path,
				assetBundleName
			);

		queuedResourceLoad.Callback =
			loadResourceCallback;

		queuedResourceLoad.PreloadOnly =
			isPreload;

		if (isPreload)
		{
			queuedResourceLoad.BackgroundLoad =
				true;
		}
		else
		{
			queuedResourceLoad.BackgroundLoad =
				backgroundLoad;
		}

		queuedResourceLoad.SilentFail =
			silentFail;

		queuedResourceLoad.UseHighResLowRes =
			useHighRes;

		mQueuedResourceLoads.Add(
			queuedResourceLoad
		);

		if (
			mQueuedResourceLoadCoroutine ==
			null)
		{
			mQueuedResourceLoadCoroutine =
				StartCoroutine(
					LoadResourceCoroutine()
				);
		}
	}

	// ============================================================
	// RESOURCE -> ASSETBUNDLE RESOLVER
	// ============================================================

	private string ResolveResourceBundleName(
		string assetPath,
		string assetBundleName)
	{
		string normalizedBundle =
			NormalizeBundleName(assetBundleName);

		// An explicit bundle always wins. Creature, character,
		// environment, gameboard and audio callers already know
		// which dedicated bundle they need.
		if (!string.IsNullOrEmpty(normalizedBundle))
		{
			return normalizedBundle;
		}

		string normalizedPath =
			(assetPath ?? string.Empty)
				.Replace("\\\\", "/")
				.Trim('/');

		if (normalizedPath.StartsWith(
			"Assets/Resources/",
			StringComparison.OrdinalIgnoreCase))
		{
			normalizedPath =
				normalizedPath.Substring(
					"Assets/Resources/".Length
				);
		}
		else if (normalizedPath.StartsWith(
			"Resources/",
			StringComparison.OrdinalIgnoreCase))
		{
			normalizedPath =
				normalizedPath.Substring(
					"Resources/".Length
				);
		}

		string lowerPath =
			normalizedPath.ToLowerInvariant();

		// If an old caller omitted the bundle for an asset that was
		// moved to its own bundle, infer that bundle from the legacy
		// Resources path.
		string[] dedicatedRoots =
		{
			"creatures/",
			"characters/",
			"environment/",
			"gameboard/"
		};

		for (int i = 0; i < dedicatedRoots.Length; i++)
		{
			string root = dedicatedRoots[i];

			if (!lowerPath.StartsWith(
				root,
				StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}

			int start = root.Length;
			int end = lowerPath.IndexOf('/', start);

			if (end > start)
			{
				return NormalizeBundleName(
					lowerPath.Substring(
						start,
						end - start
					)
				);
			}

			break;
		}

		// MainResourcesBundle contains the migrated Resources hierarchy:
		// Atlases, Banners, UI, Textures, Materials, VFX, etc.
		// Therefore every remaining legacy Resources path belongs here.
		return GetMainResourcesBundleName();
	}

	// ============================================================
	// ASSETBUNDLE ASSET PATH RESOLVER
	// ============================================================

	private string ResolveAssetBundlePath(
		AssetBundle assetBundle,
		string requestedPath)
	{
		if (assetBundle == null ||
			string.IsNullOrEmpty(requestedPath))
		{
			return requestedPath;
		}

		string normalizedRequested =
			requestedPath
				.Replace("\\", "/")
				.Trim('/')
				.ToLowerInvariant();

		string requestedFileName =
			Path.GetFileNameWithoutExtension(
				normalizedRequested
			);

		string[] assetNames =
			assetBundle.GetAllAssetNames();

		// 1. Exact AssetBundle path.
		for (int i = 0; i < assetNames.Length; i++)
		{
			string candidate =
				assetNames[i]
					.Replace("\\", "/")
					.Trim('/')
					.ToLowerInvariant();

			if (candidate == normalizedRequested)
			{
				return assetNames[i];
			}
		}

		// 2. Same path, ignoring the file extension.
		for (int i = 0; i < assetNames.Length; i++)
		{
			string candidate =
				assetNames[i]
					.Replace("\\", "/")
					.Trim('/')
					.ToLowerInvariant();

			string candidateWithoutExtension =
				Path.ChangeExtension(
					candidate,
					null
				);

			if (candidateWithoutExtension ==
				normalizedRequested)
			{
				return assetNames[i];
			}
		}

		// 3. Match by suffix. This is safer than filename-only
		// matching when two folders contain the same prefab name.
		string suffix =
			"/" + normalizedRequested;

		for (int i = 0; i < assetNames.Length; i++)
		{
			string candidate =
				assetNames[i]
					.Replace("\\", "/")
					.Trim('/')
					.ToLowerInvariant();

			if (candidate.EndsWith(
				suffix,
				StringComparison.OrdinalIgnoreCase))
			{
				return assetNames[i];
			}
		}

		// 4. Final fallback: match the actual asset filename.
		for (int i = 0; i < assetNames.Length; i++)
		{
			string candidate =
				assetNames[i]
					.Replace("\\", "/")
					.Trim('/')
					.ToLowerInvariant();

			string candidateFileName =
				Path.GetFileNameWithoutExtension(
					candidate
				);

			if (candidateFileName ==
				requestedFileName)
			{
				return assetNames[i];
			}
		}

		return requestedPath;
	}

	// ============================================================
	// QUEUED RESOURCE COROUTINE
	// ============================================================

	private IEnumerator LoadResourceCoroutine()
	{
		while (
			mQueuedResourceLoads.Count > 0)
		{
			QueuedResourceLoad queuedLoad =
				mQueuedResourceLoads[0];

			mQueuedResourceLoads.RemoveAt(0);

			mCurrentlyBackgroundLoading =
				queuedLoad.BackgroundLoad;

			if (IsUsingAssetBundles())
			{
				queuedLoad.AssetBundle =
					ResolveResourceBundleName(
						queuedLoad.AssetPath,
						queuedLoad.AssetBundle
					);

				if (
					string.IsNullOrEmpty(
						queuedLoad.AssetBundle))
				{
					Debug.LogError(
						"[SLOTResourceManager] " +
						"AssetBundle name is null or empty. " +
						"Asset: " +
						queuedLoad.AssetPath
					);

					if (
						queuedLoad.Callback !=
						null)
					{
						queuedLoad.Callback(
							null
						);
					}

					continue;
				}

				queuedLoad.AssetBundle =
					NormalizeBundleName(
						queuedLoad.AssetBundle
					);

				// =================================================
				// LOCAL MAIN SCENES
				// =================================================

				if (
					IsMainScenesBundle(
						queuedLoad.AssetBundle))
				{
					Debug.Log(
						"[SLOTResourceManager] " +
						"MainScenesBundle requested. " +
						"It is LOCAL. No download."
					);

					if (
						queuedLoad.Callback !=
						null)
					{
						queuedLoad.Callback(
							null
						);
					}

					continue;
				}

				AssetBundle assetBundle =
					null;

				// =================================================
				// PRELOAD ONLY
				// =================================================

				if (queuedLoad.PreloadOnly)
				{
					assetBundle =
						Singleton<
							KFFAssetBundleManager
						>.Instance
							.GetAssetBundleByName(
								queuedLoad.AssetBundle
							);
				}
				else
				{
					/*
					 * IMPORTANT:
					 *
					 * Do NOT turn creature prefab paths into
					 * Assets/Resources/... here.
					 *
					 * Bundle asset path must remain:
					 *
					 * ArcherDan/archerdan.prefab
					 */

					if (
						queuedLoad.UseHighResLowRes)
					{
						queuedLoad.AssetPath =
							GetResourceName(
								queuedLoad.AssetPath
							);
					}

					assetBundle =
						Singleton<
							KFFAssetBundleManager
						>.Instance
							.GetAssetBundleForResource(
								queuedLoad.AssetPath
							);
				}

				// =================================================
				// BUNDLE NOT LOADED
				// =================================================

				if (assetBundle == null)
				{
					int retryCount = 0;

					bool cancelled = false;

					while (
						assetBundle == null &&
						retryCount <
							MAX_RETRY_COUNT)
					{
						retryCount++;

						bool wasSuccess =
							false;

						string errorMessage =
							null;

						Debug.Log(
							"[SLOTResourceManager] " +
							"Loading AssetBundle attempt " +
							retryCount +
							"/" +
							MAX_RETRY_COUNT +
							": " +
							queuedLoad.AssetBundle
						);

						yield return
							StartCoroutine(
								Singleton<
									KFFAssetBundleManager
								>.Instance
								.LoadAssetBundleCoroutine(
									assetBundleBaseURL,
									queuedLoad.AssetBundle,
									delegate(
										bool success,
										string errMsg,
										AssetBundle loadedBundle)
									{
										wasSuccess =
											success;

										errorMessage =
											errMsg;

										if (
											success &&
											loadedBundle !=
												null)
										{
											assetBundle =
												loadedBundle;
										}
										else
										{
											assetBundle =
												null;
										}
									}
								)
							);

						if (
							wasSuccess &&
							assetBundle != null)
						{
							Debug.Log(
								"[SLOTResourceManager] " +
								"AssetBundle loaded successfully: " +
								queuedLoad.AssetBundle
							);

							break;
						}

						Debug.LogError(
							"[SLOTResourceManager] " +
							"AssetBundle load failed. " +
							"Attempt: " +
							retryCount +
							"/" +
							MAX_RETRY_COUNT +
							" Bundle: " +
							queuedLoad.AssetBundle +
							" Error: " +
							errorMessage
						);

						if (
							retryCount >=
							MAX_RETRY_COUNT)
						{
							Debug.LogError(
								"[SLOTResourceManager] " +
								"Giving up after " +
								MAX_RETRY_COUNT +
								" attempts: " +
								queuedLoad.AssetBundle
							);

							cancelled = true;

							break;
						}

						if (
							ProgressHiddenDuringPreload())
						{
							continue;
						}

						if (
							queuedLoad.SilentFail)
						{
							cancelled = true;

							break;
						}

						int selection = -1;

						Singleton<
							SimplePopupController
						>.Instance
							.ShowMessage(
								string.Empty,
								KFFLocalization.Get(
									"!!ERROR_LOADING_ASSET_BODY"
								),
								delegate
								{
									selection = 1;
								},
								KFFLocalization.Get(
									"!!RETRY"
								)
							);

						while (
							selection == -1)
						{
							yield return null;
						}

						if (selection == 0)
						{
							cancelled = true;
							break;
						}
					}

					if (
						cancelled ||
						assetBundle == null)
					{
						Debug.LogError(
							"[SLOTResourceManager] " +
							"Resource load aborted: " +
							queuedLoad.AssetPath +
							" | Bundle: " +
							queuedLoad.AssetBundle
						);

						if (
							queuedLoad.Callback !=
							null)
						{
							queuedLoad.Callback(
								null
							);
						}

						continue;
					}
				}

				// =================================================
				// PRELOAD ONLY
				// =================================================

				if (queuedLoad.PreloadOnly)
				{
					Debug.Log(
						"[SLOTResourceManager] " +
						"Preload completed: " +
						queuedLoad.AssetBundle
					);

					if (
						queuedLoad.Callback !=
						null)
					{
						queuedLoad.Callback(
							null
						);
					}

					continue;
				}

				// =================================================
				// LOAD ASSET FROM BUNDLE
				// =================================================

				string finalAssetPath =
					queuedLoad.AssetPath;

				// AssetBundle paths do not include the Resources category
				// directory. Convert the old resource path to the actual
				// path stored inside the bundle.
				if (
					finalAssetPath.StartsWith(
						"Characters/",
						StringComparison.OrdinalIgnoreCase) ||
					finalAssetPath.StartsWith(
						"Creatures/",
						StringComparison.OrdinalIgnoreCase) ||
					finalAssetPath.StartsWith(
						"Environment/",
						StringComparison.OrdinalIgnoreCase))
				{
					int slashIndex =
						finalAssetPath.IndexOf('/');

					if (slashIndex >= 0 &&
						slashIndex + 1 < finalAssetPath.Length)
					{
						finalAssetPath =
							finalAssetPath.Substring(
								slashIndex + 1
							);
					}
				}

				if (
					!finalAssetPath.EndsWith(
						".prefab",
						StringComparison.OrdinalIgnoreCase) &&
					!finalAssetPath.EndsWith(
						".png",
						StringComparison.OrdinalIgnoreCase) &&
					!finalAssetPath.EndsWith(
						".jpg",
						StringComparison.OrdinalIgnoreCase) &&
					!finalAssetPath.EndsWith(
						".jpeg",
						StringComparison.OrdinalIgnoreCase) &&
					!finalAssetPath.EndsWith(
						".wav",
						StringComparison.OrdinalIgnoreCase) &&
					!finalAssetPath.EndsWith(
						".mp3",
						StringComparison.OrdinalIgnoreCase))
				{
					string resourceExtension =
						Singleton<
							KFFAssetBundleManager
						>.Instance
							.GetResourceExtension(
								finalAssetPath
							);

					if (!string.IsNullOrEmpty(resourceExtension))
					{
						finalAssetPath += resourceExtension;
					}
					else if (
						finalAssetPath.StartsWith(
							"GameBoard/",
							StringComparison.OrdinalIgnoreCase))
					{
						finalAssetPath += ".prefab";
					}
				}

				// -------------------------------------------------
				// DO NOT USE FixPath HERE.
				//
				// AssetBundle path must be:
				//
				// ArcherDan/archerdan.prefab
				//
				// NOT:
				//
				// Assets/Resources/Creatures/...
				// -------------------------------------------------

				finalAssetPath =
					finalAssetPath.Replace(
						"\\",
						"/"
					);

				finalAssetPath =
					finalAssetPath.Trim('/');

				string resolvedAssetPath =
					ResolveAssetBundlePath(
						assetBundle,
						finalAssetPath
					);

				Debug.Log(
					"[SLOTResourceManager] Asset resolve: " +
					finalAssetPath +
					" -> " +
					resolvedAssetPath +
					" | Bundle: " +
					assetBundle.name
				);

				AssetBundleRequest
					bundleRequest =
						assetBundle
							.LoadAssetAsync(
								resolvedAssetPath
							);

				UnityEngine.Object result2 =
					null;

				if (
					bundleRequest !=
					null)
				{
					yield return bundleRequest;

					result2 =
						bundleRequest.asset;
				}

				if (result2 == null)
				{
					Debug.LogError(
						"[SLOTResourceManager] " +
						"Asset not found in bundle: " +
						finalAssetPath +
						" | Resolved: " +
						resolvedAssetPath +
						" | Bundle: " +
						queuedLoad.AssetBundle
					);
				}
				else
				{
					Debug.Log(
						"[SLOTResourceManager] " +
						"Asset loaded: " +
						resolvedAssetPath +
						" | Bundle: " +
						queuedLoad.AssetBundle
				}

				if (
					queuedLoad.Callback !=
					null)
				{
					queuedLoad.Callback(
						result2
					);
				}
			}
			else if (
				queuedLoad.PreloadOnly)
			{
				if (
					queuedLoad.Callback !=
					null)
				{
					queuedLoad.Callback(
						null
					);
				}
			}
			else
			{
				string aPath =
					queuedLoad.AssetPath;

				if (
					queuedLoad.UseHighResLowRes)
				{
					aPath =
						GetResourceName(
							queuedLoad.AssetPath
						);
				}

				ResourceRequest
					resourceRequest =
						Resources.LoadAsync(
							aPath
						);

				UnityEngine.Object result =
					null;

				if (
					resourceRequest !=
					null)
				{
					yield return resourceRequest;

					result =
						resourceRequest.asset;
				}

				if (
					queuedLoad.Callback !=
					null)
				{
					queuedLoad.Callback(
						result
					);
				}
			}
		}

		mQueuedResourceLoadCoroutine =
			null;

		mCurrentlyBackgroundLoading =
			false;
	}

	// ============================================================
	// ASSET BUNDLES
	// ============================================================

	public bool IsUsingAssetBundles()
	{
		return true;
	}

	// Kept for compatibility with old code.
	// IMPORTANT:
	// This is NOT used for AssetBundle asset loading.
	private string FixPath(
		string path,
		bool useHighResLowRes = true)
	{
		if (string.IsNullOrEmpty(path))
		{
			return path;
		}

		path =
			path.Replace(
				"\\",
				"/"
			);

		if (
			path.StartsWith(
				"Assets/Resources/",
				StringComparison.OrdinalIgnoreCase))
		{
			return path;
		}

		if (!path.StartsWith("Assets/"))
		{
			return
				"Assets/Resources/" +
				path;
		}

		return path;
	}

	public static UnityEngine.Object GetAsset(
		AsyncOperation a)
	{
		ResourceRequest resourceRequest =
			a as ResourceRequest;

		if (resourceRequest != null)
		{
			return resourceRequest.asset;
		}

		AssetBundleRequest
			assetBundleRequest =
				a as AssetBundleRequest;

		if (assetBundleRequest != null)
		{
			return assetBundleRequest.asset;
		}

		return null;
	}

	// ============================================================
	// RESOURCE LOAD PROGRESS
	// ============================================================

	public void StartResourceLoadProgress(
		int totalBundles)
	{
		mBundleIndex = 0;

		mTotalBundles =
			totalBundles;

		mKeepResourceLoadProgressAlive = false;
	}

	private void AppendResourceLoadProgress(
		int additionalBundles)
	{
		if (additionalBundles <= 0)
		{
			return;
		}

		if (mTotalBundles <= 0)
		{
			mBundleIndex = 0;
			mTotalBundles = additionalBundles;
		}
		else
		{
			mTotalBundles += additionalBundles;
		}
	}

	private void FinishResourceLoadProgress()
	{
		mKeepResourceLoadProgressAlive = false;

		if (mTotalBundles > 0 &&
			mBundleIndex >= mTotalBundles)
		{
			mTotalBundles = -1;
		}
	}

	private bool ProgressHiddenDuringPreload()
	{
		return
			mCurrentlyBackgroundLoading &&
			!LoadingScreenController
				.ShowingLoadingScreenNotFadingOut();
	}

	public void GetResourceLoadProgress(
		out float totalProgress,
		out float fileProgress)
	{
		totalProgress = -1f;
		fileProgress = -1f;

		if (
			mTotalBundles <= 0 ||
			ProgressHiddenDuringPreload())
		{
			return;
		}

		totalProgress =
			(float)mBundleIndex /
			(float)mTotalBundles;

		WWW activeWWW =
			Singleton<
				KFFAssetBundleManager
			>.Instance
				.GetActiveWWW();

		if (activeWWW != null)
		{
			mInProgressWWW =
				activeWWW;
		}

		if (mInProgressWWW != null)
		{
			totalProgress +=
				mInProgressWWW.progress /
				(float)mTotalBundles;

			if (
				(
					LoadingScreenController
						.ShowingLoadingScreenNotFadingOut()
					||
					DetachedSingleton<
						SceneFlowManager
					>.Instance
						.GetCurrentScene()
						==
						SceneFlowManager
							.Scene
							.AssetBundleDownload
				)
				&&
				mTotalBundles > 1)
			{
				fileProgress =
					mInProgressWWW.progress;
			}
		}
	}

	private void CheckResourceLoadStart(
		int defaultAmount = 1)
	{
		if (mTotalBundles <= 0)
		{
			mBundleIndex = 0;

			mTotalBundles =
				defaultAmount;
		}
	}

	public void OnResourceLoadDone()
	{
		mInProgressWWW =
			null;

		mBundleIndex++;

		if (
			mBundleIndex >=
			mTotalBundles &&
			!mKeepResourceLoadProgressAlive)
		{
			mTotalBundles =
				-1;
		}
	}

	// ============================================================
	// CREATURE RESOURCES
	// ============================================================

	public IEnumerator LoadCreatureResources(
		CreatureData creature,
		Action<GameObject> callback)
	{
		if (creature == null)
		{
			if (callback != null)
			{
				callback(null);
			}

			yield break;
		}

		// CreatureData.ID is the logical/data ID.
		// CreatureData.Prefab is the actual prefab folder/name.
		// Example:
		// TUT_Opp_Attacker -> Pig -> Pig/Pig.prefab
		string creatureName = creature.Prefab;

		if (string.IsNullOrEmpty(creatureName))
		{
			Debug.LogError(
				"[SLOTResourceManager] Creature prefab name is empty. " +
				"ID=" + creature.ID
			);

			if (callback != null)
			{
				callback(null);
			}

			yield break;
		}

		string bundleName =
			NormalizeBundleName(creatureName);

		string assetPath =
			creatureName +
			"/" +
			creatureName +
			".prefab";

		Debug.Log(
			"[SLOTResourceManager] Creature prefab load: " +
			"ID=" + creature.ID +
			" Prefab=" + creatureName +
			" Bundle=" + bundleName +
			" Asset=" + assetPath
		);

		GameObject result = null;
		bool done = false;

		QueueResourceLoad(
			assetPath,
			bundleName,
			delegate(UnityEngine.Object obj)
			{
				result = obj as GameObject;
				done = true;
			},
			false,
			false,
			false,
			false
		);

		while (!done)
		{
			yield return null;
		}

		if (callback != null)
		{
			callback(result);
		}
	}

	// ============================================================
	// LEADER RESOURCES
	// ============================================================

	public IEnumerator LoadLeaderResources(
		LeaderData leader,
		Action<UnityEngine.GameObject>
			callback)
	{
		CheckResourceLoadStart();

		GameObject objData =
			null;

		UICamera.LockInput();

		bool done = false;

		string bundleName =
			NormalizeBundleName(
				leader.Prefab
			);

		QueueResourceLoad(
			"Characters/" +
			leader.Prefab +
			"/" +
			leader.Prefab,
			bundleName,
			delegate(
				UnityEngine.Object loadedResource)
			{
				objData =
					loadedResource as
					GameObject;

				done = true;
			}
		);

		while (!done)
		{
			yield return null;
		}

		OnResourceLoadDone();

		UICamera.UnlockInput();

		if (objData == null)
		{
			Debug.LogError(
				"[SLOTResourceManager] LoadLeaderResources failed. " +
				"Leader: " +
				(leader != null ? leader.Prefab : "NULL")
			);
		}

		if (callback != null)
		{
			callback(objData);
		}
	}

	// ============================================================
	// ENVIRONMENT
	// ============================================================

	public IEnumerator LoadEnvironmentResources(
		QuestData quest,
		Action<UnityEngine.Object>
			callback)
	{
		CheckResourceLoadStart();

		UnityEngine.Object objData =
			null;

		UICamera.LockInput();

		bool done = false;

		string bundleName =
			NormalizeBundleName(
				quest.LevelPrefab
			);

		QueueResourceLoad(
			"Environment/" +
			quest.LevelPrefab +
			"/" +
			quest.LevelPrefab,
			bundleName,
			delegate(
				UnityEngine.Object loadedResource)
			{
				objData =
					loadedResource;

				done = true;
			}
		);

		while (!done)
		{
			yield return null;
		}

		OnResourceLoadDone();

		UICamera.UnlockInput();

		callback(objData);
	}

	// ============================================================
	// GAME BOARD
	// ============================================================

	public IEnumerator LoadGameBoardResources(
		QuestData quest,
		Action<UnityEngine.Object>
			callback)
	{
		CheckResourceLoadStart();

		UnityEngine.Object objData =
			null;

		UICamera.LockInput();

		bool done = false;

		string bundleName =
			NormalizeBundleName(
				quest.BoardPrefab
			);

		QueueResourceLoad(
			"GameBoard/" +
			quest.BoardPrefab +
			"/" +
			quest.BoardPrefab,
			bundleName,
			delegate(
				UnityEngine.Object loadedResource)
			{
				objData =
					loadedResource;

				done = true;
			}
		);

		while (!done)
		{
			yield return null;
		}

		OnResourceLoadDone();

		UICamera.UnlockInput();

		callback(objData);
	}

	// ============================================================
	// AUDIO
	// ============================================================

	public void LoadAudioResource(
		string clipName,
		string bundleName,
		Action<AudioClip> callback)
	{
		CheckResourceLoadStart();

		bundleName =
			NormalizeBundleName(
				bundleName
			);

		bool lockInput =
			LoadingScreenController
				.ShowingLoadingScreenNotFadingOut();

		if (lockInput)
		{
			UICamera.LockInput();
		}

		QueueResourceLoad(
			clipName,
			bundleName,
			delegate(
				UnityEngine.Object loadedResource)
			{
				OnResourceLoadDone();

				if (lockInput)
				{
					UICamera.UnlockInput();
				}

				callback(
					loadedResource as
					AudioClip
				);
			},
			false,
			false,
			false
		);
	}

	// ============================================================
	// UI TEXTURE
	// ============================================================

	public void QueueUITextureLoad(
		string texture,
		string assetBundle,
		string placeholderTexture,
		UITexture textureObject)
	{
		Texture textureFromCache =
			UITexture.GetTextureFromCache(
				Path.GetFileName(
					texture
				)
			);

		if (textureFromCache != null)
		{
			textureObject.UnloadTexture();

			textureObject.mainTexture =
				textureFromCache;

			return;
		}

		QueuedTextureLoad
			queuedTextureLoad =
				new QueuedTextureLoad();

		// Use the same central resolver as normal resource loads.
		// UI assets are part of MainResourcesBundle after the
		// AssetBundle migration unless a caller explicitly supplies
		// another bundle.
		queuedTextureLoad.AssetBundle =
			ResolveResourceBundleName(
				texture,
				assetBundle
			);

		queuedTextureLoad.AssetPath =
			texture;

		queuedTextureLoad.TextureObject =
			textureObject;

		mQueuedTextureLoads.Add(
			queuedTextureLoad
		);

		textureObject.ReplaceTexture(
			placeholderTexture
		);

		textureObject.StreamingInTexture =
			queuedTextureLoad.AssetPath;

		if (
			mQueuedTextureLoadCoroutine ==
			null)
		{
			mQueuedTextureLoadCoroutine =
				StartCoroutine(
					QueuedTextureLoadCoroutine()
				);
		}
	}

	private IEnumerator
		QueuedTextureLoadCoroutine()
	{
		while (
			mQueuedTextureLoads.Count > 0)
		{
			QueuedTextureLoad nextLoad =
				mQueuedTextureLoads[0];

			mQueuedTextureLoads.RemoveAt(0);

			if (
				nextLoad.TextureObject ==
					null ||
				nextLoad.TextureObject
					.StreamingInTexture
					!= nextLoad.AssetPath)
			{
				continue;
			}

			Texture alreadyLoaded =
				UITexture.GetTextureFromCache(
					Path.GetFileName(
						nextLoad.AssetPath
					)
				);

			if (alreadyLoaded != null)
			{
				nextLoad.TextureObject
					.UnloadTexture();

				nextLoad.TextureObject
					.mainTexture =
					alreadyLoaded;

				continue;
			}

			Texture texture =
				null;

			bool done =
				false;

			QueueResourceLoad(
				nextLoad.AssetPath,
				nextLoad.AssetBundle,
				delegate(
					UnityEngine.Object
						loadedResource)
				{
					texture =
						loadedResource as
						Texture;

					done = true;
				},
				false,
				true
			);

			while (!done)
			{
				yield return null;
			}

			if (
				nextLoad.TextureObject !=
					null &&
				nextLoad.TextureObject
					.StreamingInTexture
					==
					nextLoad.AssetPath &&
				texture != null)
			{
				nextLoad.TextureObject
					.UnloadTexture();

				nextLoad.TextureObject
					.mainTexture =
					texture;

				UITexture
					.AddLoadedTextureToCache(
						texture
					);
			}
		}

		mQueuedTextureLoadCoroutine =
			null;
	}

	// ============================================================
	// ASSET BUNDLE PRELOAD
	// ============================================================

	public void StartAssetBundlePreload(
		PreloadBundlesPoint preloadPoint)
	{
		if (!IsUsingAssetBundles())
		{
			return;
		}

		switch (preloadPoint)
		{
			case PreloadBundlesPoint.IntroBattle:
			{
				QuestLoadoutData startDeck =
					QuestLoadoutDataManager
						.Instance
						.GetData(
							"StartDeck"
						);

				QuestLoadoutData mainL1Q1 =
					QuestLoadoutDataManager
						.Instance
						.GetData(
							"MAIN_L1_Q1"
						);

				int totalBundles = 0;

				if (startDeck != null)
				{
					totalBundles +=
						startDeck
							.Entries
							.Count;
				}

				if (mainL1Q1 != null)
				{
					totalBundles +=
						mainL1Q1
							.Entries
							.Count;
				}

				StartResourceLoadProgress(
					totalBundles
				);

				if (startDeck != null)
				{
					foreach (
						QuestLoadoutEntry entry
						in startDeck.Entries)
					{
						if (
							entry == null ||
							entry.Creature == null)
						{
							continue;
						}

						QueueCreatureResourceLoad(
							entry.Creature
						);
					}
				}

				if (mainL1Q1 != null)
				{
					foreach (
						QuestLoadoutEntry entry
						in mainL1Q1.Entries)
					{
						if (
							entry == null ||
							entry.Creature == null)
						{
							continue;
						}

						QueueCreatureResourceLoad(
							entry.Creature
						);
					}
				}

				break;
			}

			case PreloadBundlesPoint.Q1:
			{
				QuestLoadoutData q1 =
					QuestLoadoutDataManager
						.Instance
						.GetData(
							"MAIN_L1_Q2"
						);

				if (q1 == null)
				{
					StartResourceLoadProgress(
						0
					);

					break;
				}

				int totalBundles =
					q1.Entries.Count;

				StartResourceLoadProgress(
					totalBundles
				);

				foreach (
					QuestLoadoutEntry entry
					in q1.Entries)
				{
					if (
						entry == null ||
						entry.Creature == null)
					{
						continue;
					}

					QueueCreatureResourceLoad(
						entry.Creature
					);
				}

				break;
			}

			case PreloadBundlesPoint.Q2:
			{
				StartResourceLoadProgress(
					0
				);

				break;
			}

			default:
			{
				StartResourceLoadProgress(
					0
				);

				break;
			}
		}
	}

	// ============================================================
	// CREATURE BUNDLE
	// ============================================================

	private string GetCreatureBundleName(
		CreatureData creature)
	{
		if (creature == null)
		{
			return string.Empty;
		}

		return NormalizeBundleName(
			creature.Prefab
		);
	}

	private void QueueCreatureResourceLoad(
		CreatureData creature)
	{
		if (creature == null)
		{
			return;
		}

		string bundleName =
			GetCreatureBundleName(
				creature
			);

		// ========================================================
		// ONLY LOAD THE CREATURE PREFAB.
		//
		// Example:
		//
		// Bundle:
		// archerdan
		//
		// Asset:
		// ArcherDan/archerdan.prefab
		//
		// NO TEXTURE LOAD HERE.
		// ========================================================

		string assetPath =
			creature.Prefab +
			"/" +
			creature.Prefab +
			".prefab";

		QueueResourceLoad(
			assetPath,
			bundleName,
			OnAssetPreloadDone,
			true,
			true,
			false,
			true
		);
	}

	private void OnAssetPreloadDone(
		UnityEngine.Object loadedAsset)
	{
		OnResourceLoadDone();
	}

	// ============================================================
	// HI-RES / LOW-RES BUNDLE CHECK
	// ============================================================

	private bool IsHiRezLowResBundle(
		string bundleName)
	{
		if (string.IsNullOrEmpty(
			bundleName))
		{
			return false;
		}

		bundleName =
			NormalizeBundleName(
				bundleName
			);

		int versionIndex =
			bundleName.IndexOf(
				".v"
			);

		if (versionIndex >= 0)
		{
			bundleName =
				bundleName.Substring(
					0,
					versionIndex
				);
		}

		if (
			bundleName ==
				"mainresourcesbundle" ||
			bundleName ==
				"mainscenesbundle" ||
			bundleName ==
				"ftueaudiobundle" ||
			bundleName ==
				"mainaudiobundle")
		{
			return false;
		}

		return true;
	}

	// ============================================================
	// IMMEDIATE INITIAL BUNDLE PRELOAD
	// ============================================================

	private IEnumerator PreloadBundlesImmediately()
	{
		if (!IsUsingAssetBundles())
			yield break;

		List<string> bundlesToLoadUpFront =
			BuildBundlesToLoadUpFront();

		if (bundlesToLoadUpFront == null ||
			bundlesToLoadUpFront.Count == 0)
		{
			Debug.Log(
				"[SLOTResourceManager] " +
				"No valid BundlesToLoadUpFront configured."
			);

			yield break;
		}

		// ========================================================
		// BUILD REAL DOWNLOAD LIST
		// ========================================================

		List<string> bundles = new List<string>();
		HashSet<string> uniqueBundles =
			new HashSet<string>();

		for (int i = 0;
			i < bundlesToLoadUpFront.Count;
			i++)
		{
			string originalName =
				bundlesToLoadUpFront[i];

			if (string.IsNullOrEmpty(originalName))
				continue;

			string bundleName =
				NormalizeBundleName(originalName);

			if (string.IsNullOrEmpty(bundleName))
				continue;

			// ----------------------------------------------------
			// LOCAL MAIN SCENES
			// ----------------------------------------------------

			if (IsMainScenesBundle(bundleName))
			{
				Debug.Log(
					"[SLOTResourceManager] " +
					"Skipping local bundle: " +
					bundleName
				);

				continue;
			}

			// ----------------------------------------------------
			// FAKE / OLD / INVALID PRELOAD ENTRIES
			// ----------------------------------------------------

			if (IsFakePreloadBundle(bundleName))
			{
				Debug.Log(
					"[SLOTResourceManager] " +
					"Ignoring fake preload entry: " +
					originalName
				);

				continue;
			}

			// ----------------------------------------------------
			// DUPLICATES
			// ----------------------------------------------------

			if (!uniqueBundles.Add(bundleName))
			{
				Debug.Log(
					"[SLOTResourceManager] " +
					"Ignoring duplicate preload entry: " +
					bundleName
				);

				continue;
			}

			bundles.Add(bundleName);
		}

		// ========================================================
		// NOTHING TO DOWNLOAD
		// ========================================================

		if (bundles.Count == 0)
		{
			Debug.Log(
				"[SLOTResourceManager] " +
				"No valid remote upfront bundles."
			);

			yield break;
		}

		// ========================================================
		// PROGRESS
		// ========================================================

		// The complete startup total was registered before the
		// primary bundle download started. Do NOT reset or append
		// another total here.
		mCurrentlyBackgroundLoading = false;

		Debug.Log(
			"[SLOTResourceManager] " +
			"Starting initial bundle preparation. " +
			"Count: " + bundles.Count
		);

		// ========================================================
		// LOAD EVERY REAL BUNDLE
		// ========================================================

		for (int i = 0;
			i < bundles.Count;
			i++)
		{
			string bundleName =
				bundles[i];

			// ----------------------------------------------------
			// RAM CACHE
			// ----------------------------------------------------

			AssetBundle existingBundle =
				Singleton<KFFAssetBundleManager>
					.Instance
					.GetAssetBundleByName(
						bundleName
					);

			if (existingBundle != null)
			{
				Debug.Log(
					"[SLOTResourceManager] " +
					"Upfront bundle already in RAM: " +
					bundleName
				);

				if (bundleName ==
					GetMainResourcesBundleName())
				{
					mResourcesBundle =
						existingBundle;
				}

				OnResourceLoadDone();

				continue;
			}

			// ----------------------------------------------------
			// LOAD
			// ----------------------------------------------------

			bool loaded = false;
			int retryCount = 0;

			while (!loaded &&
				retryCount < MAX_RETRY_COUNT)
			{
				retryCount++;

				bool success = false;
				string error = null;
				AssetBundle loadedBundle = null;

				Debug.Log(
					"[SLOTResourceManager] " +
					"Preparing bundle " +
					retryCount + "/" +
					MAX_RETRY_COUNT + ": " +
					bundleName
				);

				yield return StartCoroutine(
					Singleton<KFFAssetBundleManager>
						.Instance
						.LoadAssetBundleCoroutine(
							assetBundleBaseURL,
							bundleName,
							delegate(
								bool result,
								string errorMessage,
								AssetBundle bundle)
							{
								success = result;
								error = errorMessage;
								loadedBundle = bundle;
							}
						)
				);

				if (success &&
					loadedBundle != null)
				{
					loaded = true;

					if (bundleName ==
						GetMainResourcesBundleName())
					{
						mResourcesBundle =
							loadedBundle;
					}

					Debug.Log(
						"[SLOTResourceManager] " +
						"Bundle ready: " +
						bundleName
					);

					OnResourceLoadDone();

					break;
				}

				Debug.LogError(
					"[SLOTResourceManager] " +
					"Bundle preparation failed.\n" +
					"Bundle: " + bundleName +
					"\nAttempt: " + retryCount +
					"/" + MAX_RETRY_COUNT +
					"\nError: " + error
				);

				if (retryCount >= MAX_RETRY_COUNT)
				{
					mPrimaryBundlesLoadFailed = true;

					Debug.LogError(
						"[SLOTResourceManager] " +
						"Bundle permanently failed: " +
						bundleName
					);

					yield break;
				}

				int selection = -1;

				Singleton<SimplePopupController>
					.Instance
					.ShowMessage(
						string.Empty,
						KFFLocalization.Get(
							"!!ERROR_LOADING_ASSET_BODY"
						),
						delegate
						{
							selection = 1;
						},
						KFFLocalization.Get(
							"!!RETRY"
						)
					);

				while (selection == -1)
				{
					yield return null;
				}

				if (selection == 0)
				{
					mPrimaryBundlesLoadFailed = true;
					yield break;
				}
			}
		}

		mCurrentlyBackgroundLoading = false;

		Debug.Log(
			"[SLOTResourceManager] " +
			"Initial bundle preparation completed."
		);

		// Only now is the single startup progress session complete.
		FinishResourceLoadProgress();
	}

	// ============================================================
	// PRIMARY BUNDLES
	// ============================================================

	public IEnumerator
		LoadPrimaryBundlesCouroutine()
	{
		mPrimaryBundlesLoadFailed =
			false;

		if (!IsUsingAssetBundles())
		{
			yield break;
		}

		// ========================================================
		// MAIN SCENES
		//
		// LOCAL ONLY.
		// NEVER DOWNLOAD.
		// ========================================================

		Debug.Log(
			"[SLOTResourceManager] " +
			"MainScenesBundle is LOCAL. " +
			"Remote download skipped."
		);

		// ========================================================
		// MANDATORY PRIMARY BUNDLES
		// ========================================================

		List<string> bundlesToLoad =
			new List<string>();

		string mainResourcesBundle =
			GetMainResourcesBundleName();

		if (!string.IsNullOrEmpty(
			mainResourcesBundle))
		{
			bundlesToLoad.Add(
				mainResourcesBundle
			);
		}

		// ========================================================
		// LOAD PRIMARY BUNDLES
		// ========================================================

		if (bundlesToLoad.Count > 0)
		{
			// Build the complete startup progress total BEFORE the
			// first download starts. This prevents 100% -> 0%.
			List<string> startupBundles =
				BuildBundlesToLoadUpFront();

			int startupTotal =
				bundlesToLoad.Count +
				(startupBundles != null
					? startupBundles.Count
					: 0);

			StartResourceLoadProgress(
				startupTotal
			);

			// Keep the single startup progress session alive until
			// PreloadBundlesImmediately() has finished.
			mKeepResourceLoadProgressAlive = true;

			mCurrentlyBackgroundLoading =
				false;

			for (
				int i = 0;
				i < bundlesToLoad.Count;
				i++)
			{
				string bundleName =
					NormalizeBundleName(
						bundlesToLoad[i]
					);

				if (
					IsMainScenesBundle(
						bundleName))
				{
					Debug.Log(
						"[SLOTResourceManager] " +
						"Skipping local MainScenesBundle."
					);

					OnResourceLoadDone();

					continue;
				}

				// ------------------------------------------------
				// RAM CACHE
				// ------------------------------------------------

				AssetBundle existingBundle =
					Singleton<
						KFFAssetBundleManager
					>.Instance
						.GetAssetBundleByName(
							bundleName
						);

				if (existingBundle != null)
				{
					Debug.Log(
						"[SLOTResourceManager] " +
						"Primary bundle already loaded: " +
						bundleName
					);

					if (
						bundleName ==
						GetMainResourcesBundleName())
					{
						mResourcesBundle =
							existingBundle;
					}

					OnResourceLoadDone();

					continue;
				}

				// ------------------------------------------------
				// LOAD
				// ------------------------------------------------

				bool loaded =
					false;

				int retryCount =
					0;

				while (
					!loaded &&
					retryCount <
					MAX_RETRY_COUNT)
				{
					retryCount++;

					bool success =
						false;

					string error =
						null;

					AssetBundle loadedBundle =
						null;

					Debug.Log(
						"[SLOTResourceManager] " +
						"Primary bundle load " +
						retryCount +
						"/" +
						MAX_RETRY_COUNT +
						": " +
						bundleName
					);

					yield return StartCoroutine(
						Singleton<
							KFFAssetBundleManager
						>.Instance
						.LoadAssetBundleCoroutine(
							assetBundleBaseURL,
							bundleName,
							delegate(
								bool result,
								string errorMessage,
								AssetBundle bundle)
							{
								success =
									result;

								error =
									errorMessage;

								loadedBundle =
									bundle;
							}
						)
					);

					if (
						success &&
						loadedBundle != null)
					{
						loaded =
							true;

						if (
							bundleName ==
							GetMainResourcesBundleName())
						{
							mResourcesBundle =
								loadedBundle;
						}

						Debug.Log(
							"[SLOTResourceManager] " +
							"Primary bundle ready: " +
							bundleName
						);

						OnResourceLoadDone();

						break;
					}

					Debug.LogError(
						"[SLOTResourceManager] " +
						"Primary bundle failed.\n" +
						"Bundle: " +
						bundleName +
						"\nAttempt: " +
						retryCount +
						"/" +
						MAX_RETRY_COUNT +
						"\nError: " +
						error
					);

					if (
						retryCount >=
						MAX_RETRY_COUNT)
					{
						mPrimaryBundlesLoadFailed =
							true;

						yield break;
					}

					int selection =
						-1;

					Singleton<
						SimplePopupController
					>.Instance
						.ShowMessage(
							string.Empty,
							KFFLocalization.Get(
								"!!ERROR_LOADING_ASSET_BODY"
							),
							delegate
							{
								selection = 1;
							},
							KFFLocalization.Get(
								"!!RETRY"
							)
						);

					while (
						selection == -1)
					{
						yield return null;
					}

					if (selection == 0)
					{
						mPrimaryBundlesLoadFailed =
							true;

						yield break;
					}
				}
			}
		}

		// ========================================================
		// UPFRONT BUNDLES
		// ========================================================

		yield return StartCoroutine(
			PreloadBundlesImmediately()
		);

		if (mPrimaryBundlesLoadFailed)
		{
			Debug.LogError(
				"[SLOTResourceManager] " +
				"Initial bundle preparation failed."
			);

			yield break;
		}

		mCurrentlyBackgroundLoading =
			false;

		Debug.Log(
			"[SLOTResourceManager] " +
			"ALL INITIAL ASSETBUNDLES ARE READY."
		);
	}

	private bool IsFakePreloadBundle(string bundleName)
	{
		if (string.IsNullOrEmpty(bundleName))
			return true;

		string name = NormalizeBundleName(bundleName);

		switch (name)
		{
			case "gameboard_forest":
			case "warrior":
			case "thief":
			case "diablo_base":
			case "genie_evo_01":
			case "kraken_base":
			case "snakequeen_base":
			case "mermaid_base":
			case "headless_base":
			case "ftueaudiobundle":
			case "introbattle":
				return true;
		}

		return false;
	}

	private List<string> BuildBundlesToLoadUpFront()
	{
		List<string> result = new List<string>();

		AddBundleList(result, SystemAssetBundles);
		AddBundleList(result, CreatureAssetBundles);
		AddBundleList(result, HeroAssetBundles);
		AddBundleList(result, EnvironmentAssetBundles);
		AddBundleList(result, ExtraAssetBundlesPack);

		return result;
	}

	private void AddBundleList(
		List<string> target,
		List<string> source)
	{
		if (source == null)
			return;

		for (int i = 0; i < source.Count; i++)
		{
			string bundle = NormalizeBundleName(source[i]);

			if (string.IsNullOrEmpty(bundle))
				continue;

			if (IsMainScenesBundle(bundle))
				continue;

			if (IsFakePreloadBundle(bundle))
				continue;

			if (!target.Contains(bundle))
				target.Add(bundle);
		}
	}
}
