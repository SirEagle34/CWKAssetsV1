using System;
using UnityEngine;

public class MaintenanceOverlay : MonoBehaviour
{
	private enum Phase
	{
		None = 0,
		Countdown = 1,
		Closed = 2,
		Back = 3
	}

	private Phase mPhase;

	private GameObject mRoot;

	private UIPanel mPanel;

	private UILabel mBanner;

	private GameObject mWall;

	private UILabel mTitle;

	private UILabel mBody;

	private UILabel mEta;

	private UILabel mNote;

	private GameObject mPlay;

	private float mNextTick;

	private float mNextPoll;

	private bool mSaved;

	private float mH = 1536f;

	private float mW = 2048f;

	public static bool Showing { get; private set; }

	private void Update()
	{
		if (Time.realtimeSinceStartup < mNextTick)
		{
			return;
		}
		mNextTick = Time.realtimeSinceStartup + 0.25f;
		try
		{
			Tick();
		}
		catch (Exception ex)
		{
			Debug.LogWarning("[Maintenance] " + ex.Message);
		}
	}

	private void Tick()
	{
		double at;
		double eta;
		string message;
		bool flag = LiveChannel.Maintenance(out at, out eta, out message);
		double num = LiveChannel.ServerNow();
		Phase phase;
		if (Showing)
		{
			phase = ((flag && num >= at) ? Phase.Closed : Phase.Back);
		}
		else if (flag && num >= at)
		{
			phase = ((PlayerInfoScript.SessionLoadedAt >= 0f) ? Phase.Closed : Phase.None);
		}
		else if (flag || !(PlayerInfoScript.SessionLoadedAt >= 0f) || !LiveChannel.MaintenanceMissed())
		{
			phase = ((flag && at - num <= 3600.0) ? Phase.Countdown : Phase.None);
		}
		else
		{
			Debug.Log("[Maintenance] missed a maintenance - reloading for the update");
			Close();
			phase = Phase.Back;
		}
		if (!Ensure())
		{
			return;
		}
		switch (phase)
		{
		case Phase.Countdown:
		{
			ShowWall(on: false);
			mBanner.gameObject.SetActive(!LoadingScreenController.ShowingLoadingScreen());
			bool flag2 = DetachedSingleton<SceneFlowManager>.Instance.InFrontEnd();
			mBanner.transform.localPosition = (flag2 ? new Vector3((0f - mW) * 0.05f, (0f - mH) * 0.5f + 38f, 0f) : new Vector3(0f, mH * 0.5f - 22f, 0f));
			int num3 = Mathf.Max(0, (int)Math.Ceiling(at - num));
			mBanner.text = string.Format(RedeemUI.T("!!MAINT_BANNER", "Maintenance in {0}"), Clock(num3));
			mBanner.color = ((num3 <= 60) ? new Color(1f, 0.42f, 0.38f) : new Color(1f, 0.86f, 0.55f));
			break;
		}
		case Phase.Closed:
		{
			mBanner.gameObject.SetActive(value: false);
			if (!Showing)
			{
				Close();
			}
			ShowWall(on: true);
			mTitle.text = RedeemUI.T("!!MAINT_TITLE", "Maintenance");
			mBody.text = (string.IsNullOrEmpty(message) ? RedeemUI.T("!!MAINT_BODY", "We're updating Card Wars Kingdom with new content.\n\nYour progress is saved.") : message);
			double num2 = eta - num;
			mEta.text = ((eta <= 0.0) ? RedeemUI.T("!!MAINT_ETA_UNKNOWN", "We'll be back as soon as possible.") : ((num2 > 30.0) ? string.Format(RedeemUI.T("!!MAINT_ETA", "Expected back in about {0}"), Minutes(num2)) : RedeemUI.T("!!MAINT_ETA_SOON", "Almost done - any moment now.")));
			mNote.text = RedeemUI.T("!!MAINT_NOTE", "This screen updates by itself.");
			mPlay.SetActive(value: false);
			Poll();
			break;
		}
		case Phase.Back:
			ShowWall(on: true);
			mTitle.text = RedeemUI.T("!!MAINT_BACK_TITLE", "We're back!");
			mBody.text = RedeemUI.T("!!MAINT_BACK", "Maintenance is over. Tap Play to load the update.");
			mEta.text = string.Empty;
			mNote.text = string.Empty;
			mPlay.SetActive(value: true);
			break;
		default:
			mBanner.gameObject.SetActive(value: false);
			ShowWall(on: false);
			break;
		}
		mPhase = phase;
	}

