using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public static class BlueprintCache
{
	[Serializable]
	private class Entry
	{
		public string name;

		public string data;
	}

	[Serializable]
	private class Wrapper
	{
		public Entry[] items;
	}

	private static Dictionary<string, string> sFiles;

	public static string ETag;

	public static string Get(string filePath)
	{
		if (sFiles == null || string.IsNullOrEmpty(filePath))
		{
			return null;
		}
		if (!sFiles.TryGetValue(Path.GetFileNameWithoutExtension(filePath.Replace('\\', '/')), out var value))
		{
			return null;
		}
		return value;
	}

	public static IEnumerator Fetch()
	{
		sFiles = null;
		string cacheFile = Path.Combine(Application.persistentDataPath, "blueprints_cache.json");
		string etagFile = Path.Combine(Application.persistentDataPath, "blueprints_cache.etag");
		using UnityWebRequest req = UnityWebRequest.Get(SQSettings.CDN_URL + "blueprints");
		req.timeout = 30;
		LiveContent.AddHeaders(req);
		try
		{
			if (File.Exists(cacheFile) && File.Exists(etagFile))
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
			string text = null;
			if (req.responseCode == 304)
			{
				text = File.ReadAllText(cacheFile);
				ETag = File.ReadAllText(etagFile).Trim();
			}
			else if (req.result == UnityWebRequest.Result.Success && req.responseCode == 200)
			{
				text = req.downloadHandler.text;
				File.WriteAllText(cacheFile, text);
				string text2 = (ETag = req.GetResponseHeader("ETag"));
				if (!string.IsNullOrEmpty(text2))
				{
					File.WriteAllText(etagFile, text2);
				}
				else if (File.Exists(etagFile))
				{
					File.Delete(etagFile);
				}
			}
			if (!string.IsNullOrEmpty(text))
			{
				Wrapper wrapper = JsonUtility.FromJson<Wrapper>("{\"items\":" + text + "}");
				Dictionary<string, string> dictionary = new Dictionary<string, string>();
				if (wrapper != null && wrapper.items != null)
				{
					Entry[] items = wrapper.items;
					foreach (Entry entry in items)
					{
						if (entry != null && !string.IsNullOrEmpty(entry.name) && entry.data != null)
						{
							dictionary[entry.name] = entry.data;
						}
					}
				}
				sFiles = ((dictionary.Count > 0) ? dictionary : null);
			}
			Debug.Log("[Blueprints] " + ((sFiles != null) ? (sFiles.Count + " tables in one request (HTTP " + req.responseCode + ")") : ("bulk fetch unavailable (" + req.responseCode + " " + req.error + "), loading tables one by one")));
		}
		catch (Exception ex2)
		{
			sFiles = null;
			Debug.LogWarning("[Blueprints] bulk fetch failed, loading tables one by one: " + ex2.Message);
		}
	}
}
