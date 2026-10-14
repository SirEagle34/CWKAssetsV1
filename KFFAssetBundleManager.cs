using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using JsonFx.Json;
using UnityEngine;
using UnityEngine.Networking;

public class KFFAssetBundleManager : Singleton<KFFAssetBundleManager>
{
	private class AssetInfo
	{
		public string extension;

		public AssetBundle assetBundle;

		public string assetName;
	}

	private struct BundlePatch
	{
		public string File;

		public Hash128 Hash;
	}

	private static AssetBundleManifest mainManifest = null;

	private bool mainManifestLoading;

	private static Dictionary<string, AssetInfo> assetInfoDict = new Dictionary<string, AssetInfo>();

	private static List<AssetBundle> loadedAssetBundles = new List<AssetBundle>();

	private static Dictionary<string, AssetBundle> assetBundleDict = new Dictionary<string, AssetBundle>();

	private static readonly HashSet<string> loadingBundles = new HashSet<string>();

	private UnityWebRequest activeRequest;

	private const int CACHE_DOWNLOAD_ATTEMPTS = 3;

	private int getExtensionsCount;

	private int getExtensionsProgress;

	private float loadProgress;

	public bool pauseTest;

	public const string NotInManifestError = "bundle-not-in-manifest";

	public const string OfflinePrefix = "assets/resourcesoffline/";

	public static string BundleResourcesPrefix = "assets/resources/";

	private static string sCapturedUnityError;

	private static Dictionary<string, BundlePatch> sPatches;

	private static bool sPatchesStarted;

	private static bool sPatchesReady;

	private static Dictionary<string, long> sBundleSizes;

	private static bool sSizesRequested;

	public bool IsBusy { get; private set; }

	public static long DownloadedBytes { get; private set; }

	public static bool HasBundleSizes => sBundleSizes != null;

	public IEnumerator IndexExternalBundle(AssetBundle assetBundle, string assetBundleName)
	{
		return GetExtensionsFromAssetBundle(assetBundle, assetBundleName);
	}

	private IEnumerator GetExtensionsFromAssetBundle(AssetBundle assetBundle, string assetBundleName)
	{
		if (loadedAssetBundles.Contains(assetBundle))
		{
			yield break;
		}
		loadedAssetBundles.Add(assetBundle);
		assetBundleDict[assetBundleName.ToLower()] = assetBundle;
		string[] allAssetNames = assetBundle.GetAllAssetNames();
		getExtensionsCount = allAssetNames.Length;
		getExtensionsProgress = 0;
		float realtimeSinceStartup = Time.realtimeSinceStartup;
		float interval = 1f / 60f;
		string[] array = allAssetNames;
		string[] array2 = array;
		foreach (string text in array2)
		{
			string extension = Path.GetExtension(text);
			string text2 = text.Substring(0, text.Length - extension.Length);
			AssetInfo value = new AssetInfo
			{
				extension = extension,
				assetBundle = assetBundle,
				assetName = text
			};
			assetInfoDict[text2] = value;
			if (text2.StartsWith("assets/resourcesoffline/"))
			{
				string key = "assets/resources/" + text2.Substring("assets/resourcesoffline/".Length);
				assetInfoDict[key] = value;
			}
			if (Time.realtimeSinceStartup - realtimeSinceStartup >= interval)
			{
				yield return null;
				realtimeSinceStartup = Time.realtimeSinceStartup;
			}
			getExtensionsProgress++;
		}
		getExtensionsCount = 0;
	}

	private static void OnUnityLog(string condition, string stackTrace, LogType type)
	{
		if ((type == LogType.Error || type == LogType.Exception || type == LogType.Assert) && !string.IsNullOrEmpty(condition) && sCapturedUnityError == null)
		{
			sCapturedUnityError = condition;
		}
	}

	private static AssetBundle OpenBundleCapturingError(UnityWebRequest uwr, out string unityError)
	{
		sCapturedUnityError = null;
		Application.logMessageReceived += OnUnityLog;
		AssetBundle result = null;
		try
		{
			result = DownloadHandlerAssetBundle.GetContent(uwr);
		}
		catch (Exception ex)
		{
			sCapturedUnityError = ex.GetType().Name + ": " + ex.Message;
		}
		finally
		{
			Application.logMessageReceived -= OnUnityLog;
		}
		unityError = sCapturedUnityError;
		sCapturedUnityError = null;
		return result;
	}

