using System;
using System.Collections.Generic;
using UnityEngine;

public class OnlineExtremePvPManager : Singleton<OnlineExtremePvPManager>
{
	public enum OnlineExtremePvPEventCode
	{
		Connected,
		Disconnected,
		Error,
		JoinedToExist,
		NotFound,
		NewPlayerJoined,
		CreatedNewGame,
		IncomingMessages,
		PlayerLeft,
		PropertiesChanged,
		Disabled,
		SendSuccess,
		SendTimeout
	}

	private enum GameState
	{
		None,
		Initialized,
		LoggingInServer,
		LoggedInServer,
		SearchingGame,
		LoggingInGame,
		JoiningGame,
		HostingGame,
		InGameHost,
		InGameClient,
		LeavingHost,
		LeavingClient,
		LeavingServer,
		ErrorState
	}

	private enum NextAction
	{
		None,
		CreateGame,
		ConnectGame,
		SearchGame,
		DisconnectServer
	}

	private class StateController
    {
        public GameState currentState { get; set; }

        public List<TBPvPExtremeManager.tbPvPEventCode>
            expectedEvents { get; set; }

        public List<TBPvPExtremeManager.tbPvPEventCode>
            possibleEvents { get; set; }

        public NextAction nextAction { get; set; }

        public StateController(
            GameState state,
            List<TBPvPExtremeManager.tbPvPEventCode> targetEvents,
            NextAction action,
            List<TBPvPExtremeManager.tbPvPEventCode> events)
        {
            currentState = state;
            expectedEvents = targetEvents;
            possibleEvents = events;
            nextAction = action;
        }

        public StateController()
        {
            currentState = GameState.None;
            expectedEvents = null;
            possibleEvents = null;
            nextAction = NextAction.None;
        }

        public bool validate(
            TBPvPExtremeManager.tbPvPEventCode e)
        {
            if (possibleEvents == null)
                return false;

            foreach (
                TBPvPExtremeManager.tbPvPEventCode possibleEvent
                in possibleEvents)
            {
                if (e == possibleEvent)
                {
                    return true;
                }
            }

            return false;
        }

        public bool compare(
            TBPvPExtremeManager.tbPvPEventCode e)
        {
            if (expectedEvents == null)
                return false;

            foreach (
                TBPvPExtremeManager.tbPvPEventCode expectedEvent
                in expectedEvents)
            {
                if (e == expectedEvent)
                {
                    return true;
                }
            }

            return false;
        }
    }

	public delegate void OnlineExtremePvPEvent(
		OnlineExtremePvPEventCode e,
		object obj
	);

	private const byte ACK_BIT = 128;

	private const int IndexRoundOver = 16;

	private const byte HEARTBEAT_TYPE = byte.MaxValue;

	private TBPvPExtremeManager pvpExtremeManager;

	private string UserName = string.Empty;

	private string TargetRoom = string.Empty;

	private int Mylevel;

	private int TargetRange;

	private int TargetRange2nd;

	private int TargetRange3rd;

	private int MyPlayerID;

	private string CurrentGameRoom = string.Empty;

	private DateTime ReconnectStart;

	private bool WaitingAck;

	private float WaitingAckTimeout;

	private bool NeedToResend;

	private int CurrentMessageSendIndex;

	private int CurrentMessageRecvIndex;

	private float HeartBeat;

	private float ResendDelay;

	private string LastMessage;

	private OnlineExtremePvPEvent targetCallback;

	private StateController onlineStateController =
		new StateController();

	private void Awake()
	{
		pvpExtremeManager =
			Singleton<TBPvPExtremeManager>.Instance;
	}

	private void Start()
	{
		onlineStateController.currentState =
			GameState.Initialized;
	}

