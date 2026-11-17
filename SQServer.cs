using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

public class SQServer
{
	public const string SECRET_KEY = "xc0u18^g0!ac3k%0+2vgglmnr1)x^!o(n6@$m3t^(7l!(#kv!-";

	private const string CROOZ_SUPPORT_LOGIN = "api/GetLoginKey";

	private const string FB_LOGIN_URL = "account/fbTokenAuth/";

	private const string FB_CONNECT_URL_ANDROID = "account/fb_connect_android/";

	private const string FB_CONNECT_URL = "account/fb_connect/";

	private const string GC_LOGIN_URL = "account/gcAuth/";

	private const string PRE_AUTH_URL = "account/preAuth/";

	private const string CONFIRM_PURCHASE_URL = "billing/confirm_purchase/";

	private const string GET_UNCONFIRMED_PURCHASE_URL = "billing/get_unconfirmed_hard_currency/";

	private const string RECORD_PURCHASE_URL = "billing/record_purchase/";

	private const string GET_PURCHASES_URL = "billing/getPurchases/";

	private const string GET_STORE_PRODUCTS = "billing/iap_products/";

	private const string GET_SERVER_TIME = "time/";

	private const int MP_VERSION = 0;

	private const string POST_NEW_PLAYER_URL = "multiplayer/new_player/";

	private const string GET_PLAYER_URL = "multiplayer/player/";

	private const string POST_UPDATE_PLAYER_URL = "multiplayer/update_player/";

	private const string POST_UPDATE_DECK_URL = "multiplayer/update_deck_name/";

	private const string GET_PLAYER_RECORD_URL = "multiplayer/player_record/";

	private const string POST_PERSONAL_RECORD_URL = "multiplayer/record/";

	private const string POST_TOURNAMENT_PLAYER_RESULT = "multiplayer/tournament/player/";

	private const string TOURNAMENT_END_DATE = "multiplayer/tournament/expiration/";

	private const string GET_LEADERBOARD_URL = "multiplayer/active_leaderboard/";

	private const string GET_PUBLICLEADERBOARD_URL = "multiplayer/active_pvpleaderboard/";

	private const string POST_MATCHMAKE_URL = "multiplayer/matchmake/";

	private const string POST_TOURNAMENT_REDEMPTION = "multiplayer/tournament/complete/";

	private const string POST_REDEEMCODE_CHECK = "multiplayer/redeemcodeDW/";

	private const string GET_MONTHLY_STORE_URL = "persist/static/monthly_store";

	private const string BUY_MONTHLY_STORE_URL = "persist/static/monthly_store/buy";

	private const string POST_FRIEND_MYINFO_UPDATE_URL = "persist/friends_update_myinfoDW/";

	private const string POST_FRIEND_REQUEST_WITH_MYINFO_URL = "persist/friends_request_withmyinfoDW/";

	private const string POST_FRIEND_CONFIRM_WITH_MYINFO_URL = "persist/friends_confirm_request_withmyinfoDW/";

	private const string POST_FRIEND_FAKEREQESTRECEIVE_URL = "persist/friendsfakereq/";

	private const string POST_FRIEND_USE_FRIEND_URL = "persist/friends_use_friendDW/";

	private const string POST_FRIEND_USE_PLAYER_URL = "persist/friends_use_playerDW/";

	private const string POST_FRIEND_HELPER_LIST_URL = "persist/friends_find_candidatesDW/";

	private const string POST_FRIEND_GETINFO_URL = "persist/friends_informationDW/";

	private const string POST_USER_HISTORY_URL = "persist/user_history/";

	private const string POST_USER_HISTORY2_URL = "persist/user_history2/";

	private const string POST_USER_HISTORY_ANDROID_URL = "persist/user_history_android/";

	private const string POST_USER_ACTION2 = "persist/user_action2/";

	private const string POST_USER_GETCC = "persist/getcc/";

	public const string POST_PLACE_ME_LEADERBOARD = "dw_leaderboard/placeme/";

	public const string POST_REGISTER_RESULT = "dw_leaderboard/registerresult/";

	public const string POST_FETCH_ENTRIES = "dw_leaderboard/fetchentries/";

	public const string POST_HAS_ENDED = "dw_leaderboard/hasended/";

	private TFServer tfServer;

	private string nonce = string.Empty;

	public static string IAP_VERIFICATION_SERVER_URL => "http://dw-live.kffgames.com";

	public SQServer(CookieContainer cookies)
	{
		tfServer = new TFServer(cookies, SQSettings.PATCHING_FILE_LIMIT);
	}

	public void SetLoggedOut()
	{
		tfServer.ShortCircuitAllRequests();
	}

	public bool IsNetworkError(Dictionary<string, object> response)
	{
		if (response.ContainsKey("error"))
		{
			return "Network error".Equals(response["error"]);
		}
		return false;
	}

	public void PreAuth(TFServer.JsonResponseHandler callback)
	{
		GetToJSON(SQSettings.SERVER_URL + "account/preAuth/", callback);
	}

	public void GetToJSON(string url, TFServer.JsonResponseHandler callback)
	{
		tfServer.GetToJSON(url, callback);
	}

	public void GetBlueprint(string md5hash, TFServer.JsonResponseHandler callback)
	{
		Dictionary<string, object> dictionary = new Dictionary<string, object>();
		dictionary["hash"] = md5hash;
		tfServer.PostToJSON(SQSettings.CDN_URL + "blueprint", dictionary, callback);
	}

	public void GetPurchases(string playerId, TFServer.JsonResponseHandler callback)
	{
		Dictionary<string, object> dictionary = new Dictionary<string, object>();
		dictionary["playerId"] = playerId;
		dictionary["signature"] = SignDictionary(dictionary, string.Empty, "xc0u18^g0!ac3k%0+2vgglmnr1)x^!o(n6@$m3t^(7l!(#kv!-");
		tfServer.PostToJSON(SQSettings.SERVER_URL + "billing/getPurchases/", dictionary, callback);
	}

	public void GetUnconfirmedPurchases(string playerId, TFServer.JsonResponseHandler callback)
	{
		Dictionary<string, object> dictionary = new Dictionary<string, object>();
		dictionary["player_id"] = playerId;
		dictionary["signature"] = SignDictionary(dictionary, string.Empty, "xc0u18^g0!ac3k%0+2vgglmnr1)x^!o(n6@$m3t^(7l!(#kv!-");
		tfServer.PostToJSON(SQSettings.SERVER_URL + "billing/get_unconfirmed_hard_currency/", dictionary, callback);
	}

