using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MiniJSON;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class CWKGoogleLink : MonoBehaviour
{
	private delegate void Done(long code, string body);

	// Your Google OAuth configuration. BrokerFlow browser sign-in is configured server-side;
	// these values are retained for your project configuration but are not sent by this client.
	private const string CLIENT_ID = "619682022908-11hn3k45pq8t75tgnkqf013jnfkfrfnv.apps.googleusercontent.com";
	private const string CLIENT_SECRET = "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";
	private const string SCOPE = "openid email profile";

	private const float MAX_WAIT_SECONDS = 300f;
	private const float POLL_INTERVAL_SECONDS = 2f;

	private static CWKGoogleLink mInstance;

	private bool mBusy;

	public static bool LastKnownLinked;

	public static string LastKnownEmail = string.Empty;

	public static int LastKnownReward;

	public static bool LastKnownRelink;

	public const string RewardEvent = "google link reward";

	public Action<string> OnStatus;

	private bool mReportToLogin;

	private const string WaitingText = "Sign in with Google in your browser.\nThis will continue by itself.";

	private const string ChangeWaitingText = "In your browser, first choose the Google account linked now, then the new one.\nThis will continue by itself.";

	private Action<string, string> mOnResult;

	private string mCreateTicket;

	private bool mWaitDone;

	public const string LoggedOutKey = "CWKLoggedOut";

	private string mNativeResult;

	public static CWKGoogleLink Instance
	{
		get
		{
			if (mInstance == null)
			{
				GameObject obj = new GameObject("CWKGoogleLink");
				UnityEngine.Object.DontDestroyOnLoad(obj);
				mInstance = obj.AddComponent<CWKGoogleLink>();
			}
			return mInstance;
		}
	}

	public bool IsBusy => mBusy;

	public bool CanStartNewWithGoogle => !string.IsNullOrEmpty(mCreateTicket);

	public void RefreshStatus(Action<bool, string> done)
	{
		StartCoroutine(StatusRoutine(done));
	}

	private IEnumerator StatusRoutine(Action<bool, string> done)
	{
		string body = null;
		long code = 0L;
		yield return PostJson(ServerUrl("account/google/status"), "{}", withPlayerHeaders: true, delegate(long c, string b)
		{
			code = c;
			body = b;
		});
		Dictionary<string, object> dictionary = Parse(body);
		bool flag = code == 200 && dictionary != null && Bool(dictionary, "linked");
		string text = (flag ? (Str(dictionary, "email") ?? string.Empty) : string.Empty);
		LastKnownLinked = flag;
		LastKnownEmail = text;
		LastKnownReward = ((code == 200) ? Num(dictionary, "reward", 0) : 0);
		LastKnownRelink = code == 200 && !flag && Bool(dictionary, "relink");
		Debug.Log("[CWKGoogleLink] status: http=" + code + " linked=" + flag);
		done?.Invoke(flag, text);
	}

	public void Cancel()
	{
		mWaitDone = true;
		StopAllCoroutines();
		mBusy = false;
		CloseWaitingPopup();
	}

	private static void CloseWaitingPopup()
	{
		SimplePopupController instance = Singleton<SimplePopupController>.Instance;
		if (instance != null && instance.Busy && instance.Body != null && (instance.Body.text == "Sign in with Google in your browser.\nThis will continue by itself." || instance.Body.text == "In your browser, first choose the Google account linked now, then the new one.\nThis will continue by itself."))
		{
			instance.OnClickYes();
		}
	}

	public void BeginLink(Action<string> onLinked = null)
	{
		if (!mBusy)
		{
			mOnResult = null;
			StartCoroutine(BrokerFlow("link", onLinked));
		}
	}

	public void BeginSignIn()
	{
		if (!mBusy)
		{
			mOnResult = null;
			StartCoroutine(BrokerFlow("signin"));
		}
	}

	public void BeginRestore(Action<string, string> onResult = null)
	{
		if (!mBusy)
		{
			mOnResult = onResult;
			StartCoroutine(BrokerFlow("restore"));
		}
	}

	public void BeginChange()
	{
		if (!mBusy)
		{
			mOnResult = null;
			StartCoroutine(BrokerFlow("relink"));
		}
	}

	public void BeginCreate(Action<string, string> onResult)
	{
		if (onResult == null)
		{
			Debug.LogWarning("[CWKGoogleLink] BeginCreate callback is null.");
			return;
		}
		if (mBusy || !CanStartNewWithGoogle)
		{
			onResult(null, "That sign-in expired. Please sign in again.");
		}
		else
		{
			StartCoroutine(CreateFlow(mCreateTicket, onResult));
		}
	}

	private IEnumerator CreateFlow(string ticket, Action<string, string> onResult)
	{
		mBusy = true;
		mCreateTicket = null;
		long code = 0L;
		string body = null;
		yield return PostJson(ServerUrl("account/google/create"), JsonObject("ticket", ticket), withPlayerHeaders: false, delegate(long c, string b)
		{
			code = c;
			body = b;
		});
		mBusy = false;
		Dictionary<string, object> d = Parse(body);
		string text = Str(d, "username");
		if (code == 200 && !string.IsNullOrEmpty(text))
		{
			onResult(text, null);
		}
		else
		{
			onResult(null, Str(d, "error") ?? "Couldn't start a new game. Please try again.");
		}
	}

	private bool Deliver(string player, string error)
	{
		Action<string, string> action = mOnResult;
		if (action == null)
		{
			return false;
		}
		mOnResult = null;
		mBusy = false;
		action(player, error);
		return true;
	}

	private IEnumerator BrokerFlow(string mode, Action<string> onLinked = null)
	{
		mBusy = true;
		mWaitDone = false;
		mCreateTicket = null;
		bool isSignIn = mode == "signin";
		bool isLink = mode == "link";
		bool isChange = mode == "relink";
		mReportToLogin = isSignIn;
		string body = null;
		long code = 0L;
		yield return PostJson(ServerUrl("account/google/begin"), JsonObject("mode", mode, "native", NativeKind()), isLink || isChange, delegate(long c, string b)
		{
			code = c;
			body = b;
		});
		Dictionary<string, object> dictionary = Parse(body);
		if (code == 503)
		{
			if (!Deliver(null, "Google sign-in isn't switched on yet."))
			{
				Report("Google sign-in isn't switched on yet.");
			}
			mBusy = false;
			yield break;
		}
		if (code != 200 || dictionary == null || string.IsNullOrEmpty(Str(dictionary, "ticket")))
		{
			string text = Str(dictionary, "error") ?? "Couldn't start the sign-in. Please try again.";
			if (!Deliver(null, text))
			{
				Report(text);
			}
			mBusy = false;
			yield break;
		}
		string ticket = Str(dictionary, "ticket");
		string native = NativeKind();
		string clientId = Str(dictionary, "client_id");
		string authUrl = Str(dictionary, "auth_url");
		bool viaBrowser = true;
		if ((native == "android" && !string.IsNullOrEmpty(clientId)) || (native == "ios" && !string.IsNullOrEmpty(authUrl)))
		{
			viaBrowser = false;
			for (int step = 1; step <= 2; step++)
			{
				if (isChange && (native == "android" || step == 1))
				{
					yield return ShowAndWait((step == 1) ? "First choose the Google account linked now." : "Now choose your new Google account.");
				}
				mNativeResult = null;
				if (native == "android")
				{
					AndroidSignIn(clientId);
				}
				else
				{
					IOSSignIn(authUrl);
				}
				while (mNativeResult == null)
				{
					yield return null;
				}
				string[] array = mNativeResult.Split(new char[1] { '|' }, 2);
				if (array[0] == "cancel")
				{
					mWaitDone = true;
					mBusy = false;
					Deliver(null, null);
					yield break;
				}
				if (array[0] != "ok" && array[0] != "done")
				{
					Debug.LogWarning("[CWKGoogleLink] in-game sign-in failed, using the browser: " + mNativeResult);
					viaBrowser = true;
					break;
				}
				if (native == "ios")
				{
					break;
				}
				string tb = null;
				yield return PostJson(ServerUrl("account/google/token"), JsonObject("ticket", ticket, "id_token", array[1]), withPlayerHeaders: false, delegate(long c, string b)
				{
					tb = b;
				});
				if (Str(Parse(tb), "status") != "next")
				{
					break;
				}
			}
		}
		if (viaBrowser)
		{
			Application.OpenURL(ServerUrl("account/google/start") + "?ticket=" + WWW.EscapeURL(ticket));
			if (mOnResult != null)
			{
				Popup(string.Empty, isChange ? "In your browser, first choose the Google account linked now, then the new one.\nThis will continue by itself." : "Sign in with Google in your browser.\nThis will continue by itself.", delegate
				{
					if (!mWaitDone)
					{
						Cancel();
						Deliver(null, null);
					}
				}, "Cancel");
			}
			else
			{
				Report(isChange ? "In your browser, first choose the Google account linked now, then the new one.\nThis will continue by itself." : "Sign in with Google in your browser.\nThis will continue by itself.");
			}
		}
		float waited = 0f;
		bool pollNow = !viaBrowser;
		while (waited < MAX_WAIT_SECONDS)
		{
			if (!pollNow)
			{
				yield return new WaitForSeconds(POLL_INTERVAL_SECONDS);
				waited += POLL_INTERVAL_SECONDS;
			}
			pollNow = false;
			string pb = null;
			long pc = 0L;
			yield return Get(ServerUrl("account/google/poll") + "?ticket=" + WWW.EscapeURL(ticket), delegate(long c, string b)
			{
				pc = c;
				pb = b;
			});
			Dictionary<string, object> d = Parse(pb);
			string text2 = Str(d, "status");
			if (text2 == "pending")
			{
				continue;
			}
			mWaitDone = true;
			CloseWaitingPopup();
			if (text2 == "ok")
			{
				string text3 = Str(d, "username");
				if (isLink)
				{
					string text4 = Str(d, "email");
					string text5 = (string.IsNullOrEmpty(text4) ? "your Google account" : text4);
					if (onLinked != null)
					{
						LastKnownEmail = text4 ?? string.Empty;
						LastKnownLinked = true;
						mBusy = false;
						onLinked(text4);
						yield break;
					}
					if (Bool(d, "already"))
					{
						Report("This account is ALREADY linked to:\n" + text5 + "\n\nNothing changed - you only need to link once.");
					}
					else
					{
						Report("Linked successfully to:\n" + text5 + "\n\nOn a new device, sign in with Google to get this account back.");
					}
					LastKnownEmail = text4;
					LastKnownLinked = true;
				}
				else if (isChange)
				{
					string text6 = Str(d, "email");
					LastKnownEmail = text6 ?? string.Empty;
					LastKnownLinked = true;
					Report(Bool(d, "already") ? "That is already the Google account linked to this player. Nothing changed." : ("Your Google account is now:\n" + (string.IsNullOrEmpty(text6) ? "your new Google account" : text6)));
				}
				else if (string.IsNullOrEmpty(text3))
				{
					if (!Deliver(null, "The server did not name an account. Please try again."))
					{
						Report("The server did not name an account. Please try again.");
					}
				}
				else if (isSignIn)
				{
					EnterGameAs(text3);
				}
				else if (!Deliver(text3, null))
				{
					if (text3 == Singleton<PlayerInfoScript>.Instance.GetPlayerCode())
					{
						Report("This device is already playing that account.");
					}
					else
					{
						mBusy = false;
						string found = text3;
						string text7 = Str(d, "email");
						PromptWhenFree(RevivalText.Translate("Switch to the account linked to:") + "\n" + (string.IsNullOrEmpty(text7) ? found : text7) + "\n\n" + RevivalText.Translate("The progress on this device isn't linked to Google and will be lost."), delegate
						{
							StartCoroutine(SwitchAccount(found, loggingOut: false));
						}, null, "Switch", "Cancel");
					}
				}
				mBusy = false;
			}
			else
			{
				string text8 = Str(d, "error") ?? "The sign-in didn't finish. Please try again.";
				mCreateTicket = (Bool(d, "can_create") ? ticket : null);
				if (!Deliver(null, text8))
				{
					Report(text8);
				}
				mBusy = false;
			}
			yield break;
		}
		mWaitDone = true;
		CloseWaitingPopup();
		if (!Deliver(null, "Timed out waiting for the browser. Please try again."))
		{
			Report("Timed out waiting for the browser. Please try again.");
		}
		mBusy = false;
	}

	public static void EnterGameAs(string username)
	{
		WriteIdentity(username);
		try
		{
			File.Delete(Path.Combine(Application.persistentDataPath, "lastplayer"));
		}
		catch (Exception ex)
		{
			Debug.LogWarning("[CWKGoogleLink] could not forget the last player: " + ex.Message);
		}
		PlayerInfoScript instance = Singleton<PlayerInfoScript>.Instance;
		if (instance != null)
		{
			instance.Logout(clearUserdata: false);
			instance.SaveData.PlayerName = username;
		}
		PlayerPrefs.SetInt("Account", 1);
		PlayerPrefs.SetInt("AfterLogin", 0);
		PlayerPrefs.Save();
		Debug.Log("[CWKGoogleLink] signed in as " + username + " - entering game");
		SceneManager.LoadScene("BootVideoScene");
	}

	private void Report(string message)
	{
		if (OnStatus != null && mReportToLogin)
		{
			OnStatus(message);
		}
		else
		{
			Popup(string.Empty, message);
		}
	}

	private static void WriteIdentity(string username)
	{
		PlayerPrefs.SetString("lastUserName", username);
		PlayerPrefs.SetString("Nickname", username);
		PlayerPrefs.Save();
		try
		{
			File.WriteAllText(Path.Combine(Application.persistentDataPath, "lastUserName"), username);
		}
		catch (Exception ex)
		{
			Debug.LogWarning("[CWKGoogleLink] could not write the identity file: " + ex.Message);
		}
	}

	private IEnumerator UnlinkRoutine()
	{
		mBusy = true;
		string body = null;
		long code = 0L;
		yield return PostJson(ServerUrl("account/google/unlink"), "{}", withPlayerHeaders: true, delegate(long c, string b)
		{
			code = c;
			body = b;
		});
		Popup("Google", (code == 200) ? "Your Google account has been unlinked from this player." : (Str(Parse(body), "error") ?? "Couldn't unlink. Please try again."));
		mBusy = false;
	}

	public void AskLogOut()
	{
		if (mBusy)
		{
			Cancel();
		}
		RefreshStatus(delegate(bool linked, string email)
		{
			if (linked)
			{
				AskLogOutLinked(email);
			}
			else
			{
				AskLogOutUnlinked();
			}
		});
	}

	private void AskLogOutLinked(string email)
	{
		PromptWhenFree(RevivalText.Translate("Log out?") + "\n\n" + RevivalText.Translate("You can sign back in any time with Google:") + "\n" + (string.IsNullOrEmpty(email) ? RevivalText.Translate("your Google account") : email), null, LogOutNow, "Stay", "Log Out");
	}

	private void AskLogOutUnlinked()
	{
		PromptWhenFree(RevivalText.Translate("Your account isn't linked to Google.") + "\n\n" + RevivalText.Translate("If you log out now, you will lose this account forever. Link Google first to keep it safe."), delegate
		{
			BeginLink(AskLogOutLinked);
		}, delegate
		{
			PromptWhenFree(RevivalText.Translate("Are you sure? This account and all its progress will be gone forever."), null, LogOutNow, "Go Back", "Log Out");
		}, "Link", "Log Out");
	}

	public void AskDeviceLocked()
	{
		PromptWhenFree(RevivalText.Translate("This account is active on another device.") + "\n\n" + RevivalText.Translate("Sign in with the Google account linked to it to play here too."), delegate
		{
			BeginSignIn();
		}, LogOutNow, "Sign In", "Log Out");
	}

	public void AskReconnect()
	{
		string me = ((Singleton<PlayerInfoScript>.Instance != null) ? Singleton<PlayerInfoScript>.Instance.GetPlayerCode() : null);
		SimplePopupController instance = Singleton<SimplePopupController>.Instance;
		if (!(instance != null))
		{
			return;
		}
		instance.ShowMessage(string.Empty, RevivalText.Translate("Your account has been opened on another device."), delegate
		{
			if (!string.IsNullOrEmpty(me))
			{
				PlayerInfoScript.SessionMoved = false;
				PlayerInfoScript.SessionLoadedAt = -1f;
				EnterGameAs(me);
			}
		}, RevivalText.Translate("Reconnect"), SimplePopupController.PopupPriority.ServerError);
	}

	private void PromptWhenFree(string body, SimplePopupController.PopupButtonCallback yes, SimplePopupController.PopupButtonCallback no, string yesText, string noText)
	{
		StartCoroutine(PromptRoutine(body, yes, no, yesText, noText));
	}

	private static IEnumerator PromptRoutine(string body, SimplePopupController.PopupButtonCallback yes, SimplePopupController.PopupButtonCallback no, string yesText, string noText)
	{
		yield return null;
		SimplePopupController popup = Singleton<SimplePopupController>.Instance;
		if (!(popup == null))
		{
			while (popup.Busy)
			{
				yield return null;
			}
			yield return new WaitForSecondsRealtime(0.2f);
			while (popup.Busy)
			{
				yield return null;
			}
			popup.ShowPrompt(string.Empty, body, yes, no, RevivalText.Translate(yesText), RevivalText.Translate(noText));
			popup.SetBackCallback(null);
		}
	}

	private void LogOutNow()
	{
		StartCoroutine(SwitchAccount(Guid.NewGuid().ToString(), loggingOut: true));
	}

	private IEnumerator SwitchAccount(string username, bool loggingOut)
	{
		bool done = false;
		bool saved = false;
		PlayerInfoScript instance = Singleton<PlayerInfoScript>.Instance;
		if (instance != null)
		{
			instance.Save(delegate(bool ok)
			{
				done = true;
				saved = ok;
			});
		}
		else
		{
			bool flag = true;
			saved = true;
			done = flag;
		}
		float t = 0f;
		while (!done && t < 20f)
		{
			yield return null;
			t += Time.unscaledDeltaTime;
		}
		Debug.Log("[CWKGoogleLink] switching account: saved=" + saved + " logout=" + loggingOut);
		if (!done || (loggingOut && !saved))
		{
			Popup(string.Empty, RevivalText.Translate("Couldn't reach the server. Please try again."));
			yield break;
		}
		string[] array = new string[8] { "Nickname", "Password", "Email", "Admin", "Ban", "Jumpscare", "CardGot", "BattleTeamInstance" };
		for (int num = 0; num < array.Length; num++)
		{
			PlayerPrefs.DeleteKey(array[num]);
		}
		if (loggingOut)
		{
			PlayerPrefs.SetInt("CWKLoggedOut", 1);
		}
		EnterGameAs(username);
	}

	public void OnNativeSignIn(string msg)
	{
		mNativeResult = (string.IsNullOrEmpty(msg) ? "error|" : msg);
	}

	private static string NativeKind()
	{
		return string.Empty;
	}

	private void AndroidSignIn(string webClientId)
	{
		mNativeResult = "error|not android";
	}

	private void IOSSignIn(string authUrl)
	{
		mNativeResult = "error|not ios";
	}

	private static IEnumerator ShowAndWait(string message)
	{
		SimplePopupController popup = Singleton<SimplePopupController>.Instance;
		if (!(popup == null))
		{
			while (popup.Busy)
			{
				yield return null;
			}
			popup.ShowMessage(string.Empty, message, null, "OK");
			while (popup.Busy)
			{
				yield return null;
			}
		}
	}

	private static string ServerUrl(string path)
	{
		string sERVER_URL = SQSettings.SERVER_URL;
		if (string.IsNullOrEmpty(sERVER_URL))
		{
			return path;
		}
		if (!sERVER_URL.EndsWith("/"))
		{
			return sERVER_URL + "/" + path;
		}
		return sERVER_URL + path;
	}

	private IEnumerator Get(string url, Done done)
	{
		using UnityWebRequest req = UnityWebRequest.Get(url);
		req.timeout = 15;
		yield return req.SendWebRequest();
		done(req.responseCode, (req.downloadHandler != null) ? req.downloadHandler.text : null);
	}

	private IEnumerator PostJson(
		string url,
		string json,
		bool withPlayerHeaders,
		Done done)
	{
		using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
		{
			req.uploadHandler =
				new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));

			req.downloadHandler = new DownloadHandlerBuffer();
			req.SetRequestHeader("Content-Type", "application/json");
			req.timeout = 20;

			if (withPlayerHeaders &&
				Singleton<PlayerInfoScript>.Instance != null)
			{
				req.SetRequestHeader(
					"Player-Id",
					Singleton<PlayerInfoScript>.Instance.GetPlayerCode()
				);
			}

			req.SetRequestHeader(
				"X-Nick-Description",
				WWW.EscapeURL(TFUtils.DeviceName ?? string.Empty)
					.Replace("+", "%20")
			);

			req.SetRequestHeader(
				"X-Nick-Legacy",
				WWW.EscapeURL(TFUtils.LegacyDeviceName ?? string.Empty)
					.Replace("+", "%20")
			);

			Debug.Log("[CWKGoogleLink] POST " + url);

			yield return req.SendWebRequest();

			string response =
				req.downloadHandler != null
					? req.downloadHandler.text
					: string.Empty;

			Debug.Log(
				"[CWKGoogleLink] HTTP=" + req.responseCode +
				" Result=" + req.result +
				" Error=" + req.error +
				" Response=" + response
			);

			done?.Invoke(req.responseCode, response);
		}
	}


	private static string JsonObject(params object[] values)
	{
		Dictionary<string, object> data = new Dictionary<string, object>();
		for (int i = 0; i + 1 < values.Length; i += 2)
		{
			string key = values[i] as string;
			if (key != null)
			{
				data[key] = values[i + 1];
			}
		}
		return Json.Serialize(data);
	}

	private static Dictionary<string, object> Parse(string body)
	{
		if (string.IsNullOrEmpty(body))
		{
			return null;
		}
		try
		{
			return Json.Deserialize(body) as Dictionary<string, object>;
		}
		catch
		{
			return null;
		}
	}

	private static bool Bool(Dictionary<string, object> d, string key)
	{
		if (d == null || !d.ContainsKey(key) || d[key] == null)
		{
			return false;
		}
		if (d[key] is bool)
		{
			return (bool)d[key];
		}
		return false;
	}

	private static string Str(Dictionary<string, object> d, string key)
	{
		if (d != null && d.TryGetValue(key, out var value) && value != null)
		{
			return value.ToString();
		}
		return null;
	}

	private static int Num(Dictionary<string, object> d, string key, int fallback)
	{
		if (d != null && d.TryGetValue(key, out var value) && value != null && int.TryParse(value.ToString(), out var result))
		{
			return result;
		}
		return fallback;
	}

	private static void Popup(string title, string message)
	{
		Popup(title, message, null, "OK");
	}

	private static void Popup(string title, string message, SimplePopupController.PopupButtonCallback cb, string button)
	{
		SimplePopupController instance = Singleton<SimplePopupController>.Instance;
		string body = (string.IsNullOrEmpty(title) ? message : (title + "\n\n" + message));
		if (instance != null)
		{
			instance.ShowMessage(string.Empty, body, cb, button);
		}
		else
		{
			Debug.Log("[CWKGoogleLink] " + title + ": " + message);
		}
	}

	
	private void Awake()
	{
		Debug.Log(
			"[CWKGoogleLink] Awake çağrıldı. InstanceID="
			+ GetInstanceID()
		);

		if (mInstance != null && mInstance != this)
		{
			Debug.LogWarning(
				"[CWKGoogleLink] Kopya nesne siliniyor. InstanceID="
				+ GetInstanceID()
			);

			Destroy(gameObject);
			return;
		}

		mInstance = this;
		DontDestroyOnLoad(gameObject);
	}

}