	private void Update()
    {
        if (WaitingAck)
        {
            WaitingAckTimeout -= Time.deltaTime;

            if (WaitingAckTimeout <= 0f)
            {
                if (targetCallback != null)
                {
                    targetCallback(
                        OnlineExtremePvPEventCode.SendTimeout,
                        null
                    );
                }

                ResetValue();
            }
        }

        if (NeedToResend)
        {
            ResendDelay -= Time.deltaTime;

            if (ResendDelay <= 0f)
            {
                SendMessage(
                    LastMessage,
                    false
                );

                NeedToResend = false;
            }
        }

        if (
            onlineStateController.currentState ==
                GameState.InGameHost ||
            onlineStateController.currentState ==
                GameState.InGameClient)
        {
            HeartBeat -= Time.deltaTime;

            if (HeartBeat <= 0f)
            {
                SendMessageHeatBeat();
            }
        }
    }

	private void tbpvpcallback(
		TBPvPExtremeManager.tbPvPEventCode e,
		object obj)
	{
		if (!onlineStateController.validate(e))
		{
			// Normal OnlinePvPManager'da da burada
			// herhangi bir işlem yapılmıyor.
		}

		switch (onlineStateController.currentState)
		{
			case GameState.Initialized:
				break;

			case GameState.LoggedInServer:

				if (onlineStateController.compare(e))
				{
					if (onlineStateController.nextAction ==
						NextAction.CreateGame)
					{
						onlineStateController.currentState =
							GameState.LoggingInGame;

						List<TBPvPExtremeManager.tbPvPEventCode> expected =
							new List<TBPvPExtremeManager.tbPvPEventCode>();

						expected.Add(
							TBPvPExtremeManager.tbPvPEventCode.CreatedNewRoom
						);

						onlineStateController.expectedEvents = expected;

						List<TBPvPExtremeManager.tbPvPEventCode> possible =
							new List<TBPvPExtremeManager.tbPvPEventCode>();

						possible.Add(
							TBPvPExtremeManager.tbPvPEventCode.CreatedNewRoom
						);

						possible.Add(
							TBPvPExtremeManager.tbPvPEventCode.Disconnected
						);

						possible.Add(
							TBPvPExtremeManager.tbPvPEventCode.ErrorDisconnected
						);

						onlineStateController.possibleEvents =
							possible;

						pvpExtremeManager.CreateMyRoom(
							UserName
						);
					}
					else if (onlineStateController.nextAction ==
						NextAction.ConnectGame)
					{
						onlineStateController.currentState =
							GameState.JoiningGame;

						List<TBPvPExtremeManager.tbPvPEventCode> expected =
							new List<TBPvPExtremeManager.tbPvPEventCode>();

						expected.Add(
							TBPvPExtremeManager.tbPvPEventCode.JoinedToExist
						);

						expected.Add(
							TBPvPExtremeManager.tbPvPEventCode.NotFound
						);

						onlineStateController.expectedEvents =
							expected;

						List<TBPvPExtremeManager.tbPvPEventCode> possible =
							new List<TBPvPExtremeManager.tbPvPEventCode>();

						possible.Add(
							TBPvPExtremeManager.tbPvPEventCode.JoinedToExist
						);

						possible.Add(
							TBPvPExtremeManager.tbPvPEventCode.NotFound
						);

						possible.Add(
							TBPvPExtremeManager.tbPvPEventCode.Connected
						);

						possible.Add(
							TBPvPExtremeManager.tbPvPEventCode.Disconnected
						);

						possible.Add(
							TBPvPExtremeManager.tbPvPEventCode.ErrorDisconnected
						);

						onlineStateController.possibleEvents =
							possible;

						pvpExtremeManager.ConnectGame(
							TargetRoom
						);
					}
					else if (onlineStateController.nextAction ==
						NextAction.SearchGame)
					{
						onlineStateController.currentState =
							GameState.SearchingGame;

						List<TBPvPExtremeManager.tbPvPEventCode> expected =
							new List<TBPvPExtremeManager.tbPvPEventCode>();

						expected.Add(
							TBPvPExtremeManager.tbPvPEventCode.JoinedToExist
						);

						expected.Add(
							TBPvPExtremeManager.tbPvPEventCode.CreatedNewRoom
						);

						onlineStateController.expectedEvents =
							expected;

						List<TBPvPExtremeManager.tbPvPEventCode> possible =
							new List<TBPvPExtremeManager.tbPvPEventCode>();

						possible.Add(
							TBPvPExtremeManager.tbPvPEventCode.JoinedToExist
						);

						possible.Add(
							TBPvPExtremeManager.tbPvPEventCode.CreatedNewRoom
						);

						possible.Add(
							TBPvPExtremeManager.tbPvPEventCode.NotFound
						);

						possible.Add(
							TBPvPExtremeManager.tbPvPEventCode.Disconnected
						);

						possible.Add(
							TBPvPExtremeManager.tbPvPEventCode.ErrorDisconnected
						);

						onlineStateController.possibleEvents =
							possible;

						pvpExtremeManager.SearchGame(
							Mylevel,
							TargetRange,
							TargetRange2nd,
							TargetRange3rd
						);
					}
					else
					{
						if (targetCallback != null)
						{
							targetCallback(
								OnlineExtremePvPEventCode.Error,
								null
							);
						}
					}

					break;
				}

				switch (e)
				{
					case TBPvPExtremeManager.tbPvPEventCode.Disabled:

						if (targetCallback != null)
						{
							targetCallback(
								OnlineExtremePvPEventCode.Disabled,
								null
							);
						}

						break;

					case TBPvPExtremeManager.tbPvPEventCode.ErrorDisconnected:

						if (targetCallback != null)
						{
							targetCallback(
								OnlineExtremePvPEventCode.Error,
								null
							);
						}

						break;
				}

				break;

			case GameState.SearchingGame:

				if (onlineStateController.compare(e))
				{
					switch (e)
					{
						case TBPvPExtremeManager.tbPvPEventCode.CreatedNewRoom:

							if (targetCallback != null)
							{
								onlineStateController.currentState =
									GameState.HostingGame;

								targetCallback(
									OnlineExtremePvPEventCode.CreatedNewGame,
									obj
								);

								List<TBPvPExtremeManager.tbPvPEventCode> expected =
									new List<TBPvPExtremeManager.tbPvPEventCode>();

								expected.Add(
									TBPvPExtremeManager.tbPvPEventCode.NewPlayerJoined
								);

								onlineStateController.expectedEvents =
									expected;

								List<TBPvPExtremeManager.tbPvPEventCode> possible =
									new List<TBPvPExtremeManager.tbPvPEventCode>();

								possible.Add(
									TBPvPExtremeManager.tbPvPEventCode.NewPlayerJoined
								);

								possible.Add(
									TBPvPExtremeManager.tbPvPEventCode.CreatedNewRoom
								);

								possible.Add(
									TBPvPExtremeManager.tbPvPEventCode.Disconnected
								);

								possible.Add(
									TBPvPExtremeManager.tbPvPEventCode.ErrorDisconnected
								);

								onlineStateController.possibleEvents =
									possible;
							}

							break;

						case TBPvPExtremeManager.tbPvPEventCode.JoinedToExist:

							if (pvpExtremeManager.GameClientInstance != null &&
								pvpExtremeManager.GameClientInstance.CurrentRoom != null)
							{
								CurrentGameRoom =
									pvpExtremeManager.GameClientInstance
										.CurrentRoom.Name;
							}

							MyPlayerID = 2;

							if (targetCallback != null)
							{
								onlineStateController.currentState =
									GameState.InGameClient;

								targetCallback(
									OnlineExtremePvPEventCode.JoinedToExist,
									obj
								);

								List<TBPvPExtremeManager.tbPvPEventCode> expected =
									new List<TBPvPExtremeManager.tbPvPEventCode>();

								expected.Add(
									TBPvPExtremeManager.tbPvPEventCode.IncomingMessages
								);

								expected.Add(
									TBPvPExtremeManager.tbPvPEventCode.PlayerLeft
								);

								onlineStateController.expectedEvents =
									expected;

								List<TBPvPExtremeManager.tbPvPEventCode> possible =
									new List<TBPvPExtremeManager.tbPvPEventCode>();

								possible.Add(
									TBPvPExtremeManager.tbPvPEventCode.IncomingMessages
								);

								possible.Add(
									TBPvPExtremeManager.tbPvPEventCode.PlayerLeft
								);

								possible.Add(
									TBPvPExtremeManager.tbPvPEventCode.Disconnected
								);

								possible.Add(
									TBPvPExtremeManager.tbPvPEventCode.ErrorDisconnected
								);

								onlineStateController.possibleEvents =
									possible;
							}

							break;
					}
				}
				else if (
					e == TBPvPExtremeManager.tbPvPEventCode.ErrorDisconnected &&
					targetCallback != null)
				{
					targetCallback(
						OnlineExtremePvPEventCode.Error,
						null
					);
				}

				break;

			case GameState.LoggingInGame:

				if (onlineStateController.compare(e))
				{
					if (
						e ==
						TBPvPExtremeManager.tbPvPEventCode.CreatedNewRoom &&
						targetCallback != null)
					{
						onlineStateController.currentState =
							GameState.HostingGame;

						targetCallback(
							OnlineExtremePvPEventCode.CreatedNewGame,
							obj
						);

						List<TBPvPExtremeManager.tbPvPEventCode> expected =
							new List<TBPvPExtremeManager.tbPvPEventCode>();

						expected.Add(
							TBPvPExtremeManager.tbPvPEventCode.NewPlayerJoined
						);

						onlineStateController.expectedEvents =
							expected;

						List<TBPvPExtremeManager.tbPvPEventCode> possible =
							new List<TBPvPExtremeManager.tbPvPEventCode>();

						possible.Add(
							TBPvPExtremeManager.tbPvPEventCode.NewPlayerJoined
						);

						possible.Add(
							TBPvPExtremeManager.tbPvPEventCode.CreatedNewRoom
						);

						possible.Add(
							TBPvPExtremeManager.tbPvPEventCode.Disconnected
						);

						possible.Add(
							TBPvPExtremeManager.tbPvPEventCode.ErrorDisconnected
						);

						onlineStateController.possibleEvents =
							possible;
					}
				}
				else if (
					(e ==
						TBPvPExtremeManager.tbPvPEventCode.ErrorDisconnected ||
					 e ==
						TBPvPExtremeManager.tbPvPEventCode.Disconnected) &&
					targetCallback != null)
				{
					targetCallback(
						OnlineExtremePvPEventCode.Error,
						null
					);
				}

				break;

			case GameState.JoiningGame:

				if (onlineStateController.compare(e))
				{
					switch (e)
					{
						case TBPvPExtremeManager.tbPvPEventCode.JoinedToExist:

							if (pvpExtremeManager.GameClientInstance != null)
							{
								CurrentGameRoom =
									pvpExtremeManager.GameClientInstance.RoomName;
							}

							MyPlayerID = 2;

							if (targetCallback != null)
							{
								onlineStateController.currentState =
									GameState.InGameClient;

								targetCallback(
									OnlineExtremePvPEventCode.JoinedToExist,
									obj
								);

								List<TBPvPExtremeManager.tbPvPEventCode> expected =
									new List<TBPvPExtremeManager.tbPvPEventCode>();

								expected.Add(
									TBPvPExtremeManager.tbPvPEventCode.IncomingMessages
								);

								expected.Add(
									TBPvPExtremeManager.tbPvPEventCode.PlayerLeft
								);

								onlineStateController.expectedEvents =
									expected;

								List<TBPvPExtremeManager.tbPvPEventCode> possible =
									new List<TBPvPExtremeManager.tbPvPEventCode>();

								possible.Add(
									TBPvPExtremeManager.tbPvPEventCode.IncomingMessages
								);

								possible.Add(
									TBPvPExtremeManager.tbPvPEventCode.PlayerLeft
								);

								possible.Add(
									TBPvPExtremeManager.tbPvPEventCode.Disconnected
								);

								possible.Add(
									TBPvPExtremeManager.tbPvPEventCode.ErrorDisconnected
								);

								onlineStateController.possibleEvents =
									possible;
							}

							break;

						case TBPvPExtremeManager.tbPvPEventCode.NotFound:

							if (targetCallback != null)
							{
								targetCallback(
									OnlineExtremePvPEventCode.NotFound,
									obj
								);

								DeActivateOnlinePvP();
							}

							break;
					}
				}
				else if (
					(e ==
						TBPvPExtremeManager.tbPvPEventCode.ErrorDisconnected ||
					 e ==
						TBPvPExtremeManager.tbPvPEventCode.Disconnected) &&
					targetCallback != null)
				{
					targetCallback(
						OnlineExtremePvPEventCode.Error,
						null
					);
				}

				break;

			case GameState.HostingGame:

				if (onlineStateController.compare(e))
				{
					if (
						e ==
						TBPvPExtremeManager.tbPvPEventCode.NewPlayerJoined)
					{
						if (pvpExtremeManager.GameClientInstance != null)
						{
							CurrentGameRoom =
								pvpExtremeManager.GameClientInstance.RoomName;
						}

						MyPlayerID = 1;

						if (targetCallback != null)
						{
							onlineStateController.currentState =
								GameState.InGameHost;

							targetCallback(
								OnlineExtremePvPEventCode.NewPlayerJoined,
								obj
							);

							List<TBPvPExtremeManager.tbPvPEventCode> expected =
								new List<TBPvPExtremeManager.tbPvPEventCode>();

							expected.Add(
								TBPvPExtremeManager.tbPvPEventCode.IncomingMessages
							);

							expected.Add(
								TBPvPExtremeManager.tbPvPEventCode.PlayerLeft
							);

							onlineStateController.expectedEvents =
								expected;

							List<TBPvPExtremeManager.tbPvPEventCode> possible =
								new List<TBPvPExtremeManager.tbPvPEventCode>();

							possible.Add(
								TBPvPExtremeManager.tbPvPEventCode.IncomingMessages
							);

							possible.Add(
								TBPvPExtremeManager.tbPvPEventCode.PlayerLeft
							);

							possible.Add(
								TBPvPExtremeManager.tbPvPEventCode.ErrorDisconnected
							);

							onlineStateController.possibleEvents =
								possible;
						}
					}
				}
				else if (
					(e ==
						TBPvPExtremeManager.tbPvPEventCode.ErrorDisconnected ||
					 e ==
						TBPvPExtremeManager.tbPvPEventCode.Disconnected) &&
					targetCallback != null)
				{
					targetCallback(
						OnlineExtremePvPEventCode.Error,
						null
					);
				}

				break;

			case GameState.InGameHost:
			case GameState.InGameClient:

				if (onlineStateController.compare(e))
				{
					switch (e)
					{
						case TBPvPExtremeManager.tbPvPEventCode.IncomingMessages:
						{
							if (targetCallback == null)
								break;

							TBPvPExtremeManager.IncomingGameMessageObject incoming =
								obj as TBPvPExtremeManager.IncomingGameMessageObject;

							if (incoming == null)
							{
								targetCallback(
									OnlineExtremePvPEventCode.Error,
									null
								);
								break;
							}

							byte messageType =
								incoming.MessageType;

							if (messageType == HEARTBEAT_TYPE)
								break;

							if ((messageType & ACK_BIT) != 0)
							{
								int ackIndex =
									Convert.ToInt32(
										messageType & 127
									);

								if (CurrentMessageSendIndex ==
									ackIndex)
								{
									targetCallback(
										OnlineExtremePvPEventCode.SendSuccess,
										null
									);
								}
								else
								{
									targetCallback(
										OnlineExtremePvPEventCode.Error,
										null
									);
								}

								WaitingAck = false;
								break;
							}

							int incomingIndex =
								Convert.ToInt32(messageType);

							int nextIndex =
								CurrentMessageRecvIndex + 1;

							if (nextIndex >= IndexRoundOver)
								nextIndex = 1;

							if (incomingIndex ==
								CurrentMessageRecvIndex)
							{
								SendMessageACK(
									Convert.ToByte(incomingIndex)
								);
							}
							else if (incomingIndex ==
								nextIndex)
							{
								SendMessageACK(
									Convert.ToByte(incomingIndex)
								);

								CurrentMessageRecvIndex =
									incomingIndex;

								targetCallback(
									OnlineExtremePvPEventCode.IncomingMessages,
									obj
								);
							}
							else
							{
								targetCallback(
									OnlineExtremePvPEventCode.Error,
									null
								);
							}

							break;
						}

						case TBPvPExtremeManager.tbPvPEventCode.PlayerLeft:

							if (targetCallback != null)
							{
								targetCallback(
									OnlineExtremePvPEventCode.PlayerLeft,
									obj
								);
							}

							DeActivateOnlinePvP();

							break;

						case TBPvPExtremeManager.tbPvPEventCode.PropertiesChanged:

							if (targetCallback != null)
							{
								targetCallback(
									OnlineExtremePvPEventCode.PropertiesChanged,
									obj
								);
							}

							break;
					}
				}
				else if (
					(e ==
						TBPvPExtremeManager.tbPvPEventCode.ErrorDisconnected ||
					 e ==
						TBPvPExtremeManager.tbPvPEventCode.Disconnected) &&
					targetCallback != null)
				{
					targetCallback(
						OnlineExtremePvPEventCode.Error,
						null
					);
				}

				break;

			case GameState.LeavingHost:
				break;

			case GameState.LeavingClient:
				break;

			case GameState.LeavingServer:

				if (onlineStateController.compare(e))
				{
					if (
						e ==
						TBPvPExtremeManager.tbPvPEventCode.Disconnected &&
						targetCallback != null)
					{
						onlineStateController.currentState =
							GameState.None;

						targetCallback(
							OnlineExtremePvPEventCode.Disconnected,
							obj
						);
					}
				}
				else if (
					e ==
					TBPvPExtremeManager.tbPvPEventCode.ErrorDisconnected &&
					targetCallback != null)
				{
					targetCallback(
						OnlineExtremePvPEventCode.Error,
						null
					);
				}

				break;

			case GameState.ErrorState:
				break;

			case GameState.LoggingInServer:
				break;
		}
	}

