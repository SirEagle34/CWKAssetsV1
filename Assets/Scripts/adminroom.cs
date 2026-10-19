using System.Collections.Generic;
using UnityEngine;
using TMPro;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine.SceneManagement;
using PlayFab.AddonModels;
using PlayFab.CloudScriptModels;
using Newtonsoft.Json;

public class adminroom : MonoBehaviour
{
    public TextMeshProUGUI status;
    public TextMeshProUGUI playerslist;

    public TMP_InputField nickname;

    public void Exit()
    {
        SceneManager.LoadScene("BootVideoScene");
    }

    public void SearchPlayers()
    {
        GetLeaderboard();
    }

    // Funkcja pobierająca leaderboard z PlayFab
    public void GetLeaderboard()
    {
        var request = new GetLeaderboardRequest
        {
            StatisticName = "Ban",
            StartPosition = 0,
            MaxResultsCount = 100
        };

        PlayFabClientAPI.GetLeaderboard(request, OnLeaderboardReceived, OnError);
    }

    // Callback dla otrzymania leaderboarda
    private void OnLeaderboardReceived(GetLeaderboardResult result)
    {
        // Czyszczenie poprzednich wyników
        playerslist.text = "Banned Players:\n";

        // Iterujemy przez leaderboard i sprawdzamy wartość statystyki "Ban"
        foreach (var entry in result.Leaderboard)
        {
            if (entry.StatValue == 1)  // Sprawdzamy czy statystyka "Ban" wynosi 1
            {
                GetPlayerData(entry.PlayFabId, entry.DisplayName);
            }
        }
    }

    // Pobieranie danych gracza (Nickname)
    private void GetPlayerData(string playFabId, string displayName)
    {
        var request = new GetUserDataRequest
        {
            PlayFabId = playFabId
        };

        PlayFabClientAPI.GetUserData(request, (result) => OnPlayerDataReceived(result, displayName), OnError);
    }

    // Callback po pobraniu danych gracza (Nickname)
    private void OnPlayerDataReceived(GetUserDataResult result, string displayName)
    {
        string nickname = ""; // Domyślna wartość, jeśli Nickname nie istnieje

        if (result.Data != null && result.Data.ContainsKey("Nickname"))
        {
            nickname = result.Data["Nickname"].Value;
        }

        // Dodajemy tylko graczy, którzy mają Ban = 1
        playerslist.text += $"{displayName} - {nickname}\n";
        status.text = "Found banned players!";
        status.color = Color.green;
    }

    // Obsługa błędów PlayFab
    private void OnError(PlayFabError error)
    {
        Debug.LogError("Error: " + error.GenerateErrorReport());
        status.text = "Error retrieving data.";
        status.color = Color.red;
    }

    public void UpdateStarRatingForPlayer()
    {
        // Pobieramy dane o graczu
        PlayFabClientAPI.GetUserData(new GetUserDataRequest(), OnGetUserDataSuccess, OnError);
    }

    // Callback, kiedy dane gracza zostały pomyślnie pobrane
    private void OnGetUserDataSuccess(GetUserDataResult result)
    {
        // Sprawdzamy, czy mamy SerializedInventory
        if (result.Data != null && result.Data.ContainsKey("SerializedInventory"))
        {
            string serializedInventory = result.Data["SerializedInventory"].Value;

            // Deserializujemy dane JSON do listy przedmiotów
            List<ItemData> inventory = JsonConvert.DeserializeObject<List<ItemData>>(serializedInventory);

            // Zaktualizowanie StarRating na 5 dla każdego przedmiotu
            foreach (var item in inventory)
            {
                item.StarRating = 5;
            }

            // Serializujemy dane z powrotem do JSON
            string updatedInventory = JsonConvert.SerializeObject(inventory);

            // Zapisujemy zaktualizowane dane
            UpdateUserData(updatedInventory);
        }
        else
        {
            Debug.LogError("Brak danych SerializedInventory!");
        }
    }

    // Funkcja do zapisu zaktualizowanych danych gracza
    private void UpdateUserData(string updatedInventory)
    {
        // Przygotowanie danych do zapisu
        var userData = new Dictionary<string, string>
        {
            { "SerializedInventory", updatedInventory }
        };

        var request = new UpdateUserDataRequest
        {
            Data = userData
        };

        PlayFabClientAPI.UpdateUserData(request, OnUpdateUserDataSuccess, OnStarsError);
    }

    // Callback, kiedy dane gracza zostały pomyślnie zapisane
    private void OnUpdateUserDataSuccess(UpdateUserDataResult result)
    {
        Debug.Log("Dane użytkownika zostały zaktualizowane pomyślnie!");
        PlayerPrefs.DeleteAll();
        status.text = "Creatures stars set to 5!";
        status.color = Color.green;
    }

