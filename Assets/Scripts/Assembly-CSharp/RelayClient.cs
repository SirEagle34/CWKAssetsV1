using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using MiniJSON;
using UnityEngine;

public class RelayClient
{
	private const int MaxFrameBytes = 262144;

	private const int ConnectTimeoutMs = 10000;

	public TBPvPManager photonManager;

	public TBPvPManager.TBPvPEvent pvpEventCallback;

	public string RoomName = string.Empty;

	public int PlayerNr;

	public string OpponentNickName = string.Empty;

	private TcpClient mClient;

	private NetworkStream mStream;

	private Thread mReader;

	private volatile bool mRunning;

	private volatile bool mConnected;

	private readonly Queue<Dictionary<string, object>> mInbox = new Queue<Dictionary<string, object>>();

	private readonly object mInboxLock = new object();

	private readonly object mSendLock = new object();

	private volatile bool mFaulted;

	private string mHost;

	private int mPort;

	private bool mHosting;

	private int mAttempts;

	private int mLevel;

	public string AppId { get; set; }

	public string AppVersion { get; set; }

	public string NickName { get; set; }

	public bool IsConnected => mConnected;

	public void init()
	{
		RoomName = string.Empty;
		PlayerNr = 0;
		OpponentNickName = string.Empty;
		mHosting = false;
		mFaulted = false;
		lock (mInboxLock)
		{
			mInbox.Clear();
		}
	}

	public bool ConnectToRegionMaster(string region)
	{
		Disconnect();
		mHost = SQSettings.RELAY_HOST;
		mPort = SQSettings.RELAY_PORT;
		if (string.IsNullOrEmpty(mHost))
		{
			Debug.LogError("[Relay] no relay_host in server_settings.json");
			return false;
		}
		try
		{
			mClient = new TcpClient();
			IAsyncResult asyncResult = mClient.BeginConnect(mHost, mPort, null, null);
			if (!asyncResult.AsyncWaitHandle.WaitOne(10000))
			{
				mClient.Close();
				Debug.LogError("[Relay] connect timed out to " + mHost + ":" + mPort);
				return false;
			}
			mClient.EndConnect(asyncResult);
			mClient.NoDelay = true;
			mStream = mClient.GetStream();
			mRunning = true;
			mConnected = true;
			mReader = new Thread(ReadLoop);
			mReader.IsBackground = true;
			mReader.Start();
			Dictionary<string, object> dictionary = new Dictionary<string, object>();
			dictionary["op"] = "hello";
			dictionary["nick"] = NickName ?? string.Empty;
			dictionary["appVersion"] = AppVersion ?? string.Empty;
			Send(dictionary);
			return true;
		}
		catch (Exception ex)
		{
			Debug.LogError("[Relay] connect failed: " + ex.Message);
			Cleanup();
			return false;
		}
	}

	public void Disconnect()
	{
		if (mConnected)
		{
			Dictionary<string, object> dictionary = new Dictionary<string, object>();
			dictionary["op"] = "leave";
			Send(dictionary);
		}
		Cleanup();
	}

	public void StopThread()
	{
		Cleanup();
	}

	private void Cleanup()
	{
		mRunning = false;
		mConnected = false;
		try
		{
			if (mStream != null)
			{
				mStream.Close();
			}
		}
		catch (Exception)
		{
		}
		try
		{
			if (mClient != null)
			{
				mClient.Close();
			}
		}
		catch (Exception)
		{
		}
		mStream = null;
		mClient = null;
		mReader = null;
	}

