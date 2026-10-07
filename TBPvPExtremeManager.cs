using System;
using System.Collections.Generic;
using System.Net;
using ExitGames.Client.Photon;
using ExitGames.Client.Photon.LoadBalancing;
using UnityEngine;

public class TBPvPExtremeManager : Singleton<TBPvPExtremeManager>
{
	public enum GameState
	{
		None,
		Login,
		InGame
	}

	public enum tbPvPEventCode
	{
		Connected,
		Disconnected,
		ErrorDisconnected,
		JoinedToExist,
		NotFound,
		NewPlayerJoined,
		CreatedNewRoom,
		IncomingMessages,
		PlayerLeft,
		PropertiesChanged,
		Disabled
	}

	public class IncomingGameMessageObject
	{
		public byte MessageType;
		public string Message;
	}

	public delegate void TBPvPEvent(
		tbPvPEventCode e,
		object obj
	);

	public PhotonInterface GameClientInstance;

	public GameState CurrentState;

	public string PvPExtremeCompatibilityVersion = "612";

	private string UnrankedMatch = "U";

	private int savegameListStartIndex;

	private bool mFriendMatch;

	private string myCountryCode = "..";

	private string myIP = string.Empty;

	private string myLocation = string.Empty;

	private bool visible;

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

	public string CountryCode
	{
		get
		{
			return myCountryCode;
		}
	}

	public string LocationCode
	{
		get
		{
			return myLocation;
		}
	}

	public string IPAddress
	{
		get
		{
			return myIP;
		}
	}

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
		if (GameClientInstance != null)
		{
			return GameClientInstance.IsConnected;
		}

