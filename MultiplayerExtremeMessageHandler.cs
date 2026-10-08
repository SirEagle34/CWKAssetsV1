using System;
using System.Collections;
using System.Collections.Generic;
using CodeStage.AntiCheat.ObscuredTypes;
using MiniJSON;
using UnityEngine;

public class MultiplayerExtremeMessageHandler : Singleton<MultiplayerExtremeMessageHandler>
{
	private enum MessageTypeEnum
	{
		MatchStartData,
		CreaturePlay,
		CardPlay,
		DragAttack,
		EndTurn,
		MatchRequest,
		AcceptMatchRequest,
		DeclineMatchRequest,
		QuickChat,
		Quitting,
		Unset,
		AutoDeclineMatchRequest,
		NotifyMatchRequestCanceled,
		NotifyMatchRequestAcceptConfirmed,
		PingSend,
		PingReturn,
	}

	private enum MatchMode
	{
		MM_UNDEFINED,
		MM_GENERAL_HOST,
		MM_GENERAL_CLIENT,
		MM_ALLY_HOST,
		MM_ALLY_CLIENT,
		MM_EXTREME
	}

	private enum JoinStatusEnum
	{
		NotJoined,
		JoinedAsHost,
		JoinedAsClient
	}

	public class ReceivedMatchRequest
	{
		public string PlayerName;

		public string PlayerID;
	}

	private ReceivedMatchRequest mEnteringFriendMatch;

	public enum MatchRequestRejectReason
	{
		Declined,
		NotAvailable,
		NotUnlocked,
		Blocked,
		Incompatible,
		Timeout
	}

	private class QueuedAction
	{
		public AIDecision Decision;
		public int TargetLaneIndex;
		public Dictionary<string, object> DebugStateDict;
	}

	public enum InternetReachablility
	{
		Undefined,
		Checking,
		Reachable,
		NotReachable
	}

	private JoinStatusEnum mJoinStatus;

	private bool mInBattle;

	private bool mFriendJoinedGame;
		
	private bool mFriendGameCreated;

	private bool mRetryJoiningFriendGame;

    private Ping mPing;

	private float mPingStartTime;

	private bool mWaitingPingResponse;

	private bool mNeedToResponse;

	private InternetReachablility mInternet;

	private DateTime mPaused = DateTime.Now;

	private DateTime mAllyMatchNow = DateTime.Now.AddYears(1);


	private bool mHost;

	private bool mWaitingAck;

	private bool mUserLeft;

	private bool mClearingMatchRequests;

	private bool mOpponentLeft;

	private bool mOpponentLeftError;

	private bool mQuittingMatch;

	private bool mNeedToDetectDisconnect;

	private bool mInDisconnectDelay;

	private bool mCancelingMatchmaking;

	private bool mRestartingMatchmaking;

	private float mRestartMatchmakingTimeout;

	private bool mInReconnectTry;

	private int mRetryCount;

	private float mRetryTimeout;

	private bool mAllyMatchWatch;

	private bool mAllyMatchTimeout;

    public float DisconnectDelay;

	private bool mSentFriendStartData;
	
	private Dictionary<string, object> mSaveJsonDict;

	private string mWaitingForMatchRequestResponseFrom;

	private string mWaitingForPingResponseFrom;

	private const float TimeoutTime = 30f;

	private const bool mAllowCarrierDataNetwork = false;

	private const string mPingAddress = "google.com";

	private const float mWaitingTime = 2f;

	public float JoinFriendGameDelay = 1f;

	private MatchMode mMatchMode;
	private List<QueuedAction> mQueuedActions =
		new List<QueuedAction>();

	private List<ReceivedMatchRequest> mReceivedMatchRequests = new List<ReceivedMatchRequest>();

	private bool mAcceptMatchResponseReceived;

	public bool InBattle
	{
		get
		{
			return mInBattle;
		}
	}


	public void ClearAllMatchRequests()
	{
		mReceivedMatchRequests.Clear();
	}

	// ============================================================
	// MATCHMAKING
	// ============================================================

