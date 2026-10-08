using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using MiniJSON;
using UnityEngine;

public class SLOTResourceManager : Singleton<SLOTResourceManager>
{
	private class LoadAssetBundleQueueInfo
	{
		public string path;

		public Type t;

		public string assetBundleName;

		public Action<object> loadAssetBundleCallback;

		public bool async;
	}

	private class QueuedResourceLoad
	{
		public string AssetBundle;

		public string AssetPath;

		public Action<UnityEngine.Object> Callback;

		public bool PreloadOnly;

		public bool BackgroundLoad;

		public bool SilentFail;

		public bool UseHighResLowRes;
	}

	private class QueuedTextureLoad
	{
		public string AssetBundle;

		public string AssetPath;

		public UITexture TextureObject;
	}

	public enum PreloadBundlesPoint
	{
		IntroBattle = 0,
		Q1 = 1,
		Q2 = 2
	}

	public const string MainResourcesBundle = "MainResourcesBundle";

	public const string MainScenesBundle = "MainScenesBundle";

	public const string GeneralUIBundle = "GeneralBundle";

	public const string FtueUIBundle = "FTUEBundle";

	public const string FtueAudioBundle = "FTUEAudioBundle";

	public const string MainAudioBundle = "MainAudioBundle";

	private const string LOCAL_TEXTURE_CACHE_DIRECTORY = "TextureCache";

	private const string ETAG_EXTENSION = ".DragonWarsEtag";

	private const int MAX_RETRY_COUNT = 3;

	public const string CreaturePortraitPlaceholder = "UI/UI/LoadingPlaceholder";

	public const string CardArtPlaceholder = "UI/UI/LoadingPlaceholder";

	public const string LeagueBannerPlaceholder = "UI/UI/LoadingPlaceholder";

	public const string HeroPortraitPlaceholder = "UI/UI/LoadingPlaceholder";

	public List<string> BundlesToLoadUpFront = new List<string>();

	public string resourcePathPrefix_hires;

	public string resourcePathPrefix_lores;

	public List<UIAtlas> UiAtlases;

	public UIAtlas lowResAtlas;

	private List<AssetBundle> assetBundles = new List<AssetBundle>();

	private static AssetBundle mResourcesBundle;

	private static AssetBundle mScenesBundle;

	private List<LoadAssetBundleQueueInfo> loadAssetBundleQueue = new List<LoadAssetBundleQueueInfo>();

	private List<QueuedResourceLoad> mQueuedResourceLoads = new List<QueuedResourceLoad>();

	private Coroutine mQueuedResourceLoadCoroutine;

	private int mBundleIndex;

	private int mTotalBundles = -1;

	private float mInProgressDownload = -1f;

	private bool mCurrentlyBackgroundLoading;

	private List<QueuedTextureLoad> mQueuedTextureLoads = new List<QueuedTextureLoad>();

	private Coroutine mQueuedTextureLoadCoroutine;

	public const string DefaultResourcePathPrefix = "Assets/ResourcesOffline/";

	private bool mUseAssetBundles;

	public string assetBundleBaseURL { get; set; }

	public string resourcePathPrefix { get; set; }

	public bool ResourceLoadInProgress => mQueuedResourceLoadCoroutine != null;

	private void Start()
	{
		GetBaseURLs();
		string text = ((!KFFLODManager.IsLowEndDevice()) ? resourcePathPrefix_hires : resourcePathPrefix_lores);
		if (string.IsNullOrEmpty(text) || text.Contains("_hirez") || text.Contains("_lowrez"))
		{
			text = "Assets/ResourcesOffline/";
		}
		resourcePathPrefix = text;
	}

	private void GetBaseURLs()
	{
		_ = string.Empty;
		string streamingAssetsFile = TFUtils.GetStreamingAssetsFile("asset_bundle_settings.json");
		Dictionary<string, object> dictionary = (Dictionary<string, object>)Json.Deserialize((!streamingAssetsFile.Contains("://")) ? File.ReadAllText(streamingAssetsFile) : SQSettings.getJsonPath(streamingAssetsFile));
		assetBundleBaseURL = (string)dictionary["asset_bundle_url"];
		mUseAssetBundles = dictionary.ContainsKey("use_asset_bundles") && Convert.ToBoolean(dictionary["use_asset_bundles"]);
		Debug.Log("[Bundles] use_asset_bundles=" + mUseAssetBundles + " base=" + assetBundleBaseURL);
	}