    private void ActivateOnlinePvP(
        string name,
        bool ranked,
        OnlineExtremePvPEvent callback)
    {
        onlineStateController.currentState =
            GameState.LoggedInServer;

        List<TBPvPExtremeManager.tbPvPEventCode>
            expected =
                new List<TBPvPExtremeManager.tbPvPEventCode>();

        expected.Add(
            TBPvPExtremeManager.tbPvPEventCode.Connected
        );

        onlineStateController.expectedEvents =
            expected;

        List<TBPvPExtremeManager.tbPvPEventCode>
            possible =
                new List<TBPvPExtremeManager.tbPvPEventCode>();

        possible.Add(
            TBPvPExtremeManager.tbPvPEventCode.Connected
        );

        possible.Add(
            TBPvPExtremeManager.tbPvPEventCode.Disconnected
        );

        possible.Add(
            TBPvPExtremeManager.tbPvPEventCode.ErrorDisconnected
        );

        possible.Add(
            TBPvPExtremeManager.tbPvPEventCode.Disabled
        );

        onlineStateController.possibleEvents =
            possible;

        if (callback != null)
        {
            targetCallback = callback;
        }

        UserName = name;

        pvpExtremeManager.ConnectToServer(
            name,
            ranked,
            OnTBPvPCallback
        );
    }

