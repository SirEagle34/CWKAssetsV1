using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using MiniJSON;
using UnityEngine;
using UnityEngine.Networking;

public class LiveChannel : MonoBehaviour
{
	private static LiveChannel sInstance;

	private static Dictionary<string, object> sLast;

	private static float sLastPoll = -999f;

	private const string SeenKey = "cwk_live_seen";

	private static bool sHaveBase;

	private static string sRestartBase;

	private static float sPolledAt;

	private static bool sRestarting;

	private static bool sToldSaveBlocked;

	private static bool sToldDeviceLocked;

	private static bool sQuitForMove;

	private static int sSession;

	private static string sFlagName;

	private static bool sFlagGone;

	private static string sEndedBase;

	public static double SimAt = -1.0;

	public static double SimLen = 60.0;

	private static void LeaveBattleIfIn()
	{
		if (sQuitForMove || !DetachedSingleton<SceneFlowManager>.Instance.InBattleScene() || LoadingScreenController.ShowingLoadingScreen())
		{
			return;
		}
		PauseController instance = Singleton<PauseController>.Instance;
		if (instance == null)
		{
			return;
		}
		sQuitForMove = true;
		try
		{
			Debug.Log("[Session] leaving the battle - account opened in another game");
			instance.ForceQuit();
		}
		catch (Exception ex)
		{
			Debug.LogWarning("[Session] could not leave the battle: " + ex.Message);
		}
	}

	public static void NewSession()
	{
		sHaveBase = false;
		sRestarting = false;
		sEndedBase = null;
		sSession++;
	}

	public static void Ensure()
	{
		if (sInstance == null)
		{
			GameObject obj = new GameObject("LiveChannel");
			UnityEngine.Object.DontDestroyOnLoad(obj);
			sInstance = obj.AddComponent<LiveChannel>();
			obj.AddComponent<MaintenanceOverlay>();
			string[] commandLineArgs = Environment.GetCommandLineArgs();
			int num = Array.IndexOf(commandLineArgs, "-maintsim");
			if (num >= 0 && num + 1 < commandLineArgs.Length)
			{
				SimAt = Now() + double.Parse(commandLineArgs[num + 1], CultureInfo.InvariantCulture);
				int num2 = Array.IndexOf(commandLineArgs, "-maintsim_len");
				SimLen = ((num2 >= 0 && num2 + 1 < commandLineArgs.Length) ? double.Parse(commandLineArgs[num2 + 1], CultureInfo.InvariantCulture) : 60.0);
			}
		}
	}

	private IEnumerator Start()
	{
		StartCoroutine(SessionFlagLoop());
		while (true)
		{
			yield return new WaitForSecondsRealtime(PollSeconds());
			yield return PollOnce();
			yield return CrashReporter.Flush();
		}
	}

	private static float SessionPollSeconds()
	{
		double num = Num(Get(sLast, "session_poll"));
		if (!(num <= 0.0))
		{
			return (float)Math.Max(2.0, Math.Min(60.0, num));
		}
		return 5f;
	}

	private IEnumerator SessionFlagLoop()
	{
		while (true)
		{
			yield return new WaitForSecondsRealtime(SessionPollSeconds());
			yield return CheckSessionFlag();
		}
	}

	private static IEnumerator CheckSessionFlag()
	{
		if (sFlagGone || PlayerInfoScript.SessionMoved || PlayerInfoScript.SessionLoadedAt < 0f)
		{
			yield break;
		}
		if (sFlagName == null)
		{
			using SHA256 sHA = SHA256.Create();
			sFlagName = BitConverter.ToString(sHA.ComputeHash(Encoding.UTF8.GetBytes(PlayerInfoScript.LaunchSession))).Replace("-", string.Empty).ToLowerInvariant()
				.Substring(0, 32);
		}
		float startedAt = Time.realtimeSinceStartup;
		using UnityWebRequest req = UnityWebRequest.Get(SQSettings.SERVER_URL + "sess/" + sFlagName + "?t=" + Environment.TickCount);
		req.timeout = 10;
		yield return req.SendWebRequest();
		if (req.responseCode == 404)
		{
			sFlagGone = true;
			yield break;
		}
		if (req.result == UnityWebRequest.Result.Success && req.downloadHandler.text == "1" && PlayerInfoScript.SessionLoadedAt >= 0f && startedAt > PlayerInfoScript.SessionLoadedAt && !PlayerInfoScript.SessionMoved)
		{
			Debug.Log("[Session] account opened in another game");
			PlayerInfoScript.SessionMoved = true;
		}
	}

