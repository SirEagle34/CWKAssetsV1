using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GraveyardCinematic : ShapeShiftCinematic
{
	private class Grave
	{
		public Transform Stone;

		public Transform Mound;

		public Transform Ghost;

		public Material GhostMat;

		public LineRenderer Thread;

		public Vector3 At;

		public float Bob;

		public float GhostY;

		public float GhostH;

		public Vector3 Home;

		public ParticleSystem Stream;
	}

	private static GraveyardCinematic gInstance;

	private static readonly Color Ghost = new Color(0.45f, 1f, 0.68f);

	private static readonly Color Blood = new Color(1f, 0.08f, 0.14f);

	private static readonly Color NightTint = new Color(0.3f, 0.34f, 0.68f);

	private static readonly Color Moon = new Color(0.62f, 0.76f, 1f);

	private static readonly Color Earth = new Color(0.42f, 0.3f, 0.2f);

	private const float StoneHeight = 2.7f;

	private readonly List<Grave> mGraves = new List<Grave>();

	private readonly List<CreatureItem> mItems = new List<CreatureItem>();

	private GameObject mWorld;

	private GameObject mNightQuad;

	private Material mNightMat;

	private ParticleSystem mFog;

	private ParticleSystem mEmbers;

	private UILabel mSpeech;

	private UILabel mSpeaker;

	private UITexture mSpeechFace;

	private UILabel mPrice;

	private UILabel mBanner;

	private PlayerState mOwner;

	private bool mHeroWasShown;

	private CreatureState mPayer;

	private int mChosen = -1;

	private int mBestSlot;

	private bool mOnBoard = true;

	private bool mRising;

	private bool mRaiseStarted;

	private float mGround;

	private Vector3 mGraveCentre;

	private Vector3 mFacing;

	private static bool sKitLoaded;

	private static GameObject sKit;

	private static GameObject sHands;

	private static Shader sToon;

	private static Shader sGhost;

	private static Shader sNight;

	private static Shader sFx;

	private static Shader sAdd;

	private static Shader sMist;

	private static Texture2D tPalette;

	private static Texture2D tDust;

	private static Texture2D tFog;

	private static Texture2D tGlow;

	private static Texture2D tHole;

	private static Texture2D tCrack;

	private static Texture2D tBolt;

	private static Texture2D tNoise;

	private static Texture2D tBeam;

	private float mNightAlpha;

	private float mLift;

	private readonly List<Material> mMist = new List<Material>();

	private readonly List<Material> mBeams = new List<Material>();

	private readonly List<Transform> mBats = new List<Transform>();

	private bool mBatsLeave;

	private float mBatsUp;

	private int mDrift;

	private static Material sToonMat;

	private static readonly string[] Stones = new string[6] { "GY_gravestone_round", "GY_gravestone_cross", "GY_gravestone_decorative", "GY_gravestone_roof", "GY_gravestone_bevel", "GY_gravestone_cross_large" };

	private readonly List<Grave> mDressing = new List<Grave>();

	private int mSaid;

	public new static GraveyardCinematic Instance
	{
		get
		{
			if (gInstance == null)
			{
				gInstance = new GameObject("GraveyardCinematic").AddComponent<GraveyardCinematic>();
			}
			return gInstance;
		}
	}

	public static bool AnyFallen()
	{
		DWGame instance = Singleton<DWGame>.Instance;
		PlayerState playerState = ((instance == null || instance.CurrentBoardState == null) ? null : instance.CurrentBoardState.GetPlayerState(PlayerType.User));
		if (playerState != null)
		{
			return playerState.RaisableDead().Count > 0;
		}
		return true;
	}

	public void BeginPick(PlayerState owner, CreatureState payer, string title, Action<string> onPick)
	{
		List<CreatureState> list = ((owner == null) ? new List<CreatureState>() : owner.RaisableDead());
		if (payer == null || list.Count == 0 || mPickCallback != null || mRising)
		{
			onPick((list.Count > 0) ? PlayerState.GraveyardPickKey(list, 0) : string.Empty);
		}
		else
		{
			StartCoroutine(PickRoutine(owner, list, payer, title, onPick));
		}
	}

	private IEnumerator PickRoutine(PlayerState owner, List<CreatureState> dead, CreatureState payer, string title, Action<string> onPick)
	{
		mRising = true;
		mRaiseStarted = false;
		mOnBoard = owner.GetCreatureCount() < MiscParams.CreaturesOnBoard;
		Offer(owner, dead, -1);
		if (Singleton<PlayerInfoScript>.Instance.StateData.MultiplayerMode)
		{
			Singleton<DWGame>.Instance.StopMultiplayerTimer();
		}
		yield return StartCoroutine(Nightfall(owner, payer, title, L("!!GRAVEYARD_LINE_OPEN", "The dead don't rest while I'm playing.")));
		yield return StartCoroutine(RaiseGraves(interactive: true));
		mSubtitle.text = L("!!GRAVEYARD_PICK", "Raise one of your fallen creatures");
		mPrice.text = string.Format(L("!!GRAVEYARD_PRICE", "Blood price: {0} is Cursed"), payer.Data.Form.Name);
		StartCoroutine(ShapeShiftCinematic.FadeLabel(mSubtitle, 1f, 0.5f));
		StartCoroutine(ShapeShiftCinematic.FadeLabel(mPrice, 1f, 0.7f));
		mPickCallback = onPick;
		mPickOpenedAt = Time.realtimeSinceStartup;
		LastLog = "graveyard open: " + mGraves.Count + " graves";
		while (mPickCallback != null)
		{
			if (Time.realtimeSinceStartup - mPickOpenedAt > 20f)
			{
				Pick(mBestSlot, force: true);
			}
			else if (Input.GetMouseButtonDown(0))
			{
				int num = GhostAt(Input.mousePosition);
				if (num >= 0)
				{
					Pick(num);
				}
			}
			Idle();
			yield return null;
		}
	}

	public new void Pick(int index, bool force = false)
	{
		if (mPickCallback != null && index >= 0 && index < mGraves.Count && (force || !(Time.realtimeSinceStartup - mPickOpenedAt < 0.6f)))
		{
			Action<string> cb = mPickCallback;
			mPickCallback = null;
			StartCoroutine(GravePicked(index, cb));
		}
	}

	private IEnumerator GravePicked(int index, Action<string> cb)
	{
		string key = mIds[index];
		foreach (GameObject item in mAllowed)
		{
			UICamera.AlwaysAllowedColliders.Remove(item);
		}
		mAllowed.Clear();
		yield return StartCoroutine(TearFree(index));
		if (Singleton<PlayerInfoScript>.Instance.StateData.MultiplayerMode)
		{
			Singleton<DWGame>.Instance.ResumeMultiplayerTimer();
		}
		LastLog = "raised " + key;
		StartCoroutine(NightWatch());
		cb(key);
	}

	private IEnumerator NightWatch()
	{
		yield return new WaitForSeconds(8f);
		if (mRising && !mRaiseStarted)
		{
			LastLog += " | watchdog ended the night";
			yield return StartCoroutine(Dawn());
		}
	}

	private int GhostAt(Vector3 screen)
	{
		Camera mainCam = Singleton<DWGameCamera>.Instance.MainCam;
		int result = -1;
		float num = float.MaxValue;
		for (int i = 0; i < mGraves.Count; i++)
		{
			Grave grave = mGraves[i];
			Vector3 position = grave.Home + Vector3.up * Mathf.Max(grave.GhostH, 2.7f);
			Vector3 vector = mainCam.WorldToScreenPoint(grave.At);
			Vector3 vector2 = mainCam.WorldToScreenPoint(position);
			if (!(vector.z < 0f) && !(vector2.z < 0f))
			{
				float num2 = Mathf.Max(40f, Vector2.Distance(vector, vector2) * 0.45f);
				float num3 = Vector2.Distance((vector + vector2) * 0.5f, screen);
				if (num3 < num2 * 1.25f && num3 < num)
				{
					result = i;
					num = num3;
				}
			}
		}
		return result;
	}

	public IEnumerator Raise(PlayerState owner, CreatureState raised, CreatureState payer, bool onBoard)
	{
		mRaiseStarted = true;
		if (!mRising)
		{
			mRising = true;
			mOnBoard = onBoard;
			List<CreatureState> list = owner.RaisableDead();
			list.Insert(Mathf.Min(list.Count, list.Count / 2), raised);
			Offer(owner, list, list.IndexOf(raised));
			yield return StartCoroutine(Nightfall(owner, payer ?? raised, string.Format(L("!!GRAVEYARD_WATCH", "{0} is raising the dead"), HeroName(owner)), L("!!GRAVEYARD_LINE_WATCH", "Death's just a nap for my crew.")));
			mPrice.text = ((payer == null) ? string.Empty : string.Format(L("!!GRAVEYARD_PRICE", "Blood price: {0} is Cursed"), payer.Data.Form.Name));
			yield return StartCoroutine(RaiseGraves(interactive: false));
			StartCoroutine(ShapeShiftCinematic.FadeLabel(mPrice, 1f, 0.6f));
			for (float t = 0f; t < 1.4f; t += Time.deltaTime)
			{
				Idle();
				yield return null;
			}
			yield return StartCoroutine(TearFree(Mathf.Max(0, mChosen)));
		}
	}

	public IEnumerator RiseFromGround(CreatureState c, GameObject body)
	{
		DWBattleLaneObject laneObj = Singleton<DWBattleLane>.Instance.GetLaneObject(c);
		if (laneObj == null || body == null)
		{
			yield break;
		}
		LoadKit();
		if (!base.InCloseUp)
		{
			BeginScene();
		}
		EnsureWorld();
		SLOTAudioManager audio = Singleton<SLOTAudioManager>.Instance;
		Vector3 home = body.transform.localPosition;
		Vector3 at = laneObj.transform.position;
		float y = at.y;
		at.y = y;
		float h = Mathf.Max(3f, c.Data.Form.Height * laneObj.transform.lossyScale.y);
		float depth = h * 1.15f / Mathf.Max(0.001f, laneObj.transform.lossyScale.y);
		float radius = Mathf.Clamp(c.Data.Form.Width * laneObj.transform.lossyScale.x * 0.42f, 1.8f, 3.4f);
		mDrift++;
		int num = ((c.Owner.Type != PlayerType.User) ? 1 : 0);
		float num2 = 0f;
		int num3 = 0;
		foreach (DWBattleLaneObject item in Singleton<DWBattleLane>.Instance.BattleLaneObjects[num])
		{
			if (item != null)
			{
				num2 += item.transform.position.x;
				num3++;
			}
		}
		num2 = ((num3 > 0 && Mathf.Abs(num2 / (float)num3) > 0.5f) ? (num2 / (float)num3) : ((num == 0) ? (-9f) : 9f));
		Vector3 vector = (mFacing = new Vector3(0f - Mathf.Sign(num2), 0f, 0f));
		string[] obj = new string[10]
		{
			"[Graveyard] rise ",
			c.Data.Form.ID,
			" at ",
			null,
			null,
			null,
			null,
			null,
			null,
			null
		};
		Vector3 vector2 = at;
		obj[3] = vector2.ToString();
		obj[4] = " row x ";
		obj[5] = num2.ToString();
		obj[6] = " body ";
		obj[7] = body.transform.position.ToString();
		obj[8] = " lane rot ";
		obj[9] = laneObj.transform.eulerAngles.y.ToString();
		Debug.Log(string.Concat(obj));
		LastLog = LastLog + " | rise at " + at.ToString("F1") + " row " + num2.ToString("F1");
		Vector3 vector3 = at + Vector3.up * h * 0.42f;
		float angle = 35f * ((at.z >= 0f) ? (-1f) : 1f) * vector.x;
		Vector3 dir = Quaternion.AngleAxis(angle, Vector3.up) * vector;
		float num4 = h * 0.8f / Mathf.Tan(mSavedFov * 0.5f * (MathF.PI / 180f)) + radius * 0.6f;
		dir = (dir * Mathf.Cos(MathF.PI * 4f / 45f) + Vector3.up * Mathf.Sin(MathF.PI * 4f / 45f)).normalized;
		StartCoroutine(MoveCam(vector3 + dir * num4, vector3, mSavedFov, 0.9f));
		StartCoroutine(Drift(0.95f, 6f * vector.x, 0.14f, 4.5f));
		StartCoroutine(BatsClimb(9f, 1.2f));
		foreach (Material mBeam in mBeams)
		{
			StartCoroutine(FadeMat(mBeam, 0f, 0.5f));
		}
		foreach (Grave mGrafe in mGraves)
		{
			StartCoroutine(SinkProp(mGrafe.Stone, 0.6f, 0f));
			StartCoroutine(SinkProp(mGrafe.Mound, 0.6f, 0.05f));
		}
		foreach (Grave item2 in mDressing)
		{
			StartCoroutine(SinkProp(item2.Stone, 0.6f, 0f));
		}
		yield return new WaitForSeconds(0.55f);
		GameObject site = new GameObject("RiseSite");
		site.transform.SetParent(mWorld.transform, worldPositionStays: false);
		site.transform.position = at;
		Material crackMat = Decal(site.transform, tCrack, radius * 3.2f, 0.03f, new Color(1f, 1f, 1f, 0f), 3000);
		Beam(at, 30f, radius * 2.6f, Moon, 0.45f, 0.5f);
		Transform crack = ((crackMat == null) ? null : site.transform.GetChild(site.transform.childCount - 1));
		audio.PlaySound("creature/C_Smash_Pieclops");
		Singleton<DWGameCamera>.Instance.CameraShake(0.12f, 0.6f);
		for (float t = 0f; t < 0.45f; t += Time.deltaTime)
		{
			float num5 = t / 0.45f;
			if (crackMat != null)
			{
				crackMat.color = new Color(1f, 1f, 1f, Mathf.Clamp01(num5 * 2f));
				crack.localScale = Vector3.one * radius * 3.2f * Mathf.Lerp(0.3f, 1f, Mathf.Sqrt(num5));
			}
			yield return null;
		}
		audio.PlaySound("battle/SFX_RezIn");
		audio.PlaySound("battle/SFX_DeathSting");
		Singleton<DWGameCamera>.Instance.CameraShake(0.4f, 0.45f);
		Material holeMat = Decal(site.transform, tHole, radius * 2.15f, 0.05f, Color.white, 3000);
		Transform child = site.transform.GetChild(site.transform.childCount - 1);
		StartCoroutine(Scale(child, Vector3.one * radius * 2.15f, 0.22f, overshoot: true, Vector3.one * 0.2f));
		Transform rim = Prop("GY_Rim", site.transform, radius * 2.35f, active: true, byWidth: true);
		if (rim != null)
		{
			Vector3 localPosition = rim.localPosition;
			rim.localPosition = localPosition - Vector3.up * radius * 0.4f;
			StartCoroutine(Slide3(rim, localPosition, 0.25f, overshoot: true));
		}
		List<Transform> sods = new List<Transform>();
		for (int i = 0; i < 10; i++)
		{
			float f = (float)i / 10f * MathF.PI * 2f + UnityEngine.Random.Range(-0.12f, 0.12f);
			Vector3 vector4 = new Vector3(Mathf.Cos(f), 0f, Mathf.Sin(f));
			Transform transform = Prop("GY_Sod_" + i, site.transform, radius * UnityEngine.Random.Range(0.55f, 0.7f), active: true, byWidth: true);
			if (!(transform == null))
			{
				transform.position = at + vector4 * radius * 0.98f + Vector3.up * 0.02f;
				transform.localScale = new Vector3(transform.localScale.x, transform.localScale.y * 0.55f, transform.localScale.z);
				StartCoroutine(FlipSod(transform, vector4, UnityEngine.Random.Range(28f, 58f), 0.3f, (float)i * 0.015f));
				sods.Add(transform);
			}
		}
		Dust(at + Vector3.up * 0.3f, radius, 26, 1.6f, 2.6f, Earth);
		Debris(at, radius * 0.7f, 14, 7f, 11f);
		Glow(at + Vector3.up * 0.6f, Ghost, radius * 3f, 0.5f, 0.9f);
		Material under = Pool(site.transform, at, Ghost, radius * 3.4f, 0.7f);
		yield return new WaitForSeconds(0.3f);
		Transform[] hands = new Transform[2];
		Vector3 normalized = new Vector3(dir.x, 0f, dir.z).normalized;
		for (int j = 0; j < 2; j++)
		{
			hands[j] = Prop((j == 0) ? "GY_HandL" : "GY_HandR", site.transform, h * 0.8f, active: false);
			if (!(hands[j] == null))
			{
				Vector3 vector5 = Vector3.Cross(Vector3.up, normalized).normalized * ((j == 0) ? (-1f) : 1f);
				Vector3 at2 = at + normalized * radius * 0.45f + vector5 * radius * 0.5f;
				hands[j].rotation = Quaternion.LookRotation(normalized, Vector3.up) * Quaternion.Euler(0f, (j == 0) ? 15f : (-15f), (j == 0) ? 10f : (-10f)) * hands[j].localRotation;
				StartCoroutine(Claw(hands[j], at2, h, normalized, (float)j * 0.12f));
			}
		}
		audio.PlaySound("creature/GhostStalker");
		yield return new WaitForSeconds(0.75f);
		body.transform.localPosition = home - Vector3.up * depth;
		body.SetActive(value: true);
		Animator componentInChildren = body.GetComponentInChildren<Animator>();
		if (componentInChildren != null)
		{
			componentInChildren.speed = 1f;
		}
		ParticleSystem column = Embers(at, radius * 0.6f, 40f, Ghost, 0.4f, 3.5f, 6f);
		float[] steps = new float[3] { 0.32f, 0.62f, 1f };
		float from = 0f;
		for (int s = 0; s < steps.Length; s++)
		{
			bool last = s == steps.Length - 1;
			audio.PlaySound(last ? "battle/SFX_Rez_In" : "battle/SFX_Land_UselessSwamp");
			Singleton<DWGameCamera>.Instance.CameraShake(last ? 0.3f : 0.18f, 0.25f);
			Dust(at + Vector3.up * 0.2f, radius * 0.9f, last ? 18 : 10, 1.2f, 2.2f, Earth);
			Debris(at, radius * 0.5f, last ? 8 : 4, 5f, 8f);
			float t = (last ? 0.55f : 0.24f);
			for (float t2 = 0f; t2 < t; t2 += Time.deltaTime)
			{
				float t3 = (last ? ShapeShiftCinematic.EaseOutBack(t2 / t) : Mathf.SmoothStep(0f, 1f, t2 / t));
				float num6 = Mathf.LerpUnclamped(from, steps[s], t3);
				body.transform.localPosition = home - Vector3.up * depth * (1f - num6);
				body.transform.localRotation = Quaternion.Euler(0f, body.transform.localEulerAngles.y, Mathf.Sin(t2 * 34f) * 5f * (1f - num6));
				yield return null;
			}
			from = steps[s];
			if (!last)
			{
				yield return new WaitForSeconds(0.16f);
			}
		}
		body.transform.localPosition = home;
		body.transform.localRotation = Quaternion.Euler(0f, body.transform.localEulerAngles.y, 0f);
		if (column != null)
		{
			column.Stop();
		}
		laneObj.RevertMaterials();
		StartCoroutine(Singleton<DWGame>.Instance.TriggerSpawnCreatureEffects(c));
		Glow(at + Vector3.up * h * 0.4f, Ghost, h * 2.2f, 0.85f, 0.7f);
		if (c.Data.Form.IntroSound != null)
		{
			audio.PlaySound("creature/" + c.Data.Form.IntroSound);
		}
		for (int k = 0; k < 2; k++)
		{
			StartCoroutine(SinkProp(hands[k], 0.5f, 0.1f));
		}
		yield return new WaitForSeconds(0.7f);
		foreach (Transform item3 in sods)
		{
			StartCoroutine(SinkProp(item3, 0.7f, UnityEngine.Random.Range(0f, 0.2f)));
		}
		StartCoroutine(SinkProp(rim, 0.8f, 0.15f));
		StartCoroutine(FadeMat(holeMat, 0f, 0.8f));
		StartCoroutine(FadeMat(under, 0f, 0.6f));
		StartCoroutine(FadeMat(crackMat, 0f, 1.1f));
		yield return new WaitForSeconds(0.9f);
	}

	private void Offer(PlayerState owner, List<CreatureState> dead, int chosen)
	{
		mIds.Clear();
		mWhys.Clear();
		mItems.Clear();
		List<string> list = new List<string>();
		List<CreatureItem> list2 = new List<CreatureItem>();
		for (int i = 0; i < dead.Count; i++)
		{
			list.Add(PlayerState.GraveyardPickKey(dead, i));
			list2.Add(dead[i].Data);
		}
		List<ShapeShiftAdvisor.Option> list3 = ShapeShiftAdvisor.Rank(owner, null, list, list2, 1f, list.Count);
		List<ShapeShiftAdvisor.Option> list4 = new List<ShapeShiftAdvisor.Option>();
		for (int j = 0; j < list3.Count; j++)
		{
			if (j % 2 == 1)
			{
				list4.Insert(0, list3[j]);
			}
			else
			{
				list4.Add(list3[j]);
			}
		}
		mChosen = -1;
		mBestSlot = list4.IndexOf(list3[0]);
		foreach (ShapeShiftAdvisor.Option item in list4)
		{
			int num = list.IndexOf(item.Id);
			mIds.Add(item.Id);
			mWhys.Add(item.Why);
			mItems.Add(list2[num]);
			if (num == chosen)
			{
				mChosen = mIds.Count - 1;
			}
		}
	}

	private void BeginScene()
	{
		EnsureLabels();
		DWGameCamera instance = Singleton<DWGameCamera>.Instance;
		iTween.Stop(instance.MainCam.gameObject);
		iTween.Stop(instance.MainCamLookAt.gameObject);
		instance.MainCamLookAt.SetFollowCam(enable: true);
		mSavedCam = instance.MainCam.transform.position;
		mSavedLook = instance.MainCamLookAt.transform.position;
		mSavedFov = instance.MainCam.fieldOfView;
		mHeroWasShown = (instance.MainCam.cullingMask & (1 << LayerMask.NameToLayer("PIP"))) != 0;
		instance.RenderP1Character(render: true);
		base.InCloseUp = true;
		mPanel.gameObject.SetActive(value: true);
		mShade.alpha = 0f;
		HideHud();
		StartCoroutine(Bars(show: true, 0.6f));
	}

	private IEnumerator Nightfall(PlayerState owner, CreatureState payer, string title, string line)
	{
		mOwner = owner;
		mPayer = payer;
		mShifter = payer;
		LoadKit();
		BeginScene();
		EnsureWorld();
		Layout(owner);
		Singleton<SLOTAudioManager>.Instance.PlaySound("battle/SFX_DeathSting");
		Singleton<CharacterAnimController>.Instance.PlayHeroAnim(owner.Type, CharAnimType.HeroCard_00);
		StartCoroutine(NightGrade(1f, 1.3f));
		Fog(on: true);
		Animator animator = HeroOf(owner);
		if (animator != null)
		{
			Bounds bounds = ShapeShiftCinematic.ModelBounds(animator.gameObject);
			Vector3 normalized = new Vector3(0f - animator.transform.position.x, 0f, 0f - animator.transform.position.z).normalized;
			if (normalized == Vector3.zero)
			{
				normalized = mFacing;
			}
			Vector3 vector = bounds.center + Vector3.up * bounds.size.y * 0.12f;
			float num = bounds.size.y * 0.95f / Mathf.Tan(mSavedFov * 0.5f * (MathF.PI / 180f));
			Vector3 normalized2 = (Quaternion.AngleAxis(18f, Vector3.up) * normalized * Mathf.Cos(0.17453292f) + Vector3.up * Mathf.Sin(0.17453292f)).normalized;
			StartCoroutine(MoveCam(vector + normalized2 * num, vector, mSavedFov, 1.1f));
		}
		StartCoroutine(Lightning(0.55f, mGraveCentre + mFacing * 6f + Vector3.up * 0.2f, big: false));
		StartCoroutine(Speak(owner, line, 0.45f));
		mTitle.text = title;
		StartCoroutine(ShapeShiftCinematic.FadeLabel(mTitle, 1f, 0.5f));
		yield return new WaitForSeconds(2.3f);
	}

	private void Layout(PlayerState owner)
	{
		int num = ((owner.Type != PlayerType.User) ? 1 : 0);
		DWBattleLane instance = Singleton<DWBattleLane>.Instance;
		float num2 = 0f;
		int num3 = 0;
		mGround = 0f;
		foreach (DWBattleLaneObject item in instance.BattleLaneObjects[num])
		{
			if (item != null)
			{
				num2 += item.transform.position.x;
				mGround = item.transform.position.y;
				num3++;
			}
		}
		float num4 = ((num3 > 0) ? (num2 / (float)num3) : (instance.CreatureOffsetX * ((num == 0) ? 1f : (-1f))));
		if (Mathf.Abs(num4) < 1f)
		{
			num4 = ((num == 0) ? (-9f) : 9f);
		}
		mFacing = new Vector3(0f - Mathf.Sign(num4), 0f, 0f);
		mGraveCentre = new Vector3(num4 * 0.42f, mGround, 0f);
	}

	private IEnumerator RaiseGraves(bool interactive)
	{
		SpawnGraves();
		int num = Mathf.Max(1, mGraves.Count);
		float span = (float)(num - 1) * GraveStep(num) + 5.94f;
		Vector3 vector = mGraveCentre + Vector3.up * 2.7f * 0.95f - mFacing * 1.5f;
		float num2 = mSavedFov;
		float num3 = Mathf.Tan(num2 * 0.5f * (MathF.PI / 180f)) * (float)Screen.width / Mathf.Max(1f, Screen.height);
		float num4 = Mathf.Max(14.04f, span * 0.62f / num3);
		Vector3 vector2 = Quaternion.AngleAxis(-16f * mFacing.x, Vector3.up) * mFacing;
		vector2 = (vector2 * Mathf.Cos(MathF.PI * 2f / 15f) + Vector3.up * Mathf.Sin(MathF.PI * 2f / 15f)).normalized;
		yield return StartCoroutine(MoveCam(vector + vector2 * num4, vector, num2, 1f));
		StartCoroutine(Drift(0f, -7f * mFacing.x, 0.1f, 22f));
		Beam(mGraveCentre - mFacing * 0.5f, 34f, Mathf.Max(9f, span * 0.9f), Moon, 0.28f, 1.2f);
		Wisps(4);
		for (int i = 0; i < mGraves.Count; i++)
		{
			StartCoroutine(Erupt(i, 0.22f * (float)i));
		}
		yield return new WaitForSeconds(0.22f * (float)mGraves.Count + 0.7f);
		SpawnRow(interactive);
		yield return new WaitForSeconds(0.7f);
	}

	private static float GraveStep(int n)
	{
		if (n > 3)
		{
			return Mathf.Max(3.2f, 13.8f / (float)(n - 1));
		}
		return 4.6f;
	}

	private IEnumerator Erupt(int i, float delay)
	{
		yield return new WaitForSeconds(delay);
		Grave g = mGraves[i];
		SLOTAudioManager audio = Singleton<SLOTAudioManager>.Instance;
		audio.PlaySound("battle/SFX_Land_UselessSwamp");
		Singleton<DWGameCamera>.Instance.CameraShake(0.14f, 0.22f);
		Dust(g.At + Vector3.up * 0.2f, 1.6f, 14, 1.1f, 2f, Earth);
		Debris(g.At, 1.2f, 7, 5f, 9f);
		if (g.Stone != null)
		{
			Vector3 position = g.Stone.position;
			g.Stone.position = position - Vector3.up * 2.7f * 1.1f;
			g.Stone.gameObject.SetActive(value: true);
			StartCoroutine(Slide3(g.Stone, position, 0.32f, overshoot: true, wobble: true));
		}
		if (g.Mound != null)
		{
			Vector3 localScale = g.Mound.localScale;
			g.Mound.localScale = new Vector3(localScale.x, 0.01f, localScale.z);
			g.Mound.gameObject.SetActive(value: true);
			StartCoroutine(Scale(g.Mound, localScale, 0.3f, overshoot: true, g.Mound.localScale));
		}
		yield return new WaitForSeconds(0.35f);
		if (g.Ghost != null)
		{
			audio.PlaySound("creature/Ghost");
			g.Ghost.gameObject.SetActive(value: true);
			g.Ghost.position = g.Home - Vector3.up * (g.GhostH + 0.5f);
			StartCoroutine(Slide3(g.Ghost, g.Home, 1.1f, overshoot: false));
			if (g.Stream != null)
			{
				g.Stream.Play();
			}
			StartCoroutine(FadeMat(g.GhostMat, 0.95f, 0.8f));
		}
		Glow(g.At + Vector3.up * 2.7f * 0.6f, Ghost, 6.4800005f, 0.45f, 0.8f);
	}

	private void Idle()
	{
		float time = Time.time;
		Vector3 vector = ((mPayer != null) ? ShapeShiftCinematic.ChestOf(mPayer) : Vector3.zero);
		for (int i = 0; i < mGraves.Count; i++)
		{
			Grave grave = mGraves[i];
			if (!(grave.Ghost == null) && grave.Ghost.gameObject.activeSelf)
			{
				grave.Ghost.position = grave.Home + Vector3.up * (Mathf.Sin(time * 1.6f + grave.Bob) * 0.16f);
				if (grave.Thread != null && mPayer != null && vector != Vector3.zero)
				{
					Thread(grave.Thread, vector, grave.Ghost.position + Vector3.up * grave.GhostH * 0.55f, 0.55f + 0.3f * Mathf.Sin(time * 4f + grave.Bob), 0.07f);
				}
			}
		}
	}

	private IEnumerator TearFree(int index)
	{
		mChosen = index;
		Grave chosen = mGraves[index];
		CreatureItem item = mItems[index];
		SLOTAudioManager audio = Singleton<SLOTAudioManager>.Instance;
		audio.PlaySound("ui/SFX_CreatureCardPickup");
		StartCoroutine(ShapeShiftCinematic.FadeLabel(mSubtitle, 0f, 0.25f));
		for (int i = 0; i < mHolders.Count; i++)
		{
			if (i != index)
			{
				StartCoroutine(MoveHolder(mHolders[i], mHolders[i].localPosition - new Vector3(0f, mH * 0.5f, 0f), mHolders[i].localScale, 0.35f, 0.03f * (float)Mathf.Abs(i - index)));
				mCards[i].SetTargetAlpha(0f, 6f);
			}
		}
		if (index < mHolders.Count)
		{
			StartCoroutine(MoveHolder(mHolders[index], mHolders[index].localPosition - new Vector3(0f, mH * 0.5f, 0f), mHolders[index].localScale, 0.5f, 0.4f));
			mCards[index].SetTargetAlpha(0f, 3f);
		}
		mDrift++;
		yield return StartCoroutine(Lightning(0f, chosen.At + Vector3.up * 2.7f * 0.9f, big: true));
		Beam(chosen.At + mFacing * 0.8f, 30f, 5.5f, Moon, 0.55f, 0.35f);
		audio.PlaySound("creature/GhostStalker");
		for (int j = 0; j < mGraves.Count; j++)
		{
			if (j != index)
			{
				Grave grave = mGraves[j];
				float num = 0.05f * (float)Mathf.Abs(j - index);
				if (grave.Ghost != null)
				{
					StartCoroutine(Slide3(grave.Ghost, grave.At - Vector3.up * 2.5f, 0.5f, overshoot: false, wobble: false, num));
					StartCoroutine(FadeMat(grave.GhostMat, 0f, 0.45f, num));
				}
				if (grave.Thread != null)
				{
					grave.Thread.enabled = false;
				}
				if (grave.Stream != null)
				{
					grave.Stream.Stop();
				}
				StartCoroutine(SinkProp(grave.Stone, 0.55f, 0.25f + num));
				StartCoroutine(SinkProp(grave.Mound, 0.5f, 0.3f + num));
				Dust(grave.At, 1.4f, 8, 0.9f, 1.6f, Earth);
			}
		}
		Vector3 chest = ((mPayer != null) ? ShapeShiftCinematic.ChestOf(mPayer) : Vector3.zero);
		Vector3 ghostAt = ((chosen.Ghost != null) ? (chosen.Ghost.position + Vector3.up * chosen.GhostH * 0.55f) : (chosen.At + Vector3.up * 2.7f));
		if (chosen.Thread != null && chest != Vector3.zero)
		{
			audio.PlaySound("battle/SFX_RezOut");
			Shudder(mPayer);
			Glow(chest, Blood, 4f, 0.9f, 0.6f);
			for (float t = 0f; t < 0.7f; t += Time.deltaTime)
			{
				Thread(chosen.Thread, chest, ghostAt, 1f, Mathf.Lerp(0.16f, 0.06f, t / 0.7f));
				BloodPulse(chosen.Thread, t / 0.7f);
				yield return null;
			}
			chosen.Thread.enabled = false;
		}
		audio.PlaySound("gacha/SFX_GachaSplash");
		Glow(ghostAt, Ghost, 6f, 1f, 0.7f);
		if (chosen.GhostMat != null)
		{
			chosen.GhostMat.SetColor("_Color", new Color(0.8f, 1f, 0.85f, 1f));
		}
		mBanner.text = string.Format(mOnBoard ? L("!!GRAVEYARD_RISES", "{0} rises from the grave!") : L("!!GRAVEYARD_HAND", "{0} returns to your hand!"), item.Form.Name);
		mBanner.transform.localScale = Vector3.one * 0.5f;
		StartCoroutine(ShapeShiftCinematic.FadeLabel(mBanner, 1f, 0.2f));
		StartCoroutine(Scale(mBanner.transform, Vector3.one, 0.35f, overshoot: true, mBanner.transform.localScale));
		StartCoroutine(ShapeShiftCinematic.FadeLabel(mPrice, 0f, 0.4f));
		StartCoroutine(Speak(null, L("!!GRAVEYARD_LINE_RAISE", "Get up. The night isn't over."), 0f));
		yield return new WaitForSeconds(1.2f);
		if (chosen.Ghost != null)
		{
			Vector3 to = (mOnBoard ? (chosen.At - Vector3.up * 3f) : (chosen.Ghost.position + Vector3.up * 8f));
			StartCoroutine(Slide3(chosen.Ghost, to, 0.45f, overshoot: false));
			StartCoroutine(FadeMat(chosen.GhostMat, 0f, 0.45f));
		}
		Dust(chosen.At, 1.8f, 16, 1.2f, 2.2f, Earth);
		Singleton<DWGameCamera>.Instance.CameraShake(0.2f, 0.3f);
		audio.PlaySound("battle/SFX_Land_UselessSwamp");
		yield return new WaitForSeconds(0.5f);
		StartCoroutine(ShapeShiftCinematic.FadeLabel(mBanner, 0f, 0.5f));
		StartCoroutine(ShapeShiftCinematic.FadeLabel(mTitle, 0f, 0.5f));
		ClearCards();
	}

	public IEnumerator Dawn()
	{
		if (!mRising)
		{
			yield break;
		}
		mRising = false;
		mDrift++;
		Fog(on: false);
		StartCoroutine(Hush());
		StartCoroutine(NightGrade(0f, 1f));
		foreach (Grave item in mDressing)
		{
			StartCoroutine(SinkProp(item.Stone, 0.5f, 0f));
		}
		foreach (Grave mGrafe in mGraves)
		{
			StartCoroutine(SinkProp(mGrafe.Stone, 0.5f, 0f));
			StartCoroutine(SinkProp(mGrafe.Mound, 0.5f, 0f));
			if (mGrafe.Ghost != null)
			{
				StartCoroutine(FadeMat(mGrafe.GhostMat, 0f, 0.4f));
			}
			if (mGrafe.Thread != null)
			{
				mGrafe.Thread.enabled = false;
			}
		}
		StartCoroutine(ShapeShiftCinematic.FadeLabel(mPrice, 0f, 0.3f));
		StartCoroutine(ShapeShiftCinematic.FadeLabel(mBanner, 0f, 0.3f));
		yield return new WaitForSeconds(0.6f);
		if (base.InCloseUp)
		{
			yield return StartCoroutine(Release());
			Singleton<DWGameCamera>.Instance.RenderP1Character(mHeroWasShown);
		}
		ClearWorld();
		mChosen = -1;
	}

	private static void LoadKit()
	{
		if (!sKitLoaded)
		{
			sKitLoaded = true;
			sKit = SLOTResourceManager.Load<GameObject>("Graveyard/GY_Kit");
			sHands = SLOTResourceManager.Load<GameObject>("Graveyard/GY_Hands");
			sToon = SLOTResourceManager.Load<Shader>("Graveyard/GY_Toon");
			sGhost = SLOTResourceManager.Load<Shader>("Graveyard/GY_Ghost");
			sNight = SLOTResourceManager.Load<Shader>("Graveyard/GY_Night");
			sFx = SLOTResourceManager.Load<Shader>("Graveyard/GY_FX");
			sAdd = SLOTResourceManager.Load<Shader>("Graveyard/GY_Add");
			tPalette = SLOTResourceManager.Load<Texture2D>("Graveyard/gy_palette");
			tDust = SLOTResourceManager.Load<Texture2D>("Graveyard/gy_fx_dust");
			tFog = SLOTResourceManager.Load<Texture2D>("Graveyard/gy_fx_fog");
			tGlow = SLOTResourceManager.Load<Texture2D>("Graveyard/gy_fx_glow");
			tHole = SLOTResourceManager.Load<Texture2D>("Graveyard/gy_fx_hole");
			tCrack = SLOTResourceManager.Load<Texture2D>("Graveyard/gy_fx_crack");
			tBolt = SLOTResourceManager.Load<Texture2D>("Graveyard/gy_fx_bolt");
			sMist = SLOTResourceManager.Load<Shader>("Graveyard/GY_Mist");
			tNoise = SLOTResourceManager.Load<Texture2D>("Graveyard/gy_fx_noise");
			tBeam = SLOTResourceManager.Load<Texture2D>("Graveyard/gy_fx_beam");
			Shader shader = Shader.Find("Unlit/Transparent Colored");
			sFx = sFx ?? shader;
			sAdd = sAdd ?? sFx;
			sGhost = sGhost ?? sAdd;
			sToon = sToon ?? Shader.Find("Unlit/Texture");
			tGlow = tGlow ?? Texture2D.whiteTexture;
			tDust = tDust ?? tGlow;
			tFog = tFog ?? tGlow;
			tBolt = tBolt ?? tGlow;
			if (sToon != null)
			{
				sToonMat = new Material(sToon);
				sToonMat.mainTexture = tPalette;
			}
			Debug.Log("[Graveyard] kit " + (sKit != null) + " toon " + (sToon != null) + " ghost " + (sGhost != null) + " night " + (sNight != null) + " fx " + (sFx != null) + " palette " + (tPalette != null) + " mist " + (sMist != null && tNoise != null) + " beam " + (tBeam != null) + " bat " + (sKit != null && FindDeep(sKit.transform, "GY_BatBody") != null));
		}
	}

	private void EnsureWorld()
	{
		if (mWorld == null)
		{
			mWorld = new GameObject("GraveyardWorld");
		}
		EnsureLabels();
	}

	private void ClearWorld()
	{
		mGraves.Clear();
		if (mWorld != null)
		{
			UnityEngine.Object.Destroy(mWorld);
		}
		mWorld = null;
		mFog = null;
		mEmbers = null;
		mMist.Clear();
		mBeams.Clear();
		mBats.Clear();
		mLift = 0f;
		if (mNightQuad != null)
		{
			mNightQuad.SetActive(value: false);
		}
	}

	private Transform Prop(string name, Transform parent, float height, bool active, bool byWidth = false)
	{
		Transform transform = ((sKit == null) ? null : FindDeep(sKit.transform, name));
		if (transform == null && sHands != null)
		{
			transform = FindDeep(sHands.transform, name);
		}
		if (transform == null)
		{
			return null;
		}
		GameObject gameObject = UnityEngine.Object.Instantiate(transform.gameObject);
		gameObject.name = name;
		gameObject.transform.SetParent(parent, worldPositionStays: false);
		gameObject.transform.localPosition = Vector3.zero;
		gameObject.transform.localRotation = transform.localRotation;
		gameObject.transform.localScale = Vector3.one;
		Renderer[] componentsInChildren = gameObject.GetComponentsInChildren<Renderer>(includeInactive: true);
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].sharedMaterial = sToonMat;
		}
		Bounds bounds = ModelBoundsAll(gameObject);
		float num = height / Mathf.Max(0.001f, byWidth ? Mathf.Max(bounds.size.x, bounds.size.z) : bounds.size.y);
		gameObject.transform.localScale = Vector3.one * num;
		gameObject.SetActive(active);
		return gameObject.transform;
	}

	private static Transform FindDeep(Transform t, string name)
	{
		if (t.name == name)
		{
			return t;
		}
		for (int i = 0; i < t.childCount; i++)
		{
			Transform transform = FindDeep(t.GetChild(i), name);
			if (transform != null)
			{
				return transform;
			}
		}
		return null;
	}

	private static Bounds ModelBoundsAll(GameObject go)
	{
		bool flag = false;
		Bounds result = new Bounds(go.transform.position, Vector3.zero);
		Renderer[] componentsInChildren = go.GetComponentsInChildren<Renderer>(includeInactive: true);
		foreach (Renderer renderer in componentsInChildren)
		{
			if (flag)
			{
				result.Encapsulate(renderer.bounds);
				continue;
			}
			result = renderer.bounds;
			flag = true;
		}
		return result;
	}

	private void SpawnGraves()
	{
		ClearGraveProps();
		int count = mIds.Count;
		float num = GraveStep(count);
		Vector3 normalized = Vector3.Cross(mFacing, Vector3.up).normalized;
		Quaternion quaternion = Quaternion.LookRotation(mFacing, Vector3.up);
		DWBattleLaneObject reference = ((mPayer == null) ? null : Singleton<DWBattleLane>.Instance.GetLaneObject(mPayer));
		for (int i = 0; i < count; i++)
		{
			float num2 = (float)i - (float)(count - 1) * 0.5f;
			Grave grave = new Grave();
			grave.At = mGraveCentre + normalized * num2 * num - mFacing * num2 * num2 * 0.35f;
			grave.Bob = UnityEngine.Random.Range(0f, 6.28f);
			GameObject gameObject = new GameObject("Grave_" + mIds[i]);
			gameObject.transform.SetParent(mWorld.transform, worldPositionStays: false);
			gameObject.transform.position = grave.At;
			gameObject.transform.rotation = quaternion * Quaternion.Euler(0f, num2 * 7f + UnityEngine.Random.Range(-5f, 5f), 0f);
			grave.Stone = Prop(Stones[(i + count) % Stones.Length], gameObject.transform, 2.7f * UnityEngine.Random.Range(0.92f, 1.08f), active: false);
			if (grave.Stone != null)
			{
				grave.Stone.localRotation = grave.Stone.localRotation * Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(-4f, 4f));
			}
			grave.GhostY = 0.35f;
			grave.Home = grave.At + mFacing * 1.45f + Vector3.up * grave.GhostY;
			MakeGhost(grave, mItems[i], gameObject.transform, reference);
			Pool(gameObject.transform, grave.At + mFacing * 1.2f, Ghost, 3.6f, 0.4f);
			grave.Stream = Embers(grave.At + mFacing * 1.3f, 0.7f, 8f, Ghost, 0.11f, 1.6f, 1.3f);
			if (grave.Stream != null)
			{
				grave.Stream.transform.SetParent(gameObject.transform, worldPositionStays: true);
				grave.Stream.Stop();
			}
			grave.Thread = Line(gameObject.transform, sAdd, tBolt, Blood);
			mGraves.Add(grave);
		}
		string[] array = new string[6] { "GY_cross_wood", "GY_gravestone_broken", "GY_lantern_candle", "GY_rocks_tall", "GY_gravestone_debris", "GY_cross_wood" };
		float num3 = (float)(count - 1) * 0.5f * num + num * 0.75f;
		for (int j = 0; j < array.Length; j++)
		{
			float num4 = ((j % 2 == 0) ? (-1f) : 1f);
			Vector3 vector = mGraveCentre + normalized * num4 * (num3 + (float)(j / 2) * 1.6f) - mFacing * ((float)(j / 2) * 1.2f + 0.8f);
			GameObject gameObject2 = new GameObject("Dress_" + j);
			gameObject2.transform.SetParent(mWorld.transform, worldPositionStays: false);
			gameObject2.transform.position = vector;
			gameObject2.transform.rotation = quaternion * Quaternion.Euler(0f, UnityEngine.Random.Range(-25f, 25f), UnityEngine.Random.Range(-6f, 6f));
			float height = (array[j].Contains("lantern") ? 1.485f : (array[j].Contains("cross") ? 2.97f : 1.6200001f));
			Transform transform = Prop(array[j], gameObject2.transform, height, active: false);
			if (transform != null)
			{
				Grave grave2 = new Grave
				{
					Stone = transform,
					At = vector
				};
				StartCoroutine(RiseDressing(grave2, 0.35f + 0.12f * (float)j, array[j].Contains("lantern")));
				mDressing.Add(grave2);
			}
		}
	}

	private IEnumerator RiseDressing(Grave d, float delay, bool lantern)
	{
		yield return new WaitForSeconds(delay);
		if (!(d.Stone == null))
		{
			Vector3 end = d.Stone.position;
			d.Stone.position = end - Vector3.up * 2.7f;
			d.Stone.gameObject.SetActive(value: true);
			Dust(d.At, 1f, 6, 0.8f, 1.4f, Earth);
			yield return StartCoroutine(Slide3(d.Stone, end, 0.35f, overshoot: true, wobble: true));
			if (lantern)
			{
				Glow(end + Vector3.up * 2.7f * 0.32f, new Color(1f, 0.75f, 0.35f), 2.6f, 0.75f, -1f, d.Stone);
			}
		}
	}

	private void ClearGraveProps()
	{
		ClearCards();
		foreach (Grave mGrafe in mGraves)
		{
			if (mGrafe.Stone != null)
			{
				UnityEngine.Object.Destroy(mGrafe.Stone.parent.gameObject);
			}
		}
		foreach (Grave item in mDressing)
		{
			if (item.Stone != null)
			{
				UnityEngine.Object.Destroy(item.Stone.parent.gameObject);
			}
		}
		mGraves.Clear();
		mDressing.Clear();
	}

	private void MakeGhost(Grave g, CreatureItem item, Transform parent, DWBattleLaneObject reference)
	{
		GameObject gameObject = ((item == null || item.Form == null) ? null : (SLOTResourceManager.Load("Creatures/" + item.Form.Prefab + "/" + item.Form.Prefab, typeof(GameObject)) as GameObject));
		if (gameObject == null || sGhost == null)
		{
			return;
		}
		GameObject gameObject2 = new GameObject("Ghost");
		gameObject2.SetActive(value: false);
		gameObject2.transform.SetParent(parent, worldPositionStays: false);
		GameObject gameObject3 = UnityEngine.Object.Instantiate(gameObject, gameObject2.transform);
		MonoBehaviour[] componentsInChildren = gameObject3.GetComponentsInChildren<MonoBehaviour>(includeInactive: true);
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			UnityEngine.Object.DestroyImmediate(componentsInChildren[i]);
		}
		ParticleSystem[] componentsInChildren2 = gameObject3.GetComponentsInChildren<ParticleSystem>(includeInactive: true);
		for (int i = 0; i < componentsInChildren2.Length; i++)
		{
			componentsInChildren2[i].gameObject.SetActive(value: false);
		}
		Material material = new Material(sGhost);
		material.renderQueue = 3300;
		material.SetColor("_Color", new Color(Ghost.r, Ghost.g, Ghost.b, 0f));
		Texture texture = null;
		Renderer[] componentsInChildren3 = gameObject3.GetComponentsInChildren<Renderer>(includeInactive: true);
		foreach (Renderer renderer in componentsInChildren3)
		{
			if (!(renderer is ParticleSystemRenderer))
			{
				if (texture == null && renderer.sharedMaterial != null)
				{
					texture = renderer.sharedMaterial.mainTexture;
				}
				Material[] array = new Material[renderer.sharedMaterials.Length];
				for (int j = 0; j < array.Length; j++)
				{
					array[j] = material;
				}
				renderer.sharedMaterials = array;
				if (renderer is SkinnedMeshRenderer skinnedMeshRenderer)
				{
					skinnedMeshRenderer.updateWhenOffscreen = true;
				}
			}
		}
		Texture2D texture2D = SLOTResourceManager.Load("Creatures/" + item.Form.Prefab + "/Textures/" + item.Faction.ToString() + "/" + item.Form.PrefabTexture, typeof(Texture2D)) as Texture2D;
		material.mainTexture = ((texture2D != null) ? texture2D : texture);
		Vector3 vector = ((reference != null) ? reference.transform.lossyScale : Vector3.one);
		float num = Mathf.Max(0.5f, item.Form.Height * vector.y);
		float num2 = Mathf.Clamp(3.3f / num, 0.6f, 2.2f);
		gameObject2.transform.localScale = vector * num2;
		g.GhostH = num * num2;
		gameObject2.transform.rotation = ((reference != null) ? reference.transform.rotation : Quaternion.LookRotation(mFacing));
		gameObject3.transform.localPosition = Vector3.zero;
		gameObject3.transform.localRotation = Quaternion.Euler(0f, (mOwner != null && mOwner.Type == PlayerType.Opponent) ? 180f : 0f, 0f);
		g.Ghost = gameObject2.transform;
		g.GhostMat = material;
	}

	private void EnsureNightQuad()
	{
		if (!(mNightQuad != null) && !(sNight == null))
		{
			mNightQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
			mNightQuad.name = "GraveyardNight";
			UnityEngine.Object.Destroy(mNightQuad.GetComponent<Collider>());
			mNightMat = new Material(sNight);
			mNightMat.SetColor("_Color", new Color(NightTint.r, NightTint.g, NightTint.b, 0f));
			mNightMat.SetFloat("_Vignette", 0.8f);
			mNightMat.renderQueue = 3200;
			mNightQuad.GetComponent<Renderer>().sharedMaterial = mNightMat;
			mNightQuad.SetActive(value: false);
		}
	}

	private void LateUpdate()
	{
		if (!(mNightQuad != null) || !mNightQuad.activeSelf)
		{
			return;
		}
		Camera mainCam = Singleton<DWGameCamera>.Instance.MainCam;
		if (mNightQuad.transform.parent != mainCam.transform)
		{
			mNightQuad.transform.SetParent(mainCam.transform, worldPositionStays: false);
			mNightQuad.layer = mainCam.gameObject.layer;
			for (int i = 0; i < 32; i++)
			{
				if ((mainCam.cullingMask & (1 << i)) != 0)
				{
					mNightQuad.layer = i;
					break;
				}
			}
		}
		float num = mainCam.nearClipPlane * 1.5f + 0.01f;
		float num2 = 2f * num * Mathf.Tan(mainCam.fieldOfView * 0.5f * (MathF.PI / 180f)) * 1.1f;
		mNightQuad.transform.localPosition = new Vector3(0f, 0f, num);
		mNightQuad.transform.localRotation = Quaternion.identity;
		mNightQuad.transform.localScale = new Vector3(num2 * mainCam.aspect, num2, 1f);
		mNightMat.SetColor("_Color", new Color(NightTint.r, NightTint.g, NightTint.b, mNightAlpha * (1f - mLift)));
	}

	private IEnumerator NightGrade(float to, float time)
	{
		EnsureNightQuad();
		if (!(mNightMat == null))
		{
			mNightQuad.SetActive(value: true);
			float a0 = mNightAlpha;
			for (float t = 0f; t < time; t += Time.deltaTime)
			{
				mNightAlpha = Mathf.Lerp(a0, to, Mathf.SmoothStep(0f, 1f, t / time));
				yield return null;
			}
			mNightAlpha = to;
			if (to <= 0f)
			{
				mNightQuad.SetActive(value: false);
			}
		}
	}

	private void Fog(bool on)
	{
		if (!on)
		{
			foreach (Material item in mMist)
			{
				StartCoroutine(FadeMat(item, 0f, 1f));
			}
			foreach (Material mBeam in mBeams)
			{
				StartCoroutine(FadeMat(mBeam, 0f, 0.6f));
			}
			mBatsLeave = true;
			if (mFog != null)
			{
				mFog.Stop();
			}
			if (mEmbers != null)
			{
				mEmbers.Stop();
			}
			return;
		}
		Vector3 vector = mGraveCentre;
		mMist.Clear();
		Vector3 centre = new Vector3(0f, mGround, 0f);
		Mist(centre, 70f, 0.12f, 0.6f, 0.05f, new Color(0.42f, 0.52f, 0.74f), 3300);
		Mist(centre, 70f, 0.75f, 0.38f, 0.075f, new Color(0.48f, 0.6f, 0.82f), 3301);
		Mist(centre, 70f, 1.7f, 0.2f, 0.04f, new Color(0.55f, 0.66f, 0.88f), 3302);
		mBatsLeave = false;
		mBatsUp = 0f;
		Bats(9);
		mFog = System(vector + Vector3.up * 0.3f, sFx, tFog, 3300);
		ParticleSystem.MainModule main = mFog.main;
		main.loop = true;
		main.startLifetime = 7f;
		main.startSpeed = 0.35f;
		main.startSize = new ParticleSystem.MinMaxCurve(5f, 8f);
		main.startRotation = new ParticleSystem.MinMaxCurve(0f, 6.28f);
		main.startColor = new Color(0.55f, 0.68f, 0.92f, 0.09f);
		main.maxParticles = 60;
		main.prewarm = true;
		ParticleSystem.EmissionModule emission = mFog.emission;
		emission.rateOverTime = 7f;
		ParticleSystem.ShapeModule shape = mFog.shape;
		shape.shapeType = ParticleSystemShapeType.Box;
		shape.scale = new Vector3(14f, 0.4f, 26f);
		ParticleSystem.ColorOverLifetimeModule colorOverLifetime = mFog.colorOverLifetime;
		colorOverLifetime.enabled = true;
		colorOverLifetime.color = FadeInOut(0.25f, 0.7f);
		ParticleSystem.RotationOverLifetimeModule rotationOverLifetime = mFog.rotationOverLifetime;
		rotationOverLifetime.enabled = true;
		rotationOverLifetime.z = new ParticleSystem.MinMaxCurve(-0.15f, 0.15f);
		mFog.GetComponent<ParticleSystemRenderer>().renderMode = ParticleSystemRenderMode.Billboard;
		mFog.Play();
		mEmbers = Embers(vector, 7f, 9f, Ghost, 0.12f, 2.6f, 1.6f);
	}

	private void Mist(Vector3 centre, float size, float lift, float alpha, float scale, Color color, int queue)
	{
		if (!(sMist == null) && !(tNoise == null) && !(mWorld == null))
		{
			GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Quad);
			UnityEngine.Object.Destroy(obj.GetComponent<Collider>());
			obj.name = "GY_Mist";
			obj.transform.SetParent(mWorld.transform, worldPositionStays: false);
			obj.transform.position = centre + Vector3.up * lift;
			obj.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
			obj.transform.localScale = Vector3.one * size;
			Material material = new Material(sMist);
			material.mainTexture = tNoise;
			material.renderQueue = queue;
			material.SetFloat("_Scale", scale);
			material.SetColor("_Color", new Color(color.r, color.g, color.b, 0f));
			obj.GetComponent<Renderer>().sharedMaterial = material;
			mMist.Add(material);
			StartCoroutine(FadeMat(material, alpha, 1.8f));
		}
	}

	private Material Beam(Vector3 at, float height, float width, Color color, float alpha, float fadeIn)
	{
		if (tBeam == null || mWorld == null)
		{
			return null;
		}
		GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
		UnityEngine.Object.Destroy(gameObject.GetComponent<Collider>());
		gameObject.name = "GY_Beam";
		gameObject.transform.SetParent(mWorld.transform, worldPositionStays: false);
		Material material = new Material(sAdd);
		material.mainTexture = tBeam;
		material.renderQueue = 3306;
		material.SetColor("_Color", new Color(color.r, color.g, color.b, 0f));
		gameObject.GetComponent<Renderer>().sharedMaterial = material;
		mBeams.Add(material);
		StartCoroutine(BeamFace(gameObject.transform, material, at, height, width));
		StartCoroutine(FadeMat(material, alpha, fadeIn));
		return material;
	}

	private IEnumerator BeamFace(Transform q, Material m, Vector3 at, float height, float width)
	{
		Camera cam = Singleton<DWGameCamera>.Instance.MainCam;
		Vector3 axis = (Vector3.up - mFacing * 0.2f + Vector3.forward * 0.1f).normalized;
		while (q != null)
		{
			Vector3 vector = at + axis * height * 0.5f;
			Vector3 vector2 = Vector3.ProjectOnPlane(cam.transform.position - vector, axis);
			if (vector2.sqrMagnitude > 0.0001f)
			{
				q.rotation = Quaternion.LookRotation(-vector2, axis);
			}
			q.position = vector;
			float x = width * (1f + 0.05f * Mathf.Sin(Time.time * 1.3f + at.z));
			q.localScale = new Vector3(x, height, 1f);
			yield return null;
		}
	}

	private Material Pool(Transform parent, Vector3 at, Color color, float size, float alpha)
	{
		if (mWorld == null)
		{
			return null;
		}
		GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
		UnityEngine.Object.Destroy(gameObject.GetComponent<Collider>());
		gameObject.name = "GY_Pool";
		gameObject.transform.SetParent((parent != null) ? parent : mWorld.transform, worldPositionStays: true);
		gameObject.transform.position = at + Vector3.up * 0.07f;
		gameObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
		gameObject.transform.localScale = Vector3.one * size / Mathf.Max(0.001f, gameObject.transform.parent.lossyScale.x);
		Material material = new Material(sAdd);
		material.mainTexture = tGlow;
		material.renderQueue = 3303;
		material.SetColor("_Color", new Color(color.r, color.g, color.b, 0f));
		gameObject.GetComponent<Renderer>().sharedMaterial = material;
		StartCoroutine(FadeMat(material, alpha, 0.6f));
		return material;
	}

	private void Wisps(int count)
	{
		for (int i = 0; i < count; i++)
		{
			if (!(mWorld != null))
			{
				break;
			}
			GameObject gameObject = new GameObject("GY_Wisp");
			gameObject.transform.SetParent(mWorld.transform, worldPositionStays: false);
			gameObject.transform.position = mGraveCentre + Vector3.up * 2f;
			TrailRenderer trailRenderer = gameObject.AddComponent<TrailRenderer>();
			Material material = new Material(sAdd);
			material.mainTexture = tBolt;
			material.renderQueue = 3307;
			trailRenderer.sharedMaterial = material;
			trailRenderer.time = 0.7f;
			trailRenderer.startWidth = 0.32f;
			trailRenderer.endWidth = 0f;
			trailRenderer.minVertexDistance = 0.08f;
			Gradient gradient = new Gradient();
			gradient.SetKeys(new GradientColorKey[2]
			{
				new GradientColorKey(new Color(0.8f, 1f, 0.85f), 0f),
				new GradientColorKey(Ghost, 1f)
			}, new GradientAlphaKey[2]
			{
				new GradientAlphaKey(0.9f, 0f),
				new GradientAlphaKey(0f, 1f)
			});
			trailRenderer.colorGradient = gradient;
			Glow(gameObject.transform.position, Ghost, 1.4f, 0.95f, -1f, gameObject.transform);
			StartCoroutine(WispFly(gameObject.transform, i));
		}
	}

	private IEnumerator WispFly(Transform w, int i)
	{
		float r = 3.2f + (float)i * 1.1f;
		float speed = (0.55f + 0.12f * (float)i) * ((i % 2 == 0) ? 1f : (-1f));
		float phase = (float)i * 1.7f;
		Vector3 side = Vector3.Cross(Vector3.up, mFacing).normalized;
		float t = 0f;
		while (w != null)
		{
			float f = phase + t * speed;
			float num = 1.1f + 1.6f * Mathf.PerlinNoise(t * 0.45f, (float)i * 3.1f) + Mathf.Sin(t * 2.3f + (float)i) * 0.25f;
			w.position = mGraveCentre + side * Mathf.Cos(f) * r + mFacing * Mathf.Sin(f) * r * 0.45f + Vector3.up * num;
			yield return null;
			t += Time.deltaTime;
		}
	}

	private void Bats(int count)
	{
		mBats.Clear();
		if (((sKit == null) ? null : FindDeep(sKit.transform, "GY_BatBody")) == null || mWorld == null)
		{
			return;
		}
		for (int i = 0; i < count; i++)
		{
			GameObject gameObject = new GameObject("GY_Bat");
			gameObject.transform.SetParent(mWorld.transform, worldPositionStays: false);
			Raw("GY_BatBody", gameObject.transform);
			List<Transform> list = new List<Transform>();
			List<float> list2 = new List<float>();
			string[] array = new string[2] { "GY_BatWingL", "GY_BatWingR" };
			foreach (string text in array)
			{
				Transform transform = Raw(text, gameObject.transform);
				if (transform != null)
				{
					Renderer componentInChildren = transform.GetComponentInChildren<Renderer>();
					list.Add(transform);
					list2.Add(Mathf.Sign(gameObject.transform.InverseTransformPoint(componentInChildren.bounds.center).x));
				}
			}
			Bounds bounds = ModelBoundsAll(gameObject);
			float num = UnityEngine.Random.Range(1.3f, 1.9f);
			gameObject.transform.localScale = Vector3.one * num / Mathf.Max(0.01f, bounds.size.x);
			gameObject.SetActive(value: false);
			mBats.Add(gameObject.transform);
			StartCoroutine(BatFly(gameObject.transform, list, list2, i, count));
		}
	}

	private Transform Raw(string name, Transform parent)
	{
		Transform transform = ((sKit == null) ? null : FindDeep(sKit.transform, name));
		if (transform == null)
		{
			return null;
		}
		GameObject gameObject = UnityEngine.Object.Instantiate(transform.gameObject);
		gameObject.name = name;
		gameObject.transform.SetParent(parent, worldPositionStays: false);
		gameObject.transform.localPosition = Vector3.zero;
		gameObject.transform.localRotation = transform.localRotation;
		gameObject.transform.localScale = Vector3.one;
		Renderer[] componentsInChildren = gameObject.GetComponentsInChildren<Renderer>(includeInactive: true);
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].sharedMaterial = sToonMat;
		}
		return gameObject.transform;
	}

	private IEnumerator BatFly(Transform bat, List<Transform> wings, List<float> sides, int i, int count)
	{
		yield return new WaitForSeconds(0.15f * (float)i);
		if (bat == null)
		{
			yield break;
		}
		List<Quaternion> rest = new List<Quaternion>();
		foreach (Transform wing in wings)
		{
			rest.Add(wing.localRotation);
		}
		float radius = UnityEngine.Random.Range(5f, 9f);
		float height = UnityEngine.Random.Range(6.5f, 11f);
		float speed = UnityEngine.Random.Range(0.55f, 0.85f) * ((i % 3 == 0) ? (-1f) : 1f);
		float phase = (float)i * 6.283f / (float)count;
		float flapHz = UnityEngine.Random.Range(6f, 8.5f);
		float flap = UnityEngine.Random.value * 6f;
		Vector3 last = Vector3.zero;
		float leave = 0f;
		bat.gameObject.SetActive(value: true);
		float t = 0f;
		while (bat != null)
		{
			if (mBatsLeave)
			{
				leave += Time.deltaTime;
				if (leave > 3f)
				{
					UnityEngine.Object.Destroy(bat.gameObject);
					break;
				}
			}
			float num = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 3.2f));
			float num2 = Mathf.Lerp(34f, radius, num) + leave * leave * 9f;
			float f = phase + t * speed;
			Vector3 vector = mGraveCentre - mFacing * 5f + Vector3.up * mBatsUp + new Vector3(Mathf.Cos(f) * num2, height + Mathf.Sin(t * 1.7f + (float)i) * 1.1f + (1f - num) * 6f + leave * leave * 5f, Mathf.Sin(f) * num2 * 0.8f);
			flap += Time.deltaTime * flapHz * 6.283f;
			vector.y += Mathf.Sin(flap) * 0.12f;
			bat.position = vector;
			if (last != Vector3.zero && (vector - last).sqrMagnitude > 1E-06f)
			{
				Vector3 vector2 = vector - last;
				float z = Mathf.Clamp((0f - speed) * 40f, -35f, 35f);
				bat.rotation = Quaternion.LookRotation(vector2.normalized, Vector3.up) * Quaternion.Euler(0f, 0f, z);
			}
			last = vector;
			for (int j = 0; j < wings.Count; j++)
			{
				wings[j].localRotation = Quaternion.AngleAxis((Mathf.Sin(flap) * 55f + 12f) * sides[j], Vector3.forward) * rest[j];
			}
			yield return null;
			t += Time.deltaTime;
		}
	}

	private IEnumerator BatsClimb(float to, float time)
	{
		float a0 = mBatsUp;
		for (float t = 0f; t < time; t += Time.deltaTime)
		{
			mBatsUp = Mathf.Lerp(a0, to, Mathf.SmoothStep(0f, 1f, t / time));
			yield return null;
		}
		mBatsUp = to;
	}

	private IEnumerator Drift(float delay, float yawDeg, float push, float time)
	{
		int id = ++mDrift;
		if (delay > 0f)
		{
			yield return new WaitForSeconds(delay);
		}
		DWGameCamera instance = Singleton<DWGameCamera>.Instance;
		Transform camT = instance.MainCam.transform;
		Vector3 look = instance.MainCamLookAt.transform.position;
		Vector3 off = camT.position - look;
		for (float t = 0f; t < time; t += Time.deltaTime)
		{
			if (id != mDrift)
			{
				break;
			}
			float num = Mathf.SmoothStep(0f, 1f, t / time);
			camT.position = look + Quaternion.AngleAxis(yawDeg * num, Vector3.up) * off * (1f - push * num);
			yield return null;
		}
	}

	private ParticleSystem Embers(Vector3 at, float radius, float rate, Color color, float size, float life, float speed)
	{
		ParticleSystem particleSystem = System(at, sAdd, tGlow, 3304);
		ParticleSystem.MainModule main = particleSystem.main;
		main.loop = true;
		main.startLifetime = new ParticleSystem.MinMaxCurve(life * 0.6f, life);
		main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.5f, speed);
		main.startSize = new ParticleSystem.MinMaxCurve(size * 0.6f, size * 1.6f);
		main.startColor = new Color(color.r, color.g, color.b, 0.9f);
		main.maxParticles = 200;
		ParticleSystem.EmissionModule emission = particleSystem.emission;
		emission.rateOverTime = rate;
		ParticleSystem.ShapeModule shape = particleSystem.shape;
		shape.shapeType = ParticleSystemShapeType.Circle;
		shape.radius = radius;
		shape.rotation = new Vector3(-90f, 0f, 0f);
		ParticleSystem.VelocityOverLifetimeModule velocityOverLifetime = particleSystem.velocityOverLifetime;
		velocityOverLifetime.enabled = true;
		velocityOverLifetime.space = ParticleSystemSimulationSpace.World;
		velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(-0.3f, 0.3f);
		velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(speed * 0.5f, speed);
		velocityOverLifetime.z = new ParticleSystem.MinMaxCurve(-0.3f, 0.3f);
		ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particleSystem.colorOverLifetime;
		colorOverLifetime.enabled = true;
		colorOverLifetime.color = FadeInOut(0.15f, 0.6f);
		particleSystem.Play();
		return particleSystem;
	}

	private ParticleSystem System(Vector3 at, Shader shader, Texture tex, int queue)
	{
		GameObject obj = new GameObject("GY_Particles");
		obj.transform.SetParent(mWorld.transform, worldPositionStays: false);
		obj.transform.position = at;
		ParticleSystem particleSystem = obj.AddComponent<ParticleSystem>();
		particleSystem.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmittingAndClear);
		ParticleSystem.MainModule main = particleSystem.main;
		main.playOnAwake = false;
		main.simulationSpace = ParticleSystemSimulationSpace.World;
		main.scalingMode = ParticleSystemScalingMode.Shape;
		obj.GetComponent<ParticleSystemRenderer>().sharedMaterial = new Material(shader)
		{
			mainTexture = tex,
			renderQueue = queue
		};
		return particleSystem;
	}

	private static ParticleSystem.MinMaxGradient FadeInOut(float inAt, float outAt)
	{
		Gradient gradient = new Gradient();
		gradient.SetKeys(new GradientColorKey[2]
		{
			new GradientColorKey(Color.white, 0f),
			new GradientColorKey(Color.white, 1f)
		}, new GradientAlphaKey[4]
		{
			new GradientAlphaKey(0f, 0f),
			new GradientAlphaKey(1f, inAt),
			new GradientAlphaKey(1f, outAt),
			new GradientAlphaKey(0f, 1f)
		});
		return new ParticleSystem.MinMaxGradient(gradient);
	}

	private void Dust(Vector3 at, float radius, int count, float minSize, float maxSize, Color color)
	{
		if (!(mWorld == null))
		{
			ParticleSystem particleSystem = System(at, sFx, tDust, 3000);
			ParticleSystem.MainModule main = particleSystem.main;
			main.loop = false;
			main.duration = 0.2f;
			main.startLifetime = new ParticleSystem.MinMaxCurve(0.7f, 1.3f);
			main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 5.5f);
			main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
			main.startRotation = new ParticleSystem.MinMaxCurve(0f, 6.28f);
			main.startColor = new ParticleSystem.MinMaxGradient(color, Color.Lerp(color, Color.white, 0.35f));
			main.gravityModifier = 0.35f;
			ParticleSystem.EmissionModule emission = particleSystem.emission;
			emission.rateOverTime = 0f;
			emission.SetBursts(new ParticleSystem.Burst[1]
			{
				new ParticleSystem.Burst(0f, (short)count)
			});
			ParticleSystem.ShapeModule shape = particleSystem.shape;
			shape.shapeType = ParticleSystemShapeType.Cone;
			shape.angle = 55f;
			shape.radius = radius * 0.6f;
			shape.rotation = new Vector3(-90f, 0f, 0f);
			ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = particleSystem.sizeOverLifetime;
			sizeOverLifetime.enabled = true;
			sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.5f), new Keyframe(0.3f, 1f), new Keyframe(1f, 1.35f)));
			ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particleSystem.colorOverLifetime;
			colorOverLifetime.enabled = true;
			colorOverLifetime.color = FadeInOut(0.05f, 0.55f);
			ParticleSystem.LimitVelocityOverLifetimeModule limitVelocityOverLifetime = particleSystem.limitVelocityOverLifetime;
			limitVelocityOverLifetime.enabled = true;
			limitVelocityOverLifetime.dampen = 0.12f;
			limitVelocityOverLifetime.limit = 0.5f;
			ParticleSystem.TextureSheetAnimationModule textureSheetAnimation = particleSystem.textureSheetAnimation;
			textureSheetAnimation.enabled = true;
			textureSheetAnimation.numTilesX = 2;
			textureSheetAnimation.numTilesY = 2;
			textureSheetAnimation.animation = ParticleSystemAnimationType.WholeSheet;
			textureSheetAnimation.frameOverTime = new ParticleSystem.MinMaxCurve(0f);
			textureSheetAnimation.startFrame = new ParticleSystem.MinMaxCurve(0f, 3.99f);
			particleSystem.Play();
			UnityEngine.Object.Destroy(particleSystem.gameObject, 3f);
		}
	}

	private void Debris(Vector3 at, float radius, int count, float minUp, float maxUp)
	{
		for (int i = 0; i < count; i++)
		{
			Transform transform = Prop("GY_Chunk_" + UnityEngine.Random.Range(0, 6), mWorld.transform, UnityEngine.Random.Range(0.35f, 0.75f), active: true);
			if (transform == null)
			{
				break;
			}
			float f = UnityEngine.Random.Range(0f, 6.28f);
			Vector3 vector = new Vector3(Mathf.Cos(f), 0f, Mathf.Sin(f));
			transform.position = at + vector * UnityEngine.Random.Range(0f, radius) + Vector3.up * 0.1f;
			transform.rotation = UnityEngine.Random.rotation;
			StartCoroutine(Fly(transform, vector * UnityEngine.Random.Range(1.5f, 4.5f) + Vector3.up * UnityEngine.Random.Range(minUp, maxUp), at.y));
		}
	}

	private IEnumerator Fly(Transform t, Vector3 v, float ground)
	{
		Vector3 spin = UnityEngine.Random.onUnitSphere * UnityEngine.Random.Range(200f, 600f);
		for (float life = 0f; life < 3f; life += Time.deltaTime)
		{
			if (!(t != null))
			{
				break;
			}
			v += Vector3.down * 26f * Time.deltaTime;
			t.position += v * Time.deltaTime;
			t.Rotate(spin * Time.deltaTime, Space.World);
			if (t.position.y < ground && v.y < 0f)
			{
				break;
			}
			yield return null;
		}
		if (t != null)
		{
			yield return StartCoroutine(SinkProp(t, 0.35f, 0.25f));
		}
	}

	private void Glow(Vector3 at, Color color, float size, float alpha, float life, Transform follow = null)
	{
		if (!(mWorld == null))
		{
			GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
			UnityEngine.Object.Destroy(gameObject.GetComponent<Collider>());
			gameObject.name = "GY_Glow";
			gameObject.transform.SetParent((follow != null) ? follow : mWorld.transform, worldPositionStays: true);
			gameObject.transform.position = at;
			gameObject.transform.localScale = Vector3.one * size / Mathf.Max(0.001f, (follow != null) ? follow.lossyScale.x : 1f);
			Material material = new Material(sAdd);
			material.mainTexture = tGlow;
			material.renderQueue = 3310;
			material.SetColor("_Color", new Color(color.r, color.g, color.b, alpha));
			gameObject.GetComponent<Renderer>().sharedMaterial = material;
			StartCoroutine(Billboard(gameObject.transform, material, alpha, life));
		}
	}

	private IEnumerator Billboard(Transform q, Material m, float alpha, float life)
	{
		Camera cam = Singleton<DWGameCamera>.Instance.MainCam;
		Color c = m.GetColor("_Color");
		Vector3 s0 = q.localScale;
		float t = 0f;
		while (q != null && (life < 0f || t < life))
		{
			q.rotation = cam.transform.rotation;
			if (life > 0f)
			{
				float num = t / life;
				c.a = alpha * (1f - num) * Mathf.Clamp01(num * 8f);
				q.localScale = s0 * (0.7f + 0.5f * num);
			}
			else
			{
				c.a = alpha * (0.8f + 0.2f * Mathf.PerlinNoise(t * 6f, 0.3f));
			}
			m.SetColor("_Color", c);
			yield return null;
			t += Time.deltaTime;
		}
		if (q != null)
		{
			UnityEngine.Object.Destroy(q.gameObject);
		}
	}

	private Material Decal(Transform parent, Texture tex, float size, float lift, Color color, int queue)
	{
		if (tex == null)
		{
			return null;
		}
		GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Quad);
		UnityEngine.Object.Destroy(obj.GetComponent<Collider>());
		obj.name = "GY_Decal";
		obj.transform.SetParent(parent, worldPositionStays: false);
		obj.transform.localPosition = Vector3.up * lift;
		obj.transform.rotation = Quaternion.Euler(90f, UnityEngine.Random.Range(0f, 360f), 0f);
		obj.transform.localScale = Vector3.one * size;
		Material material = new Material(sFx);
		material.mainTexture = tex;
		material.renderQueue = queue;
		material.SetColor("_Color", color);
		obj.GetComponent<Renderer>().sharedMaterial = material;
		return material;
	}

	private IEnumerator Lightning(float delay, Vector3 strike, bool big)
	{
		if (delay > 0f)
		{
			yield return new WaitForSeconds(delay);
		}
		if (!(mWorld == null))
		{
			Singleton<SLOTAudioManager>.Instance.PlaySound("battle/SFX_Action_LightingStrike");
			Vector3 sky = strike + Vector3.up * 26f + new Vector3(UnityEngine.Random.Range(-5f, 5f), 0f, UnityEngine.Random.Range(-5f, 5f));
			LineRenderer bolt = Line(mWorld.transform, sAdd, tBolt, new Color(0.85f, 0.92f, 1f));
			LineRenderer branch = Line(mWorld.transform, sAdd, tBolt, new Color(0.7f, 0.85f, 1f));
			bolt.enabled = true;
			branch.enabled = true;
			mFlash.color = new Color(0.8f, 0.9f, 1f, 1f);
			Singleton<DWGameCamera>.Instance.CameraShake(big ? 0.4f : 0.15f, big ? 0.5f : 0.3f);
			if (big)
			{
				Glow(strike, new Color(0.75f, 0.9f, 1f), 7f, 1f, 0.6f);
				Glow(strike, Ghost, 10f, 0.6f, 1.2f);
				Dust(strike - Vector3.up * 2.7f * 0.9f, 1.4f, 10, 1f, 1.8f, new Color(0.6f, 0.62f, 0.75f));
			}
			for (int f = 0; f < 4; f++)
			{
				Jag(bolt, sky, strike, 16, 1.6f, (big ? 1.3f : 0.8f) * (1f - (float)f * 0.15f));
				Vector3 vector = Vector3.Lerp(sky, strike, UnityEngine.Random.Range(0.35f, 0.6f));
				Jag(branch, vector, vector + new Vector3(UnityEngine.Random.Range(-6f, 6f), 0f - UnityEngine.Random.Range(5f, 9f), UnityEngine.Random.Range(-6f, 6f)), 8, 1f, 0.45f);
				mFlash.alpha = ((f % 2 != 0) ? 0.04f : (big ? 0.3f : 0.18f));
				mLift = ((f % 2 != 0) ? 0.25f : (big ? 0.9f : 0.7f));
				yield return new WaitForSeconds(0.06f);
			}
			for (float t = 0f; t < 0.25f; t += Time.deltaTime)
			{
				float num = (branch.widthMultiplier = (bolt.widthMultiplier = 1f - t / 0.25f));
				mFlash.alpha = 0.06f * num;
				mLift = 0.4f * num;
				yield return null;
			}
			mFlash.alpha = 0f;
			mLift = 0f;
			UnityEngine.Object.Destroy(bolt.gameObject);
			UnityEngine.Object.Destroy(branch.gameObject);
		}
	}

	private static void Jag(LineRenderer l, Vector3 a, Vector3 b, int points, float wander, float width)
	{
		l.positionCount = points;
		Vector3 vector = b - a;
		Vector3 normalized = Vector3.Cross(vector, Vector3.up).normalized;
		Vector3 normalized2 = Vector3.Cross(vector, normalized).normalized;
		for (int i = 0; i < points; i++)
		{
			float num = (float)i / (float)(points - 1);
			float num2 = ((i == 0 || i == points - 1) ? 0f : wander);
			l.SetPosition(i, a + vector * num + normalized * UnityEngine.Random.Range(0f - num2, num2) + normalized2 * UnityEngine.Random.Range(0f - num2, num2));
		}
		l.startWidth = width * 0.6f;
		l.endWidth = width;
		l.widthMultiplier = 1f;
	}

	private LineRenderer Line(Transform parent, Shader shader, Texture tex, Color color)
	{
		GameObject obj = new GameObject("GY_Line");
		obj.transform.SetParent(parent, worldPositionStays: false);
		LineRenderer lineRenderer = obj.AddComponent<LineRenderer>();
		lineRenderer.sharedMaterial = new Material(shader)
		{
			mainTexture = tex,
			renderQueue = 3305
		};
		lineRenderer.useWorldSpace = true;
		lineRenderer.textureMode = LineTextureMode.Stretch;
		lineRenderer.startColor = color;
		lineRenderer.endColor = color;
		lineRenderer.numCapVertices = 2;
		lineRenderer.enabled = false;
		return lineRenderer;
	}

	private static void Thread(LineRenderer l, Vector3 from, Vector3 to, float alpha, float width)
	{
		l.enabled = true;
		l.positionCount = 24;
		float num = Vector3.Distance(from, to) * 0.12f;
		for (int i = 0; i < 24; i++)
		{
			float num2 = (float)i / 23f;
			l.SetPosition(i, Vector3.Lerp(from, to, num2) - Vector3.up * num * 4f * num2 * (1f - num2));
		}
		Color blood = Blood;
		blood.a = alpha;
		l.startColor = blood;
		l.endColor = new Color(blood.r, blood.g * 2f, blood.b * 2f, alpha);
		l.startWidth = width * 1.6f;
		l.endWidth = width;
	}

	private static void BloodPulse(LineRenderer l, float k)
	{
		Gradient gradient = new Gradient();
		float time = Mathf.Clamp(k, 0.02f, 0.98f);
		gradient.SetKeys(new GradientColorKey[3]
		{
			new GradientColorKey(Blood, 0f),
			new GradientColorKey(new Color(1f, 0.75f, 0.75f), time),
			new GradientColorKey(Blood, 1f)
		}, new GradientAlphaKey[3]
		{
			new GradientAlphaKey(0.9f, 0f),
			new GradientAlphaKey(1f, time),
			new GradientAlphaKey(0.9f, 1f)
		});
		l.colorGradient = gradient;
	}

	private IEnumerator Slide3(Transform t, Vector3 to, float time, bool overshoot, bool wobble = false, float delay = 0f)
	{
		if (delay > 0f)
		{
			yield return new WaitForSeconds(delay);
		}
		if (t == null)
		{
			yield break;
		}
		Vector3 p0 = t.position;
		Quaternion r0 = t.localRotation;
		for (float x = 0f; x < time; x += Time.deltaTime)
		{
			if (!(t != null))
			{
				break;
			}
			float t2 = (overshoot ? ShapeShiftCinematic.EaseOutBack(x / time) : Mathf.SmoothStep(0f, 1f, x / time));
			t.position = Vector3.LerpUnclamped(p0, to, t2);
			if (wobble)
			{
				t.localRotation = r0 * Quaternion.Euler(Mathf.Sin(x * 40f) * 6f * (1f - x / time), 0f, Mathf.Cos(x * 33f) * 5f * (1f - x / time));
			}
			yield return null;
		}
		if (t != null)
		{
			t.position = to;
			t.localRotation = r0;
		}
	}

	private IEnumerator Scale(Transform t, Vector3 to, float time, bool overshoot, Vector3 from)
	{
		for (float x = 0f; x < time; x += Time.deltaTime)
		{
			if (!(t != null))
			{
				break;
			}
			float t2 = (overshoot ? ShapeShiftCinematic.EaseOutBack(x / time) : Mathf.SmoothStep(0f, 1f, x / time));
			t.localScale = Vector3.LerpUnclamped(from, to, t2);
			yield return null;
		}
		if (t != null)
		{
			t.localScale = to;
		}
	}

	private IEnumerator SinkProp(Transform t, float time, float delay)
	{
		if (delay > 0f)
		{
			yield return new WaitForSeconds(delay);
		}
		if (t == null || !t.gameObject.activeInHierarchy)
		{
			yield break;
		}
		Bounds bounds = ModelBoundsAll(t.gameObject);
		Vector3 p0 = t.position;
		Vector3 p1 = p0 - Vector3.up * (bounds.size.y + 0.3f);
		for (float x = 0f; x < time; x += Time.deltaTime)
		{
			if (!(t != null))
			{
				break;
			}
			float num = x / time;
			t.position = Vector3.Lerp(p0, p1, num * num) + new Vector3(Mathf.Sin(x * 50f) * 0.04f, 0f, 0f);
			yield return null;
		}
		if (t != null)
		{
			t.gameObject.SetActive(value: false);
		}
	}

	private IEnumerator FlipSod(Transform sod, Vector3 outward, float angle, float time, float delay)
	{
		if (delay > 0f)
		{
			yield return new WaitForSeconds(delay);
		}
		MeshFilter componentInChildren = sod.GetComponentInChildren<MeshFilter>();
		Vector3 vector = ((componentInChildren != null && componentInChildren.sharedMesh != null) ? sod.TransformDirection(componentInChildren.sharedMesh.bounds.center) : (-outward));
		vector.y = 0f;
		if (vector.sqrMagnitude > 1E-06f)
		{
			sod.rotation = Quaternion.FromToRotation(vector.normalized, -outward) * sod.rotation;
		}
		Quaternion flat = sod.rotation;
		Vector3 hinge = Vector3.Cross(-outward, Vector3.up);
		if ((Quaternion.AngleAxis(30f, hinge) * -outward).y < 0f)
		{
			hinge = -hinge;
		}
		for (float x = 0f; x < time; x += Time.deltaTime)
		{
			if (!(sod != null))
			{
				break;
			}
			sod.rotation = Quaternion.AngleAxis(angle * ShapeShiftCinematic.EaseOutBack(x / time), hinge) * flat;
			yield return null;
		}
		if (sod != null)
		{
			sod.rotation = Quaternion.AngleAxis(angle, hinge) * flat;
		}
	}

	private IEnumerator Claw(Transform hand, Vector3 at, float h, Vector3 toCam, float delay)
	{
		if (delay > 0f)
		{
			yield return new WaitForSeconds(delay);
		}
		if (hand == null)
		{
			yield break;
		}
		List<Transform> joints = new List<Transform>();
		List<Quaternion> rest = new List<Quaternion>();
		Transform[] componentsInChildren = hand.GetComponentsInChildren<Transform>(includeInactive: true);
		foreach (Transform transform in componentsInChildren)
		{
			if (transform.name.Contains("_F") || transform.name.Contains("_T_"))
			{
				joints.Add(transform);
				rest.Add(transform.localRotation);
			}
		}
		Vector3 low = (hand.position = at - Vector3.up * h * 0.85f);
		hand.gameObject.SetActive(value: true);
		Vector3 up = at - Vector3.up * h * 0.25f;
		Quaternion r0 = hand.rotation;
		for (float x = 0f; x < 0.3f; x += Time.deltaTime)
		{
			hand.position = Vector3.LerpUnclamped(low, up, ShapeShiftCinematic.EaseOutBack(x / 0.3f));
			Curl(joints, rest, 0.2f);
			yield return null;
		}
		Dust(at, 0.8f, 6, 0.8f, 1.3f, Earth);
		for (float x = 0f; x < 0.45f; x += Time.deltaTime)
		{
			Curl(joints, rest, 0.2f + 2.6f * Mathf.Abs(Mathf.Sin(x / 0.45f * MathF.PI * 1.5f)));
			yield return null;
		}
		Vector3 vector2 = Vector3.Cross(Vector3.up, toCam);
		if ((Quaternion.AngleAxis(30f, vector2) * Vector3.up).y > 0f && Vector3.Dot(Quaternion.AngleAxis(30f, vector2) * Vector3.up, toCam) < 0f)
		{
			vector2 = -vector2;
		}
		Quaternion lean = Quaternion.AngleAxis(38f, vector2) * r0;
		for (float x = 0f; x < 0.18f; x += Time.deltaTime)
		{
			float num = x / 0.18f;
			hand.rotation = Quaternion.Slerp(r0, lean, num);
			hand.position = Vector3.Lerp(up, at - Vector3.up * h * 0.32f, num);
			Curl(joints, rest, 1.6f + 1f * num);
			yield return null;
		}
		Singleton<DWGameCamera>.Instance.CameraShake(0.1f, 0.15f);
		Dust(at, 0.7f, 5, 0.7f, 1.2f, Earth);
	}

	private static void Curl(List<Transform> joints, List<Quaternion> rest, float k)
	{
		for (int i = 0; i < joints.Count; i++)
		{
			rest[i].ToAngleAxis(out var angle, out var axis);
			if (angle < 0.5f)
			{
				axis = Vector3.right;
			}
			joints[i].localRotation = Quaternion.AngleAxis(Mathf.Max(angle, 12f) * k, axis);
		}
	}

	private IEnumerator FadeMat(Material m, float alpha, float time, float delay = 0f)
	{
		if (delay > 0f)
		{
			yield return new WaitForSeconds(delay);
		}
		if (m == null)
		{
			yield break;
		}
		Color c = m.GetColor("_Color");
		float a0 = c.a;
		for (float t = 0f; t < time; t += Time.deltaTime)
		{
			if (!(m != null))
			{
				break;
			}
			c.a = Mathf.Lerp(a0, alpha, t / time);
			m.SetColor("_Color", c);
			yield return null;
		}
		if (m != null)
		{
			c.a = alpha;
			m.SetColor("_Color", c);
		}
	}

	private static Animator HeroOf(PlayerState owner)
	{
		CharacterAnimController instance = Singleton<CharacterAnimController>.Instance;
		int intValue = owner.Type.IntValue;
		if (!(instance != null) || intValue < 0 || intValue >= instance.playerCharacters.Count)
		{
			return null;
		}
		return instance.playerCharacters[intValue];
	}

	private static string HeroName(PlayerState owner)
	{
		if (owner == null || owner.Leader == null || owner.Leader.Form == null)
		{
			return string.Empty;
		}
		return owner.Leader.Form.Name;
	}

	private void EnsureLabels()
	{
		EnsureOverlay();
		if (!(mSpeech != null))
		{
			mPanel.depth = 1496;
			mFlashPanel.depth = 1505;
			UILabel src = Singleton<PrefabReferences>.Instance.Card.GetComponent<CardPrefabScript>().Name;
			float num = mH * 0.09f;
			float y = (0f - mH) * 0.5f + num * 0.5f;
			float num2 = num * 0.82f;
			mSpeechFace = ShapeShiftCinematic.Quad(mCardPanel, Texture2D.whiteTexture, Color.white, 5, num2, num2, new Vector3((0f - mW) * 0.5f + mW * 0.04f + num2 * 0.5f, y, 0f));
			mSpeechFace.alpha = 0f;
			mSpeaker = MakeLabel(src, 24, new Color(1f, 0.78f, 0.36f), new Color(0.12f, 0.04f, 0f));
			mSpeaker.pivot = UIWidget.Pivot.Left;
			mSpeaker.transform.localPosition = new Vector3((0f - mW) * 0.5f + mW * 0.04f + num2 + 14f, y, 0f);
			mSpeech = MakeLabel(src, 24, new Color(0.94f, 0.94f, 1f), new Color(0.05f, 0f, 0.1f));
			mSpeech.pivot = UIWidget.Pivot.Left;
			mPrice = MakeLabel(src, 22, new Color(1f, 0.5f, 0.55f), new Color(0.18f, 0f, 0.03f));
			mBanner = MakeLabel(src, 50, new Color(0.78f, 1f, 0.84f), new Color(0.02f, 0.16f, 0.07f));
			mBanner.transform.localPosition = new Vector3(0f, mH * 0.27f, 0f);
			mTitle.color = new Color(0.72f, 1f, 0.8f);
			mTitle.effectColor = new Color(0.04f, 0.12f, 0.06f);
			mSubtitle.fontSize = 24;
			mSubtitle.transform.localPosition = new Vector3(0f, mH * 0.5f - num - 20f, 0f);
			mPrice.transform.localPosition = new Vector3(0f, mH * 0.5f - num - 48f, 0f);
		}
	}

	private UILabel MakeLabel(UILabel src, int size, Color color, Color outline)
	{
		UILabel uILabel = Label(src, size, Vector3.zero, color);
		uILabel.effectColor = outline;
		return uILabel;
	}

	private IEnumerator Speak(PlayerState owner, string line, float delay)
	{
		if (delay > 0f)
		{
			yield return new WaitForSeconds(delay);
		}
		if (owner != null)
		{
			LeaderData leaderData = ((owner.Leader != null) ? owner.Leader.Form : null);
			mSpeaker.text = HeroName(owner).ToUpperInvariant();
			if (leaderData != null && !string.IsNullOrEmpty(leaderData.PortraitTexture))
			{
				Singleton<SLOTResourceManager>.Instance.QueueUITextureLoad(leaderData.PortraitTexture, null, "UI/UI/LoadingPlaceholder", mSpeechFace);
			}
		}
		if (!string.IsNullOrEmpty(mSpeaker.text))
		{
			StartCoroutine(ShapeShiftCinematic.FadeLabel(mSpeaker, 1f, 0.25f));
			StartCoroutine(FadeWidget(mSpeechFace, 1f, 0.25f));
		}
		mSpeech.transform.localPosition = mSpeaker.transform.localPosition + new Vector3(mSpeaker.printedSize.x + 18f, 0f, 0f);
		mSpeech.alpha = 1f;
		mSpeech.text = string.Empty;
		int said = ++mSaid;
		for (int i = 1; i <= line.Length; i++)
		{
			mSpeech.text = line.Substring(0, i);
			yield return new WaitForSeconds(0.028f);
		}
		yield return new WaitForSeconds(3f);
		if (said == mSaid)
		{
			yield return StartCoroutine(Hush());
		}
	}

	private IEnumerator Hush()
	{
		StartCoroutine(ShapeShiftCinematic.FadeLabel(mSpeech, 0f, 0.4f));
		StartCoroutine(ShapeShiftCinematic.FadeLabel(mSpeaker, 0f, 0.4f));
		yield return StartCoroutine(FadeWidget(mSpeechFace, 0f, 0.4f));
	}

	private IEnumerator FadeWidget(UIWidget w, float alpha, float time)
	{
		if (!(w == null))
		{
			float a0 = w.alpha;
			for (float t = 0f; t <= time; t += Time.deltaTime)
			{
				w.alpha = Mathf.Lerp(a0, alpha, t / time);
				yield return null;
			}
			w.alpha = alpha;
		}
	}

	private void SpawnRow(bool interactive)
	{
		ClearCards();
		int count = mIds.Count;
		float num = mH * 0.24f;
		float num2 = mW * 0.015f;
		for (int i = 0; i < count; i++)
		{
			GameObject gameObject = NGUITools.AddChild(mCardPanel.gameObject);
			gameObject.name = "Grave_" + mIds[i];
			GameObject gameObject2 = NGUITools.AddChild(gameObject);
			GameObject gameObject3 = NGUITools.AddChild(gameObject2, Singleton<PrefabReferences>.Instance.Card);
			CardPrefabScript component = gameObject3.GetComponent<CardPrefabScript>();
			component.Mode = CardPrefabScript.CardMode.BattlePopup;
			CreatureItem creatureItem = mItems[i];
			component.Populate(creatureItem);
			component.Text.text = ShapeShiftCinematic.StatsText(creatureItem) + "\n[9CFF7A]" + mWhys[i] + "[-]";
			if (interactive)
			{
				component.JokerMode = true;
				int index = i;
				component.OnJokerClick = delegate
				{
					Pick(index);
				};
				Collider[] componentsInChildren = gameObject3.GetComponentsInChildren<Collider>(includeInactive: true);
				foreach (Collider collider in componentsInChildren)
				{
					UICamera.AlwaysAllowedColliders.Add(collider.gameObject);
					mAllowed.Add(collider.gameObject);
				}
			}
			else
			{
				Collider[] componentsInChildren = gameObject3.GetComponentsInChildren<Collider>(includeInactive: true);
				for (int num3 = 0; num3 < componentsInChildren.Length; num3++)
				{
					componentsInChildren[num3].enabled = false;
				}
			}
			Bounds bounds = NGUIMath.CalculateRelativeWidgetBounds(gameObject2.transform, component.Frame.transform, considerInactive: true);
			gameObject2.transform.localPosition = -bounds.center;
			float num4 = num / Mathf.Max(1f, bounds.size.y);
			float num5 = bounds.size.x * num4;
			if ((float)count * num5 + (float)(count - 1) * num2 > mW * 0.8f)
			{
				num4 *= mW * 0.8f / ((float)count * num5 + (float)(count - 1) * num2);
				num5 = bounds.size.x * num4;
			}
			float num6 = (float)i - (float)(count - 1) * 0.5f;
			Vector3 vector = new Vector3(num6 * (num5 + num2), (0f - mH) * 0.5f + mH * 0.09f + bounds.size.y * num4 * 0.5f + 8f, 0f);
			gameObject.transform.localScale = Vector3.one * num4;
			gameObject.transform.localPosition = vector + new Vector3(0f, (0f - mH) * 0.4f, 0f);
			component.SetAlpha(0f);
			component.SetTargetAlpha(1f, 8f);
			StartCoroutine(MoveHolder(gameObject.transform, vector, gameObject.transform.localScale, 0.45f, 0.06f * (float)i, Quaternion.identity, overshoot: true));
			mHolders.Add(gameObject.transform);
			mCards.Add(component);
		}
	}

	private static void Shudder(CreatureState c)
	{
		DWBattleLaneObject dWBattleLaneObject = ((c == null) ? null : Singleton<DWBattleLane>.Instance.GetLaneObject(c));
		if (dWBattleLaneObject != null && dWBattleLaneObject.CreatureObject != null)
		{
			iTween.ShakePosition(dWBattleLaneObject.CreatureObject, iTween.Hash("amount", new Vector3(0.2f, 0f, 0.2f), "time", 0.6f));
		}
	}

	private static string L(string key, string fallback)
	{
		string text = KFFLocalization.Get(key);
		if (!string.IsNullOrEmpty(text) && !(text == key) && !text.Contains(key.TrimStart('!')))
		{
			return text;
		}
		return fallback;
	}
}