    private void DeActivateOnlinePvP()
    {
        onlineStateController.currentState =
            GameState.LeavingServer;

        List<TBPvPExtremeManager.tbPvPEventCode>
            expected =
                new List<TBPvPExtremeManager.tbPvPEventCode>();

        expected.Add(
            TBPvPExtremeManager.tbPvPEventCode.Disconnected
        );

        onlineStateController.expectedEvents =
            expected;

        List<TBPvPExtremeManager.tbPvPEventCode>
            possible =
                new List<TBPvPExtremeManager.tbPvPEventCode>();

        possible.Add(
            TBPvPExtremeManager.tbPvPEventCode.Disconnected
        );

        possible.Add(
            TBPvPExtremeManager.tbPvPEventCode.ErrorDisconnected
        );

        onlineStateController.possibleEvents =
            possible;

        pvpExtremeManager.Disconnect();
    }

	public void CreateGame(
        string name,
        OnlineExtremePvPEvent callback)
    {
        ResetValue();

        onlineStateController.nextAction =
            NextAction.CreateGame;

        ActivateOnlinePvP(
            name,
            true,
            callback
        );
    }

    public void JoinGame(
        string targetName,
        string myName,
        OnlineExtremePvPEvent callback)
    {
        ResetValue();

        TargetRoom = targetName;

        UserName = myName;

        onlineStateController.nextAction =
            NextAction.ConnectGame;

        ActivateOnlinePvP(
            myName,
            true,
            callback
        );
    }

