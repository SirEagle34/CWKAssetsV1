using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Text;
using CodeStage.AntiCheat.ObscuredTypes;
using UnityEngine;

public class MaintShots : MonoBehaviour
{
	private string mDir;

	private bool mLive;

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
	private static void Boot()
	{
		string[] commandLineArgs = Environment.GetCommandLineArgs();
		bool flag = Array.IndexOf(commandLineArgs, "-maintlive") >= 0;
		if (flag || Array.IndexOf(commandLineArgs, "-maintshots") >= 0)
		{
			Application.runInBackground = true;
			GameObject obj = new GameObject(flag ? "prime[31]" : "MaintShots");
			UnityEngine.Object.DontDestroyOnLoad(obj);
			obj.AddComponent<MaintShots>().mLive = flag;
		}
	}

	private IEnumerator Live()
	{
		string[] files = Directory.GetFiles(mDir, "ml_0*");
		for (int i = 0; i < files.Length; i++)
		{
			File.Delete(files[i]);
		}
		files = new string[5] { "done.txt", "pause.txt", "resume.txt", "stop.txt", "ml_log.txt" };
		foreach (string path in files)
		{
			File.Delete(Path.Combine(mDir, path));
		}
		BindingFlags bindingAttr = BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic;
		FieldInfo phaseField = typeof(MaintenanceOverlay).GetField("mPhase", bindingAttr);
		MethodInfo onPlay = typeof(MaintenanceOverlay).GetMethod("OnPlay", bindingAttr);
		FieldInfo baseField = typeof(LiveChannel).GetField("sEndedBase", bindingAttr);
		FieldInfo instField = typeof(LiveChannel).GetField("sInstance", bindingAttr);
		StringBuilder log = new StringBuilder();
		float backSince = -1f;
		GameObject sleeping = null;
		PlayerInfoScript instance = Singleton<PlayerInfoScript>.Instance;
		string text = instance.SaveData.MultiplayerLevel.ToString();
		ObscuredInt pointsInMultiplayerLevel = instance.SaveData.PointsInMultiplayerLevel;
		log.AppendLine("rank before: level " + text + " points " + pointsInMultiplayerLevel.ToString());
		if (Array.IndexOf(Environment.GetCommandLineArgs(), "-maintbattle") >= 0)
		{
			QuestData questData = null;
			foreach (QuestData item in QuestDataManager.Instance.GetDatabase())
			{
				if (questData == null && item.League != null && item.League.QuestLine == QuestLineEnum.Main)
				{
					questData = item;
				}
			}
			instance.StateData.CurrentActiveQuest = questData;
			instance.StateData.CurrentLoadout = instance.GetCurrentLoadout().Copy();
			instance.StateData.SelectedHelper = null;
			instance.SaveData.RankedPvpMatchStarted = true;
			log.AppendLine("battle: " + ((questData == null) ? "default" : questData.ID) + ", ranked-in-progress flag set");
			DetachedSingleton<SceneFlowManager>.Instance.LoadBattleScene();
		}
		int frame = 0;
		for (int n = 0; n < 4000; n++)
		{
			MaintenanceOverlay ov = UnityEngine.Object.FindObjectOfType<MaintenanceOverlay>();
			string phase = ((ov != null) ? phaseField.GetValue(ov).ToString() : "none");
			double at;
			double eta;
			string message;
			bool flag = LiveChannel.Maintenance(out at, out eta, out message);
			bool flag2 = UnityEngine.Object.FindObjectOfType<TownController>() != null;
			instance = Singleton<PlayerInfoScript>.Instance;
			log.AppendLine(n + " " + DateTime.Now.ToString("HH:mm:ss") + " phase=" + phase + " announced=" + flag + " startsIn=" + (flag ? ((int)(at - LiveChannel.ServerNow())).ToString() : "-") + " town=" + flag2 + " loading=" + LoadingScreenController.ShowingLoadingScreen() + " battle=" + DetachedSingleton<SceneFlowManager>.Instance.InBattleScene() + " str=" + RedeemUI.T("!!MAINT_E2E", "none") + " etag=" + BlueprintCache.ETag + " base=" + (baseField.GetValue(null) ?? "null")?.ToString() + " missed=" + LiveChannel.MaintenanceMissed() + ((sleeping != null) ? " ASLEEP" : string.Empty) + " rankedFlag=" + (instance != null && instance.SaveData != null && instance.SaveData.RankedPvpMatchStarted));
			File.WriteAllText(Path.Combine(mDir, "ml_log.txt"), log.ToString());
			if (n % 2 == 0)
			{
				yield return Shot("ml_" + frame++.ToString("00000"));
			}
			if (phase == "Back")
			{
				if (backSince < 0f)
				{
					backSince = Time.realtimeSinceStartup;
				}
				else if (Time.realtimeSinceStartup - backSince > 4f && ov != null)
				{
					log.AppendLine("tap Play");
					onPlay.Invoke(ov, new object[1]);
					backSince = -1f;
					yield return new WaitForSeconds(3f);
				}
			}
			else
			{
				backSince = -1f;
			}
			if (File.Exists(Path.Combine(mDir, "pause.txt")) && sleeping == null)
			{
				File.Delete(Path.Combine(mDir, "pause.txt"));
				MonoBehaviour monoBehaviour = instField.GetValue(null) as MonoBehaviour;
				if (monoBehaviour != null)
				{
					sleeping = monoBehaviour.gameObject;
					sleeping.SetActive(value: false);
					log.AppendLine("pause: app asleep (no check-ins, no maintenance screen)");
				}
			}
			if (File.Exists(Path.Combine(mDir, "resume.txt")) && sleeping != null)
			{
				File.Delete(Path.Combine(mDir, "resume.txt"));
				sleeping.SetActive(value: true);
				sleeping.SendMessage("OnApplicationFocus", true);
				sleeping = null;
				log.AppendLine("resume: app awake (focus)");
			}
			if (File.Exists(Path.Combine(mDir, "stop.txt")))
			{
				break;
			}
			yield return new WaitForSeconds(1f);
		}
		instance = Singleton<PlayerInfoScript>.Instance;
		string[] obj = new string[6]
		{
			"rank at the end: level ",
			instance.SaveData.MultiplayerLevel.ToString(),
			" points ",
			null,
			null,
			null
		};
		pointsInMultiplayerLevel = instance.SaveData.PointsInMultiplayerLevel;
		obj[3] = pointsInMultiplayerLevel.ToString();
		obj[4] = " rankedFlag ";
		obj[5] = instance.SaveData.RankedPvpMatchStarted.ToString();
		log.AppendLine(string.Concat(obj));
		File.WriteAllText(Path.Combine(mDir, "ml_log.txt"), log.ToString());
		File.WriteAllText(Path.Combine(mDir, "done.txt"), "OK live");
	}

