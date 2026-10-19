using System;
using System.Collections;
using System.Collections.Generic;
using CodeStage.AntiCheat.ObscuredTypes;
using MiniJSON;
using UnityEngine;

public class MultiplayerMessageHandler : Singleton<MultiplayerMessageHandler>
{
	private enum MessageTypeEnum
	{
		MatchStartData = 0,
		CreaturePlay = 1,
		CardPlay = 2,
		DragAttack = 3,
		EndTurn = 4,
		MatchRequest = 5,
		AcceptMatchRequest = 6,
		DeclineMatchRequest = 7,
		AutoDeclineMatchRequest = 8,
		NotifyMatchRequestCanceled = 9,
		NotifyMatchRequestAcceptConfirmed = 10,
		PingSend = 11,
		PingReturn = 12,
		QuickChat = 13,
		Quitting = 14,
		Unset = 15
	}

	private enum JoinStatusEnum
	{
		NotJoined = 0,
		JoinedAsHost = 1,
		JoinedAsClient = 2
	}

	public enum MatchRequestRejectReason
	{
		Declined = 0,
		NotAvailable = 1,
		NotUnlocked = 2,
		Blocked = 3,
		Incompatible = 4,
		Timeout = 5
	}

	public enum InternetReachablility
	{
		Undefined = 0,
		Checking = 1,
		Reachable = 2,
		NotReachable = 3
	}

	public class ReceivedMatchRequest
	{
		public string PlayerName;

		public string PlayerID;
	}

	private class QueuedAction
	{
		public AIDecision Decision;

		public int TargetLaneIndex;

		public Dictionary<string, object> DebugStateDict;
	}

	private enum MatchMode
	{
		MM_UNDEFINED = 0,
		MM_GENERAL_HOST = 1,
		MM_GENERAL_CLIENT = 2,
		MM_ALLY_HOST = 3,
		MM_ALLY_CLIENT = 4
	}

	private const float TimeoutTime = 30f;

	private const bool mAllowCarrierDataNetwork = false;

	private const string mPingAddress = "google.com";

	private const float mWaitingTime = 2f;

	public float DisconnectDelay;

	public float JoinFriendGameDelay = 1f;

	private List<ReceivedMatchRequest> mReceivedMatchRequests = new List<ReceivedMatchRequest>();

	private List<QueuedAction> mQueuedActions = new List<QueuedAction>();

	private JoinStatusEnum mJoinStatus;

	private bool mUserLeft;

	private bool mOpponentLeft;

	private bool mInDisconnectDelay;

	private ReceivedMatchRequest mEnteringFriendMatch;

	private string mWaitingForMatchRequestResponseFrom;

	private string mWaitingForPingResponseFrom;

	private bool mFriendGameCreated;

	private bool mFriendJoinedGame;

	private bool mRetryJoiningFriendGame;

	private bool mSentFriendStartData;

	private bool mCancelingMatchmaking;

	private bool mRestartingMatchmaking;

	private float mRestartMatchmakingTimeout;

	private bool mClearingMatchRequests;

	private bool mNeedToDetectDisconnect;

	private bool mInReconnectTry;

	private int mRetryCount;

	private float mRetryTimeout;

	private bool mWaitingAck;

	private bool mHost;

	private bool mOpponentLeftError;

	private bool mInBattle;

	private bool mQuittingMatch;

	private bool mNeedToResponse;

	private Dictionary<string, object> mSaveJsonDict;

	private Ping mPing;

	private float mPingStartTime;

	private bool mWaitingPingResponse;

	private InternetReachablility mInternet;

	private DateTime mPaused = DateTime.Now;

	private DateTime mAllyMatchNow = DateTime.Now.AddYears(1);

	private bool mAllyMatchWatch;

	private bool mAllyMatchTimeout;

	private MatchMode mMatchMode;

	private bool mAcceptMatchResponseReceived;

	public bool InBattle => mInBattle;

	public bool Timeout => mAllyMatchTimeout;

	public string EnteringFriendsGame()
	{
		if (mEnteringFriendMatch != null)
		{
			return mEnteringFriendMatch.PlayerID;
		}
		return null;
	}

	private void OnApplicationPause(bool pauseStatus)
	{
		if (mInBattle)
		{
			if (pauseStatus)
			{
				mPaused = DateTime.Now;
			}
			else if ((DateTime.Now - mPaused).TotalMilliseconds > (double)MiscParams.PvPPauseAllowMilliseconds)
			{
				mInternet = InternetReachablility.Undefined;
				mOpponentLeft = false;
				mOpponentLeftError = false;
				OnQuitClicked();
			}
		}
	}

	private void KPISyncFail(string reason)
	{
		if (!DetachedSingleton<SceneFlowManager>.Instance.InBattleScene() && Singleton<PlayerInfoScript>.Instance.StateData.MultiplayerMode && Singleton<PVPPrepScreenController>.Instance.matchFound)
		{
			Singleton<PVPPrepScreenController>.Instance.syncTime = 0f;
			Singleton<PVPPrepScreenController>.Instance.matchFound = false;
		}
	}

	private void Update()
	{
		if (mRestartMatchmakingTimeout > 0f)
		{
			mRestartMatchmakingTimeout -= Time.deltaTime;
			if (mRestartMatchmakingTimeout <= 0f)
			{
				KPISyncFail("TimeOut");
				CancelMatchmaking(restarting: true);
				StartMatchmaking(restarting: true);
			}
		}
		if (mPing != null)
		{
			bool flag = true;
			if (mPing.isDone)
			{
				if (mPing.time >= 0)
				{
					InternetAvailable();
				}
				else
				{
					KPISyncFail("NoConnection");
					InternetIsNotAvailable();
				}
			}
			else if (Time.time - mPingStartTime < 2f)
			{
				flag = false;
			}
			else
			{
				KPISyncFail("NoConnection");
				InternetIsNotAvailable();
			}
			if (flag)
			{
				mPing = null;
			}
		}
		if (mInternet == InternetReachablility.NotReachable)
		{
			KPISyncFail("ServerNotReachable");
			mInternet = InternetReachablility.Undefined;
			mOpponentLeft = false;
			mOpponentLeftError = false;
			OnQuitClicked();
		}
	}

	public void ResetAllyMatchWatch()
	{
		mAllyMatchWatch = false;
		mAllyMatchTimeout = false;
		mMatchMode = MatchMode.MM_UNDEFINED;
	}

	private void InternetIsNotAvailable()
	{
		mInternet = InternetReachablility.NotReachable;
	}

	private void InternetAvailable()
	{
		mInternet = InternetReachablility.Reachable;
	}

