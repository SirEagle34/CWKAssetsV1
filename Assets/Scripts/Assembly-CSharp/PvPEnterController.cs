using Multiplayer;

public class PvPEnterController : Singleton<PvPEnterController>
{
	private enum States
	{
		WAITING = 0,
		STATUS_READY = 1,
		USER_READY = 2,
		RANK_READY = 3,
		SCREEN_READY = 4
	}

	public UITweenController PvPMainScreenShow;

	private MultiplayerData mMultiplayerData;

	private ResponseFlag mResponseFlag;

	private string mMultiplyerUserName;

	private string mMultiplyerUserRank;

	private States state;

	public void Populate()
	{
		UICamera.LockInput();
		if (SessionManager.Instance.IsReady())
		{
			StartMultiplayerActivation();
		}
	}

	private void StartMultiplayerActivation()
	{
		global::Multiplayer.Multiplayer.GetMultiplayerStatus(SessionManager.Instance.theSession, MultiplayerDataCallback);
	}

	private void MultiplayerDataCallback(MultiplayerData data, ResponseFlag flag)
	{
		mMultiplayerData = data;
		mResponseFlag = flag;
		state = States.STATUS_READY;
	}

	private void ProcessStatus()
	{
		PlayerInfoScript instance = Singleton<PlayerInfoScript>.Instance;
		if (mMultiplayerData == null)
		{
			if (mResponseFlag == ResponseFlag.None)
			{
				if (!string.IsNullOrEmpty(instance.SaveData.MultiplayerPlayerName))
				{
					CreateMultiplayerUser(mMultiplyerUserName);
				}
				else
				{
					UICamera.ForceUnlockInput();
					Singleton<SimplePopupController>.Instance.ShowInput("My Name Is", OnNameInput, OnNameInputCancel);
				}
			}
			state = States.WAITING;
		}
		else
		{
			if ((bool)instance)
			{
				instance.PvPData.TotalTrophies = mMultiplayerData.trophies;
				instance.PvPData.PlayerName = mMultiplayerData.name;
				instance.SaveData.MultiplayerPlayerName = mMultiplayerData.name;
				instance.Save();
			}
			state = States.USER_READY;
		}
	}

	private void OnNameInput()
	{
		mMultiplyerUserName = Singleton<SimplePopupController>.Instance.GetInputValue();
		CreateMultiplayerUser(mMultiplyerUserName);
	}

	private void OnNameInputCancel()
	{
		Singleton<TownHudController>.Instance.ReturnToTownView();
	}

	private void CreateMultiplayerUser(string mp_name)
	{
		PlayerInfoScript instance = Singleton<PlayerInfoScript>.Instance;
		if (mp_name != string.Empty)
		{
			_ = string.Empty;
			instance.PvPSerialize();
			instance.SaveData.MultiplayerPlayerName = mp_name;
			instance.Save();
			global::Multiplayer.Multiplayer.CreateMultiplayerUser(SessionManager.Instance.theSession, instance, MultiplayerUserCreatedCallback);
		}
	}

	private void MultiplayerUserCreatedCallback(MultiplayerData data, ResponseFlag flag)
	{
		state = States.USER_READY;
		mMultiplayerData = data;
		mResponseFlag = flag;
	}

	private void ProcessUser()
	{
		if (mMultiplyerUserName != null)
		{
			PlayerInfoScript instance = Singleton<PlayerInfoScript>.Instance;
			instance.PvPData.PlayerName = mMultiplyerUserName;
			instance.Save();
		}
	}

	private void GetRank()
	{
		Session theSession = SessionManager.Instance.theSession;
		state = States.WAITING;
		global::Multiplayer.Multiplayer.GetRank(theSession, global: true, GetRankCallback);
	}

	private void GetRankCallback(string data, ResponseFlag flag)
	{
		state = States.RANK_READY;
		mMultiplyerUserRank = data;
		mResponseFlag = flag;
	}

	private void ProcessRank()
	{
		if (mResponseFlag == ResponseFlag.Success)
		{
			Singleton<PlayerInfoScript>.Instance.PvPData.PlayerRank = mMultiplyerUserRank;
		}
		state = States.SCREEN_READY;
	}

	private void Update()
	{
		switch (state)
		{
		case States.STATUS_READY:
			ProcessStatus();
			break;
		case States.USER_READY:
			ProcessUser();
			GetRank();
			break;
		case States.RANK_READY:
			ProcessRank();
			break;
		case States.SCREEN_READY:
			EnterPvPScreen();
			break;
		}
	}

	private void EnterPvPScreen()
	{
		state = States.WAITING;
	}

	public void UnlockInput()
	{
		UICamera.UnlockInput();
	}
}