	public void SavePurchase(string store_id, string store, string sandbox, string partial, string bundle_id, string productId, string playerId, string receipt, TFServer.JsonResponseHandler callback)
	{
		Dictionary<string, object> dictionary = new Dictionary<string, object>();
		dictionary["player_id"] = playerId;
		dictionary["store_id"] = store_id;
		dictionary["language"] = partial;
		dictionary["store"] = store;
		dictionary["environment"] = sandbox;
		dictionary["bundle_id"] = bundle_id;
		dictionary["product_id"] = productId;
		dictionary["receipt"] = receipt;
		dictionary["signature"] = SignDictionary(dictionary, nonce, "xc0u18^g0!ac3k%0+2vgglmnr1)x^!o(n6@$m3t^(7l!(#kv!-");
		tfServer.PostToJSON(SQSettings.SERVER_URL + "billing/record_purchase/", dictionary, callback);
	}

	public void ConfirmPurchase(string playerId, string transaction_id, TFServer.JsonResponseHandler callback)
	{
		Dictionary<string, object> dictionary = new Dictionary<string, object>();
		dictionary["player_id"] = playerId;
		dictionary["transaction_id"] = transaction_id;
		dictionary["signature"] = SignDictionary(dictionary, nonce, "xc0u18^g0!ac3k%0+2vgglmnr1)x^!o(n6@$m3t^(7l!(#kv!-");
		tfServer.PostToJSON(IAP_VERIFICATION_SERVER_URL + "billing/confirm_purchase/", dictionary, callback);
	}

	public void GetStoreProducts(string store, TFServer.JsonResponseHandler callback)
	{
		Dictionary<string, object> dictionary = new Dictionary<string, object>();
		dictionary["signature"] = SignDictionary(dictionary, nonce, "xc0u18^g0!ac3k%0+2vgglmnr1)x^!o(n6@$m3t^(7l!(#kv!-");
		tfServer.PostToJSON(IAP_VERIFICATION_SERVER_URL + "billing/iap_products/" + store + "/", dictionary, callback);
	}

	public void FbLogin(string playerId, string fbAccessToken, string fbExpirationDate, string nonce, TFServer.JsonResponseHandler callback)
	{
		this.nonce = nonce;
		Dictionary<string, object> dictionary = new Dictionary<string, object>();
		dictionary["fb_access_token"] = fbAccessToken;
		dictionary["fb_expiration_date"] = 0;
		dictionary["gc_id"] = playerId;
		dictionary["signature"] = SignDictionary(dictionary, nonce, "xc0u18^g0!ac3k%0+2vgglmnr1)x^!o(n6@$m3t^(7l!(#kv!-");
		_ = Application.platform;
		if (Application.platform == RuntimePlatform.Android && MiscParams.AndroidUsesDifferentFBAccount)
		{
			tfServer.PostToJSON(SQSettings.SERVER_URL + "account/fb_connect_android/", dictionary, callback);
		}
		else
		{
			tfServer.PostToJSON(SQSettings.SERVER_URL + "account/fb_connect/", dictionary, callback);
		}
	}

	public void GcLogin(string playerId, string alias, string nonce, TFServer.JsonResponseHandler callback)
	{
		this.nonce = nonce;
		Dictionary<string, object> dictionary = new Dictionary<string, object>();
		dictionary["player_id"] = playerId;
		dictionary["alias"] = alias;
		dictionary["signature"] = SignDictionary(dictionary, nonce, "xc0u18^g0!ac3k%0+2vgglmnr1)x^!o(n6@$m3t^(7l!(#kv!-");
		tfServer.PostToJSON(SQSettings.SERVER_URL + "account/gcAuth/", dictionary, callback);
	}

	public void GetTime(TFServer.JsonResponseHandler handler)
	{
		tfServer.GetToJSON(SQSettings.SERVER_URL + "time/", handler);
	}

	public void MultiplayerPlayerInfo(string playerId, TFServer.JsonResponseHandler callback)
	{
		tfServer.GetToJSON(SQSettings.SERVER_URL + "multiplayer/player/" + playerId, callback);
	}

	public void MultiplayerNewPlayer(string name, string icon, string deck, float deckRank, string landscapes, string helpercreature, string leader, int leaderLevel, int maxLevel, int allyboxspace, TFServer.JsonResponseHandler callback)
	{
		Dictionary<string, object> dictionary = new Dictionary<string, object>();
		dictionary["player_id"] = Singleton<PlayerInfoScript>.Instance.GetPlayerCode();
		dictionary["name"] = name;
		dictionary["icon"] = icon;
		dictionary["deck"] = deck;
		dictionary["deck_rank"] = deckRank;
		dictionary["landscapes"] = landscapes;
		dictionary["helper_creature"] = helpercreature;
		dictionary["leader"] = leader;
		dictionary["leader_level"] = leaderLevel;
		dictionary["allyboxspace"] = allyboxspace;
		dictionary["level"] = maxLevel;
		dictionary["version"] = 0;
		dictionary["signature"] = SignDictionary(dictionary, nonce, "xc0u18^g0!ac3k%0+2vgglmnr1)x^!o(n6@$m3t^(7l!(#kv!-");
		tfServer.PostToJSON(SQSettings.SERVER_URL + "multiplayer/new_player/", dictionary, callback, ignoreEtag: true);
	}

	public void MultiplayerUpdateDeck(string name, string deck, int needUpdate, string landscapes, string helpercreature, string leader, int leaderLevel, int allyboxspace, TFServer.JsonResponseHandler callback)
	{
		Dictionary<string, object> dictionary = new Dictionary<string, object>();
		dictionary["player_id"] = Singleton<PlayerInfoScript>.Instance.GetPlayerCode();
		dictionary["name"] = name;
		dictionary["deck"] = deck;
		dictionary["deck_rank"] = needUpdate;
		dictionary["landscapes"] = landscapes;
		dictionary["helper_creature"] = helpercreature;
		dictionary["leader"] = leader;
		dictionary["leader_level"] = leaderLevel;
		dictionary["allyboxspace"] = allyboxspace;
		dictionary["version"] = 0;
		dictionary["signature"] = SignDictionary(dictionary, nonce, "xc0u18^g0!ac3k%0+2vgglmnr1)x^!o(n6@$m3t^(7l!(#kv!-");
		tfServer.PostToJSON(SQSettings.SERVER_URL + "multiplayer/update_deck_name/", dictionary, callback, ignoreEtag: true);
	}

