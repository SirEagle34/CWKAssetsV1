using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using PlayFab;
using PlayFab.ClientModels;
using TMPro;

public class leaderboard : MonoBehaviour
{
    public GameObject ScorePrefab1;
    public GameObject space1;
    public GameObject ScorePrefab2;
    public GameObject space2;
    public GameObject ScorePrefab3;
    public GameObject space3;

    private void Start()
    {
        GetLeaderBoard();
        GetLeaderBoard2();
        GetLeaderBoard3();
    }

    public void Exit()
    {
        SceneManager.LoadScene("BootVideoScene");
    }

    public void GetLeaderBoard()
    {
        var request = new GetLeaderboardRequest
        {
            StatisticName = "MultiplayerWinStreak",
            StartPosition = 0,
            MaxResultsCount = 7
        };

        PlayFabClientAPI.GetLeaderboard(request, OnLeaderBoardGet, OnError);
    }

    void OnLeaderBoardGet(GetLeaderboardResult result)
    {
        foreach (var item in result.Leaderboard)
        {
            GameObject score = Instantiate(ScorePrefab1, space1.transform);
            TextMeshProUGUI[] texts = score.GetComponentsInChildren<TextMeshProUGUI>();
            Image positionBG = score.transform.Find("PositionBG").GetComponent<Image>();
            texts[0].text = (item.Position + 1).ToString() + ".";
            texts[1].text = item.DisplayName;
            texts[2].text = item.StatValue.ToString();

            switch (item.Position)
            {
                case 0: // 1. miejsce - z這ty
                    positionBG.color = new Color(0.85f, 0.7f, 0f, 1f); // Z這ty (RGB: 255, 215, 0)
                    break;
                case 1: // 2. miejsce - srebrny
                    positionBG.color = new Color(0.75f, 0.75f, 0.75f, 1f); // Srebrny (RGB: 192, 192, 192)
                    break;
                case 2: // 3. miejsce - br頊owy
                    positionBG.color = new Color(0.6f, 0.3f, 0.1f, 1f); // Br頊owy (RGB: 153, 76, 26)
                    break;
                default:
                    break;
            }
        }
    }
    void OnError(PlayFabError error)
    {
        Debug.Log("Error!");
    }

    public void GetLeaderBoard2()
    {
        var request = new GetLeaderboardRequest
        {
            StatisticName = "Gems",
            StartPosition = 0,
            MaxResultsCount = 7
        };

        PlayFabClientAPI.GetLeaderboard(request, OnLeaderBoardGet2, OnError);
    }

    void OnLeaderBoardGet2(GetLeaderboardResult result)
    {
        foreach (var item in result.Leaderboard)
        {
            GameObject score = Instantiate(ScorePrefab2, space2.transform);
            TextMeshProUGUI[] texts = score.GetComponentsInChildren<TextMeshProUGUI>();
            Image positionBG = score.transform.Find("PositionBG").GetComponent<Image>();
            texts[0].text = (item.Position + 1).ToString() + ".";
            texts[1].text = item.DisplayName;
            texts[2].text = item.StatValue.ToString();

            switch (item.Position)
            {
                case 0: // 1. miejsce - z這ty
                    positionBG.color = new Color(0.85f, 0.7f, 0f, 1f); // Z這ty (RGB: 255, 215, 0)
                    break;
                case 1: // 2. miejsce - srebrny
                    positionBG.color = new Color(0.75f, 0.75f, 0.75f, 1f); // Srebrny (RGB: 192, 192, 192)
                    break;
                case 2: // 3. miejsce - br頊owy
                    positionBG.color = new Color(0.6f, 0.3f, 0.1f, 1f); // Br頊owy (RGB: 153, 76, 26)
                    break;
                default:
                    break;
            }
        }
    }

    public void GetLeaderBoard3()
    {
        var request = new GetLeaderboardRequest
        {
            StatisticName = "Coins",
            StartPosition = 0,
            MaxResultsCount = 7
        };

        PlayFabClientAPI.GetLeaderboard(request, OnLeaderBoardGet3, OnError);
    }

    void OnLeaderBoardGet3(GetLeaderboardResult result)
    {
        foreach (var item in result.Leaderboard)
        {
            GameObject score = Instantiate(ScorePrefab3, space3.transform);
            TextMeshProUGUI[] texts = score.GetComponentsInChildren<TextMeshProUGUI>();
            Image positionBG = score.transform.Find("PositionBG").GetComponent<Image>();
            texts[0].text = (item.Position + 1).ToString() + ".";
            texts[1].text = item.DisplayName;
            texts[2].text = item.StatValue.ToString();

            switch (item.Position)
            {
                case 0: // 1. miejsce - z這ty
                    positionBG.color = new Color(0.85f, 0.7f, 0f, 1f); // Z這ty (RGB: 255, 215, 0)
                    break;
                case 1: // 2. miejsce - srebrny
                    positionBG.color = new Color(0.75f, 0.75f, 0.75f, 1f); // Srebrny (RGB: 192, 192, 192)
                    break;
                case 2: // 3. miejsce - br頊owy
                    positionBG.color = new Color(0.6f, 0.3f, 0.1f, 1f); // Br頊owy (RGB: 153, 76, 26)
                    break;
                default:
                    break;
            }
        }
    }
}