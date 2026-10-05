using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class KFFAssetBundleManager : Singleton<KFFAssetBundleManager>
{
	private class AssetInfo
	{
		public string extension;
		public AssetBundle assetBundle;
	}

	private static Dictionary<string, AssetInfo> assetInfoDict =
		new Dictionary<string, AssetInfo>();

	private static List<AssetBundle> loadedAssetBundles =
		new List<AssetBundle>();

	private static Dictionary<string, AssetBundle> assetBundleDict =
		new Dictionary<string, AssetBundle>();

	private WWW activeWWW;

	private int getExtensionsCount;
	private int getExtensionsProgress;

	private float loadProgress;

	public bool pauseTest;

	public bool IsBusy { get; private set; }

	// ============================================================
	// DOWNLOAD INFORMATION
	// ============================================================

	private string activeBundleName = string.Empty;

	private long activeFileBytes;
	private long activeFileTotalBytes;

	private long totalDownloadedBytes;
	private long totalDownloadBytes;

	private float downloadSpeed;
	private float lastSpeedTime;
	private long lastSpeedBytes;

	public string ActiveBundleName
	{
		get { return activeBundleName; }
	}

	public long ActiveFileBytes
	{
		get { return activeFileBytes; }
	}

	public long ActiveFileTotalBytes
	{
		get { return activeFileTotalBytes; }
	}

	public long TotalDownloadedBytes
	{
		get { return totalDownloadedBytes; }
	}

	public long TotalDownloadBytes
	{
		get { return totalDownloadBytes; }
	}

	public float DownloadSpeed
	{
		get { return downloadSpeed; }
	}

	// ============================================================
	// NORMALIZE
	// ============================================================

	private string NormalizeBundleName(string bundleName)
	{
		if (string.IsNullOrEmpty(bundleName))
		{
			return null;
		}

		return bundleName
			.Replace("\\", "/")
			.Trim('/')
			.ToLowerInvariant();
	}

	private string NormalizeResourcePath(string resourcePath)
	{
		if (string.IsNullOrEmpty(resourcePath))
		{
			return null;
		}

		return resourcePath
			.Replace("\\", "/")
			.ToLowerInvariant();
	}

	// ============================================================
	// CACHE
	// ============================================================

	private bool IsBundleCached(
		string url,
		int version)
	{
		if (string.IsNullOrEmpty(url))
		{
			return false;
		}

		try
		{
			return Caching.IsVersionCached(
				url,
				version
			);
		}
		catch (Exception ex)
		{
			Debug.LogWarning(
				"[KFFAssetBundleManager] " +
				"Could not check Unity cache.\n" +
				"URL: " + url +
				"\nError: " + ex
			);

			return false;
		}
	}

	private void LogCacheState(
		string bundleName,
		string url,
		int version)
	{
		bool cached =
			IsBundleCached(
				url,
				version
			);

		Debug.Log(
			"[KFFAssetBundleManager] CACHE CHECK\n" +
			"Bundle: " + bundleName +
			"\nVersion: " + version +
			"\nCached: " + cached +
			"\nURL: " + url
		);
	}

	// ============================================================
	// DOWNLOAD STATS
	// ============================================================

	private void ResetDownloadStats(
		string bundleName)
	{
		activeBundleName =
			bundleName;

		activeFileBytes = 0;
		activeFileTotalBytes = 0;

		downloadSpeed = 0f;

		lastSpeedTime =
			Time.realtimeSinceStartup;

		lastSpeedBytes = 0;
	}

	private void UpdateDownloadStats(
		WWW www)
	{
		if (www == null)
		{
			return;
		}

		activeFileBytes =
			www.bytesDownloaded;

		long contentLength = 0;

		try
		{
			Dictionary<string, string> headers =
				www.responseHeaders;

			if (headers != null)
			{
				string value = null;

				if (headers.ContainsKey(
					"CONTENT-LENGTH"))
				{
					value =
						headers["CONTENT-LENGTH"];
				}
				else if (headers.ContainsKey(
					"Content-Length"))
				{
					value =
						headers["Content-Length"];
				}

				if (!string.IsNullOrEmpty(value))
				{
					long.TryParse(
						value,
						out contentLength
					);
				}
			}
		}
		catch
		{
			contentLength = 0;
		}

		if (contentLength > 0)
		{
			activeFileTotalBytes =
				contentLength;
		}

		float now =
			Time.realtimeSinceStartup;

		float elapsed =
			now - lastSpeedTime;

		if (elapsed >= 0.5f)
		{
			long deltaBytes =
				activeFileBytes -
				lastSpeedBytes;

			if (deltaBytes >= 0)
			{
				downloadSpeed =
					deltaBytes / elapsed;
			}

			lastSpeedBytes =
				activeFileBytes;

			lastSpeedTime =
				now;
		}
	}

	// ============================================================
	// ASSET INFO
	// ============================================================

	private IEnumerator GetExtensionsFromAssetBundle(
		AssetBundle assetBundle,
		string assetBundleName)
	{
		if (assetBundle == null)
		{
			yield break;
		}

		assetBundleName =
			NormalizeBundleName(
				assetBundleName
			);

		if (loadedAssetBundles.Contains(
			assetBundle))
		{
			yield break;
		}

		loadedAssetBundles.Add(
			assetBundle
		);

		if (!string.IsNullOrEmpty(
			assetBundleName))
		{
			assetBundleDict[
				assetBundleName
			] = assetBundle;
		}

		string[] assetNames =
			assetBundle.GetAllAssetNames();

		getExtensionsCount =
			assetNames.Length;

		getExtensionsProgress = 0;

		float timestamp =
			Time.realtimeSinceStartup;

		float interval =
			1f / 60f;

		foreach (string assetName in assetNames)
		{
			if (string.IsNullOrEmpty(
				assetName))
			{
				getExtensionsProgress++;
				continue;
			}

			string normalizedAssetName =
				NormalizeResourcePath(
					assetName
				);

			string extension =
				Path.GetExtension(
					normalizedAssetName
				);

			string pathWithoutExtension;

			if (!string.IsNullOrEmpty(
				extension))
			{
				pathWithoutExtension =
					normalizedAssetName.Substring(
						0,
						normalizedAssetName.Length -
						extension.Length
					);
			}
			else
			{
				pathWithoutExtension =
					normalizedAssetName;
			}

			AssetInfo info =
				new AssetInfo();

			info.extension =
				extension;

			info.assetBundle =
				assetBundle;

			assetInfoDict[
				pathWithoutExtension
			] = info;

			if (
				Time.realtimeSinceStartup -
				timestamp >= interval)
			{
				yield return null;

				timestamp =
					Time.realtimeSinceStartup;
			}

			getExtensionsProgress++;
		}

		getExtensionsCount = 0;
		getExtensionsProgress = 0;
	}

	// ============================================================
	// MAIN LOAD
	// ============================================================

	public IEnumerator LoadAssetBundleCoroutine(
		string assetBundleBaseURL,
		string assetBundleName,
		Action<bool, string, AssetBundle> doneCallback)
	{
		IsBusy = true;

		loadProgress = 0f;

		bool success = false;
		string errMsg = null;
		AssetBundle retBundle = null;

		assetBundleName =
			NormalizeBundleName(
				assetBundleName
			);

		if (string.IsNullOrEmpty(
			assetBundleBaseURL))
		{
			errMsg =
				"AssetBundle base URL is empty.";
		}
		else if (string.IsNullOrEmpty(
			assetBundleName))
		{
			errMsg =
				"AssetBundle name is empty.";
		}
		else
		{
			string baseURL =
				assetBundleBaseURL
					.TrimEnd('/') +
				"/";

			yield return StartCoroutine(
				LoadSingleAssetBundle(
					baseURL,
					assetBundleName,
					(successResult,
						errorResult,
						bundleResult) =>
					{
						success =
							successResult;

						errMsg =
							errorResult;

						retBundle =
							bundleResult;
					}
				)
			);
		}

		if (doneCallback != null)
		{
			doneCallback(
				success,
				errMsg,
				retBundle
			);
		}

		while (pauseTest)
		{
			yield return null;
		}

		IsBusy = false;
	}

	// ============================================================
	// LOAD SINGLE BUNDLE
	// ============================================================

	private IEnumerator LoadSingleAssetBundle(
		string baseURL,
		string assetBundleName,
		Action<bool, string, AssetBundle> doneCallback)
	{
		assetBundleName =
			NormalizeBundleName(
				assetBundleName
			);

		// --------------------------------------------------------
		// 1. RAM CACHE
		// --------------------------------------------------------

		AssetBundle existingBundle =
			GetAssetBundleByName(
				assetBundleName
			);

		if (existingBundle != null)
		{
			Debug.Log(
				"[KFFAssetBundleManager] " +
				"RAM CACHE HIT: " +
				assetBundleName
			);

			if (doneCallback != null)
			{
				doneCallback(
					true,
					null,
					existingBundle
				);
			}

			yield break;
		}

		// --------------------------------------------------------
		// 2. UNITY DISK CACHE
		// --------------------------------------------------------

		string url =
			baseURL +
			assetBundleName;

		const int cacheVersion = 0;

		LogCacheState(
			assetBundleName,
			url,
			cacheVersion
		);

		bool cached =
			IsBundleCached(
				url,
				cacheVersion
			);

		if (cached)
		{
			Debug.Log(
				"[KFFAssetBundleManager] " +
				"DISK CACHE HIT: " +
				assetBundleName
			);
		}
		else
		{
			Debug.Log(
				"[KFFAssetBundleManager] " +
				"DISK CACHE MISS. " +
				"Downloading: " +
				assetBundleName
			);
		}

		// --------------------------------------------------------
		// 3. LOAD FROM CACHE OR DOWNLOAD
		// --------------------------------------------------------

		ResetDownloadStats(
			assetBundleName
		);

		WWW www =
			WWW.LoadFromCacheOrDownload(
				url,
				cacheVersion
			);

		activeWWW =
			www;

		while (!www.isDone)
		{
			UpdateDownloadStats(
				www
			);

			yield return null;
		}

		UpdateDownloadStats(
			www
		);

		activeWWW = null;

		loadProgress = 1f;

		// --------------------------------------------------------
		// BYTE STATS
		// --------------------------------------------------------

		if (activeFileTotalBytes > 0)
		{
			activeFileBytes =
				activeFileTotalBytes;
		}

		if (!cached)
		{
			totalDownloadedBytes +=
				activeFileBytes;

			if (activeFileTotalBytes > 0)
			{
				totalDownloadBytes +=
					activeFileTotalBytes;
			}
		}

		// --------------------------------------------------------
		// DOWNLOAD / CACHE ERROR
		// --------------------------------------------------------

		if (www.error != null)
		{
			Debug.LogError(
				"[KFFAssetBundleManager] " +
				"AssetBundle LOAD FAILED\n" +
				"URL: " + url +
				"\nERROR: " + www.error
			);

			if (doneCallback != null)
			{
				doneCallback(
					false,
					"AssetBundle load failed: " +
					url +
					"\n" +
					www.error,
					null
				);
			}

			yield break;
		}

		// --------------------------------------------------------
		// BUNDLE NULL
		// --------------------------------------------------------

		if (www.assetBundle == null)
		{
			Debug.LogError(
				"[KFFAssetBundleManager] " +
				"AssetBundle is NULL\n" +
				"URL: " + url
			);

			if (doneCallback != null)
			{
				doneCallback(
					false,
					"AssetBundle is null: " +
					url,
					null
				);
			}

			yield break;
		}

		// --------------------------------------------------------
		// REGISTER ASSET INFORMATION
		// --------------------------------------------------------

		yield return StartCoroutine(
			GetExtensionsFromAssetBundle(
				www.assetBundle,
				assetBundleName
			)
		);

		Debug.Log(
			"[KFFAssetBundleManager] " +
			"AssetBundle READY: " +
			assetBundleName +
			(cached
				? " [CACHE]"
				: " [DOWNLOADED]")
		);

		if (doneCallback != null)
		{
			doneCallback(
				true,
				null,
				www.assetBundle
			);
		}
	}

	// ============================================================
	// RESOURCE LOOKUP
	// ============================================================

	public string GetResourceExtension(
		string resourcePath)
	{
		if (
			assetInfoDict == null ||
			string.IsNullOrEmpty(resourcePath))
		{
			return null;
		}

		resourcePath =
			NormalizeResourcePath(
				resourcePath
			);

		AssetInfo info;

		if (
			assetInfoDict.TryGetValue(
				resourcePath,
				out info))
		{
			return info.extension;
		}

		return null;
	}

	public AssetBundle GetAssetBundleForResource(
		string resourcePath)
	{
		if (
			assetInfoDict == null ||
			string.IsNullOrEmpty(resourcePath))
		{
			return null;
		}

		resourcePath =
			NormalizeResourcePath(
				resourcePath
			);

		AssetInfo info;

		if (
			assetInfoDict.TryGetValue(
				resourcePath,
				out info))
		{
			return info.assetBundle;
		}

		return null;
	}

	public AssetBundle GetAssetBundleByName(
		string assetBundleName)
	{
		if (string.IsNullOrEmpty(
			assetBundleName))
		{
			return null;
		}

		assetBundleName =
			NormalizeBundleName(
				assetBundleName
			);

		AssetBundle bundle;

		if (
			assetBundleDict.TryGetValue(
				assetBundleName,
				out bundle))
		{
			return bundle;
		}

		return null;
	}

	// ============================================================
	// STATUS
	// ============================================================

	public WWW GetActiveWWW()
	{
		return activeWWW;
	}

	public float GetLoadProgress()
	{
		if (activeWWW != null)
		{
			return activeWWW.progress;
		}

		return loadProgress;
	}

	public float GetAssetBundleInitProgress()
	{
		if (getExtensionsCount <= 0)
		{
			return -1f;
		}

		return
			(float)getExtensionsProgress /
			(float)getExtensionsCount;
	}

	// ============================================================
	// UNLOAD
	// ============================================================

	public void UnloadAllAssetBundles()
	{
		foreach (
			AssetBundle loadedAssetBundle
			in loadedAssetBundles)
		{
			if (loadedAssetBundle != null)
			{
				loadedAssetBundle.Unload(true);
			}
		}

		loadedAssetBundles.Clear();

		assetBundleDict.Clear();

		assetInfoDict.Clear();

		activeWWW = null;

		getExtensionsCount = 0;
		getExtensionsProgress = 0;

		loadProgress = 0f;

		activeBundleName =
			string.Empty;

		activeFileBytes = 0;
		activeFileTotalBytes = 0;

		totalDownloadedBytes = 0;
		totalDownloadBytes = 0;

		downloadSpeed = 0f;

		lastSpeedTime = 0f;
		lastSpeedBytes = 0;

		IsBusy = false;
	}
}