	public static string GetResourceName(string name)
	{
		return GetResourceName(name, KFFLODManager.IsLowEndDevice());
	}

	public static string GetResourceName(string name, bool lowRes)
	{
		if (lowRes)
		{
			if (name.Contains("low_"))
			{
				return name;
			}
			int num = name.LastIndexOf("/");
			if (num >= 0)
			{
				string text = name.Substring(0, num);
				string text2 = name.Substring(num + 1);
				return text + "/low_" + text2;
			}
			return "low_" + name;
		}
		return name;
	}

	public bool IsHiLoRezResource(string path)
	{
		if (path.Contains("Flags/"))
		{
			return false;
		}
		return true;
	}

	public static UnityEngine.Object Load(string path, Type t = null)
	{
		SLOTResourceManager instance = Singleton<SLOTResourceManager>.Instance;
		if (instance != null)
		{
			return instance.LoadResource(path, t);
		}
		if (!(t == null))
		{
			return Resources.Load(path, t);
		}
		return Resources.Load(path);
	}

	public static T Load<T>(string path) where T : UnityEngine.Object
	{
		return Load(path, typeof(T)) as T;
	}

	public static T[] LoadAll<T>(string folderPath) where T : UnityEngine.Object
	{
		SLOTResourceManager instance = Singleton<SLOTResourceManager>.Instance;
		if (!(instance != null))
		{
			return Resources.LoadAll<T>(folderPath);
		}
		return instance.LoadAllResources<T>(folderPath);
	}

	public T LoadResource<T>(string path) where T : UnityEngine.Object
	{
		return LoadResource(path, typeof(T)) as T;
	}

	public T[] LoadAllResources<T>(string folderPath) where T : UnityEngine.Object
	{
		if (IsUsingAssetBundles())
		{
			List<string> resourcePathsUnder = Singleton<KFFAssetBundleManager>.Instance.GetResourcePathsUnder(folderPath);
			if (resourcePathsUnder.Count > 0)
			{
				List<T> list = new List<T>(resourcePathsUnder.Count);
				foreach (string item in resourcePathsUnder)
				{
					AssetBundle assetBundleForResourcePath = Singleton<KFFAssetBundleManager>.Instance.GetAssetBundleForResourcePath(folderPath.TrimEnd('/') + "/" + Path.GetFileNameWithoutExtension(item));
					if (!(assetBundleForResourcePath == null))
					{
						T val = assetBundleForResourcePath.LoadAsset(Path.GetFileNameWithoutExtension(item), typeof(T)) as T;
						if (val != null)
						{
							list.Add(val);
						}
					}
				}
				if (list.Count > 0)
				{
					return list.ToArray();
				}
			}
			Debug.LogWarning("[BundleMiss] LoadAllResources fell back to Resources for folder: " + folderPath);
		}
		return Resources.LoadAll<T>(folderPath);
	}

	public UnityEngine.Object LoadResource(string path, Type t = null)
	{
		path = path.Replace("low_", string.Empty);
		UnityEngine.Object obj = LiveContent.TryLoad(path, t);
		if (obj != null)
		{
			return obj;
		}
		if (IsUsingAssetBundles() && mResourcesBundle != null)
		{
			string text = Path.GetFileNameWithoutExtension(path);
			if (text == string.Empty)
			{
				return null;
			}
			if (IsHiLoRezResource(path))
			{
				text = GetResourceName(text);
			}
			UnityEngine.Object obj2 = ((t == null) ? mResourcesBundle.LoadAsset(text) : mResourcesBundle.LoadAsset(text, t));
			if (obj2 != null)
			{
				return obj2;
			}
		}
		if (IsUsingAssetBundles())
		{
			AssetBundle assetBundleForResourcePath = Singleton<KFFAssetBundleManager>.Instance.GetAssetBundleForResourcePath(path);
			if (assetBundleForResourcePath != null)
			{
				string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(path);
				UnityEngine.Object obj3 = ((t == null) ? assetBundleForResourcePath.LoadAsset(fileNameWithoutExtension) : assetBundleForResourcePath.LoadAsset(fileNameWithoutExtension, t));
				if (obj3 != null)
				{
					return obj3;
				}
			}
		}
		if (KFFLODManager.IsLowEndDevice() && IsHiLoRezResource(path))
		{
			string resourceName = GetResourceName(path);
			UnityEngine.Object obj4 = ((t == null) ? Resources.Load(resourceName) : Resources.Load(resourceName, t));
			if (obj4 != null)
			{
				return obj4;
			}
		}
		UnityEngine.Object obj5 = ((t != null) ? Resources.Load(path, t) : Resources.Load(path));
		if (obj5 == null && IsUsingAssetBundles())
		{
			Debug.LogWarning("[BundleMiss] asset not found in any bundle OR Resources: " + path + ((t != null) ? (" (type " + t.Name + ")") : ""));
		}
		return obj5;
	}