    public void SearchGame(
        string name,
        int level,
        int range,
        int range2nd,
        int range3rd,
        bool ranked,
        OnlineExtremePvPEvent callback)
    {
        ResetValue();

        Mylevel = level;

        TargetRange = range;

        TargetRange2nd = range2nd;

        TargetRange3rd = range3rd;

        UserName = name;

        onlineStateController.nextAction =
            NextAction.SearchGame;

        ActivateOnlinePvP(
            name,
            ranked,
            callback
        );
    }

    private void ResetValue()
    {
        NeedToResend = false;

        WaitingAck = false;

        WaitingAckTimeout = -1f;

        CurrentMessageSendIndex = 1;

        CurrentMessageRecvIndex = 1;

        HeartBeat = MiscParams.PvPHeartBeat;

        ResendDelay = MiscParams.PvPResendDelay;

        LastMessage = string.Empty;
    }

    public void RestartGame()
    {
        ResetValue();

        if (MyPlayerID == 1)
        {
            onlineStateController.nextAction =
                NextAction.CreateGame;

            ActivateOnlinePvP(
                CurrentGameRoom,
                true,
                null
            );

            ReconnectStart =
                DateTime.Now;
        }
        else if (MyPlayerID == 2)
        {
            TargetRoom =
                CurrentGameRoom;

            onlineStateController.nextAction =
                NextAction.ConnectGame;

            ActivateOnlinePvP(
                UserName,
                true,
                null
            );

            ReconnectStart =
                DateTime.Now;
        }
    }