	public IEnumerator LoadAssetBundleCoroutine(string assetBundleBaseURL, string assetBundleName, Action<bool, string, AssetBundle> doneCallback, bool resolveDependencies = true)
	{
		string bundleKey = assetBundleName.ToLower();
		while (loadingBundles.Contains(bundleKey))
		{
			yield return null;
		}
		AssetBundle assetBundleByName = GetAssetBundleByName(assetBundleName);
		if (assetBundleByName != null)
		{
			doneCallback?.Invoke(arg1: true, null, assetBundleByName);
			yield break;
		}
		loadingBundles.Add(bundleKey);
		IsBusy = true;
		loadProgress = 0f;
		bool success = false;
		string errMsg = null;
		AssetBundle retBundle = null;
		if (!string.IsNullOrEmpty(assetBundleBaseURL))
		{
			string basePlatformURL2 = ResolveBaseURL(assetBundleBaseURL);
			yield return StartCoroutine(LoadMainManifest(basePlatformURL2));
			if (mainManifest != null)
			{
				while (!Caching.ready)
				{
					yield return null;
				}
				Hash128 hash = BundleHash(assetBundleName);
				if (!hash.isValid)
				{
					errMsg = "bundle-not-in-manifest: " + assetBundleName;
					loadingBundles.Remove(bundleKey);
					doneCallback?.Invoke(arg1: false, errMsg, null);
					IsBusy = false;
					yield break;
				}
				if (resolveDependencies)
				{
					string[] deps = mainManifest.GetAllDependencies(assetBundleName);
					foreach (string dep in deps)
					{
						if (string.IsNullOrEmpty(dep) || dep.ToLower() == bundleKey || GetAssetBundleByName(dep) != null)
						{
							continue;
						}
						string depErr = null;
						yield return StartCoroutine(LoadAssetBundleCoroutine(assetBundleBaseURL, dep, delegate(bool depOk, string err, AssetBundle unused)
						{
							if (!depOk)
							{
								depErr = err;
							}
						}, resolveDependencies: false));
						if (depErr == null)
						{
							Debug.Log("[Bundles] loaded dependency '" + dep + "' for '" + assetBundleName + "'");
						}
						if (depErr != null)
						{
							Debug.LogWarning("[Bundles] dependency '" + dep + "' of '" + assetBundleName + "' failed to load: " + depErr);
						}
					}
				}
				string uri = basePlatformURL2 + BundleFileName(assetBundleName);
				UnityWebRequest uwr = (activeRequest = UnityWebRequestAssetBundle.GetAssetBundle(uri, hash));
				yield return uwr.SendWebRequest();
				activeRequest = null;
				CountDownloaded(uwr);
				loadProgress = 1f;
				bool flag = uwr.result == UnityWebRequest.Result.Success;
				string unityError = null;
				AssetBundle assetBundle = (flag ? OpenBundleCapturingError(uwr, out unityError) : null);
				if (!flag || assetBundle == null)
				{
					errMsg = (flag ? ("failed to open (" + uwr.downloadedBytes + "B)\n" + (unityError ?? "no reason logged")) : uwr.error);
				}
				else
				{
					success = true;
					retBundle = assetBundle;
					yield return StartCoroutine(GetExtensionsFromAssetBundle(assetBundle, assetBundleName));
				}
			}
		}
		loadingBundles.Remove(bundleKey);
		doneCallback?.Invoke(success, errMsg, retBundle);
		while (pauseTest)
		{
			yield return null;
		}
		IsBusy = false;
	}

	public static string PlatformFolder()
	{
		switch (Application.platform)
		{
		case RuntimePlatform.Android:
			return "Android";
		case RuntimePlatform.IPhonePlayer:
			return "iOS";
		case RuntimePlatform.OSXEditor:
		case RuntimePlatform.OSXPlayer:
			return "Mac";
		default:
			return "Windows";
		}
	}

	public static string BundleFileName(string assetBundleName)
	{
		if (sPatches != null && sPatches.TryGetValue(assetBundleName.ToLower(), out var value))
		{
			return value.File;
		}
		return assetBundleName.ToLower().Replace(' ', '.');
	}

	private Hash128 BundleHash(string assetBundleName)
	{
		if (sPatches != null && sPatches.TryGetValue(assetBundleName.ToLower(), out var value))
		{
			return value.Hash;
		}
		return mainManifest.GetAssetBundleHash(assetBundleName);
	}