	public void MultiplayerUpdatePlayer(string name, string icon, int maxLevel, TFServer.JsonResponseHandler callback)
	{
		Dictionary<string, object> dictionary = new Dictionary<string, object>();
		dictionary["name"] = name;
		dictionary["icon"] = icon;
		dictionary["level"] = maxLevel;
		dictionary["signature"] = SignDictionary(dictionary, nonce, "xc0u18^g0!ac3k%0+2vgglmnr1)x^!o(n6@$m3t^(7l!(#kv!-");
		tfServer.PostToJSON(SQSettings.SERVER_URL + "multiplayer/update_player/", dictionary, callback);
	}

	public void RedeemcodeCheck(
		string redeemcode,
		string version,
		string subject,
		string message,
		DateTime start_date,
		DateTime end_date,
		int soft_currency,
		int hard_currency,
		TFServer.JsonResponseHandler callback)
	{
		Dictionary<string, object> dictionary = new Dictionary<string, object>();

		dictionary["redeemcode"] = redeemcode;
		dictionary["version"] = 0;
		dictionary["subject"] = subject;
		dictionary["message"] = message;
		dictionary["start_date"] = start_date.ToString("u");
		dictionary["end_date"] = end_date.ToString("u");

		dictionary["soft_currency"] = soft_currency;
		dictionary["free_hard_currency"] = hard_currency;

		dictionary["signature"] = SignDictionary(
			dictionary,
			nonce,
			"xc0u18^g0!ac3k%0+2vgglmnr1)x^!o(n6@$m3t^(7l!(#kv!-"
		);

		tfServer.PostToJSON(
			SQSettings.SERVER_URL + "multiplayer/redeemcodeDW/",
			dictionary,
			callback
		);
	}

	public void MultiplayerExtendedRecord(string playerId, TFServer.JsonResponseHandler callback)
	{
		tfServer.GetToJSON(SQSettings.SERVER_URL + "multiplayer/player_record/" + playerId, callback);
	}

	public void MultiplayerLeaderboardPlayer(string playerId, TFServer.JsonResponseHandler callback)
	{
		tfServer.GetToJSON(SQSettings.SERVER_URL + "multiplayer/active_leaderboard/" + playerId, callback);
	}

	public void MultiplayerLeaderboardPublicPlayer(string playerId, TFServer.JsonResponseHandler callback)
	{
		tfServer.GetToJSON(SQSettings.SERVER_URL + "multiplayer/active_pvpleaderboard/" + playerId, callback);
	}

	public void MultiplayerLeaderboardTop(TFServer.JsonResponseHandler callback)
	{
		tfServer.GetToJSON(SQSettings.SERVER_URL + "multiplayer/active_leaderboard/", callback);
	}

	public void MultiplayerGetRank(string playerId, TFServer.JsonResponseHandler callback)
	{
		tfServer.GetToJSON(SQSettings.SERVER_URL + "multiplayer/active_leaderboard/rank/" + playerId, callback);
	}

	public void MultiplayerPersonalRecord(string target, TFServer.JsonResponseHandler callback)
	{
		tfServer.GetToJSON(SQSettings.SERVER_URL + "multiplayer/record/" + target, callback);
	}

	public void MultiplayerNotification(TFServer.JsonResponseHandler callback)
	{
		tfServer.PostToJSON(SQSettings.SERVER_URL + "multiplayer/record/recent/", new Dictionary<string, object>(), callback);
	}

	public void MultiplayerTournamentPlayerResult(TFServer.JsonResponseHandler callback)
	{
		tfServer.GetToJSON(SQSettings.SERVER_URL + "multiplayer/tournament/player/", callback);
	}

	public void MultiplayerFindMatch(int maxLevel, TFServer.JsonResponseHandler callback)
	{
		Dictionary<string, object> dictionary = new Dictionary<string, object>();
		dictionary["level"] = maxLevel;
		dictionary["version"] = 0;
		dictionary["signature"] = SignDictionary(dictionary, nonce, "xc0u18^g0!ac3k%0+2vgglmnr1)x^!o(n6@$m3t^(7l!(#kv!-");
		tfServer.PostToJSON(SQSettings.SERVER_URL + "multiplayer/matchmake/find/", dictionary, callback);
	}

	public void MultiplayerStartMatch(string matchId, float deckRank, string leader, int leaderLevel, TFServer.JsonStringHandler callback)
	{
		Dictionary<string, object> dictionary = new Dictionary<string, object>();
		dictionary["match_id"] = matchId;
		dictionary["deck_rank"] = deckRank;
		dictionary["leader"] = leader;
		dictionary["leader_level"] = leaderLevel;
		dictionary["version"] = 0;
		dictionary["signature"] = SignDictionary(dictionary, nonce, "xc0u18^g0!ac3k%0+2vgglmnr1)x^!o(n6@$m3t^(7l!(#kv!-");
		tfServer.PostToString(SQSettings.SERVER_URL + "multiplayer/matchmake/start/", dictionary, callback);
	}

	public void MultiplayerEndMatch(string matchId, bool loss, TFServer.JsonResponseHandler callback)
	{
		Dictionary<string, object> dictionary = new Dictionary<string, object>();
		dictionary["match_id"] = matchId;
		dictionary["loss"] = loss;
		dictionary["signature"] = SignDictionary(dictionary, nonce, "xc0u18^g0!ac3k%0+2vgglmnr1)x^!o(n6@$m3t^(7l!(#kv!-");
		tfServer.PostToJSON(SQSettings.SERVER_URL + "multiplayer/matchmake/complete/", dictionary, callback);
	}

	public void MultiplayerCheaterTournamentEnd(TFServer.JsonResponseHandler callback)
	{
		Dictionary<string, object> dictionary = new Dictionary<string, object>();
		dictionary["sync"] = true;
		dictionary["signature"] = SignDictionary(dictionary, nonce, "xc0u18^g0!ac3k%0+2vgglmnr1)x^!o(n6@$m3t^(7l!(#kv!-");
		tfServer.PostToJSON(SQSettings.SERVER_URL + "multiplayer/tournament/expiration/", dictionary, callback);
	}