	private void OnlineEventCallback(OnlinePvPManager.OnlinePvPEventCode eventCode, object obj)
	{
		switch (eventCode)
		{
		case OnlinePvPManager.OnlinePvPEventCode.CreatedNewGame:
			mRestartMatchmakingTimeout = -1f;
			if (mEnteringFriendMatch != null)
			{
				if (mMatchMode == MatchMode.MM_ALLY_CLIENT)
				{
					KPISyncFail("GameNotFound");
					mOpponentLeftError = true;
					OnQuitClicked();
				}
				else
				{
					mFriendGameCreated = true;
				}
			}
			break;
		case OnlinePvPManager.OnlinePvPEventCode.JoinedToExist:
			Singleton<BusyIconPanelController>.Instance.Hide();
			mHost = false;
			if (mEnteringFriendMatch != null)
			{
				if (mMatchMode == MatchMode.MM_ALLY_HOST)
				{
					KPISyncFail("GameNotFound");
					mOpponentLeftError = true;
					OnQuitClicked();
				}
				else
				{
					mFriendJoinedGame = true;
				}
			}
			else if (!mInReconnectTry)
			{
				OnMatchmakingComplete(amIPrimary: false);
			}
			if (mInReconnectTry)
			{
				mInReconnectTry = false;
				Singleton<DWGame>.Instance.ResumeMultiplayerTimer();
				if (mWaitingAck)
				{
					Singleton<OnlinePvPManager>.Instance.ReSendMessage();
				}
			}
			break;
		case OnlinePvPManager.OnlinePvPEventCode.NewPlayerJoined:
			Singleton<BusyIconPanelController>.Instance.Hide();
			mHost = true;
			if (mEnteringFriendMatch != null)
			{
				mFriendJoinedGame = true;
			}
			else if (!mInReconnectTry)
			{
				OnMatchmakingComplete(amIPrimary: true);
			}
			if (mInReconnectTry)
			{
				mInReconnectTry = false;
				Singleton<DWGame>.Instance.ResumeMultiplayerTimer();
				if (mWaitingAck)
				{
					Singleton<OnlinePvPManager>.Instance.ReSendMessage();
				}
			}
			break;
		case OnlinePvPManager.OnlinePvPEventCode.IncomingMessages:
		{
			TBPvPManager.IncomingGameMessageObject incomingGameMessageObject = obj as TBPvPManager.IncomingGameMessageObject;
			if (incomingGameMessageObject.Message != null && !(incomingGameMessageObject.Message == string.Empty))
			{
				ReceiveMessage(incomingGameMessageObject.Message);
			}
			break;
		}
		case OnlinePvPManager.OnlinePvPEventCode.SendSuccess:
			Singleton<BusyIconPanelController>.Instance.Hide();
			mWaitingAck = false;
			break;
		case OnlinePvPManager.OnlinePvPEventCode.SendTimeout:
			if (mNeedToDetectDisconnect && !mInReconnectTry)
			{
				mInReconnectTry = true;
				mRetryTimeout = ((!mHost) ? 1.4f : 0.4f);
				mRetryCount = ((!mHost) ? MiscParams.PvPReconnectRetryCountClient : MiscParams.PvPReconnectRetryCountHost);
				mRetryCount++;
				Singleton<BusyIconPanelController>.Instance.Show();
				if (mInBattle)
				{
					Singleton<DWGame>.Instance.StopMultiplayerTimer();
				}
				Singleton<OnlinePvPManager>.Instance.LeaveGame();
			}
			else if (mInternet != InternetReachablility.Undefined)
			{
				Singleton<BusyIconPanelController>.Instance.Show();
				NetworkReachabilityCheckStart();
			}
			break;
		case OnlinePvPManager.OnlinePvPEventCode.PlayerLeft:
			if (mQuittingMatch)
			{
				Singleton<OnlinePvPManager>.Instance.LeaveGame();
				mQuittingMatch = false;
			}
			else if (!mInBattle)
			{
				mOpponentLeftError = true;
				OnQuitClicked();
			}
			else if (mNeedToDetectDisconnect)
			{
				mInReconnectTry = true;
				mOpponentLeftError = true;
				mRetryTimeout = ((!mHost) ? 1.4f : 0.4f);
				mRetryCount = ((!mHost) ? MiscParams.PvPReconnectRetryCountClient : MiscParams.PvPReconnectRetryCountHost);
				mRetryCount++;
				Singleton<BusyIconPanelController>.Instance.Show();
				if (mInBattle)
				{
					Singleton<DWGame>.Instance.StopMultiplayerTimer();
				}
				Singleton<OnlinePvPManager>.Instance.LeaveGame();
			}
			else
			{
				ReceiveQuitMessage();
			}
			break;
		case OnlinePvPManager.OnlinePvPEventCode.NotFound:
			if (mNeedToDetectDisconnect && !mInReconnectTry)
			{
				mInReconnectTry = true;
				mRetryTimeout = ((!mHost) ? MiscParams.PvPReconnectTimeoutClient : 0.4f);
			}
			else
			{
				mRetryJoiningFriendGame = true;
			}
			break;
		case OnlinePvPManager.OnlinePvPEventCode.Error:
			if (mNeedToDetectDisconnect && !mInReconnectTry)
			{
				mInReconnectTry = true;
				mRetryTimeout = ((!mHost) ? 1.4f : 0.4f);
				mRetryCount = ((!mHost) ? MiscParams.PvPReconnectRetryCountClient : MiscParams.PvPReconnectRetryCountHost);
				mRetryCount++;
				Singleton<BusyIconPanelController>.Instance.Show();
				if (mInBattle)
				{
					Singleton<DWGame>.Instance.StopMultiplayerTimer();
				}
				Singleton<OnlinePvPManager>.Instance.LeaveGame();
			}
			else
			{
				Singleton<BusyIconPanelController>.Instance.Show();
				NetworkReachabilityCheckStart();
			}
			break;
		case OnlinePvPManager.OnlinePvPEventCode.Disabled:
			break;
		}
	}

	private void OnReconnectClicked()
	{
		mRetryTimeout = ((!mHost) ? MiscParams.PvPReconnectTimeoutClient : MiscParams.PvPReconnectTimeoutHost);
		Singleton<OnlinePvPManager>.Instance.RestartGame();
	}

	private void OnQuitClicked()
	{
		mWaitingAck = false;
		if (mOpponentLeftError)
		{
			mOpponentLeft = true;
		}
		else
		{
			mUserLeft = true;
		}
		Singleton<BusyIconPanelController>.Instance.Hide();
		if (mEnteringFriendMatch != null || mSentFriendStartData)
		{
			Singleton<PVPPrepScreenController>.Instance.OnFriendLeft();
		}
		mEnteringFriendMatch = null;
		mInternet = InternetReachablility.Undefined;
	}

	private void SendMessage(MessageTypeEnum messageType, Dictionary<string, object> jsonDict = null)
	{
		if (jsonDict == null)
		{
			jsonDict = new Dictionary<string, object>();
		}
		string message = SerializeDictionary(jsonDict, messageType);
		mWaitingAck = true;
		Singleton<BusyIconPanelController>.Instance.Show();
		Singleton<OnlinePvPManager>.Instance.SendMessage(message);
	}

	private void SendChatBasedMessage(MessageTypeEnum messageType, string playerName, string playerID, Dictionary<string, object> jsonDict = null)
	{
		if (jsonDict == null)
		{
			jsonDict = new Dictionary<string, object>();
		}
		jsonDict["SenderID"] = Singleton<PlayerInfoScript>.Instance.GetPlayerCode();
		jsonDict["SenderName"] = Singleton<PlayerInfoScript>.Instance.SaveData.MultiplayerPlayerName;
		string inviteData = SerializeDictionary(jsonDict, messageType);
		Singleton<ChatManager>.Instance.SendLine(string.Empty, playerName, playerID, null, null, inviteData);
	}

	private string SerializeDictionary(Dictionary<string, object> jsonDict, MessageTypeEnum addType = MessageTypeEnum.Unset)
	{
		string text = "{";
		if (addType != MessageTypeEnum.Unset)
		{
			string text2 = text;
			int num = (int)addType;
			text = text2 + "\"Type\":" + num + ",";
		}
		foreach (KeyValuePair<string, object> item in jsonDict)
		{
			string text3 = text;
			text = text3 + "\"" + item.Key + "\":" + SerializeObject(item.Value) + ",";
		}
		return text + "}";
	}

	private string SerializeList(List<object> jsonList)
	{
		string text = "[";
		foreach (object json in jsonList)
		{
			text = text + SerializeObject(json) + ",";
		}
		return text + "]";
	}

	private string SerializeObject(object jsonObject)
	{
		if (jsonObject is Dictionary<string, object>)
		{
			return SerializeDictionary(jsonObject as Dictionary<string, object>);
		}
		if (jsonObject is List<object>)
		{
			return SerializeList(jsonObject as List<object>);
		}
		if (jsonObject is string || jsonObject is ObscuredString)
		{
			string text = jsonObject.ToString();
			if (text.StartsWith("{"))
			{
				return text;
			}
			return "\"" + text + "\"";
		}
		if (jsonObject is bool)
		{
			if ((bool)jsonObject)
			{
				return "1";
			}
			return "0";
		}
		return jsonObject.ToString();
	}

