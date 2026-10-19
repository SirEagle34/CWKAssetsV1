using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class UIPatches
{
	private static List<object> sRules = new List<object>();

	private static readonly HashSet<string> sDone = new HashSet<string>();

	private static float sNextTry;

	private static bool sHooked;

	public static void SetRules(List<object> rules)
	{
		sRules = rules ?? new List<object>();
		sNextTry = 0f;
		if (!sHooked)
		{
			sHooked = true;
			SceneManager.sceneLoaded += delegate
			{
				sDone.Clear();
				sNextTry = 0f;
			};
		}
	}

	public static void Tick()
	{
		if (sRules.Count == 0 || Time.realtimeSinceStartup < sNextTry)
		{
			return;
		}
		sNextTry = Time.realtimeSinceStartup + 1f;
		foreach (object sRule in sRules)
		{
			Dictionary<string, object> dictionary = sRule as Dictionary<string, object>;
			string text = Str(dictionary, "id");
			if (text == null || sDone.Contains(text))
			{
				continue;
			}
			try
			{
				if (!Matches(dictionary))
				{
					sDone.Add(text);
				}
				else if (Apply(dictionary))
				{
					sDone.Add(text);
					Debug.Log("[UIPatch] applied " + text);
				}
			}
			catch (Exception ex)
			{
				sDone.Add(text);
				Debug.LogWarning("[UIPatch] " + text + " failed: " + ex.Message);
			}
		}
	}

	private static bool Matches(Dictionary<string, object> r)
	{
		string text = Str(r, "min");
		string text2 = Str(r, "max");
		if (text != null && LiveContent.Compare(Application.version, text) < 0)
		{
			return false;
		}
		if (text2 != null && LiveContent.Compare(Application.version, text2) > 0)
		{
			return false;
		}
		if (r.ContainsKey("max_build") && 2 > Convert.ToInt32(r["max_build"]))
		{
			return false;
		}
		List<object> list = (r.ContainsKey("platforms") ? (r["platforms"] as List<object>) : null);
		if (list != null && !list.Contains(Application.platform.ToString()))
		{
			return false;
		}
		string text3 = Str(r, "scene");
		if (text3 != null)
		{
			return SceneManager.GetSceneByName(text3).isLoaded;
		}
		return true;
	}

	private static bool Apply(Dictionary<string, object> r)
	{
		string scene = Str(r, "scene");
		GameObject gameObject = Find(scene, Str(r, "path"));
		if (gameObject == null)
		{
			return false;
		}
		Dictionary<string, object> dictionary = (r.ContainsKey("click") ? (r["click"] as Dictionary<string, object>) : null);
		MonoBehaviour monoBehaviour = null;
		if (dictionary != null)
		{
			GameObject gameObject2 = Find(scene, Str(dictionary, "path"));
			if (gameObject2 == null)
			{
				return false;
			}
			monoBehaviour = gameObject2.GetComponent(Str(dictionary, "component")) as MonoBehaviour;
			if (monoBehaviour == null)
			{
				throw new Exception("no " + Str(dictionary, "component") + " on " + Str(dictionary, "path"));
			}
		}
		if (r.ContainsKey("active"))
		{
			gameObject.SetActive(Bool(r["active"]));
		}
		string text = Str(r, "component");
		if (text != null && r.ContainsKey("enabled"))
		{
			Component component = gameObject.GetComponent(text);
			bool enabled = Bool(r["enabled"]);
			if (component is Behaviour)
			{
				((Behaviour)component).enabled = enabled;
			}
			else if (component is Collider)
			{
				((Collider)component).enabled = enabled;
			}
			else
			{
				if (!(component is Renderer))
				{
					throw new Exception("no switchable " + text);
				}
				((Renderer)component).enabled = enabled;
			}
		}
		if (r.ContainsKey("text"))
		{
			UILabel component2 = gameObject.GetComponent<UILabel>();
			string text2 = LiveChannel.Pick(r["text"]);
			if (component2 == null || text2 == null)
			{
				throw new Exception("no UILabel or text");
			}
			component2.text = text2;
		}
		if (monoBehaviour != null)
		{
			UIButton component3 = gameObject.GetComponent<UIButton>();
			if (component3 == null)
			{
				throw new Exception("no UIButton");
			}
			EventDelegate.Add(component3.onClick, new EventDelegate(monoBehaviour, Str(dictionary, "method")));
		}
		string text3 = Str(r, "send");
		if (text3 != null)
		{
			gameObject.SendMessage(text3, SendMessageOptions.DontRequireReceiver);
		}
		return true;
	}

	private static GameObject Find(string scene, string path)
	{
		if (string.IsNullOrEmpty(path))
		{
			return null;
		}
		path = path.TrimStart('/');
		int num = path.IndexOf('/');
		string text = ((num < 0) ? path : path.Substring(0, num));
		string text2 = ((num < 0) ? null : path.Substring(num + 1));
		for (int i = 0; i < SceneManager.sceneCount; i++)
		{
			Scene sceneAt = SceneManager.GetSceneAt(i);
			if (!sceneAt.isLoaded || (scene != null && sceneAt.name != scene))
			{
				continue;
			}
			GameObject[] rootGameObjects = sceneAt.GetRootGameObjects();
			foreach (GameObject gameObject in rootGameObjects)
			{
				if (!(gameObject.name != text))
				{
					if (text2 == null)
					{
						return gameObject;
					}
					Transform transform = gameObject.transform.Find(text2);
					if (transform != null)
					{
						return transform.gameObject;
					}
				}
			}
		}
		return null;
	}

	private static string Str(Dictionary<string, object> d, string key)
	{
		if (d == null || !d.TryGetValue(key, out var value) || value == null)
		{
			return null;
		}
		return value.ToString();
	}

	private static bool Bool(object o)
	{
		if (!(o is bool))
		{
			return Convert.ToBoolean(o, CultureInfo.InvariantCulture);
		}
		return (bool)o;
	}
}
