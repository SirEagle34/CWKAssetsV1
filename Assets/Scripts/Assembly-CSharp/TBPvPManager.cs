using System;
using System.Collections.Generic;
using System.Net;
using ExitGames.Client.Photon;
using ExitGames.Client.Photon.LoadBalancing;
using UnityEngine;

public class TBPvPManager : Singleton<TBPvPManager>
{
	public enum GameState
	{
		None = 0,
		Login = 1,
		InGame = 2
	}

	public enum tbPvPEventCode
	{
		Connected = 0,
		Disconnected = 1,
		ErrorDisconnected = 2,
		JoinedToExist = 3,
		NotFound = 4,
		NewPlayerJoined = 5,
		CreatedNewRoom = 6,
		IncomingMessages = 7,
		PlayerLeft = 8,
		PropertiesChanged = 9,
		Disabled = 10
	}

	public class IncomingGameMessageObject
	{
		public byte MessageType;

		public string Message;
	}

	public delegate void TBPvPEvent(tbPvPEventCode e, object obj);

	private const bool OnGuiShortcut = true;

	public PhotonInterface GameClientInstance;

	public RelayClient RelayInstance;

	public GameState CurrentState;

	public string PvPCompatibilityVersion = "110";

	private string UnrankedMatch = "U";

	private int savegameListStartIndex;

	private bool mFriendMatch;

	private string myCountryCode = "..";

	private string myIP = string.Empty;

	private string myLocation = string.Empty;

	private bool visible;

	private bool UseRelay => RelayInstance != null;

	public bool FriendMatch
	{
		get
		{
			return mFriendMatch;
		}
		set
		{
			mFriendMatch = value;
		}
	}

	public string CountryCode => myCountryCode;

	public string LocationCode => myLocation;

	public string IPAddress => myIP;

	public bool Visible
	{
		get
		{
			return visible;
		}
		set
		{
			visible = value;
			OnVisibleChanged();
		}
	}

	public bool IsConnected()
	{
		if (UseRelay)
		{
			return RelayInstance.IsConnected;
		}
		if (GameClientInstance != null)
		{
			return GameClientInstance.IsConnected;
		}
		return false;
	}

	public void Awake()
	{
		Application.runInBackground = true;

		// The selected mode must be loaded before the networking client is created.
		if (string.IsNullOrEmpty(SQSettings.PHOTON_PUN_APP_ID))
		{
			try
			{
				SQSettings.Init();
			}
			catch (Exception ex)
			{
				Debug.LogError("[TBPvPManager] Could not initialize SQSettings: " + ex.Message);
			}
		}

		if (string.IsNullOrEmpty(SQSettings.PHOTON_PUN_APP_ID))
		{
			Debug.LogError("[TBPvPManager] Photon PUN App ID is empty for mode " + SQSettings.GAME_MODE_ID + ".");
			return;
		}

		if (SQSettings.USE_RELAY)
		{
			RelayInstance = new RelayClient();
			RelayInstance.photonManager = this;
			RelayInstance.AppId = SQSettings.PHOTON_PUN_APP_ID;
		}
		else
		{
			GameClientInstance = new PhotonInterface();
			GameClientInstance.photonManager = this;
			GameClientInstance.AppId = SQSettings.PHOTON_PUN_APP_ID;
		}
	}

	public void Start()
	{
	}

	public void OnEnable()
	{
	}

	public void CheckIP()
	{
		Session theSession = SessionManager.Instance.theSession;
		TFServer.JsonResponseHandler handler = delegate(Dictionary<string, object> data, HttpStatusCode status)
		{
			myIP = string.Empty;
			myCountryCode = "US";
			if (status == HttpStatusCode.OK)
			{
				if (data.ContainsKey("ip"))
				{
					myIP = Convert.ToString(data["ip"]);
				}
				if (data.ContainsKey("country"))
				{
					myCountryCode = Convert.ToString(data["country"]);
				}
			}
		};
		theSession.Server.GetCC(handler);
	}

	public string GetCountryCode(int side)
	{
		if (UseRelay || GameClientInstance == null)
		{
			return string.Empty;
		}
		return GameClientInstance.GetCountryCode(side);
	}