    // Callback, kiedy wystąpi błąd
    private void OnStarsError(PlayFabError error)
    {
        Debug.LogError($"Błąd: {error.GenerateErrorReport()}");
    }

    public void BanUser()
    {
        string displayName = nickname.text.Trim();

        if (string.IsNullOrEmpty(displayName))
        {
            Debug.LogWarning("[BanPlayer] Display name is empty!");
            status.color = Color.red;
            status.text = "Enter nickname!";
            return;
        }

        Debug.Log($"[BanPlayer] Searching for player: {displayName}");

        var request = new GetAccountInfoRequest { TitleDisplayName = displayName };
        PlayFabClientAPI.GetAccountInfo(request, OnGetAccountSuccess, OnError);
    }

    private void OnGetAccountSuccess(GetAccountInfoResult result)
    {
        if (result.AccountInfo == null || string.IsNullOrEmpty(result.AccountInfo.PlayFabId))
        {
            Debug.LogWarning("[BanPlayer] Player not found!");
            status.color = Color.red;
            status.text = "Player not found!";
            return;
        }

        string playFabId = result.AccountInfo.PlayFabId;
        Debug.Log($"[BanPlayer] Player found! PlayFabID: {playFabId}");

        ExecuteCloudScriptRequest request = new ExecuteCloudScriptRequest
        {
            FunctionName = "BanPlayer", // Nazwa funkcji w PlayFab CloudScript
            FunctionParameter = new { PlayerId = playFabId },
            GeneratePlayStreamEvent = true
        };

        PlayFabClientAPI.ExecuteCloudScript(request, OnBanSuccess, OnBanError);
    }

    private void OnBanSuccess(PlayFab.ClientModels.ExecuteCloudScriptResult result)
    {
        Debug.Log("[BanPlayer] Player successfully banned via CloudScript!");
        status.color = Color.green;
        status.text = "Player banned!";
    }

    private void OnBanError(PlayFabError error)
    {
        Debug.LogError($"[BanPlayer] Error: {error.GenerateErrorReport()}");
    }



    public void UnbanUser()
    {
        string displayName = nickname.text.Trim();

        if (string.IsNullOrEmpty(displayName))
        {
            Debug.LogWarning("[BanPlayer] Display name is empty!");
            status.color = Color.red;
            status.text = "Enter nickname!";
            return;
        }

        Debug.Log($"[BanPlayer] Searching for player: {displayName}");

        var request = new GetAccountInfoRequest { TitleDisplayName = displayName };
        PlayFabClientAPI.GetAccountInfo(request, OnGetAccountSuccess2, OnError);
    }

    private void OnGetAccountSuccess2(GetAccountInfoResult result)
    {
        if (result.AccountInfo == null || string.IsNullOrEmpty(result.AccountInfo.PlayFabId))
        {
            Debug.LogWarning("[BanPlayer] Player not found!");
            status.color = Color.red;
            status.text = "Player not found!";
            return;
        }

        string playFabId = result.AccountInfo.PlayFabId;
        Debug.Log($"[BanPlayer] Player found! PlayFabID: {playFabId}");

        ExecuteCloudScriptRequest request = new ExecuteCloudScriptRequest
        {
            FunctionName = "UnBanPlayer", // Nazwa funkcji w PlayFab CloudScript
            FunctionParameter = new { PlayerId = playFabId },
            GeneratePlayStreamEvent = true
        };

        PlayFabClientAPI.ExecuteCloudScript(request, OnUnBanSuccess, OnUnBanError);
    }

    private void OnUnBanSuccess(PlayFab.ClientModels.ExecuteCloudScriptResult result)
    {
        Debug.Log("[BanPlayer] Player successfully banned via CloudScript!");
        status.color = Color.green;
        status.text = "Player unbanned!";
    }

    private void OnUnBanError(PlayFabError error)
    {
        Debug.LogError($"[BanPlayer] Error: {error.GenerateErrorReport()}");
    }

    public void AddRank()
    {
        string displayName = nickname.text.Trim();

        if (string.IsNullOrEmpty(displayName))
        {
            Debug.LogWarning("[BanPlayer] Display name is empty!");
            status.color = Color.red;
            status.text = "Enter nickname!";
            return;
        }

        Debug.Log($"[BanPlayer] Searching for player: {displayName}");

        var request = new GetAccountInfoRequest { TitleDisplayName = displayName };
        PlayFabClientAPI.GetAccountInfo(request, OnGetAccountSuccess3, OnError);
    }

