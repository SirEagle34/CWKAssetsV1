using System.Globalization;
using UnityEngine;

public class AssetBundleProgressPopup : Singleton<AssetBundleProgressPopup>
{
	public float DelayBeforeSpinnerAppearing;

	public float DelayBeforeBarAppearing;

	public float SingleBarLabelOffset;

	public GameObject Panel;

	public GameObject ProgressBarParent;

	public UISprite ProgressBar;

	public UISprite FileProgressBar;

	public UILabel ProgressLabel;

	public Transform LoadingLabel;

	private float mTimeLoading = -1f;

	private Vector3 mBaseLabelPos;

	private bool mShowFileProgress;

	private long mBaseBytes;

	private long mTotalBytes = -1L;

	private float mNextTotalCheck;

	private float mSampleTime;

	private long mSampleBytes;

	private float mSpeed = -1f;

	private UILabel mDetailLabel;

	private void Awake()
	{
		mBaseLabelPos = LoadingLabel.localPosition;
	}

	private void Update()
	{
		Singleton<SLOTResourceManager>.Instance.GetResourceLoadProgress(out var totalProgress, out var fileProgress);
		fileProgress = -1f;
		bool flag = LoadingScreenController.ShowingLoadingScreenNotFadingOut() || DetachedSingleton<SceneFlowManager>.Instance.GetCurrentScene() == SceneFlowManager.Scene.AssetBundleDownload;
		if (totalProgress != -1f)
		{
			float num = mTimeLoading;
			if (mTimeLoading == -1f)
			{
				if (flag)
				{
					mTimeLoading = DelayBeforeBarAppearing;
				}
				else
				{
					mTimeLoading = 0f;
				}
				UICamera.LockInput();
			}
			mTimeLoading += Time.deltaTime;
			if (num < DelayBeforeSpinnerAppearing && mTimeLoading >= DelayBeforeSpinnerAppearing)
			{
				Panel.SetActive(value: true);
				ProgressBarParent.SetActive(value: false);
			}
			if (num < DelayBeforeBarAppearing && mTimeLoading >= DelayBeforeBarAppearing)
			{
				ProgressBarParent.SetActive(value: true);
				if (Singleton<BusyIconPanelController>.Instance != null)
				{
					Singleton<BusyIconPanelController>.Instance.Show();
				}
			}
			if (!ProgressBarParent.activeSelf)
			{
				return;
			}
			if (!flag || !ShowBytes())
			{
				ProgressBar.fillAmount = totalProgress;
				ProgressLabel.text = (int)(totalProgress * 100f) + "%";
			}
			if (fileProgress == -1f)
			{
				if (mShowFileProgress)
				{
					FileProgressBar.fillAmount = 1f;
					return;
				}
				FileProgressBar.transform.parent.gameObject.SetActive(value: false);
				Vector3 localPosition = mBaseLabelPos;
				localPosition.y += SingleBarLabelOffset;
				LoadingLabel.localPosition = localPosition;
			}
			else if (fileProgress != 1f || mShowFileProgress)
			{
				FileProgressBar.transform.parent.gameObject.SetActive(value: true);
				LoadingLabel.localPosition = mBaseLabelPos;
				FileProgressBar.fillAmount = fileProgress;
				mShowFileProgress = true;
			}
			return;
		}
		if (mTimeLoading != -1f)
		{
			Panel.SetActive(value: false);
			if (Singleton<BusyIconPanelController>.Instance != null)
			{
				Singleton<BusyIconPanelController>.Instance.Hide();
			}
			UICamera.UnlockInput();
		}
		mTimeLoading = -1f;
		mShowFileProgress = false;
		mTotalBytes = -1L;
		mSpeed = -1f;
		if (mDetailLabel != null)
		{
			mDetailLabel.gameObject.SetActive(value: false);
		}
	}

	private bool ShowBytes()
	{
		float realtimeSinceStartup = Time.realtimeSinceStartup;
		if (mTotalBytes < 0)
		{
			if (realtimeSinceStartup < mNextTotalCheck)
			{
				return false;
			}
			mNextTotalCheck = realtimeSinceStartup + 1f;
			long num = Singleton<SLOTResourceManager>.Instance.PendingDownloadBytes();
			if (num <= 0)
			{
				return false;
			}
			mBaseBytes = KFFAssetBundleManager.DownloadedBytes;
			mTotalBytes = num;
			mSampleTime = realtimeSinceStartup;
			mSampleBytes = 0L;
		}
		long num2 = KFFAssetBundleManager.DownloadedBytes - mBaseBytes + Singleton<KFFAssetBundleManager>.Instance.ActiveDownloadedBytes();
		num2 = (long)Mathf.Clamp(num2, 0f, mTotalBytes);
		if (realtimeSinceStartup - mSampleTime >= 1f)
		{
			float num3 = Mathf.Max(0f, (float)(num2 - mSampleBytes) / (realtimeSinceStartup - mSampleTime));
			mSpeed = ((mSpeed < 0f) ? num3 : Mathf.Lerp(mSpeed, num3, 0.3f));
			mSampleTime = realtimeSinceStartup;
			mSampleBytes = num2;
		}
		float num4 = (float)num2 / (float)mTotalBytes;
		ProgressBar.fillAmount = num4;
		ProgressLabel.text = (int)(num4 * 100f) + "%   " + Megabytes(num2, mTotalBytes) + " / " + Megabytes(mTotalBytes, mTotalBytes) + " MB";
		UILabel uILabel = DetailLabel();
		if (mSpeed > 1024f)
		{
			int totalSeconds = Mathf.CeilToInt((float)(mTotalBytes - num2) / mSpeed);
			uILabel.text = (mSpeed / 1048576f).ToString("0.0", CultureInfo.InvariantCulture) + " MB/s     " + KFFLocalization.Get("!!TIME_VAL_LEFT").Replace("<val1>", PlayerInfoScript.FormatTimeString(totalSeconds, showSeconds: true, truncateZeroes: true));
			uILabel.gameObject.SetActive(value: true);
		}
		return true;
	}

	private UILabel DetailLabel()
	{
		if (mDetailLabel == null)
		{
			mDetailLabel = Object.Instantiate(ProgressLabel.gameObject, ProgressLabel.transform.parent, worldPositionStays: false).GetComponent<UILabel>();
			CurrencyUI.RemoveCopiedShadows(mDetailLabel.gameObject);
			mDetailLabel.name = "DownloadDetail";
			mDetailLabel.overflowMethod = UILabel.Overflow.ResizeFreely;
			mDetailLabel.effectStyle = UILabel.Effect.Outline;
			mDetailLabel.effectColor = Color.black;
			mDetailLabel.color = Color.white;
			mDetailLabel.transform.position = ProgressBar.transform.parent.position;
			Vector3 localPosition = mDetailLabel.transform.localPosition;
			localPosition.x = ProgressLabel.transform.localPosition.x;
			localPosition.y = ProgressLabel.transform.localPosition.y - (float)ProgressLabel.height - 12f;
			mDetailLabel.transform.localPosition = localPosition;
			mDetailLabel.gameObject.SetActive(value: false);
		}
		return mDetailLabel;
	}

	private static string Megabytes(long bytes, long total)
	{
		float num = (float)bytes / 1048576f;
		if (total >= 104857600)
		{
			return ((long)num).ToString("N0", CultureInfo.InvariantCulture);
		}
		return num.ToString("0.0", CultureInfo.InvariantCulture);
	}
}
