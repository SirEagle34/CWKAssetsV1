using DW_Leaderboards;
using UnityEngine;

public class PvPMatchEndHandler : MonoBehaviour
{
	public LeaderboardManager leaderboardManager;

	public void HandleMatchEnd(string opponentPlayerID, bool didIWin)
	{
		if (SessionManager.Instance == null || SessionManager.Instance.theSession == null)
		{
			Debug.LogError("Oturum veya SessionManager bulunamadı! Maç sonucu sunucuya gönderilemiyor.");
			return;
		}
		Session theSession = SessionManager.Instance.theSession;
		string playerID = SessionManager.Instance.PlayerID;
		string currentSeasonID = "Season_1";
		Debug.Log($"Maç sonucu gönderiliyor... Oyuncu: {playerID}, Galibiyet: {didIWin}, Rakip: {opponentPlayerID}");
		DW_Leaderboard.RegisterMatchResult(theSession, playerID, currentSeasonID, opponentPlayerID, didIWin, delegate(int newScore, ResponseFlag flag)
		{
			if (flag == ResponseFlag.Success)
			{
				Debug.Log($"Maç sonucu sunucuya işlendi! Yeni PvP Puanınız: {newScore}");
				if (leaderboardManager != null)
				{
					Debug.Log("Liderlik tablosu güncelleniyor...");
					leaderboardManager.LoadLeaderboard();
				}
			}
			else
			{
				Debug.LogError("Maç sonucu sunucuya gönderilirken hata oluştu.");
			}
		});
	}
}