	public void ReceiveMessage(string jsonString)
	{
		try
		{
			if (Json.Deserialize(jsonString) is Dictionary<string, object> dictionary && dictionary.ContainsKey("Type"))
			{
				int num = Convert.ToInt32(dictionary["Type"]);
				Debug.Log("[PvPDiag] recv opponent msg Type=" + num + " inBattle=" + mInBattle);
				switch (num)
				{
				case 0:
					ReceiveMatchStartData(dictionary);
					break;
				case 1:
					ReceiveCreaturePlay(dictionary);
					break;
				case 2:
					ReceiveCardPlay(dictionary);
					break;
				case 3:
					ReceiveDragAttack(dictionary);
					break;
				case 4:
					ReceiveEndTurn(dictionary);
					break;
				case 13:
					ReceiveQuickChat(dictionary);
					break;
				case 14:
					ReceiveQuitMessage();
					break;
				}
			}
		}
		catch (Exception ex)
		{
			Debug.LogWarning("[PvP] dropped an opponent message that failed to process (match continues): " + ex.Message);
		}
	}

	public void ReceiveChatBasedMessage(string jsonString)
	{
		if (string.IsNullOrEmpty(jsonString))
		{
			return;
		}
		Dictionary<string, object> dictionary = (Dictionary<string, object>)Json.Deserialize(jsonString);
		if (!(Convert.ToString(dictionary["SenderID"]) == Singleton<PlayerInfoScript>.Instance.GetPlayerCode()))
		{
			switch (Convert.ToInt32(dictionary["Type"]))
			{
			case 5:
				ReceiveMatchRequest(dictionary);
				break;
			case 6:
				ReceiveAcceptMatchRequest(dictionary);
				break;
			case 7:
				ReceiveDeclineMatchRequest(dictionary);
				break;
			case 9:
				ReceiveMatchRequestCanceled(dictionary);
				break;
			case 10:
				ReceiveMatchRequestAcceptConfirmed(dictionary);
				break;
			case 11:
				ReceivePingSend(dictionary);
				break;
			case 12:
				ReceivePingReturn(dictionary);
				break;
			case 8:
				break;
			}
		}
	}

	private void Reset()
	{
		mEnteringFriendMatch = null;
		mSentFriendStartData = false;
		mUserLeft = false;
		mOpponentLeft = false;
		mCancelingMatchmaking = false;
		mRestartingMatchmaking = false;
		mRestartMatchmakingTimeout = -1f;
		mNeedToDetectDisconnect = false;
		mRetryCount = -1;
		mInReconnectTry = false;
		mRetryTimeout = -1f;
		mWaitingAck = false;
		mOpponentLeftError = false;
		mInBattle = false;
		mQuittingMatch = false;
		mQueuedActions.Clear();
		ClearAllMatchRequests();
		ResetAllyMatchWatch();
	}

	public void StartMatchmaking(bool restarting = false)
	{
		StartCoroutine(StartMatchmakingCo(restarting));
	}

	private IEnumerator StartMatchmakingCo(bool restarting)
	{
		while (mInDisconnectDelay)
		{
			if (mCancelingMatchmaking && restarting)
			{
				yield break;
			}
			yield return null;
		}
		while (mQuittingMatch)
		{
			yield return null;
		}
		Reset();
		mRestartMatchmakingTimeout = 30f;
		mJoinStatus = JoinStatusEnum.NotJoined;
		bool rankedMode = Singleton<PlayerInfoScript>.Instance.PvPData.RankedMode;
		int range = ((!rankedMode) ? MiscParams.MultiplayerUnrankedSearchRange : MiscParams.MultiplayerRankedSearchRange);
		int range2nd = ((!rankedMode) ? MiscParams.MultiplayerUnrankedSearchRange2nd : MiscParams.MultiplayerRankedSearchRange2nd);
		int range3rd = ((!rankedMode) ? MiscParams.MultiplayerUnrankedSearchRange3rd : MiscParams.MultiplayerRankedSearchRange3rd);
		if (Singleton<PlayerInfoScript>.Instance.PvPData.ExtremeMode)
		{
			range = MiscParams.MultiplayerExtremeSearchRange;
			range2nd = MiscParams.MultiplayerExtremeSearchRange2nd;
			range3rd = MiscParams.MultiplayerExtremeSearchRange3rd;
		}
		Singleton<OnlinePvPManager>.Instance.SearchGame(Singleton<PlayerInfoScript>.Instance.SaveData.MultiplayerPlayerName, GetMyLevel(), range, range2nd, range3rd, rankedMode, OnlineEventCallback);
	}

	private int GetMyLevel()
	{
		if (Singleton<PlayerInfoScript>.Instance.PvPData.RankedMode || Singleton<PlayerInfoScript>.Instance.PvPData.ExtremeMode)
		{
			return Singleton<PlayerInfoScript>.Instance.SaveData.MultiplayerLevel;
		}
		return Singleton<PlayerInfoScript>.Instance.GetCurrentLoadout().GetTotalCreatureLevel();
	}

	public void CancelMatchmaking(bool restarting = false)
	{
		mRestartMatchmakingTimeout = -1f;
		StartCoroutine(StartDisconnectDelay());
		if (restarting)
		{
			mRestartingMatchmaking = true;
		}
		else
		{
			mCancelingMatchmaking = true;
		}
		Singleton<OnlinePvPManager>.Instance.LeaveGame();
	}

	private IEnumerator StartDisconnectDelay()
	{
		mInDisconnectDelay = true;
		yield return new WaitForSeconds(DisconnectDelay);
		mInDisconnectDelay = false;
		mCancelingMatchmaking = false;
	}

	public void CancelReconnect()
	{
		Singleton<OnlinePvPManager>.Instance.LeaveGame();
		StartCoroutine(StartCancelReconnectDelay());
	}

	private IEnumerator StartCancelReconnectDelay()
	{
		mInDisconnectDelay = true;
		yield return new WaitForSeconds(MiscParams.PvPLeaveDelay);
		mInDisconnectDelay = false;
	}

	public void ReConnectMatchmaking()
	{
		StartCoroutine(ReConnectMatchmakingCo());
	}

	private IEnumerator ReConnectMatchmakingCo()
	{
		while (mInDisconnectDelay)
		{
			yield return null;
		}
		OnReconnectClicked();
	}

	public void OnMatchmakingComplete(bool amIPrimary)
	{
		if (mJoinStatus == JoinStatusEnum.NotJoined && !mCancelingMatchmaking && !mRestartingMatchmaking)
		{
			if (Singleton<PlayerInfoScript>.Instance.StateData.MultiplayerMode)
			{
				Singleton<PVPPrepScreenController>.Instance.matchFound = true;
			}
			mRestartMatchmakingTimeout = 30f;
			mJoinStatus = (amIPrimary ? JoinStatusEnum.JoinedAsHost : JoinStatusEnum.JoinedAsClient);
			Singleton<PVPPrepScreenController>.Instance.OnConnectionComplete(amIPrimary);
		}
	}

	public void SendMatchRequestToPlayer(string playerName, string playerID)
	{
		Reset();
		mEnteringFriendMatch = new ReceivedMatchRequest();
		mEnteringFriendMatch.PlayerName = playerName;
		mEnteringFriendMatch.PlayerID = playerID;
		mWaitingForMatchRequestResponseFrom = playerID;
		Dictionary<string, object> dictionary = new Dictionary<string, object>();
		dictionary.Add("AppVersion", Singleton<TBPvPManager>.Instance.GetCompatibilityVersion(ranked: false));
		SendChatBasedMessage(MessageTypeEnum.MatchRequest, playerName, playerID, dictionary);
		mAllyMatchWatch = true;
		mAllyMatchTimeout = false;
		mAllyMatchNow = DateTime.Now;
	}