	public void MultiplayerGetTournamentEnd(TFServer.JsonResponseHandler callback)
	{
		tfServer.GetToJSON(SQSettings.SERVER_URL + "multiplayer/tournament/expiration/", callback);
	}

	public void MultiplayerRedeemReward(int tournamentId, TFServer.JsonResponseHandler callback)
	{
		Dictionary<string, object> dictionary = new Dictionary<string, object>();
		dictionary["tournament_id"] = tournamentId;
		dictionary["signature"] = SignDictionary(dictionary, nonce, "xc0u18^g0!ac3k%0+2vgglmnr1)x^!o(n6@$m3t^(7l!(#kv!-");
		tfServer.PostToJSON(SQSettings.SERVER_URL + "multiplayer/tournament/complete/", dictionary, callback);
	}

	public void GetMonthlyStore(string playerId, TFServer.JsonResponseHandler callback)
	{
		tfServer.GetToJSON(SQSettings.SERVER_URL + "persist/static/monthly_store?player_id=" + playerId, callback);
	}

	public void BuyMonthlyStore(string playerId, string storeType, string offerId, TFServer.JsonResponseHandler callback)
	{
		Dictionary<string, object> dictionary = new Dictionary<string, object>();
		dictionary["player_id"] = playerId;
		dictionary["store_type"] = storeType;
		dictionary["offer_id"] = offerId;
		dictionary["v2"] = "1";
		string text = SQSettings.SERVER_URL + "persist/static/monthly_store/buy";
		Debug.Log("========================================");
		Debug.Log("[Monthly Store] BUY URL: " + text);
		Debug.Log("[Monthly Store] BUY player_id: " + playerId);
		Debug.Log("[Monthly Store] BUY store_type: " + storeType);
		Debug.Log("[Monthly Store] BUY offer_id: " + offerId);
		Debug.Log("========================================");
		tfServer.PostToJSON(text, dictionary, callback);
	}

	public bool SessionValid()
	{
		Cookie cookie = tfServer.GetCookie(new Uri(SQSettings.SERVER_URL), "sessionid");
		if (cookie != null)
		{
			return !cookie.Expired;
		}
		return false;
	}

	private string SignDictionary(Dictionary<string, object> data, string nonce, string secret)
	{
		string text = string.Empty;
		List<string> list = new List<string>(data.Keys);
		list.Sort();
		foreach (string item in list)
		{
			text = text + item + data[item];
		}
		text += nonce;
		using HMACSHA256 hMACSHA = new HMACSHA256();
		hMACSHA.Key = Encoding.ASCII.GetBytes(secret);
		return Convert.ToBase64String(hMACSHA.ComputeHash(Encoding.ASCII.GetBytes(text))).Replace('+', '-').Replace('/', '_');
	}

	public void Friend_update_myinfo(bool helpcount, bool anonymoushelpcount, TFServer.JsonResponseHandler callback)
	{
		Dictionary<string, object> dictionary = new Dictionary<string, object>();
		dictionary["resethelp"] = (helpcount ? 1 : 0);
		dictionary["resetahelp"] = (anonymoushelpcount ? 1 : 0);
		dictionary["signature"] = SignDictionary(dictionary, nonce, "xc0u18^g0!ac3k%0+2vgglmnr1)x^!o(n6@$m3t^(7l!(#kv!-");
		tfServer.PostToJSON(SQSettings.SERVER_URL + "persist/friends_update_myinfoDW/", dictionary, callback);
	}

	public void Friend_use_friend(string friend_id, TFServer.JsonResponseHandler callback)
	{
		Dictionary<string, object> dictionary = new Dictionary<string, object>();
		dictionary["friendid"] = friend_id;
		dictionary["signature"] = SignDictionary(dictionary, nonce, "xc0u18^g0!ac3k%0+2vgglmnr1)x^!o(n6@$m3t^(7l!(#kv!-");
		tfServer.PostToJSON(SQSettings.SERVER_URL + "persist/friends_use_friendDW/", dictionary, callback);
	}

	public void Friend_use_player(string user_id, TFServer.JsonResponseHandler callback)
	{
		Dictionary<string, object> dictionary = new Dictionary<string, object>();
		dictionary["userid"] = user_id;
		dictionary["signature"] = SignDictionary(dictionary, nonce, "xc0u18^g0!ac3k%0+2vgglmnr1)x^!o(n6@$m3t^(7l!(#kv!-");
		tfServer.PostToJSON(SQSettings.SERVER_URL + "persist/friends_use_playerDW/", dictionary, callback);
	}

	private void WithFreshSave(Action send)
	{
		PlayerInfoScript instance = Singleton<PlayerInfoScript>.Instance;
		SessionManager instance2 = SessionManager.Instance;
		if (instance == null || instance2 == null)
		{
			send();
			return;
		}
		instance2.SaveToServer(instance.Serialize(), delegate
		{
			send();
		});
	}

	public void OlympicList(int offset, TFServer.JsonResponseHandler callback)
	{
		tfServer.PostToJSON(SQSettings.SERVER_URL + "persist/olympic_listDW/",
			new Dictionary<string, object> { { "offset", offset } }, callback);
	}

	public void OlympicOwners(int offset, TFServer.JsonResponseHandler callback)
	{
		tfServer.PostToJSON(SQSettings.SERVER_URL + "persist/olympic_ownersDW/",
			new Dictionary<string, object> { { "offset", offset } }, callback);
	}

	public void OlympicQuests(string ownerId, int offset, TFServer.JsonResponseHandler callback)
	{
		tfServer.PostToJSON(SQSettings.SERVER_URL + "persist/olympic_questsDW/",
			new Dictionary<string, object> { { "owner_id", ownerId }, { "offset", offset } }, callback);
	}

	public void OlympicGet(int dungeonId, TFServer.JsonResponseHandler callback)
	{
		tfServer.PostToJSON(SQSettings.SERVER_URL + "persist/olympic_getDW/",
			new Dictionary<string, object> { { "id", dungeonId } }, callback);
	}

