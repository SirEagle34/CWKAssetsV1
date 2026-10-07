using UnityEngine;

public class AssetBundleProgressPopup :
	Singleton<AssetBundleProgressPopup>
{
	public float DelayBeforeSpinnerAppearing = 0.25f;

	public float DelayBeforeBarAppearing = 0.75f;

	public float SingleBarLabelOffset = 20f;

	public GameObject Panel;

	public GameObject ProgressBarParent;

	public UISprite ProgressBar;

	public UISprite FileProgressBar;

	public UILabel ProgressLabel;

	public Transform LoadingLabel;

	// ============================================================
	// OPTIONAL LABELS
	// ============================================================

	public UILabel DownloadSizeLabel;

	public UILabel CurrentFileLabel;

	public UILabel SpeedLabel;

	public UILabel RemainingTimeLabel;

	public UILabel FileSizeLabel;

	// ============================================================

	private float mTimeLoading = -1f;

	private Vector3 mBaseLabelPos;

	private bool mShowFileProgress;

	// ============================================================
	// AWAKE
	// ============================================================

	private void Awake()
	{
		if (LoadingLabel != null)
		{
			mBaseLabelPos =
				LoadingLabel.localPosition;
		}
	}

	// ============================================================
	// UPDATE
	// ============================================================

	private void Update()
	{
		// ========================================================
		// BATTLE SCENE
		// ========================================================
		// AssetBundleProgressPopup must NEVER interfere with
		// BattleScene input/UI.
		// ========================================================

		if (IsBattleScene())
		{
			ForceBattleSceneInputReset();

			if (mTimeLoading != -1f)
			{
				StopDisplay();
			}

			return;
		}

		// ========================================================
		// RESOURCE MANAGER
		// ========================================================

		SLOTResourceManager resourceManager =
			Singleton<SLOTResourceManager>.Instance;

		if (resourceManager == null)
		{
			return;
		}

		float totalProgress;
		float fileProgress;

		resourceManager.GetResourceLoadProgress(
			out totalProgress,
			out fileProgress
		);

		// No active resource loading.
		if (totalProgress < 0f)
		{
			StopDisplay();
			return;
		}

		// ========================================================
		// LOADING SCREEN CHECK
		// ========================================================

		bool showingLoadingScreen = false;

		try
		{
			showingLoadingScreen =
				LoadingScreenController
					.ShowingLoadingScreenNotFadingOut();
		}
		catch
		{
			showingLoadingScreen = false;
		}

		// ========================================================
		// ASSET BUNDLE DOWNLOAD SCENE CHECK
		// ========================================================

		bool assetBundleScene = false;

		try
		{
			if (DetachedSingleton<SceneFlowManager>
				.Instance != null)
			{
				assetBundleScene =
					DetachedSingleton<SceneFlowManager>
						.Instance
						.GetCurrentScene() ==
					SceneFlowManager.Scene.AssetBundleDownload;
			}
		}
		catch
		{
			assetBundleScene = false;
		}

		bool showPopup =
			showingLoadingScreen ||
			assetBundleScene;

		float previousTime =
			mTimeLoading;

		// ========================================================
		// INITIALIZE DISPLAY
		// ========================================================

		if (mTimeLoading == -1f)
		{
			mTimeLoading =
				showPopup
					? DelayBeforeBarAppearing
					: 0f;
		}

		mTimeLoading +=
			Time.deltaTime;

		// ========================================================
		// PANEL
		// ========================================================

		if (previousTime <
				DelayBeforeSpinnerAppearing &&
			mTimeLoading >=
				DelayBeforeSpinnerAppearing)
		{
			if (Panel != null)
			{
				Panel.SetActive(true);
			}

			if (ProgressBarParent != null)
			{
				ProgressBarParent.SetActive(false);
			}
		}

		if (previousTime <
				DelayBeforeBarAppearing &&
			mTimeLoading >=
				DelayBeforeBarAppearing)
		{
			if (ProgressBarParent != null)
			{
				ProgressBarParent.SetActive(true);
			}

			ShowBusyIcon();
		}

		if (ProgressBarParent == null ||
			!ProgressBarParent.activeSelf)
		{
			return;
		}

		// ========================================================
		// OVERALL PROGRESS
		// ========================================================

		if (ProgressBar != null)
		{
			ProgressBar.fillAmount =
				Mathf.Clamp01(totalProgress);
		}

		if (ProgressLabel != null)
		{
			ProgressLabel.text =
				Mathf.RoundToInt(
					Mathf.Clamp01(totalProgress) *
					100f
				) + "%";
		}

		// ========================================================
		// DOWNLOAD INFORMATION
		// ========================================================

		long currentBytes =
			resourceManager.CurrentDownloadBytes;

		long currentTotalBytes =
			resourceManager.CurrentDownloadTotalBytes;

		long totalBytes =
			resourceManager.TotalDownloadBytes;

		long downloadedBytes =
			resourceManager.TotalDownloadedBytes;

		float speed =
			resourceManager.DownloadSpeed;

		string bundleName =
			resourceManager.CurrentDownloadBundle;

		// ========================================================
		// SINGLE PROGRESS BAR
		// ========================================================

		if (FileProgressBar != null)
		{
			if (FileProgressBar.transform.parent != null)
			{
				FileProgressBar.transform.parent.gameObject
					.SetActive(false);
			}
			else
			{
				FileProgressBar.gameObject
					.SetActive(false);
			}
		}

		if (LoadingLabel != null)
		{
			LoadingLabel.localPosition =
				mBaseLabelPos;
		}

		mShowFileProgress = false;

		// ========================================================
		// CURRENT FILE NAME
		// ========================================================

		if (CurrentFileLabel != null)
		{
			if (string.IsNullOrEmpty(bundleName))
			{
				CurrentFileLabel.text =
					"Preparing download...";
			}
			else
			{
				CurrentFileLabel.text =
					bundleName;
			}
		}

		// ========================================================
		// CURRENT FILE SIZE
		// ========================================================

		if (FileSizeLabel != null)
		{
			if (currentTotalBytes > 0)
			{
				FileSizeLabel.text =
					FormatBytes(currentBytes) +
					" / " +
					FormatBytes(currentTotalBytes);
			}
			else
			{
				FileSizeLabel.text =
					FormatBytes(currentBytes);
			}
		}

		// ========================================================
		// TOTAL SIZE
		// ========================================================

		if (DownloadSizeLabel != null)
		{
			if (totalBytes > 0)
			{
				DownloadSizeLabel.text =
					FormatBytes(downloadedBytes) +
					" / " +
					FormatBytes(totalBytes);
			}
			else
			{
				DownloadSizeLabel.text =
					FormatBytes(downloadedBytes) +
					" / Calculating...";
			}
		}

		// ========================================================
		// SPEED
		// ========================================================

		if (SpeedLabel != null)
		{
			if (speed > 0f)
			{
				SpeedLabel.text =
					"Speed: " +
					FormatBytesPerSecond(speed);
			}
			else
			{
				SpeedLabel.text =
					"Speed: --";
			}
		}

		// ========================================================
		// ETA
		// ========================================================

		if (RemainingTimeLabel != null)
		{
			if (speed > 0f &&
				totalBytes > downloadedBytes)
			{
				long remainingBytes =
					totalBytes -
					downloadedBytes;

				float seconds =
					remainingBytes /
					speed;

				RemainingTimeLabel.text =
					"Remaining: " +
					FormatTime(seconds);
			}
			else
			{
				RemainingTimeLabel.text =
					"Remaining: --";
			}
		}
	}

	// ============================================================
	// BATTLE SCENE CHECK
	// ============================================================

	private bool IsBattleScene()
	{
		try
		{
			return UnityEngine.SceneManagement.SceneManager
				.GetActiveScene()
				.name == "BattleScene";
		}
		catch
		{
			return false;
		}
	}

	// ============================================================
	// FORCE BATTLE INPUT RESET
	// ============================================================
	//
	// If another loading screen previously called LockInput(),
	// one UnlockInput() may not be enough if the NGUI lock counter
	// was incremented more than once.
	//
	// This is intentionally limited to BattleScene.
	// ============================================================

	private void ForceBattleSceneInputReset()
	{
		try
		{
			UICamera.UnlockInput();
			UICamera.UnlockInput();
			UICamera.UnlockInput();
			UICamera.UnlockInput();
			UICamera.UnlockInput();
		}
		catch
		{
		}

		try
		{
			if (Panel != null &&
				Panel.activeSelf)
			{
				Panel.SetActive(false);
			}
		}
		catch
		{
		}

		try
		{
			if (ProgressBarParent != null &&
				ProgressBarParent.activeSelf)
			{
				ProgressBarParent.SetActive(false);
			}
		}
		catch
		{
		}

		try
		{
			HideBusyIcon();
		}
		catch
		{
		}
	}

	// ============================================================
	// STOP DISPLAY
	// ============================================================

	private void StopDisplay()
	{
		if (mTimeLoading != -1f)
		{
			if (Panel != null)
			{
				Panel.SetActive(false);
			}

			if (ProgressBarParent != null)
			{
				ProgressBarParent.SetActive(false);
			}

			HideBusyIcon();
		}

		mTimeLoading = -1f;
		mShowFileProgress = false;
	}

	// ============================================================
	// BUSY ICON
	// ============================================================

	private void ShowBusyIcon()
	{
		try
		{
			if (Singleton<BusyIconPanelController>.Instance != null)
			{
				Singleton<BusyIconPanelController>
					.Instance
					.Show();
			}
		}
		catch
		{
		}
	}

	private void HideBusyIcon()
	{
		try
		{
			if (Singleton<BusyIconPanelController>.Instance != null)
			{
				Singleton<BusyIconPanelController>
					.Instance
					.Hide();
			}
		}
		catch
		{
		}
	}

	// ============================================================
	// FORMAT BYTES
	// ============================================================

	private string FormatBytes(long bytes)
	{
		if (bytes < 0)
		{
			bytes = 0;
		}

		if (bytes < 1024)
		{
			return bytes + " B";
		}

		float kb =
			bytes / 1024f;

		if (kb < 1024f)
		{
			return kb.ToString("0.0") +
				" KB";
		}

		float mb =
			kb / 1024f;

		if (mb < 1024f)
		{
			return mb.ToString("0.00") +
				" MB";
		}

		float gb =
			mb / 1024f;

		return gb.ToString("0.00") +
			" GB";
	}

	// ============================================================
	// FORMAT SPEED
	// ============================================================

	private string FormatBytesPerSecond(
		float bytesPerSecond)
	{
		if (bytesPerSecond < 1024f)
		{
			return bytesPerSecond.ToString("0") +
				" B/s";
		}

		float kb =
			bytesPerSecond / 1024f;

		if (kb < 1024f)
		{
			return kb.ToString("0") +
				" KB/s";
		}

		float mb =
			kb / 1024f;

		return mb.ToString("0.00") +
			" MB/s";
	}

	// ============================================================
	// FORMAT TIME
	// ============================================================

	private string FormatTime(float seconds)
	{
		if (seconds < 0f ||
			float.IsInfinity(seconds) ||
			float.IsNaN(seconds))
		{
			return "--";
		}

		int totalSeconds =
			Mathf.CeilToInt(seconds);

		int hours =
			totalSeconds / 3600;

		int minutes =
			(totalSeconds % 3600) / 60;

		int secs =
			totalSeconds % 60;

		if (hours > 0)
		{
			return hours + "h " +
				minutes + "m " +
				secs + "s";
		}

		if (minutes > 0)
		{
			return minutes + "m " +
				secs + "s";
		}

		return secs + "s";
	}
}