	public void QueueResourceLoad(string path, string assetBundleName, Action<UnityEngine.Object> loadResourceCallback, bool isPreload = false, bool silentFail = false, bool useHighRes = true, bool backgroundLoad = false)
	{
		QueuedResourceLoad queuedResourceLoad = new QueuedResourceLoad();
		queuedResourceLoad.AssetPath = path;
		queuedResourceLoad.AssetBundle = assetBundleName;
		queuedResourceLoad.Callback = loadResourceCallback;
		queuedResourceLoad.PreloadOnly = isPreload;
		if (isPreload)
		{
			queuedResourceLoad.BackgroundLoad = true;
		}
		else
		{
			queuedResourceLoad.BackgroundLoad = backgroundLoad;
		}
		queuedResourceLoad.SilentFail = silentFail;
		queuedResourceLoad.UseHighResLowRes = useHighRes;
		mQueuedResourceLoads.Add(queuedResourceLoad);
		if (mQueuedResourceLoadCoroutine == null)
		{
			mQueuedResourceLoadCoroutine = StartCoroutine(LoadResourceCoroutine());
		}
	}

	private IEnumerator LoadResourceCoroutine()
	{
		while (mQueuedResourceLoads.Count > 0)
		{
			QueuedResourceLoad queuedLoad = mQueuedResourceLoads[0];
			mQueuedResourceLoads.RemoveAt(0);
			mCurrentlyBackgroundLoading = queuedLoad.BackgroundLoad;
			if (!queuedLoad.PreloadOnly && !string.IsNullOrEmpty(queuedLoad.AssetPath))
			{
				UnityEngine.Object obj = LiveContent.TryLoad(queuedLoad.AssetPath.Replace("low_", string.Empty), null);
				if (obj != null)
				{
					queuedLoad.Callback(obj);
					continue;
				}
			}
			if (IsUsingAssetBundles())
			{
				queuedLoad.AssetBundle = ((queuedLoad.AssetBundle != null) ? queuedLoad.AssetBundle.ToLowerInvariant() : string.Empty);
				if (KFFLODManager.IsLowEndDevice() && IsHiRezLowRezBundle(queuedLoad.AssetBundle))
				{
					queuedLoad.AssetBundle = "low_" + queuedLoad.AssetBundle;
				}
				AssetBundle assetBundle;
				if (queuedLoad.PreloadOnly)
				{
					assetBundle = Singleton<KFFAssetBundleManager>.Instance.GetAssetBundleByName(queuedLoad.AssetBundle);
				}
				else
				{
					if (queuedLoad.UseHighResLowRes)
					{
						queuedLoad.AssetPath = FixPath(GetResourceName(queuedLoad.AssetPath));
					}
					else
					{
						queuedLoad.AssetPath = FixPath(queuedLoad.AssetPath, useHighResLowRes: false);
					}
					assetBundle = Singleton<KFFAssetBundleManager>.Instance.GetAssetBundleForResource(queuedLoad.AssetPath);
				}
				if (assetBundle == null)
				{
					while (true)
					{
						bool wasSuccess = false;
						string failMsg = null;
						yield return StartCoroutine(Singleton<KFFAssetBundleManager>.Instance.LoadAssetBundleCoroutine(assetBundleBaseURL, queuedLoad.AssetBundle, delegate(bool success, string errMsg, AssetBundle loadedBundle)
						{
							wasSuccess = success;
							failMsg = errMsg;
							if (success)
							{
								assetBundle = loadedBundle;
							}
							else
							{
								assetBundle = null;
							}
						}));
						if (wasSuccess)
						{
							break;
						}
						if (!string.IsNullOrEmpty(failMsg) && (failMsg.Contains("404") || failMsg.StartsWith("bundle-not-in-manifest")))
						{
							Debug.LogWarning("[Bundles] permanently unavailable, skipping: " + queuedLoad.AssetBundle + " (" + failMsg + ")");
							break;
						}
						if (ProgressHiddenDuringPreload())
						{
							continue;
						}
						if (queuedLoad.SilentFail)
						{
							break;
						}
						int selection = -1;
						Singleton<SimplePopupController>.Instance.ShowMessage(string.Empty, "Could not download game content. Check your connection and try again.", delegate
						{
							selection = 1;
						}, KFFLocalization.Get("!!RETRY"));
						while (true)
						{
							switch (selection)
							{
							case -1:
								yield return null;
								continue;
							case 0:
								goto end_IL_01f6;
							}
							break;
						}
						continue;
						end_IL_01f6:
						break;
					}
				}
				if (assetBundle == null)
				{
					queuedLoad.Callback(null);
					continue;
				}
				if (queuedLoad.PreloadOnly)
				{
					queuedLoad.Callback(null);
					continue;
				}
				string bundleAssetName = Singleton<KFFAssetBundleManager>.Instance.GetBundleAssetName(queuedLoad.AssetPath);
				queuedLoad.AssetPath += Singleton<KFFAssetBundleManager>.Instance.GetResourceExtension(queuedLoad.AssetPath);
				AssetBundleRequest bundleRequest = assetBundle.LoadAssetAsync(bundleAssetName ?? queuedLoad.AssetPath);
				UnityEngine.Object obj2 = null;
				if (bundleRequest != null)
				{
					yield return bundleRequest;
					obj2 = bundleRequest.asset;
				}
				queuedLoad.Callback(obj2);
			}
			else if (queuedLoad.PreloadOnly)
			{
				queuedLoad.Callback(null);
			}
			else
			{
				string path = queuedLoad.AssetPath;
				if (queuedLoad.UseHighResLowRes)
				{
					path = GetResourceName(queuedLoad.AssetPath);
				}
				ResourceRequest resourceRequest = Resources.LoadAsync(path);
				if (resourceRequest == null && KFFLODManager.IsLowEndDevice())
				{
					resourceRequest = Resources.LoadAsync(queuedLoad.AssetPath);
				}
				UnityEngine.Object obj3 = null;
				if (resourceRequest != null)
				{
					yield return resourceRequest;
					obj3 = resourceRequest.asset;
				}
				queuedLoad.Callback(obj3);
			}
		}
		mQueuedResourceLoadCoroutine = null;
	}

