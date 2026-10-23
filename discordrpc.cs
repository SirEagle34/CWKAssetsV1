// Discord SDK solo es compatible con Windows, macOS y Linux (NO con Android/iOS)
#if !UNITY_ANDROID && !UNITY_IOS && !UNITY_WEBGL
using Discord;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;

public class DiscordRPC : MonoBehaviour
{
#if !UNITY_ANDROID && !UNITY_IOS && !UNITY_WEBGL
    private Discord.Discord discord;
#endif

    void Start()
    {
#if !UNITY_ANDROID && !UNITY_IOS && !UNITY_WEBGL
        try
        {
            discord = new Discord.Discord(1319604146831097856, (ulong)Discord.CreateFlags.NoRequireDiscord);
            UpdateActivityBasedOnScene();
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("Discord is not running or failed to initialize: " + ex.Message);
            discord = null;
        }
#else
        Debug.Log("Discord RPC no está disponible en esta plataforma (Android/iOS/WebGL)");
#endif
    }

    void Update()
    {
#if !UNITY_ANDROID && !UNITY_IOS && !UNITY_WEBGL
        if (discord != null)
        {
            try
            {
                discord.RunCallbacks();
            }
            catch (System.Exception ex)
            {
                Debug.LogError("Discord callbacks error: " + ex.Message);
            }
        }
#endif
    }

    void OnDisable()
    {
#if !UNITY_ANDROID && !UNITY_IOS && !UNITY_WEBGL
        CleanupDiscord();
#endif
    }

    void OnApplicationQuit()
    {
#if !UNITY_ANDROID && !UNITY_IOS && !UNITY_WEBGL
        CleanupDiscord();
#endif
    }

#if !UNITY_ANDROID && !UNITY_IOS && !UNITY_WEBGL
    private void CleanupDiscord()
    {
        if (discord != null)
        {
            discord.Dispose();
            discord = null;
        }
    }

    private void UpdateActivityBasedOnScene()
    {
#if !UNITY_ANDROID && !UNITY_IOS && !UNITY_WEBGL
        if (discord == null) return;

        if (SceneManager.sceneCount > 1)
        {
            Scene scene = SceneManager.GetSceneAt(1);
            if (scene.name == "FrontEndScene")
            {
                InMenu();
            }
            else if (scene.name == "BattleScene")
            {
                InBattle();
            }
        }
        else
        {
            Debug.LogWarning("Not enough scenes loaded to determine activity.");
        }
#endif
    }

    public void InMenu()
    {
#if !UNITY_ANDROID && !UNITY_IOS && !UNITY_WEBGL
        if (discord == null) return;

        var activityManager = discord.GetActivityManager();
        var activity = new Discord.Activity
        {
            State = "In menu",
            Assets = new Discord.ActivityAssets
            {
                LargeImage = "card_wars_kingdom"
            }
        };

        activityManager.UpdateActivity(activity, result =>
        {
            if (result == Discord.Result.Ok)
            {
                Debug.Log("Discord RPC updated successfully!");
            }
            else
            {
                Debug.LogWarning("Failed to update Discord RPC: " + result);
            }
        });
#endif
    }

    public void InBattle()
    {
#if !UNITY_ANDROID && !UNITY_IOS && !UNITY_WEBGL
        if (discord == null) return;

        var activityManager = discord.GetActivityManager();
        var activity = new Discord.Activity
        {
            State = "Playing battle",
            Assets = new Discord.ActivityAssets
            {
                LargeImage = "in_battle"
            }
        };

        activityManager.UpdateActivity(activity, result =>
        {
            if (result == Discord.Result.Ok)
            {
                Debug.Log("Discord RPC updated successfully!");
            }
            else
            {
                Debug.LogWarning("Failed to update Discord RPC: " + result);
            }
        });
#endif
    }
#endif
}