	public void SendPingToPlayer(string playerName, string playerID)
	{
		mEnteringFriendMatch = new ReceivedMatchRequest();
		mEnteringFriendMatch.PlayerName = playerName;
		mEnteringFriendMatch.PlayerID = playerID;
		mWaitingForPingResponseFrom = playerID;
		Dictionary<string, object> dictionary = new Dictionary<string, object>();
		dictionary.Add("Time", DateTime.Now.ToBinary());
		dictionary["SenderID"] = Singleton<PlayerInfoScript>.Instance.GetPlayerCode();
		dictionary["SenderName"] = Singleton<PlayerInfoScript>.Instance.SaveData.MultiplayerPlayerName;
		dictionary["receiverID"] = playerID;
		SendChatBasedMessage(MessageTypeEnum.PingSend, playerName, playerID, dictionary);
	}

	private void ReceivePingSend(Dictionary<string, object> jsonDict)
	{
		if (jsonDict != null)
		{
			DateTime.FromBinary(Convert.ToInt64(jsonDict["Time"]));
			Convert.ToString(jsonDict["SenderName"]);
			Convert.ToString(jsonDict["SenderID"]);
			if (Convert.ToString(jsonDict["receiverID"]) == Singleton<PlayerInfoScript>.Instance.GetPlayerCode())
			{
				mSaveJsonDict = jsonDict;
				mNeedToResponse = true;
			}
		}
	}

	private void SendPingResponse()
	{
		mNeedToResponse = false;
		string playerName = Convert.ToString(mSaveJsonDict["SenderName"]);
		string playerID = Convert.ToString(mSaveJsonDict["SenderID"]);
		mSaveJsonDict["receiverID"] = "received";
		SendChatBasedMessage(MessageTypeEnum.PingReturn, playerName, playerID, mSaveJsonDict);
	}

	private void ReceivePingReturn(Dictionary<string, object> jsonDict)
	{
		if (jsonDict != null)
		{
			DateTime dateTime = DateTime.FromBinary(Convert.ToInt64(jsonDict["Time"]));
			_ = DateTime.Now - dateTime;
		}
	}

	public void CancelMatchRequest()
	{
		mWaitingForMatchRequestResponseFrom = null;
		LeaveGame();
	}

	public void ReceiveMatchRequest(Dictionary<string, object> jsonDict)
	{
		string playerID = Convert.ToString(jsonDict["SenderID"]);
		if (!PVPMatchRequestController.ShowingRequestFromPlayer(playerID) && mReceivedMatchRequests.Find((ReceivedMatchRequest m) => m.PlayerID == playerID) == null)
		{
			ReceivedMatchRequest receivedMatchRequest = new ReceivedMatchRequest();
			receivedMatchRequest.PlayerID = playerID;
			receivedMatchRequest.PlayerName = Convert.ToString(jsonDict["SenderName"]);
			string compatibilityVersion = Singleton<TBPvPManager>.Instance.GetCompatibilityVersion(ranked: false);
			string text = Convert.ToString(jsonDict["AppVersion"]);
			if (Singleton<PlayerInfoScript>.Instance.SaveData.IgnoredPlayers.ContainsKey(playerID))
			{
				DeclineMatchRequest(receivedMatchRequest, MatchRequestRejectReason.Blocked);
				return;
			}
			if (compatibilityVersion != text)
			{
				DeclineMatchRequest(receivedMatchRequest, MatchRequestRejectReason.Incompatible);
				return;
			}
			mReceivedMatchRequests.Add(receivedMatchRequest);
			mAllyMatchWatch = true;
			mAllyMatchTimeout = false;
		}
	}

	public bool AnyMatchRequestsPending()
	{
		if (mClearingMatchRequests)
		{
			return false;
		}
		return mReceivedMatchRequests.Count > 0;
	}

	public ReceivedMatchRequest GetNextMatchRequest()
	{
		if (mClearingMatchRequests)
		{
			return null;
		}
		if (mReceivedMatchRequests.Count > 0)
		{
			ReceivedMatchRequest result = mReceivedMatchRequests[0];
			mReceivedMatchRequests.RemoveAt(0);
			return result;
		}
		return null;
	}

	public void ClearAllMatchRequests()
	{
		mReceivedMatchRequests.Clear();
	}

	public void AcceptMatchRequest(ReceivedMatchRequest request)
	{
		StartCoroutine(AcceptMatchRequestCo(request));
	}

	private IEnumerator AcceptMatchRequestCo(ReceivedMatchRequest request)
	{
		mClearingMatchRequests = true;
		foreach (ReceivedMatchRequest mReceivedMatchRequest in mReceivedMatchRequests)
		{
			DeclineMatchRequest(mReceivedMatchRequest, MatchRequestRejectReason.NotAvailable);
			yield return null;
		}
		mReceivedMatchRequests.Clear();
		mClearingMatchRequests = false;
		while (mQuittingMatch)
		{
			yield return null;
		}
		Reset();
		mEnteringFriendMatch = request;
		Singleton<BusyIconPanelController>.Instance.Show();
		float timeout = 30f;
		mAcceptMatchResponseReceived = false;
		SendChatBasedMessage(MessageTypeEnum.AcceptMatchRequest, request.PlayerName, request.PlayerID);
		while (!mAcceptMatchResponseReceived)
		{
			timeout -= Time.deltaTime;
			if (timeout <= 0f)
			{
				ReceiveMatchRequestCanceled(null);
				break;
			}
			yield return null;
		}
	}

	private void ReceiveAcceptMatchRequest(Dictionary<string, object> jsonDict)
	{
		string text = Convert.ToString(jsonDict["SenderID"]);
		string playerName = Convert.ToString(jsonDict["SenderName"]);
		if (text != mWaitingForMatchRequestResponseFrom)
		{
			SendChatBasedMessage(MessageTypeEnum.NotifyMatchRequestCanceled, playerName, text);
			return;
		}
		Singleton<TBPvPManager>.Instance.FriendMatch = true;
		StartCoroutine(OnMatchRequestAcceptedCo(jsonDict));
	}

	private IEnumerator OnMatchRequestAcceptedCo(Dictionary<string, object> jsonDict)
	{
		string playerID = Convert.ToString(jsonDict["SenderID"]);
		string playerName = Convert.ToString(jsonDict["SenderName"]);
		while (mQuittingMatch)
		{
			yield return null;
		}
		Singleton<PVPSendMatchRequestController>.Instance.OnJoinProcessStarted();
		UICamera.LockInput();
		mFriendGameCreated = false;
		mFriendJoinedGame = false;
		mRestartMatchmakingTimeout = 30f;
		mJoinStatus = JoinStatusEnum.NotJoined;
		mMatchMode = MatchMode.MM_ALLY_HOST;
		bool rankedMode = Singleton<PlayerInfoScript>.Instance.PvPData.RankedMode;
		int multiplayerUnrankedSearchRange = MiscParams.MultiplayerUnrankedSearchRange;
		int multiplayerUnrankedSearchRange2nd = MiscParams.MultiplayerUnrankedSearchRange2nd;
		int multiplayerUnrankedSearchRange3rd = MiscParams.MultiplayerUnrankedSearchRange3rd;
		Singleton<OnlinePvPManager>.Instance.SetFriendFilter(Singleton<PlayerInfoScript>.Instance.GetPlayerCode());
		Singleton<OnlinePvPManager>.Instance.SearchGame(Singleton<PlayerInfoScript>.Instance.GetPlayerCode(), GetMyLevel(), multiplayerUnrankedSearchRange, multiplayerUnrankedSearchRange2nd, multiplayerUnrankedSearchRange3rd, rankedMode, OnlineEventCallback);
		while (!mFriendGameCreated)
		{
			yield return null;
		}
		SendChatBasedMessage(MessageTypeEnum.NotifyMatchRequestAcceptConfirmed, playerName, playerID);
		while (!mFriendJoinedGame)
		{
			yield return null;
		}
		UICamera.UnlockInput();
		Singleton<PVPSendMatchRequestController>.Instance.OnMatchRequestAccepted();
	}

