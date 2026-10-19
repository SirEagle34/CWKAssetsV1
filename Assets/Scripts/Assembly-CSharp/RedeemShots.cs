using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;

public class RedeemShots : MonoBehaviour
{
	private string[] mCodes;

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
	private static void Boot()
	{
		string[] commandLineArgs = Environment.GetCommandLineArgs();
		int num = Array.IndexOf(commandLineArgs, "-redeemshots");
		if (num >= 0 && num + 1 < commandLineArgs.Length)
		{
			Application.runInBackground = true;
			GameObject obj = new GameObject("RedeemShots");
			UnityEngine.Object.DontDestroyOnLoad(obj);
			obj.AddComponent<RedeemShots>().mCodes = commandLineArgs[num + 1].Split(',');
		}
	}

	private IEnumerator Start()
	{
		string dir = Path.Combine(Application.persistentDataPath, "tradeshots");
		Directory.CreateDirectory(dir);
		string[] files = Directory.GetFiles(dir, "rs_*");
		for (int i = 0; i < files.Length; i++)
		{
			File.Delete(files[i]);
		}
		File.Delete(Path.Combine(dir, "done.txt"));
		while (UnityEngine.Object.FindObjectOfType<TownController>() == null)
		{
			yield return new WaitForSeconds(1f);
		}
		yield return new WaitForSeconds(12f);
		PlayerSaveData save = Singleton<PlayerInfoScript>.Instance.SaveData;
		StringBuilder log = new StringBuilder();
		for (int n = 0; n < mCodes.Length; n++)
		{
			int gems = save.HardCurrency;
			int coins = save.SoftCurrency;
			int creatures = save.CreatureCount;
			yield return StartCoroutine(RedeemUI.Run(mCodes[n]));
			yield return new WaitForSeconds(1.5f);
			yield return new WaitForEndOfFrame();
			ScreenCapture.CaptureScreenshot(Path.Combine(dir, "rs_" + n + "_" + mCodes[n] + ".png"));
			log.AppendLine(mCodes[n] + ": " + RewardManager.LastRedeemReason + " [" + RewardManager.LastRedeemRewards + "] gems " + gems + "->" + save.HardCurrency + " coins " + coins + "->" + save.SoftCurrency + " creatures " + creatures + "->" + save.CreatureCount);
			yield return new WaitForSeconds(1f);
			Singleton<SimplePopupController>.Instance.OnClickYes();
			yield return new WaitForSeconds(2f);
		}
		File.WriteAllText(Path.Combine(dir, "rs_log.txt"), log.ToString());
		File.WriteAllText(Path.Combine(dir, "done.txt"), "OK");
	}
}
