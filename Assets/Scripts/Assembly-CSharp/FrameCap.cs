using System;
using System.Collections;
using System.IO;
using UnityEngine;

public class FrameCap : MonoBehaviour
{
	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
	private static void Boot()
	{
		string[] commandLineArgs = Environment.GetCommandLineArgs();
		if (!Application.isMobilePlatform && QualitySettings.vSyncCount == 0 && Application.targetFrameRate <= 0 && Array.IndexOf(commandLineArgs, "-nocap") < 0)
		{
			double value = Screen.currentResolution.refreshRateRatio.value;
			Application.targetFrameRate = Mathf.Max(240, Mathf.CeilToInt((float)value * 2f));
		}
		if (Array.IndexOf(commandLineArgs, "-fpsprobe") >= 0)
		{
			GameObject obj = new GameObject("FrameCapProbe");
			UnityEngine.Object.DontDestroyOnLoad(obj);
			obj.AddComponent<FrameCap>();
		}
	}

	private IEnumerator Start()
	{
		Application.runInBackground = true;
		while (UnityEngine.Object.FindObjectOfType<TownController>() == null)
		{
			yield return new WaitForSeconds(1f);
		}
		yield return new WaitForSeconds(15f);
		int f0 = Time.frameCount;
		float t0 = Time.realtimeSinceStartup;
		yield return new WaitForSecondsRealtime(20f);
		float num = (float)(Time.frameCount - f0) / (Time.realtimeSinceStartup - t0);
		string text = Path.Combine(Application.persistentDataPath, "tradeshots");
		Directory.CreateDirectory(text);
		File.WriteAllText(Path.Combine(text, "fps.txt"), "fps " + num.ToString("F1") + " cap " + Application.targetFrameRate + " hz " + Screen.currentResolution.refreshRateRatio.value.ToString("F1") + "\n");
		Application.Quit();
	}
}