	public bool IsUsingAssetBundles()
	{
		return mUseAssetBundles;
	}

	private string FixPath(string path, bool useHighResLowRes = true)
	{
		string text = ((resourcePathPrefix == "Assets/ResourcesOffline/") ? KFFAssetBundleManager.BundleResourcesPrefix : resourcePathPrefix);
		if (path.StartsWith("Assets/Resources/"))
		{
			if (useHighResLowRes)
			{
				path = path.Replace("Assets/Resources/", text);
			}
		}
		else if (!path.StartsWith("Assets/"))
		{
			path = text + path;
		}
		return path;
	}

	public static UnityEngine.Object GetAsset(AsyncOperation a)
	{
		if (a is ResourceRequest resourceRequest)
		{
			return resourceRequest.asset;
		}
		if (a is AssetBundleRequest assetBundleRequest)
		{
			return assetBundleRequest.asset;
		}
		return null;
	}

	public long PendingDownloadBytes()
	{
		string[] allBundleNames = KFFAssetBundleManager.GetAllBundleNames();
		if (!KFFAssetBundleManager.HasBundleSizes || allBundleNames == null)
		{
			return -1L;
		}
		KFFAssetBundleManager instance = Singleton<KFFAssetBundleManager>.Instance;
		long num = 0L;
		foreach (string text in allBundleNames)
		{
			if (!string.IsNullOrEmpty(text) && (!KFFLODManager.IsLowEndDevice() || !IsHiRezLowRezBundle(text)) && instance.GetAssetBundleByName(text) == null && !instance.IsBundleCached(assetBundleBaseURL, text))
			{
				num += KFFAssetBundleManager.BundleSize(text);
			}
		}
		return num;
	}