	public void OlympicClaimTradeReward(int dungeonId, int amount, TFServer.JsonResponseHandler callback)
	{
		tfServer.PostToJSON(SQSettings.SERVER_URL + "persist/olympic_reward_claimDW/",
			new Dictionary<string, object> { { "id", dungeonId }, { "amount", amount } }, callback);
	}

	public void OlympicGetLocks(TFServer.JsonResponseHandler callback)
	{
		tfServer.PostToJSON(SQSettings.SERVER_URL + "persist/olympic_locksDW/",
			new Dictionary<string, object>(), callback);
	}

	public void OlympicGetBalance(TFServer.JsonResponseHandler callback)
	{
		tfServer.PostToJSON(SQSettings.SERVER_URL + "persist/olympic_balanceDW/",
			new Dictionary<string, object>(), delegate(Dictionary<string, object> response, HttpStatusCode status)
		{
			ApplyOlympicBalance(response, status, callback);
		});
	}

	public void OlympicCreate(string requestId, string name, string leaderId, int expectedCost, List<int> creatures,
		List<Dictionary<string, object>> rewards, TFServer.JsonResponseHandler callback)
	{
		OlympicSave("olympic_createDW", requestId, 0, name, leaderId, expectedCost, creatures, rewards, callback);
	}

	public void OlympicCreateStatus(string requestId, TFServer.JsonResponseHandler callback)
	{
		PlayerInfoScript player = Singleton<PlayerInfoScript>.Instance;
		if (player == null || PlayerInfoScript.SaveBlocked || PlayerInfoScript.DeviceLocked || PlayerInfoScript.SessionMoved)
		{
			callback(new Dictionary<string, object> { { "success", false }, { "error", "Player save is unavailable." } }, HttpStatusCode.ServiceUnavailable);
			return;
		}
		string playerCode = player.GetPlayerCode();
		tfServer.PostToJSON(SQSettings.SERVER_URL + "persist/olympic_create_statusDW/",
			new Dictionary<string, object> { { "request_id", requestId } },
			delegate(Dictionary<string, object> response, HttpStatusCode status)
		{
			if (Singleton<PlayerInfoScript>.Instance != player || player.GetPlayerCode() != playerCode)
			{
				callback(new Dictionary<string, object> { { "success", false }, { "error", "Player session changed." } }, HttpStatusCode.ServiceUnavailable);
				return;
			}
			object dataValue;
			object found;
			object success;
			Dictionary<string, object> data = response != null && response.TryGetValue("data", out dataValue)
				? dataValue as Dictionary<string, object> : null;
			if (status != HttpStatusCode.OK || response == null || !response.TryGetValue("success", out success) ||
				!(success is bool) || !(bool)success)
			{
				callback(response, status);
				return;
			}
			if (data == null || !data.TryGetValue("found", out found) || !(found is bool))
			{
				Debug.LogError("[Olympic] Invalid create status response.");
				callback(new Dictionary<string, object> { { "success", false }, { "error", "Invalid create status response." } }, HttpStatusCode.BadGateway);
				return;
			}
			if (!(bool)found)
			{
				callback(response, status);
				return;
			}
			ApplyOlympicBalance(response, status, delegate(Dictionary<string, object> result, HttpStatusCode resultStatus)
			{
				if (resultStatus != HttpStatusCode.OK)
				{
					callback(result, resultStatus);
					return;
				}
				player.Save(delegate(bool saved)
				{
					if (!saved)
					{
						callback(new Dictionary<string, object> { { "success", false }, { "error", "Could not synchronize the Olympic balance." } }, HttpStatusCode.ServiceUnavailable);
						return;
					}
					callback(result, resultStatus);
				});
			});
		});
	}

	public void OlympicEdit(string requestId, int dungeonId, string name, string leaderId,
		List<int> creatures, List<Dictionary<string, object>> rewards, TFServer.JsonResponseHandler callback)
	{
		OlympicSave("olympic_editDW", requestId, dungeonId, name, leaderId, 0, creatures, rewards, callback);
	}

	private void OlympicSave(string endpoint, string requestId, int dungeonId, string name, string leaderId, int expectedCost,
		List<int> creatures, List<Dictionary<string, object>> rewards, TFServer.JsonResponseHandler callback)
	{
		PlayerInfoScript player = Singleton<PlayerInfoScript>.Instance;
		if (player == null || PlayerInfoScript.SaveBlocked || PlayerInfoScript.DeviceLocked || PlayerInfoScript.SessionMoved)
		{
			callback(new Dictionary<string, object> { { "success", false }, { "error", "Player save is unavailable." } }, HttpStatusCode.ServiceUnavailable);
			return;
		}
		string playerCode = player.GetPlayerCode();
		Dictionary<string, object> data = new Dictionary<string, object>
		{
			{ "request_id", requestId },
			{ "name", name },
			{ "creatures", MiniJSON.Json.Serialize(creatures) },
			{ "rewards", MiniJSON.Json.Serialize(rewards) }
		};
		if (dungeonId > 0)
		{
			data["id"] = dungeonId;
			if (!string.IsNullOrEmpty(leaderId)) data["leader_id"] = leaderId;
		}
		else
		{
			data["leader_id"] = leaderId;
			data["expected_cost"] = expectedCost;
		}
		player.Save(delegate(bool saved)
		{
			if (Singleton<PlayerInfoScript>.Instance != player || player.GetPlayerCode() != playerCode)
			{
				callback(new Dictionary<string, object> { { "success", false }, { "error", "Player session changed." } }, HttpStatusCode.ServiceUnavailable);
				return;
			}
			if (!saved)
			{
				callback(new Dictionary<string, object> { { "success", false }, { "error", "Could not save player inventory." } }, HttpStatusCode.ServiceUnavailable);
				return;
			}
			tfServer.PostToJSON(SQSettings.SERVER_URL + "persist/" + endpoint + "/", data,
				delegate(Dictionary<string, object> response, HttpStatusCode status)
				{
					if (Singleton<PlayerInfoScript>.Instance != player || player.GetPlayerCode() != playerCode)
					{
						callback(new Dictionary<string, object> { { "success", false }, { "error", "Player session changed." } }, HttpStatusCode.ServiceUnavailable);
						return;
					}
					ApplyOlympicBalance(response, status, delegate(Dictionary<string, object> result, HttpStatusCode resultStatus)
					{
						object success;
						if (resultStatus != HttpStatusCode.OK || result == null ||
							!result.TryGetValue("success", out success) || !(success is bool) || !(bool)success)
						{
							callback(result, resultStatus);
							return;
						}
						player.Save(delegate(bool acknowledged)
						{
							if (!acknowledged)
							{
								Debug.LogError("[Olympic] Dungeon was saved, but the updated currency balance could not be acknowledged.");
								callback(new Dictionary<string, object>
								{
									{ "success", false },
									{ "error", "Dungeon saved and charged, but balance synchronization failed. Refresh the Olympic balance before retrying." },
									{ "data", result["data"] }
								}, HttpStatusCode.ServiceUnavailable);
								return;
							}
							callback(result, resultStatus);
						});
					});
				});
		});
	}