	private IEnumerator LoadBundlePatches(string basePlatformURL)
	{
		using UnityWebRequest req = UnityWebRequest.Get(basePlatformURL + "bundle_patches.json");
		yield return req.SendWebRequest();
		if (req.result != UnityWebRequest.Result.Success)
		{
			yield break;
		}
		try
		{
			Dictionary<string, object> dictionary = JsonReader.Deserialize<Dictionary<string, object>>(req.downloadHandler.text);
			Dictionary<string, BundlePatch> dictionary2 = new Dictionary<string, BundlePatch>();
			foreach (KeyValuePair<string, object> item in dictionary)
			{
				if (item.Value is Dictionary<string, object> dictionary3 && dictionary3.ContainsKey("file") && dictionary3.ContainsKey("hash"))
				{
					Hash128 hash = Hash128.Parse(dictionary3["hash"].ToString());
					if (hash.isValid)
					{
						dictionary2[item.Key.ToLower()] = new BundlePatch
						{
							File = dictionary3["file"].ToString(),
							Hash = hash
						};
						string[] obj = new string[6]
						{
							"[Bundles] patch: ",
							item.Key,
							" -> ",
							dictionary3["file"]?.ToString(),
							" ",
							null
						};
						Hash128 hash2 = hash;
						obj[5] = hash2.ToString();
						Debug.Log(string.Concat(obj));
					}
				}
			}
			sPatches = dictionary2;
		}
		catch (Exception ex)
		{
			Debug.LogWarning("[Bundles] bad bundle_patches.json, ignored: " + ex.Message);
		}
	}

	public bool IsBundleCached(string assetBundleBaseURL, string assetBundleName)
	{
		if (mainManifest == null || !sPatchesReady || !Caching.ready || string.IsNullOrEmpty(assetBundleBaseURL))
		{
			return false;
		}
		Hash128 hash = BundleHash(assetBundleName);
		if (hash.isValid)
		{
			return Caching.IsVersionCached(ResolveBaseURL(assetBundleBaseURL) + BundleFileName(assetBundleName), hash);
		}
		return false;
	}

	public static string ResolveBaseURL(string configuredBase)
	{
		string text = PlatformFolder();
		if (configuredBase.Contains("{PLATFORM}"))
		{
			return configuredBase.Replace("{PLATFORM}", text);
		}
		return configuredBase + text + "/";
	}

	private IEnumerator LoadMainManifest(string basePlatformURL)
	{
		while (mainManifestLoading)
		{
			yield return null;
		}
		if (mainManifest == null)
		{
			mainManifestLoading = true;
			string url = basePlatformURL + PlatformFolder();
			WWW www = new WWW(url);
			yield return www;
			if (www.error == null && www.assetBundle != null)
			{
				mainManifest = www.assetBundle.LoadAsset("AssetBundleManifest") as AssetBundleManifest;
			}
			mainManifestLoading = false;
		}
		if (!sPatchesStarted)
		{
			sPatchesStarted = true;
			yield return StartCoroutine(LoadBundlePatches(basePlatformURL));
			sPatchesReady = true;
		}
		while (!sPatchesReady)
		{
			yield return null;
		}
		if (!sSizesRequested)
		{
			sSizesRequested = true;
			StartCoroutine(LoadBundleSizes(basePlatformURL));
		}
	}

	private static void CountDownloaded(UnityWebRequest uwr)
	{
		if (uwr.result == UnityWebRequest.Result.Success)
		{
			DownloadedBytes += (long)uwr.downloadedBytes;
		}
	}

	public long ActiveDownloadedBytes()
	{
		if (activeRequest == null)
		{
			return 0L;
		}
		return (long)activeRequest.downloadedBytes;
	}

	public static long BundleSize(string assetBundleName)
	{
		if (sBundleSizes == null || !sBundleSizes.TryGetValue(BundleFileName(assetBundleName), out var value))
		{
			return 0L;
		}
		return value;
	}

	private IEnumerator LoadBundleSizes(string basePlatformURL)
	{
		using UnityWebRequest req = UnityWebRequest.Get(basePlatformURL + "bundle_sizes.json");
		yield return req.SendWebRequest();
		if (req.result != UnityWebRequest.Result.Success)
		{
			Debug.LogWarning("[Bundles] no bundle_sizes.json (" + req.error + "); download bar shows percent only");
			yield break;
		}
		try
		{
			Dictionary<string, object> dictionary = JsonReader.Deserialize<Dictionary<string, object>>(req.downloadHandler.text);
			Dictionary<string, long> dictionary2 = new Dictionary<string, long>();
			foreach (KeyValuePair<string, object> item in dictionary)
			{
				dictionary2[item.Key.ToLower()] = Convert.ToInt64(item.Value);
			}
			sBundleSizes = dictionary2;
		}
		catch (Exception ex)
		{
			Debug.LogWarning("[Bundles] bad bundle_sizes.json: " + ex.Message);
		}
	}

	public string GetResourceExtension(string resourcePath)
	{
		if (assetInfoDict != null && !string.IsNullOrEmpty(resourcePath))
		{
			resourcePath = resourcePath.ToLower();
			if (assetInfoDict.ContainsKey(resourcePath))
			{
				return assetInfoDict[resourcePath].extension;
			}
		}
		return null;
	}