	public void StartResourceLoadProgress(int totalBundles)
	{
		mBundleIndex = 0;
		mTotalBundles = totalBundles;
	}

	private bool ProgressHiddenDuringPreload()
	{
		if (mCurrentlyBackgroundLoading)
		{
			return !LoadingScreenController.ShowingLoadingScreenNotFadingOut();
		}
		return false;
	}

	public void GetResourceLoadProgress(out float totalProgress, out float fileProgress)
	{
		totalProgress = -1f;
		fileProgress = -1f;
		if (mTotalBundles <= 0 || ProgressHiddenDuringPreload())
		{
			return;
		}
		totalProgress = (float)mBundleIndex / (float)mTotalBundles;
		float activeDownloadProgress = Singleton<KFFAssetBundleManager>.Instance.GetActiveDownloadProgress();
		if (activeDownloadProgress >= 0f)
		{
			mInProgressDownload = activeDownloadProgress;
		}
		if (mInProgressDownload >= 0f)
		{
			totalProgress += mInProgressDownload / (float)mTotalBundles;
			if ((LoadingScreenController.ShowingLoadingScreenNotFadingOut() || DetachedSingleton<SceneFlowManager>.Instance.GetCurrentScene() == SceneFlowManager.Scene.AssetBundleDownload) && mTotalBundles > 1)
			{
				fileProgress = mInProgressDownload;
			}
		}
	}

	private void CheckResourceLoadStart(int defaultAmount = 1)
	{
		if (mTotalBundles <= 0)
		{
			mBundleIndex = 0;
			mTotalBundles = defaultAmount;
		}
	}

	public void OnResourceLoadDone()
	{
		mInProgressDownload = -1f;
		mBundleIndex++;
		if (mBundleIndex >= mTotalBundles)
		{
			mTotalBundles = -1;
		}
	}

	public IEnumerator LoadCreatureResources(CreatureData creature, Action<GameObject, Texture2D> callback, bool lockInput = true)
	{
		CheckResourceLoadStart(2);
		GameObject objData = null;
		Texture2D texture = null;
		string rootFolder = "Creatures/" + creature.Prefab + "/";
		if (lockInput)
		{
			UICamera.LockInput();
		}
		bool done2 = false;
		QueueResourceLoad(rootFolder + creature.Prefab, creature.Prefab, delegate(UnityEngine.Object loadedResouce)
		{
			objData = loadedResouce as GameObject;
			done2 = true;
		}, isPreload: false, silentFail: false, useHighRes: true, !lockInput);
		while (!done2)
		{
			yield return null;
		}
		OnResourceLoadDone();
		done2 = false;
		QueueResourceLoad(string.Concat(rootFolder, "Textures/", creature.Faction, "/", creature.PrefabTexture), creature.Prefab, delegate(UnityEngine.Object loadedResouce)
		{
			texture = loadedResouce as Texture2D;
			done2 = true;
		}, isPreload: false, silentFail: false, useHighRes: false, !lockInput);
		while (!done2)
		{
			yield return null;
		}
		OnResourceLoadDone();
		if (lockInput)
		{
			UICamera.UnlockInput();
		}
		callback(objData, texture);
	}

	public IEnumerator LoadLeaderResources(LeaderData leader, Action<GameObject> callback)
	{
		CheckResourceLoadStart();
		GameObject objData = null;
		UICamera.LockInput();
		bool done = false;
		QueueResourceLoad("Characters/" + leader.Prefab + "/" + leader.Prefab, leader.Prefab, delegate(UnityEngine.Object loadedResouce)
		{
			objData = loadedResouce as GameObject;
			done = true;
		});
		while (!done)
		{
			yield return null;
		}
		OnResourceLoadDone();
		UICamera.UnlockInput();
		callback(objData);
	}

