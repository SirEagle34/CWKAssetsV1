using UnityEngine;
using UnityEngine.UI;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine.SceneManagement;
using TMPro;
using Newtonsoft.Json;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

public class login : MonoBehaviour
{
    public TMP_InputField nicknameInputField;
    public TMP_InputField passwordInputField;
    public TextMeshProUGUI errorText;
    public GameObject loginbt;
    public GameObject registerbt;
    public void RegisterAccount()
    {
        string nickname = nicknameInputField.text;
        string password = passwordInputField.text;

        // Sprawdzenie, czy nick spełnia wymagania
        if (string.IsNullOrEmpty(nickname) || nickname.Length < 3)
        {
            errorText.text = "Nickname must consist of at least 3 letters!";
            errorText.color = Color.red;
            return;
        }

        if (string.IsNullOrEmpty(password) || password.Length < 3)
        {
            errorText.text = "Password must consist of at least 3 letters!";
            errorText.color = Color.red;
            return;
        }

        if(PlayerPrefs.GetInt("Account", 0) == 1)
        {
            errorText.text = "You already have an account on this device!";
            errorText.color = Color.red;
            return;
        }
        // Wygenerowanie adresu email na podstawie nicku
        string email = nickname + "@gmail.com";

        // Ustawienie displayname użytkownika na podany nick
        string displayName = nickname;

        loginbt.SetActive(false);
        registerbt.SetActive(false);
        errorText.color = Color.green;
        errorText.text = "Register success!\n Please wait...";

        // Rejestracja konta PlayFab
        RegisterPlayFabAccount(nickname, email, password, displayName);
    }

    private void RegisterPlayFabAccount(string nickname, string email, string password, string displayName)
    {
        RegisterPlayFabUserRequest request = new RegisterPlayFabUserRequest
        {
            Username = nickname,
            Email = email,
            Password = password,
            DisplayName = displayName
        };

        PlayFabClientAPI.RegisterPlayFabUser(request, result =>
        {
            PlayerPrefs.SetInt("Account", 1);
            PlayerPrefs.SetString("Nickname", nicknameInputField.text);
            PlayerPrefs.SetString("Password", passwordInputField.text);
            PlayerPrefs.SetString("Email", email);
            SceneManager.LoadScene("BootVideoScene");
        }, error =>
        {
            errorText.text = "Register error!: " + error.ErrorMessage;
            errorText.color = Color.red;
            loginbt.SetActive(true);
            registerbt.SetActive(true);
        });
    }


    public void Login()
    {
        string nickname = nicknameInputField.text;
        string password = passwordInputField.text;

        // Sprawdzenie, czy podane dane są prawidłowe
        if (string.IsNullOrEmpty(nickname) || string.IsNullOrEmpty(password))
        {
            errorText.text = "Enter nickname and password!";
            errorText.color = Color.red;
            return;
        }

        // Wygenerowanie adresu email na podstawie nicku
        string email = nickname + "@gmail.com";
        errorText.text = "Login success!\n Please wait...";
        errorText.color = Color.green;
        loginbt.SetActive(false);
        registerbt.SetActive(false);
        // Logowanie do konta PlayFab
        LoginWithPlayFab(nickname, email, password);
    }

    private void LoginWithPlayFab(string nickname, string email, string password)
    {
        LoginWithPlayFabRequest request = new LoginWithPlayFabRequest
        {
            Username = nickname,
            Password = password
        };

        PlayFabClientAPI.LoginWithPlayFab(request, result =>
        {
            PlayerPrefs.SetString("Nickname", nicknameInputField.text);
            PlayerPrefs.SetString("Password", passwordInputField.text);
            PlayerPrefs.SetString("Email", email);
            LoadAndAssignCustomPlayerPrefs();
            // Tutaj można wykonać operacje po zalogowaniu, np. przejście do menu głównego.
        }, error =>
        {
            errorText.text = "Login failed!: " + error.ErrorMessage;
            errorText.color = Color.red;
            loginbt.SetActive(true);
            registerbt.SetActive(true);
        });
    }