	private static void ApplyOlympicBalance(Dictionary<string, object> response, HttpStatusCode status,
		TFServer.JsonResponseHandler callback)
	{
		if (status == HttpStatusCode.OK && response != null &&
			response.TryGetValue("success", out object success) && success is bool && (bool)success)
		{
			Dictionary<string, object> data = response.ContainsKey("data") ? response["data"] as Dictionary<string, object> : null;
			int paid;
			int free;
			if (data == null || !data.ContainsKey("paid_hard_currency") || !data.ContainsKey("free_hard_currency") ||
				!int.TryParse(Convert.ToString(data["paid_hard_currency"]), out paid) ||
				!int.TryParse(Convert.ToString(data["free_hard_currency"]), out free) || paid < 0 || free < 0)
			{
				Debug.LogError("[Olympic] Server returned an invalid currency balance.");
				callback(new Dictionary<string, object> { { "success", false }, { "error", "Invalid Olympic currency balance." } }, HttpStatusCode.BadGateway);
				return;
			}
			Singleton<PlayerInfoScript>.Instance.SaveData.ManualSetHardCurrency(paid, free);
		}
		callback(response, status);
	}

	public void OlympicDelete(string requestId, int dungeonId, TFServer.JsonResponseHandler callback)
	{
		tfServer.PostToJSON(SQSettings.SERVER_URL + "persist/olympic_deleteDW/",
			new Dictionary<string, object> { { "request_id", requestId }, { "id", dungeonId } }, callback);
	}

	public void TradeSync(TFServer.JsonResponseHandler callback)
	{
		Dictionary<string, object> dictionary = new Dictionary<string, object>();
		dictionary["player_id"] = Singleton<PlayerInfoScript>.Instance.GetPlayerCode();
		tfServer.PostToJSON(SQSettings.SERVER_URL + "persist/trade_syncDW/", dictionary, callback);
	}

	public void TradeGetBalance(TFServer.JsonResponseHandler callback)
	{
		Dictionary<string, object> dictionary = new Dictionary<string, object>();
		dictionary["player_id"] = Singleton<PlayerInfoScript>.Instance.GetPlayerCode();
		tfServer.PostToJSON(SQSettings.SERVER_URL + "persist/trade_balanceDW/", dictionary, callback);
	}

	public void TradeGetFriends(TFServer.JsonResponseHandler callback)
	{
		Dictionary<string, object> dictionary = new Dictionary<string, object>();
		dictionary["player_id"] = Singleton<PlayerInfoScript>.Instance.GetPlayerCode();
		tfServer.PostToJSON(SQSettings.SERVER_URL + "persist/trade_friendsDW/", dictionary, callback);
	}

	public void TradeListItem(long uniqueId, int price, TFServer.JsonResponseHandler callback, string itemJson = null)
	{
		Dictionary<string, object> dictionary = new Dictionary<string, object>();
		dictionary["player_id"] = Singleton<PlayerInfoScript>.Instance.GetPlayerCode();
		dictionary["unique_id"] = uniqueId;
		dictionary["price"] = price;
		if (!string.IsNullOrEmpty(itemJson))
		{
			dictionary["item"] = itemJson;
		}
		WithFreshSave(delegate
		{
			tfServer.PostToJSON(SQSettings.SERVER_URL + "persist/trade_listDW/", dictionary, callback);
		});
	}

	public void TradeBrowse(int startpos, int endpos, TFServer.JsonResponseHandler callback)
	{
		Dictionary<string, object> dictionary = new Dictionary<string, object>();
		dictionary["startpos"] = startpos;
		dictionary["endpos"] = endpos;
		tfServer.PostToJSON(SQSettings.SERVER_URL + "persist/trade_browseDW/", dictionary, callback);
	}

	public void TradeBrowseV2(Dictionary<string, object> args, TFServer.JsonResponseHandler callback)
	{
		Dictionary<string, object> dictionary = new Dictionary<string, object>(args);
		dictionary["v"] = 2;
		dictionary["player_id"] = Singleton<PlayerInfoScript>.Instance.GetPlayerCode();
		tfServer.PostToJSON(SQSettings.SERVER_URL + "persist/trade_browseDW/", dictionary, callback);
	}

	public void TradeBuy(int listingId, TFServer.JsonResponseHandler callback)
	{
		Dictionary<string, object> dictionary = new Dictionary<string, object>();
		dictionary["player_id"] = Singleton<PlayerInfoScript>.Instance.GetPlayerCode();
		dictionary["listing_id"] = listingId;
		WithFreshSave(delegate
		{
			tfServer.PostToJSON(SQSettings.SERVER_URL + "persist/trade_buyDW/", dictionary, callback);
		});
	}

	public void TradeCancel(int listingId, TFServer.JsonResponseHandler callback)
	{
		Dictionary<string, object> dictionary = new Dictionary<string, object>();
		dictionary["player_id"] = Singleton<PlayerInfoScript>.Instance.GetPlayerCode();
		dictionary["listing_id"] = listingId;
		WithFreshSave(delegate
		{
			tfServer.PostToJSON(SQSettings.SERVER_URL + "persist/trade_cancelDW/", dictionary, callback);
		});
	}

	public void TradeBuyPack(int pack, string currency, TFServer.JsonResponseHandler callback)
	{
		Dictionary<string, object> dictionary = new Dictionary<string, object>();
		dictionary["player_id"] = Singleton<PlayerInfoScript>.Instance.GetPlayerCode();
		dictionary["pack"] = pack;
		dictionary["currency"] = currency;
		WithFreshSave(delegate
		{
			tfServer.PostToJSON(SQSettings.SERVER_URL + "persist/trade_buycoinsDW/", dictionary, callback);
		});
	}

