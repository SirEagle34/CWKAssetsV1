using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Allies;
using UnityEngine;

public class BattleHistoryItemController : UIStreamingGridListItem
{
	private enum NextAction
	{
		NONE = 0,
		WAITING = 1,
		PROCEED = 2,
		ERROR = 3,
		RETRY = 4
	}

	private const string searchUrlPre = "/games/current/videos?client_id=7396cfa9e9b9c126e67bf9b8b4d13b85911767bd&videoHistoryTag=";

	public UILabel opponentNameLabel;

	public UILabel opponentMatchSeason;

	public UILabel opponentMatchResult;

	public UILabel opponentMatchDate;

	public GameObject AddFriendButton;

	public GameObject ViewReplayButton;

	public LeagueBadge OpponentBadge;

	public UITexture opponentFlag;

	public Transform[] opponentCreatureNodes;

	[SerializeField]
	private Color _DarkGreen;

	[SerializeField]
	private Color _DarkRed;

	private BattleHistory battleHistory;

	private List<InventoryTile> mSpawnedTiles = new List<InventoryTile>();

	private Dictionary<string, object> featuredVideo;

	private bool mWaitingForInvite;

	private ResponseFlag mAllyInviteStatus;

	private bool mWaitForUserAction;

	private NextAction mUserActionProceed;

	private PlayerSaveData.ProceedNextStep mNextFunction;