	private void ReceiveMatchRequestCanceled(Dictionary<string, object> jsonDict)
	{
		if (jsonDict != null)
		{
			string text = Convert.ToString(jsonDict["SenderID"]);
			if (mEnteringFriendMatch == null || mEnteringFriendMatch.PlayerID != text)
			{
				return;
			}
		}
		mEnteringFriendMatch = null;
		mAcceptMatchResponseReceived = true;
		Singleton<BusyIconPanelController>.Instance.Hide();
		Singleton<SimplePopupController>.Instance.ShowMessage(string.Empty, KFFLocalization.Get("!!MATCH_REQUEST_CANCELED"));
		ResetAllyMatchWatch();
	}

	private void ReceiveMatchRequestAcceptConfirmed(Dictionary<string, object> jsonDict)
	{
		Singleton<TBPvPManager>.Instance.FriendMatch = true;
		StartCoroutine(JoinInviteMatchCo());
	}

	private IEnumerator JoinInviteMatchCo()
	{
		mAcceptMatchResponseReceived = true;
		do
		{
			yield return new WaitForSeconds(JoinFriendGameDelay);
			mFriendJoinedGame = false;
			mRetryJoiningFriendGame = false;
			mMatchMode = MatchMode.MM_ALLY_CLIENT;
			bool rankedMode = Singleton<PlayerInfoScript>.Instance.PvPData.RankedMode;
			int multiplayerUnrankedSearchRange = MiscParams.MultiplayerUnrankedSearchRange;
			int multiplayerUnrankedSearchRange2nd = MiscParams.MultiplayerUnrankedSearchRange2nd;
			int multiplayerUnrankedSearchRange3rd = MiscParams.MultiplayerUnrankedSearchRange3rd;
			Singleton<OnlinePvPManager>.Instance.SetFriendFilter(mEnteringFriendMatch.PlayerID);
			Singleton<OnlinePvPManager>.Instance.SearchGame(mEnteringFriendMatch.PlayerID, GetMyLevel(), multiplayerUnrankedSearchRange, multiplayerUnrankedSearchRange2nd, multiplayerUnrankedSearchRange3rd, rankedMode, OnlineEventCallback);
			while (!mRetryJoiningFriendGame && !mFriendJoinedGame)
			{
				yield return null;
			}
		}
		while (!mFriendJoinedGame);
		Singleton<BusyIconPanelController>.Instance.Hide();
		Singleton<PlayerInfoScript>.Instance.PvPData.AmIPrimary = false;
		Singleton<PVPPrepScreenController>.Instance.Show(PvpMode.Friend, mEnteringFriendMatch.PlayerName);
	}

	public void DeclineMatchRequest(ReceivedMatchRequest request, MatchRequestRejectReason reason)
	{
		Dictionary<string, object> dictionary = new Dictionary<string, object>();
		dictionary.Add("Reason", (int)reason);
		SendChatBasedMessage(MessageTypeEnum.DeclineMatchRequest, request.PlayerName, request.PlayerID, dictionary);
		ResetAllyMatchWatch();
	}

	private void ReceiveDeclineMatchRequest(Dictionary<string, object> jsonDict)
	{
		MatchRequestRejectReason reason = (MatchRequestRejectReason)Convert.ToInt32(jsonDict["Reason"]);
		if (!(Convert.ToString(jsonDict["SenderID"]) != mWaitingForMatchRequestResponseFrom))
		{
			Singleton<PVPSendMatchRequestController>.Instance.OnMatchRequestDeclined(reason);
			ResetAllyMatchWatch();
		}
	}

	public void SendMatchStartData(Loadout loadout)
	{
		if (mEnteringFriendMatch != null)
		{
			mSentFriendStartData = true;
		}
		mEnteringFriendMatch = null;
		PvPGameStateData pvPData = Singleton<PlayerInfoScript>.Instance.PvPData;
		Dictionary<string, object> dictionary = new Dictionary<string, object>();
		dictionary.Add("Name", Singleton<PlayerInfoScript>.Instance.SaveData.MultiplayerPlayerName);
		dictionary.Add("ID", Singleton<PlayerInfoScript>.Instance.GetPlayerCode());
		dictionary.Add("Level", GetMyLevel());
		dictionary.Add("Card", Singleton<PlayerInfoScript>.Instance.SaveData.SelectedCardBack.ID);
		dictionary.Add("Portrait", Singleton<PlayerInfoScript>.Instance.SaveData.SelectedPortrait.ID);
		if (pvPData.RankedMode)
		{
			dictionary.Add("BestLevel", Singleton<PlayerInfoScript>.Instance.SaveData.BestMultiplayerLevel);
		}
		dictionary.Add("Leader", loadout.Leader.SelectedSkin.ID);
		if (Singleton<PlayerInfoScript>.Instance.IsFacebookLogin())
		{
			dictionary.Add("FBID", Singleton<KFFSocialManager>.Instance.FBUser.UserId);
		}
		InventorySlotItem helperCreature = Singleton<PlayerInfoScript>.Instance.StateData.HelperCreature;
		int num = MiscParams.CreaturesOnTeam - 1;
		List<object> list = new List<object>();
		string text = string.Empty;
		for (int i = 0; i < loadout.CreatureSet.Count; i++)
		{
			InventorySlotItem inventorySlotItem = loadout.CreatureSet[i];
			if (helperCreature != null && i == num)
			{
				inventorySlotItem = helperCreature;
			}
			if (inventorySlotItem != null && inventorySlotItem.Creature != null)
			{
				list.Add(inventorySlotItem.Creature.Serialize());
				text = text + inventorySlotItem.Creature.UniqueId + ",";
			}
		}
		dictionary.Add("Creatures", list);
		Debug.Log("[PvPDiag] SendMatchStartData sending creature uids=[" + text + "] helperPresent=" + (helperCreature != null));
		List<object> list2 = new List<object>();
		foreach (InventorySlotItem item in Singleton<PlayerInfoScript>.Instance.SaveData.FindAllExCards((CardItem card) => loadout.CreatureSet.Find((InventorySlotItem creature) => creature != null && creature.Creature.UniqueId == card.CreatureUID) != null))
		{
			list2.Add(item.Card.Serialize());
		}
		dictionary.Add("Cards", list2);
		if (pvPData.AmIPrimary)
		{
			pvPData.WonInitialCoinFlip = UnityEngine.Random.Range(0, 2) == 0;
			dictionary.Add("CoinFlip", !pvPData.WonInitialCoinFlip);
			dictionary.Add("Seed", KFFRandom.Seed);
		}
		SendMessage(MessageTypeEnum.MatchStartData, dictionary);
		ResetAllyMatchWatch();
	}

