using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using MiniJSON;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public static class CrashReporter
{
	public const int MaxDistinct = 50;

	private static readonly object sLock = new object();

	private static readonly Dictionary<string, Dictionary<string, object>> sPending = new Dictionary<string, Dictionary<string, object>>();

	private static readonly HashSet<string> sSeen = new HashSet<string>();

	private static bool sSending;

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
	private static void Init()
	{
		Application.logMessageReceivedThreaded += OnLog;
	}

	private static void OnLog(string msg, string stack, LogType type)
	{
		if (type == LogType.Exception || type == LogType.Error || type == LogType.Assert)
		{
			Record(msg, stack, type.ToString());
		}
	}

	public static bool Record(string msg, string stack, string kind)
	{
		try
		{
			msg = msg ?? string.Empty;
			stack = stack ?? string.Empty;
			int num = stack.IndexOf('\n');
			string text = ((msg.Length > 200) ? msg.Substring(0, 200) : msg) + "|" + ((num > 0) ? stack.Substring(0, num) : stack);
			lock (sLock)
			{
				if (sPending.TryGetValue(text, out var value))
				{
					value["count"] = (int)value["count"] + 1;
					return true;
				}
				if (!sSeen.Contains(text) && sSeen.Count >= 50)
				{
					return false;
				}
				sSeen.Add(text);
				sPending[text] = new Dictionary<string, object>
				{
					{
						"msg",
						"[" + kind + "] " + ((msg.Length > 400) ? msg.Substring(0, 400) : msg)
					},
					{
						"stack",
						(stack.Length > 1500) ? stack.Substring(0, 1500) : stack
					},
					{ "count", 1 },
					{ "_k", text }
				};
				return true;
			}
		}
		catch (Exception)
		{
			return false;
		}
	}

	public static List<object> Take()
	{
		List<object> list = new List<object>();
		lock (sLock)
		{
			foreach (string item in new List<string>(sPending.Keys))
			{
				if (list.Count >= 20)
				{
					break;
				}
				list.Add(sPending[item]);
				sPending.Remove(item);
			}
		}
		return list;
	}

	private static void PutBack(List<object> list)
	{
		lock (sLock)
		{
			foreach (Dictionary<string, object> item in list)
			{
				string key = (string)item["_k"];
				if (sPending.TryGetValue(key, out var value))
				{
					value["count"] = (int)value["count"] + (int)item["count"];
				}
				else
				{
					sPending[key] = item;
				}
			}
		}
	}

	public static IEnumerator Flush()
	{
		if (sSending)
		{
			yield break;
		}
		List<object> list = Take();
		if (list.Count == 0)
		{
			yield break;
		}
		sSending = true;
		string value = string.Empty;
		string value2 = string.Empty;
		try
		{
			value = SceneManager.GetActiveScene().name;
			value2 = ((SessionManager.Instance != null) ? (SessionManager.Instance.PlayerID ?? string.Empty) : string.Empty);
		}
		catch (Exception)
		{
		}
		foreach (Dictionary<string, object> item in list)
		{
			item["scene"] = value;
			item["version"] = Application.version;
			item["platform"] = Application.platform.ToString();
			item["device"] = SystemInfo.deviceModel;
			item["player"] = value2;
			item["lang"] = Language.CurrentLanguage().ToString();
		}
		bool num;
		using (UnityWebRequest req = new UnityWebRequest(SQSettings.CDN_URL + "report", "POST"))
		{
			req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(Json.Serialize(list)));
			req.downloadHandler = new DownloadHandlerBuffer();
			req.SetRequestHeader("Content-Type", "application/json");
			req.timeout = 15;
			yield return req.SendWebRequest();
			num = req.result == UnityWebRequest.Result.Success;
		}
		if (!num)
		{
			PutBack(list);
		}
		sSending = false;
	}
}