	public static float PollSeconds()
	{
		double num = Num(Get(sLast, "poll"));
		float num2 = ((num <= 0.0) ? 60f : ((float)Math.Max(30.0, Math.Min(600.0, num))));
		if (MaintenanceOverlay.Showing || (Maintenance(out var at, out var _, out var _) && at - ServerNow() < 180.0))
		{
			num2 = Math.Min(num2, 15f);
		}
		return num2;
	}

	public static double ServerNow()
	{
		double num = Num(Get(sLast, "now"));
		if (!(num > 0.0))
		{
			return Now();
		}
		return num + (double)(Time.realtimeSinceStartup - sPolledAt);
	}

	public static bool Maintenance(out double at, out double eta, out string message)
	{
		at = (eta = 0.0);
		message = null;
		if (SimAt > 0.0)
		{
			if (Now() >= SimAt + SimLen)
			{
				return false;
			}
			at = SimAt;
			eta = SimAt + SimLen;
			return true;
		}
		if (!(Get(sLast, "maintenance") is Dictionary<string, object> d))
		{
			return false;
		}
		at = Num(Get(d, "at"));
		eta = Num(Get(d, "eta"));
		message = Pick(Get(d, "message"));
		return true;
	}

	public static bool MaintenanceMissed()
	{
		if (sEndedBase != null)
		{
			return EndedId() != sEndedBase;
		}
		return false;
	}

	private static string EndedId()
	{
		return ((long)Num(Get(sLast, "maint_ended"))).ToString();
	}

	public static void LeaveBattleForMaintenance()
	{
		if (DetachedSingleton<SceneFlowManager>.Instance.InBattleScene() && !LoadingScreenController.ShowingLoadingScreen() && !(Singleton<PauseController>.Instance == null))
		{
			Debug.Log("[Maintenance] leaving the battle - no contest");
			Singleton<PauseController>.Instance.LeaveForMaintenance();
		}
	}

	public static bool MaintenanceStarting(double seconds)
	{
		if (Maintenance(out var at, out var _, out var _))
		{
			return ServerNow() >= at - seconds;
		}
		return false;
	}

	public static void Reload()
	{
		SaveAndReload();
	}

	private void OnApplicationFocus(bool focus)
	{
		if (focus)
		{
			StartCoroutine(CheckSessionFlag());
		}
		if (focus && Time.realtimeSinceStartup - sLastPoll > 3f)
		{
			StartCoroutine(PollOnce());
		}
	}

	private void Update()
	{
		ShowPending();
		UIPatches.Tick();
	}

	public static IEnumerator PollOnce()
	{
		sLastPoll = Time.realtimeSinceStartup;
		float startedAt = Time.realtimeSinceStartup;
		int session = sSession;
		string me = ((PlayerInfoScript.SessionLoadedAt >= 0f && Singleton<PlayerInfoScript>.Instance != null) ? Singleton<PlayerInfoScript>.Instance.GetPlayerCode() : null);
		using UnityWebRequest req = UnityWebRequest.Get(SQSettings.CDN_URL + "live");
		req.timeout = 15;
		LiveContent.AddHeaders(req);
		if (!string.IsNullOrEmpty(me))
		{
			req.SetRequestHeader("Player-Id", me);
			req.SetRequestHeader("X-Session", PlayerInfoScript.LaunchSession);
		}
		yield return req.SendWebRequest();
		if (req.result != UnityWebRequest.Result.Success || req.responseCode != 200)
		{
			yield break;
		}
		try
		{
			if (!(Json.Deserialize(req.downloadHandler.text) is Dictionary<string, object> dictionary))
			{
				yield break;
			}
			sLast = dictionary;
			sPolledAt = Time.realtimeSinceStartup;
			UIPatches.SetRules(Get(dictionary, "ui") as List<object>);
			if (dictionary.ContainsKey("session_moved") && !string.IsNullOrEmpty(me) && PlayerInfoScript.SessionLoadedAt >= 0f && startedAt > PlayerInfoScript.SessionLoadedAt)
			{
				if (!PlayerInfoScript.SessionMoved)
				{
					Debug.Log("[Session] account opened in another game");
				}
				PlayerInfoScript.SessionMoved = true;
			}
			if (!sHaveBase)
			{
				sRestartBase = RestartId();
				sHaveBase = true;
			}
			if (sEndedBase == null && session == sSession)
			{
				sEndedBase = EndedId();
			}
		}
		catch (Exception ex)
		{
			Debug.LogWarning("[Live] ignored: " + ex.Message);
		}
	}