    public void LoadAndAssignCustomPlayerPrefs()
    {
        PlayerSaveData saveData = Singleton<PlayerInfoScript>.Instance.SaveData;
        PlayerInfoScript playerInfo = Singleton<PlayerInfoScript>.Instance;
        // Przygotowanie listy nazw statystyk do wczytania
        var statisticNames = new string[] { "Gems", "Coins", "RankXP", "Muryo", "ReadWriteParam", "SelectedLoadout", "MyHelperCreatureID", "PvpSpecialDomainNumber", "SelectedLoadout", "MultiplayerWinStreak",
       "ExpeditionSlots", "ADMIN", "InventorySpace", "TopCompletedQuestId", "Ban", "OneTimeCalendarDaysClaimed", "BestMultiplayerLevel", "MultiplayerLevel",
            "PvpRankRewardsGranted", "PointsInMultiplayerLevel", "TopShownCompletedQuestId"};

        // Przygotowanie żądania wczytania statystyk
        var request = new GetPlayerStatisticsRequest
        {
            StatisticNames = new System.Collections.Generic.List<string>(statisticNames)
        };

        // Wysłanie żądania wczytania statystyk
        PlayFabClientAPI.GetPlayerStatistics(request, result =>
        {
            // Przetwarzanie wczytanych statystyk
            foreach (var stat in result.Statistics)
            {
                // Sprawdzanie, która statystyka została wczytana
                // i przypisanie jej do odpowiedniego klucza PlayerPrefs
                if (stat.StatisticName == "Gems")
                {
                    saveData.HardCurrency = stat.Value;
                    Debug.Log("Wczytano statystykę Gems: " + stat.Value);
                }
                else if (stat.StatisticName == "Coins")
                {
                    saveData.SoftCurrency = stat.Value;
                }
                else if (stat.StatisticName == "RankXP")
                {
                    saveData.RankXP = stat.Value;
                }
                else if (stat.StatisticName == "Muryo")
                {
                    saveData.Muryo = stat.Value;
                }
                else if (stat.StatisticName == "ReadWriteParam")
                {
                    saveData.ReadWriteParam = stat.Value;
                }
                else if (stat.StatisticName == "SelectedLoadout")
                {
                    saveData.SelectedLoadout = stat.Value;
                }
                else if (stat.StatisticName == "MyHelperCreatureID")
                {
                    saveData.MyHelperCreatureID = stat.Value;
                }
                else if (stat.StatisticName == "PvpSpecialDomainNumber")
                {
                    saveData.PvpSpecialDomainNumber = stat.Value;
                }
                else if (stat.StatisticName == "SelectedLoadout")
                {
                    saveData.SelectedLoadout = stat.Value;
                }
                else if (stat.StatisticName == "MultiplayerWinStreak")
                {
                    saveData.MultiplayerWinStreak = stat.Value;
                }
                else if (stat.StatisticName == "ExpeditionSlots")
                {
                    saveData.ExpeditionSlots = stat.Value;
                }
                else if (stat.StatisticName == "ADMIN")
                {
                    PlayerPrefs.SetInt("Admin", stat.Value);
                }
                else if (stat.StatisticName == "InventorySpace")
                {
                    saveData.InventorySpace = stat.Value;
                }
                else if (stat.StatisticName == "TopCompletedQuestId")
                {
                    saveData.TopCompletedQuestId = stat.Value;
                }
                else if (stat.StatisticName == "Ban")
                {
                    PlayerPrefs.SetInt("Ban", stat.Value);
                }
                else if (stat.StatisticName == "OneTimeCalendarDaysClaimed")
                {
                    saveData.OneTimeCalendarDaysClaimed = stat.Value;
                }
                else if (stat.StatisticName == "BestMultiplayerLevel")
                {
                    saveData.BestMultiplayerLevel = stat.Value;
                }
                else if (stat.StatisticName == "MultiplayerLevel")
                {
                    saveData.MultiplayerLevel = stat.Value;
                }
                else if (stat.StatisticName == "PvpRankRewardsGranted")
                {
                    saveData.PvpRankRewardsGranted = stat.Value;
                }
                else if (stat.StatisticName == "PointsInMultiplayerLevel")
                {
                    saveData.PointsInMultiplayerLevel = stat.Value;
                }
                else if (stat.StatisticName == "TopShownCompletedQuestId")
                {
                    saveData.TopShownCompletedQuestId = stat.Value;
                }
            }

            PlayFabClientAPI.GetUserData(new GetUserDataRequest(), userDataResult =>
       {
           if (userDataResult.Data != null && userDataResult.Data.ContainsKey("Nickname"))
           {
               string nickName = userDataResult.Data["Nickname"].Value;
               saveData.MultiplayerPlayerName = nickName;
               Debug.Log("Wczytano statystykę Nickname: " + nickName);
           }

           if (userDataResult.Data.ContainsKey("HasAuthenticated"))
           {
               bool.TryParse(userDataResult.Data["HasAuthenticated"].Value, out bool hasAuthenticated);
               saveData.HasAuthenticated = hasAuthenticated;
           }

           if (userDataResult.Data.ContainsKey("SerializedUnlocks"))
           {
               string serializedUnlocks = userDataResult.Data["SerializedUnlocks"].Value;
               try
               {
                   // Sprawdzanie czy dane to tablica JSON
                   JArray unlockArray = JArray.Parse(serializedUnlocks);
                   object[] unlockArrayObj = unlockArray.ToObject<object[]>();
                   playerInfo.DeserializeUnlocks(unlockArrayObj);
               }
               catch (JsonReaderException ex)
               {
                   Debug.LogError("Błąd w deserializacji SerializedUnlocks: " + ex.Message);
               }
           }
           if (userDataResult.Data.ContainsKey("TutorialsDone"))
           {
               string tutorialsDone = userDataResult.Data["TutorialsDone"].Value;
               try
               {
                   // Sprawdzanie czy dane to tablica JSON
                   JArray tutorialsArray = JArray.Parse(tutorialsDone);
                   object[] tutorialsArrajObj = tutorialsArray.ToObject<object[]>();
                   TutorialDataManager.Instance.Deserialize(tutorialsArrajObj);
               }
               catch (JsonReaderException ex)
               {
                   Debug.LogError("Błąd w deserializacji TutorialsDone: " + ex.Message);
               }
           }
           if (userDataResult.Data.ContainsKey("PlayedFirstBattleInPvpSeason"))
           {
               string value = userDataResult.Data["PlayedFirstBattleInPvpSeason"].Value;
               saveData.PlayedFirstBattleInPvpSeason = value == "1";
           }

           // Cargar inventario completo
           if (userDataResult.Data.ContainsKey("SerializedInventory"))
           {
               string serializedInventory = userDataResult.Data["SerializedInventory"].Value;
               try
               {
                   JArray inventoryArray = JArray.Parse(serializedInventory);
                   List<Dictionary<string, object>> inventoryList = inventoryArray.ToObject<List<Dictionary<string, object>>>();
                   if (inventoryList != null && inventoryList.Count > 0)
                   {
                       playerInfo.DeserializeInventory(inventoryList.ToArray());
                       Debug.Log("✅ Inventario cargado: " + inventoryList.Count + " items");
                   }
               }
               catch (JsonReaderException ex)
               {
                   Debug.LogError("Błąd w deserializacji Inventory: " + ex.Message);
               }
           }

           // Cargar Leaders
           if (userDataResult.Data.ContainsKey("Leaders"))
           {
               string leaders = userDataResult.Data["Leaders"].Value;
               try
               {
                   JArray leadersArray = JArray.Parse(leaders);
                   List<Dictionary<string, object>> leadersList = leadersArray.ToObject<List<Dictionary<string, object>>>();
                   if (leadersList != null && leadersList.Count > 0)
                   {
                       playerInfo.DeserializeLeaders(leadersList.ToArray());
                       Debug.Log("✅ Leaders cargados: " + leadersList.Count);
                   }
               }
               catch (JsonReaderException ex)
               {
                   Debug.LogError("Błąd w deserializacji Leaders: " + ex.Message);
               }
           }

           // Cargar EvoMatCollection
           if (userDataResult.Data.ContainsKey("EvoMatCollection"))
           {
               string evoMatCollection = userDataResult.Data["EvoMatCollection"].Value;
               try
               {
                   JArray evoMatArray = JArray.Parse(evoMatCollection);
                   List<Dictionary<string, object>> evoMatList = evoMatArray.ToObject<List<Dictionary<string, object>>>();
                   if (evoMatList != null && evoMatList.Count > 0)
                   {
                       playerInfo.DeserializeEvoMatCollection(evoMatList.ToArray());
                       Debug.Log("✅ EvoMat cargados: " + evoMatList.Count);
                   }
               }
               catch (JsonReaderException ex)
               {
                   Debug.LogError("Błąd w deserializacji EvoMatCollection: " + ex.Message);
               }
           }

           // Cargar CardBacks
           if (userDataResult.Data.ContainsKey("SerializeCardBacks"))
           {
               string serializedCardBacks = userDataResult.Data["SerializeCardBacks"].Value;
               try
               {
                   if (serializedCardBacks.StartsWith("\"") && serializedCardBacks.EndsWith("\""))
                   {
                       serializedCardBacks = JsonConvert.DeserializeObject<string>(serializedCardBacks);
                   }

                   JToken parsedData = JToken.Parse(serializedCardBacks);
                   if (parsedData is JArray cardBacksArray)
                   {
                       string[] cardBacksStringArray = cardBacksArray.ToObject<string[]>();
                       playerInfo.DeserializeCardBacks(cardBacksStringArray);
                       Debug.Log("✅ CardBacks cargados: " + cardBacksStringArray.Length);
                   }
               }
               catch (JsonReaderException ex)
               {
                   Debug.LogError("Błąd w deserializacji CardBacks: " + ex.Message);
               }
           }

           // Cargar UsedRedeemCodes
           if (userDataResult.Data.ContainsKey("SerializeUsedRedeemCodes"))
           {
               string serializedUsedRedeemCodes = userDataResult.Data["SerializeUsedRedeemCodes"].Value;
               try
               {
                   JArray usedRedeemCodesArray = JArray.Parse(serializedUsedRedeemCodes);
                   List<Dictionary<string, object>> usedRedeemCodesList = usedRedeemCodesArray.ToObject<List<Dictionary<string, object>>>();
                   if (usedRedeemCodesList != null && usedRedeemCodesList.Count > 0)
                   {
                       playerInfo.DeserializeUsedRedeemCodes(usedRedeemCodesList.ToArray());
                       Debug.Log("✅ UsedRedeemCodes cargada: " + usedRedeemCodesList.Count);
                   }
               }
               catch (JsonReaderException ex)
               {
                   Debug.LogError("Błąd w deserializacji UsedRedeemCodes: " + ex.Message);
               }
           }

           // Cargar CreatureCollection
           if (userDataResult.Data.ContainsKey("SerializedCreatureCollection"))
           {
               string serializedCreatureCollection = userDataResult.Data["SerializedCreatureCollection"].Value;
               try
               {
                   JArray creatureCollectionArray = JArray.Parse(serializedCreatureCollection);
                   List<Dictionary<string, object>> creatureCollectionList = creatureCollectionArray.ToObject<List<Dictionary<string, object>>>();
                   if (creatureCollectionList != null && creatureCollectionList.Count > 0)
                   {
                       playerInfo.DeserializeCreatureCollection(creatureCollectionList.ToArray());
                       Debug.Log("✅ CreatureCollection cargada: " + creatureCollectionList.Count);
                   }
               }
               catch (JsonReaderException ex)
               {
                   Debug.LogError("Błąd w deserializacji CreatureCollection: " + ex.Message);
               }
           }

           // Cargar CardCollection
           if (userDataResult.Data.ContainsKey("SerializeCardCollection"))
           {
               string serializeCardCollection = userDataResult.Data["SerializeCardCollection"].Value;
               try
               {
                   JArray cardCollectionArray = JArray.Parse(serializeCardCollection);
                   List<Dictionary<string, object>> cardCollectionList = cardCollectionArray.ToObject<List<Dictionary<string, object>>>();
                   if (cardCollectionList != null && cardCollectionList.Count > 0)
                   {
                       playerInfo.DeserializeCardCollection(cardCollectionList.ToArray());
                       Debug.Log("✅ CardCollection cargada: " + cardCollectionList.Count);
                   }
               }
               catch (JsonReaderException ex)
               {
                   Debug.LogError("Błąd w deserializacji CardCollection: " + ex.Message);
               }
           }

           // Cargar CreatureSorts
           if (userDataResult.Data.ContainsKey("SerializeCreatureSorts"))
           {
               string serializeCreatureSorts = userDataResult.Data["SerializeCreatureSorts"].Value;
               try
               {
                   JArray creatureSortsArray = JArray.Parse(serializeCreatureSorts);
                   List<Dictionary<string, object>> creatureSortsList = creatureSortsArray.ToObject<List<Dictionary<string, object>>>();
                   if (creatureSortsList != null && creatureSortsList.Count > 0)
                   {
                       playerInfo.DeserializeCreatureSorts(creatureSortsList.ToArray());
                       Debug.Log("✅ CreatureSorts cargados: " + creatureSortsList.Count);
                   }
               }
               catch (JsonReaderException ex)
               {
                   Debug.LogError("Błąd w deserializacji CreatureSorts: " + ex.Message);
               }
           }

           // Cargar Expeditions
           if (userDataResult.Data.ContainsKey("Expeditions"))
           {
               string expeditions = userDataResult.Data["Expeditions"].Value;
               try
               {
                   JArray expeditionsArray = JArray.Parse(expeditions);
                   List<Dictionary<string, object>> expeditionsList = expeditionsArray.ToObject<List<Dictionary<string, object>>>();
                   if (expeditionsList != null && expeditionsList.Count > 0)
                   {
                       DetachedSingleton<ExpeditionManager>.Instance.Deserialize(expeditionsList.ToArray());
                       Debug.Log("✅ Expeditions cargadas: " + expeditionsList.Count);
                   }
               }
               catch (JsonReaderException ex)
               {
                   Debug.LogError("Błąd w deserializacji Expeditions: " + ex.Message);
               }
           }

           // Cargar Loadouts
           if (userDataResult.Data.ContainsKey("SerializedLoadouts"))
           {
               string serializedLoadouts = userDataResult.Data["SerializedLoadouts"].Value;
               try
               {
                   JArray loadoutsArray = JArray.Parse(serializedLoadouts);
                   List<Dictionary<string, object>> loadoutsList = loadoutsArray.ToObject<List<Dictionary<string, object>>>();
                   if (loadoutsList != null && loadoutsList.Count > 0)
                   {
                       playerInfo.DeserializeLoadouts(loadoutsList.ToArray());
                       Debug.Log("✅ Loadouts cargados: " + loadoutsList.Count);
                   }
               }
               catch (JsonReaderException ex)
               {
                   Debug.LogError("Błąd w deserializacji Loadouts: " + ex.Message);
               }
           }

           // Cargar QuestStars
           if (userDataResult.Data.ContainsKey("SerializeQuestStars"))
           {
               string serializedQuestStars = userDataResult.Data["SerializeQuestStars"].Value;
               try
               {
                   int[] questStarsArray = JsonConvert.DeserializeObject<int[]>(serializedQuestStars);
                   if (questStarsArray != null && questStarsArray.Length > 0)
                   {
                       playerInfo.DeserializeQuestStars(questStarsArray);
                       Debug.Log("✅ QuestStars cargados: " + questStarsArray.Length);
                   }
               }
               catch (JsonReaderException ex)
               {
                   Debug.LogError("Błąd w deserializacji QuestStars: " + ex.Message);
               }
           }

           Debug.Log("🎮 ¡TODOS LOS DATOS CARGADOS DESDE PLAYFAB!");
           PlayerPrefs.SetInt("AfterLogin", 1);
           PlayerPrefs.Save(); // Zapisanie PlayerPrefs
           SceneManager.LoadScene("BootVideoScene");
       }, error =>
       {
           Debug.Log("Błąd w pobieraniu statystyk!");
       });
        }, error =>
    {
        Debug.Log("Błąd w pobieraniu statystyk: " + error.GenerateErrorReport());
    });
    }
}