	private void ReceiveMatchStartData(Dictionary<string, object> jsonDict)
	{
		if (mCancelingMatchmaking || mRestartingMatchmaking)
		{
			return;
		}
		mRestartMatchmakingTimeout = -1f;
		PvPGameStateData pvPData = Singleton<PlayerInfoScript>.Instance.PvPData;
		pvPData.OpponentName = Convert.ToString(jsonDict["Name"]);
		pvPData.OpponentID = Convert.ToString(jsonDict["ID"]);
		pvPData.OpponentLevel = Convert.ToInt32(jsonDict["Level"]);
		pvPData.OpponentBestLevel = TFUtils.LoadInt(jsonDict, "BestLevel", -1);
		pvPData.OpponentCardBack = CardBackDataManager.Instance.GetData(TFUtils.LoadString(jsonDict, "Card", string.Empty));
		if (pvPData.OpponentCardBack == null)
		{
			pvPData.OpponentCardBack = CardBackDataManager.DefaultData;
		}
		pvPData.OpponentPortraitData = PlayerPortraitDataManager.Instance.GetData(TFUtils.LoadString(jsonDict, "Portrait", string.Empty));
		if (pvPData.OpponentPortraitData == null)
		{
			pvPData.OpponentPortraitData = PlayerPortraitDataManager.Instance.GetData("Default");
		}
		pvPData.OpponentFBID = null;
		pvPData.OpponentPortrait = null;
		bool flag = false;
		if (pvPData.OpponentPortraitData.ID == "Facebook")
		{
			if (jsonDict.TryGetValue("FBID", out var value))
			{
				pvPData.OpponentFBID = Convert.ToString(value);
			}
			if (0 == 0)
			{
				pvPData.OpponentPortraitData = PlayerPortraitDataManager.Instance.GetData("Default");
			}
		}
		pvPData.OpponentLoadout = new Loadout();
		LeaderData data = LeaderDataManager.Instance.GetData(Convert.ToString(jsonDict["Leader"]));
		if (data == null)
		{
			data = LeaderDataManager.Instance.GetData("Leader_Finn");
		}
		LeaderData leader = ((data.SkinParentLeader == null) ? data : data.SkinParentLeader);
		pvPData.OpponentLoadout.Leader = new LeaderItem(leader);
		pvPData.OpponentLoadout.Leader.SelectedSkin = data;
		foreach (object item2 in (List<object>)jsonDict["Creatures"])
		{
			InventorySlotItem item = new InventorySlotItem(new CreatureItem(item2 as Dictionary<string, object>, fromOtherPlayer: true));
			pvPData.OpponentLoadout.CreatureSet.Add(item);
		}
		foreach (object item3 in (List<object>)jsonDict["Cards"])
		{
			InventorySlotItem card = new InventorySlotItem(new CardItem(item3 as Dictionary<string, object>));
			InventorySlotItem inventorySlotItem = pvPData.OpponentLoadout.CreatureSet.Find((InventorySlotItem m) => m.Creature != null && m.Creature.UniqueId == card.Card.CreatureUID);
			if (inventorySlotItem != null && inventorySlotItem.Creature != null)
			{
				inventorySlotItem.Creature.ExCards[card.Card.CreatureSlot] = card;
			}
		}
		if (!pvPData.AmIPrimary)
		{
			pvPData.WonInitialCoinFlip = Convert.ToInt32(jsonDict["CoinFlip"]) == 1;
			KFFRandom.Seed = Convert.ToInt32(jsonDict["Seed"]);
		}
		if (!flag)
		{
			Singleton<PVPPrepScreenController>.Instance.OnOpponentDataReceived();
		}
		ResetAllyMatchWatch();
	}

	public void OnStartBattle()
	{
		_ = Singleton<PlayerInfoScript>.Instance.PvPData.AmIPrimary;
		mSentFriendStartData = false;
		mInBattle = true;
	}

	public AIDecision GetNextOpponentMove()
	{
		if (mQueuedActions.Count > 0)
		{
			QueuedAction queuedAction = mQueuedActions[0];
			mQueuedActions.RemoveAt(0);
			Debug.Log("[PvPDiag] GetNextOpponentMove -> returning action (isDeploy=" + queuedAction.Decision.IsDeploy + "), remaining=" + mQueuedActions.Count);
			if (queuedAction.DebugStateDict != null)
			{
				foreach (string item in FindGameStateMismatches(queuedAction.DebugStateDict))
				{
					_ = item;
				}
			}
			Singleton<DWGame>.Instance.SetTargetByLane(PlayerType.Opponent, queuedAction.TargetLaneIndex);
			return queuedAction.Decision;
		}
		return null;
	}

	public void SendCreaturePlay(CreatureItem creature, int laneIndex)
	{
		Debug.Log("[PvPDiag] SendCreaturePlay deploying uid=" + creature.UniqueId + " lane=" + laneIndex);
		Dictionary<string, object> dictionary = new Dictionary<string, object>();
		dictionary.Add("Seed", KFFRandom.Seed);
		dictionary.Add("ID", creature.UniqueId);
		dictionary.Add("Lane", laneIndex);
		SendMessage(MessageTypeEnum.CreaturePlay, dictionary);
	}

	private void ReceiveCreaturePlay(Dictionary<string, object> jsonDict)
	{
		PvPGameStateData pvPData = Singleton<PlayerInfoScript>.Instance.PvPData;
		QueuedAction queuedAction = new QueuedAction();
		queuedAction.Decision = new AIDecision();
		queuedAction.Decision.Seed = Convert.ToInt32(jsonDict["Seed"]);
		queuedAction.Decision.IsDeploy = true;
		int creatureID = Convert.ToInt32(jsonDict["ID"]);
		InventorySlotItem inventorySlotItem = pvPData.OpponentLoadout.CreatureSet.Find((InventorySlotItem m) => m.Creature != null && m.Creature.UniqueId == creatureID);
		if (inventorySlotItem == null || inventorySlotItem.Creature == null)
		{
			string text = string.Empty;
			foreach (InventorySlotItem item in pvPData.OpponentLoadout.CreatureSet)
			{
				if (item != null && item.Creature != null)
				{
					text = text + item.Creature.UniqueId + ",";
				}
			}
			Debug.LogWarning("[PvPDiag] deploy wants uid=" + creatureID + " but synced opponent loadout has [" + text + "] -- skipping (soft-lock cause).");
		}
		else
		{
			queuedAction.Decision.Creature = inventorySlotItem.Creature;
			queuedAction.Decision.LaneIndex1 = Convert.ToInt32(jsonDict["Lane"]);
			if (jsonDict.TryGetValue("DebugStateCheck", out var value))
			{
				queuedAction.DebugStateDict = (Dictionary<string, object>)value;
			}
			mQueuedActions.Add(queuedAction);
			Debug.Log("[PvPDiag] queued opponent DEPLOY uid=" + creatureID + " lane=" + queuedAction.Decision.LaneIndex1 + " queueLen=" + mQueuedActions.Count);
		}
	}

	public void SendCardPlay(CardData card, PlayerType player, int laneIndex1, int targetLane, string shapeShiftPick = null)
	{
		Dictionary<string, object> dictionary = new Dictionary<string, object>();
		dictionary.Add("Seed", KFFRandom.Seed);
		dictionary.Add("ID", card.ID);
		dictionary.Add("Player", player.IntValue);
		if (laneIndex1 != -1)
		{
			dictionary.Add("Lane1", laneIndex1);
		}
		if (targetLane != -1)
		{
			dictionary.Add("Target", targetLane);
		}
		if (shapeShiftPick != null)
		{
			dictionary.Add("Pick", shapeShiftPick);
		}
		SendMessage(MessageTypeEnum.CardPlay, dictionary);
	}

	private void ReceiveCardPlay(Dictionary<string, object> jsonDict)
	{
		QueuedAction queuedAction = new QueuedAction();
		queuedAction.Decision = new AIDecision();
		queuedAction.Decision.Seed = Convert.ToInt32(jsonDict["Seed"]);
		queuedAction.Decision.Card = CardDataManager.Instance.GetData(Convert.ToString(jsonDict["ID"]));
		queuedAction.Decision.TargetPlayer = ((Convert.ToInt32(jsonDict["Player"]) != 0) ? PlayerType.User : PlayerType.Opponent);
		queuedAction.Decision.LaneIndex1 = TFUtils.LoadInt(jsonDict, "Lane1", -1);
		queuedAction.TargetLaneIndex = TFUtils.LoadInt(jsonDict, "Target", -1);
		queuedAction.Decision.ShapeShiftPick = TFUtils.LoadString(jsonDict, "Pick", null);
		if (jsonDict.TryGetValue("DebugStateCheck", out var value))
		{
			queuedAction.DebugStateDict = (Dictionary<string, object>)value;
		}
		mQueuedActions.Add(queuedAction);
	}