	private void Reset()
	{
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

	public void ResetAllyMatchWatch()
	{
		mAllyMatchWatch = false;
		mAllyMatchTimeout = false;
		mMatchMode = MatchMode.MM_UNDEFINED;
	}

	public void StartExtremeMatchmaking()
	{
		Reset();

		mMatchMode = MatchMode.MM_EXTREME;
		mRestartMatchmakingTimeout = 30f;
		mJoinStatus = JoinStatusEnum.NotJoined;

		bool ranked =
			Singleton<PlayerInfoScript>.Instance.PvPData.ExtremeMode;

		Debug.Log(
			"[EXTREME] Start matchmaking. Ranked = " +
			ranked
		);

		int levelRange = ((!ranked) ? MiscParams.MultiplayerUnrankedSearchRange : MiscParams.MultiplayerRankedSearchRange);
		int levelRange2nd = ((!ranked) ? MiscParams.MultiplayerUnrankedSearchRange2nd : MiscParams.MultiplayerRankedSearchRange2nd);
		int levelRange3rd = ((!ranked) ? MiscParams.MultiplayerUnrankedSearchRange3rd : MiscParams.MultiplayerRankedSearchRange3rd);
		Singleton<OnlineExtremePvPManager>.Instance.SearchGame(Singleton<PlayerInfoScript>.Instance.SaveData.MultiplayerPlayerName, GetExtremeLevel(), levelRange, levelRange2nd, levelRange3rd, ranked, OnExtremeOnlineEvent);

	}

	private void OnExtremeOnlineEvent(
		OnlineExtremePvPManager.OnlineExtremePvPEventCode eventCode,
		object obj)
	{
		Debug.Log("[EXTREME] Online Event = " + eventCode);

		switch (eventCode)
		{
			case OnlineExtremePvPManager.OnlineExtremePvPEventCode.Connected:
			{
				Debug.Log("[EXTREME] Connected");
				break;
			}

			case OnlineExtremePvPManager.OnlineExtremePvPEventCode.CreatedNewGame:
			{
				Debug.Log("[EXTREME] CreatedNewGame");

				mHost = true;

				// BURADA MATCH COMPLETE YOK!
				// Rakibin gelmesini bekliyoruz.
				break;
			}

			case OnlineExtremePvPManager.OnlineExtremePvPEventCode.NewPlayerJoined:
			{
				Debug.Log("[EXTREME] NewPlayerJoined");

				mHost = true;

				OnExtremeMatchmakingComplete(true);

				break;
			}

			case OnlineExtremePvPManager.OnlineExtremePvPEventCode.JoinedToExist:
			{
				Debug.Log("[EXTREME] JoinedToExist");

				mHost = false;

				OnExtremeMatchmakingComplete(false);

				break;
			}

			case OnlineExtremePvPManager.OnlineExtremePvPEventCode.IncomingMessages:
			{
				Debug.Log("[EXTREME] IncomingMessages");

				TBPvPExtremeManager.IncomingGameMessageObject msgObj =
					obj as TBPvPExtremeManager.IncomingGameMessageObject;

				if (msgObj != null &&
					!string.IsNullOrEmpty(msgObj.Message))
				{
					Debug.Log(
						"[EXTREME] RECEIVE = " +
						msgObj.Message
					);

					ReceiveMessage(msgObj.Message);
				}

				break;
			}

			case OnlineExtremePvPManager.OnlineExtremePvPEventCode.SendSuccess:
			{
				Debug.Log("[EXTREME] SendSuccess");

				mWaitingAck = false;

				break;
			}

			case OnlineExtremePvPManager.OnlineExtremePvPEventCode.SendTimeout:
			{
				Debug.Log("[EXTREME] SendTimeout");

				break;
			}

			case OnlineExtremePvPManager.OnlineExtremePvPEventCode.PlayerLeft:
			{
				Debug.Log("[EXTREME] PlayerLeft");

				mOpponentLeft = true;

				break;
			}

			case OnlineExtremePvPManager.OnlineExtremePvPEventCode.NotFound:
			{
				Debug.Log("[EXTREME] NotFound");

				break;
			}

			case OnlineExtremePvPManager.OnlineExtremePvPEventCode.Error:
			{
				Debug.Log("[EXTREME] Error");

				break;
			}

			case OnlineExtremePvPManager.OnlineExtremePvPEventCode.Disconnected:
			{
				Debug.Log("[EXTREME] Disconnected");

				break;
			}

			case OnlineExtremePvPManager.OnlineExtremePvPEventCode.PropertiesChanged:
			{
				Debug.Log("[EXTREME] PropertiesChanged");

				break;
			}

			case OnlineExtremePvPManager.OnlineExtremePvPEventCode.Disabled:
			{
				Debug.Log("[EXTREME] Disabled");

				break;
			}
		}
	}

	private void OnExtremeMatchmakingComplete(bool amIPrimary)
	{
		if (mJoinStatus != JoinStatusEnum.NotJoined)
		{
			Debug.Log(
				"[EXTREME] Matchmaking already completed."
			);

			return;
		}

		if (mCancelingMatchmaking ||
			mRestartingMatchmaking)
		{
			Debug.Log(
				"[EXTREME] Matchmaking cancelled/restarting."
			);

			return;
		}

		mRestartMatchmakingTimeout = -1f;

		mJoinStatus =
			amIPrimary
				? JoinStatusEnum.JoinedAsHost
				: JoinStatusEnum.JoinedAsClient;

		Singleton<PlayerInfoScript>.Instance.PvPData.AmIPrimary =
			amIPrimary;

		Debug.Log(
			"[EXTREME] MATCH FOUND! Primary = " +
			amIPrimary
		);

		if (Singleton<PlayerInfoScript>.Instance.StateData.MultiplayerMode)
		{
			Singleton<PVPExtremePrepScreenController>.Instance
				.matchFound = true;
		}

		Singleton<PVPExtremePrepScreenController>.Instance
			.OnConnectionComplete(amIPrimary);
	}

	// ============================================================
	// SERIALIZATION
	// ============================================================

	private string SerializeDictionary(
		Dictionary<string, object> jsonDict,
		MessageTypeEnum addType = MessageTypeEnum.Unset)
	{
		string text = "{";

		if (addType != MessageTypeEnum.Unset)
		{
			text += "\"Type\":" + (int)addType + ",";
		}

		foreach (KeyValuePair<string, object> item in jsonDict)
		{
			text += "\"" +
				item.Key +
				"\":" +
				SerializeObject(item.Value) +
				",";
		}

		return text + "}";
	}

	private string SerializeList(List<object> jsonList)
	{
		string text = "[";

		foreach (object json in jsonList)
		{
			text += SerializeObject(json) + ",";
		}

		return text + "]";
	}

	private string SerializeObject(object jsonObject)
	{
		if (jsonObject is Dictionary<string, object>)
		{
			return SerializeDictionary(
				jsonObject as Dictionary<string, object>);
		}

		if (jsonObject is List<object>)
		{
			return SerializeList(
				jsonObject as List<object>);
		}

		if (jsonObject is string ||
			jsonObject is ObscuredString)
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
			return ((bool)jsonObject) ? "1" : "0";
		}

		return jsonObject.ToString();
	}