	public static bool Allowed(string feature)
	{
		Dictionary<string, object> dictionary = Flag(feature);
		if (dictionary == null || !IsOffNow(dictionary))
		{
			return true;
		}
		string text = Pick(Get(dictionary, "message")) ?? Text("feature_off");
		if (!string.IsNullOrEmpty(text) && Singleton<SimplePopupController>.Instance != null)
		{
			Singleton<SimplePopupController>.Instance.ShowMessage(string.Empty, text);
		}
		Debug.Log("[Live] feature off: " + feature);
		return false;
	}

	public static bool IsOn(string feature)
	{
		Dictionary<string, object> dictionary = Flag(feature);
		if (dictionary != null)
		{
			return !IsOffNow(dictionary);
		}
		return true;
	}

	private static Dictionary<string, object> Flag(string feature)
	{
		return Get(Section("flags"), feature) as Dictionary<string, object>;
	}

	private static bool IsOffNow(Dictionary<string, object> f)
	{
		try
		{
			object obj = Get(f, "on");
			if (!(obj is bool) || (bool)obj)
			{
				return false;
			}
			double num = Now();
			double num2 = Num(Get(f, "from"));
			double num3 = Num(Get(f, "until"));
			string text = Get(f, "min_version") as string;
			return (num2 <= 0.0 || num >= num2) && (num3 <= 0.0 || num < num3) && (string.IsNullOrEmpty(text) || LiveContent.VersionAtLeast(text));
		}
		catch (Exception)
		{
			return false;
		}
	}

	private static void ShowPending()
	{
		if (PlayerInfoScript.SessionMoved)
		{
			LeaveBattleIfIn();
			SimplePopupController instance = Singleton<SimplePopupController>.Instance;
			if (instance != null && instance.CurrentPriority != SimplePopupController.PopupPriority.ServerError && CWKGoogleLink.Instance != null)
			{
				CWKGoogleLink.Instance.AskReconnect();
			}
			return;
		}
		sQuitForMove = false;
		if (sLast == null || !CanShow())
		{
			return;
		}
		try
		{
			if (PlayerInfoScript.DeviceLocked && !sToldDeviceLocked && CWKGoogleLink.Instance != null)
			{
				sToldDeviceLocked = true;
				CWKGoogleLink.Instance.AskDeviceLocked();
			}
			else if (PlayerInfoScript.SaveBlocked && !sToldSaveBlocked)
			{
				sToldSaveBlocked = true;
				Show(Text("save_unreadable") ?? "Your progress could not be loaded on this version of the game.\n\nIt is safe on the server - please update the game.");
			}
			else
			{
				if (RestartDue())
				{
					return;
				}
				Dictionary<string, object> dictionary = (MaintenanceOverlay.Showing ? null : (Get(sLast, "maintenance") as Dictionary<string, object>));
				if (dictionary != null)
				{
					double num = Num(Get(dictionary, "at"));
					double num2 = num - Now();
					if (num2 > 0.0)
					{
						string text = Text("maint_soon");
						if (text != null && Once("maint_soon_" + (long)num))
						{
							Show(text.Replace("{min}", Math.Max(1, (int)Math.Ceiling(num2 / 60.0)).ToString()));
							return;
						}
					}
				}
				if (Get(sLast, "notices") is List<object> list)
				{
					foreach (object item in list)
					{
						Dictionary<string, object> dictionary2 = item as Dictionary<string, object>;
						string text2 = ((dictionary2 != null) ? (Get(dictionary2, "id") as string) : null);
						string text3 = ((dictionary2 != null) ? Pick(Get(dictionary2, "text")) : null);
						if (!string.IsNullOrEmpty(text2) && !string.IsNullOrEmpty(text3) && Once("notice_" + text2))
						{
							Show(text3);
							return;
						}
					}
				}
				string text4 = Get(sLast, "content") as string;
				if (string.IsNullOrEmpty(text4) || string.IsNullOrEmpty(BlueprintCache.ETag) || !(text4 != BlueprintCache.ETag))
				{
					return;
				}
				string text5 = Text("content_new");
				if (text5 != null && Once("content_" + text4))
				{
					Singleton<SimplePopupController>.Instance.ShowPrompt(string.Empty, text5, delegate
					{
						Reload();
					}, delegate
					{
					}, RedeemUI.T("!!RESTART_NOW", "Restart"), RedeemUI.T("!!LATER", "Later"));
				}
			}
		}
		catch (Exception ex)
		{
			Debug.LogWarning("[Live] " + ex.Message);
			sLast = null;
		}
	}

	private static string RestartId()
	{
		if (!(Get(sLast, "restart") is Dictionary<string, object> d))
		{
			return null;
		}
		return Get(d, "id") as string;
	}