	public void SendDragAttack(int laneIndex1, int laneIndex2)
	{
		Dictionary<string, object> dictionary = new Dictionary<string, object>();
		dictionary.Add("Seed", KFFRandom.Seed);
		dictionary.Add("Lane1", laneIndex1);
		dictionary.Add("Lane2", laneIndex2);
		SendMessage(MessageTypeEnum.DragAttack, dictionary);
	}

	private void ReceiveDragAttack(Dictionary<string, object> jsonDict)
	{
		QueuedAction queuedAction = new QueuedAction();
		queuedAction.Decision = new AIDecision();
		queuedAction.Decision.Seed = Convert.ToInt32(jsonDict["Seed"]);
		queuedAction.Decision.IsAttack = true;
		queuedAction.Decision.LaneIndex1 = Convert.ToInt32(jsonDict["Lane1"]);
		queuedAction.Decision.LaneIndex2 = Convert.ToInt32(jsonDict["Lane2"]);
		if (jsonDict.TryGetValue("DebugStateCheck", out var value))
		{
			queuedAction.DebugStateDict = (Dictionary<string, object>)value;
		}
		mQueuedActions.Add(queuedAction);
	}

	public void SendEndTurn()
	{
		SendMessage(MessageTypeEnum.EndTurn);
	}

	private void ReceiveEndTurn(Dictionary<string, object> jsonDict)
	{
		QueuedAction queuedAction = new QueuedAction();
		queuedAction.Decision = new AIDecision();
		queuedAction.Decision.EndTurn = true;
		mQueuedActions.Add(queuedAction);
	}

	public void SendLeaveGame(string reason)
	{
		mNeedToDetectDisconnect = false;
		if (reason == "leave")
		{
			if (mInBattle)
			{
				mQuittingMatch = true;
				SendMessage(MessageTypeEnum.Quitting);
			}
			else
			{
				LeaveGame();
			}
		}
		mInBattle = false;
	}

	private void ReceiveLeaveGame()
	{
		EndGameAtDisconnect();
		Singleton<SimplePopupController>.Instance.ShowMessage(string.Empty, KFFLocalization.Get("!!OPPONENT_LEFT"), ConfirmOpponentLeft, SimplePopupController.PopupPriority.PvpError);
	}

	private void ConfirmOpponentLeft()
	{
		if (Singleton<PlayerInfoScript>.Instance.StateData.MultiplayerMode)
		{
			Singleton<DWGame>.Instance.turnNumber = 0;
			Singleton<DWGame>.Instance.battleDuration = 0f;
		}
		Singleton<DWGameMessageHandler>.Instance.RegisterPlayerWin();
		Singleton<SLOTMusic>.Instance.PlayVictoryMusic();
		Singleton<DWGame>.Instance.SetGameState(GameState.P2Defeated);
		LeaveGame();
	}

	private void ReceiveDisconnect()
	{
		EndGameAtDisconnect();
		Singleton<SimplePopupController>.Instance.ShowMessage(string.Empty, KFFLocalization.Get("!!PVP_CONNECTION_LOST"), ConfirmConnectionLost, SimplePopupController.PopupPriority.PvpError);
	}

	private void ConfirmConnectionLost()
	{
		if (Singleton<PlayerInfoScript>.Instance.StateData.MultiplayerMode)
		{
			Singleton<DWGame>.Instance.turnNumber = 0;
			Singleton<DWGame>.Instance.battleDuration = 0f;
		}
		Singleton<DWGameMessageHandler>.Instance.RegisterPlayerLoss();
		Singleton<SLOTMusic>.Instance.PlayLoserMusic();
		Singleton<DWGame>.Instance.SetGameState(GameState.P1Defeated);
		LeaveGame();
	}

	private void EndGameAtDisconnect()
	{
		Singleton<BusyIconPanelController>.Instance.Hide();
		Singleton<BattleHudController>.Instance.EnemyThinkingObject.SetActive(value: false);
		Singleton<CreatureInfoPopup>.Instance.HideIfShowing();
		Singleton<HandCardController>.Instance.CancelCardDrag();
		Singleton<DWBattleLane>.Instance.CancelDragAttack();
		Singleton<HandCardController>.Instance.UnzoomCard();
		Singleton<BattleHudController>.Instance.CloseLeaderPopupIfShowing();
		Singleton<DWBattleLane>.Instance.EndFreeCam();
		Singleton<PauseController>.Instance.HideIfShowing();
		Singleton<PauseController>.Instance.HideButton();
		Singleton<QuickMessageController>.Instance.HideIfShowing();
		Singleton<QuickMessageController>.Instance.HideButton();
		Singleton<DWGame>.Instance.StopMultiplayerTimer();
		Singleton<BattleHudController>.Instance.ClearPvpTimer(timeUp: false);
		mInBattle = false;
		ResetAllyMatchWatch();
	}

	public void LeaveGame()
	{
		Singleton<BusyIconPanelController>.Instance.Hide();
		Singleton<OnlinePvPManager>.Instance.LeaveGame();
		mWaitingAck = false;
		mInReconnectTry = false;
		mRetryTimeout = -1f;
		mInBattle = false;
		mEnteringFriendMatch = null;
		ResetAllyMatchWatch();
	}

	public bool CheckPlayerLeft()
	{
		if ((mOpponentLeft || mUserLeft) && LiveChannel.MaintenanceStarting(10.0))
		{
			return false;
		}
		if (mOpponentLeft)
		{
			mOpponentLeft = false;
			ReceiveLeaveGame();
			return true;
		}
		if (mUserLeft)
		{
			mUserLeft = false;
			ReceiveDisconnect();
			return true;
		}
		return false;
	}

	public void SendQuickChat(QuickChatData data)
	{
		Dictionary<string, object> dictionary = new Dictionary<string, object>();
		dictionary.Add("ID", data.ID);
		SendMessage(MessageTypeEnum.QuickChat, dictionary);
	}

	private void ReceiveQuickChat(Dictionary<string, object> jsonDict)
	{
		string iD = Convert.ToString(jsonDict["ID"]);
		QuickChatData data = QuickChatDataManager.Instance.GetData(iD);
		if (data != null)
		{
			Singleton<QuickMessageController>.Instance.ShowOpponentChatMessage(data);
		}
	}

	private void ReceiveQuitMessage()
	{
		mOpponentLeft = true;
		LeaveGame();
	}

	private Dictionary<string, object> BuildGameStateDict()
	{
		BoardState currentBoardState = Singleton<DWGame>.Instance.CurrentBoardState;
		return new Dictionary<string, object>
		{
			["MyHand"] = BuildCardList(currentBoardState.GetPlayerState(PlayerType.User).Hand),
			["MyCreatureHand"] = BuildCardList(currentBoardState.GetPlayerState(PlayerType.User).DeploymentList),
			["YourHand"] = BuildCardList(currentBoardState.GetPlayerState(PlayerType.Opponent).Hand),
			["YourCreatureHand"] = BuildCardList(currentBoardState.GetPlayerState(PlayerType.Opponent).DeploymentList),
			["MyCreatures"] = BuildCreaturesDict(currentBoardState.GetCreatures(PlayerType.User)),
			["YourCreatures"] = BuildCreaturesDict(currentBoardState.GetCreatures(PlayerType.Opponent))
		};
	}

	private List<object> BuildCardList(List<CardData> cards)
	{
		List<object> list = new List<object>();
		foreach (CardData card in cards)
		{
			list.Add(card.ID);
		}
		return list;
	}

	private List<object> BuildCardList(List<CreatureState> creatures)
	{
		List<object> list = new List<object>();
		foreach (CreatureState creature in creatures)
		{
			list.Add(creature.Data.Form.ID);
		}
		return list;
	}

