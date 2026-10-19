using System.Collections;
using System.Reflection;
using UnityEngine;

public class GoogleRewardBanner : MonoBehaviour
{
	private const string BannerName = "GoogleRewardButton";

	private GameObject mBanner;

	private GameObject mSpacer;

	private UIGrid mGrid;

	private static GoogleRewardBanner _instance;

	private bool mChecked;

	private bool mClaiming;

	public static GoogleRewardBanner Wire(TownHudController hud)
	{
		if (hud == null || hud.CraftRoomButton == null)
		{
			Debug.LogWarning("[GoogleReward] HUD veya CraftRoomButton bulunamadı.");
			return null;
		}

		Transform rail = hud.CraftRoomButton.transform.parent;

		if (rail == null)
		{
			Debug.LogWarning("[GoogleReward] Town rail bulunamadı.");
			return null;
		}

		// Önceden oluşturulmuş banner bileşenini yeniden kullan.
		GoogleRewardBanner existing = hud.GetComponent<GoogleRewardBanner>();

		Transform existingButton = rail.Find("GoogleRewardButton");
		Transform existingSpacer = rail.Find("GoogleRewardSpacer");

		if (existing != null && existingButton != null)
		{
			existing.mBanner = existingButton.gameObject;
			existing.mSpacer = existingSpacer != null
				? existingSpacer.gameObject
				: null;
			existing.mGrid = rail.GetComponent<UIGrid>();

			// Aynı rail üzerindeki fazladan kopyaları temizle.
			for (int i = rail.childCount - 1; i >= 0; i--)
			{
				Transform child = rail.GetChild(i);

				if (child == null)
					continue;

				if (child.name == "GoogleRewardButton" &&
					child != existingButton)
				{
					UnityEngine.Object.Destroy(child.gameObject);
				}
				else if (child.name == "GoogleRewardSpacer" &&
						existingSpacer != null &&
						child != existingSpacer)
				{
					UnityEngine.Object.Destroy(child.gameObject);
				}
			}

			Debug.Log("[GoogleReward] Mevcut banner yeniden kullanılıyor.");
			return existing;
		}

		if (existingButton != null)
		{
			// UI nesnesi var ama bileşen yok; yeniden klonlama.
			Debug.LogWarning(
				"[GoogleReward] Banner nesnesi mevcut, yeni kopya oluşturulmadı.");
			return null;
		}

		Transform storeButton = rail.Find("Store Button");

		if (storeButton == null)
		{
			Debug.LogWarning("[GoogleReward] Store Button bulunamadı.");
			return null;
		}

		// Spacer için kullanılabilecek mevcut bir Unavailable nesnesini bul.
		Transform spacerSource = null;

		for (int i = 0; i < rail.childCount; i++)
		{
			Transform child = rail.GetChild(i);

			if (child.name.StartsWith("Unavailable"))
			{
				spacerSource = child;
				break;
			}
		}

		if (spacerSource == null)
		{
			Debug.LogWarning("[GoogleReward] Spacer kaynağı bulunamadı.");
			return null;
		}

		GoogleRewardBanner banner = hud.gameObject.AddComponent<GoogleRewardBanner>();
		banner.mGrid = rail.GetComponent<UIGrid>();

		banner.mBanner = UnityEngine.Object.Instantiate(
			storeButton.gameObject, rail);
		banner.mBanner.name = "GoogleRewardButton";

		banner.mSpacer = UnityEngine.Object.Instantiate(
			spacerSource.gameObject, rail);
		banner.mSpacer.name = "GoogleRewardSpacer";

		banner.mBanner.transform.SetSiblingIndex(
			storeButton.GetSiblingIndex() + 1);

		banner.mSpacer.transform.SetSiblingIndex(
			banner.mBanner.transform.GetSiblingIndex() + 1);

		banner.Dress();
		banner.Show(false);
		banner.Refresh();

		Debug.Log("[GoogleReward] Banner ve spacer oluşturuldu.");
		return banner;
	}

	
	private void Dress()
	{
		if (mBanner == null)
		{
			Debug.LogError("[GoogleReward] Dress iptal: mBanner null.");
			return;
		}

		string componentNames = string.Empty;

		MonoBehaviour[] behaviours =
			mBanner.GetComponentsInChildren<MonoBehaviour>(true);

		foreach (MonoBehaviour behaviour in behaviours)
		{
			if (behaviour == null)
				continue;

			string typeName = behaviour.GetType().Name;

			// Klonlanan Store butonunun eski davranışlarını devre dışı bırak.
			if (typeName == "UILocalize" || typeName == "UIPlayTween")
			{
				behaviour.enabled = false;
				continue;
			}

			componentNames += typeName + " ";
		}

		Debug.Log("[GoogleReward] banner components: " + componentNames);

		UIEventTrigger[] triggers =
			mBanner.GetComponentsInChildren<UIEventTrigger>(true);

		foreach (UIEventTrigger trigger in triggers)
		{
			if (trigger == null)
				continue;

			if (trigger.onClick != null)
				trigger.onClick.Clear();
		}

		UIButton[] buttons =
			mBanner.GetComponentsInChildren<UIButton>(true);

		foreach (UIButton button in buttons)
		{
			if (button == null)
				continue;

			if (button.onClick != null)
				button.onClick.Clear();
		}

		Collider[] colliders =
			mBanner.GetComponentsInChildren<Collider>(true);

		foreach (Collider collider in colliders)
		{
			if (collider == null)
				continue;

			UIEventListener listener =
				UIEventListener.Get(collider.gameObject);

			if (listener != null)
				listener.onClick = OnClick;
		}

		UISprite icon = null;

		UISprite[] sprites =
			mBanner.GetComponentsInChildren<UISprite>(true);

		foreach (UISprite sprite in sprites)
		{
			if (sprite != null && sprite.name == "Icon")
				icon = sprite;
		}

		UILabel[] labels =
			mBanner.GetComponentsInChildren<UILabel>(true);

		foreach (UILabel label in labels)
		{
			if (label == null)
				continue;

			if (label.name == "Label Store")
			{
				label.text = "Link Google";
			}
			else if (label.name == "Label Plus")
			{
				label.text = CWKGoogleLink.LastKnownReward.ToString();
				label.fontSize = 20;

				if (icon != null && label.transform.parent != null)
				{
					Vector3 position =
						label.transform.parent.InverseTransformPoint(
							icon.transform.position);

					label.transform.localPosition = new Vector3(
						position.x,
						position.y - 18f,
						label.transform.localPosition.z);
				}
			}
		}

		if (icon != null)
		{
			icon.spriteName = "Icon_Currency_Hard";
			icon.width = 40;
			icon.height = 41;
		}
	}


