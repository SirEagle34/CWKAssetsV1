using System;
using System.Collections.Generic;
using System.IO;
using MiniJSON;
using UnityEngine;
using UnityEngine.SceneManagement;

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
    private static Dictionary<string, object> selectedModeSettings;
    private static bool initialized;

    public static bool useLocalServer;

    public static bool USE_RELAY { get { EnsureInitialized(); return useRelay; } }
    public static string RELAY_HOST { get { EnsureInitialized(); return relayHost; } }
    public static int RELAY_PORT { get { EnsureInitialized(); return relayPort; } }
    public static string CDN_URL { get { EnsureInitialized(); return (serverUrl ?? string.Empty) + "persist/static/"; } }
    public static string MANIFEST_URL { get { EnsureInitialized(); return (serverUrl ?? string.Empty) + "persist/static/manifest.json"; } }
    public static string SERVER_URL { get { EnsureInitialized(); return serverUrl ?? string.Empty; } }
    public static string PHOTON_CHAT_APP_ID { get { EnsureInitialized(); return photonChatAppID ?? string.Empty; } }
    public static string PHOTON_PUN_APP_ID { get { EnsureInitialized(); return photonPUNAppID ?? string.Empty; } }
    public static string GAME_MODE_ID { get { EnsureInitialized(); return selectedModeId; } }
    public static int SAVE_INTERVAL { get { EnsureInitialized(); return saveInterval; } }
    public static int PATCHING_FILE_LIMIT { get { EnsureInitialized(); return patchingFileLimit; } }
    public static string BundleIdentifier { get { EnsureInitialized(); return bundleIdentifier; } }
    public static string BundleVersion { get { EnsureInitialized(); return bundleVersion; } }

    public static string SERVER_PREFIX
    {
        get
        {
            EnsureInitialized();
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
        initialized = false;
        selectedModeSettings = null;
        serverUrl = null;
        photonChatAppID = null;
        photonPUNAppID = null;

        PlayerPrefs.SetString(GameModePlayerPrefsKey, modeId);
        PlayerPrefs.Save();

        Debug.Log("[SQSettings] Game mode selected: " + modeId);
        return true;
    }

    public static void Init()
    {
        // Invalidate first so a failed reload cannot leave stale settings marked as valid.
        initialized = false;
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

        selectedModeSettings = settings;

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
        initialized = true;

        Debug.Log("[SQSettings] Initialized mode=" + selectedModeId +
            ", server=" + serverUrl +
            ", PhotonPUNConfigured=" + (!string.IsNullOrEmpty(photonPUNAppID)));
    }

    private static void EnsureInitialized()
    {
        if (initialized)
            return;

        try
        {
            Init();
        }
        catch (Exception ex)
        {
            initialized = false;
            Debug.LogError("[SQSettings] Initialization failed: " + ex.Message);
        }
    }

    public static string GetConfiguredSceneName(string sceneKey, string fallback = null)
    {
        if (string.IsNullOrEmpty(sceneKey))
            return fallback ?? string.Empty;

        EnsureInitialized();

        object scenesValue;
        Dictionary<string, object> scenes;
        object sceneValue;

        if (selectedModeSettings != null &&
            selectedModeSettings.TryGetValue("scenes", out scenesValue) &&
            (scenes = scenesValue as Dictionary<string, object>) != null &&
            scenes.TryGetValue(sceneKey, out sceneValue) &&
            sceneValue != null)
        {
            string configuredName = Convert.ToString(sceneValue);
            if (!string.IsNullOrEmpty(configuredName) && Application.CanStreamedLevelBeLoaded(configuredName))
                return configuredName;

            Debug.LogWarning("[SQSettings] Configured scene '" + configuredName + "' for key '" +
                sceneKey + "' is not in Build Settings. Checking fallback.");
        }

        if (!string.IsNullOrEmpty(fallback) && Application.CanStreamedLevelBeLoaded(fallback))
            return fallback;

        // Use the shared cwk_0001 scene only if it exists in Build Settings.
        try
        {
            Dictionary<string, object> root = ReadSettings("server_settings.json");
            object baseModeValue;
            if (root.TryGetValue("cwk_0001", out baseModeValue))
            {
                Dictionary<string, object> baseMode = baseModeValue as Dictionary<string, object>;
                object baseScenesValue;
                if (baseMode != null && baseMode.TryGetValue("scenes", out baseScenesValue))
                {
                    Dictionary<string, object> baseScenes = baseScenesValue as Dictionary<string, object>;
                    object baseSceneValue;
                    if (baseScenes != null && baseScenes.TryGetValue(sceneKey, out baseSceneValue) &&
                        baseSceneValue != null)
                    {
                        string baseScene = Convert.ToString(baseSceneValue);
                        if (!string.IsNullOrEmpty(baseScene) && Application.CanStreamedLevelBeLoaded(baseScene))
                        {
                            Debug.LogWarning("[SQSettings] Using shared scene '" + baseScene +
                                "' for key '" + sceneKey + "' while mode '" + selectedModeId + "' is active.");
                            return baseScene;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("[SQSettings] Could not resolve scene fallback: " + ex.Message);
        }

        return fallback ?? string.Empty;
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