using System;
using System.Collections.Generic;
using System.IO;
using MiniJSON;
using UnityEngine;

public class SQSettings
{
    private const string GameModePlayerPrefsKey = "CWK_GAME_MODE";
    private static string selectedModeId = "cwk_0001";
    private static string serverUrl;
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
    public static string GAME_MODE_ID => selectedModeId;
    public static int SAVE_INTERVAL => saveInterval;
    public static int PATCHING_FILE_LIMIT => patchingFileLimit;
    public static string BundleIdentifier => bundleIdentifier;
    public static string BundleVersion => bundleVersion;

    public static string SERVER_PREFIX
    {
        get
        {
            string[] parts = (serverUrl ?? string.Empty).Trim().Split(
                new string[] { "://", "." }, StringSplitOptions.None);
            return parts.Length > 1 ? parts[1] : string.Empty;
        }
    }

    private SQSettings() { }

    public static string getJsonPath(string filePath)
    {
        WWW request = new WWW(filePath);
        while (!request.isDone) { }
        return request.text;
    }

    public static bool SelectGameMode(string modeId)
    {
        if (modeId != "cwk_0001" && modeId != "cwk_0002")
        {
            Debug.LogError("[SQSettings] Unknown game mode: " + modeId);
            return false;
        }

        selectedModeId = modeId;
        PlayerPrefs.SetString(GameModePlayerPrefsKey, modeId);
        PlayerPrefs.Save();
        return true;
    }

    public static void Init()
    {
        Debug.Log("Entering SQSettings.Init()");
        bundleIdentifier = Application.identifier;
        bundleVersion = Application.version;

        string savedMode = PlayerPrefs.GetString(GameModePlayerPrefsKey, "cwk_0001");
        selectedModeId = savedMode == "cwk_0002" ? "cwk_0002" : "cwk_0001";

        Dictionary<string, object> root = ReadSettings("server_settings.json");
        Dictionary<string, object> settings = root;
        object modeValue;
        if (root.TryGetValue(selectedModeId, out modeValue))
        {
            settings = modeValue as Dictionary<string, object>;
            if (settings == null)
                throw new InvalidDataException("server_settings.json entry '" + selectedModeId + "' must be an object.");
        }

        string urlKey = useLocalServer ? "local_server_url" : "server_url";
        serverUrl = ReadString(settings, urlKey, string.Empty);
        photonChatAppID = ReadString(settings, "photon_chat_app_id", string.Empty);
        photonPUNAppID = ReadString(settings, "photon_pun_app_id", string.Empty);
        useRelay = ReadBool(settings, "use_relay", false);
        relayHost = ReadString(settings, "relay_host", string.Empty);
        relayPort = ReadInt(settings, "relay_port", 9090);

        if (string.IsNullOrEmpty(serverUrl))
            Debug.LogError("[SQSettings] No server URL configured for " + selectedModeId + ".");
        if (string.IsNullOrEmpty(photonPUNAppID))
            Debug.LogWarning("[SQSettings] Photon PUN App ID is empty for " + selectedModeId + ".");

        Dictionary<string, object> global = ReadSettings("global_settings.json");
        saveInterval = ReadInt(global, "save_interval", 300);
        patchingFileLimit = ReadInt(global, "patching_file_limit", 10);
    }

    private static Dictionary<string, object> ReadSettings(string fileName)
    {
        string path = TFUtils.GetStreamingAssetsFile(fileName);
        string json = path.Contains("://") ? getJsonPath(path) : File.ReadAllText(path);
        Dictionary<string, object> parsed = Json.Deserialize(json) as Dictionary<string, object>;
        if (parsed == null)
            throw new InvalidDataException("Invalid JSON object in " + fileName);
        return parsed;
    }

    private static string ReadString(Dictionary<string, object> data, string key, string fallback)
    {
        object value;
        return data != null && data.TryGetValue(key, out value) && value != null
            ? Convert.ToString(value)
            : fallback;
    }

    private static bool ReadBool(Dictionary<string, object> data, string key, bool fallback)
    {
        object value;
        if (data == null || !data.TryGetValue(key, out value) || value == null)
            return fallback;
        return Convert.ToBoolean(value);
    }

    private static int ReadInt(Dictionary<string, object> data, string key, int fallback)
    {
        object value;
        if (data == null || !data.TryGetValue(key, out value) || value == null)
            return fallback;
        return Convert.ToInt32(value);
    }
}
