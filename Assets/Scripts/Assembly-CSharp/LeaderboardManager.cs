using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DW_Leaderboards;
using UnityEngine;

public class LeaderboardManager : MonoBehaviour
{
	public GameObject rowPrefab;

	public Transform contentParent;

	private List<LeaderboardPlayer> players = new List<LeaderboardPlayer>();

	private bool mShownOnce;

	private IEnumerator Start()
	{
		while (SessionManager.Instance == null || SessionManager.Instance.theSession == null)
		{
			yield return null;
		}
		LoadLeaderboard();
	}

	public void LoadLeaderboard()
	{
		Session theSession = SessionManager.Instance.theSession;
		if (theSession == null)
		{
			Debug.LogError("Session null");
		}
		else
		{
			DW_Leaderboard.FetchLeaderboardsEntries(theSession, 1, 30, OnLeaderboardReceived);
		}
	}

	private void OnLeaderboardReceived(List<LeaderboardData> data, ResponseFlag flag)
	{
		if (flag != ResponseFlag.Success || data == null)
		{
			return;
		}
		players.Clear();
		foreach (LeaderboardData datum in data)
		{
			if (datum != null)
			{
				players.Add(new LeaderboardPlayer
				{
					ranking = datum.ranking,
					playerId = datum.PlayerID,
					playerName = (string.IsNullOrEmpty(datum.PlayerName) ? "Anonymous" : datum.PlayerName),
					level = datum.MultiplayerLevel,
					points = datum.Points
				});
			}
		}
		players = players.OrderBy((LeaderboardPlayer x) => x.ranking).ToList();
		PopulateUI(players);
	}

	private void PopulateUI(List<LeaderboardPlayer> list)
	{
		foreach (Transform item in contentParent)
		{
			Object.Destroy(item.gameObject);
		}
		foreach (LeaderboardPlayer item2 in list)
		{
			GameObject obj = Object.Instantiate(rowPrefab, contentParent);
			obj.transform.localPosition = Vector3.zero;
			obj.transform.localScale = Vector3.one;
			LeaderboardRowNGUI component = obj.GetComponent<LeaderboardRowNGUI>();
			if (component != null)
			{
				bool isMe = SessionManager.Instance != null && item2.playerId == SessionManager.Instance.PlayerID;
				component.SetData(item2.ranking, "", item2.playerName, item2.level, item2.points, isMe);
			}
		}
		UIGrid component2 = contentParent.GetComponent<UIGrid>();
		if (component2 != null)
		{
			component2.cellHeight = 64f;
			component2.repositionNow = true;
		}
		UIScrollView uIScrollView = NGUITools.FindInParents<UIScrollView>(contentParent.gameObject);
		if (uIScrollView != null)
		{
			UIPanel uIPanel = NGUITools.FindInParents<UIPanel>(uIScrollView.transform.parent.gameObject);
			UIPanel component3 = uIScrollView.GetComponent<UIPanel>();
			if (uIPanel != null && component3 != null)
			{
				component3.depth = uIPanel.depth + 1;
			}
			if (component3 != null)
			{
				Vector3 localPosition = contentParent.localPosition;
				contentParent.localPosition = new Vector3(component3.baseClipRegion.x, localPosition.y, localPosition.z);
			}
			foreach (Transform item3 in contentParent)
			{
				(item3.GetComponent<UIDragScrollView>() ?? item3.gameObject.AddComponent<UIDragScrollView>()).scrollView = uIScrollView;
			}
		}
		if (base.isActiveAndEnabled && !mShownOnce)
		{
			mShownOnce = true;
			StartCoroutine(ScrollToTop());
		}
	}

	private IEnumerator ScrollToTop()
	{
		yield return null;
		UIGrid component = contentParent.GetComponent<UIGrid>();
		if (component != null)
		{
			component.Reposition();
		}
		UIScrollView uIScrollView = NGUITools.FindInParents<UIScrollView>(contentParent.gameObject);
		if (uIScrollView != null)
		{
			uIScrollView.ResetPosition();
		}
	}
}