	public void ConnectToServer(string playerName, bool ranked, TBPvPEvent callback)
	{
		if (!MiscParams.PvpEnable)
		{
			callback?.Invoke(tbPvPEventCode.Disabled, null);
		}
		else if (UseRelay)
		{
			if (RelayInstance.IsConnected)
			{
				callback?.Invoke(tbPvPEventCode.Connected, null);
				return;
			}
			RelayInstance.init();
			RelayInstance.AppVersion = GetMatchVersion(ranked);
			RelayInstance.NickName = playerName;
			RelayInstance.pvpEventCallback = callback;
			RelayInstance.ConnectToRegionMaster("US");
			Screen.sleepTimeout = -1;
		}
		else if (GameClientInstance == null)
		{
			Debug.LogError("[TBPvPManager] Photon client is not initialized. Check SQSettings.Init() and the selected game mode.");
			callback?.Invoke(tbPvPEventCode.ErrorDisconnected, null);
		}
		else if (GameClientInstance.IsConnected)
		{
			callback?.Invoke(tbPvPEventCode.Connected, null);
		}
		else
		{
			GameClientInstance.init();
			GameClientInstance.AppVersion = GetMatchVersion(ranked);
			GameClientInstance.NickName = playerName;
			PhotonInterface gameClientInstance = GameClientInstance;
			gameClientInstance.OnStateChangeAction = (Action<ClientState>)Delegate.Combine(gameClientInstance.OnStateChangeAction, new Action<ClientState>(OnStateChanged));
			GameClientInstance.ConnectToRegionMaster("US");
			GameClientInstance.pvpEventCallback = callback;
			Screen.sleepTimeout = -1;
		}
	}

	private string GetMatchVersion(bool ranked)
	{
		PvPGameStateData pvPData = Singleton<PlayerInfoScript>.Instance.PvPData;
		return GetCompatibilityVersion(ranked, pvPData != null && pvPData.ExtremeMode && !FriendMatch);
	}

	public string GetCompatibilityVersion(bool ranked, bool extreme = false)
	{
		int num = (extreme ? MiscParams.PvPExtremeCompatibilityVersion : MiscParams.PvPCompatibilityVersion);
		if (Convert.ToInt32(PvPCompatibilityVersion) < 0)
		{
			return PvPCompatibilityVersion;
		}
		string text2;
		if (num >= 0)
		{
			string text = ((Singleton<PlayerInfoScript>.Instance.SaveData.PvpSpecialDomainNumber == 0) ? "y" : "x");
			text2 = Convert.ToString(num) + "D" + text;
		}
		else
		{
			text2 = Convert.ToString(num);
		}
		if (!ranked)
		{
			text2 += UnrankedMatch;
		}
		return text2;
	}

	public void Disconnect()
	{
		FriendMatch = false;
		Visible = false;
		CurrentState = GameState.Login;

		if (UseRelay)
		{
			RelayInstance.Disconnect();
		}
		else if (GameClientInstance != null)
		{
			GameClientInstance.Disconnect();
			GameClientInstance.OnStateChangeAction = (Action<ClientState>)Delegate.Remove(
				GameClientInstance.OnStateChangeAction,
				new Action<ClientState>(OnStateChanged));
		}

		Screen.sleepTimeout = -2;
	}

	public void OnApplicationQuit()
	{
		if (UseRelay)
		{
			RelayInstance.Disconnect();
			RelayInstance = null;
			return;
		}
		if (GameClientInstance != null && GameClientInstance.loadBalancingPeer != null)
		{
			GameClientInstance.Disconnect();
			GameClientInstance.loadBalancingPeer.StopThread();
		}
		GameClientInstance = null;
	}

	private void onPushNotificationsReceived(string payload)
	{
		if (!UseRelay && GameClientInstance != null &&
			GameClientInstance.Server == LoadBalancingClient.ServerConnection.MasterServer)
		{
			GetRoomsList();
		}
	}

	private void OnStateChanged(ClientState state)
	{
		if (state == ClientState.ConnectedToMaster)
		{
			Visible = true;
			if (GameClientInstance.pvpEventCallback != null)
			{
				GameClientInstance.pvpEventCallback(tbPvPEventCode.Connected, null);
			}
		}
	}

