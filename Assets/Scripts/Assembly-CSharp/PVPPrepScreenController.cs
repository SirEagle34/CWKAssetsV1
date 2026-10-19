using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class PVPPrepScreenController : Singleton<PVPPrepScreenController>
{
	private enum NextAction
	{
		NONE = 0,
		WAITING = 1,
		PROCEED = 2,
		ERROR = 3,
		RETRY = 4
	}

	[Serializable]
	private class BanlistResponse
	{
		public string[] banned_creatures;

		public string[] banned_leaders;

		public string server_time;

		public ActiveBanInfo[] active_bans;
	}

	[Serializable]
	private class ActiveBanInfo
	{
		public string id;

		public string name;

		public string starts;

		public string expires;
	}

	public float CountdownTime;

	public UILabel HardCurrencyLabel;

	public UILabel SoftCurrencyLabel;

	public UILabel PlayerStamina;

	public UILabel PlayerStaminaTimer;

	public UILabel EnergyCost;

	public LeagueBadge PlayerBadge;

	public LeagueBadge OpponentBadge;

	public UILabel Countdown;

	public GameObject ReadyBlockingCollider;

	public UILabel LevelSetNotificationLabel;

	public UITexture[] PIPIconTargets = new UITexture[2];

	public Transform[] MyCreatureNodes;

	public UILabel MyPlayerNameLabel;

	public UILabel TeamCost;

	public UILabel TeamName;

	public UILabel TitleLabel;

	public UILabel LeaderName;

	public GameObject CancelButton;

	public GameObject BackButton;

	public GameObject PressToSearchLabel;

	public GameObject HeartButtonDecoration;

	public GameObject CheckmarkButtonDecoration;

	public UITexture BackgroundTexture;

	[Header("Ban UI (assign if available)")]
	[SerializeField]
	private GameObject Button_StartBattle;

	[SerializeField]
	private GameObject Label_Play;

	[SerializeField]
	private GameObject BanWarningNode;

	[SerializeField]
	private UILabel BanWarningLabel;

	[Header("Ban Visuals (optional)")]
	[SerializeField]
	private GameObject BanTileLabelPrefab;

	[SerializeField]
	private Vector3 BanTileLabelLocalOffset = new Vector3(120.1f, -3f, 0f);

	private List<GameObject> mMySpawnedCreatures = new List<GameObject>();

	private GameObject mSpawnedBanCreature;

	private GameObject mBanTileLabelInstance;

	private GameObject mButtonBlockedLabelInstance;

	private UITexture mStartButtonTexture;

	private Color mButtonOriginalColor = Color.white;

	private bool mWaitingForOpponentStartData;

	private bool mOpponentStartDataReceived;

	private float mMatchStartCountdown = -1f;

	private PvpMode mMode;

	private HelperItem mAlly;

	private int mLastCountdownVal;

	private bool mActive;

	public bool matchFound;

	public float syncTime;

	public ParticleSystem CountdownVFX;

	public GameObject SwordShieldAnimation;

	[Header("Tweens")]
	public UITweenController ShowTween;

	public UITweenController ShowAllyTween;

	public UITweenController HideTween;

	public UITweenController ExceededTeamCostTween;

	public UITweenController ConsumeTicketTween;

	public UITweenController ShowConnectingTween;

	public UITweenController HideConnectingTween;

	public UITweenController OpponentFoundTween;

	public UITweenController ShowFlagsAndCounterTween;

	public UITweenController FriendMatchStartingTween;

	public UITweenController CountdownPulseTween;

	public UITweenController ReadyTween;

	private bool mSearching;

	private bool mBackButtonPressed;

	private bool mWaitForUserAction;

	private NextAction mUserActionProceed;

	private PlayerSaveData.ProceedNextStep mNextFunction;

	private bool hasShownBanPopupThisOpen;

	private List<string> pvpBanlist = new List<string>();

	private List<string> pvpLeaderBanlist = new List<string>();

	private bool banlistFetched;

	private bool isFetchingBanlist;

	private void SafePlay(UITweenController tween)
	{
		if (tween == null)
		{
			return;
		}
		try
		{
			tween.Play();
		}
		catch (Exception ex)
		{
			string text = ((tween != null && tween.gameObject != null) ? tween.gameObject.name : "<unknown tween>");
			Debug.LogWarning("[PVPPrepScreenController] SafePlay failed on " + text + ": " + ex.Message);
		}
	}

	private void SafeStopAndReset(UITweenController tween)
	{
		if (tween == null)
		{
			return;
		}
		try
		{
			tween.StopAndReset();
		}
		catch (Exception ex)
		{
			string text = ((tween != null && tween.gameObject != null) ? tween.gameObject.name : "<unknown tween>");
			Debug.LogWarning("[PVPPrepScreenController] SafeStopAndReset failed on " + text + ": " + ex.Message);
		}
	}

	private void SafePlayWithCallback(UITweenController tween, EventDelegate.Callback callback)
	{
		if (tween == null)
		{
			callback?.Invoke();
			return;
		}
		try
		{
			tween.PlayWithCallback(callback);
		}
		catch (Exception ex)
		{
			string text = ((tween != null && tween.gameObject != null) ? tween.gameObject.name : "<unknown tween>");
			Debug.LogWarning("[PVPPrepScreenController] SafePlayWithCallback failed on " + text + ": " + ex.Message);
			callback?.Invoke();
		}
	}

	private void Awake()
	{
		ResolveLocalReferences();
		Transform transform = FindChildRecursive(base.transform, "PlayBlockedLabel");
		if (transform != null)
		{
			transform.gameObject.SetActive(value: false);
			mButtonBlockedLabelInstance = transform.gameObject;
		}
	}

	private void ResolveLocalReferences()
	{
		if (Button_StartBattle != null && Label_Play != null && BanWarningNode != null && BanWarningLabel != null)
		{
			CacheStartButtonTexture();
			return;
		}
		Action<Transform> action = delegate(Transform parent)
		{
			string text = "";
			for (int i = 0; i < parent.childCount; i++)
			{
				text = text + parent.GetChild(i).name + ", ";
			}
			Debug.LogWarning("[PVPPrep] Children of " + parent.name + ": " + text);
		};
		if (Button_StartBattle == null)
		{
			Transform transform = base.transform.Find("GameStartButtons/Button_StartBattle");
			if (transform == null)
			{
				transform = FindChildRecursive(base.transform, "Button_StartBattle");
			}
			if (transform != null)
			{
				Button_StartBattle = transform.gameObject;
			}
			else
			{
				Debug.LogWarning("[PVPPrep] Button_StartBattle not found under " + base.name);
				action(base.transform);
			}
		}
		CacheStartButtonTexture();
		if (Label_Play == null)
		{
			Transform transform2 = base.transform.Find("GameStartButtons/Label_Play");
			if (transform2 == null)
			{
				transform2 = FindChildRecursive(base.transform, "Label_Play");
			}
			if (transform2 != null)
			{
				Label_Play = transform2.gameObject;
			}
			else
			{
				Debug.LogWarning("[PVPPrep] Label_Play not found under " + base.name);
			}
		}
		if (BanWarningNode == null)
		{
			Transform transform3 = FindChildRecursive(base.transform, "BanWarningNode");
			if (transform3 != null)
			{
				BanWarningNode = transform3.gameObject;
			}
			else
			{
				Debug.LogWarning("[PVPPrep] BanWarningNode not found under " + base.name);
				action(base.transform);
			}
		}
		if (BanWarningNode != null)
		{
			BanWarningNode.transform.localPosition = new Vector3(-670f, 610f, -76f);
			if (BanWarningLabel == null)
			{
				BanWarningLabel = BanWarningNode.GetComponentInChildren<UILabel>(includeInactive: true);
			}
			if (BanWarningLabel != null)
			{
				BanWarningLabel.transform.localPosition = Vector3.zero;
			}
		}
		if (BanWarningLabel == null && BanWarningNode != null)
		{
			BanWarningLabel = BanWarningNode.GetComponentInChildren<UILabel>(includeInactive: true);
			if (BanWarningLabel == null)
			{
				Debug.LogWarning("[PVPPrep] BanWarningLabel not found under BanWarningNode");
			}
		}
	}

	private void CacheStartButtonTexture()
	{
		if (!(Button_StartBattle == null) && !(mStartButtonTexture != null))
		{
			mStartButtonTexture = Button_StartBattle.GetComponent<UITexture>();
			if (mStartButtonTexture != null)
			{
				mButtonOriginalColor = mStartButtonTexture.color;
			}
		}
	}

	private Transform FindChildRecursive(Transform parent, string name)
	{
		if (parent == null)
		{
			return null;
		}
		if (parent.name == name)
		{
			return parent;
		}
		for (int i = 0; i < parent.childCount; i++)
		{
			Transform child = parent.GetChild(i);
			Transform transform = FindChildRecursive(child, name);
			if (transform != null)
			{
				return transform;
			}
		}
		return null;
	}

	public void DebugDumpAndForceShow()
	{
		Debug.Log("[PVPPrep] DebugDumpAndForceShow invoked.");
		Debug.Log("[PVPPrep] This controller activeInHierarchy: " + base.gameObject.activeInHierarchy);
		Debug.Log("[PVPPrep] BanWarningNode assigned: " + (BanWarningNode != null));
		Debug.Log("[PVPPrep] BanWarningLabel assigned: " + (BanWarningLabel != null));
		Debug.Log("[PVPPrep] Button_StartBattle assigned: " + (Button_StartBattle != null));
		Debug.Log("[PVPPrep] Label_Play assigned: " + (Label_Play != null));
		Debug.Log("[PVPPrep] ShowTween assigned: " + (ShowTween != null));
		Debug.Log("[PVPPrep] ShowAllyTween assigned: " + (ShowAllyTween != null));
		Debug.Log("[PVPPrep] ExceededTeamCostTween assigned: " + (ExceededTeamCostTween != null));
		if (BanWarningNode != null)
		{
			BanWarningNode.SetActive(value: true);
		}
		if (Button_StartBattle != null)
		{
			Button_StartBattle.SetActive(value: true);
		}
		if (Label_Play != null)
		{
			Label_Play.SetActive(value: true);
		}
		if (BanWarningLabel != null)
		{
			Debug.Log("[PVPPrep] BanWarningLabel.text: " + BanWarningLabel.text);
		}
	}

	public bool InFriendMatchLobby()
	{
		if (mActive)
		{
			return mMode == PvpMode.Friend;
		}
		return false;
	}

	public void Show(PvpMode mode, string allyName, bool inShouldShowBackButton = false)
	{
		hasShownBanPopupThisOpen = false;
		mActive = true;
		if (ReadyBlockingCollider != null)
		{
			ReadyBlockingCollider.SetActive(value: false);
		}
		if (SwordShieldAnimation != null)
		{
			SwordShieldAnimation.SetActive(value: false);
		}
		if (LevelSetNotificationLabel != null)
		{
			LevelSetNotificationLabel.text = string.Empty;
		}
		Singleton<PlayerInfoScript>.Instance.StateData.MultiplayerMode = true;
		Singleton<PlayerInfoScript>.Instance.PvPData.RankedMode = mode == PvpMode.Ranked;
		Singleton<PlayerInfoScript>.Instance.PvPData.ExtremeMode = mode == PvpMode.Extreme;
		mMode = mode;
		if (mode == PvpMode.Extreme)
		{
			PvpExtremeTeam.Reset();
		}
		if (mode.EnforceBans() && !banlistFetched)
		{
			StartCoroutine(FetchPVPBanlist());
		}
		if (BackButton != null)
		{
			BackButton.SetActive(inShouldShowBackButton);
		}
		if (EnergyCost != null)
		{
			EnergyCost.text = MiscParams.PvpStaminaMatchCost.ToString();
		}
		if (BanWarningNode != null)
		{
			BanWarningNode.SetActive(value: false);
		}
		if (BanWarningLabel != null)
		{
			BanWarningLabel.text = string.Empty;
			BanWarningLabel.gameObject.SetActive(value: false);
		}
		if (mButtonBlockedLabelInstance != null)
		{
			mButtonBlockedLabelInstance.SetActive(value: false);
		}
		Transform[] array = (from t in base.transform.root.GetComponentsInChildren<Transform>(includeInactive: true)
			where t.name == "PlayBlockedLabel"
			select t).ToArray();
		for (int num = 0; num < array.Length; num++)
		{
			array[num].gameObject.SetActive(value: false);
		}
		if (Label_Play != null)
		{
			Label_Play.SetActive(value: true);
		}
		RefreshMyLoadout();
		mWaitingForOpponentStartData = false;
		mOpponentStartDataReceived = false;
		mMatchStartCountdown = -1f;
		switch (mode)
		{
		case PvpMode.Ranked:
			if (TitleLabel != null)
			{
				TitleLabel.text = KFFLocalization.Get("!!RANKED_MATCH");
			}
			break;
		case PvpMode.Extreme:
			if (TitleLabel != null)
			{
				TitleLabel.text = KFFLocalization.Get("!!EXTREME_MATCH");
			}
			break;
		case PvpMode.Unranked:
			if (TitleLabel != null)
			{
				TitleLabel.text = KFFLocalization.Get("!!UNRANKED_MATCH");
			}
			break;
		case PvpMode.Friend:
			if (TitleLabel != null)
			{
				TitleLabel.text = KFFLocalization.Get("!!ALLY_MATCH");
			}
			break;
		}
		if (mode == PvpMode.Friend)
		{
			SafePlay(ShowAllyTween);
			if (PressToSearchLabel != null)
			{
				PressToSearchLabel.SetActive(value: false);
			}
			if (HeartButtonDecoration != null)
			{
				HeartButtonDecoration.SetActive(value: false);
			}
			if (CheckmarkButtonDecoration != null)
			{
				CheckmarkButtonDecoration.SetActive(value: true);
			}
		}
		else
		{
			SafePlay(ShowTween);
			if (PressToSearchLabel != null)
			{
				PressToSearchLabel.SetActive(value: true);
			}
			if (HeartButtonDecoration != null)
			{
				HeartButtonDecoration.SetActive(value: true);
			}
			if (CheckmarkButtonDecoration != null)
			{
				CheckmarkButtonDecoration.SetActive(value: false);
			}
		}
		Singleton<PlayerInfoScript>.Instance.CheckPvpSeasonStatus();
	}

	private Loadout MyLoadout()
	{
		if (mMode == PvpMode.Extreme)
		{
			Loadout current = PvpExtremeTeam.Current;
			if (current != null)
			{
				return current;
			}
		}
		return Singleton<PlayerInfoScript>.Instance.GetCurrentLoadout();
	}

	private void ShowTeamButtons(bool show)
	{
		UIButton[] componentsInChildren = GetComponentsInChildren<UIButton>(includeInactive: true);
		foreach (UIButton uIButton in componentsInChildren)
		{
			foreach (EventDelegate item in uIButton.onClick)
			{
				if (item != null && item.methodName == "OnClickEditDeck")
				{
					uIButton.gameObject.SetActive(show);
					break;
				}
			}
		}
	}

	private void RefreshMyLoadout()
	{
		if (MyPlayerNameLabel != null)
		{
			MyPlayerNameLabel.text = Singleton<PlayerInfoScript>.Instance.PvPData.PlayerName;
		}
		Loadout loadout = MyLoadout();
		if (loadout != null && PIPIconTargets != null && PIPIconTargets.Length != 0 && PIPIconTargets[0] != null)
		{
			try
			{
				Singleton<FrontEndPIPController>.Instance.ShowModelPortraits(new LeaderData[1] { loadout.Leader.SelectedSkin }, new UIWidget[1] { PIPIconTargets[0] });
			}
			catch (Exception ex)
			{
				Debug.LogWarning("[PVPPrep] ShowModelPortraits failed: " + ex.Message);
			}
		}
		foreach (GameObject mMySpawnedCreature in mMySpawnedCreatures)
		{
			NGUITools.Destroy(mMySpawnedCreature);
		}
		mMySpawnedCreatures.Clear();
		InventoryTile.ClearDelegates(clickable: true);
		if (loadout != null)
		{
			MyCreatureNodes = TeamRow.Fit(MyCreatureNodes, loadout.CreatureSet.Count);
		}
		if (loadout != null)
		{
			if (MyCreatureNodes != null)
			{
				for (int i = 0; i < loadout.CreatureSet.Count && i < MyCreatureNodes.Length; i++)
				{
					if (loadout.CreatureSet[i] == null)
					{
						continue;
					}
					GameObject gameObject = MyCreatureNodes[i].InstantiateAsChild(Singleton<PrefabReferences>.Instance.InventoryTile);
					if (gameObject != null)
					{
						gameObject.ChangeLayer(base.gameObject.layer);
						InventoryTile component = gameObject.GetComponent<InventoryTile>();
						if (component != null)
						{
							component.Populate(loadout.CreatureSet[i]);
							component.ShowRarityFrameMini();
						}
						mMySpawnedCreatures.Add(gameObject);
					}
				}
			}
			int selectedLoadout = Singleton<PlayerInfoScript>.Instance.SaveData.SelectedLoadout;
			bool flag = mMode == PvpMode.Extreme;
			if (TeamName != null)
			{
				TeamName.text = (flag ? "Extreme Team" : Singleton<PlayerInfoScript>.Instance.GetTeamName(selectedLoadout));
			}
			int num = ((!flag) ? loadout.GetTeamCost() : 0);
			int teamCost = Singleton<PlayerInfoScript>.Instance.RankData.TeamCost;
			if (TeamCost != null)
			{
				TeamCost.text = KFFLocalization.Get("!!TEAM_WEIGHT") + "  " + num + " / " + teamCost;
				TeamCost.gameObject.SetActive(!flag);
			}
			ShowTeamButtons(!flag);
			if (LeaderName != null)
			{
				LeaderName.text = loadout.Leader.Form.Name;
			}
			if (BackgroundTexture != null)
			{
				BackgroundTexture.ReplaceTexture(loadout.Leader.Form.VSScreenBackground);
			}
			if (num > teamCost)
			{
				SafePlay(ExceededTeamCostTween);
			}
			else
			{
				SafeStopAndReset(ExceededTeamCostTween);
			}
			RefreshMyLeagueBadge(includeNameAndFlag: true);
		}
		CheckPvPBanlistInPrep();
	}

	private void CheckPvPBanlistInPrep()
	{
		ResolveLocalReferences();
		if (!mMode.EnforceBans())
		{
			Debug.Log("[PVPPrep] Skipping ban check – mode is " + mMode.ToString() + ".");
			ClearBanVisuals();
			DisableStartButtonInteraction(disable: false);
			return;
		}
		Loadout loadout = MyLoadout();
		if (loadout == null)
		{
			return;
		}
		ClearBanVisuals();
		bool flag = false;
		List<string> list = new List<string>();
		if (loadout.Leader?.Form != null)
		{
			string iD = loadout.Leader.Form.ID;
			if (pvpLeaderBanlist.Contains(iD))
			{
				flag = true;
				list.Add(loadout.Leader.Form.Name);
				DisableStartButtonInteraction(disable: true);
				if (BanWarningNode != null)
				{
					BanWarningNode.SetActive(value: true);
				}
				CacheStartButtonTexture();
				if (mStartButtonTexture != null)
				{
					mStartButtonTexture.color = Color.Lerp(mButtonOriginalColor, Color.red, 0.85f);
				}
			}
		}
		foreach (InventorySlotItem item in loadout.CreatureSet)
		{
			if (item?.Creature?.Form == null)
			{
				continue;
			}
			CreatureItem creature = item.Creature;
			if (!pvpBanlist.Contains(creature.Form.ID))
			{
				continue;
			}
			flag = true;
			list.Add(creature.Form.Name);
			if (!(mSpawnedBanCreature == null))
			{
				continue;
			}
			DisableStartButtonInteraction(disable: true);
			if (BanWarningNode != null)
			{
				BanWarningNode.SetActive(value: true);
				mSpawnedBanCreature = BanWarningNode.InstantiateAsChild(Singleton<PrefabReferences>.Instance.InventoryTile);
				if (mSpawnedBanCreature != null)
				{
					mSpawnedBanCreature.ChangeLayer(base.gameObject.layer);
					InventoryTile component = mSpawnedBanCreature.GetComponent<InventoryTile>();
					if (component != null)
					{
						component.PopulateAndForceDisplay(creature.Form);
					}
					Transform anchor = EnsureBanWarningAnchor();
					ParentAndResetSpawn(mSpawnedBanCreature, anchor, BanTileLabelLocalOffset);
					EnsureBanTileHint(mSpawnedBanCreature, "This creature is banned in PvP!", BanTileLabelLocalOffset);
				}
			}
			else if (MyCreatureNodes != null && MyCreatureNodes.Length != 0)
			{
				mSpawnedBanCreature = MyCreatureNodes[0].InstantiateAsChild(Singleton<PrefabReferences>.Instance.InventoryTile);
				if (mSpawnedBanCreature != null)
				{
					mSpawnedBanCreature.ChangeLayer(base.gameObject.layer);
					InventoryTile component2 = mSpawnedBanCreature.GetComponent<InventoryTile>();
					if (component2 != null)
					{
						component2.PopulateAndForceDisplay(creature.Form);
					}
					ParentAndResetSpawn(mSpawnedBanCreature, MyCreatureNodes[0], BanTileLabelLocalOffset);
					EnsureBanTileHint(mSpawnedBanCreature, "This creature is banned in PvP!", BanTileLabelLocalOffset);
				}
			}
			else if (Singleton<PrefabReferences>.Instance?.InventoryTile != null)
			{
				mSpawnedBanCreature = UnityEngine.Object.Instantiate(Singleton<PrefabReferences>.Instance.InventoryTile);
				mSpawnedBanCreature.ChangeLayer(base.gameObject.layer);
				InventoryTile component3 = mSpawnedBanCreature.GetComponent<InventoryTile>();
				if (component3 != null)
				{
					component3.PopulateAndForceDisplay(creature.Form);
				}
				ParentAndResetSpawn(mSpawnedBanCreature, base.transform, BanTileLabelLocalOffset);
				EnsureBanTileHint(mSpawnedBanCreature, "This creature is banned in PvP!", BanTileLabelLocalOffset);
			}
			CacheStartButtonTexture();
			if (mStartButtonTexture != null)
			{
				mStartButtonTexture.color = Color.Lerp(mButtonOriginalColor, Color.red, 0.85f);
			}
		}
		if (flag)
		{
			string text = ((list.Count == 1) ? list[0] : ((list.Count != 2) ? (string.Join(", ", list.GetRange(0, list.Count - 1).ToArray()) + " and " + list[list.Count - 1]) : (list[0] + " and " + list[1])));
			if (BanWarningLabel != null)
			{
				BanWarningLabel.text = "⚠ " + text + " is banned in PvP!";
				BanWarningLabel.gameObject.SetActive(value: true);
			}
			EnsureSinglePlayBlockedLabel();
			if (!hasShownBanPopupThisOpen)
			{
				hasShownBanPopupThisOpen = true;
				string body = KFFLocalization.Get("!!PVP_BANNED_CREATURE").Replace("<creature>", text);
				Singleton<SimplePopupController>.Instance.ShowMessage(string.Empty, body);
			}
		}
		else
		{
			DisableStartButtonInteraction(disable: false);
			ClearBanVisuals();
		}
	}

	private Transform EnsureBanWarningAnchor()
	{
		if (BanWarningNode != null)
		{
			return BanWarningNode.transform.parent ?? BanWarningNode.transform;
		}
		Transform transform = FindChildRecursive(base.transform, "BanWarningAnchor");
		if (transform != null)
		{
			return transform;
		}
		GameObject obj = new GameObject("BanWarningAnchor");
		obj.layer = base.gameObject.layer;
		obj.transform.SetParent(base.transform, worldPositionStays: false);
		obj.transform.localPosition = new Vector3(480f, -323.5f, 0f);
		obj.transform.localRotation = Quaternion.identity;
		obj.transform.localScale = Vector3.one;
		obj.SetActive(value: true);
		return obj.transform;
	}

	private void ParentAndResetSpawn(GameObject spawned, Transform anchor, Vector3 labelOffset)
	{
		if (!(spawned == null) && !(anchor == null))
		{
			spawned.transform.SetParent(anchor, worldPositionStays: false);
			spawned.transform.localPosition = Vector3.zero;
			spawned.transform.localRotation = Quaternion.identity;
			spawned.transform.localScale = Vector3.one;
			spawned.transform.localEulerAngles = Vector3.zero;
			Transform transform = spawned.transform.Find("BanTileLabel");
			if (transform != null)
			{
				transform.SetParent(spawned.transform, worldPositionStays: false);
				transform.localPosition = labelOffset;
				transform.localRotation = Quaternion.identity;
				transform.localScale = Vector3.one;
			}
		}
	}

	private void EnsureBanTileHint(GameObject spawnedTile, string hintText, Vector3 localOffset)
	{
		if (spawnedTile == null)
		{
			return;
		}
		Vector3 localPosition = new Vector3(117f, -1.5f, 0f);
		if (mBanTileLabelInstance != null)
		{
			UILabel component = mBanTileLabelInstance.GetComponent<UILabel>();
			if (component != null)
			{
				component.text = hintText;
			}
			mBanTileLabelInstance.transform.SetParent(spawnedTile.transform, worldPositionStays: false);
			mBanTileLabelInstance.transform.localPosition = localPosition;
			return;
		}
		if (BanTileLabelPrefab != null)
		{
			mBanTileLabelInstance = NGUITools.AddChild(spawnedTile, BanTileLabelPrefab);
			if (mBanTileLabelInstance != null)
			{
				mBanTileLabelInstance.transform.localPosition = localPosition;
			}
			UILabel componentInChildren = mBanTileLabelInstance.GetComponentInChildren<UILabel>();
			UILabel componentInChildren2 = spawnedTile.GetComponentInChildren<UILabel>();
			if (componentInChildren != null && componentInChildren2 != null)
			{
				componentInChildren.depth = componentInChildren2.depth + 1;
			}
			return;
		}
		GameObject gameObject = new GameObject("BanTileHint");
		gameObject.layer = spawnedTile.layer;
		gameObject.transform.SetParent(spawnedTile.transform, worldPositionStays: false);
		gameObject.transform.localPosition = localPosition;
		gameObject.transform.localRotation = Quaternion.identity;
		gameObject.transform.localScale = Vector3.one;
		UILabel uILabel = gameObject.AddComponent<UILabel>();
		if (BanWarningLabel != null)
		{
			uILabel.bitmapFont = BanWarningLabel.bitmapFont;
		}
		uILabel.fontSize = 20;
		uILabel.color = Color.yellow;
		uILabel.text = hintText;
		uILabel.alignment = NGUIText.Alignment.Center;
		UILabel componentInChildren3 = spawnedTile.GetComponentInChildren<UILabel>();
		if (componentInChildren3 != null)
		{
			uILabel.depth = componentInChildren3.depth + 1;
		}
		mBanTileLabelInstance = gameObject;
	}

	private void EnsureSinglePlayBlockedLabel()
	{
		if (Label_Play == null)
		{
			return;
		}
		Transform transform = ((Label_Play.transform.parent != null) ? Label_Play.transform.parent : Label_Play.transform);
		Transform transform2 = transform.Find("PlayBlockedLabel");
		if (transform2 == null)
		{
			Transform transform3 = base.transform.root.GetComponentsInChildren<Transform>(includeInactive: true).FirstOrDefault((Transform t) => t.name == "PlayBlockedLabel");
			if (transform3 != null)
			{
				transform2 = transform3;
			}
		}
		Transform[] array = (from t in base.transform.root.GetComponentsInChildren<Transform>(includeInactive: true)
			where t.name == "PlayBlockedLabel"
			select t).ToArray();
		foreach (Transform transform4 in array)
		{
			if (transform2 == null || transform4 != transform2)
			{
				NGUITools.Destroy(transform4.gameObject);
			}
		}
		if (transform2 != null)
		{
			transform2.SetParent(transform, worldPositionStays: false);
			transform2.localPosition = Label_Play.transform.localPosition + new Vector3(4.5f, 0f, 0f);
			transform2.localEulerAngles = Vector3.zero;
			transform2.localScale = Vector3.one;
			UILabel component = transform2.GetComponent<UILabel>();
			UILabel component2 = Label_Play.GetComponent<UILabel>();
			if (component != null && component2 != null)
			{
				component.depth = component2.depth + 1;
			}
			transform2.gameObject.SetActive(value: true);
			mButtonBlockedLabelInstance = transform2.gameObject;
			Label_Play.SetActive(value: false);
			return;
		}
		GameObject gameObject = new GameObject("PlayBlockedLabel");
		gameObject.layer = Label_Play.layer;
		gameObject.transform.SetParent(transform, worldPositionStays: false);
		gameObject.transform.localPosition = Label_Play.transform.localPosition + new Vector3(4.5f, 0f, 0f);
		gameObject.transform.localRotation = Quaternion.identity;
		gameObject.transform.localScale = Vector3.one;
		UILabel uILabel = gameObject.AddComponent<UILabel>();
		UILabel component3 = Label_Play.GetComponent<UILabel>();
		if (component3 != null)
		{
			uILabel.bitmapFont = component3.bitmapFont;
			uILabel.fontSize = Mathf.Max(16, component3.fontSize - 2);
			uILabel.depth = component3.depth + 1;
		}
		uILabel.color = Color.red;
		uILabel.text = "BLOCKED";
		uILabel.alignment = NGUIText.Alignment.Left;
		int siblingIndex = Label_Play.transform.GetSiblingIndex();
		gameObject.transform.SetSiblingIndex(siblingIndex + 1);
		mButtonBlockedLabelInstance = gameObject;
		Label_Play.SetActive(value: false);
	}

	private void ClearBanVisuals()
	{
		if (mSpawnedBanCreature != null)
		{
			NGUITools.Destroy(mSpawnedBanCreature);
			mSpawnedBanCreature = null;
		}
		if (mBanTileLabelInstance != null)
		{
			NGUITools.Destroy(mBanTileLabelInstance);
			mBanTileLabelInstance = null;
		}
		if (mButtonBlockedLabelInstance != null)
		{
			mButtonBlockedLabelInstance.SetActive(value: false);
		}
		Transform[] array = (from t in base.transform.root.GetComponentsInChildren<Transform>(includeInactive: true)
			where t.name == "PlayBlockedLabel"
			select t).ToArray();
		for (int num = 0; num < array.Length; num++)
		{
			array[num].gameObject.SetActive(value: false);
		}
		if (mStartButtonTexture != null)
		{
			mStartButtonTexture.color = mButtonOriginalColor;
		}
		if (Label_Play != null)
		{
			Label_Play.SetActive(value: true);
		}
		if (BanWarningLabel != null)
		{
			BanWarningLabel.text = string.Empty;
			BanWarningLabel.gameObject.SetActive(value: false);
		}
		if (BanWarningNode != null)
		{
			BanWarningNode.SetActive(value: false);
		}
		hasShownBanPopupThisOpen = false;
	}

	private void DisableStartButtonInteraction(bool disable)
	{
		if (Button_StartBattle == null)
		{
			ResolveLocalReferences();
		}
		if (!(Button_StartBattle == null))
		{
			UIButton component = Button_StartBattle.GetComponent<UIButton>();
			if (component != null)
			{
				component.isEnabled = !disable;
			}
			Button component2 = Button_StartBattle.GetComponent<Button>();
			if (component2 != null)
			{
				component2.interactable = !disable;
			}
			Collider component3 = Button_StartBattle.GetComponent<Collider>();
			if (component3 != null)
			{
				component3.enabled = !disable;
			}
		}
	}

	private void RefreshMyLeagueBadge(bool includeNameAndFlag)
	{
		if (PlayerBadge != null)
		{
			PlayerBadge.PopulateMyData();
			PlayerBadge.ShowNameAndFlag(includeNameAndFlag);
		}
	}

	private void RefreshOpponentLeagueBadge(bool includeNameAndFlag)
	{
		PvPGameStateData pvPData = Singleton<PlayerInfoScript>.Instance.PvPData;
		if (pvPData == null || OpponentBadge == null || pvPData == null || OpponentBadge == null || pvPData.OpponentPortraitData == null)
		{
			return;
		}
		if (pvPData.OpponentPortraitData.ID == "Facebook")
		{
			OpponentBadge.PopulateOtherPlayerData(pvPData.OpponentName, pvPData.OpponentPortrait, pvPData.OpponentLevel, pvPData.OpponentBestLevel, Singleton<CountryFlagManager>.Instance.TextureForCountryCode(Singleton<OnlinePvPManager>.Instance.GetOpponentCountryCode()));
		}
		else if (pvPData.OpponentPortraitData.ID == "Default")
		{
			if (pvPData.OpponentLoadout != null && pvPData.OpponentLoadout.Leader != null && pvPData.OpponentLoadout.Leader.SelectedSkin != null)
			{
				OpponentBadge.PopulateOtherPlayerData(pvPData.OpponentName, pvPData.OpponentLoadout.Leader.SelectedSkin.PortraitTexture, pvPData.OpponentLevel, pvPData.OpponentBestLevel, Singleton<CountryFlagManager>.Instance.TextureForCountryCode(Singleton<OnlinePvPManager>.Instance.GetOpponentCountryCode()));
			}
		}
		else if (pvPData.OpponentPortraitData.Texture != null)
		{
			OpponentBadge.PopulateOtherPlayerData(pvPData.OpponentName, pvPData.OpponentPortraitData.Texture, pvPData.OpponentLevel, pvPData.OpponentBestLevel, Singleton<CountryFlagManager>.Instance.TextureForCountryCode(Singleton<OnlinePvPManager>.Instance.GetOpponentCountryCode()));
		}
	}

	public void OnClickNextLoadout()
	{
		if (mMode == PvpMode.Extreme)
		{
			PvpExtremeTeam.CycleHero(1);
		}
		else
		{
			Singleton<PlayerInfoScript>.Instance.GoToNextLoadout();
		}
		RefreshMyLoadout();
	}

	public void OnClickPrevLoadout()
	{
		if (mMode == PvpMode.Extreme)
		{
			PvpExtremeTeam.CycleHero(-1);
		}
		else
		{
			Singleton<PlayerInfoScript>.Instance.GoToPrevLoadout();
		}
		RefreshMyLoadout();
	}

	public void OnClickPlay()
	{
		mSearching = false;
		Loadout loadout = MyLoadout();
		if (!MiscParams.PvpEnable)
		{
			Singleton<SimplePopupController>.Instance.ShowMessage(KFFLocalization.Get("!!GAME_ERROR_CONTACTING"), KFFLocalization.Get("!!PVP_UNDER_MAINTENANCE"), playErrorSound: true);
			return;
		}
		if (!loadout.IsUsable())
		{
			Singleton<SimplePopupController>.Instance.ShowMessage(string.Empty, KFFLocalization.Get("!!NEED_CREATURE"));
			Singleton<SLOTAudioManager>.Instance.PlayErrorSound();
			return;
		}
		if (mMode == PvpMode.Extreme && PvpExtremeTeam.Current == null)
		{
			Singleton<SimplePopupController>.Instance.ShowMessage(string.Empty, "Extreme matches are not available right now. Please try again later.");
			return;
		}
		if (mMode != PvpMode.Extreme && loadout.GetTeamCost() > Singleton<PlayerInfoScript>.Instance.RankData.TeamCost)
		{
			Singleton<SimplePopupController>.Instance.ShowMessage(string.Empty, KFFLocalization.Get("!!EXCEEDS_WEIGHT"));
			Singleton<SLOTAudioManager>.Instance.PlayErrorSound();
			return;
		}
		if (DetachedSingleton<StaminaManager>.Instance.GetStamina(StaminaType.Pvp) < MiscParams.PvpStaminaMatchCost)
		{
			Singleton<SimplePopupController>.Instance.ShowPurchasePrompt(KFFLocalization.Get("!!NO_PVP_STAMINA_BUY"), KFFLocalization.Get("!!NO_PVP_STAMINA_NOBUY"), MiscParams.StaminaRefillCost, RefillStaminaAndTryAgain);
			Singleton<SLOTAudioManager>.Instance.PlayErrorSound();
			return;
		}
		Singleton<PlayerInfoScript>.Instance.StateData.CurrentLoadout = loadout;
		if (mMode == PvpMode.Friend)
		{
			ReadyBlockingCollider.SetActive(value: true);
			ReadyTween.Play();
			mWaitingForOpponentStartData = true;
			Singleton<MultiplayerMessageHandler>.Instance.SendMatchStartData(MyLoadout());
		}
		else
		{
			mSearching = true;
			ShowConnectingTween.Play();
			Singleton<MultiplayerMessageHandler>.Instance.StartMatchmaking();
		}
	}

	public void OnConnectionComplete(bool amIPrimary)
	{
		Singleton<PlayerInfoScript>.Instance.PvPData.AmIPrimary = amIPrimary;
		mWaitingForOpponentStartData = true;
		mSearching = false;
		Singleton<MultiplayerMessageHandler>.Instance.SendMatchStartData(MyLoadout());
	}

	public void OnOpponentDataReceived()
	{
		mOpponentStartDataReceived = true;
		mSearching = false;
	}

	public void OnClickCancelConnect()
	{
		SafePlay(HideConnectingTween);
		mWaitingForOpponentStartData = false;
		mOpponentStartDataReceived = false;
		mSearching = false;
		Singleton<MultiplayerMessageHandler>.Instance.CancelMatchmaking();
	}

	private void LoadBattleScene()
	{
		Singleton<FrontEndPIPController>.Instance.UnloadModels(clearResource: false);
		Singleton<PlayerInfoScript>.Instance.StateData.SelectedHelper = null;
		UICamera.UnlockInput();
		Singleton<SLOTMusic>.Instance.StopMusic(0.5f);
		DetachedSingleton<SceneFlowManager>.Instance.LoadBattleScene();
	}

	private void RefillStaminaAndTryAgain()
	{
		mWaitForUserAction = true;
		mUserActionProceed = NextAction.WAITING;
		Singleton<BusyIconPanelController>.Instance.Show();
		Singleton<PlayerInfoScript>.Instance.SaveData.Purchase("stamina", string.Empty, 1, UserActionCallback);
		mNextFunction = StaminaRefillExecute;
	}

	public void UserActionCallback(PlayerSaveData.ActionResult result)
	{
		if (result.success)
		{
			mUserActionProceed = NextAction.PROCEED;
		}
		else
		{
			mUserActionProceed = NextAction.ERROR;
		}
	}

	private void StaminaRefillExecute()
	{
		DetachedSingleton<StaminaManager>.Instance.RefillStamina();
		Singleton<BuyStaminaPopupController>.Instance.Show(OnClickPlay);
	}

	public void OnClickEditDeck()
	{
		Singleton<EditDeckController>.Instance.Show(RefreshMyLoadout);
		Singleton<FrontEndPIPController>.Instance.HideModelPortrait();
	}

	public void OnClickMyLeader()
	{
		Loadout loadout = MyLoadout();
		Singleton<LeaderDetailsController>.Instance.Show(loadout.Leader, OnLeaderPopupClosed);
	}

	private void OnLeaderPopupClosed()
	{
		RefreshMyLoadout();
	}

	private void Update()
	{
		UpdateTimers();
		RefreshCurrency();
		if (mWaitingForOpponentStartData && mOpponentStartDataReceived && mMatchStartCountdown == -1f)
		{
			PvPGameStateData pvPData = Singleton<PlayerInfoScript>.Instance.PvPData;
			RefreshMyLeagueBadge(includeNameAndFlag: true);
			UICamera.LockInput();
			if (mMode == PvpMode.Friend)
			{
				SafePlay(FriendMatchStartingTween);
				if (SwordShieldAnimation != null)
				{
					SwordShieldAnimation.SetActive(value: true);
				}
				SafePlay(ShowFlagsAndCounterTween);
				if (ReadyBlockingCollider != null)
				{
					ReadyBlockingCollider.SetActive(value: false);
				}
				RefreshOpponentLeagueBadge(includeNameAndFlag: true);
			}
			else
			{
				SafePlay(OpponentFoundTween);
				if (SwordShieldAnimation != null)
				{
					SwordShieldAnimation.SetActive(value: true);
				}
				SafePlay(ShowFlagsAndCounterTween);
				RefreshOpponentLeagueBadge(includeNameAndFlag: true);
			}
			DetachedSingleton<StaminaManager>.Instance.ConsumeStamina(StaminaType.Pvp, MiscParams.PvpStaminaMatchCost);
			Singleton<MultiplayerMessageHandler>.Instance.OnStartBattle();
			if (Singleton<PlayerInfoScript>.Instance.PvPData.RankedMode)
			{
				Singleton<PlayerInfoScript>.Instance.SaveData.RankedPvpMatchStarted = true;
			}
			if (Singleton<PlayerInfoScript>.Instance.PvPData.ExtremeMode)
			{
				Singleton<PlayerInfoScript>.Instance.SaveData.ExtremePvpMatchStarted = true;
			}
			pvPData.HistoryEntryForThisMatch = BattleHistoryLocalSavesManager.Instance.SaveNewBattleHistory();
			Singleton<PlayerInfoScript>.Instance.Save();
			mMatchStartCountdown = CountdownTime;
			mLastCountdownVal = -1;
		}
		if (mMatchStartCountdown > 0f)
		{
			if (OpponentFoundTween == null || !OpponentFoundTween.AnyTweenPlaying())
			{
				mMatchStartCountdown -= Time.deltaTime;
			}
			if (mMatchStartCountdown < 0f)
			{
				BattleHistoryLocalSavesManager.Instance.CachedOpponentFlag = Singleton<OnlinePvPManager>.Instance.GetOpponentCountryCode();
				mMatchStartCountdown = -1f;
				LoadBattleScene();
			}
			int num = (int)Mathf.Ceil(Mathf.Max(0f, mMatchStartCountdown));
			if (Countdown != null)
			{
				Countdown.text = num.ToString();
			}
			if (num != mLastCountdownVal)
			{
				SafePlay(CountdownPulseTween);
				mLastCountdownVal = num;
				if (CountdownVFX != null)
				{
					CountdownVFX.Play();
				}
			}
		}
		if (!mWaitForUserAction)
		{
			return;
		}
		if (mUserActionProceed == NextAction.PROCEED)
		{
			Singleton<BusyIconPanelController>.Instance.Hide();
			mWaitForUserAction = false;
			mUserActionProceed = NextAction.NONE;
			if (mNextFunction != null)
			{
				mNextFunction();
			}
			mWaitForUserAction = false;
			mUserActionProceed = NextAction.NONE;
		}
		if (mUserActionProceed == NextAction.ERROR)
		{
			mWaitForUserAction = false;
			mUserActionProceed = NextAction.NONE;
			Singleton<BusyIconPanelController>.Instance.Hide();
			Singleton<SimplePopupController>.Instance.ShowMessage(string.Empty, KFFLocalization.Get("!!SERVER_ERROR_MESSAGE"), OnCloseServerAccessErrorPopup);
		}
	}

	private void RefreshCurrency()
	{
		if (SoftCurrencyLabel != null)
		{
			SoftCurrencyLabel.text = Singleton<PlayerInfoScript>.Instance.SaveData.SoftCurrency.ToString();
		}
		if (HardCurrencyLabel != null)
		{
			HardCurrencyLabel.text = Singleton<PlayerInfoScript>.Instance.SaveData.HardCurrency.ToString();
		}
	}

	private void OnCloseServerAccessErrorPopup()
	{
	}

	private void UpdateTimers()
	{
		if (matchFound)
		{
			syncTime += Time.deltaTime;
		}
		if (PlayerStamina != null && PlayerStamina.gameObject.activeInHierarchy)
		{
			DetachedSingleton<StaminaManager>.Instance.GetStaminaInfo(StaminaType.Pvp, out var _, out var maxStamina, out var secondsUntilNextStamina);
			if (PlayerStamina != null)
			{
				PlayerStamina.text = maxStamina.ToString();
			}
			if (PlayerStaminaTimer != null)
			{
				PlayerStaminaTimer.text = ((secondsUntilNextStamina <= 0) ? string.Empty : PlayerInfoScript.BuildTimerString(secondsUntilNextStamina));
			}
		}
	}

	public void OnBackClicked()
	{
		mBackButtonPressed = true;
		OnCloseClicked();
	}

	public void OnCloseClicked()
	{
		Singleton<MouseOrbitCamera>.Instance.CheckTiltCamSettingBeforeTutorial();
		if (mMode == PvpMode.Friend)
		{
			Singleton<SimplePopupController>.Instance.ShowPrompt(string.Empty, KFFLocalization.Get("!!CONFIRM_LEAVE_PVP_LOBBY"), OnConfirmClose, OnCancelClose);
		}
		else
		{
			OnConfirmClose();
		}
	}

	private void OnConfirmClose()
	{
		mActive = false;
		ClearBanVisuals();
		if (mMode == PvpMode.Friend)
		{
			Singleton<MultiplayerMessageHandler>.Instance.SendLeaveGame("leave");
		}
		Singleton<FrontEndPIPController>.Instance.HideModelPortrait();
		SafePlay(HideTween);
		if (!mBackButtonPressed)
		{
			Singleton<PvPModeSelectController>.Instance.Hide();
			return;
		}
		Singleton<PvPModeSelectController>.Instance.ShowElements();
		mBackButtonPressed = false;
		hasShownBanPopupThisOpen = false;
	}

	private void OnCancelClose()
	{
		if (ShowAllyTween != null)
		{
			ShowAllyTween.ReattachBackButtonTarget();
		}
	}

	public void OnFriendLeft()
	{
		if (mActive)
		{
			mActive = false;
			Singleton<SimplePopupController>.Instance.ShowMessage(string.Empty, KFFLocalization.Get("!!OTHER_PLAYER_LEFT"), OnConfirmPlayerLeft);
		}
	}

	private void OnConfirmPlayerLeft()
	{
		Singleton<FrontEndPIPController>.Instance.HideModelPortrait();
		SafePlay(HideTween);
		MenuStackManager.RemoveTopItemFromStack(removeTargetComponent: true);
		ClearBanVisuals();
	}

	public void Unload()
	{
		Singleton<PlayerInfoScript>.Instance.StateData.MultiplayerMode = false;
		foreach (GameObject mMySpawnedCreature in mMySpawnedCreatures)
		{
			if (mMySpawnedCreature != null)
			{
				NGUITools.Destroy(mMySpawnedCreature);
			}
		}
		mMySpawnedCreatures.Clear();
		ClearBanVisuals();
		if (mStartButtonTexture != null)
		{
			mStartButtonTexture.color = mButtonOriginalColor;
		}
	}

	private void OnApplicationPause(bool pauseStatus)
	{
		if ((!pauseStatus || !IsAndroidScreenRecordingActive()) && mSearching)
		{
			OnClickCancelConnect();
		}
	}

	private bool IsAndroidScreenRecordingActive()
	{
		return false;
	}

	public void DebugFriend()
	{
		SafePlay(FriendMatchStartingTween);
		if (SwordShieldAnimation != null)
		{
			SwordShieldAnimation.SetActive(value: true);
		}
		SafePlay(ShowFlagsAndCounterTween);
		if (ReadyBlockingCollider != null)
		{
			ReadyBlockingCollider.SetActive(value: false);
		}
		Debug.Log("[PVPPrep] DebugFriend invoked: FriendMatchStartingTween and flags shown.");
	}

	public void DebugMatch()
	{
		SafePlay(OpponentFoundTween);
		if (SwordShieldAnimation != null)
		{
			SwordShieldAnimation.SetActive(value: true);
		}
		Debug.Log("[PVPPrep] DebugMatch invoked: OpponentFoundTween played.");
	}

	private IEnumerator FetchPVPBanlist()
	{
		if (isFetchingBanlist)
		{
			Debug.Log("[PVPPrep] Already fetching banlist, skipping duplicate request");
			yield break;
		}
		isFetchingBanlist = true;
		Debug.Log("[PVPPrep] Fetching PVP banlist from server...");
		string text = SQSettings.SERVER_URL + "persist/static/pvp_banlist";
		Debug.Log("[PVPPrep] Banlist URL: " + text);
		using (UnityWebRequest request = UnityWebRequest.Get(text))
		{
			request.timeout = 5;
			yield return request.SendWebRequest();
			if (request.result == UnityWebRequest.Result.Success)
			{
				try
				{
					string text2 = request.downloadHandler.text;
					Debug.Log("[PVPPrep] Banlist response: " + text2);
					BanlistResponse banlistResponse = JsonUtility.FromJson<BanlistResponse>(text2);
					if (banlistResponse != null)
					{
						pvpBanlist = new List<string>(banlistResponse.banned_creatures ?? new string[0]);
						pvpLeaderBanlist = new List<string>(banlistResponse.banned_leaders ?? new string[0]);
						banlistFetched = true;
						Debug.Log($"[PVPPrep] Fetched {pvpBanlist.Count} banned creatures, {pvpLeaderBanlist.Count} banned leaders");
						if (banlistResponse.active_bans != null && banlistResponse.active_bans.Length != 0)
						{
							Debug.Log("[PVPPrep] Active ban periods:");
							ActiveBanInfo[] active_bans = banlistResponse.active_bans;
							foreach (ActiveBanInfo activeBanInfo in active_bans)
							{
								Debug.Log("  - " + activeBanInfo.name + " (expires " + activeBanInfo.expires + ")");
							}
						}
						CheckPvPBanlistInPrep();
					}
					else
					{
						Debug.LogWarning("[PVPPrep] Banlist response parsed as null");
						UseFallbackBanlist();
					}
				}
				catch (Exception ex)
				{
					Debug.LogError("[PVPPrep] Failed to parse banlist JSON: " + ex.Message);
					UseFallbackBanlist();
				}
			}
			else
			{
				Debug.LogWarning("[PVPPrep] Failed to fetch banlist: " + request.error + ". Using empty banlist.");
				UseFallbackBanlist();
			}
		}
		isFetchingBanlist = false;
	}

	private void UseFallbackBanlist()
	{
		pvpBanlist = new List<string>();
		pvpLeaderBanlist = new List<string>();
		banlistFetched = true;
		Debug.Log("[PVPPrep] Using empty banlist (fallback)");
	}
}