	public IEnumerator LoadEnvironmentResources(QuestData quest, Action<UnityEngine.Object> callback)
	{
		CheckResourceLoadStart();
		UnityEngine.Object objData = null;
		UICamera.LockInput();
		bool done = false;
		QueueResourceLoad("Environment/" + quest.LevelPrefab + "/" + quest.LevelPrefab, quest.LevelPrefab, delegate(UnityEngine.Object loadedResouce)
		{
			objData = loadedResouce;
			done = true;
		});
		while (!done)
		{
			yield return null;
		}
		OnResourceLoadDone();
		UICamera.UnlockInput();
		callback(objData);
	}

	public IEnumerator LoadGameBoardResources(QuestData quest, Action<UnityEngine.Object> callback)
	{
		CheckResourceLoadStart();
		UnityEngine.Object objData = null;
		UICamera.LockInput();
		bool done = false;
		QueueResourceLoad("GameBoard/" + quest.BoardPrefab + "/" + quest.BoardPrefab, quest.BoardPrefab, delegate(UnityEngine.Object loadedResouce)
		{
			objData = loadedResouce;
			done = true;
		});
		while (!done)
		{
			yield return null;
		}
		OnResourceLoadDone();
		UICamera.UnlockInput();
		callback(objData);
	}

	public void LoadAudioResource(string clipName, string bundleName, Action<AudioClip> callback)
	{
		CheckResourceLoadStart();
		bool lockInput = LoadingScreenController.ShowingLoadingScreenNotFadingOut();
		if (lockInput)
		{
			UICamera.LockInput();
		}
		QueueResourceLoad(clipName, bundleName, delegate(UnityEngine.Object loadedResouce)
		{
			OnResourceLoadDone();
			if (lockInput)
			{
				UICamera.UnlockInput();
			}
			callback(loadedResouce as AudioClip);
		}, isPreload: false, silentFail: false, useHighRes: false);
	}

	public void QueueUITextureLoad(string texture, string assetBundle, string placeholderTexture, UITexture textureObject)
	{
		Texture textureFromCache = UITexture.GetTextureFromCache(Path.GetFileName(texture));
		if (textureFromCache != null)
		{
			textureObject.UnloadTexture();
			textureObject.mainTexture = textureFromCache;
			return;
		}
		QueuedTextureLoad queuedTextureLoad = new QueuedTextureLoad();
		queuedTextureLoad.AssetBundle = assetBundle;
		queuedTextureLoad.AssetPath = texture;
		queuedTextureLoad.TextureObject = textureObject;
		mQueuedTextureLoads.Add(queuedTextureLoad);
		textureObject.ReplaceTexture(placeholderTexture);
		textureObject.StreamingInTexture = queuedTextureLoad.AssetPath;
		if (mQueuedTextureLoadCoroutine == null)
		{
			mQueuedTextureLoadCoroutine = StartCoroutine(QueuedTextureLoadCoroutine());
		}
	}

	private IEnumerator QueuedTextureLoadCoroutine()
	{
		while (mQueuedTextureLoads.Count > 0)
		{
			QueuedTextureLoad nextLoad = mQueuedTextureLoads[0];
			mQueuedTextureLoads.RemoveAt(0);
			if (nextLoad.TextureObject == null || nextLoad.TextureObject.StreamingInTexture != nextLoad.AssetPath)
			{
				continue;
			}
			Texture textureFromCache = UITexture.GetTextureFromCache(Path.GetFileName(nextLoad.AssetPath));
			if (textureFromCache != null)
			{
				nextLoad.TextureObject.UnloadTexture();
				nextLoad.TextureObject.mainTexture = textureFromCache;
				continue;
			}
			Texture texture = null;
			bool done = false;
			QueueResourceLoad(nextLoad.AssetPath, nextLoad.AssetBundle, delegate(UnityEngine.Object loadedResouce)
			{
				texture = loadedResouce as Texture;
				done = true;
			}, isPreload: false, silentFail: true);
			while (!done)
			{
				yield return null;
			}
			if (texture == null)
			{
				texture = Resources.Load<Texture>(nextLoad.AssetPath);
			}
			if (!(nextLoad.TextureObject == null) && !(nextLoad.TextureObject.StreamingInTexture != nextLoad.AssetPath) && texture != null)
			{
				nextLoad.TextureObject.UnloadTexture();
				nextLoad.TextureObject.mainTexture = texture;
				UITexture.AddLoadedTextureToCache(texture);
			}
		}
		mQueuedTextureLoadCoroutine = null;
	}