	public void LeaveGame()
	{
		DeActivateOnlinePvP();
	}

	public void SendMessage(
		string message,
		bool keepIt = true)
	{
		Debug.Log(
			"[EXTREME MANAGER] SendMessage State = " +
			onlineStateController.currentState
		);

		Debug.Log(
			"[EXTREME MANAGER] Message = " +
			message
		);

		if (
			onlineStateController.currentState !=
				GameState.InGameHost &&
			onlineStateController.currentState !=
				GameState.InGameClient)
		{
			Debug.LogError(
				"[EXTREME MANAGER] SendMessage BLOCKED! State = " +
				onlineStateController.currentState
			);

			return;
		}

		CurrentMessageSendIndex++;

		if (CurrentMessageSendIndex >= 16)
		{
			CurrentMessageSendIndex = 1;
		}

		pvpExtremeManager.SendMessage(
			Convert.ToByte(CurrentMessageSendIndex),
			message
		);

		NeedToResend = false;

		if (keepIt)
		{
			LastMessage = message;
		}

		if (CurrentMessageSendIndex > 2)
		{
			WaitingAck = true;

			WaitingAckTimeout =
				MiscParams.PvPMessageAckTimeout;
		}
	}

	private void SendMessageACK(byte type)
    {
        pvpExtremeManager.SendMessage(
            Convert.ToByte(type + ACK_BIT),
            string.Empty
        );
    }