	private void Close()
	{
		Showing = true;
		Debug.Log("[Maintenance] closing the game for maintenance");
		LiveChannel.LeaveBattleForMaintenance();
		if (!mSaved)
		{
			mSaved = true;
			try
			{
				Singleton<PlayerInfoScript>.Instance.Save();
			}
			catch (Exception ex)
			{
				Debug.LogWarning("[Maintenance] save: " + ex.Message);
			}
		}
	}

	private void Poll()
	{
		if (Time.realtimeSinceStartup >= mNextPoll && LiveChannel.SimAt <= 0.0)
		{
			mNextPoll = Time.realtimeSinceStartup + 15f;
			StartCoroutine(LiveChannel.PollOnce());
		}
	}

	private void OnPlay(GameObject go)
	{
		mPlay.SetActive(value: false);
		mNote.text = RedeemUI.T("!!MAINT_LOADING", "Loading...");
		Showing = false;
		mSaved = false;
		LiveChannel.NewSession();
		LiveChannel.Reload();
		Invoke("HideAfterReload", 2f);
	}

	private void HideAfterReload()
	{
		ShowWall(on: false);
	}

	private void ShowWall(bool on)
	{
		if (mWall.activeSelf != on)
		{
			mWall.SetActive(on);
		}
	}

	private static string Clock(int seconds)
	{
		if (seconds < 3600)
		{
			return $"{seconds / 60:00}:{seconds % 60:00}";
		}
		return $"{seconds / 3600}:{seconds / 60 % 60:00}:{seconds % 60:00}";
	}

	private static string Minutes(double seconds)
	{
		int num = Mathf.Max(1, (int)Math.Ceiling(seconds / 60.0));
		if (num < 120)
		{
			return string.Format(RedeemUI.T("!!MAINT_MINUTES", "{0} min"), num);
		}
		return string.Format(RedeemUI.T("!!MAINT_HOURS", "{0} h {1} min"), num / 60, num % 60);
	}