	private void Send(Dictionary<string, object> msg)
	{
		if (!mConnected || mStream == null)
		{
			return;
		}
		try
		{
			byte[] bytes = Encoding.UTF8.GetBytes(Json.Serialize(msg));
			byte[] array = new byte[4 + bytes.Length];
			array[0] = (byte)((bytes.Length >> 24) & 0xFF);
			array[1] = (byte)((bytes.Length >> 16) & 0xFF);
			array[2] = (byte)((bytes.Length >> 8) & 0xFF);
			array[3] = (byte)(bytes.Length & 0xFF);
			Buffer.BlockCopy(bytes, 0, array, 4, bytes.Length);
			lock (mSendLock)
			{
				mStream.Write(array, 0, array.Length);
				mStream.Flush();
			}
		}
		catch (Exception ex)
		{
			Debug.LogWarning("[Relay] send failed: " + ex.Message);
			mFaulted = true;
			mConnected = false;
		}
	}

	private void ReadLoop()
	{
		byte[] array = new byte[4];
		try
		{
			while (mRunning && ReadExactly(array, 4))
			{
				int num = (array[0] << 24) | (array[1] << 16) | (array[2] << 8) | array[3];
				if (num <= 0 || num > 262144)
				{
					Debug.LogWarning("[Relay] bad frame length " + num);
					break;
				}
				byte[] array2 = new byte[num];
				if (!ReadExactly(array2, num))
				{
					break;
				}
				if (Json.Deserialize(Encoding.UTF8.GetString(array2)) is Dictionary<string, object> item)
				{
					lock (mInboxLock)
					{
						mInbox.Enqueue(item);
					}
				}
			}
		}
		catch (Exception ex)
		{
			if (mRunning)
			{
				Debug.LogWarning("[Relay] read loop ended: " + ex.Message);
			}
		}
		finally
		{
			mConnected = false;
			mFaulted = true;
		}
	}

	private bool ReadExactly(byte[] buffer, int count)
	{
		int num;
		for (int i = 0; i < count; i += num)
		{
			if (mStream == null)
			{
				return false;
			}
			num = mStream.Read(buffer, i, count - i);
			if (num <= 0)
			{
				return false;
			}
		}
		return true;
	}

	public void Service()
	{
		while (true)
		{
			Dictionary<string, object> dictionary = null;
			lock (mInboxLock)
			{
				if (mInbox.Count > 0)
				{
					dictionary = mInbox.Dequeue();
				}
			}
			if (dictionary == null)
			{
				break;
			}
			Dispatch(dictionary);
		}
		if (mFaulted)
		{
			mFaulted = false;
			Cleanup();
			Fire(TBPvPManager.tbPvPEventCode.ErrorDisconnected, null);
		}
	}

	private void Dispatch(Dictionary<string, object> msg)
	{
		switch (GetString(msg, "ev"))
		{
		case "hello":
			Fire(TBPvPManager.tbPvPEventCode.Connected, null);
			break;
		case "joined":
		{
			RoomName = GetString(msg, "room");
			PlayerNr = GetInt(msg, "playerNr");
			Dictionary<string, object> dictionary2 = Get(msg, "peer") as Dictionary<string, object>;
			OpponentNickName = ((dictionary2 != null) ? GetString(dictionary2, "nick") : string.Empty);
			if (mHosting)
			{
				mHosting = false;
				Fire(TBPvPManager.tbPvPEventCode.CreatedNewRoom, null);
			}
			else
			{
				Fire(TBPvPManager.tbPvPEventCode.JoinedToExist, string.IsNullOrEmpty(OpponentNickName) ? null : OpponentNickName);
			}
			break;
		}
		case "noMatch":
			Fire(TBPvPManager.tbPvPEventCode.NotFound, null);
			break;
		case "playerJoined":
			OpponentNickName = GetString(msg, "nick");
			Fire(TBPvPManager.tbPvPEventCode.NewPlayerJoined, OpponentNickName);
			break;
		case "playerLeft":
			Fire(TBPvPManager.tbPvPEventCode.PlayerLeft, null);
			break;
		case "data":
		{
			if (!(Get(msg, "data") is Dictionary<string, object> dictionary))
			{
				break;
			}
			List<TBPvPManager.IncomingGameMessageObject> list = new List<TBPvPManager.IncomingGameMessageObject>();
			foreach (KeyValuePair<string, object> item in dictionary)
			{
				if (byte.TryParse(item.Key, out var result))
				{
					TBPvPManager.IncomingGameMessageObject incomingGameMessageObject = new TBPvPManager.IncomingGameMessageObject();
					incomingGameMessageObject.MessageType = result;
					incomingGameMessageObject.Message = ((item.Value != null) ? item.Value.ToString() : string.Empty);
					list.Add(incomingGameMessageObject);
				}
			}
			if (list.Count > 0)
			{
				Fire(TBPvPManager.tbPvPEventCode.IncomingMessages, list);
			}
			break;
		}
		case "error":
			Debug.LogWarning("[Relay] server error: " + GetString(msg, "msg"));
			break;
		}
	}