	public void ReSendMessage()
    {
        NeedToResend = true;

        ResendDelay =
            MiscParams.PvPResendDelay;
    }

    private void SendMessageHeatBeat()
    {
        pvpExtremeManager.SendMessage(
            HEARTBEAT_TYPE,
            string.Empty
        );

        HeartBeat =
            MiscParams.PvPHeartBeat;
    }

	public void Reset()
	{
		DeActivateOnlinePvP();
	}

    public string GetMyCountryCode()
    {
        return pvpExtremeManager.GetCountryCode(0);
    }

    public string GetOpponentCountryCode()
    {
        return pvpExtremeManager.GetCountryCode(1);
    }

    public string GetMyCountryTexture()
    {
        return Singleton<CountryFlagManager>
            .Instance
            .GetMyFlag();
    }

    public void SetFriendFilter(string myFilter)
    {
        pvpExtremeManager
            .GameClientInstance
            .myFilterName = myFilter;
    }

    private void OnTBPvPCallback(
        TBPvPExtremeManager.tbPvPEventCode code,
        object obj)
    {
        if (targetCallback == null)
            return;

        switch (code)
        {
            case TBPvPExtremeManager.tbPvPEventCode.Connected:
                targetCallback(
                    OnlineExtremePvPEventCode.Connected,
                    obj
                );
                break;

            case TBPvPExtremeManager.tbPvPEventCode.Disconnected:
                targetCallback(
                    OnlineExtremePvPEventCode.Disconnected,
                    obj
                );
                break;

            case TBPvPExtremeManager.tbPvPEventCode.ErrorDisconnected:
                targetCallback(
                    OnlineExtremePvPEventCode.Error,
                    obj
                );
                break;

            case TBPvPExtremeManager.tbPvPEventCode.JoinedToExist:
                targetCallback(
                    OnlineExtremePvPEventCode.JoinedToExist,
                    obj
                );
                break;

            case TBPvPExtremeManager.tbPvPEventCode.NotFound:
                targetCallback(
                    OnlineExtremePvPEventCode.NotFound,
                    obj
                );
                break;

            case TBPvPExtremeManager.tbPvPEventCode.NewPlayerJoined:
                targetCallback(
                    OnlineExtremePvPEventCode.NewPlayerJoined,
                    obj
                );
                break;

            case TBPvPExtremeManager.tbPvPEventCode.CreatedNewRoom:
                targetCallback(
                    OnlineExtremePvPEventCode.CreatedNewGame,
                    obj
                );
                break;

            case TBPvPExtremeManager.tbPvPEventCode.IncomingMessages:
                targetCallback(
                    OnlineExtremePvPEventCode.IncomingMessages,
                    obj
                );
                break;

            case TBPvPExtremeManager.tbPvPEventCode.PlayerLeft:
                targetCallback(
                    OnlineExtremePvPEventCode.PlayerLeft,
                    obj
                );
                break;

            case TBPvPExtremeManager.tbPvPEventCode.PropertiesChanged:
                targetCallback(
                    OnlineExtremePvPEventCode.PropertiesChanged,
                    obj
                );
                break;

            case TBPvPExtremeManager.tbPvPEventCode.Disabled:
                targetCallback(
                    OnlineExtremePvPEventCode.Disabled,
                    obj
                );
                break;
        }
    }
}