	private bool Ensure()
	{
		if (mRoot != null)
		{
			return true;
		}
		SimplePopupController instance = Singleton<SimplePopupController>.Instance;
		UIRoot uIRoot = ((instance != null) ? NGUITools.FindInParents<UIRoot>(instance.gameObject) : null);
		if (uIRoot == null || instance.Body == null || instance.OkButton == null)
		{
			return false;
		}
		mRoot = new GameObject("MaintenanceRoot");
		mRoot.layer = uIRoot.gameObject.layer;
		mRoot.transform.SetParent(uIRoot.transform.parent, worldPositionStays: false);
		mRoot.transform.position = uIRoot.transform.position;
		mRoot.transform.rotation = uIRoot.transform.rotation;
		UIRoot uIRoot2 = mRoot.AddComponent<UIRoot>();
		uIRoot2.scalingStyle = uIRoot.scalingStyle;
		uIRoot2.manualHeight = uIRoot.manualHeight;
		uIRoot2.minimumHeight = uIRoot.minimumHeight;
		uIRoot2.maximumHeight = uIRoot.maximumHeight;
		mRoot.transform.localScale = uIRoot.transform.localScale;
		if (mRoot.transform.parent == null)
		{
			UnityEngine.Object.DontDestroyOnLoad(mRoot);
		}
		mH = uIRoot2.activeHeight;
		mW = mH * (float)Screen.width / Mathf.Max(1f, Screen.height);
		mPanel = NGUITools.AddChild<UIPanel>(mRoot);
		mPanel.name = "Maintenance";
		mPanel.depth = 6000;
		mPanel.sortingOrder = 6000;
		UILabel body = instance.Body;
		mBanner = Label(mPanel.gameObject, body, 28, Vector3.zero, 0);
		mBanner.gameObject.SetActive(value: false);
		mWall = NGUITools.AddChild(mPanel.gameObject);
		mWall.name = "Wall";
		Texture2D texture2D = new Texture2D(2, 2);
		texture2D.SetPixels(new Color[4]
		{
			Color.white,
			Color.white,
			Color.white,
			Color.white
		});
		texture2D.Apply();
		UITexture uITexture = NGUITools.AddWidget<UITexture>(mWall);
		uITexture.mainTexture = texture2D;
		uITexture.shader = Shader.Find("Unlit/Transparent Colored");
		uITexture.color = new Color(0.035f, 0.035f, 0.06f, 0.96f);
		uITexture.depth = 1;
		uITexture.width = Mathf.CeilToInt(mW + 8f);
		uITexture.height = Mathf.CeilToInt(mH + 8f);
		uITexture.autoResizeBoxCollider = true;
		NGUITools.AddWidgetCollider(uITexture.gameObject);
		mTitle = Label(mWall, body, 64, new Vector3(0f, mH * 0.22f, 0f), 0);
		mTitle.color = new Color(1f, 0.86f, 0.55f);
		mBody = Label(mWall, body, 34, new Vector3(0f, mH * 0.06f, 0f), Mathf.RoundToInt(Mathf.Min(mW * 0.7f, 1300f)));
		mEta = Label(mWall, body, 36, new Vector3(0f, (0f - mH) * 0.1f, 0f), 0);
		mEta.color = new Color(0.62f, 0.86f, 1f);
		mNote = Label(mWall, body, 24, new Vector3(0f, (0f - mH) * 0.17f, 0f), 0);
		mNote.color = new Color(1f, 1f, 1f, 0.55f);
		mPlay = UnityEngine.Object.Instantiate(instance.OkButton.gameObject, mWall.transform, worldPositionStays: false);
		mPlay.name = "Play";
		mPlay.transform.localPosition = new Vector3(0f, (0f - mH) * 0.27f, 0f);
		mPlay.transform.localScale = instance.OkButton.localScale;
		UIButton component = mPlay.GetComponent<UIButton>();
		if (component != null)
		{
			component.onClick.Clear();
		}
		UIWidget[] componentsInChildren = mPlay.GetComponentsInChildren<UIWidget>(includeInactive: true);
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].depth += 20;
		}
		UILabel componentInChildren = mPlay.GetComponentInChildren<UILabel>(includeInactive: true);
		if (componentInChildren != null)
		{
			componentInChildren.text = RedeemUI.T("!!MAINT_PLAY", "Play");
		}
		UIEventListener.Get(mPlay).onClick = OnPlay;
		mPlay.SetActive(value: false);
		mWall.SetActive(value: false);
		return true;
	}

	private static UILabel Label(GameObject parent, UILabel src, int size, Vector3 pos, int width)
	{
		UILabel uILabel = NGUITools.AddWidget<UILabel>(parent);
		uILabel.bitmapFont = src.bitmapFont;
		uILabel.trueTypeFont = src.trueTypeFont;
		uILabel.fontSize = size;
		if (width > 0)
		{
			uILabel.overflowMethod = UILabel.Overflow.ResizeHeight;
			uILabel.width = width;
		}
		else
		{
			uILabel.overflowMethod = UILabel.Overflow.ResizeFreely;
		}
		uILabel.alignment = NGUIText.Alignment.Center;
		uILabel.pivot = UIWidget.Pivot.Center;
		uILabel.effectStyle = UILabel.Effect.Outline;
		uILabel.effectColor = new Color(0f, 0f, 0f, 0.85f);
		uILabel.effectDistance = new Vector2(2f, 2f);
		uILabel.color = Color.white;
		uILabel.depth = 10;
		uILabel.transform.localPosition = pos;
		return uILabel;
	}
}