	public void TradeSendOffer(string toPlayerId, string offerUniqueIds, string requestUniqueIds, int offerCoins, int requestCoins, TFServer.JsonResponseHandler callback, string offerItems = null, string requestItems = null)
	{
		Dictionary<string, object> dictionary = new Dictionary<string, object>();
		dictionary["from_id"] = Singleton<PlayerInfoScript>.Instance.GetPlayerCode();
		dictionary["to_id"] = toPlayerId;
		dictionary["offer_unique_ids"] = offerUniqueIds;
		dictionary["request_unique_ids"] = requestUniqueIds;
		dictionary["offer_coins"] = offerCoins;
		dictionary["request_coins"] = requestCoins;
		if (!string.IsNullOrEmpty(offerItems))
		{
			dictionary["offer_items"] = offerItems;
		}
		if (!string.IsNullOrEmpty(requestItems))
		{
			dictionary["request_items"] = requestItems;
		}
		WithFreshSave(delegate
		{
			tfServer.PostToJSON(SQSettings.SERVER_URL + "persist/trade_offerDW/", dictionary, callback);
		});
	}

	public void TradeGetInventory(string playerId, TFServer.JsonResponseHandler callback)
	{
		Dictionary<string, object> dictionary = new Dictionary<string, object>();
		dictionary["player_id"] = playerId;
		WithFreshSave(delegate
		{
			tfServer.PostToJSON(SQSettings.SERVER_URL + "persist/trade_inventoryDW/", dictionary, callback);
		});
	}

	public void TradeOffersReceived(TFServer.JsonResponseHandler callback)
	{
		Dictionary<string, object> dictionary = new Dictionary<string, object>();
		dictionary["player_id"] = Singleton<PlayerInfoScript>.Instance.GetPlayerCode();
		tfServer.PostToJSON(SQSettings.SERVER_URL + "persist/trade_offers_receivedDW/", dictionary, callback);
	}

	public void TradeAcceptOffer(int offerId, TFServer.JsonResponseHandler callback)
	{
		Dictionary<string, object> dictionary = new Dictionary<string, object>();
		dictionary["player_id"] = Singleton<PlayerInfoScript>.Instance.GetPlayerCode();
		dictionary["offer_id"] = offerId;
		WithFreshSave(delegate
		{
			tfServer.PostToJSON(SQSettings.SERVER_URL + "persist/trade_acceptDW/", dictionary, callback);
		});
	}

	public void TradeDeclineOffer(int offerId, TFServer.JsonResponseHandler callback)
	{
		Dictionary<string, object> dictionary = new Dictionary<string, object>();
		dictionary["player_id"] = Singleton<PlayerInfoScript>.Instance.GetPlayerCode();
		dictionary["offer_id"] = offerId;
		WithFreshSave(delegate
		{
			tfServer.PostToJSON(SQSettings.SERVER_URL + "persist/trade_declineDW/", dictionary, callback);
		});
	}

	public void Friend_request_with_myinfo(string playerId, TFServer.JsonResponseHandler callback)
	{
		Dictionary<string, object> dictionary = new Dictionary<string, object>();
		dictionary["player_id"] = Singleton<PlayerInfoScript>.Instance.GetPlayerCode();
		dictionary["invite_id"] = playerId;
		dictionary["signature"] = SignDictionary(dictionary, nonce, "xc0u18^g0!ac3k%0+2vgglmnr1)x^!o(n6@$m3t^(7l!(#kv!-");
		tfServer.PostToJSON(SQSettings.SERVER_URL + "persist/friends_request_withmyinfoDW/", dictionary, callback);
	}

	public void Friend_fake_request_with_myinfo(TFServer.JsonResponseHandler handler)
	{
		tfServer.GetToJSON(SQSettings.SERVER_URL + "persist/friendsfakereq/", handler);
	}

	public void Friend_confirm_with_myinfo(string playerId, TFServer.JsonResponseHandler callback)
	{
		Dictionary<string, object> dictionary = new Dictionary<string, object>();
		dictionary["player_id"] = Singleton<PlayerInfoScript>.Instance.GetPlayerCode();
		dictionary["invite_id"] = playerId;
		dictionary["signature"] = SignDictionary(dictionary, nonce, "xc0u18^g0!ac3k%0+2vgglmnr1)x^!o(n6@$m3t^(7l!(#kv!-");
		tfServer.PostToJSON(SQSettings.SERVER_URL + "persist/friends_confirm_request_withmyinfoDW/", dictionary, callback);
	}

	public void Friend_get_userinfo(string playerId, TFServer.JsonResponseHandler callback)
	{
		Dictionary<string, object> dictionary = new Dictionary<string, object>();
		dictionary["userid"] = playerId;
		dictionary["signature"] = SignDictionary(dictionary, nonce, "xc0u18^g0!ac3k%0+2vgglmnr1)x^!o(n6@$m3t^(7l!(#kv!-");
		tfServer.PostToJSON(SQSettings.SERVER_URL + "persist/friends_informationDW/", dictionary, callback);
	}

	public void Friend_get_helpers(string optionTarget, List<string> excludeIDs, TFServer.JsonResponseHandler callback)
	{
		string value = string.Join(",", excludeIDs.ToArray());
		Dictionary<string, object> dictionary = new Dictionary<string, object>();
		dictionary["level"] = optionTarget;
		dictionary["player_id"] = Singleton<PlayerInfoScript>.Instance.GetPlayerCode();
		dictionary["version"] = value;
		dictionary["signature"] = SignDictionary(dictionary, nonce, "xc0u18^g0!ac3k%0+2vgglmnr1)x^!o(n6@$m3t^(7l!(#kv!-");
		tfServer.PostToJSON(SQSettings.SERVER_URL + "persist/friends_find_candidatesDW/", dictionary, callback);
	}

	public void User_currency_history2(int num, TFServer.JsonResponseHandler callback)
	{
		Dictionary<string, object> dictionary = new Dictionary<string, object>();
		dictionary["number"] = num;
		dictionary["signature"] = SignDictionary(dictionary, nonce, "xc0u18^g0!ac3k%0+2vgglmnr1)x^!o(n6@$m3t^(7l!(#kv!-");
		tfServer.PostToJSON(SQSettings.SERVER_URL + "persist/user_history2/", dictionary, callback);
	}