	private static bool RestartDue()
	{
		string text = RestartId();
		if (!sHaveBase || sRestarting || string.IsNullOrEmpty(text) || text == sRestartBase)
		{
			return false;
		}
		double num = Num(Get(Get(sLast, "restart") as Dictionary<string, object>, "at")) - Num(Get(sLast, "now")) - (double)(Time.realtimeSinceStartup - sPolledAt);
		if (num > 0.0)
		{
			string text2 = Text("restart_soon");
			if (text2 != null && Once("restart_soon_" + text))
			{
				Show(text2.Replace("{min}", Math.Max(1, (int)Math.Ceiling(num / 60.0)).ToString()));
				return true;
			}
			return false;
		}
		if (!InTownIdle())
		{
			return false;
		}
		sRestartBase = text;
		if (!Once("restart_" + text))
		{
			return false;
		}
		sRestarting = true;
		Debug.Log("[Live] forced restart " + text);
		Singleton<SimplePopupController>.Instance.ShowMessage(string.Empty, Text("restart_now") ?? "Restarting the game for an update.", SaveAndReload);
		return true;
	}

	private static bool InTownIdle()
	{
		TownEnvironmentHolder instance = Singleton<TownEnvironmentHolder>.Instance;
		TownController instance2 = Singleton<TownController>.Instance;
		MouseOrbitCamera instance3 = Singleton<MouseOrbitCamera>.Instance;
		if (instance != null && instance.Loaded && instance2 != null && instance2.IsIntroDone() && instance3 != null)
		{
			return !instance3.IsZoomedInToBuilding();
		}
		return false;
	}

	private static void SaveAndReload()
	{
		bool done = false;
		Action go = delegate
		{
			if (!done)
			{
				done = true;
				if (Singleton<ChatManager>.Instance != null)
				{
					Singleton<ChatManager>.Instance.EndChat();
				}
				SessionManager.Instance.theSession.ReloadGame();
			}
		};
		try
		{
			Singleton<PlayerInfoScript>.Instance.Save(delegate
			{
				go();
			});
		}
		catch (Exception ex)
		{
			Debug.LogWarning("[Live] save before restart: " + ex.Message);
		}
		sInstance.StartCoroutine(After(10f, go));
	}

	private static IEnumerator After(float seconds, Action a)
	{
		yield return new WaitForSecondsRealtime(seconds);
		a();
	}

	private static bool InTown()
	{
		if (DetachedSingleton<SceneFlowManager>.Instance.InFrontEnd())
		{
			return !LoadingScreenController.ShowingLoadingScreen();
		}
		return false;
	}

	private static bool CanShow()
	{
		if (!InTown() || MaintenanceOverlay.Showing || MaintenanceMissed())
		{
			return false;
		}
		SimplePopupController instance = Singleton<SimplePopupController>.Instance;
		if (instance != null)
		{
			return !instance.Busy;
		}
		return false;
	}

	private static void Show(string text)
	{
		if (!string.IsNullOrEmpty(text))
		{
			Singleton<SimplePopupController>.Instance.ShowMessage(string.Empty, text);
		}
	}

	private static bool Once(string key)
	{
		string text = PlayerPrefs.GetString("cwk_live_seen", string.Empty);
		if (("\n" + text + "\n").Contains("\n" + key + "\n"))
		{
			return false;
		}
		text = ((text.Length > 4000) ? text.Substring(text.Length - 2000) : text) + "\n" + key;
		PlayerPrefs.SetString("cwk_live_seen", text.Trim('\n'));
		PlayerPrefs.Save();
		return true;
	}

	private static Dictionary<string, object> Section(string name)
	{
		return Get(sLast, name) as Dictionary<string, object>;
	}

	private static string Text(string key)
	{
		return Pick(Get(Section("texts"), key));
	}

	public static string Pick(object map)
	{
		if (!(map is Dictionary<string, object> d))
		{
			return map as string;
		}
		object obj = Get(d, Language.CurrentLanguage().ToString()) ?? Get(d, "EN");
		if (obj != null)
		{
			return Language.FixArabicText(obj.ToString());
		}
		return null;
	}

	private static object Get(Dictionary<string, object> d, string key)
	{
		if (d == null || key == null || !d.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	private static double Num(object o)
	{
		try
		{
			return (o == null) ? 0.0 : Convert.ToDouble(o, CultureInfo.InvariantCulture);
		}
		catch (Exception)
		{
			return 0.0;
		}
	}

	private static double Now()
	{
		return (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
	}
}