	private static bool KeepOnClick(string type)
	{
		switch (type)
		{
		default:
			return type == "UIButtonRotation";
		case "UIButton":
		case "UIEventTrigger":
		case "UIPlaySound":
		case "UIEventListener":
		case "UIButtonScale":
		case "UIButtonColor":
		case "UIButtonOffset":
			return true;
		}
	}

	public void Refresh()
	{
		if (mChecked || mBanner == null)
		{
			return;
		}
		if (!LiveChannel.IsOn("GoogleReward") || !Singleton<TutorialController>.Instance.IsFTUETutorialComplete())
		{
			Show(on: false);
			return;
		}
		mChecked = true;
		CWKGoogleLink.Instance.RefreshStatus(delegate
		{
			Show(CWKGoogleLink.LastKnownReward > 0 || CWKGoogleLink.LastKnownRelink);
			if (CWKGoogleLink.LastKnownRelink)
			{
				StartCoroutine(AskToRelink());
			}
		});
	}

	private IEnumerator AskToRelink()
	{
		SimplePopupController instance = Singleton<SimplePopupController>.Instance;
		while (instance == null || instance.Busy || !Singleton<TownController>.Instance.IsIntroDone() || Singleton<TutorialController>.Instance.IsAnyTutorialActive())
		{
			yield return new WaitForSeconds(0.5f);
			instance = Singleton<SimplePopupController>.Instance;
		}
		if (CWKGoogleLink.LastKnownRelink)
		{
			OnClick(null);
		}
	}

	private void Show(bool on)
	{
		if (mBanner == null)
		{
			return;
		}
		if (on)
		{
			UILabel[] componentsInChildren = mBanner.GetComponentsInChildren<UILabel>(includeInactive: true);
			foreach (UILabel uILabel in componentsInChildren)
			{
				if (uILabel.name == "Label Plus")
				{
					uILabel.text = CWKGoogleLink.LastKnownReward.ToString();
					uILabel.gameObject.SetActive(CWKGoogleLink.LastKnownReward > 0);
				}
			}
		}
		mBanner.SetActive(on);
		mSpacer.SetActive(on);
		if (mGrid != null)
		{
			mGrid.Reposition();
		}
	}