	public void User_currency_history(string country, int transaction, int tier, int paid, int free, TFServer.JsonResponseHandler callback)
	{
		Dictionary<string, object> dictionary = new Dictionary<string, object>();
		dictionary["country"] = country;
		dictionary["transaction"] = transaction.ToString();
		dictionary["tier"] = tier.ToString();
		dictionary["paid"] = paid.ToString();
		dictionary["free"] = free.ToString();
		dictionary["signature"] = SignDictionary(dictionary, nonce, "xc0u18^g0!ac3k%0+2vgglmnr1)x^!o(n6@$m3t^(7l!(#kv!-");
		tfServer.PostToJSON(SQSettings.SERVER_URL + "persist/user_history_android/", dictionary, callback);
	}

	public void User_action(int pd, int fr, int cu, int dp, int df, int dc, string us, int hd, int misc, string evt, string cc, TFServer.JsonResponseHandler callback, string ctx = null, int ds = 0, int dv = 0, int de = 0, string items = null)
	{
		Dictionary<string, object> dictionary = new Dictionary<string, object>();
		dictionary["v2"] = "1";
		dictionary["ctx"] = ctx ?? string.Empty;
		dictionary["items"] = items ?? string.Empty;
		dictionary["ds"] = ds.ToString();
		dictionary["dv"] = dv.ToString();
		dictionary["de"] = de.ToString();
		dictionary["player_id"] = Singleton<PlayerInfoScript>.Instance.GetPlayerCode();
		dictionary["pd"] = pd.ToString();
		dictionary["fr"] = fr.ToString();
		dictionary["cu"] = cu.ToString();
		dictionary["dp"] = dp.ToString();
		dictionary["df"] = df.ToString();
		dictionary["dc"] = dc.ToString();
		dictionary["us"] = us;
		dictionary["hd"] = hd.ToString();
		dictionary["misc"] = misc.ToString();
		dictionary["evt"] = evt;
		dictionary["cc"] = cc;
		dictionary["ad"] = 1;
		dictionary["signature"] = SignDictionary(dictionary, nonce, "xc0u18^g0!ac3k%0+2vgglmnr1)x^!o(n6@$m3t^(7l!(#kv!-");
		tfServer.PostToJSON(SQSettings.SERVER_URL + "persist/user_action2/", dictionary, callback);
	}

	public void GetCC(TFServer.JsonResponseHandler handler)
	{
		tfServer.GetToJSON(SQSettings.SERVER_URL + "persist/getcc/", handler);
	}

	public void PlaceMeOnLeaderboard(string user_id, string currentSeasonID, TFServer.JsonResponseHandler callback)
	{
		Dictionary<string, object> dictionary = new Dictionary<string, object>();
		dictionary["userid"] = user_id;
		dictionary["currentseasonid"] = currentSeasonID;
		dictionary["signature"] = SignDictionary(dictionary, nonce, "xc0u18^g0!ac3k%0+2vgglmnr1)x^!o(n6@$m3t^(7l!(#kv!-");
		tfServer.PostToJSON(SQSettings.SERVER_URL + "dw_leaderboard/placeme/", dictionary, callback);
	}

	public void RegisterMatchResult(string user_id, string currentSeasonID, string opnentID, bool didIWin, TFServer.JsonResponseHandler callback, PvpMode mode = PvpMode.Ranked)
	{
		Dictionary<string, object> dictionary = new Dictionary<string, object>();
		dictionary["userid"] = user_id;
		dictionary["currentseasonid"] = currentSeasonID;
		dictionary["opponentid"] = opnentID;
		dictionary["didiwin"] = didIWin.ToString();
		if (mode == PvpMode.Extreme || mode == PvpMode.Zen)
		{
			dictionary["mode"] = mode.ToString().ToLowerInvariant();
		}
		dictionary["signature"] = SignDictionary(dictionary, nonce, "xc0u18^g0!ac3k%0+2vgglmnr1)x^!o(n6@$m3t^(7l!(#kv!-");
		tfServer.PostToJSON(SQSettings.SERVER_URL + "dw_leaderboard/registerresult/", dictionary, callback);
	}

	public void FetchLeaderboardsEntries(int startPosition, int endPosition, TFServer.JsonResponseHandler callback, PvpMode mode = PvpMode.Ranked)
	{
		Dictionary<string, object> dictionary = new Dictionary<string, object>();
		dictionary["startpos"] = startPosition.ToString();
		dictionary["endpos"] = endPosition.ToString();
		if (mode == PvpMode.Extreme || mode == PvpMode.Zen)
		{
			dictionary["mode"] = mode.ToString().ToLowerInvariant();
		}
		dictionary["signature"] = SignDictionary(dictionary, nonce, "xc0u18^g0!ac3k%0+2vgglmnr1)x^!o(n6@$m3t^(7l!(#kv!-");
		tfServer.PostToJSON(SQSettings.SERVER_URL + "dw_leaderboard/fetchentries/", dictionary, callback);
	}

	public void HasSeasonEnded(string user_id, TFServer.JsonResponseHandler callback)
	{
		Dictionary<string, object> dictionary = new Dictionary<string, object>();
		dictionary["userid"] = user_id;
		dictionary["signature"] = SignDictionary(dictionary, nonce, "xc0u18^g0!ac3k%0+2vgglmnr1)x^!o(n6@$m3t^(7l!(#kv!-");
		tfServer.PostToJSON(SQSettings.SERVER_URL + "dw_leaderboard/hasended/", dictionary, callback);
	}

	public void CompassSupportLogin(string project_id, string support_id, string p, string user_id, string checkkey, TFServer.JsonStringHandler callback)
	{
		Dictionary<string, object> dictionary = new Dictionary<string, object>();
		dictionary["project_id"] = project_id;
		dictionary["support_id"] = support_id;
		dictionary["p"] = p;
		dictionary["user_id"] = user_id;
		dictionary["checkkey"] = checkkey;
		tfServer.PostToString(MiscParams.CompassSupportURL + "api/GetLoginKey", dictionary, callback);
	}
}