	public void StartAssetBundlePreload(PreloadBundlesPoint preloadPoint)
	{
		switch (preloadPoint)
		{
		case PreloadBundlesPoint.IntroBattle:
		{
			QuestLoadoutData data2 = QuestLoadoutDataManager.Instance.GetData("StartDeck");
			QuestLoadoutData data3 = QuestLoadoutDataManager.Instance.GetData("MAIN_L1_Q1");
			int totalBundles3 = 3 + 2 * (data2.Entries.Count + data3.Entries.Count);
			StartResourceLoadProgress(totalBundles3);
			QueueResourceLoad(null, "MainAudioBundle", OnAssetPreloadDone, isPreload: true);
			QueueResourceLoad(null, "FTUE", OnAssetPreloadDone, isPreload: true);
			QueueResourceLoad(null, "FTUEBundle", OnAssetPreloadDone, isPreload: true);
			foreach (QuestLoadoutEntry entry in data2.Entries)
			{
				QueueCreatureResourceLoad(entry.Creature);
			}
			{
				foreach (QuestLoadoutEntry entry2 in data3.Entries)
				{
					QueueCreatureResourceLoad(entry2.Creature);
				}
				break;
			}
		}
		case PreloadBundlesPoint.Q1:
		{
			QuestLoadoutData data = QuestLoadoutDataManager.Instance.GetData("MAIN_L1_Q2");
			int totalBundles2 = 2 * data.Entries.Count;
			StartResourceLoadProgress(totalBundles2);
			{
				foreach (QuestLoadoutEntry entry3 in data.Entries)
				{
					QueueCreatureResourceLoad(entry3.Creature);
				}
				break;
			}
		}
		case PreloadBundlesPoint.Q2:
		{
			int totalBundles = 2;
			StartResourceLoadProgress(totalBundles);
			QueueResourceLoad(null, "GeneralBundle", OnAssetPreloadDone, isPreload: true);
			QueueResourceLoad(null, "BaseGame", OnAssetPreloadDone, isPreload: true);
			break;
		}
		}
	}

	private void QueueCreatureResourceLoad(CreatureData creature)
	{
		QueueResourceLoad("Creatures/" + creature.Prefab + "/" + creature.Prefab, creature.Prefab, OnAssetPreloadDone, isPreload: true);
		QueueResourceLoad(string.Concat("Creatures/", creature.Prefab, "/Textures/", creature.Faction, "/", creature.PrefabTexture), creature.Prefab, OnAssetPreloadDone, isPreload: true);
	}

	private void OnAssetPreloadDone(UnityEngine.Object loadedAsset)
	{
		OnResourceLoadDone();
	}

	private bool IsHiRezLowRezBundle(string bundleName)
	{
		bundleName = bundleName.ToLowerInvariant();
		switch (bundleName)
		{
		case "mainresourcesbundle":
		case "mainscenesbundle":
		case "ftueaudiobundle":
		case "mainaudiobundle":
			return false;
		default:
			return true;
		}
	}