	private void Fire(TBPvPManager.tbPvPEventCode code, object payload)
	{
		if (pvpEventCallback != null)
		{
			pvpEventCallback(code, payload);
		}
	}

	public bool JoinRandomRoom(int mylevel, int range, int range2nd, int range3rd, bool firsttime = true)
	{
		int num = (firsttime ? range : ((mAttempts != 1) ? range3rd : range2nd));
		if (firsttime)
		{
			mAttempts = 0;
		}
		int num2 = mylevel - num;
		int num3 = mylevel + num;
		if (num2 < 1)
		{
			num2 = 1;
		}
		mLevel = mylevel;
		Dictionary<string, object> dictionary = new Dictionary<string, object>();
		dictionary["op"] = "joinRandom";
		dictionary["lobby"] = "myLobby";
		dictionary["min"] = num2;
		dictionary["max"] = num3;
		Send(dictionary);
		return true;
	}

	public bool JoinTheGame(string targetname)
	{
		Dictionary<string, object> dictionary = new Dictionary<string, object>();
		dictionary["op"] = "joinRoom";
		dictionary["name"] = targetname;
		Send(dictionary);
		RoomName = targetname;
		return true;
	}

	public void OpJoinRoom(string roomName, int actorNumber)
	{
		JoinTheGame(roomName);
	}

	public bool OpCreateRoom(string playerId)
	{
		mHosting = true;
		Dictionary<string, object> dictionary = new Dictionary<string, object>();
		dictionary["op"] = "createRoom";
		dictionary["lobby"] = "myLobby";
		dictionary["c0"] = mLevel;
		dictionary["name"] = playerId;
		dictionary["maxPlayers"] = 2;
		Send(dictionary);
		return true;
	}

	public void OpLeaveRoom(bool willReturn)
	{
		Dictionary<string, object> dictionary = new Dictionary<string, object>();
		dictionary["op"] = "leave";
		Send(dictionary);
		RoomName = string.Empty;
		PlayerNr = 0;
	}

	public void SendGameMessage(byte messageType, string messageBody)
	{
		Dictionary<string, object> dictionary = new Dictionary<string, object>();
		dictionary[messageType.ToString()] = messageBody;
		Dictionary<string, object> dictionary2 = new Dictionary<string, object>();
		dictionary2["op"] = "raise";
		dictionary2["data"] = dictionary;
		Send(dictionary2);
	}

	private static object Get(Dictionary<string, object> d, string key)
	{
		if (d == null || !d.TryGetValue(key, out var value))
		{
			return null;
		}
		return value;
	}

	private static string GetString(Dictionary<string, object> d, string key)
	{
		object obj = Get(d, key);
		if (obj == null)
		{
			return string.Empty;
		}
		return obj.ToString();
	}

	private static int GetInt(Dictionary<string, object> d, string key)
	{
		object obj = Get(d, key);
		if (obj == null)
		{
			return 0;
		}
		if (obj is long)
		{
			return (int)(long)obj;
		}
		if (obj is double)
		{
			return (int)(double)obj;
		}
		if (!int.TryParse(obj.ToString(), out var result))
		{
			return 0;
		}
		return result;
	}
}