	private void OnClick(GameObject go)
	{
		Debug.Log("[GoogleReward] tapped: linked=" + CWKGoogleLink.LastKnownLinked + " reward=" + CWKGoogleLink.LastKnownReward + " claiming=" + mClaiming + " busy=" + CWKGoogleLink.Instance.IsBusy);
		if (mClaiming)
		{
			return;
		}
		if (CWKGoogleLink.Instance.IsBusy)
		{
			CWKGoogleLink.Instance.Cancel();
		}
		if (CWKGoogleLink.LastKnownLinked)
		{
			Claim(null);
			return;
		}
		string body = ((CWKGoogleLink.LastKnownReward > 0) ? Amount(T("Link your Google account for {0} free gems!"), CWKGoogleLink.LastKnownReward) : T("Your Google account was removed. Link a new one to keep your account safe.")) + "\n\n" + T("If you lose this device, sign in with Google to get your account back.");
		Singleton<SimplePopupController>.Instance.ShowPrompt(string.Empty, body, delegate
		{
			CWKGoogleLink.Instance.BeginLink(AfterLink);
		}, null, T("Link"), T("Later"));
	}

	private void AfterLink(string email)
	{
		CWKGoogleLink.LastKnownRelink = false;
		if (CWKGoogleLink.LastKnownReward > 0)
		{
			Claim(email);
			return;
		}
		Show(on: false);
		StartCoroutine(ShowWhenFree(T("Linked to:") + "\n" + (string.IsNullOrEmpty(email) ? T("your Google account") : email)));
	}

	private void Claim(string email)
	{
		mClaiming = true;
		Debug.Log("[GoogleReward] claiming " + CWKGoogleLink.LastKnownReward);
		Singleton<PlayerInfoScript>.Instance.SaveData.ClaimCurrency("google link reward", string.Empty, 0, CWKGoogleLink.LastKnownReward, 0, 0, delegate(PlayerSaveData.ActionResult r)
		{
			mClaiming = false;
			Debug.Log("[GoogleReward] claim reply: success=" + r.success + " gems=" + r.gems + " reason=" + r.reason);
			string text;
			if (r.success && r.gems > 0)
			{
				CWKGoogleLink.LastKnownReward = 0;
				Show(on: false);
				Singleton<PlayerInfoScript>.Instance.Save();
				Singleton<SLOTAudioManager>.Instance.PlaySound("ui/SFX_MissionPopupShow");
				text = Amount(T("You got {0} gems!"), r.gems);
				if (!string.IsNullOrEmpty(email))
				{
					text = T("Linked to:") + "\n" + email + "\n\n" + text;
				}
			}
			else if (r.reason == "ALREADY_CLAIMED")
			{
				CWKGoogleLink.LastKnownReward = 0;
				Show(on: false);
				text = T("You already got this reward.");
			}
			else if (r.reason == "NOT_LINKED")
			{
				CWKGoogleLink.LastKnownLinked = false;
				text = T("Link your Google account first.");
			}
			else
			{
				text = T("Couldn't reach the server. Please try again.");
			}
			StartCoroutine(ShowWhenFree(text));
		});
	}

	private IEnumerator ShowWhenFree(string msg)
	{
		yield return null;
		SimplePopupController popup = Singleton<SimplePopupController>.Instance;
		while (popup != null && popup.Busy)
		{
			yield return null;
		}
		if (popup != null)
		{
			popup.ShowMessage(string.Empty, msg);
		}
	}

	private static string Amount(string text, int n)
	{
		return text.Replace("{0}", n.ToString()).Replace("}0{", n.ToString());
	}

	private static string T(string english)
	{
		return RevivalText.Translate(english);
	}

	private void Awake()
	{
		if (_instance != null && _instance != this)
		{
			Debug.LogError(
				"[GoogleRewardBanner] KOPYA ENGELLENDİ! " +
				"InstanceID=" + GetInstanceID() +
				" Object=" + gameObject.name
			);

			Destroy(gameObject);
			return;
		}

		_instance = this;

		Debug.Log(
			"[GoogleRewardBanner] Ana nesne: " +
			GetInstanceID()
		);
	}

	private void OnDestroy()
	{
		if (_instance == this)
			_instance = null;
	}
}
