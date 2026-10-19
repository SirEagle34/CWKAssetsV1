using System.Collections.Generic;
using Multiplayer;
using UnityEngine;

public class PvPGlobalLeaderboardController : MonoBehaviour
{
	public GameObject PlayerLeaderboardTemplate;

	private int LeaderBoardMaxSize = 25;

	private AsyncData<List<LeaderboardData>> mAsyncGlobalLeaderboardData = new AsyncData<List<LeaderboardData>>();

	private AsyncData<List<LeaderboardData>> mAsyncLocalLeaderboardData = new AsyncData<List<LeaderboardData>>();

	private void OnEnable()
	{
		if (mAsyncGlobalLeaderboardData.processed)
		{
			global::Multiplayer.Multiplayer.TopLeaderboard(SessionManager.Instance.theSession, LeaderboardCallback);
		}
		if (mAsyncLocalLeaderboardData.processed)
		{
			global::Multiplayer.Multiplayer.NearbyLeaderboard(SessionManager.Instance.theSession, LocalLeaderboardCallback);
		}
	}

	public void LeaderboardCallback(List<LeaderboardData> data)
	{
		ResponseFlag a_flag = ResponseFlag.Success;
		mAsyncGlobalLeaderboardData.Set(a_flag, data);
	}

	public void LocalLeaderboardCallback(List<LeaderboardData> data)
	{
		ResponseFlag a_flag = ResponseFlag.Success;
		mAsyncLocalLeaderboardData.Set(a_flag, data);
	}

	private void Update()
	{
		if ((!mAsyncGlobalLeaderboardData.processed || !mAsyncLocalLeaderboardData.processed) && mAsyncGlobalLeaderboardData.MP_Data != null && mAsyncGlobalLeaderboardData.MP_Data.Count != 0)
		{
			BuildLeaderboard();
			mAsyncGlobalLeaderboardData.processed = true;
			mAsyncLocalLeaderboardData.processed = true;
		}
	}

	private void BuildLeaderboard()
	{
		UIStreamingGrid component = GetComponent<UIStreamingGrid>();
		if (component == null)
		{
			Debug.LogError("UIStreamingGrid missing");
			return;
		}
		ClearList(component);
		int num = 0;
		foreach (LeaderboardData mP_Datum in mAsyncGlobalLeaderboardData.MP_Data)
		{
			GameObject aLeaderboardItemObj = NGUITools.AddChild(component.gameObject, PlayerLeaderboardTemplate);
			fillItem(aLeaderboardItemObj, mP_Datum);
			if (++num >= LeaderBoardMaxSize)
			{
				break;
			}
		}
	}

	private void ClearList(UIStreamingGrid list)
	{
		if (!(list == null))
		{
			Transform transform = list.transform;
			for (int num = transform.childCount - 1; num >= 0; num--)
			{
				Object.Destroy(transform.GetChild(num).gameObject);
			}
		}
	}

	private void fillItem(GameObject aLeaderboardItemObj, object data)
	{
		PvPLeaderboardItem obj = aLeaderboardItemObj.GetComponentsInChildren<PvPLeaderboardItem>(includeInactive: true)[0];
		obj.gameObject.SetActive(value: true);
		LeaderboardData leaderboardData = data as LeaderboardData;
		obj.Name.text = leaderboardData.name;
		obj.Rank.text = leaderboardData.rank.ToString();
		obj.Score.text = leaderboardData.trophies.ToString();
	}
}