		return false;
	}

	public void Awake()
	{
		Application.runInBackground = true;

		GameClientInstance = new PhotonInterface();

		GameClientInstance.photonExtremeManager = this;

		GameClientInstance.AppId =
			"6bc707ec-39ed-494a-bd6b-7e2d1b269e6f";
	}

	public void Start()
	{
	}

	public void OnEnable()
	{
	}

	public void CheckIP()
	{
		Session theSession =
			SessionManager.Instance.theSession;

		TFServer.JsonResponseHandler handler =
			delegate(
				Dictionary<string, object> data,
				HttpStatusCode status)
			{
				myIP = string.Empty;
				myCountryCode = "US";

				if (status == HttpStatusCode.OK)
				{
					if (data.ContainsKey("ip"))
					{
						myIP = Convert.ToString(
							data["ip"]);
					}

					if (data.ContainsKey("country"))
					{
						myCountryCode = Convert.ToString(
							data["country"]);
					}
				}
			};

		theSession.Server.GetCC(handler);
	}

	public string GetCountryCode(int side)
	{
		return GameClientInstance.GetCountryCode(side);
	}

	public void ConnectToServer(
        string playerName,
        bool ranked,
        TBPvPEvent callback)
    {
        if (!MiscParams.PvpEnable)
        {
            if (callback != null)
            {
                callback(
                    tbPvPEventCode.Disabled,
                    null
                );
            }

            return;
        }

        if (GameClientInstance.IsConnected)
        {
            if (callback != null)
            {
                callback(
                    tbPvPEventCode.Connected,
                    null
                );
            }

            return;
        }

        GameClientInstance.init();

        GameClientInstance.AppVersion =
            GetCompatibilityVersion(ranked);

        GameClientInstance.NickName =
            playerName;

        GameClientInstance.ConnectToRegionMaster("US");

        GameClientInstance.pvpExtremeEventCallback =
			delegate(
				TBPvPExtremeManager.tbPvPEventCode code,
				object obj)
			{
				if (callback != null)
				{
					callback(
						code,
						obj
					);
				}
			};

        Screen.sleepTimeout =
            SleepTimeout.NeverSleep;
    }

	public string GetCompatibilityVersion(bool ranked)
    {
        int pvpExtremeCompatibilityVersion =
            MiscParams.PvPExtremeCompatibilityVersion;

        if (Convert.ToInt32(PvPExtremeCompatibilityVersion) < 0)
        {
            return PvPExtremeCompatibilityVersion;
        }

        string text =
            ((Singleton<PlayerInfoScript>.Instance.SaveData
                .PvpSpecialDomainNumber == 0)
                ? "y"
                : "x");

        string version =
            Convert.ToString(pvpExtremeCompatibilityVersion)
            + "D"
            + text;

        if (!ranked)
        {
            version += "U";
        }

        return version;
    }

	public void Disconnect()
    {
        FriendMatch = false;

        Visible = false;

        CurrentState = GameState.Login;

        if (GameClientInstance != null)
        {
            GameClientInstance.Disconnect();

            GameClientInstance.OnStateChangeAction =
                (Action<ClientState>)Delegate.Remove(
                    GameClientInstance.OnStateChangeAction,
                    new Action<ClientState>(OnStateChanged)
                );
        }

        Screen.sleepTimeout =
            SleepTimeout.SystemSetting;
    }

	public void OnApplicationQuit()
	{
		if (GameClientInstance != null &&
			GameClientInstance.loadBalancingPeer != null)
		{
			GameClientInstance.Disconnect();

			GameClientInstance.loadBalancingPeer.StopThread();
		}

		GameClientInstance = null;
	}

	private void onPushNotificationsReceived(
		string payload)
	{
		if (GameClientInstance != null &&
			GameClientInstance.Server ==
			LoadBalancingClient.ServerConnection.MasterServer)
		{
			GetRoomsList();
		}
	}

	private void OnStateChanged(ClientState state)
    {
        switch (state)
        {
            case ClientState.ConnectedToMaster:

                Visible = true;

                if (GameClientInstance.pvpEventCallback != null)
                {
                    GameClientInstance.pvpEventCallback(
                        TBPvPManager.tbPvPEventCode.Connected,
                        null
                    );
                }

                break;
        }
    }

	private void OnVisibleChanged()
	{
		if (!visible)
		{
		}
	}

	public void Update()
	{
		if (GameClientInstance != null &&
			GameClientInstance.loadBalancingPeer != null)
		{
			GameClientInstance.Service(
				GameClientInstance.loadBalancingPeer
			);
		}
	}

	private void GetRoomsList()
	{
		savegameListStartIndex = 0;

		GameClientInstance.OpWebRpc(
			"GetGameList",
			null
		);
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
		LeaveGame(false);
	}

	public void AbandonGame()
	{
		LeaveGame(true);
	}

	public void LeaveGame(bool doAbandon)
	{
		if (GameClientInstance == null)
			return;

		GameClientInstance.OpLeaveRoom(
			!doAbandon
		);

		CurrentState = GameState.Login;

		Visible = false;
	}

	public void SearchGame(
        int mylevel,
        int range,
        int range2nd,
        int range3rd)
    {
        Visible = false;

        CurrentState = GameState.InGame;

        GameClientInstance.JoinRandomRoom(
            mylevel,
            range,
            range2nd,
            range3rd
        );
    }

    public void ConnectGame(string targetname)
    {
        Visible = false;

        CurrentState = GameState.InGame;

        GameClientInstance.JoinTheGame(
            targetname
        );
    }

	public void LoadGame(object[] parameters)
    {
        string roomName =
            parameters[0] as string;

        int actorNumber =
            (int)parameters[1];

        Visible = false;

        CurrentState = GameState.InGame;

        GameClientInstance.OpJoinRoom(
            roomName,
            actorNumber
        );
    }

	public void CreateMyRoom(string playerId)
    {
        Visible = false;

        CurrentState = GameState.InGame;

        GameClientInstance.OpCreateRoom(
            playerId
        );
    }

	public void SendMessage(
        byte messagetype,
        string messagebody)
    {
        Hashtable hashtable =
            new Hashtable();

        hashtable[messagetype] =
            messagebody;

        GameClientInstance.loadBalancingPeer.OpRaiseEvent(
            1,
            hashtable,
            true,
            null
        );
    }
}