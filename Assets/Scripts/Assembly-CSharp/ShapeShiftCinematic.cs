using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShapeShiftCinematic : MonoBehaviour
{
	// Override point for independent cinematic references such as CloneCinematic.
	protected virtual bool UsesCloneReferences => false;
	private static ShapeShiftCinematic sInstance;

	protected const float PickTimeout = 20f;

	protected const float CamMoveTime = 1.1f;

	public string LastLog = string.Empty;

	protected UIPanel mPanel;

	protected UIPanel mCardPanel;

	protected UIPanel mFlashPanel;

	protected float mW;

	protected float mH;

	protected UITexture mTopBar;

	protected UITexture mBottomBar;

	protected UITexture mShade;

	protected UITexture mFlash;

	protected UILabel mTitle;

	protected UILabel mSubtitle;

	protected readonly List<Transform> mHolders = new List<Transform>();

	protected readonly List<CardPrefabScript> mCards = new List<CardPrefabScript>();

	protected readonly List<GameObject> mAllowed = new List<GameObject>();

	protected readonly List<string> mIds = new List<string>();

	protected readonly List<string> mWhys = new List<string>();

	protected readonly List<KeyValuePair<UIPanel, float>> mHud = new List<KeyValuePair<UIPanel, float>>();

	protected readonly List<Renderer> mHudFx = new List<Renderer>();

	protected Action<string> mPickCallback;

	protected float mPickOpenedAt;

	protected CreatureState mShifter;

	protected bool mCharged;

	protected bool mShiftStarted;

	protected Vector3 mSavedCam;

	protected Vector3 mSavedLook;

	protected float mSavedFov;

	public static ShapeShiftCinematic Instance
	{
		get
		{
			if (sInstance == null)
			{
				sInstance = new GameObject("ShapeShiftCinematic").AddComponent<ShapeShiftCinematic>();
			}
			return sInstance;
		}
	}

	public bool InCloseUp { get; protected set; }

	public IList<string> OfferedIds => mIds;

	public static bool CanShiftNow(CardData card)
	{
		DWGame instance = Singleton<DWGame>.Instance;
		PlayerState playerState = ((instance == null || instance.CurrentBoardState == null) ? null : instance.CurrentBoardState.GetPlayerState(PlayerType.User));
		if (playerState == null)
		{
			return true;
		}
		foreach (LaneState lane in playerState.Lanes)
		{
			if (lane.Creature != null && lane.Creature.HP > 0 && LuaEffects.CardAllows(card, "canTarget", playerState, lane.Creature))
			{
				return true;
			}
		}
		return false;
	}

	public void BeginPick(CreatureState shifter, List<ShapeShiftAdvisor.Option> options, string title, Action<string> onPick)
	{
		if (shifter == null || options == null || options.Count == 0 || mPickCallback != null)
		{
			onPick("");
		}
		else
		{
			StartCoroutine(PickRoutine(shifter, options, title, onPick));
		}
	}

	protected IEnumerator PickRoutine(CreatureState shifter, List<ShapeShiftAdvisor.Option> options, string title, Action<string> onPick)
	{
		mShifter = shifter;
		mCharged = false;
		mShiftStarted = false;
		mIds.Clear();
		mWhys.Clear();
		foreach (ShapeShiftAdvisor.Option option in options)
		{
			mIds.Add(option.Id);
			mWhys.Add(option.Why);
		}
		if (Singleton<PlayerInfoScript>.Instance.StateData.MultiplayerMode)
		{
			Singleton<DWGame>.Instance.StopMultiplayerTimer();
		}
		foreach (string mId in mIds)
		{
			CreatureData data = CreatureDataManager.Instance.GetData(mId);
			if (data != null)
			{
				StartCoroutine(Singleton<SLOTResourceManager>.Instance.LoadCreatureResources(data, delegate
				{
				}));
			}
		}
		EnsureOverlay();
		mPanel.gameObject.SetActive(value: true);
		Singleton<SLOTAudioManager>.Instance.PlaySound("gacha/SFX_GachaFlyToCam");
		StartCoroutine(FadeShade(0.55f, 0.8f));
		yield return StartCoroutine(CloseUp(shifter, forPicker: true));
		mTitle.text = title;
		string text = KFFLocalization.Get("!!SPOOKY_0050_PICK");
		mSubtitle.text = ((string.IsNullOrEmpty(text) || text.Contains("SPOOKY_0050_PICK")) ? "Choose a new form" : text);
		StartCoroutine(FadeLabel(mTitle, 1f, 0.4f));
		StartCoroutine(FadeLabel(mSubtitle, 1f, 0.6f));
		SpawnCards();
		Singleton<SLOTAudioManager>.Instance.PlaySound("gacha/SFX_Gacha_Enter2");
		mPickCallback = onPick;
		mPickOpenedAt = Time.realtimeSinceStartup;
		LastLog = "picker open: " + mCards.Count + " cards";
		while (mPickCallback != null)
		{
			if (Time.realtimeSinceStartup - mPickOpenedAt > 20f)
			{
				Pick(UnityEngine.Random.Range(0, mCards.Count), force: true);
			}
			yield return null;
		}
	}

	public void Pick(int index, bool force = false)
	{
		if (mPickCallback != null && index >= 0 && index < mCards.Count && (force || !(Time.realtimeSinceStartup - mPickOpenedAt < 0.6f)))
		{
			Action<string> cb = mPickCallback;
			mPickCallback = null;
			StartCoroutine(PickedRoutine(index, cb));
		}
	}

	protected IEnumerator PickedRoutine(int index, Action<string> cb)
	{
		string id = mIds[index];
		foreach (GameObject item in mAllowed)
		{
			UICamera.AlwaysAllowedColliders.Remove(item);
		}
		mAllowed.Clear();
		Singleton<SLOTAudioManager>.Instance.PlaySound("ui/SFX_CreatureCardPickup");
		for (int i = 0; i < mHolders.Count; i++)
		{
			if (i != index)
			{
				StartCoroutine(MoveHolder(mHolders[i], mHolders[i].localPosition + new Vector3(0f, (0f - mH) * 0.7f, 0f), mHolders[i].localScale, 0.35f, 0.04f * (float)Mathf.Abs(i - index)));
				mCards[i].SetTargetAlpha(0f, 6f);
			}
		}
		StartCoroutine(FadeLabel(mSubtitle, 0f, 0.25f));
		Transform chosen = mHolders[index];
		CardPrefabScript card = mCards[index];
		if (card.SelectionVFX != null)
		{
			card.SelectionVFX.SetActive(value: true);
		}
		Vector3 pos = new Vector3(0f, (0f - mH) * 0.12f, 0f);
		yield return StartCoroutine(MoveHolder(chosen, pos, chosen.localScale * 1.18f, 0.3f, 0f, Quaternion.identity));
		yield return new WaitForSeconds(0.35f);
		Vector3 pos2 = UIPointOf(ChestOf(mShifter));
		Singleton<SLOTAudioManager>.Instance.PlaySound("gacha/SFX_GachaFlyToCam");
		card.SetTargetAlpha(0f, 5f);
		StartCoroutine(FadeLabel(mTitle, 0f, 0.4f));
		yield return StartCoroutine(MoveHolder(chosen, pos2, chosen.localScale * 0.12f, 0.45f, 0f, Quaternion.Euler(0f, 0f, 25f)));
		ClearCards();
		Charge(mShifter);
		if (Singleton<PlayerInfoScript>.Instance.StateData.MultiplayerMode)
		{
			Singleton<DWGame>.Instance.ResumeMultiplayerTimer();
		}
		LastLog = "picked " + id;
		StartCoroutine(Watchdog());
		cb(id);
	}

	protected IEnumerator Watchdog()
	{
		yield return new WaitForSeconds(8f);
		if (InCloseUp && !mShiftStarted)
		{
			LastLog += " | watchdog released the camera";
			yield return StartCoroutine(Release());
		}
	}

	public IEnumerator Shift(CreatureState c, GameObject oldBody, Func<bool> ready, Action swap)
	{
		mShiftStarted = true;
		mShifter = c;
		if (!InCloseUp)
		{
			EnsureOverlay();
			mPanel.gameObject.SetActive(value: true);
			Singleton<SLOTAudioManager>.Instance.PlaySound("gacha/SFX_GachaFlyToCam");
			StartCoroutine(FadeShade(0.35f, 0.6f));
			yield return StartCoroutine(CloseUp(c, forPicker: false));
		}
		if (!mCharged)
		{
			Charge(c, oldBody);
		}
		for (float waited = 0f; waited < 0.9f || (!ready() && waited < 6f); waited += Time.deltaTime)
		{
			yield return null;
		}
		Singleton<SLOTAudioManager>.Instance.PlaySound("battle/SFX_Rez_In");
		Puff(oldBody, 1.7f);
		Singleton<DWGameCamera>.Instance.CameraShake(0.35f, 0.45f);
		yield return StartCoroutine(FadeFlash(0.9f, 0.12f));
		swap();
		StartCoroutine(FadeFlash(0f, 0.7f));
		Singleton<SLOTAudioManager>.Instance.PlaySound("gacha/SFX_GachaSplash");
		DWBattleLaneObject laneObject = Singleton<DWBattleLane>.Instance.GetLaneObject(c);
		if (laneObject != null && laneObject.CreatureObject != null)
		{
			Frame(c, laneObject.CreatureObject, forPicker: false, out var camPos, out var lookPos, out var fov);
			yield return StartCoroutine(MoveCam(camPos + (camPos - lookPos).normalized * 1.5f, lookPos, fov, 1.6f));
		}
		yield return new WaitForSeconds(0.5f);
		yield return StartCoroutine(Release());
	}

	protected void Charge(CreatureState c, GameObject body = null)
	{
		mCharged = true;
		if (body == null)
		{
			DWBattleLaneObject laneObject = Singleton<DWBattleLane>.Instance.GetLaneObject(c);
			body = ((laneObject == null) ? null : laneObject.CreatureObject);
		}
		if (!(body == null))
		{
			Singleton<SLOTAudioManager>.Instance.PlaySound("ui/SFX_EnhanceSeq_Charge");
			Puff(body, 0.8f);
			iTween.ShakePosition(body, iTween.Hash("amount", new Vector3(0.18f, 0f, 0.18f) * Mathf.Max(1f, ModelBounds(body).size.y / 6f), "time", 1.4f));
		}
	}

	protected void Puff(GameObject body, float scale)
	{
		if (!(body == null))
		{
			Transform transform = Array.Find(body.GetComponentsInChildren<Transform>(includeInactive: true), (Transform t) => t.name == "SummonSmoke");
			if (!(transform == null))
			{
				GameObject obj = UnityEngine.Object.Instantiate(transform.gameObject, transform.position, transform.rotation, body.transform.parent);
				obj.transform.localScale = transform.lossyScale * scale;
				obj.SetActive(value: true);
				UnityEngine.Object.Destroy(obj, 4f);
			}
		}
	}

	protected IEnumerator CloseUp(CreatureState c, bool forPicker)
	{
		DWGameCamera instance = Singleton<DWGameCamera>.Instance;
		DWBattleLaneObject laneObject = Singleton<DWBattleLane>.Instance.GetLaneObject(c);
		if (!(laneObject == null) && !(laneObject.CreatureObject == null))
		{
			iTween.Stop(instance.MainCam.gameObject);
			iTween.Stop(instance.MainCamLookAt.gameObject);
			instance.MainCamLookAt.SetFollowCam(enable: true);
			mSavedCam = instance.MainCam.transform.position;
			mSavedLook = instance.MainCamLookAt.transform.position;
			mSavedFov = instance.MainCam.fieldOfView;
			InCloseUp = true;
			HideHud();
			StartCoroutine(Bars(show: true, 0.6f));
			Frame(c, laneObject.CreatureObject, forPicker, out var camPos, out var lookPos, out var fov);
			LastLog = string.Format("h {6:0.0} table {7:0.0} closeup cam {0} look {1} fov {2:0.0} (saved cam {3} look {4} fov {5:0.0})", camPos.ToString("F1"), lookPos.ToString("F1"), fov, mSavedCam.ToString("F1"), mSavedLook.ToString("F1"), mSavedFov, ModelBounds(laneObject.CreatureObject).size.y, c.Data.Form.Height);
			yield return StartCoroutine(MoveCam(camPos, lookPos, fov, 1.1f));
		}
	}

	protected IEnumerator Release()
	{
		ClearCards();
		StartCoroutine(Bars(show: false, 0.6f));
		StartCoroutine(FadeShade(0f, 0.6f));
		StartCoroutine(FadeLabel(mTitle, 0f, 0.3f));
		StartCoroutine(FadeLabel(mSubtitle, 0f, 0.3f));
		yield return StartCoroutine(MoveCam(mSavedCam, mSavedLook, mSavedFov, 1.1f));
		Singleton<DWBattleLane>.Instance.RepositionLaneObjects();
		Singleton<DWGameCamera>.Instance.MoveCameraToP1Setup();
		ShowHud();
		InCloseUp = false;
		mCharged = false;
		if (mPanel != null)
		{
			mPanel.gameObject.SetActive(value: false);
		}
	}

	protected void Frame(CreatureState c, GameObject body, bool forPicker, out Vector3 camPos, out Vector3 lookPos, out float fov)
	{
		Bounds bounds = ModelBounds(body);
		DWBattleLaneObject laneObject = Singleton<DWBattleLane>.Instance.GetLaneObject(c);
		float num = ((laneObject != null) ? laneObject.transform.position.y : body.transform.position.y);
		float num2 = c.Data.Form.Height * ((laneObject != null) ? laneObject.transform.lossyScale.y : 1f);
		float y = Mathf.Max(bounds.max.y, num + num2);
		bounds.SetMinMax(new Vector3(bounds.min.x, num, bounds.min.z), new Vector3(bounds.max.x, y, bounds.max.z));
		float num3 = Mathf.Max(bounds.size.y, 2f);
		float num4 = 0f;
		int num5 = 0;
		int num6 = ((c.Owner.Type == PlayerType.User) ? 1 : 0);
		foreach (DWBattleLaneObject item in Singleton<DWBattleLane>.Instance.BattleLaneObjects[num6])
		{
			if (item != null)
			{
				num4 += item.transform.position.x;
				num5++;
			}
		}
		float x = body.transform.position.x;
		num4 = ((num5 > 0) ? (num4 / (float)num5) : (0f - x));
		Vector3 vector = new Vector3(Mathf.Sign(num4 - x), 0f, 0f);
		float num7 = Mathf.Max(Mathf.Abs(num4 - x), 8f);
		float num8 = (forPicker ? (-0.12f) : (-0.62f));
		float num9 = (forPicker ? 0.8f : 0.78f);
		fov = mSavedFov;
		float num10 = Mathf.Tan(fov * 0.5f * (MathF.PI / 180f));
		float num11 = num3 / ((num9 - num8) * num10);
		float num12 = num7 * 0.6f;
		if (num11 > num12)
		{
			num11 = num12;
			num10 = num3 / ((num9 - num8) * num11);
			fov = Mathf.Min(75f, 2f * Mathf.Atan(num10) * 57.29578f);
		}
		lookPos = bounds.center - Vector3.up * ((num8 + num9) * 0.5f * num11 * num10);
		Vector3 vector2 = Quaternion.AngleAxis(22f * ((bounds.center.z > 0f) ? (-1f) : 1f) * vector.x, Vector3.up) * vector;
		vector2 = (vector2 * Mathf.Cos(MathF.PI / 20f) + Vector3.up * Mathf.Sin(MathF.PI / 20f)).normalized;
		camPos = lookPos + vector2 * num11;
	}

	protected IEnumerator MoveCam(Vector3 pos, Vector3 look, float fov, float time)
	{
		DWGameCamera cam = Singleton<DWGameCamera>.Instance;
		Transform camT = cam.MainCam.transform;
		Transform lookT = cam.MainCamLookAt.transform;
		Vector3 p0 = camT.position;
		Vector3 l0 = lookT.position;
		float f0 = cam.MainCam.fieldOfView;
		for (float t = 0f; t < time; t += Time.deltaTime)
		{
			float t2 = Mathf.SmoothStep(0f, 1f, Mathf.SmoothStep(0f, 1f, t / time));
			camT.position = Vector3.Lerp(p0, pos, t2);
			lookT.position = Vector3.Lerp(l0, look, t2);
			cam.MainCam.fieldOfView = Mathf.Lerp(f0, fov, t2);
			yield return null;
		}
		camT.position = pos;
		lookT.position = look;
		cam.MainCam.fieldOfView = fov;
	}

	protected static Bounds ModelBounds(GameObject go)
	{
		Bounds result = new Bounds(go.transform.position, Vector3.zero);
		bool flag = false;
		Renderer[] componentsInChildren = go.GetComponentsInChildren<Renderer>();
		foreach (Renderer renderer in componentsInChildren)
		{
			if (!(renderer is ParticleSystemRenderer) && renderer.enabled)
			{
				if (flag)
				{
					result.Encapsulate(renderer.bounds);
					continue;
				}
				result = renderer.bounds;
				flag = true;
			}
		}
		return result;
	}

	protected static Vector3 ChestOf(CreatureState c)
	{
		DWBattleLaneObject dWBattleLaneObject = ((c == null) ? null : Singleton<DWBattleLane>.Instance.GetLaneObject(c));
		if (dWBattleLaneObject == null || dWBattleLaneObject.CreatureObject == null)
		{
			return Vector3.zero;
		}
		Bounds bounds = ModelBounds(dWBattleLaneObject.CreatureObject);
		return bounds.center + Vector3.up * (bounds.size.y * 0.1f);
	}

	protected Vector3 UIPointOf(Vector3 world)
	{
		Vector3 position = Singleton<DWGameCamera>.Instance.MainCam.WorldToScreenPoint(world);
		Camera camera = NGUITools.FindCameraForLayer(mPanel.gameObject.layer);
		if (camera == null)
		{
			return Vector3.zero;
		}
		Vector3 result = mCardPanel.transform.InverseTransformPoint(camera.ScreenToWorldPoint(position));
		result.z = 0f;
		return result;
	}

	protected void HideHud()
	{
		mHud.Clear();
		UIPanel[] array = UnityEngine.Object.FindObjectsOfType<UIPanel>();
		foreach (UIPanel uIPanel in array)
		{
			if (uIPanel.parentPanel == null && uIPanel.alpha > 0f && !uIPanel.transform.IsChildOf(mPanel.transform))
			{
				mHud.Add(new KeyValuePair<UIPanel, float>(uIPanel, uIPanel.alpha));
			}
		}
		StartCoroutine(FadeHud(hide: true, 0.4f));
		mHudFx.Clear();
		ParticleSystemRenderer[] componentsInChildren = NGUITools.FindInParents<UIRoot>(Singleton<BattleHudController>.Instance.gameObject).GetComponentsInChildren<ParticleSystemRenderer>();
		foreach (ParticleSystemRenderer particleSystemRenderer in componentsInChildren)
		{
			if (particleSystemRenderer.enabled)
			{
				particleSystemRenderer.enabled = false;
				mHudFx.Add(particleSystemRenderer);
			}
		}
		Singleton<DWGameCamera>.Instance.Battle3DUICam.enabled = false;
		SetPortraits(on: false);
	}

	protected void ShowHud()
	{
		StartCoroutine(FadeHud(hide: false, 0.4f));
		Singleton<DWGameCamera>.Instance.Battle3DUICam.enabled = true;
		SetPortraits(on: true);
		foreach (Renderer item in mHudFx)
		{
			if (item != null)
			{
				item.enabled = true;
			}
		}
		mHudFx.Clear();
	}

	protected static void SetPortraits(bool on)
	{
		Portrait_PIP[] pIPPortraits = Singleton<DWGameCamera>.Instance.PIPPortraits;
		foreach (Portrait_PIP portrait_PIP in pIPPortraits)
		{
			if (portrait_PIP != null)
			{
				portrait_PIP.enabled = on;
				Camera component = portrait_PIP.GetComponent<Camera>();
				if (component != null)
				{
					component.enabled = on;
				}
			}
		}
	}

	protected IEnumerator FadeHud(bool hide, float time)
	{
		List<KeyValuePair<UIPanel, float>> panels = new List<KeyValuePair<UIPanel, float>>(mHud);
		for (float t = 0f; t <= time; t += Time.deltaTime)
		{
			float t2 = Mathf.Clamp01(t / time);
			foreach (KeyValuePair<UIPanel, float> item in panels)
			{
				if (item.Key != null)
				{
					item.Key.alpha = (hide ? Mathf.Lerp(item.Value, 0f, t2) : Mathf.Lerp(0f, item.Value, t2));
				}
			}
			yield return null;
		}
		foreach (KeyValuePair<UIPanel, float> item2 in panels)
		{
			if (item2.Key != null)
			{
				item2.Key.alpha = (hide ? 0f : item2.Value);
			}
		}
	}

	protected void EnsureOverlay()
	{
		if (!(mPanel != null))
		{
			UIRoot uIRoot = NGUITools.FindInParents<UIRoot>(Singleton<BattleHudController>.Instance.gameObject);
			GameObject gameObject = new GameObject("ShapeShiftRoot");
			gameObject.layer = uIRoot.gameObject.layer;
			gameObject.transform.SetParent(uIRoot.transform.parent, worldPositionStays: false);
			gameObject.transform.position = uIRoot.transform.position;
			gameObject.transform.rotation = uIRoot.transform.rotation;
			UIRoot uIRoot2 = gameObject.AddComponent<UIRoot>();
			uIRoot2.scalingStyle = uIRoot.scalingStyle;
			uIRoot2.manualHeight = uIRoot.manualHeight;
			uIRoot2.minimumHeight = uIRoot.minimumHeight;
			uIRoot2.maximumHeight = uIRoot.maximumHeight;
			gameObject.transform.localScale = uIRoot.transform.localScale;
			mH = uIRoot2.activeHeight;
			mW = mH * (float)Screen.width / Mathf.Max(1f, Screen.height);
			mPanel = NGUITools.AddChild<UIPanel>(gameObject);
			mPanel.name = "ShapeShiftCinematic";
			mPanel.depth = 1500;
			mCardPanel = NGUITools.AddChild<UIPanel>(mPanel.gameObject);
			mCardPanel.depth = 1501;
			mFlashPanel = NGUITools.AddChild<UIPanel>(mPanel.gameObject);
			mFlashPanel.depth = 1502;
			Texture2D texture2D = new Texture2D(2, 2);
			texture2D.SetPixels(new Color[4]
			{
				Color.white,
				Color.white,
				Color.white,
				Color.white
			});
			texture2D.Apply();
			Texture2D texture2D2 = new Texture2D(1, 64);
			for (int i = 0; i < 64; i++)
			{
				float num = 1f - (float)i / 63f;
				texture2D2.SetPixel(0, i, new Color(0f, 0f, 0f, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((num - 0.25f) / 0.6f)) * 0.95f));
			}
			texture2D2.wrapMode = TextureWrapMode.Clamp;
			texture2D2.Apply();
			mShade = Quad(mPanel, texture2D2, new Color(0.08f, 0f, 0.12f, 0f), 1, mW + 8f, mH + 8f, Vector3.zero);
			mShade.alpha = 0f;
			mShade.autoResizeBoxCollider = true;
			NGUITools.AddWidgetCollider(mShade.gameObject);
			mTopBar = Quad(mPanel, texture2D, Color.black, 2, mW + 8f, mH * 0.09f, new Vector3(0f, mH * 0.5f + mH * 0.045f, 0f));
			mBottomBar = Quad(mPanel, texture2D, Color.black, 2, mW + 8f, mH * 0.09f, new Vector3(0f, (0f - mH) * 0.5f - mH * 0.045f, 0f));
			mFlash = Quad(mFlashPanel, texture2D, new Color(0.86f, 0.62f, 1f, 1f), 1, mW + 8f, mH + 8f, Vector3.zero);
			mFlash.alpha = 0f;
			UILabel src = Singleton<PrefabReferences>.Instance.Card.GetComponent<CardPrefabScript>().Name;
			mTitle = Label(src, 52, new Vector3(0f, mH * 0.5f - mH * 0.045f, 0f), new Color(0.93f, 0.8f, 1f));
			mSubtitle = Label(src, 30, new Vector3(0f, mH * 0.05f, 0f), Color.white);
			mPanel.gameObject.SetActive(value: false);
		}
	}

	protected static UITexture Quad(UIPanel panel, Texture tex, Color color, int depth, float w, float h, Vector3 pos)
	{
		UITexture uITexture = NGUITools.AddWidget<UITexture>(panel.gameObject);
		uITexture.mainTexture = tex;
		uITexture.shader = Shader.Find("Unlit/Transparent Colored");
		uITexture.color = color;
		uITexture.depth = depth;
		uITexture.width = Mathf.CeilToInt(w);
		uITexture.height = Mathf.CeilToInt(h);
		uITexture.transform.localPosition = pos;
		return uITexture;
	}

	protected UILabel Label(UILabel src, int size, Vector3 pos, Color color)
	{
		UILabel uILabel = NGUITools.AddWidget<UILabel>(mCardPanel.gameObject);
		uILabel.bitmapFont = src.bitmapFont;
		uILabel.trueTypeFont = src.trueTypeFont;
		uILabel.fontSize = size;
		uILabel.overflowMethod = UILabel.Overflow.ResizeFreely;
		uILabel.effectStyle = UILabel.Effect.Outline;
		uILabel.effectColor = new Color(0.12f, 0f, 0.2f);
		uILabel.effectDistance = new Vector2(2f, 2f);
		uILabel.color = color;
		uILabel.depth = 400;
		uILabel.alpha = 0f;
		uILabel.transform.localPosition = pos;
		return uILabel;
	}

	protected void SpawnCards()
	{
		ClearCards();
		int count = mIds.Count;
		float num = mH * 0.42f;
		float num2 = mW * 0.012f;
		for (int i = 0; i < count; i++)
		{
			GameObject gameObject = NGUITools.AddChild(mCardPanel.gameObject);
			gameObject.name = "Form_" + mIds[i];
			GameObject gameObject2 = NGUITools.AddChild(gameObject);
			GameObject obj = NGUITools.AddChild(gameObject2, Singleton<PrefabReferences>.Instance.Card);
			CardPrefabScript component = obj.GetComponent<CardPrefabScript>();
			component.Mode = CardPrefabScript.CardMode.BattlePopup;
			CreatureItem creatureItem = PreviewItem(mIds[i]);
			component.Populate(creatureItem);
			component.Text.text = StatsText(creatureItem) + "\n[9CFF7A]" + mWhys[i] + "[-]";
			component.JokerMode = true;
			int index = i;
			component.OnJokerClick = delegate
			{
				Pick(index);
			};
			Collider[] componentsInChildren = obj.GetComponentsInChildren<Collider>(includeInactive: true);
			foreach (Collider collider in componentsInChildren)
			{
				UICamera.AlwaysAllowedColliders.Add(collider.gameObject);
				mAllowed.Add(collider.gameObject);
			}
			Bounds bounds = NGUIMath.CalculateRelativeWidgetBounds(gameObject2.transform, component.Frame.transform, considerInactive: true);
			gameObject2.transform.localPosition = -bounds.center;
			float num4 = num / Mathf.Max(1f, bounds.size.y);
			float num5 = bounds.size.x * num4;
			if ((float)count * num5 + (float)(count - 1) * num2 > mW * 0.94f)
			{
				num4 *= mW * 0.94f / ((float)count * num5 + (float)(count - 1) * num2);
				num5 = bounds.size.x * num4;
			}
			float num6 = (float)(count - 1) * 0.5f;
			float num7 = (float)i - num6;
			Vector3 vector = new Vector3(num7 * (num5 + num2), (0f - mH) * 0.5f + mH * 0.075f + bounds.size.y * num4 * 0.5f - num7 * num7 * mH * 0.012f, 0f);
			gameObject.transform.localScale = Vector3.one * num4;
			gameObject.transform.localRotation = Quaternion.Euler(0f, 0f, (0f - num7) * 2.5f);
			gameObject.transform.localPosition = vector + new Vector3(0f, (0f - mH) * 0.7f, 0f);
			component.SetAlpha(0f);
			component.SetTargetAlpha(1f, 8f);
			StartCoroutine(MoveHolder(gameObject.transform, vector, gameObject.transform.localScale, 0.5f, 0.08f * (float)i, gameObject.transform.localRotation, overshoot: true));
			mHolders.Add(gameObject.transform);
			mCards.Add(component);
		}
	}

	protected CreatureItem PreviewItem(string id)
	{
		CreatureItem creatureItem = new CreatureItem(id);
		if (mShifter != null && mShifter.Data != null)
		{
			creatureItem.Xp = mShifter.Data.Xp;
			creatureItem.StarRating = mShifter.Data.StarRating;
		}
		return creatureItem;
	}

	protected static string StatsText(CreatureItem item)
	{
		return string.Format("[FFE38A]{0}[-] {1}  [FFE38A]{2}[-] {3}", KFFLocalization.Get("!!HP"), item.HP, KFFLocalization.Get("!!STR"), item.STR);
	}

	protected void ClearCards()
	{
		foreach (GameObject item in mAllowed)
		{
			UICamera.AlwaysAllowedColliders.Remove(item);
		}
		mAllowed.Clear();
		foreach (Transform mHolder in mHolders)
		{
			if (mHolder != null)
			{
				UnityEngine.Object.Destroy(mHolder.gameObject);
			}
		}
		mHolders.Clear();
		mCards.Clear();
	}

	protected IEnumerator MoveHolder(Transform h, Vector3 pos, Vector3 scale, float time, float delay, Quaternion? rot = null, bool overshoot = false)
	{
		if (delay > 0f)
		{
			yield return new WaitForSeconds(delay);
		}
		if (h == null)
		{
			yield break;
		}
		Vector3 p0 = h.localPosition;
		Vector3 s0 = h.localScale;
		Quaternion r0 = h.localRotation;
		Quaternion r1 = rot ?? r0;
		for (float t = 0f; t < time; t += Time.deltaTime)
		{
			if (h == null)
			{
				yield break;
			}
			float num = t / time;
			float t2 = (overshoot ? EaseOutBack(num) : Mathf.SmoothStep(0f, 1f, num));
			h.localPosition = Vector3.LerpUnclamped(p0, pos, t2);
			h.localScale = Vector3.LerpUnclamped(s0, scale, t2);
			h.localRotation = Quaternion.SlerpUnclamped(r0, r1, t2);
			yield return null;
		}
		if (h != null)
		{
			h.localPosition = pos;
			h.localScale = scale;
			h.localRotation = r1;
		}
	}

	protected static float EaseOutBack(float x)
	{
		return 1f + 2.4f * Mathf.Pow(x - 1f, 3f) + 1.4f * Mathf.Pow(x - 1f, 2f);
	}

	protected IEnumerator Bars(bool show, float time)
	{
		float num = mH * 0.09f;
		float num2 = mH * 0.5f + num * 0.5f;
		float from = (show ? num2 : (num2 - num));
		float to = (show ? (num2 - num) : num2);
		for (float t = 0f; t <= time; t += Time.deltaTime)
		{
			float num3 = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / time));
			mTopBar.transform.localPosition = new Vector3(0f, num3, 0f);
			mBottomBar.transform.localPosition = new Vector3(0f, 0f - num3, 0f);
			yield return null;
		}
		mTopBar.transform.localPosition = new Vector3(0f, to, 0f);
		mBottomBar.transform.localPosition = new Vector3(0f, 0f - to, 0f);
	}

	protected IEnumerator FadeShade(float alpha, float time)
	{
		float a0 = mShade.alpha;
		for (float t = 0f; t <= time; t += Time.deltaTime)
		{
			mShade.alpha = Mathf.Lerp(a0, alpha, t / time);
			yield return null;
		}
		mShade.alpha = alpha;
	}

	protected IEnumerator FadeFlash(float alpha, float time)
	{
		float a0 = mFlash.alpha;
		for (float t = 0f; t <= time; t += Time.deltaTime)
		{
			mFlash.alpha = Mathf.Lerp(a0, alpha, t / time);
			yield return null;
		}
		mFlash.alpha = alpha;
	}

	protected static IEnumerator FadeLabel(UILabel l, float alpha, float time)
	{
		if (!(l == null))
		{
			float a0 = l.alpha;
			for (float t = 0f; t <= time; t += Time.deltaTime)
			{
				l.alpha = Mathf.Lerp(a0, alpha, t / time);
				yield return null;
			}
			l.alpha = alpha;
		}
	}
}
