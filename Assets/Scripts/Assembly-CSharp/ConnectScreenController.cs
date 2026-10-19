using System;
using System.Collections;
using UnityEngine;

public class ConnectScreenController : MonoBehaviour
{
	public UITweenController ShowBusy;

	public UITweenController HideBusy;

	public TermsOfServicePanel TOSPanel;

	public UITexture ChinaScreenTexture;

	public UITexture LoadingLogo;

	private bool IsComplete;

	private bool mWaitingForGameSelection;

	private bool mPlayerInfoLoading;

	private bool mFirstStepGuestLoginStarted;

	private bool mRealLoginStarted;

	private bool mLoginConfirmed;

	private bool mReadyCheck;

	private bool mUseFacebook;

	private LoginController mloginController;

	public bool IsLogoTweenCompleted;

	private const string NewGoogleGameKey = "CWKNewGoogleGame";

	private void OnLogingError()
	{
		if (HideBusy != null)
		{
			HideBusy.Play();
		}
		Singleton<SimplePopupController>.Instance.ShowMessage(string.Empty, KFFLocalization.Get("!!SERVER_ERROR_MESSAGE"), RetryLogin, KFFLocalization.Get("!!RETRY"));
	}

	private void RetryLogin()
	{
		Singleton<PlayerInfoScript>.Instance.Login();
	}

	private void OnEnable()
	{
		Singleton<PlayerInfoScript>.Instance.OnLoginFail += OnLogingError;
	}

	private void OnDisable()
	{
		if (Singleton<PlayerInfoScript>.Instance != null)
		{
			Singleton<PlayerInfoScript>.Instance.OnLoginFail -= OnLogingError;
			if (mloginController != null)
			{
				mloginController.OnGuestLogin -= GuestLoginSelected;
			}
		}
	}

	private void OnDestroy()
	{
		try
		{
			LoadingLogo.UnloadTexture();
		}
		catch (Exception)
		{
		}
	}

	public void PreGuestLogin()
	{
		if (HideBusy != null)
		{
			HideBusy.End();
		}
		if (Singleton<PlayerInfoScript>.Instance.HasAuthedBefore)
		{
			mRealLoginStarted = true;
			mLoginConfirmed = true;
		}
		if (ShowBusy != null)
		{
			ShowBusy.Play();
		}
		mFirstStepGuestLoginStarted = true;
	}

	public void LoginPrompt()
	{
		if (HideBusy != null)
		{
			HideBusy.Play();
		}
		LoginController loginController = UnityEngine.Object.FindObjectOfType<LoginController>();
		if (loginController != null)
		{
			loginController.OnGuestLogin += GuestLoginSelected;
			mLoginConfirmed = false;
			loginController.ActivateLoginDialog();
		}
		else
		{
			if (ShowBusy != null)
			{
				ShowBusy.Play();
			}
			Singleton<PlayerInfoScript>.Instance.Login();
			mLoginConfirmed = true;
		}
		mRealLoginStarted = true;
	}

	public void GuestLoginSelected()
	{
		mLoginConfirmed = true;
		mUseFacebook = false;
		if (ShowBusy != null)
		{
			ShowBusy.Play();
		}
	}

	
	private void Start()
	{
<<<<<<< HEAD
		if (!CWKGameModeManager.HasSelectedModeThisSession)
		{
			mWaitingForGameSelection = true;
			CWKGameModeManager manager = CWKGameModeManager.Instance;
			if (manager != null)
			{
				manager.LoadChooseGameScene();
			}
			else
			{
				Debug.LogError("[ConnectScreenController] CWKGameModeManager could not be initialized.");
			}
			return;
		}

		if (ChinaScreenTexture != null)
			ChinaScreenTexture.gameObject.SetActive(true);
=======
		Debug.Log(
			"[ConnectScreenController] Start. HasSelectedModeThisSession=" +
			CWKGameModeManager.HasSelectedModeThisSession
		);

		if (!CWKGameModeManager.HasSelectedModeThisSession)
		{
			mWaitingForGameSelection = true;

			Debug.Log(
				"[ConnectScreenController] Waiting for game selection."
			);

			CWKGameModeManager manager = CWKGameModeManager.Instance;

			if (manager != null)
			{
				Debug.Log(
					"[ConnectScreenController] Manager found. " +
					"Requesting ChooseGame scene."
				);

			}
			else
			{
				Debug.LogError(
					"[ConnectScreenController] CWKGameModeManager is null."
				);
			}

			return;
		}

		Debug.Log(
			"[ConnectScreenController] Game mode already selected this session."
		);

		if (ChinaScreenTexture != null)
			ChinaScreenTexture.gameObject.SetActive(true);

>>>>>>> ab1e8e11 (Fix SQSettings server mode integration)
		LoadingScreenController.LoadLoadingScreenLogo(LoadingLogo);
	}


	private void Update()
	{
		if (mWaitingForGameSelection)
		{
			return;
		}
		if (IsComplete)
		{
			return;
		}
		if (!mPlayerInfoLoading)
		{
			Singleton<PlayerInfoScript>.Instance.Login();
			mPlayerInfoLoading = true;
		}
		if (!Singleton<PlayerInfoScript>.Instance.IsReady())
		{
			return;
		}
		if (!mFirstStepGuestLoginStarted)
		{
			PreGuestLogin();
		}
		if (!mRealLoginStarted)
		{
			GuestLoginSelected();
		}
		if (!mLoginConfirmed)
		{
			return;
		}
		mReadyCheck = true;
		if (!mReadyCheck)
		{
			return;
		}
		IsComplete = true;
		if (HideBusy != null)
		{
			HideBusy.Play();
		}
		if (Singleton<PlayerInfoScript>.Instance.SaveData.ConfirmedTOSVersion == 0)
		{
			TOSPanel.Show(delegate
			{
				Singleton<PlayerInfoScript>.Instance.SaveData.ConfirmedTOSVersion = 1;
				Proceed();
			});
		}
		else
		{
			Proceed();
		}
	}

