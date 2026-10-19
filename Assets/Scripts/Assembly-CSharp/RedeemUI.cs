using System.Collections;
using UnityEngine;

public static class RedeemUI
{
	public static void Prompt()
	{
		SimplePopupController popup = Singleton<SimplePopupController>.Instance;
		popup.ShowInput(T("!!REDEEM_TITLE", "Redeem Code"), delegate
		{
			string inputValue = popup.GetInputValue();
			popup.StartCoroutine(Run(inputValue));
		});
	}

	public static IEnumerator Run(string code)
	{
		SimplePopupController popup = Singleton<SimplePopupController>.Instance;
		if (!TownSettingsController.testInternetConnection())
		{
			yield return WaitForPopup();
			popup.ShowMessage(string.Empty, T("!!REDEEM_ERROR", "Couldn't reach the server. Check your connection and try again."));
			yield break;
		}
		bool done = false;
		Singleton<BusyIconPanelController>.Instance.Show();
		RewardManager.RedeemCodeReward(code, delegate
		{
			done = true;
		});
		float until = Time.realtimeSinceStartup + 20f;
		while (!done && Time.realtimeSinceStartup < until)
		{
			yield return null;
		}
		Singleton<BusyIconPanelController>.Instance.Hide();
		if (!done)
		{
			RewardManager.LastRedeemReason = "ERROR";
		}
		yield return WaitForPopup();
		ShowResult();
	}

	public static void ShowResult()
	{
		Singleton<SimplePopupController>.Instance.ShowMessage(string.Empty, Message(RewardManager.LastRedeemReason, RewardManager.LastRedeemRewards));
	}

	public static string Message(string reason, string rewards)
	{
		switch (reason)
		{
		case "OK":
			if (!string.IsNullOrEmpty(rewards))
			{
				return string.Format(T("!!REDEEM_OK", "Code redeemed!\n\nYou got: {0}"), rewards);
			}
			return T("!!REDEEM_OK_PLAIN", "Code redeemed!");
		case "EMPTY":
			return T("!!REDEEM_EMPTY", "Type a code first.");
		case "INVALID":
			return T("!!REDEEM_INVALID", "That code doesn't exist. Check the spelling and try again.");
		case "USED":
			return T("!!REDEEM_USED", "You have already redeemed this code.");
		case "EXPIRED":
			return T("!!REDEEM_EXPIRED", "This code has expired.");
		case "NOT_STARTED":
			return T("!!REDEEM_NOT_STARTED", "This code isn't active yet. Try again later.");
		case "LIMIT":
			return T("!!REDEEM_LIMIT", "This code has been fully claimed.");
		case "BUSY":
			return T("!!REDEEM_BUSY", "Too many tries. Wait a few minutes and try again.");
		case "LOCKED":
			return T("!!REDEEM_LOCKED", "This account is locked to another device, so it can't redeem codes here.");
		default:
			return T("!!REDEEM_ERROR", "Couldn't reach the server. Check your connection and try again.");
		}
	}

	public static string Amount(string key, string fallback, int n)
	{
		return string.Format(T(key, fallback), n.ToString("N0"));
	}

	public static string T(string key, string fallback)
	{
		string text = KFFLocalization.Get(key);
		if (!string.IsNullOrEmpty(text) && !text.Contains(key.Substring(2)))
		{
			return text;
		}
		return fallback;
	}

	private static IEnumerator WaitForPopup()
	{
		SimplePopupController popup = Singleton<SimplePopupController>.Instance;
		float until = Time.realtimeSinceStartup + 3f;
		yield return null;
		while (popup != null && popup.Busy && Time.realtimeSinceStartup < until)
		{
			yield return null;
		}
	}
}