	private Dictionary<string, object> BuildCreaturesDict(List<CreatureState> creatures)
	{
		Dictionary<string, object> dictionary = new Dictionary<string, object>();
		foreach (CreatureState creature in creatures)
		{
			Dictionary<string, object> dictionary2 = new Dictionary<string, object>();
			dictionary[creature.Data.Form.ID] = dictionary2;
			dictionary2["Health"] = creature.HP;
			object obj = (dictionary2["Statuses"] = new Dictionary<string, object>());
			Dictionary<string, object> dictionary4 = (Dictionary<string, object>)obj;
			foreach (StatusState statusEffect in creature.StatusEffects)
			{
				dictionary4[statusEffect.Data.ID] = statusEffect.Data.GetValueString(creature, verbose: false);
			}
		}
		return dictionary;
	}

	private List<string> FindGameStateMismatches(Dictionary<string, object> jsonDict)
	{
		BoardState currentBoardState = Singleton<DWGame>.Instance.CurrentBoardState;
		List<string> list = new List<string>();
		List<object> list2 = (List<object>)jsonDict["MyHand"];
		foreach (object item in list2)
		{
			string cardID4 = item as string;
			if (currentBoardState.GetPlayerState(PlayerType.Opponent).Hand.Find((CardData m) => m.ID == cardID4) == null)
			{
				list.Add("opponent says he has card " + cardID4 + " but I say he doesn't");
			}
		}
		foreach (CardData item2 in currentBoardState.GetPlayerState(PlayerType.Opponent).Hand)
		{
			if (!list2.Contains(item2.ID))
			{
				list.Add("I say opponent has card " + item2.ID + " but opponent says he doesn't");
			}
		}
		List<object> list3 = (List<object>)jsonDict["MyCreatureHand"];
		foreach (object item3 in list3)
		{
			string cardID5 = item3 as string;
			if (currentBoardState.GetPlayerState(PlayerType.Opponent).DeploymentList.Find((CreatureState m) => m.Data.Form.ID == cardID5) == null)
			{
				list.Add("opponent says he has creature card " + cardID5 + " but I say he doesn't");
			}
		}
		foreach (CreatureState deployment in currentBoardState.GetPlayerState(PlayerType.Opponent).DeploymentList)
		{
			if (!list3.Contains(deployment.Data.Form.ID))
			{
				list.Add("I say opponent has creature card " + deployment.Data.Form.ID + " but opponent says he doesn't");
			}
		}
		List<object> list4 = (List<object>)jsonDict["YourHand"];
		foreach (object item4 in list4)
		{
			string cardID6 = item4 as string;
			if (currentBoardState.GetPlayerState(PlayerType.User).Hand.Find((CardData m) => m.ID == cardID6) == null)
			{
				list.Add("opponent says I have card " + cardID6 + " but I say I don't");
			}
		}
		foreach (CardData item5 in currentBoardState.GetPlayerState(PlayerType.User).Hand)
		{
			if (!list4.Contains(item5.ID))
			{
				list.Add("I say I have card " + item5.ID + " but opponent says I don't");
			}
		}
		List<object> list5 = (List<object>)jsonDict["YourCreatureHand"];
		foreach (object item6 in list5)
		{
			string cardID7 = item6 as string;
			if (currentBoardState.GetPlayerState(PlayerType.User).DeploymentList.Find((CreatureState m) => m.Data.Form.ID == cardID7) == null)
			{
				list.Add("opponent says I have creature card " + cardID7 + " but I say I don't");
			}
		}
		foreach (CreatureState deployment2 in currentBoardState.GetPlayerState(PlayerType.User).DeploymentList)
		{
			if (!list5.Contains(deployment2.Data.Form.ID))
			{
				list.Add("I say I have creature card " + deployment2.Data.Form.ID + " but opponent says I don't");
			}
		}
		for (int num = 0; num < 2; num++)
		{
			PlayerType idx = ((num != 0) ? PlayerType.User : PlayerType.Opponent);
			Dictionary<string, object> dictionary = ((num != 0) ? ((Dictionary<string, object>)jsonDict["YourCreatures"]) : ((Dictionary<string, object>)jsonDict["MyCreatures"]));
			foreach (CreatureState creature in currentBoardState.GetCreatures(idx))
			{
				if (!dictionary.ContainsKey(creature.Data.Form.ID))
				{
					if (num == 0)
					{
						list.Add("I say opponent has creature " + creature.Data.Form.ID + " but he says he doesn't");
					}
					else
					{
						list.Add("I say I have creature " + creature.Data.Form.ID + " but opponent says I don't");
					}
				}
			}
			KeyValuePair<string, object> creaturePair;
			foreach (KeyValuePair<string, object> item7 in dictionary)
			{
				creaturePair = item7;
				CreatureState creatureState = currentBoardState.GetCreatures(idx).Find((CreatureState m) => m.Data.Form.ID == creaturePair.Key);
				if (creatureState == null)
				{
					if (num == 0)
					{
						list.Add("opponent says he has creature " + creaturePair.Key + " but I say he doesn't");
					}
					else
					{
						list.Add("opponent says I have creature " + creaturePair.Key + " but I say I don't");
					}
					continue;
				}
				Dictionary<string, object> dictionary2 = (Dictionary<string, object>)creaturePair.Value;
				int num2 = Convert.ToInt32(dictionary2["Health"]);
				if (num2 != creatureState.HP)
				{
					if (num == 0)
					{
						list.Add("I say opponent's creature " + creaturePair.Key + " has " + creatureState.HP + " HP; opponent says " + num2);
					}
					else
					{
						list.Add("I say my creature " + creaturePair.Key + " has " + creatureState.HP + " HP; opponent says " + num2);
					}
				}
				Dictionary<string, object> dictionary3 = (Dictionary<string, object>)dictionary2["Statuses"];
				foreach (StatusState statusEffect in creatureState.StatusEffects)
				{
					if (!dictionary3.ContainsKey(statusEffect.Data.ID))
					{
						if (num == 0)
						{
							list.Add("I say opponent's creature " + creaturePair.Key + " has status " + statusEffect.Data.ID + " but he says it doesn't");
						}
						else
						{
							list.Add("I say my creature " + creaturePair.Key + " has status " + statusEffect.Data.ID + " but opponent says it doesn't");
						}
					}
				}
				KeyValuePair<string, object> statusPair;
				foreach (KeyValuePair<string, object> item8 in dictionary3)
				{
					statusPair = item8;
					StatusState statusState = creatureState.StatusEffects.Find((StatusState m) => m.Data.ID == statusPair.Key);
					if (statusState == null)
					{
						if (num == 0)
						{
							list.Add("opponent says his creature " + creaturePair.Key + " has status effect " + statusPair.Key + " but I say it doesn't");
						}
						else
						{
							list.Add("opponent says my creature " + creaturePair.Key + " has status effect " + statusPair.Key + " but I say it doesn't");
						}
						continue;
					}
					string valueString = statusState.Data.GetValueString(creatureState, verbose: false);
					string text = Convert.ToString(statusPair.Value);
					if (valueString != text)
					{
						if (num == 0)
						{
							list.Add("opponent says status " + statusPair.Key + " on his creature " + creaturePair.Key + " has value " + text + " but I say " + valueString);
						}
						else
						{
							list.Add("opponent says status " + statusPair.Key + " on my creature " + creaturePair.Key + " has value " + text + " but I say " + valueString);
						}
					}
				}
			}
		}
		return list;
	}

	private void NetworkReachabilityCheckStart()
	{
		mInternet = InternetReachablility.Checking;
		if (Application.internetReachability switch
		{
			NetworkReachability.ReachableViaLocalAreaNetwork => 1, 
			NetworkReachability.ReachableViaCarrierDataNetwork => 1, 
			_ => 0, 
		} == 0)
		{
			InternetIsNotAvailable();
			return;
		}
		mPing = new Ping("google.com");
		mPingStartTime = Time.time;
	}
}