	public IEnumerator LoadPrimaryBundlesCouroutine()
	{
		if (!IsUsingAssetBundles())
		{
			yield break;
		}
		List<string> bundlesToLoad = new List<string>(BundlesToLoadUpFront);
		if (mResourcesBundle == null)
		{
			bundlesToLoad.Add("MainResourcesBundle");
		}
		if (mScenesBundle == null)
		{
			bundlesToLoad.Add("MainScenesBundle");
		}
		if (Singleton<KFFAssetBundleManager>.Instance.GetAssetBundleByName("MainAudioBundle") == null)
		{
			bundlesToLoad.Add("MainAudioBundle");
		}
		if (bundlesToLoad.Count == 0)
		{
			yield break;
		}
		StartResourceLoadProgress(bundlesToLoad.Count);
		mCurrentlyBackgroundLoading = false;
		for (int i = 0; i < bundlesToLoad.Count; i++)
		{
			string bundleName = bundlesToLoad[i];
			if (KFFLODManager.IsLowEndDevice() && IsHiRezLowRezBundle(bundlesToLoad[i]))
			{
				bundleName = "low_" + bundleName;
			}
			if (Singleton<KFFAssetBundleManager>.Instance.GetAssetBundleByName(bundleName) != null)
			{
				OnResourceLoadDone();
				continue;
			}
			int retryCount = 0;
			while (true)
			{
				bool wasSuccess = false;
				bool missingFromManifest = false;
				string lastError = null;
				yield return StartCoroutine(Singleton<KFFAssetBundleManager>.Instance.LoadAssetBundleCoroutine(assetBundleBaseURL, bundleName, delegate(bool success, string errMsg, AssetBundle loadedBundle)
				{
					wasSuccess = success;
					lastError = errMsg;
					missingFromManifest = !success && errMsg != null && errMsg.StartsWith("bundle-not-in-manifest");
					if (success)
					{
						if (bundleName == "MainResourcesBundle")
						{
							mResourcesBundle = loadedBundle;
						}
						else if (bundleName == "MainScenesBundle")
						{
							mScenesBundle = loadedBundle;
						}
						OnResourceLoadDone();
					}
				}));
				if (wasSuccess)
				{
					break;
				}
				if (missingFromManifest)
				{
					Debug.LogWarning("[Bundles] skipping preload of '" + bundleName + "': not in this build's manifest.");
					OnResourceLoadDone();
					break;
				}
				retryCount++;
				if (retryCount <= 3)
				{
					yield return new WaitForSeconds(1f);
					continue;
				}
				string text = bundleName + (string.IsNullOrEmpty(lastError) ? " (no error text)" : (": " + lastError)) + "\nmem " + SystemInfo.systemMemorySize + "MB";
				Debug.LogError("[Bundles] FAILED " + text);
				int selection = -1;
				Singleton<SimplePopupController>.Instance.ShowPrompt(string.Empty, "Could not load game content.\n\n" + text + "\n\nPlease screenshot this and send it to us.", delegate
				{
					selection = 1;
				}, delegate
				{
					selection = 0;
				}, KFFLocalization.Get("!!RETRY"), KFFLocalization.Get("!!CANCEL"));
				while (selection == -1)
				{
					yield return null;
				}
				if (selection == 0)
				{
					Debug.LogWarning("[Bundles] player chose to quit after repeated download failures.");
					Application.Quit();
					yield break;
				}
				retryCount = 0;
			}
		}
		yield return StartCoroutine(LiveContent.LoadPacks(KFFAssetBundleManager.ResolveBaseURL(assetBundleBaseURL)));
		yield return StartCoroutine(PrecacheRemainingBundlesCoroutine());
	}

	private IEnumerator PrecacheRemainingBundlesCoroutine()
	{
		string[] allBundleNames = KFFAssetBundleManager.GetAllBundleNames();
		if (allBundleNames == null || allBundleNames.Length == 0)
		{
			yield break;
		}
		List<string> todo = new List<string>();
		foreach (string text in allBundleNames)
		{
			if (!string.IsNullOrEmpty(text) && (!KFFLODManager.IsLowEndDevice() || !IsHiRezLowRezBundle(text)) && Singleton<KFFAssetBundleManager>.Instance.GetAssetBundleByName(text) == null && !Singleton<KFFAssetBundleManager>.Instance.IsBundleCached(assetBundleBaseURL, text))
			{
				todo.Add(text);
			}
		}
		if (todo.Count == 0)
		{
			yield break;
		}
		StartResourceLoadProgress(todo.Count);
		mCurrentlyBackgroundLoading = false;
		List<string> stillFailed = new List<string>();
		for (int j = 0; j < todo.Count; j++)
		{
			mBundleIndex = j;
			string bundleName = todo[j];
			yield return StartCoroutine(Singleton<KFFAssetBundleManager>.Instance.CacheAssetBundleCoroutine(assetBundleBaseURL, bundleName, delegate(bool ok, string err)
			{
				if (!ok)
				{
					stillFailed.Add(bundleName + " (" + (err ?? "unknown") + ")");
				}
			}));
			OnResourceLoadDone();
		}
		Debug.Log("[Bundles] pre-cached " + (todo.Count - stillFailed.Count) + "/" + todo.Count + " content bundles (" + stillFailed.Count + " will fall back to on-demand).");
		if (stillFailed.Count > 0)
		{
			Debug.LogWarning("[Bundles] bundles that FAILED all download attempts: " + string.Join(", ", stillFailed.ToArray()));
		}
	}
}