	private IEnumerator Start()
	{
		mDir = Path.Combine(Application.persistentDataPath, "tradeshots");
		Directory.CreateDirectory(mDir);
		string[] files;
		if (mLive)
		{
			if (Array.IndexOf(Environment.GetCommandLineArgs(), "-maintlaunch") >= 0)
			{
				files = Directory.GetFiles(mDir, "ml_launch_*");
				for (int i = 0; i < files.Length; i++)
				{
					File.Delete(files[i]);
				}
				for (int n = 0; n < 300; n++)
				{
					if (!(UnityEngine.Object.FindObjectOfType<TownController>() == null))
					{
						break;
					}
					yield return Shot("ml_launch_" + n.ToString("000"));
					yield return new WaitForSeconds(2f);
				}
			}
			while (UnityEngine.Object.FindObjectOfType<TownController>() == null)
			{
				yield return new WaitForSeconds(1f);
			}
			yield return Live();
			yield break;
		}
		mDir = Path.Combine(Application.persistentDataPath, "tradeshots");
		Directory.CreateDirectory(mDir);
		files = Directory.GetFiles(mDir, "ms_*");
		for (int i = 0; i < files.Length; i++)
		{
			File.Delete(files[i]);
		}
		File.Delete(Path.Combine(mDir, "done.txt"));
		while (UnityEngine.Object.FindObjectOfType<TownController>() == null)
		{
			yield return new WaitForSeconds(1f);
		}
		yield return new WaitForSeconds(12f);
		LiveChannel.SimLen = 25.0;
		LiveChannel.SimAt = (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds + 75.0;
		yield return new WaitForSeconds(3f);
		yield return Shot("ms_1_countdown");
		yield return new WaitForSeconds(60f);
		yield return Shot("ms_2_last_minute");
		yield return new WaitForSeconds(16f);
		yield return Shot("ms_3_closed");
		yield return new WaitForSeconds(4f);
		yield return Shot("ms_4_closed_later");
		yield return new WaitForSeconds(24f);
		yield return Shot("ms_5_back");
		File.WriteAllText(Path.Combine(mDir, "done.txt"), "OK showing=" + MaintenanceOverlay.Showing);
	}

	private IEnumerator Shot(string name)
	{
		yield return new WaitForEndOfFrame();
		ScreenCapture.CaptureScreenshot(Path.Combine(mDir, name + ".png"));
		Debug.Log("[MaintShots] " + name);
	}
}
