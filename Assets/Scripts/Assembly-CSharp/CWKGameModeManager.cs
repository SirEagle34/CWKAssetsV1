using System;
using System.Collections.Generic;
using System.IO;
using MiniJSON;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CWKGameModeManager : MonoBehaviour
{
    public const string Mode001 = "cwk_0001";
    public const string Mode002 = "cwk_0002";

    private const string PrefKey = "CWK_GAME_MODE";
    private static CWKGameModeManager instance;
    private static bool hasSelectedModeThisSession;
    private Dictionary<string, object> rootSettings;

    public static string CurrentModeId
    {
        get
        {
            return PlayerPrefs.GetString(PrefKey, Mode001) == Mode002 ? Mode002 : Mode001;
        }
    }

    public static CWKGameModeManager Instance { get { return instance; } }
    public static bool HasSelectedModeThisSession { get { return hasSelectedModeThisSession; } }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreateInstanceBeforeSceneLoad()
    {
        hasSelectedModeThisSession = false;

        if (instance == null)
        {
            GameObject managerObject = new GameObject("CWKGameModeManager");
            managerObject.AddComponent<CWKGameModeManager>();
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        LoadConfiguration();
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    public bool SelectMode(string modeId)
    {
        if (modeId != Mode001 && modeId != Mode002)
        {
            Debug.LogError("[CWKGameModeManager] Unknown mode: " + modeId);
            return false;
        }

        // A live session caches player-specific URLs for the currently selected server.
        // Do not switch servers until that session has been logged out and cleared.
        SessionManager sessionManager = SessionManager.Instance;
        if (sessionManager != null &&
            sessionManager.theSession != null &&
            SQSettings.GAME_MODE_ID != modeId)
        {
            Debug.LogWarning("[CWKGameModeManager] Cannot change game mode while a session is active. Log out first.");
            return false;
        }

        // SelectGameMode also persists the selected mode and invalidates cached settings.
        if (!SQSettings.SelectGameMode(modeId))
            return false;

        PlayerPrefs.SetString(PrefKey, modeId);
        PlayerPrefs.Save();
        hasSelectedModeThisSession = true;

        Debug.Log("[CWKGameModeManager] Selected mode: " + modeId);
        return true;
    }

    public void SelectModeAndLoadConnect(string modeId)
    {
        if (!SelectMode(modeId))
            return;

        // LoadModeScene resolves missing mode-specific scenes to valid shared scenes.
        LoadModeScene("version_check");
    }

    public bool LoadChooseGameScene()
    {
        string sceneName = GetSceneName("choose_game");
        if (string.IsNullOrEmpty(sceneName))
            sceneName = "ChooseGame";

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError("[CWKGameModeManager] ChooseGame scene '" + sceneName +
                "' is not included in Build Settings. Continuing with the saved/default mode.");
            return false;
        }

        Debug.Log("[CWKGameModeManager] Loading game mode selection scene: " + sceneName);
        SceneManager.LoadScene(sceneName);
        return true;
    }

    public string GetSceneName(string sceneKey)
    {
        if (string.IsNullOrEmpty(sceneKey))
            return string.Empty;

        if (rootSettings == null)
            LoadConfiguration();

        object modeObject;
        if (rootSettings == null || !rootSettings.TryGetValue(CurrentModeId, out modeObject))
            return string.Empty;

        Dictionary<string, object> mode = modeObject as Dictionary<string, object>;
        object scenesObject;
        if (mode == null || !mode.TryGetValue("scenes", out scenesObject))
            return string.Empty;

        Dictionary<string, object> scenes = scenesObject as Dictionary<string, object>;
        object sceneValue;
        return scenes != null && scenes.TryGetValue(sceneKey, out sceneValue) && sceneValue != null
            ? Convert.ToString(sceneValue)
            : string.Empty;
    }

    public void LoadModeScene(string sceneKey)
    {
        string sceneName = GetSceneName(sceneKey);

        if (!string.IsNullOrEmpty(sceneName) && Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.Log("[CWKGameModeManager] Loading scene '" + sceneName + "' for " + CurrentModeId);
            SceneManager.LoadScene(sceneName);
            return;
        }

        // Do not silently load a cwk_0001 scene when a cwk_0002 scene is missing.
        // ChooseGame is handled separately by LoadChooseGameScene because it is shared.
        Debug.LogError("[CWKGameModeManager] Cannot load scene key '" + sceneKey + "' for " +
            CurrentModeId + ". Configured scene: '" + sceneName +
            "'. Ensure this mode's scene is included in Build Settings.");
    }

    private string GetSceneNameForMode(string modeId, string sceneKey)
    {
        if (rootSettings == null)
            LoadConfiguration();

        object modeObject;
        if (rootSettings == null || !rootSettings.TryGetValue(modeId, out modeObject))
            return string.Empty;

        Dictionary<string, object> mode = modeObject as Dictionary<string, object>;
        object scenesObject;
        if (mode == null || !mode.TryGetValue("scenes", out scenesObject))
            return string.Empty;

        Dictionary<string, object> scenes = scenesObject as Dictionary<string, object>;
        object sceneValue;
        return scenes != null && scenes.TryGetValue(sceneKey, out sceneValue) && sceneValue != null
            ? Convert.ToString(sceneValue)
            : string.Empty;
    }

    private void LoadConfiguration()
    {
        try
        {
            string path = TFUtils.GetStreamingAssetsFile("server_settings.json");
            string json = path.Contains("://") ? SQSettings.getJsonPath(path) : File.ReadAllText(path);
            rootSettings = Json.Deserialize(json) as Dictionary<string, object>;

            if (rootSettings == null)
                Debug.LogError("[CWKGameModeManager] server_settings.json is not a JSON object.");
        }
        catch (Exception ex)
        {
            rootSettings = null;
            Debug.LogError("[CWKGameModeManager] Could not read server_settings.json: " + ex.Message);
        }
    }
}