	private void AskPlayedBefore()
	{
		if (PlayerPrefs.GetInt("SkipLogin", 0) == 1)
		{
			Debug.Log("[ConnectScreen] Guest mode: skipping Google restore prompt.");
			AskSkipTutorial();
			return;
		}
		SimplePopupController instance = Singleton<SimplePopupController>.Instance;
		if (instance == null || !LiveChannel.IsOn("GoogleRestore") || PlayerPrefs.GetInt("CWKNewGoogleGame", 0) == 1)
		{
			PlayerPrefs.DeleteKey("CWKNewGoogleGame");
			AskSkipTutorial();
		}
		else if (PlayerPrefs.GetInt("CWKLoggedOut", 0) == 1)
		{
			instance.ShowPrompt(string.Empty, RevivalText.Translate("You're logged out.") + "\n\n" + RevivalText.Translate("Sign in with Google to continue your account, or start a new game."), delegate
			{
				CWKGoogleLink.Instance.BeginRestore(OnRestoreResult);
			}, AskSkipTutorial, RevivalText.Translate("Sign In"), RevivalText.Translate("New Game"));
		}
		else
		{
			instance.ShowPrompt(string.Empty, RevivalText.Translate("Have you played before?") + "\n\n" + RevivalText.Translate("Sign in with Google to get your account back."), delegate
			{
				CWKGoogleLink.Instance.BeginRestore(OnRestoreResult);
			}, AskSkipTutorial, KFFLocalization.Get("!!YES"), KFFLocalization.Get("!!NO"));
		}
	}

	private void OnRestoreResult(string player, string error)
	{
		if (!string.IsNullOrEmpty(player) && player == Singleton<PlayerInfoScript>.Instance.GetPlayerCode())
		{
			AskSkipTutorial();
		}
		else if (!string.IsNullOrEmpty(player))
		{
			PlayerPrefs.DeleteKey("CWKLoggedOut");
			CWKGoogleLink.EnterGameAs(player);
		}
		else
		{
			StartCoroutine(AfterNoRestore(error));
		}
	}

	private IEnumerator AfterNoRestore(string error)
	{
		yield return null;
		SimplePopupController popup = Singleton<SimplePopupController>.Instance;
		while (popup != null && popup.Busy)
		{
			yield return null;
		}
		if (popup == null)
		{
			AskSkipTutorial();
		}
		else if (string.IsNullOrEmpty(error))
		{
			AskPlayedBefore();
		}
		else if (CWKGoogleLink.Instance != null && CWKGoogleLink.Instance.CanStartNewWithGoogle)
		{
			popup.ShowPrompt(string.Empty, RevivalText.Translate(error) + "\n\n" + RevivalText.Translate("Start a new game with this Google account?"), delegate
			{
				CWKGoogleLink.Instance.BeginCreate(OnCreateResult);
			}, AskPlayedBefore, RevivalText.Translate("New Game"), RevivalText.Translate("Back"));
		}
		else
		{
			popup.ShowMessage(string.Empty, RevivalText.Translate(error), AskPlayedBefore);
		}
	}

	private void OnCreateResult(string player, string error)
	{
		if (!string.IsNullOrEmpty(player))
		{
			PlayerPrefs.DeleteKey("CWKLoggedOut");
			PlayerPrefs.SetInt("CWKNewGoogleGame", 1);
			CWKGoogleLink.EnterGameAs(player);
		}
		else
		{
			StartCoroutine(AfterNoRestore(error));
		}
	}

	private void AskSkipTutorial()
	{
		PlayerPrefs.DeleteKey("CWKLoggedOut");
		if (LiveChannel.IsOn("SkipTutorial") && Singleton<SimplePopupController>.Instance != null)
		{
			Singleton<SimplePopupController>.Instance.ShowPrompt(string.Empty, "Skip the tutorial?", delegate
			{
				Singleton<TutorialController>.Instance.LoadInitialTutorialQuest();
			}, delegate
			{
				Singleton<TutorialController>.Instance.SkipAllTutorials();
				DetachedSingleton<SceneFlowManager>.Instance.LoadFrontEndScene();
			}, "Play", "Skip");
		}
		else
		{
			Singleton<TutorialController>.Instance.LoadInitialTutorialQuest();
		}
	}

	private void Proceed()
	{
		if (!Singleton<TutorialController>.Instance.IsBlockComplete("IntroBattle"))
		{
			Singleton<KFFSocialManager>.Instance.ReportAchievement(KFFSocialManager.AchievementIDs.DW_NEWGAME);
			Singleton<PlayerInfoScript>.Instance.SaveData.SetInstalledDate(SessionManager.Instance.theSession.ThePlayer.InstalledDate);
			AskPlayedBefore();
		}
		else
		{
			DetachedSingleton<SceneFlowManager>.Instance.LoadFrontEndScene();
		}
	}
}