	public override void Populate(object dataObj)
	{
		battleHistory = dataObj as BattleHistory;
		if (battleHistory != null)
		{
			battleHistory.battleVideo = NormalizeReplayID(battleHistory.battleVideo);
		}
		opponentNameLabel.text = battleHistory.opponentName;
		PvpSeasonData pvpSeasonData = (string.IsNullOrEmpty(battleHistory.season) ? null : PvpSeasonDataManager.Instance.GetData(battleHistory.season));
		opponentMatchSeason.text = ((pvpSeasonData != null && !string.IsNullOrEmpty(pvpSeasonData.Name)) ? pvpSeasonData.Name : battleHistory.season);
		if (battleHistory.youWon)
		{
			opponentMatchResult.text = KFFLocalization.Get("!!VICTORY");
			opponentMatchResult.color = Color.green;
			opponentMatchResult.effectColor = _DarkGreen;
		}
		else
		{
			opponentMatchResult.text = KFFLocalization.Get("!!PVP_DEFEAT");
			opponentMatchResult.color = Color.red;
			opponentMatchResult.effectColor = _DarkRed;
		}
		if (opponentMatchResult.LabelShadow != null)
		{
			opponentMatchResult.LabelShadow.ShadowTextColor = opponentMatchResult.effectColor;
			opponentMatchResult.LabelShadow.ShadowEffectColor = opponentMatchResult.effectColor;
			opponentMatchResult.LabelShadow.RefreshShadowLabel();
		}
		uint num = DateTime.UtcNow.UnixTimestamp() - battleHistory.recordTime;
		uint num2 = num / 60;
		uint num3 = num2 / 60;
		uint num4 = num3 / 24;
		if (num4 != 0)
		{
			opponentMatchDate.text = KFFLocalization.Get("!!X_DAYS_AGO").Replace("<val1>", num4.ToString());
		}
		else if (num3 != 0)
		{
			opponentMatchDate.text = KFFLocalization.Get("!!X_HOURS_AGO").Replace("<val1>", num3.ToString());
		}
		else if (num2 != 0)
		{
			opponentMatchDate.text = KFFLocalization.Get("!!X_MINUTES_AGO").Replace("<val1>", num2.ToString());
		}
		else
		{
			opponentMatchDate.text = KFFLocalization.Get("!!X_SECONDS_AGO").Replace("<val1>", num.ToString());
		}
		foreach (InventoryTile mSpawnedTile in mSpawnedTiles)
		{
			NGUITools.Destroy(mSpawnedTile.gameObject);
		}
		mSpawnedTiles.Clear();
		opponentCreatureNodes = TeamRow.Fit(opponentCreatureNodes, battleHistory.creatures.Count);
		for (int i = 0; i < battleHistory.creatures.Count && i < opponentCreatureNodes.Length; i++)
		{
			CreatureData data = CreatureDataManager.Instance.GetData(battleHistory.creatures[i]);
			InventoryTile component = opponentCreatureNodes[i].InstantiateAsChild(Singleton<PrefabReferences>.Instance.InventoryTile).GetComponent<InventoryTile>();
			mSpawnedTiles.Add(component);
			component.PopulateAndForceDisplay(data);
		}
		if (!string.IsNullOrEmpty(battleHistory.opponentFBId) && Singleton<PlayerInfoScript>.Instance.IsFacebookLogin())
		{
			OpponentBadge.PopulateOtherPlayerData(battleHistory.opponentName, string.Empty, battleHistory.currentLeague, battleHistory.bestLeague, null);
		}
		else
		{
			LeaderData data2 = LeaderDataManager.Instance.GetData(battleHistory.opponentLeader);
			OpponentBadge.PopulateOtherPlayerData(battleHistory.opponentName, (data2 != null) ? data2.PortraitTexture : string.Empty, battleHistory.currentLeague, battleHistory.bestLeague, null);
		}
		RefreshInviteButton();
		EnsureViewReplayButtonReference();
		TryAutoLinkOrRepairReplayID();
		bool flag = !string.IsNullOrEmpty(battleHistory.battleVideo) && BattleReplayRecorder.ReplayFileExists(battleHistory.battleVideo);
		bool flag2 = false;
		if (UnityRecorderBattleRecorder.Instance != null)
		{
			flag2 = UnityRecorderBattleRecorder.Instance.VideoExists(battleHistory.battleVideo);
		}
		if (!flag2)
		{
			flag2 = VideoExistsOnDisk(battleHistory.battleVideo);
		}
		bool flag3 = flag || flag2;
		UnityEngine.Debug.Log(string.Format("[BattleHistoryItem] Checking replay for opponent: {0}, ReplayID: '{1}', Data: {2}, Video: {3}", battleHistory.opponentName, battleHistory.battleVideo ?? "null", flag, flag2));
		if (ViewReplayButton != null)
		{
			ViewReplayButton.SetActive(flag3);
			if (flag3)
			{
				UIButton[] componentsInChildren = ViewReplayButton.GetComponentsInChildren<UIButton>(includeInactive: true);
				for (int j = 0; j < componentsInChildren.Length; j++)
				{
					componentsInChildren[j].isEnabled = true;
				}
				Collider[] componentsInChildren2 = ViewReplayButton.GetComponentsInChildren<Collider>(includeInactive: true);
				for (int k = 0; k < componentsInChildren2.Length; k++)
				{
					componentsInChildren2[k].enabled = true;
				}
				UnityEngine.Debug.Log("[BattleHistoryItem] ViewReplayButton enabled: true");
				UIWidget[] componentsInChildren3 = ViewReplayButton.GetComponentsInChildren<UIWidget>(includeInactive: true);
				for (int l = 0; l < componentsInChildren3.Length; l++)
				{
					componentsInChildren3[l].alpha = 1f;
				}
			}
		}
		else
		{
			UnityEngine.Debug.LogWarning("[BattleHistoryItem] ViewReplayButton is null!");
		}
		SetNoReplayLabelVisible(!flag3);
	}

	private static string NormalizeReplayID(string replayID)
	{
		if (string.IsNullOrEmpty(replayID))
		{
			return replayID;
		}
		string text = replayID.Trim();
		if (text.StartsWith("Video_"))
		{
			text = text.Substring("Video_".Length);
		}
		return text;
	}

	private void TryAutoLinkOrRepairReplayID()
	{
		try
		{
			if (this.battleHistory == null)
			{
				return;
			}
			string text = NormalizeReplayID(this.battleHistory.battleVideo);
			this.battleHistory.battleVideo = text;
			bool flag = !string.IsNullOrEmpty(text) && BattleReplayRecorder.ReplayFileExists(text);
			bool flag2 = !string.IsNullOrEmpty(text) && VideoExistsOnDisk(text);
			if (!string.IsNullOrEmpty(text) && (flag || flag2))
			{
				return;
			}
			if (!string.IsNullOrEmpty(text) && !flag && !flag2)
			{
				UnityEngine.Debug.Log("[BattleHistoryItemController] Repairing invalid replayID: '" + text + "'");
			}
			else
			{
				UnityEngine.Debug.Log("[BattleHistoryItemController] Auto-linking missing replayID...");
			}
			if (Singleton<PlayerInfoScript>.Instance == null || Singleton<PlayerInfoScript>.Instance.SaveData == null)
			{
				return;
			}
			HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			List<BattleHistory> battleHistoryList = Singleton<PlayerInfoScript>.Instance.SaveData.BattleHistoryList;
			if (battleHistoryList != null)
			{
				for (int i = 0; i < battleHistoryList.Count; i++)
				{
					BattleHistory battleHistory = battleHistoryList[i];
					if (battleHistory != null && !string.IsNullOrEmpty(battleHistory.battleVideo))
					{
						hashSet.Add(NormalizeReplayID(battleHistory.battleVideo));
					}
				}
			}
			if (TryFindClosestUnlinkedReplayIDByTime(this.battleHistory.recordTime, hashSet, out var replayID) || TryFindMostRecentUnlinkedReplayID(hashSet, out replayID))
			{
				this.battleHistory.battleVideo = replayID;
				Singleton<PlayerInfoScript>.Instance.Save();
				UnityEngine.Debug.Log("[BattleHistoryItemController] Auto-linked missing replayID to: " + replayID);
			}
		}
		catch
		{
		}
	}

