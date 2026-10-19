using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using MiniJSON;
using UnityEngine;
using UnityEngine.Networking;

public static class LiveContent
{
	private class Pack
	{
		public string Name;

		public AssetBundle Bundle;

		public Dictionary<string, string> Paths = new Dictionary<string, string>();
	}

	public const int Build = 2;

	private static string sStage;

	private static string sHashOf;

	private static string sHash;

	public static Dictionary<string, string> Scripts = new Dictionary<string, string>();

	private static readonly List<Pack> sPacks = new List<Pack>();

	private static bool sPacksLoaded;

	public const int PackDownloadAttempts = 3;

	public static string Stage
	{
		get
		{
			if (sStage == null)
			{
				string text = PlayerPrefs.GetString("cwk_content_stage", string.Empty);
				string[] commandLineArgs = Environment.GetCommandLineArgs();
				for (int i = 0; i + 1 < commandLineArgs.Length; i++)
				{
					if (commandLineArgs[i] == "-content")
					{
						text = commandLineArgs[i + 1];
					}
				}
				sStage = (text ?? string.Empty).ToLowerInvariant();
			}
			return sStage;
		}
	}

	public static bool Staging => Stage == "staging";

	public static string PvpTag
	{
		get
		{
			string text = BlueprintCache.Get("_content");
			return (string.IsNullOrEmpty(text) ? string.Empty : ("_" + text)) + BattleRules.PvpFingerprint();
		}
	}

	public static void AddHeaders(UnityWebRequest req)
	{
		foreach (KeyValuePair<string, string> item in Headers())
		{
			req.SetRequestHeader(item.Key, item.Value);
		}
	}