	private void OnVisibleChanged()
	{
		_ = visible;
	}

	public void Update()
	{
		if (UseRelay)
		{
			if (RelayInstance != null)
			{
				RelayInstance.Service();
			}
		}
		else if (GameClientInstance != null && GameClientInstance.loadBalancingPeer != null)
		{
			GameClientInstance.Service(GameClientInstance.loadBalancingPeer);
		}
	}

	private void GetRoomsList()
	{
		if (GameClientInstance == null)
			return;

		savegameListStartIndex = 0;
		GameClientInstance.OpWebRpc("GetGameList", null);
	}

	public void GameListUpdate()
	{
	}

	public void SaveGameList()
	{
		GameListUpdate();
	}

	public void LeaveGame()
	{
		LeaveGame(doAbandon: false);
	}

	public void AbandonGame()
	{
		LeaveGame(doAbandon: true);
	}

	public void LeaveGame(bool doAbandon)
	{
		if (UseRelay)
		{
			RelayInstance.OpLeaveRoom(!doAbandon);
		}
		else if (GameClientInstance != null)
		{
			GameClientInstance.OpLeaveRoom(!doAbandon);
		}
		CurrentState = GameState.Login;
		Visible = false;
	}

	public void SearchGame(int mylevel, int range, int range2nd, int range3rd)
	{
		Visible = false;
		CurrentState = GameState.InGame;
		if (UseRelay)
		{
			RelayInstance.JoinRandomRoom(mylevel, range, range2nd, range3rd);
		}
		else if (GameClientInstance != null)
		{
			GameClientInstance.JoinRandomRoom(mylevel, range, range2nd, range3rd);
		}
		else
		{
			CurrentState = GameState.Login;
			Debug.LogError("[TBPvPManager] Cannot search for a match before the Photon client is initialized.");
		}
	}

	public void ConnectGame(string targetname)
	{
		Visible = false;
		CurrentState = GameState.InGame;
		if (UseRelay)
		{
			RelayInstance.JoinTheGame(targetname);
		}
		else if (GameClientInstance != null)
		{
			GameClientInstance.JoinTheGame(targetname);
		}
		else
		{
			CurrentState = GameState.Login;
			Debug.LogError("[TBPvPManager] Cannot join a room before the Photon client is initialized.");
		}
	}

	public void LoadGame(object[] parameters)
	{
		string roomName = parameters[0] as string;
		int actorNumber = (int)parameters[1];
		Visible = false;
		CurrentState = GameState.InGame;
		if (UseRelay)
		{
			RelayInstance.OpJoinRoom(roomName, actorNumber);
		}
		else if (GameClientInstance != null)
		{
			GameClientInstance.OpJoinRoom(roomName, actorNumber);
		}
		else
		{
			CurrentState = GameState.Login;
			Debug.LogError("[TBPvPManager] Cannot load a room before the Photon client is initialized.");
		}
	}

	public void CreateMyRoom(string playerId)
	{
		Visible = false;
		CurrentState = GameState.InGame;
		if (UseRelay)
		{
			RelayInstance.OpCreateRoom(playerId);
		}
		else if (GameClientInstance != null)
		{
			GameClientInstance.OpCreateRoom(playerId);
		}
		else
		{
			CurrentState = GameState.Login;
			Debug.LogError("[TBPvPManager] Cannot create a room before the Photon client is initialized.");
		}
	}

	public void SendMessage(byte messagetype, string messagebody)
	{
		if (UseRelay)
		{
			RelayInstance.SendGameMessage(messagetype, messagebody);
			return;
		}
		if (GameClientInstance == null || GameClientInstance.loadBalancingPeer == null)
		{
			Debug.LogWarning("[TBPvPManager] Message not sent because the Photon client is not ready.");
			return;
		}

		Hashtable hashtable = new Hashtable();
		hashtable[messagetype] = messagebody;
		GameClientInstance.loadBalancingPeer.OpRaiseEvent(1, hashtable, sendReliable: true, null);
	}
}