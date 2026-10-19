using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public static class CardBlocklist
{
	[Serializable]
	private class Response
	{
		public string[] banned_cards;

		public string[] blocked_cards;
	}

	private static HashSet<string> sPvp = new HashSet<string>();

	private static HashSet<string> sAll = new HashSet<string>();

	public static bool Any
	{
		get
		{
			if (sPvp.Count <= 0)
			{
				return sAll.Count > 0;
			}
			return true;
		}
	}

	public static IEnumerator Fetch()
	{
		using UnityWebRequest request = UnityWebRequest.Get(SQSettings.SERVER_URL + "persist/static/pvp_banlist");
		request.timeout = 8;
		yield return request.SendWebRequest();
		if (request.result != UnityWebRequest.Result.Success)
		{
			Debug.LogWarning("[CardBlocklist] fetch failed: " + request.error + " (keeping " + sAll.Count + "+" + sPvp.Count + ")");
			yield break;
		}
		Response response = null;
		try
		{
			response = JsonUtility.FromJson<Response>(request.downloadHandler.text);
		}
		catch (Exception ex)
		{
			Debug.LogWarning("[CardBlocklist] bad reply: " + ex.Message);
		}
		if (response != null)
		{
			sPvp = new HashSet<string>(response.banned_cards ?? new string[0]);
			sAll = new HashSet<string>(response.blocked_cards ?? new string[0]);
			Debug.Log("[CardBlocklist] blocked everywhere " + sAll.Count + ", in PvP " + sPvp.Count);
		}
	}

	public static bool IsBlocked(CardData card)
	{
		if (card == null || !Any)
		{
			return false;
		}
		if (!sAll.Contains(card.ID))
		{
			if (sPvp.Contains(card.ID))
			{
				return Singleton<PlayerInfoScript>.Instance.StateData.MultiplayerMode;
			}
			return false;
		}
		return true;
	}

	public static string BlockedText()
	{
		string text = KFFLocalization.Get("!!CARD_BLOCKED");
		if (!string.IsNullOrEmpty(text) && !text.Contains("CARD_BLOCKED"))
		{
			return text;
		}
		return "This card is blocked right now.";
	}
}