	private static bool TryFindClosestUnlinkedReplayIDByTime(uint recordTimeUnixSeconds, HashSet<string> usedReplayIDs, out string replayID)
	{
		replayID = null;
		try
		{
			string path = Path.Combine(Application.persistentDataPath, "ExportedReplays");
			if (!Directory.Exists(path))
			{
				return false;
			}
			string[] directories = Directory.GetDirectories(path, "Video_*", SearchOption.TopDirectoryOnly);
			if (directories == null || directories.Length == 0)
			{
				return false;
			}
			long num = recordTimeUnixSeconds;
			long num2 = long.MaxValue;
			string text = null;
			foreach (string text2 in directories)
			{
				string fileName = Path.GetFileName(text2);
				if (string.IsNullOrEmpty(fileName) || !fileName.StartsWith("Video_"))
				{
					continue;
				}
				string text3 = NormalizeReplayID(fileName);
				if (!string.IsNullOrEmpty(text3) && (usedReplayIDs == null || !usedReplayIDs.Contains(text3)) && TryGetBestVideoFilePathForFolder(text2, out var videoFilePath))
				{
					DateTime lastWriteTimeUtc;
					try
					{
						lastWriteTimeUtc = File.GetLastWriteTimeUtc(videoFilePath);
					}
					catch
					{
						continue;
					}
					long num3 = (long)(lastWriteTimeUtc - new DateTime(1970, 1, 1)).TotalSeconds;
					long num4 = ((num > 0) ? Math.Abs(num3 - num) : 0);
					if ((num <= 0 || num4 <= 604800) && num4 < num2)
					{
						num2 = num4;
						text = text3;
					}
				}
			}
			if (string.IsNullOrEmpty(text))
			{
				return false;
			}
			replayID = text;
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static bool TryFindMostRecentUnlinkedReplayID(HashSet<string> usedReplayIDs, out string replayID)
	{
		replayID = null;
		try
		{
			string path = Path.Combine(Application.persistentDataPath, "ExportedReplays");
			if (!Directory.Exists(path))
			{
				return false;
			}
			string[] directories = Directory.GetDirectories(path, "Video_*", SearchOption.TopDirectoryOnly);
			if (directories == null || directories.Length == 0)
			{
				return false;
			}
			DateTime dateTime = DateTime.MinValue;
			string text = null;
			foreach (string text2 in directories)
			{
				string fileName = Path.GetFileName(text2);
				if (string.IsNullOrEmpty(fileName) || !fileName.StartsWith("Video_"))
				{
					continue;
				}
				string text3 = NormalizeReplayID(fileName);
				if (!string.IsNullOrEmpty(text3) && (usedReplayIDs == null || !usedReplayIDs.Contains(text3)) && TryGetBestVideoFilePathForFolder(text2, out var videoFilePath))
				{
					DateTime lastWriteTimeUtc;
					try
					{
						lastWriteTimeUtc = File.GetLastWriteTimeUtc(videoFilePath);
					}
					catch
					{
						continue;
					}
					if (lastWriteTimeUtc > dateTime)
					{
						dateTime = lastWriteTimeUtc;
						text = text3;
					}
				}
			}
			if (string.IsNullOrEmpty(text))
			{
				return false;
			}
			replayID = text;
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static bool TryGetBestVideoFilePathForFolder(string videoFolderPath, out string videoFilePath)
	{
		videoFilePath = null;
		try
		{
			string[] array = new string[3]
			{
				Path.Combine(videoFolderPath, "replay_final.mp4"),
				Path.Combine(videoFolderPath, "final.mp4"),
				Path.Combine(videoFolderPath, "replay.mp4")
			};
			foreach (string text in array)
			{
				if (!File.Exists(text))
				{
					continue;
				}
				try
				{
					if (new FileInfo(text).Length > 1024)
					{
						videoFilePath = text;
						return true;
					}
				}
				catch
				{
				}
			}
		}
		catch
		{
		}
		return false;
	}

	public void RefreshReplayButton()
	{
		if (battleHistory == null)
		{
			return;
		}
		EnsureViewReplayButtonReference();
		bool flag = !string.IsNullOrEmpty(battleHistory.battleVideo) && BattleReplayRecorder.ReplayFileExists(battleHistory.battleVideo);
		bool flag2 = false;
		if (UnityRecorderBattleRecorder.Instance != null)
		{
			flag2 = UnityRecorderBattleRecorder.Instance.VideoExists(battleHistory.battleVideo);
		}
		if (!flag2)
		{
			flag2 = VideoExistsOnDisk(battleHistory.battleVideo);
		}
		bool flag3 = flag || flag2;
		UnityEngine.Debug.Log(string.Format("[BattleHistoryItem] RefreshReplayButton for {0}: ReplayID='{1}', Data={2}, Video={3}", battleHistory.opponentName, battleHistory.battleVideo ?? "null", flag, flag2));
		if (ViewReplayButton != null)
		{
			ViewReplayButton.SetActive(flag3);
			if (!flag3)
			{
				SetNoReplayLabelVisible(isVisible: true);
				return;
			}
			UIButton[] componentsInChildren = ViewReplayButton.GetComponentsInChildren<UIButton>(includeInactive: true);
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				componentsInChildren[i].isEnabled = flag3;
			}
			Collider[] componentsInChildren2 = ViewReplayButton.GetComponentsInChildren<Collider>(includeInactive: true);
			for (int j = 0; j < componentsInChildren2.Length; j++)
			{
				componentsInChildren2[j].enabled = flag3;
			}
			UIWidget[] componentsInChildren3 = ViewReplayButton.GetComponentsInChildren<UIWidget>(includeInactive: true);
			for (int k = 0; k < componentsInChildren3.Length; k++)
			{
				componentsInChildren3[k].alpha = (flag3 ? 1f : 0.5f);
			}
		}
		SetNoReplayLabelVisible(!flag3);
	}

	public void RefreshInviteButton()
	{
		bool flag = BattleHistoryController.Instance.alliesList.Find((AllyData m) => m.Id == battleHistory.opponentId) != null;
		bool flag2 = Singleton<PlayerInfoScript>.Instance.AlreadySentMatchHistoryInvite(battleHistory.opponentId);
		AddFriendButton.SetActive(!flag && !flag2);
		Transform transform = ((AddFriendButton.transform.parent != null) ? AddFriendButton.transform.parent.Find("Val_InviteSent") : null);
		if (transform != null)
		{
			transform.gameObject.SetActive(flag2 && !flag);
		}
	}

	public void AddFriend()
	{
		if (battleHistory != null)
		{
			Singleton<BusyIconPanelController>.Instance.Show();
			mWaitingForInvite = true;
			mAllyInviteStatus = ResponseFlag.None;
			Ally.AllyRequest(SessionManager.Instance.theSession, battleHistory.opponentId, AllyIniteToAUserCallBack);
		}
	}

	private void AllyIniteToAUserCallBack(ResponseFlag flag)
	{
		mAllyInviteStatus = flag;
	}

	private void Update()
	{
		if (mWaitingForInvite && mAllyInviteStatus != ResponseFlag.None)
		{
			mWaitingForInvite = false;
			Singleton<BusyIconPanelController>.Instance.Hide();
			if (mAllyInviteStatus == ResponseFlag.Success)
			{
				Singleton<SimplePopupController>.Instance.ShowMessage(string.Empty, KFFLocalization.Get("!!INVITE_SENT").Replace("<val1>", battleHistory.opponentName));
				Singleton<PlayerInfoScript>.Instance.OnSendMatchHistoryInvite(battleHistory.opponentId);
				BattleHistoryController.Instance.RefreshInviteButtons();
			}
			else if (mAllyInviteStatus == ResponseFlag.Duplicate)
			{
				Singleton<SimplePopupController>.Instance.ShowMessage(string.Empty, KFFLocalization.Get("!!INVITE_ALREADY_SENT"));
				Singleton<PlayerInfoScript>.Instance.OnSendMatchHistoryInvite(battleHistory.opponentId);
				BattleHistoryController.Instance.RefreshInviteButtons();
			}
			else if (mAllyInviteStatus == ResponseFlag.Exceedmyallylimit)
			{
				Singleton<SimplePopupController>.Instance.ShowPurchasePrompt(KFFLocalization.Get("!!ALLY_SLOTS_CONFIRM").Replace("<val2>", MiscParams.AllyBoxPerPurchase.ToString()), KFFLocalization.Get("!!ALLY_SLOTS_NOBUY"), MiscParams.AllyBoxPurchaseCost, OnClickConfirmAllySpace);
			}
			else if (mAllyInviteStatus == ResponseFlag.Exceeduserallylimit)
			{
				Singleton<SimplePopupController>.Instance.ShowMessage(string.Empty, KFFLocalization.Get("!!HELPER_INVITE_EXCEED_HIM"));
			}
			else
			{
				Singleton<SimplePopupController>.Instance.ShowMessage(string.Empty, KFFLocalization.Get("!!ERROR_SENDING_INVITE_SHORT").Replace("<val1>", battleHistory.opponentName));
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

	private void OnCloseServerAccessErrorPopup()
	{
	}

	private void OnClickConfirmAllySpace()
	{
		PlayerSaveData saveData = Singleton<PlayerInfoScript>.Instance.SaveData;
		mWaitForUserAction = true;
		mUserActionProceed = NextAction.WAITING;
		Singleton<BusyIconPanelController>.Instance.Show();
		saveData.Purchase("ally", string.Empty, 1, UserActionCallback);
		mNextFunction = AllySpaceExecute;
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

	private void AllySpaceExecute()
	{
		PlayerSaveData saveData = Singleton<PlayerInfoScript>.Instance.SaveData;
		saveData.AddEmptyAllySlots(MiscParams.AllyBoxPerPurchase);
		Singleton<PlayerInfoScript>.Instance.Save();
		Singleton<SimplePopupController>.Instance.ShowMessage(string.Empty, KFFLocalization.Get("!!ALLY_SLOTS_BOUGHT").Replace("<val1>", saveData.AllyBoxSpace.ToString()), OnCloseAllySpacePopup);
	}

	private void OnCloseAllySpacePopup()
	{
		AddFriend();
	}

	public void viewVideo()
	{
		if (battleHistory != null && !string.IsNullOrEmpty(battleHistory.battleVideo))
		{
			bool flag = BattleReplayRecorder.ReplayFileExists(battleHistory.battleVideo);
			bool flag2 = false;
			string text = null;
			if (UnityRecorderBattleRecorder.Instance != null)
			{
				flag2 = UnityRecorderBattleRecorder.Instance.VideoExists(battleHistory.battleVideo);
				text = UnityRecorderBattleRecorder.Instance.GetVideoPath(battleHistory.battleVideo);
			}
			if (!flag2)
			{
				flag2 = VideoExistsOnDisk(battleHistory.battleVideo);
				string folderPath;
				if (TryGetBestVideoFilePathOnDisk(battleHistory.battleVideo, out var videoFilePath))
				{
					text = videoFilePath;
				}
				else if (TryGetReplayVideoFolderOnDisk(battleHistory.battleVideo, out folderPath))
				{
					text = folderPath;
				}
			}
			if (flag2 && !string.IsNullOrEmpty(text))
			{
				if (File.Exists(text))
				{
					OpenVideo(text);
				}
				else if (Directory.Exists(text))
				{
					string text2 = Path.Combine(text, "replay_final.mp4");
					string text3 = Path.Combine(text, "final.mp4");
					string text4 = Path.Combine(text, "replay.mp4");
					if (File.Exists(text2))
					{
						OpenVideo(text2);
					}
					else if (File.Exists(text3))
					{
						OpenVideo(text3);
					}
					else if (File.Exists(text4))
					{
						OpenVideo(text4);
					}
					else
					{
						OpenFolder(text);
					}
				}
				else
				{
					UnityEngine.Debug.LogWarning("[BattleHistoryItemController] Video path not found: " + text);
				}
			}
			else if (flag)
			{
				string battleVideo = battleHistory.battleVideo;
				string text5 = null;
				if (UnityRecorderBattleRecorder.Instance != null)
				{
					text5 = UnityRecorderBattleRecorder.Instance.GetVideoFolder(battleVideo);
				}
				if (string.IsNullOrEmpty(text5))
				{
					text5 = Path.Combine(Path.Combine(Application.persistentDataPath, "ExportedReplays"), $"Video_{battleVideo}");
				}
				string text6 = Path.Combine(text5, "frames");
				string text7 = Path.Combine(text5, "replay_audio.wav");
				string outputPath = Path.Combine(text5, "replay_final.mp4");
				if (Directory.Exists(text6) && File.Exists(text7))
				{
					StartCoroutine(VideoAudioMerger.MergeFramesWithAudioCoroutine(text6, text7, outputPath, 30, delegate(bool success, string result)
					{
						if (success)
						{
							if (BattleHistoryController.Instance != null)
							{
								BattleHistoryController.Instance.RefreshAllReplayButtons();
							}
							OpenVideo(result);
						}
						else if (Singleton<SimplePopupController>.Instance != null)
						{
							Singleton<SimplePopupController>.Instance.ShowMessage(KFFLocalization.Get("!!ERROR"), "Replay exists but video merge failed.\n\n" + result);
						}
						else
						{
							UnityEngine.Debug.LogError("[BattleHistoryItemController] Repair merge failed: " + result);
						}
					}));
				}
				else if (Singleton<SimplePopupController>.Instance != null)
				{
					Singleton<SimplePopupController>.Instance.ShowMessage(KFFLocalization.Get("!!ERROR"), "Replay data is available, but no video was recorded for this battle.");
				}
			}
			else
			{
				UnityEngine.Debug.LogWarning("[BattleHistoryItemController] No replay/video found for replay ID: " + battleHistory.battleVideo);
				if (Singleton<SimplePopupController>.Instance != null)
				{
					Singleton<SimplePopupController>.Instance.ShowMessage(KFFLocalization.Get("!!ERROR"), "Replay not available for this battle.\n\nThe recording may still be processing or was not captured.");
				}
			}
		}
		else
		{
			UnityEngine.Debug.LogWarning("[BattleHistoryItemController] No replay ID available");
			if (Singleton<SimplePopupController>.Instance != null)
			{
				Singleton<SimplePopupController>.Instance.ShowMessage(KFFLocalization.Get("!!ERROR"), "No replay available for this battle");
			}
		}
	}

	private void OpenVideo(string videoPath)
	{
		if (string.IsNullOrEmpty(videoPath) || !File.Exists(videoPath))
		{
			UnityEngine.Debug.LogError("[BattleHistoryItemController] Video file not found: " + videoPath);
			return;
		}
		try
		{
			if (new FileInfo(videoPath).Length <= 0)
			{
				UnityEngine.Debug.LogError("[BattleHistoryItemController] Video file is empty (0 bytes): " + videoPath);
				string directoryName = Path.GetDirectoryName(videoPath);
				if (!string.IsNullOrEmpty(directoryName) && Directory.Exists(directoryName))
				{
					OpenFolder(directoryName);
				}
				return;
			}
		}
		catch
		{
		}
		string text = videoPath;
		try
		{
			text = Path.GetFullPath(videoPath);
		}
		catch
		{
		}
		text = text.Replace('/', '\\');
		UnityEngine.Debug.Log("[BattleHistoryItemController] Opening video: " + text);
		try
		{
			string arguments = "/select,\"" + text + "\"";
			Process.Start(new ProcessStartInfo
			{
				FileName = "explorer.exe",
				Arguments = arguments,
				UseShellExecute = true
			});
			StartCoroutine(OpenVideoAfterDelay(text, 0.25f));
		}
		catch (Exception ex)
		{
			UnityEngine.Debug.LogError("[BattleHistoryItemController] Error opening video: " + ex.Message);
			string directoryName2 = Path.GetDirectoryName(text);
			if (Directory.Exists(directoryName2))
			{
				OpenFolder(directoryName2);
			}
		}
	}

	private IEnumerator OpenVideoAfterDelay(string videoPath, float delaySeconds)
	{
		yield return new WaitForSeconds(delaySeconds);
		try
		{
			Process.Start(new ProcessStartInfo
			{
				FileName = videoPath,
				UseShellExecute = true
			});
		}
		catch (Exception ex)
		{
			UnityEngine.Debug.LogError("[BattleHistoryItemController] Error playing video: " + ex.Message);
		}
	}

	private void OpenFolder(string folderPath)
	{
		if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath))
		{
			UnityEngine.Debug.LogError("[BattleHistoryItemController] Folder not found: " + folderPath);
			return;
		}
		string text = folderPath;
		try
		{
			text = Path.GetFullPath(folderPath);
		}
		catch
		{
		}
		text = text.Replace('/', '\\');
		UnityEngine.Debug.Log("[BattleHistoryItemController] Opening folder: " + text);
		try
		{
			Process.Start(new ProcessStartInfo
			{
				FileName = "explorer.exe",
				Arguments = "\"" + text + "\"",
				UseShellExecute = true
			});
			if (Singleton<SimplePopupController>.Instance != null)
			{
				Singleton<SimplePopupController>.Instance.ShowMessage("Folder Opened", "Video folder opened:\n" + text);
			}
		}
		catch (Exception ex)
		{
			UnityEngine.Debug.LogError("[BattleHistoryItemController] Error opening folder: " + ex.Message);
		}
	}

	private void SetNoReplayLabelVisible(bool isVisible)
	{
		Transform transform = base.transform.Find("Val_NoReplay");
		if (transform == null)
		{
			transform = FindChildRecursive(base.transform, "Val_NoReplay");
		}
		if (transform != null)
		{
			transform.gameObject.SetActive(isVisible);
		}
	}

	private static bool TryGetReplayVideoFolderOnDisk(string replayID, out string folderPath)
	{
		folderPath = null;
		if (string.IsNullOrEmpty(replayID))
		{
			return false;
		}
		try
		{
			string path = Path.Combine(Application.persistentDataPath, "ExportedReplays");
			string path2 = $"Video_{replayID}";
			string text = Path.Combine(path, path2);
			if (Directory.Exists(text))
			{
				folderPath = text;
				return true;
			}
		}
		catch
		{
		}
		return false;
	}

	private static bool TryGetBestVideoFilePathOnDisk(string replayID, out string videoFilePath)
	{
		videoFilePath = null;
		if (!TryGetReplayVideoFolderOnDisk(replayID, out var folderPath))
		{
			try
			{
				string text = replayID;
				text = text.Replace('/', '_').Replace('\\', '_').Replace(':', '_');
				string text2 = Path.Combine("/storage/emulated/0/Movies/CardWarsKingdom", "replay_" + text + ".mp4");
				if (File.Exists(text2))
				{
					try
					{
						if (new FileInfo(text2).Length > 1024)
						{
							videoFilePath = text2;
							return true;
						}
					}
					catch
					{
					}
				}
			}
			catch
			{
			}
			return false;
		}
		string[] array = new string[3]
		{
			Path.Combine(folderPath, "replay_final.mp4"),
			Path.Combine(folderPath, "final.mp4"),
			Path.Combine(folderPath, "replay.mp4")
		};
		foreach (string text3 in array)
		{
			if (!File.Exists(text3))
			{
				continue;
			}
			try
			{
				if (new FileInfo(text3).Length > 1024)
				{
					videoFilePath = text3;
					return true;
				}
			}
			catch
			{
			}
		}
		return false;
	}

	private static bool VideoExistsOnDisk(string replayID)
	{
		string videoFilePath;
		return TryGetBestVideoFilePathOnDisk(replayID, out videoFilePath);
	}

	private void EnsureViewReplayButtonReference()
	{
		if (!(ViewReplayButton != null))
		{
			Transform transform = base.transform.Find("Button_ViewReplay");
			if (transform == null)
			{
				transform = FindChildRecursive(base.transform, "Button_ViewReplay");
			}
			if (transform != null)
			{
				ViewReplayButton = transform.gameObject;
			}
		}
	}

	private static Transform FindChildRecursive(Transform parent, string name)
	{
		for (int i = 0; i < parent.childCount; i++)
		{
			Transform child = parent.GetChild(i);
			if (child.name == name)
			{
				return child;
			}
			Transform transform = FindChildRecursive(child, name);
			if (transform != null)
			{
				return transform;
			}
		}
		return null;
	}
}
