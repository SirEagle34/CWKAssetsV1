using System;
using System.Collections.Generic;
using System.IO;
using MiniJSON;
using UnityEngine;

public class SQSettings
{
	private static string serverUrl;

	private static string localServerUrl;

	private static string photonChatAppID;

	private static string photonPUNAppID;

	private static int saveInterval;

	private static int patchingFileLimit;

	private static string bundleIdentifier;

	private static string bundleVersion;

	private static bool useRelay;

	private static string relayHost = string.Empty;

	private static int relayPort = 9090;

	public static bool useLocalServer;

	public static bool USE_RELAY => useRelay;

	public static string RELAY_HOST => relayHost;

	public static int RELAY_PORT => relayPort;

	public static string CDN_URL => serverUrl + "persist/static/";

	public static string MANIFEST_URL => serverUrl + "persist/static/manifest.json";

	public static string SERVER_URL => serverUrl;

	public static string PHOTON_CHAT_APP_ID => photonChatAppID;

	public static string PHOTON_PUN_APP_ID => photonPUNAppID;

	public static string SERVER_PREFIX
	{
		get
		{
			string[] array = serverUrl.Trim().Split(new string[2] { "://", "." }, StringSplitOptions.None);
			if (array.Length > 1)
			{
				return array[1];
			}
			return string.Empty;
		}
	}

	public static int SAVE_INTERVAL => saveInterval;

	public static int PATCHING_FILE_LIMIT => patchingFileLimit;

	public static string BundleIdentifier => bundleIdentifier;

	public static string BundleVersion => bundleVersion;

	private SQSettings()
	{
	}

	public static string getJsonPath(string filePath)
	{
		WWW wWW = null;
		wWW = new WWW(filePath);
		while (!wWW.isDone)
		{
		}
		return wWW.text;
	}

	public static void Init()
	{
		Debug.Log("Entering SQSettings Init()");
		bundleIdentifier = Application.identifier;
		bundleVersion = Application.version;
		_ = string.Empty;
		string streamingAssetsFile = TFUtils.GetStreamingAssetsFile("server_settings.json");
		Dictionary<string, object> dictionary = (Dictionary<string, object>)Json.Deserialize((!streamingAssetsFile.Contains("://")) ? File.ReadAllText(streamingAssetsFile) : getJsonPath(streamingAssetsFile));
		if (useLocalServer)
		{
			serverUrl = (string)dictionary["local_server_url"];
		}
		else
		{
			serverUrl = (string)dictionary["server_url"];
		}
		photonChatAppID = (string)dictionary["photon_chat_app_id"];
		photonPUNAppID = (string)dictionary["photon_pun_app_id"];
		useRelay = dictionary.ContainsKey("use_relay") && Convert.ToBoolean(dictionary["use_relay"]);
		relayHost = (dictionary.ContainsKey("relay_host") ? ((string)dictionary["relay_host"]) : string.Empty);
		relayPort = (dictionary.ContainsKey("relay_port") ? Convert.ToInt32(dictionary["relay_port"]) : 9090);
		streamingAssetsFile = TFUtils.GetStreamingAssetsFile("global_settings.json");
		dictionary = (Dictionary<string, object>)Json.Deserialize((!streamingAssetsFile.Contains("://")) ? File.ReadAllText(streamingAssetsFile) : getJsonPath(streamingAssetsFile));
		saveInterval = TFUtils.LoadInt(dictionary, "save_interval");
		int? num = TFUtils.TryLoadInt(dictionary, "patching_file_limit");
		if (num.HasValue)
		{
			patchingFileLimit = num.Value;
		}
		else
		{
			patchingFileLimit = 10;
		}
	}
}