    private void OnGetAccountSuccess3(GetAccountInfoResult result)
    {
        if (result.AccountInfo == null || string.IsNullOrEmpty(result.AccountInfo.PlayFabId))
        {
            Debug.LogWarning("[BanPlayer] Player not found!");
            status.color = Color.red;
            status.text = "Player not found!";
            return;
        }

        string playFabId = result.AccountInfo.PlayFabId;
        Debug.Log($"[BanPlayer] Player found! PlayFabID: {playFabId}");

        ExecuteCloudScriptRequest request = new ExecuteCloudScriptRequest
        {
            FunctionName = "AddRank",
            FunctionParameter = new { PlayerId = playFabId },
            GeneratePlayStreamEvent = true
        };

        PlayFabClientAPI.ExecuteCloudScript(request, OnRankSuccess, OnUnBanError);
    }

    private void OnRankSuccess(PlayFab.ClientModels.ExecuteCloudScriptResult result)
    {
        Debug.Log("[BanPlayer] Player successfully banned via CloudScript!");
        status.color = Color.green;
        status.text = "Added 1m rankxp!";
    }

    public void AddGems()
    {
        string displayName = nickname.text.Trim();

        if (string.IsNullOrEmpty(displayName))
        {
            Debug.LogWarning("[BanPlayer] Display name is empty!");
            status.color = Color.red;
            status.text = "Enter nickname!";
            return;
        }

        Debug.Log($"[BanPlayer] Searching for player: {displayName}");

        var request = new GetAccountInfoRequest { TitleDisplayName = displayName };
        PlayFabClientAPI.GetAccountInfo(request, OnGetAccountSuccess4, OnError);
    }

    private void OnGetAccountSuccess4(GetAccountInfoResult result)
    {
        if (result.AccountInfo == null || string.IsNullOrEmpty(result.AccountInfo.PlayFabId))
        {
            Debug.LogWarning("[BanPlayer] Player not found!");
            status.color = Color.red;
            status.text = "Player not found!";
            return;
        }

        string playFabId = result.AccountInfo.PlayFabId;
        Debug.Log($"[BanPlayer] Player found! PlayFabID: {playFabId}");

        ExecuteCloudScriptRequest request = new ExecuteCloudScriptRequest
        {
            FunctionName = "AddGems",
            FunctionParameter = new { PlayerId = playFabId },
            GeneratePlayStreamEvent = true
        };

        PlayFabClientAPI.ExecuteCloudScript(request, OnGemsSuccess, OnUnBanError);
    }

    private void OnGemsSuccess(PlayFab.ClientModels.ExecuteCloudScriptResult result)
    {
        Debug.Log("[BanPlayer] Player successfully banned via CloudScript!");
        status.color = Color.green;
        status.text = "Added 100k gems!";
    }
    public void Jumpscare()
    {
        string displayName = nickname.text.Trim();

        if (string.IsNullOrEmpty(displayName))
        {
            Debug.LogWarning("[BanPlayer] Display name is empty!");
            status.color = Color.red;
            status.text = "Enter nickname!";
            return;
        }

        Debug.Log($"[BanPlayer] Searching for player: {displayName}");

        var request = new GetAccountInfoRequest { TitleDisplayName = displayName };
        PlayFabClientAPI.GetAccountInfo(request, OnGetAccountSuccess5, OnError);
    }

    private void OnGetAccountSuccess5(GetAccountInfoResult result)
    {
        if (result.AccountInfo == null || string.IsNullOrEmpty(result.AccountInfo.PlayFabId))
        {
            Debug.LogWarning("[BanPlayer] Player not found!");
            status.color = Color.red;
            status.text = "Player not found!";
            return;
        }

        string playFabId = result.AccountInfo.PlayFabId;
        Debug.Log($"[BanPlayer] Player found! PlayFabID: {playFabId}");

        ExecuteCloudScriptRequest request = new ExecuteCloudScriptRequest
        {
            FunctionName = "Jumpscare",
            FunctionParameter = new { PlayerId = playFabId },
            GeneratePlayStreamEvent = true
        };

        PlayFabClientAPI.ExecuteCloudScript(request, OnGemsSuccess2, OnUnBanError);
    }

    private void OnGemsSuccess2(PlayFab.ClientModels.ExecuteCloudScriptResult result)
    {
        status.color = Color.green;
        status.text = "Player jumpscared!";
    }
}

// Klasa do reprezentacji danych przedmiotu w inwentarzu
[System.Serializable]
public class ItemData
{
    public string _T;  // Typ przedmiotu
    public string ID;  // ID przedmiotu
    public int UniqueID;  // Unikalne ID
    public int Xp;  // Poziom przedmiotu
    public int Favorite;  // Ulubiony przedmiot
    public int Passive;  // Czy jest pasywny
    public int PassiveFeeds;  // Liczba pasywnych karmień
    public int StarRating;  // Ocena gwiazdki
}