using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneFlowManager : DetachedSingleton<SceneFlowManager>
{
	public enum Scene
	{
		Loading = 0,
		BootVideoScene = 1,
		VersionCheck = 2,
		AssetBundleDownload = 3,
		Connect = 4,
		FrontEnd = 5,
		Battle = 6,
		SceneCount = 7,
		None = 8,
		ChooseGame = 9
	}

	public enum ReturnLocation
	{
		NotSet = 0,
		Town = 1,
		Map = 2,
		SpecialQuests = 3,
		Pvp = 4
	}

	private string[] mSceneMap = new string[10];

	private string[] mLowResSceneMap = new string[10];

	private Scene mPreviousScene = Scene.None;

	private Scene mCurrentScene = Scene.None;

	private ReturnLocation mReturnLocation;

	public SceneFlowManager()
	{
		mSceneMap[0] = "LoadingScreen";
		mSceneMap[1] = "BootVideoScene";
		mSceneMap[2] = "VersionCheckScene";
		mSceneMap[3] = "AssetBundleDownloadScene";
		mSceneMap[4] = "ConnectScene";
		mSceneMap[5] = "FrontEndScene";
		mSceneMap[6] = "BattleScene";
		mSceneMap[9] = "ChooseGame";
		mLowResSceneMap[0] = "low_LoadingScreen";
		mLowResSceneMap[1] = string.Empty;
		mLowResSceneMap[2] = "low_VersionCheckScene";
		mLowResSceneMap[3] = "low_AssetBundleDownloadScene";
		mLowResSceneMap[4] = "low_ConnectScene";
		mLowResSceneMap[5] = "low_FrontEndScene";
		mLowResSceneMap[6] = "low_BattleScene";
<<<<<<< HEAD
		mLowResSceneMap[7] = string.Empty;
		mLowResSceneMap[8] = string.Empty;
		mLowResSceneMap[9] = string.Empty;
=======

>>>>>>> ab1e8e11 (Fix SQSettings server mode integration)

		// Apply the selected game's scene names while preserving legacy fallback scenes.
		mSceneMap[0] = SQSettings.GetConfiguredSceneName("loading", mSceneMap[0]);
		mSceneMap[2] = SQSettings.GetConfiguredSceneName("version_check", mSceneMap[2]);
		mSceneMap[4] = SQSettings.GetConfiguredSceneName("connect", mSceneMap[4]);
		mSceneMap[5] = SQSettings.GetConfiguredSceneName("frontend", mSceneMap[5]);
		mSceneMap[6] = SQSettings.GetConfiguredSceneName("battle", mSceneMap[6]);

<<<<<<< HEAD
=======

>>>>>>> ab1e8e11 (Fix SQSettings server mode integration)
		// CWK 002 does not use the legacy low-resolution scene names.
		if (SQSettings.GAME_MODE_ID != "cwk_0001")
		{
			mLowResSceneMap[0] = string.Empty;
			mLowResSceneMap[2] = string.Empty;
			mLowResSceneMap[4] = string.Empty;
			mLowResSceneMap[5] = string.Empty;
			mLowResSceneMap[6] = string.Empty;
		}

		mCurrentScene = GetSceneFromName(SceneManager.GetActiveScene().name);
	}

	public ReturnLocation GetReturnLocation()
	{
		return mReturnLocation;
	}

	public void ClearReturnLocation()
	{
		mReturnLocation = ReturnLocation.NotSet;
	}

	public void LoadFrontEndScene(ReturnLocation locationToReturnTo = ReturnLocation.Town)
	{
		mReturnLocation = locationToReturnTo;
		LoadScene(Scene.FrontEnd);
	}

	public void LoadBattleScene()
	{
		if (Singleton<PlayerInfoScript>.Instance.StateData.MultiplayerMode)
		{
			Singleton<PVPPrepScreenController>.Instance.syncTime = 0f;
			Singleton<PVPPrepScreenController>.Instance.matchFound = false;
		}
		LoadScene(Scene.Battle);
	}

	public void LoadVersionCheckScene()
	{
		LoadScene(Scene.VersionCheck);
	}

	public void LoadBootVideoSceneDirect()
	{
		LoadScene(Scene.BootVideoScene, bypass: true);
	}

	public void LoadVersionCheckSceneDirect()
	{
		LoadScene(Scene.VersionCheck, bypass: true);
	}

	public void LoadAssetBundleSceneDirect()
	{
		LoadScene(Scene.AssetBundleDownload, bypass: true);
	}

	public void LoadConnectSceneDirect()
	{
		LoadScene(Scene.Connect, bypass: true);
	}

	private void LoadScene(Scene scene, bool bypass = false)
	{
		mPreviousScene = mCurrentScene;
		mCurrentScene = scene;
		MenuStackManager.ClearStack();
		UICamera.ForceUnlockInput();
		DoManualAnimationCleanup();
		if (!bypass)
		{
			SceneManager.LoadScene(GetNameFromScene(Scene.Loading));
		}
		else
		{
			SceneManager.LoadScene(GetNameFromScene(mCurrentScene));
		}
	}

	public void OnLoadingScreenLoaded()
	{
	}

	private Scene GetSceneFromName(string name)
	{
		for (int i = 0; i < mSceneMap.Length; i++)
		{
			if (mSceneMap[i] == name)
			{
				return (Scene)i;
			}
		}
		return Scene.None;
	}

	public string GetNameFromScene(Scene scene)
	{
		if (scene == Scene.SceneCount || scene == Scene.None)
		{
			return null;
		}

		int index = (int)scene;
		if (index < 0 || index >= mSceneMap.Length)
		{
			return null;
		}

		string result = mSceneMap[index];
		string sceneKey = null;
		switch (scene)
		{
		case Scene.Loading: sceneKey = "loading"; break;
		case Scene.VersionCheck: sceneKey = "version_check"; break;
		case Scene.Connect: sceneKey = "connect"; break;
		case Scene.FrontEnd: sceneKey = "frontend"; break;
		case Scene.Battle: sceneKey = "battle"; break;
		}

		// Resolve configurable scenes at load time so changing modes cannot leave stale scene names.
		if (!string.IsNullOrEmpty(sceneKey))
		{
			result = SQSettings.GetConfiguredSceneName(sceneKey, result);
		}

<<<<<<< HEAD
		if (scene == Scene.ChooseGame)
		{
			result = "ChooseGame";
		}

		if (SQSettings.GAME_MODE_ID != "cwk_0001" || scene == Scene.ChooseGame)
=======

		if (SQSettings.GAME_MODE_ID != "cwk_0001")
>>>>>>> ab1e8e11 (Fix SQSettings server mode integration)
		{
			return result;
		}

		if (KFFLODManager.IsLowEndDevice() && !string.IsNullOrEmpty(mLowResSceneMap[index]))
		{
			result = mLowResSceneMap[index];
		}
		return result;
	}

	public bool InBattleScene()
	{
		return mCurrentScene == Scene.Battle;
	}

	public bool InFrontEnd()
	{
		return mCurrentScene == Scene.FrontEnd;
	}

	public Scene GetCurrentScene()
	{
		return mCurrentScene;
	}

	public static void DoManualAnimationCleanup()
	{
		UnityEngine.Object[] array = UnityEngine.Object.FindObjectsOfType(typeof(Animation));
		for (int i = 0; i < array.Length; i++)
		{
			Animation animation = (Animation)array[i];
			animation.Stop();
			List<string> list = new List<string>();
			foreach (AnimationState item in animation)
			{
				list.Add(item.clip.name);
			}
			foreach (string item2 in list)
			{
				try
				{
					if (animation.GetClip(item2) != null)
					{
						animation.RemoveClip(item2);
					}
				}
				catch (Exception)
				{
				}
			}
		}
	}
}