	// ============================================================
	// SEND / RECEIVE MESSAGE
	// ============================================================

	private void SendMessage(
		MessageTypeEnum messageType,
		Dictionary<string, object> jsonDict = null)
	{
		if (jsonDict == null)
		{
			jsonDict = new Dictionary<string, object>();
		}

		string message =
			SerializeDictionary(jsonDict, messageType);

		mWaitingAck = true;

		Singleton<OnlineExtremePvPManager>.Instance
			.SendMessage(message);
	}

	public void ReceiveMessage(string jsonString)
	{
		if (string.IsNullOrEmpty(jsonString))
		{
			return;
		}

		Dictionary<string, object> dictionary =
			(Dictionary<string, object>)Json.Deserialize(jsonString);

		if (dictionary == null ||
			!dictionary.ContainsKey("Type"))
		{
			return;
		}

		switch (Convert.ToInt32(dictionary["Type"]))
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

			case 8:
				ReceiveQuickChat(dictionary);
				break;

			case 9:
				ReceiveQuitMessage();
				break;
		}
	}
	
	public void ReceiveChatBasedMessage(string jsonString)
	{
		if (string.IsNullOrEmpty(jsonString))
		{
			return;
		}
		Dictionary<string, object> dictionary = (Dictionary<string, object>)Json.Deserialize(jsonString);
		string text = Convert.ToString(dictionary["SenderID"]);
		if (!(text == Singleton<PlayerInfoScript>.Instance.GetPlayerCode()))
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
			case 12:
				ReceiveMatchRequestCanceled(dictionary);
				break;
			case 13:
				ReceiveMatchRequestAcceptConfirmed(dictionary);
				break;
			case 14:
				ReceivePingSend(dictionary);
				break;
			case 15:
				ReceivePingReturn(dictionary);
				break;
			case 11:
				break;
			}
		}
	}

    private void KPISyncFail(string reason)
	{
		if (!DetachedSingleton<SceneFlowManager>.Instance.InBattleScene() && Singleton<PlayerInfoScript>.Instance.StateData.MultiplayerMode)
		{
			if (Singleton<PVPExtremePrepScreenController>.Instance.matchFound)
			{
				Singleton<PVPExtremePrepScreenController>.Instance.syncTime = 0f;
				Singleton<PVPExtremePrepScreenController>.Instance.matchFound = false;
			}
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
				CancelMatchmaking(true);
				StartExtremeMatchmaking();
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

		Singleton<OnlineExtremePvPManager>.Instance.LeaveGame();
	}

	private IEnumerator StartDisconnectDelay()
	{
		mInDisconnectDelay = true;
		yield return new WaitForSeconds(DisconnectDelay);
		mInDisconnectDelay = false;
		mCancelingMatchmaking = false;
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
			Singleton<PVPExtremePrepScreenController>.Instance.OnFriendLeft();
		}
		mEnteringFriendMatch = null;
		mInternet = InternetReachablility.Undefined;
	}

		public void OnMatchmakingComplete(bool amIPrimary)
	{
		if (mJoinStatus == JoinStatusEnum.NotJoined && !mCancelingMatchmaking && !mRestartingMatchmaking)
		{
            if (Singleton<PlayerInfoScript>.Instance.StateData.MultiplayerMode) Singleton<PVPExtremePrepScreenController>.Instance.matchFound = true;
            mRestartMatchmakingTimeout = 30f;
			mJoinStatus = (amIPrimary ? JoinStatusEnum.JoinedAsHost : JoinStatusEnum.JoinedAsClient);
			Singleton<PVPExtremePrepScreenController>.Instance.OnConnectionComplete(amIPrimary);
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
		dictionary.Add("AppVersion", Singleton<TBPvPExtremeManager>.Instance.GetCompatibilityVersion(false));
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
			DateTime dateTime = DateTime.FromBinary(Convert.ToInt64(jsonDict["Time"]));
			string text = Convert.ToString(jsonDict["SenderName"]);
			string text2 = Convert.ToString(jsonDict["SenderID"]);
			string text3 = Convert.ToString(jsonDict["receiverID"]);
			if (text3 == Singleton<PlayerInfoScript>.Instance.GetPlayerCode())
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
			TimeSpan timeSpan = DateTime.Now - dateTime;
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
			string compatibilityVersion = Singleton<TBPvPExtremeManager>.Instance.GetCompatibilityVersion(false);
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


	public void AcceptMatchRequest(ReceivedMatchRequest request)
	{
		StartCoroutine(AcceptMatchRequestCo(request));
	}

	private IEnumerator AcceptMatchRequestCo(ReceivedMatchRequest request)
	{
		mClearingMatchRequests = true;
		foreach (ReceivedMatchRequest pendingRequest in mReceivedMatchRequests)
		{
			DeclineMatchRequest(pendingRequest, MatchRequestRejectReason.NotAvailable);
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
		Singleton<TBPvPExtremeManager>.Instance.FriendMatch = true;
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
		mMatchMode = MatchMode.MM_EXTREME;
		bool ranked = Singleton<PlayerInfoScript>.Instance.PvPData.RankedMode;
		int levelRange = MiscParams.MultiplayerUnrankedSearchRange;
		int levelRange2nd = MiscParams.MultiplayerUnrankedSearchRange2nd;
		int levelRange3rd = MiscParams.MultiplayerUnrankedSearchRange3rd;
		Singleton<OnlineExtremePvPManager>.Instance.SetFriendFilter(Singleton<PlayerInfoScript>.Instance.GetPlayerCode());
		Singleton<OnlineExtremePvPManager>.Instance.SearchGame(Singleton<PlayerInfoScript>.Instance.GetPlayerCode(), GetExtremeLevel(), levelRange, levelRange2nd, levelRange3rd, ranked, OnExtremeOnlineEvent);
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
		Singleton<TBPvPExtremeManager>.Instance.FriendMatch = true;
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
			mMatchMode = MatchMode.MM_EXTREME;
			bool ranked = Singleton<PlayerInfoScript>.Instance.PvPData.RankedMode;
			int levelRange = MiscParams.MultiplayerUnrankedSearchRange;
			int levelRange2nd = MiscParams.MultiplayerUnrankedSearchRange2nd;
			int levelRange3rd = MiscParams.MultiplayerUnrankedSearchRange3rd;
			Singleton<OnlineExtremePvPManager>.Instance.SetFriendFilter(mEnteringFriendMatch.PlayerID);
			Singleton<OnlineExtremePvPManager>.Instance.SearchGame(mEnteringFriendMatch.PlayerID, GetExtremeLevel(), levelRange, levelRange2nd, levelRange3rd, ranked, OnExtremeOnlineEvent);
			while (!mRetryJoiningFriendGame && !mFriendJoinedGame)
			{
				yield return null;
			}
		}
		while (!mFriendJoinedGame);
		Singleton<BusyIconPanelController>.Instance.Hide();
		Singleton<PlayerInfoScript>.Instance.PvPData.AmIPrimary = false;
		Singleton<PVPExtremePrepScreenController>.Instance.Show(PvpMode.Friend, mEnteringFriendMatch.PlayerName);
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
		int num = Convert.ToInt32(jsonDict["Reason"]);
		MatchRequestRejectReason reason = (MatchRequestRejectReason)num;
		string text = Convert.ToString(jsonDict["SenderID"]);
		if (!(text != mWaitingForMatchRequestResponseFrom))
		{
			Singleton<PVPSendMatchRequestController>.Instance.OnExtremeMatchRequestDeclined(reason);
			ResetAllyMatchWatch();
		}
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

	// ============================================================
	// MATCH START DATA
	// ============================================================

	public void SendMatchStartData(Loadout loadout)
	{
		PvPGameStateData pvpData =
			Singleton<PlayerInfoScript>.Instance.PvPData;

		Dictionary<string, object> dictionary =
			new Dictionary<string, object>();

		dictionary.Add(
			"Name",
			Singleton<PlayerInfoScript>.Instance
				.SaveData.MultiplayerPlayerName);

		dictionary.Add(
			"ID",
			Singleton<PlayerInfoScript>.Instance.GetPlayerCode());

		/*
		 * Extreme mode için normal GetMyLevel yerine
		 * burada kendi Extreme level sistemini kullanabilirsin.
		 */
		dictionary.Add(
			"Level",
			GetExtremeLevel());

		dictionary.Add(
			"Card",
			Singleton<PlayerInfoScript>.Instance
				.SaveData.SelectedCardBack.ID);

		dictionary.Add(
			"Portrait",
			Singleton<PlayerInfoScript>.Instance
				.SaveData.SelectedPortrait.ID);

		if (pvpData.RankedMode)
		{
			dictionary.Add(
				"BestLevel",
				Singleton<PlayerInfoScript>.Instance
					.SaveData.BestMultiplayerLevel);
		}

		dictionary.Add(
			"Leader",
			loadout.Leader.SelectedSkin.ID);

		List<object> creatures =
			new List<object>();

		foreach (InventorySlotItem item in loadout.CreatureSet)
		{
			if (item != null)
			{
				creatures.Add(
					item.Creature.Serialize());
			}
		}

		dictionary.Add(
			"Creatures",
			creatures);

		List<object> cards =
			new List<object>();

		List<InventorySlotItem> exCards =
			Singleton<PlayerInfoScript>.Instance.SaveData
				.FindAllExCards(
					(CardItem card) =>
						loadout.CreatureSet.Find(
							(InventorySlotItem creature) =>
								creature != null &&
								creature.Creature.UniqueId ==
								card.CreatureUID) != null
				);

		foreach (InventorySlotItem item in exCards)
		{
			cards.Add(
				item.Card.Serialize());
		}

		dictionary.Add(
			"Cards",
			cards);

		if (pvpData.AmIPrimary)
		{
			pvpData.WonInitialCoinFlip =
				UnityEngine.Random.Range(0, 2) == 0;

			dictionary.Add(
				"CoinFlip",
				!pvpData.WonInitialCoinFlip);

			dictionary.Add(
				"Seed",
				KFFRandom.Seed);
		}

		SendMessage(
			MessageTypeEnum.MatchStartData,
			dictionary);
	}

	private int GetExtremeLevel()
	{
		/*
		 * Extreme mode'da senin sistemindeki gerçek level
		 * değerini buraya koy.
		 *
		 * Eğer Extreme matchmaking level'i normal sistemle
		 * aynı olacaksa:
		 *
		 * return Singleton<PlayerInfoScript>.Instance
		 *     .SaveData.MultiplayerLevel;
		 */

		return Singleton<PlayerInfoScript>.Instance
			.SaveData.MultiplayerLevel;
	}

	private void ReceiveMatchStartData(
		Dictionary<string, object> jsonDict)
	{
		PvPGameStateData pvpData =
			Singleton<PlayerInfoScript>.Instance.PvPData;

		pvpData.OpponentName =
			Convert.ToString(jsonDict["Name"]);

		pvpData.OpponentID =
			Convert.ToString(jsonDict["ID"]);

		pvpData.OpponentLevel =
			Convert.ToInt32(jsonDict["Level"]);

		pvpData.OpponentBestLevel =
			TFUtils.LoadInt(
				jsonDict,
				"BestLevel",
				-1);

		pvpData.OpponentCardBack =
			CardBackDataManager.Instance.GetData(
				TFUtils.LoadString(
					jsonDict,
					"Card",
					string.Empty));

		if (pvpData.OpponentCardBack == null)
		{
			pvpData.OpponentCardBack =
				CardBackDataManager.DefaultData;
		}

		pvpData.OpponentPortraitData =
			PlayerPortraitDataManager.Instance.GetData(
				TFUtils.LoadString(
					jsonDict,
					"Portrait",
					string.Empty));

		if (pvpData.OpponentPortraitData == null)
		{
			pvpData.OpponentPortraitData =
				PlayerPortraitDataManager.Instance
					.GetData("Default");
		}

		pvpData.OpponentFBID = null;
		pvpData.OpponentPortrait = null;

		pvpData.OpponentLoadout =
			new Loadout();

		LeaderData data =
			LeaderDataManager.Instance.GetData(
				Convert.ToString(jsonDict["Leader"]));

		LeaderData leader =
			(data.SkinParentLeader == null)
				? data
				: data.SkinParentLeader;

		pvpData.OpponentLoadout.Leader =
			new LeaderItem(leader);

		pvpData.OpponentLoadout.Leader.SelectedSkin =
			data;

		List<object> creatures =
			(List<object>)jsonDict["Creatures"];

		foreach (object item in creatures)
		{
			InventorySlotItem creature =
				new InventorySlotItem(
					new CreatureItem(
						item as Dictionary<string, object>,
						true));

			pvpData.OpponentLoadout.CreatureSet
				.Add(creature);
		}

		List<object> cards =
			(List<object>)jsonDict["Cards"];

		foreach (object item in cards)
		{
			InventorySlotItem card =
				new InventorySlotItem(
					new CardItem(
						item as Dictionary<string, object>));

			InventorySlotItem creature =
				pvpData.OpponentLoadout.CreatureSet.Find(
					(InventorySlotItem m) =>
						m.Creature.UniqueId ==
						card.Card.CreatureUID);

			if (creature != null)
			{
				creature.Creature.ExCards[
					card.Card.CreatureSlot] = card;
			}
		}

		if (!pvpData.AmIPrimary)
		{
			pvpData.WonInitialCoinFlip =
				Convert.ToInt32(
					jsonDict["CoinFlip"]) == 1;

			KFFRandom.Seed =
				Convert.ToInt32(
					jsonDict["Seed"]);
		}

		/*
		 * Normal:
		 * PVPPrepScreenController.OnOpponentDataReceived()
		 *
		 * Extreme:
		 * PVPExtremePrepScreenController.OnOpponentDataReceived()
		 */
		Singleton<PVPExtremePrepScreenController>.Instance
			.OnOpponentDataReceived();
	}

	// ============================================================
	// CREATURE PLAY
	// ============================================================

	public void SendCreaturePlay(
		CreatureItem creature,
		int laneIndex)
	{
		Dictionary<string, object> dictionary =
			new Dictionary<string, object>();

		dictionary.Add(
			"Seed",
			KFFRandom.Seed);

		dictionary.Add(
			"ID",
			creature.UniqueId);

		dictionary.Add(
			"Lane",
			laneIndex);

		SendMessage(
			MessageTypeEnum.CreaturePlay,
			dictionary);
	}

	private void ReceiveCreaturePlay(
		Dictionary<string, object> jsonDict)
	{
		PvPGameStateData pvpData =
			Singleton<PlayerInfoScript>.Instance.PvPData;

		QueuedAction queuedAction =
			new QueuedAction();

		queuedAction.Decision =
			new AIDecision();

		queuedAction.Decision.Seed =
			Convert.ToInt32(
				jsonDict["Seed"]);

		queuedAction.Decision.IsDeploy =
			true;

		int creatureID =
			Convert.ToInt32(
				jsonDict["ID"]);

		InventorySlotItem creature =
			pvpData.OpponentLoadout.CreatureSet.Find(
				(InventorySlotItem m) =>
					m.Creature.UniqueId ==
					creatureID);

		if (creature == null)
		{
			Debug.LogError(
				"Extreme: Opponent creature not found: " +
				creatureID);

			return;
		}

		queuedAction.Decision.Creature =
			creature.Creature;

		queuedAction.Decision.LaneIndex1 =
			Convert.ToInt32(
				jsonDict["Lane"]);

		object value;

		if (jsonDict.TryGetValue(
			"DebugStateCheck",
			out value))
		{
			queuedAction.DebugStateDict =
				(Dictionary<string, object>)value;
		}

		mQueuedActions.Add(
			queuedAction);
	}

	// ============================================================
	// CARD PLAY
	// ============================================================

	public void SendCardPlay(
		CardData card,
		PlayerType player,
		int laneIndex1,
		int targetLane)
	{
		Dictionary<string, object> dictionary =
			new Dictionary<string, object>();

		dictionary.Add(
			"Seed",
			KFFRandom.Seed);

		dictionary.Add(
			"ID",
			card.ID);

		dictionary.Add(
			"Player",
			player.IntValue);

		if (laneIndex1 != -1)
		{
			dictionary.Add(
				"Lane1",
				laneIndex1);
		}

		if (targetLane != -1)
		{
			dictionary.Add(
				"Target",
				targetLane);
		}

		SendMessage(
			MessageTypeEnum.CardPlay,
			dictionary);
	}

	private void ReceiveCardPlay(
		Dictionary<string, object> jsonDict)
	{
		QueuedAction queuedAction =
			new QueuedAction();

		queuedAction.Decision =
			new AIDecision();

		queuedAction.Decision.Seed =
			Convert.ToInt32(
				jsonDict["Seed"]);

		queuedAction.Decision.Card =
			CardDataManager.Instance.GetData(
				Convert.ToString(
					jsonDict["ID"]));

		queuedAction.Decision.TargetPlayer =
			(Convert.ToInt32(
				jsonDict["Player"]) != 0)
				? PlayerType.User
				: PlayerType.Opponent;

		queuedAction.Decision.LaneIndex1 =
			TFUtils.LoadInt(
				jsonDict,
				"Lane1",
				-1);

		queuedAction.TargetLaneIndex =
			TFUtils.LoadInt(
				jsonDict,
				"Target",
				-1);

		object value;

		if (jsonDict.TryGetValue(
			"DebugStateCheck",
			out value))
		{
			queuedAction.DebugStateDict =
				(Dictionary<string, object>)value;
		}

		mQueuedActions.Add(
			queuedAction);
	}

	// ============================================================
	// DRAG ATTACK
	// ============================================================

	public void SendDragAttack(
		int laneIndex1,
		int laneIndex2)
	{
		Dictionary<string, object> dictionary =
			new Dictionary<string, object>();

		dictionary.Add(
			"Seed",
			KFFRandom.Seed);

		dictionary.Add(
			"Lane1",
			laneIndex1);

		dictionary.Add(
			"Lane2",
			laneIndex2);

		SendMessage(
			MessageTypeEnum.DragAttack,
			dictionary);
	}

	private void ReceiveDragAttack(
		Dictionary<string, object> jsonDict)
	{
		QueuedAction queuedAction =
			new QueuedAction();

		queuedAction.Decision =
			new AIDecision();

		queuedAction.Decision.Seed =
			Convert.ToInt32(
				jsonDict["Seed"]);

		queuedAction.Decision.IsAttack =
			true;

		queuedAction.Decision.LaneIndex1 =
			Convert.ToInt32(
				jsonDict["Lane1"]);

		queuedAction.Decision.LaneIndex2 =
			Convert.ToInt32(
				jsonDict["Lane2"]);

		object value;

		if (jsonDict.TryGetValue(
			"DebugStateCheck",
			out value))
		{
			queuedAction.DebugStateDict =
				(Dictionary<string, object>)value;
		}

		mQueuedActions.Add(
			queuedAction);
	}

	// ============================================================
	// END TURN
	// ============================================================

	public void SendEndTurn()
	{
		SendMessage(
			MessageTypeEnum.EndTurn);
	}

	private void ReceiveEndTurn(
		Dictionary<string, object> jsonDict)
	{
		QueuedAction queuedAction =
			new QueuedAction();

		queuedAction.Decision =
			new AIDecision();

		queuedAction.Decision.EndTurn =
			true;

		mQueuedActions.Add(
			queuedAction);
	}

	// ============================================================
	// OPPONENT MOVE QUEUE
	// ============================================================

	public AIDecision GetNextOpponentMove()
	{
		if (mQueuedActions.Count > 0)
		{
			QueuedAction queuedAction =
				mQueuedActions[0];

			mQueuedActions.RemoveAt(0);

			if (queuedAction.DebugStateDict != null)
			{
				FindGameStateMismatches(
					queuedAction.DebugStateDict);
			}

			Singleton<DWGame>.Instance
				.SetTargetByLane(
					PlayerType.Opponent,
					queuedAction.TargetLaneIndex);

			return queuedAction.Decision;
		}

		return null;
	}

	// ============================================================
	// BATTLE
	// ============================================================

	public void OnStartBattle()
	{
		mInBattle = true;
	}

	// ============================================================
	// QUICK CHAT
	// ============================================================

	public void SendQuickChat(
		QuickChatData data)
	{
		Dictionary<string, object> dictionary =
			new Dictionary<string, object>();

		dictionary.Add(
			"ID",
			data.ID);

		SendMessage(
			MessageTypeEnum.QuickChat,
			dictionary);
	}

	private void ReceiveQuickChat(
		Dictionary<string, object> jsonDict)
	{
		string id =
			Convert.ToString(
				jsonDict["ID"]);

		QuickChatData data =
			QuickChatDataManager.Instance.GetData(id);

		if (data != null)
		{
			Singleton<QuickMessageController>.Instance
				.ShowOpponentChatMessage(data);
		}
	}

	// ============================================================
	// QUIT / LEAVE
	// ============================================================

	public void SendLeaveGame(string reason)
	{
		mNeedToDetectDisconnect = false;

		if (reason == "leave")
		{
			if (mInBattle)
			{
				mQuittingMatch = true;

				SendMessage(
					MessageTypeEnum.Quitting);
			}
			else
			{
				LeaveGame();
			}
		}

		mInBattle = false;
	}

	private void ReceiveQuitMessage()
	{
		mOpponentLeft = true;
		LeaveGame();
	}

	public bool CheckPlayerLeft()
	{
		if (mOpponentLeft)
		{
			mOpponentLeft = false;
			return true;
		}

		if (mUserLeft)
		{
			mUserLeft = false;
			return true;
		}

		return false;
	}

	public void LeaveGame()
	{
		Singleton<BusyIconPanelController>.Instance.Hide();

		Singleton<OnlineExtremePvPManager>.Instance
			.LeaveGame();

		mWaitingAck = false;
		mInBattle = false;
		mQuittingMatch = false;

		mOpponentLeft = false;
		mUserLeft = false;

		mQueuedActions.Clear();
	}

	// ============================================================
	// GAME STATE DEBUG
	// ============================================================

	private Dictionary<string, object> BuildGameStateDict()
	{
		BoardState currentBoardState =
			Singleton<DWGame>.Instance.CurrentBoardState;

		Dictionary<string, object> dictionary =
			new Dictionary<string, object>();

		dictionary["MyHand"] =
			BuildCardList(
				currentBoardState
					.GetPlayerState(PlayerType.User)
					.Hand);

		dictionary["MyCreatureHand"] =
			BuildCreatureList(
				currentBoardState
					.GetPlayerState(PlayerType.User)
					.DeploymentList);

		dictionary["YourHand"] =
			BuildCardList(
				currentBoardState
					.GetPlayerState(PlayerType.Opponent)
					.Hand);

		dictionary["YourCreatureHand"] =
			BuildCreatureList(
				currentBoardState
					.GetPlayerState(PlayerType.Opponent)
					.DeploymentList);

		dictionary["MyCreatures"] =
			BuildCreaturesDict(
				currentBoardState
					.GetCreatures(PlayerType.User));

		dictionary["YourCreatures"] =
			BuildCreaturesDict(
				currentBoardState
					.GetCreatures(PlayerType.Opponent));

		return dictionary;
	}

	private List<object> BuildCardList(
		List<CardData> cards)
	{
		List<object> list =
			new List<object>();

		foreach (CardData card in cards)
		{
			list.Add(card.ID);
		}

		return list;
	}

	private List<object> BuildCreatureList(
		List<CreatureState> creatures)
	{
		List<object> list =
			new List<object>();

		foreach (CreatureState creature in creatures)
		{
			list.Add(
				creature.Data.Form.ID);
		}

		return list;
	}

	private Dictionary<string, object> BuildCreaturesDict(
		List<CreatureState> creatures)
	{
		Dictionary<string, object> dictionary =
			new Dictionary<string, object>();

		foreach (CreatureState creature in creatures)
		{
			Dictionary<string, object> creatureDict =
				new Dictionary<string, object>();

			dictionary[
				creature.Data.Form.ID] =
				creatureDict;

			creatureDict["Health"] =
				creature.HP;

			Dictionary<string, object> statuses =
				new Dictionary<string, object>();

			creatureDict["Statuses"] =
				statuses;

			foreach (StatusState statusEffect
				in creature.StatusEffects)
			{
				statuses[
					statusEffect.Data.ID] =
					statusEffect.Data.GetValueString(
						creature,
						false);
			}
		}

		return dictionary;
	}

	private List<string> FindGameStateMismatches(
		Dictionary<string, object> jsonDict)
	{
		/*
		 * Normal MultiplayerMessageHandler'daki metodun
		 * tamamı burada kullanılabilir.
		 *
		 * Extreme battle state normal battle state ile
		 * aynı BoardState kullandığı için değişiklik
		 * gerekmiyor.
		 */

		BoardState currentBoardState =
			Singleton<DWGame>.Instance.CurrentBoardState;

		List<string> result =
			new List<string>();

		if (jsonDict == null)
		{
			return result;
		}

		return result;
	}

    private void InternetIsNotAvailable()
	{
		mInternet = InternetReachablility.NotReachable;
	}

    private void InternetAvailable()
	{
		mInternet = InternetReachablility.Reachable;
	}


	public string EnteringFriendsGame()
	{
		if (mEnteringFriendMatch == null)
		{
			return null;
		}

		return mEnteringFriendMatch.PlayerID;
	}

}