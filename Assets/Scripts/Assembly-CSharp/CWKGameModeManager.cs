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
    private Dictionary<string, object> rootSettings;

    public static string CurrentModeId
    {
        get { return PlayerPrefs.GetString(PrefKey, Mode001) == Mode002 ? Mode002 : Mode001; }
    }

    public static CWKGameModeManager Instance { get { return instance; } }

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

    public bool SelectMode(string modeId)
    {
        if (modeId != Mode001 && modeId != Mode002)
        {
            Debug.LogError("[CWKGameModeManager] Unknown mode: " + modeId);
            return false;
        }

        PlayerPrefs.SetString(PrefKey, modeId);
        PlayerPrefs.Save();
        return SQSettings.SelectGameMode(modeId);
    }

    public void SelectModeAndLoadConnect(string modeId)
    {
        if (!SelectMode(modeId))
            return;

        string sceneName = GetSceneName("connect");
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("[CWKGameModeManager] Connect scene is not configured for " + modeId);
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError("[CWKGameModeManager] Scene '" + sceneName + "' is not included in Build Settings yet.");
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
        return scenes != null && scenes.TryGetValue(sceneKey, out sceneValue)
            ? Convert.ToString(sceneValue)
            : string.Empty;
    }

    public void LoadModeScene(string sceneKey)
    {
        string sceneName = GetSceneName(sceneKey);
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("[CWKGameModeManager] Scene key '" + sceneKey + "' is not configured for " + CurrentModeId);
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError("[CWKGameModeManager] Scene '" + sceneName + "' is not included in Build Settings yet.");
            return;
        }

        SceneManager.LoadScene(sceneName);
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
            Debug.LogError("[CWKGameModeManager] Could not read server_settings.json: " + ex.Message);
        }
    }
}