	public static Dictionary<string, string> Headers()
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		dictionary["X-Client-Version"] = Application.version;
		dictionary["X-Client-Build"] = 2.ToString();
		if (Staging)
		{
			dictionary["X-Content-Stage"] = "staging";
		}
		string text = PlayerHash();
		if (text != null)
		{
			dictionary["X-Player-Hash"] = text;
		}
		return dictionary;
	}

	public static string PlayerHash()
	{
		try
		{
			string text = ((SessionManager.Instance != null) ? SessionManager.Instance.PlayerID : null);
			if (string.IsNullOrEmpty(text))
			{
				if (sHashOf == null)
				{
					sHashOf = string.Empty;
					text = Player.LoadFromFilesystem().playerId;
				}
				else
				{
					text = sHashOf;
				}
			}
			if (string.IsNullOrEmpty(text) || text == "ua")
			{
				return null;
			}
			if (text != sHashOf)
			{
				using (SHA256 sHA = SHA256.Create())
				{
					sHash = BitConverter.ToString(sHA.ComputeHash(Encoding.UTF8.GetBytes(text))).Replace("-", string.Empty).ToLowerInvariant()
						.Substring(0, 32);
				}
				sHashOf = text;
			}
			return sHash;
		}
		catch (Exception)
		{
			return null;
		}
	}

	public static bool VersionAtLeast(string wanted)
	{
		return Compare(Application.version, wanted) >= 0;
	}

	public static int Compare(string a, string b)
	{
		string[] array = (a ?? "0").Split('.');
		string[] array2 = (b ?? "0").Split('.');
		for (int i = 0; i < 3; i++)
		{
			int result = 0;
			int result2 = 0;
			if (i < array.Length)
			{
				int.TryParse(array[i], out result);
			}
			if (i < array2.Length)
			{
				int.TryParse(array2[i], out result2);
			}
			if (result != result2)
			{
				return result.CompareTo(result2);
			}
		}
		return 0;
	}

	public static IEnumerator FetchStringsAndScripts()
	{
		string strings = null;
		yield return FetchCached("strings", delegate(string t)
		{
			strings = t;
		});
		try
		{
			Dictionary<string, object> dictionary = Json.Deserialize(strings ?? "{}") as Dictionary<string, object>;
			Dictionary<string, Dictionary<string, string>> dictionary2 = new Dictionary<string, Dictionary<string, string>>();
			if (dictionary != null)
			{
				foreach (KeyValuePair<string, object> item in dictionary)
				{
					if (!(item.Value is Dictionary<string, object> dictionary3))
					{
						continue;
					}
					Dictionary<string, string> dictionary4 = new Dictionary<string, string>();
					foreach (KeyValuePair<string, object> item2 in dictionary3)
					{
						dictionary4[item2.Key] = ((item2.Value == null) ? string.Empty : item2.Value.ToString());
					}
					dictionary2[item.Key.ToUpperInvariant()] = dictionary4;
				}
			}
			Language.RemoteEntries = dictionary2;
			Language.ApplyRemoteEntries();
			Debug.Log("[LiveContent] strings: " + dictionary2.Count + " languages");
		}
		catch (Exception ex)
		{
			Debug.LogWarning("[LiveContent] strings ignored: " + ex.Message);
		}
		string scripts = null;
		yield return FetchCached("scripts", delegate(string t)
		{
			scripts = t;
		});
		try
		{
			Dictionary<string, object> dictionary5 = Json.Deserialize(scripts ?? "{}") as Dictionary<string, object>;
			Dictionary<string, string> dictionary6 = new Dictionary<string, string>();
			if (dictionary5 != null)
			{
				foreach (KeyValuePair<string, object> item3 in dictionary5)
				{
					dictionary6[item3.Key] = ((item3.Value == null) ? string.Empty : item3.Value.ToString());
				}
			}
			Scripts = dictionary6;
			Debug.Log("[LiveContent] scripts: " + dictionary6.Count);
		}
		catch (Exception ex2)
		{
			Debug.LogWarning("[LiveContent] scripts ignored: " + ex2.Message);
		}
	}

	private static IEnumerator FetchCached(string name, Action<string> done)
	{
		string file = Path.Combine(Application.persistentDataPath, "livecontent_" + name + ".json");
		string etagFile = file + ".etag";
		string text = null;
		using (UnityWebRequest req = UnityWebRequest.Get(SQSettings.CDN_URL + name))
		{
			req.timeout = 20;
			AddHeaders(req);
			try
			{
				if (File.Exists(file) && File.Exists(etagFile))
				{
					req.SetRequestHeader("If-None-Match", File.ReadAllText(etagFile).Trim());
				}
			}
			catch (Exception)
			{
			}
			yield return req.SendWebRequest();
			try
			{
				if (req.responseCode == 200 && req.result == UnityWebRequest.Result.Success)
				{
					text = req.downloadHandler.text;
					File.WriteAllText(file, text);
					string responseHeader = req.GetResponseHeader("ETag");
					if (!string.IsNullOrEmpty(responseHeader))
					{
						File.WriteAllText(etagFile, responseHeader);
					}
				}
				else if (File.Exists(file))
				{
					text = File.ReadAllText(file);
				}
			}
			catch (Exception ex2)
			{
				Debug.LogWarning("[LiveContent] " + name + ": " + ex2.Message);
			}
		}
		done(text);
	}

	public static IEnumerator LoadPacks(string basePlatformURL)
	{
		if (string.IsNullOrEmpty(basePlatformURL))
		{
			yield break;
		}
		bool reload = sPacksLoaded;
		sPacksLoaded = true;
		List<object> list = null;
		using (UnityWebRequest req = UnityWebRequest.Get(basePlatformURL + "content_packs.json"))
		{
			req.timeout = 20;
			yield return req.SendWebRequest();
			if (req.result != UnityWebRequest.Result.Success)
			{
				yield break;
			}
			list = Json.Deserialize(req.downloadHandler.text) as List<object>;
		}
		if (list == null)
		{
			yield break;
		}
		if (reload)
		{
			foreach (Pack sPack in sPacks)
			{
				if (sPack.Bundle != null)
				{
					sPack.Bundle.Unload(unloadAllLoadedObjects: false);
				}
			}
			Debug.Log("[LiveContent] reload: " + sPacks.Count + " packs released");
			sPacks.Clear();
		}
		string[] args = Environment.GetCommandLineArgs();
		for (int i = 0; i + 1 < args.Length; i++)
		{
			if (!(args[i] == "-localpack"))
			{
				continue;
			}
			AssetBundle assetBundle = AssetBundle.LoadFromFile(args[i + 1]);
			if (assetBundle != null)
			{
				Pack lp = new Pack
				{
					Name = Path.GetFileName(args[i + 1]),
					Bundle = assetBundle
				};
				string[] allAssetNames = assetBundle.GetAllAssetNames();
				foreach (string text in allAssetNames)
				{
					lp.Paths[Normalize(text)] = text;
				}
				sPacks.Add(lp);
				yield return Singleton<KFFAssetBundleManager>.Instance.IndexExternalBundle(assetBundle, lp.Name);
				Debug.Log("[LiveContent] local pack " + lp.Name + ": " + lp.Paths.Count + " assets");
			}
		}
		foreach (object item in list)
		{
			if (!(item is Dictionary<string, object> e) || !e.ContainsKey("file") || !e.ContainsKey("hash"))
			{
				continue;
			}
			string text2 = (e.ContainsKey("stage") ? e["stage"].ToString().ToLowerInvariant() : "live");
			if ((!(text2 == "live") && (!(text2 == "staging") || !Staging)) || (e.ContainsKey("min_version") && !VersionAtLeast(e["min_version"].ToString())))
			{
				continue;
			}
			Hash128 hash = Hash128.Parse(e["hash"].ToString());
			string url = basePlatformURL + e["file"];
			AssetBundle bundle = null;
			for (int i = 1; i <= 3; i++)
			{
				if (!(bundle == null))
				{
					break;
				}
				using (UnityWebRequest req = UnityWebRequestAssetBundle.GetAssetBundle(url))
				{
					req.timeout = 60;
					yield return req.SendWebRequest();
					if (req.result == UnityWebRequest.Result.Success)
					{
						bundle = DownloadHandlerAssetBundle.GetContent(req);
					}
					else
					{
						Debug.LogWarning("[LiveContent] pack " + e["file"]?.ToString() + " attempt " + i + ": " + req.error);
					}
				}
				if (bundle == null && i < 3)
				{
					yield return new WaitForSeconds(2f * (float)i);
				}
			}
			if (!(bundle == null))
			{
				Pack lp = new Pack
				{
					Name = (e.ContainsKey("name") ? e["name"].ToString() : e["file"].ToString()),
					Bundle = bundle
				};
				string[] allAssetNames = bundle.GetAllAssetNames();
				foreach (string text3 in allAssetNames)
				{
					lp.Paths[Normalize(text3)] = text3;
				}
				sPacks.Add(lp);
				yield return Singleton<KFFAssetBundleManager>.Instance.IndexExternalBundle(bundle, lp.Name);
				Debug.Log("[LiveContent] pack " + lp.Name + ": " + lp.Paths.Count + " assets");
			}
		}
	}

	public static string Normalize(string path)
	{
		string text = path.Replace('\\', '/').ToLowerInvariant();
		if (text.StartsWith("assets/resourcesoffline/"))
		{
			text = text.Substring("assets/resourcesoffline/".Length);
		}
		else if (text.StartsWith("assets/resources/"))
		{
			text = text.Substring("assets/resources/".Length);
		}
		string extension = Path.GetExtension(text);
		if (extension.Length > 1 && extension.Length <= 10 && Regex.IsMatch(extension, "^\\.[a-z0-9]+$"))
		{
			text = text.Substring(0, text.Length - extension.Length);
		}
		return text;
	}

	public static UnityEngine.Object TryLoad(string path, Type t)
	{
		if (sPacks.Count == 0 || string.IsNullOrEmpty(path))
		{
			return null;
		}
		string key = Normalize(path);
		for (int num = sPacks.Count - 1; num >= 0; num--)
		{
			if (sPacks[num].Bundle != null && sPacks[num].Paths.TryGetValue(key, out var value))
			{
				UnityEngine.Object obj = ((t == null) ? sPacks[num].Bundle.LoadAsset(value) : sPacks[num].Bundle.LoadAsset(value, t));
				if (obj != null)
				{
					return obj;
				}
			}
		}
		return null;
	}

	public static bool Has(string path)
	{
		if (sPacks.Count == 0 || string.IsNullOrEmpty(path))
		{
			return false;
		}
		string key = Normalize(path);
		foreach (Pack sPack in sPacks)
		{
			if (sPack.Paths.ContainsKey(key))
			{
				return true;
			}
		}
		return false;
	}
}