	public string GetBundleAssetName(string resourcePath)
	{
		if (assetInfoDict != null && !string.IsNullOrEmpty(resourcePath) && assetInfoDict.TryGetValue(resourcePath.ToLower(), out var value))
		{
			return value.assetName;
		}
		return null;
	}

	public AssetBundle GetAssetBundleForResource(string resourcePath)
	{
		if (assetInfoDict != null && !string.IsNullOrEmpty(resourcePath))
		{
			resourcePath = resourcePath.ToLower();
			if (assetInfoDict.ContainsKey(resourcePath))
			{
				return assetInfoDict[resourcePath].assetBundle;
			}
		}
		return null;
	}

	public AssetBundle GetAssetBundleForResourcePath(string resourcePath)
	{
		if (assetInfoDict == null || string.IsNullOrEmpty(resourcePath))
		{
			return null;
		}
		string key = (BundleResourcesPrefix + resourcePath.TrimStart('/')).ToLower();
		if (!assetInfoDict.TryGetValue(key, out var value))
		{
			return null;
		}
		return value.assetBundle;
	}

	public List<string> GetResourcePathsUnder(string folderPath)
	{
		List<string> list = new List<string>();
		if (assetInfoDict == null || string.IsNullOrEmpty(folderPath))
		{
			return list;
		}
		string value = (BundleResourcesPrefix + folderPath.TrimStart('/').TrimEnd('/') + "/").ToLower();
		foreach (KeyValuePair<string, AssetInfo> item in assetInfoDict)
		{
			if (item.Key.StartsWith(value))
			{
				list.Add(item.Key + item.Value.extension);
			}
		}
		return list;
	}

	public AssetBundle GetAssetBundleByName(string assetBundleName)
	{
		assetBundleName = assetBundleName.ToLower();
		if (assetBundleDict != null && !string.IsNullOrEmpty(assetBundleName) && assetBundleDict.ContainsKey(assetBundleName))
		{
			return assetBundleDict[assetBundleName];
		}
		return null;
	}

	public static string[] GetAllBundleNames()
	{
		if (!(mainManifest != null))
		{
			return new string[0];
		}
		return mainManifest.GetAllAssetBundles();
	}

	public IEnumerator CacheAssetBundleCoroutine(string assetBundleBaseURL, string assetBundleName, Action<bool, string> doneCallback)
	{
		IsBusy = true;
		loadProgress = 0f;
		bool success = false;
		string errMsg = null;
		string basePlatformURL = ResolveBaseURL(assetBundleBaseURL);
		yield return StartCoroutine(LoadMainManifest(basePlatformURL));
		if (mainManifest == null)
		{
			errMsg = "no manifest";
		}
		else
		{
			while (!Caching.ready)
			{
				yield return null;
			}
			Hash128 hash = BundleHash(assetBundleName);
			if (!hash.isValid)
			{
				errMsg = "bundle-not-in-manifest: " + assetBundleName;
			}
			else
			{
				string url = basePlatformURL + BundleFileName(assetBundleName);
				for (int attempt = 0; attempt < 3; attempt++)
				{
					UnityWebRequest uwr = (activeRequest = UnityWebRequestAssetBundle.GetAssetBundle(url, hash));
					yield return uwr.SendWebRequest();
					activeRequest = null;
					CountDownloaded(uwr);
					loadProgress = 1f;
					success = uwr.result == UnityWebRequest.Result.Success;
					errMsg = (success ? null : uwr.error);
					uwr.Dispose();
					if (success)
					{
						break;
					}
					if (attempt + 1 < 3)
					{
						yield return new WaitForSeconds(attempt + 1);
					}
				}
			}
		}
		doneCallback?.Invoke(success, errMsg);
		IsBusy = false;
	}

	public float GetActiveDownloadProgress()
	{
		if (activeRequest == null)
		{
			return -1f;
		}
		return activeRequest.downloadProgress;
	}

	public float GetLoadProgress()
	{
		if (activeRequest != null)
		{
			return activeRequest.downloadProgress;
		}
		return loadProgress;
	}

	public float GetAssetBundleInitProgress()
	{
		if (getExtensionsCount <= 0)
		{
			return -1f;
		}
		return (float)getExtensionsProgress / (float)getExtensionsCount;
	}

	public void UnloadAllAssetBundles()
	{
		foreach (AssetBundle loadedAssetBundle in loadedAssetBundles)
		{
			if (loadedAssetBundle != null)
			{
				loadedAssetBundle.Unload(unloadAllLoadedObjects: true);
			}
		}
		loadedAssetBundles.Clear();
		assetBundleDict.Clear();
		mainManifest = null;
	}
}
