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

    public void LoadChooseGameScene()
    {
        string sceneName = GetSceneName("choose_game");
        if (string.IsNullOrEmpty(sceneName))
            sceneName = "ChooseGame";

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError("[CWKGameModeManager] ChooseGame scene '" + sceneName +
                "' is not included in Build Settings. Add the scene or call this method only when that scene exists.");
            return;
        }

        SceneManager.LoadScene(sceneName);
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

        // Mode-specific scene assets may not have been added to this combined project yet.
        // Use the cwk_0001 scene mapping only when that scene is actually in Build Settings.
        string fallbackScene = GetSceneNameForMode(Mode001, sceneKey);
        if (!string.IsNullOrEmpty(fallbackScene) && Application.CanStreamedLevelBeLoaded(fallbackScene))
        {
            Debug.LogWarning("[CWKGameModeManager] Scene '" + sceneName + "' for " + CurrentModeId +
                " is unavailable. Falling back to shared scene '" + fallbackScene + "'.");
            SceneManager.LoadScene(fallbackScene);
            return;
        }

        Debug.LogError("[CWKGameModeManager] Cannot load scene key '" + sceneKey + "' for " +
            CurrentModeId + ". Configured scene: '" + sceneName +
            "'. No valid fallback scene is included in Build Settings.");
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
