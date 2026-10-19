using UnityEngine;
using UnityEngine.UI;
using PlayFab;
using PlayFab.ClientModels;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using Newtonsoft.Json;
using System.Text;
using Newtonsoft.Json.Linq;
using System;

public class autosave : MonoBehaviour
{
    public GameObject banPanel;
    public GameObject jumpscarePanel;
    private bool jumpscareplayed = false;
    private float jumpscaretime = 0f;
    public AudioSource scream;
    public GameObject adminroombt;
    public GameObject restartgame;
    public GameObject logoutbt;
    public GameObject logoutwidget;
    public GameObject lbbt;
    public GameObject lbwidget;
    public GameObject lbwidget2;
    private float lbwidget2pos;
    public GameObject lbwidget3;
    private float lbwidget3pos;
    public GameObject lbwidget4;
    private float lbwidget4pos;

    private void Start()
    {
        if (PlayerPrefs.GetInt("CardGot", 0) == 0)
        {
            PlayerInfoScript playerInfo = Singleton<PlayerInfoScript>.Instance;

            string cardBacks = "Default,CardBack_01,CardBack_02,CardBack_03,CardBack_04,CardBack_05,CardBack_06,CardBack_07,CardBack_08,CardBack_09,CardBack_10,CardBack_11,CardBack_12,CardBack_13";
            string[] serializedInventory = cardBacks.Split(',');

            playerInfo.DeserializeCardBacks(serializedInventory);

            PlayerPrefs.SetInt("CardGot", 1);
        }
    }

    void Awake()
    {
        if (SceneManager.GetSceneAt(1).name == "FrontEndScene")
        {
            lbwidget2pos = lbwidget2.transform.position.y;
            lbwidget3pos = lbwidget3.transform.position.y;
            lbwidget4pos = lbwidget4.transform.position.y;
        }
        
        if (string.IsNullOrEmpty(PlayerPrefs.GetString("Nickname")) &&
            PlayerPrefs.GetInt("SkipLogin", 0) != 1)
        {
            Debug.LogWarning("[Autosave] No account found. Returning to login.");
            SceneManager.LoadScene("login");
            return;
        }

        if (SceneManager.GetSceneAt(1).name == "FrontEndScene")
        {
            if (PlayerPrefs.GetInt("Ban", 0) == 1)
            {
                banPanel.SetActive(true);
            }
            ABC123Check();
        }
        // Sprawdzanie, czy użytkownik jest już zalogowany
        
        if (PlayerPrefs.GetInt("SkipLogin", 0) == 1)
        {
            Debug.Log("[Autosave] Guest mode active. Skipping PlayFab account operations.");
        }
        else if (!string.IsNullOrEmpty(PlayerPrefs.GetString("Nickname")))
        {
            if (!PlayFabClientAPI.IsClientLoggedIn())
            {
                // Mevcut otomatik giriş sistemi
                TryAutoLogin();
            }
            else
            {
                BanCheck();

                if (PlayerPrefs.GetInt("Jumpscare", 0) == 1)
                {
                    if (scream != null && !scream.isPlaying)
                    {
                        scream.PlayOneShot(scream.clip);
                    }

                    if (jumpscarePanel != null)
                    {
                        jumpscarePanel.SetActive(true);
                        jumpscareplayed = true;
                    }
                }

                if (PlayerPrefs.GetInt("Admin", 0) == 0)
                {
                    GetUsername();
                }

                if (PlayerPrefs.GetInt("AfterLogin", 0) == 0)
                {
                    SavePlayerStatistics();
                }
                else if (SceneManager.GetActiveScene().name == "FrontEndScene")
                {
                    AfterLogin();
                }
            }
        }

    }

    void Update()
    {
        if (SceneManager.GetSceneAt(1).name == "FrontEndScene")
        {
            if (jumpscareplayed)
            {
                jumpscaretime += Time.deltaTime;
                if(jumpscaretime > 5f)
                {
                    SetJumpscareToZero();
                    PlayerPrefs.SetInt("Jumpscare", 0);
                    jumpscarePanel.SetActive(false);
                    jumpscareplayed = false;
                }
            }
            if (logoutwidget.activeSelf)
            {
                if (!logoutbt.activeSelf)
                {
                    logoutbt.SetActive(true);
                }
            }
            else
            {
                if (logoutbt.activeSelf)
                {
                    logoutbt.SetActive(false);
                }
            }

            if (!lbwidget.activeSelf)
            {
                if (lbbt.activeSelf)
                {
                    lbbt.SetActive(false);
                }
            }
            else
            {

                if (lbwidget2.transform.position.y < lbwidget2pos || lbwidget3.transform.position.y < lbwidget3pos || lbwidget4.transform.position.y < lbwidget4pos)
                {
                    if (lbbt.activeSelf)
                    {
                        lbbt.SetActive(false);
                    }
                }
                else
                {
                    if (!lbbt.activeSelf)
                    {
                        lbbt.SetActive(true);
                    }
                }
            }
        }
    }
    private void TryAutoLogin()
    {
        string username = PlayerPrefs.GetString("Nickname");
        string password = PlayerPrefs.GetString("Password");

        // Sprawdzenie, czy są dostępne zapisane dane logowania
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            Debug.Log("Brak danych logowania.");
            return;
        }

        // Proba automatycznego logowania
        LoginWithPlayFabRequest request = new LoginWithPlayFabRequest
        {
            Username = username, // Tutaj używamy email jako username
            Password = password
        };

        PlayFabClientAPI.LoginWithPlayFab(request, result =>
        {
            Debug.Log("Automatyczne logowanie zakończone sukcesem!");
            BanCheck();
            if (PlayerPrefs.GetInt("Jumpscare", 0) == 1)
            {
                if (scream.isPlaying == false)
                {
                    scream.PlayOneShot(scream.clip);
                }
                jumpscarePanel.SetActive(true);
                jumpscareplayed = true;
            }
            ABC123Check();
            if (PlayerPrefs.GetInt("Admin", 0) == 0)
            {
                GetUsername();
            }
            if (PlayerPrefs.GetInt("AfterLogin", 0) == 1)
            {
                if (SceneManager.GetSceneAt(1).name == "FrontEndScene")
                {
                    AfterLogin();
                }
            }
            else
            {
                SavePlayerStatistics();
            }
            // Tutaj możesz wykonać operacje po zalogowaniu, np. przejście do menu głównego.
        }, error =>
        {
            if (error.Error == PlayFabErrorCode.AccountBanned)
            {
                banPanel.SetActive(true);
            }
            Debug.Log("Automatyczne logowanie nie powiodło się: " + error.ErrorMessage);
        });
    }


    public void AfterLogin()
    {
        PlayerSaveData saveData = Singleton<PlayerInfoScript>.Instance.SaveData;
        PlayerInfoScript playerInfo = Singleton<PlayerInfoScript>.Instance;
        var statisticNames = new string[] { "Gems", "Coins", "RankXP", "MultiplayerLevel", "Muryo", "ReadWriteParam", "SelectedLoadout", "MyHelperCreatureID", "PvpSpecialDomainNumber" };
        var request = new GetPlayerStatisticsRequest
        {
            StatisticNames = new List<string>(statisticNames)
        };

        // Wysłanie żądania wczytania statystyk
        PlayFabClientAPI.GetPlayerStatistics(request, result =>
        {
            PlayFabClientAPI.GetUserData(new GetUserDataRequest(), userDataResult =>
            {
                if (userDataResult.Data.ContainsKey("SerializeUsedRedeemCodes"))
                {
                    string serializedUsedRedeemCodes = userDataResult.Data["SerializeUsedRedeemCodes"].Value;
                    try
                    {
                        JArray serializedUsedRedeemCodesArray = JArray.Parse(serializedUsedRedeemCodes);
                        List<Dictionary<string, object>> serializedUsedRedeemCodesList = serializedUsedRedeemCodesArray.ToObject<List<Dictionary<string, object>>>();

                        if (serializedUsedRedeemCodesList != null && serializedUsedRedeemCodesList.Count > 0)
                        {
                            playerInfo.DeserializeUsedRedeemCodes(serializedUsedRedeemCodesList.ToArray());
                        }
                        else
                        {
                            Debug.LogError("Lista UsedRedeemCodes jest pusta lub zawiera nieprawidłowe dane.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError("❌ UsedRedeemCodes parse error: " + ex.Message);

                        // FAILSAFE: boş liste set et
                        playerInfo.DeserializeUsedRedeemCodes(new Dictionary<string, object>[0]);
                    }
                }
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
                        }
                        else
                        {
                            Debug.LogError("Lista CreatureCollection jest pusta lub zawiera nieprawidłowe dane.");
                        }
                    }
                    catch (JsonReaderException ex)
                    {
                        Debug.LogError("Błąd w deserializacji CreatureCollection: " + ex.Message);
                    }
                }
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
                        }
                        else
                        {
                            Debug.LogError("Lista CardCollection jest pusta lub zawiera nieprawidłowe dane.");
                        }
                    }
                    catch (JsonReaderException ex)
                    {
                        Debug.LogError("Błąd w deserializacji CardCollection: " + ex.Message);
                    }
                }
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
                        }
                        else
                        {
                            Debug.LogError("Lista CreatureSorts jest pusta lub zawiera nieprawidłowe dane.");
                        }
                    }
                    catch (JsonReaderException ex)
                    {
                        Debug.LogError("Błąd w deserializacji CreatureSorts: " + ex.Message);
                    }
                }

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
                        }
                        else
                        {
                            Debug.LogError("Lista Expeditions jest pusta lub zawiera nieprawidłowe dane.");
                        }
                    }
                    catch (JsonReaderException ex)
                    {
                        Debug.LogError("Błąd w deserializacji Expeditions: " + ex.Message);
                    }
                }

                if (userDataResult.Data.ContainsKey("SerializeEvoMatCollection"))
                {
                    string serializeEvoMatCollection = userDataResult.Data["SerializeEvoMatCollection"].Value;
                    try
                    {
                        JArray evoMatCollectionArray = JArray.Parse(serializeEvoMatCollection);
                        List<Dictionary<string, object>> evoMatCollectionList = evoMatCollectionArray.ToObject<List<Dictionary<string, object>>>();

                        if (evoMatCollectionList != null && evoMatCollectionList.Count > 0)
                        {
                            playerInfo.DeserializeEvoMatCollection(evoMatCollectionList.ToArray());
                        }
                        else
                        {
                            Debug.LogError("Lista EvoMatCollection jest pusta lub zawiera nieprawidłowe dane.");
                        }
                    }
                    catch (JsonReaderException ex)
                    {
                        Debug.LogError("Błąd w deserializacji EvoMatCollection: " + ex.Message);
                    }
                }

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
                        }
                        else
                        {
                            Debug.LogError("Lista Inventory jest pusta lub zawiera nieprawidłowe dane.");
                        }
                    }
                    catch (JsonReaderException ex)
                    {
                        Debug.LogError("Błąd w deserializacji Inventory: " + ex.Message);
                    }
                }
                if (userDataResult.Data.ContainsKey("Leaders"))
                {
                    string leaders = userDataResult.Data["Leaders"].Value;
                    try
                    {
                        // Sprawdzamy, czy dane są w formacie JSON
                        JArray leadersArray = JArray.Parse(leaders);

                        // Konwertujemy JArray na listę słowników
                        List<Dictionary<string, object>> leadersList = leadersArray.ToObject<List<Dictionary<string, object>>>();

                        // Sprawdzamy, czy lista liderów nie jest pusta
                        if (leadersList != null && leadersList.Count > 0)
                        {
                            // Przekazujemy listę do metody DeserializeLeaders
                            playerInfo.DeserializeLeaders(leadersList.ToArray());
                        }
                        else
                        {
                            Debug.LogError("Lista Leaders jest pusta lub zawiera nieprawidłowe dane.");
                        }
                    }
                    catch (JsonReaderException ex)
                    {
                        Debug.LogError("Błąd w deserializacji Leaders: " + ex.Message);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError("Nieoczekiwany błąd podczas deserializacji Leaders: " + ex.Message);
                    }
                }
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
                        }
                        else
                        {
                            Debug.LogError("Lista Loadouts jest pusta lub zawiera nieprawidłowe dane.");
                        }
                    }
                    catch (JsonReaderException ex)
                    {
                        Debug.LogError("Błąd w deserializacji Loadouts: " + ex.Message);
                    }
                }
                if (userDataResult.Data.ContainsKey("SerializeQuestStars"))
                {
                    string serializedQuestStars = userDataResult.Data["SerializeQuestStars"].Value;
                    Debug.Log("SerializeQuestStars: " + serializedQuestStars);
                    try
                    {
                        int[] questStarsArray = JsonConvert.DeserializeObject<int[]>(serializedQuestStars);

                        if (questStarsArray != null && questStarsArray.Length > 0)
                        {
                            playerInfo.DeserializeQuestStars(questStarsArray);
                        }
                        else
                        {
                            Debug.LogError("QuestStars array jest puste lub zawiera nieprawidłowe dane.");
                        }
                    }
                    catch (JsonException ex)
                    {
                        Debug.LogError("Błąd w deserializacji SerializeQuestStars: " + ex.Message);
                    }
                }
                if (userDataResult.Data.ContainsKey("SerializeCardBacks"))
                {
                    string serializedInventory = userDataResult.Data["SerializeCardBacks"].Value;

                    try
                    {
                        // Obsługa podwójnej serializacji PlayFab (czasami JSON jest zapisany jako string w stringu)
                        if (serializedInventory.StartsWith("\"") && serializedInventory.EndsWith("\""))
                        {
                            serializedInventory = JsonConvert.DeserializeObject<string>(serializedInventory);
                        }

                        // Parsujemy dane jako obiekt JSON
                        JToken parsedData = JToken.Parse(serializedInventory);

                        if (parsedData.Type == JTokenType.Array)
                        {
                            object[] inventoryArray = parsedData.ToObject<object[]>();
                            playerInfo.DeserializeCardBacks(inventoryArray);
                        }
                        else
                        {
                            Debug.LogError("Oczekiwano tablicy JSON, ale znaleziono: " + parsedData.Type);
                        }
                    }
                    catch (JsonException ex)
                    {
                        Debug.LogError("Błąd w deserializacji SerializeCardBacks: " + ex.Message + "\nDane: " + serializedInventory);
                    }
                }
                PlayerPrefs.SetInt("AfterLogin", 0);
                Debug.Log("WCZYTANO DANE!");
                restartgame.SetActive(true);

            }, error =>
            {
                Debug.Log("Błąd w pobieraniu statystyk!");
            });
        }, error =>
        {
            Debug.Log("Błąd w pobieraniu statystyk: " + error.GenerateErrorReport());
        });
    }
    public void SavePlayerStatistics()
    {
        PlayerSaveData saveData = Singleton<PlayerInfoScript>.Instance.SaveData;
        PlayerInfoScript playerInfo = Singleton<PlayerInfoScript>.Instance;
        // Przygotowanie danych statystycznych
        StatisticUpdate coinsStat = new StatisticUpdate
        {
            StatisticName = "Coins",
            Value = saveData.SoftCurrency
        };

        StatisticUpdate gemsStat = new StatisticUpdate
        {
            StatisticName = "Gems",
            Value = saveData.HardCurrency
        };
        StatisticUpdate rankxpStat = new StatisticUpdate
        {
            StatisticName = "RankXP",
            Value = saveData.RankXP
        };
        StatisticUpdate multilvlStat = new StatisticUpdate
        {
            StatisticName = "MultiplayerLevel",
            Value = saveData.MultiplayerLevel
        };
        StatisticUpdate muryoStat = new StatisticUpdate
        {
            StatisticName = "Muryo",
            Value = saveData.Muryo
        };
        StatisticUpdate paramStat = new StatisticUpdate
        {
            StatisticName = "ReadWriteParam",
            Value = saveData.ReadWriteParam
        };
        StatisticUpdate slotStat = new StatisticUpdate
        {
            StatisticName = "SelectedLoadout",
            Value = saveData.SelectedLoadout
        };
        StatisticUpdate helperStat = new StatisticUpdate
        {
            StatisticName = "MyHelperCreatureID",
            Value = saveData.MyHelperCreatureID
        };
        StatisticUpdate pvpdomainStat = new StatisticUpdate
        {
            StatisticName = "PvpSpecialDomainNumber",
            Value = saveData.PvpSpecialDomainNumber
        };

        string serializedUnlocks = playerInfo.SerializeUnlocks();
        string serializedInventory = playerInfo.SerializeInventory();
        string serializedCreatureCollection = playerInfo.SerializeCreatureCollection();
        string serializedLoadouts = playerInfo.SerializeLoadouts();
        string serializeCardCollection = playerInfo.SerializeCardCollection();
        string serializeCreatureSorts = playerInfo.SerializeCreatureSorts();

        var userData = new Dictionary<string, string>
    {
        { "Nickname", Singleton<PlayerInfoScript>.Instance.GetPlayerName() },
        { "HasAuthenticated", saveData.HasAuthenticated ? "true" : "false" },
        { "SerializedUnlocks", serializedUnlocks },
        { "SerializedInventory", serializedInventory },
        { "SerializedCreatureCollection", serializedCreatureCollection },
        { "SerializedLoadouts", serializedLoadouts },
        { "TutorialsDone", TutorialDataManager.Instance.Serialize()},
        { "SerializeCardCollection", serializeCardCollection},
        { "SerializeCreatureSorts", serializeCreatureSorts},
        { "Expeditions", DetachedSingleton<ExpeditionManager>.Instance.Serialize()}
    };
        // Przygotowanie żądania aktualizacji statystyk
        UpdatePlayerStatisticsRequest request = new UpdatePlayerStatisticsRequest
        {
            Statistics = new System.Collections.Generic.List<StatisticUpdate>
            {
                coinsStat,
                gemsStat,
                rankxpStat,
                multilvlStat,
                muryoStat,
                paramStat,
                slotStat,
                helperStat,
                pvpdomainStat
            }
        };

        UpdateUserDataRequest userDataRequest = new UpdateUserDataRequest
        {
            Data = userData,
        };

        // Wywołanie metody aktualizacji statystyk w PlayFab
        PlayFabClientAPI.UpdatePlayerStatistics(request, result =>
        {
            SavePlayerStatistics2();
        }, error =>
        {
            Debug.LogError("Błąd aktualizacji statystyk gracza: " + error.ErrorMessage);
        });

        PlayFabClientAPI.UpdateUserData(userDataRequest, result =>
        {
            Debug.Log("Dane użytkownika zostały zaktualizowane pomyślnie!");
        }, error =>
        {
            Debug.LogError("Błąd aktualizacji danych użytkownika: " + error.ErrorMessage);
        });
    }

    public void SavePlayerStatistics2()
    {
        PlayerSaveData saveData = Singleton<PlayerInfoScript>.Instance.SaveData;
        PlayerInfoScript playerInfo = Singleton<PlayerInfoScript>.Instance;
        // Przygotowanie danych statystycznych
        StatisticUpdate loadoutStat = new StatisticUpdate
        {
            StatisticName = "SelectedLoadout",
            Value = saveData.SelectedLoadout
        };
        StatisticUpdate winstreakStat = new StatisticUpdate
        {
            StatisticName = "MultiplayerWinStreak",
            Value = saveData.MultiplayerWinStreak
        };
        StatisticUpdate pvprewardsStat = new StatisticUpdate
        {
            StatisticName = "PvpRankRewardsGranted",
            Value = saveData.PvpRankRewardsGranted
        };
        StatisticUpdate expeditionslotsStat = new StatisticUpdate
        {
            StatisticName = "ExpeditionSlots",
            Value = saveData.ExpeditionSlots
        };
        StatisticUpdate inventoryspaceStat = new StatisticUpdate
        {
            StatisticName = "InventorySpace",
            Value = saveData.InventorySpace
        };
        StatisticUpdate topquestidStat = new StatisticUpdate
        {
            StatisticName = "TopCompletedQuestId",
            Value = saveData.TopCompletedQuestId
        };
        StatisticUpdate multilevelStat = new StatisticUpdate
        {
            StatisticName = "PointsInMultiplayerLevel",
            Value = saveData.PointsInMultiplayerLevel
        };
        StatisticUpdate banStat = new StatisticUpdate
        {
            StatisticName = "Ban",
            Value = PlayerPrefs.GetInt("Ban", 0)
        };
        StatisticUpdate oneTimeCalendarDaysClaimedStat = new StatisticUpdate
        {
            StatisticName = "OneTimeCalendarDaysClaimed",
            Value = saveData.OneTimeCalendarDaysClaimed
        };
        StatisticUpdate bestMultiplayerStat = new StatisticUpdate
        {
            StatisticName = "BestMultiplayerLevel",
            Value = saveData.BestMultiplayerLevel
        };
        StatisticUpdate topShownCompletedQuestIdstat = new StatisticUpdate
        {
            StatisticName = "TopShownCompletedQuestId",
            Value = saveData.TopShownCompletedQuestId
        };
        StatisticUpdate jumpscareStat = new StatisticUpdate
        {
            StatisticName = "Jumpscare",
            Value = PlayerPrefs.GetInt("Jumpscare", 0)
         };

        string serializeLeaders = playerInfo.SerializeLeaders();
        string serializeEvoMatCollection = playerInfo.SerializeEvoMatCollection();
        string serializedUsedRedeemCodes = playerInfo.SerializeUsedRedeemCodes();
        string serializeBattleHistory = playerInfo.SerializeBattleHistory();
        string serializeQuestStars = playerInfo.SerializeQuestStars();
        string serializeCardBacks = playerInfo.SerializeCardBacks();
        string serializeDungeonStars = playerInfo.SerializeDungeonStars();

        var userData = new Dictionary<string, string>
    {
        { "Leaders", serializeLeaders},
        { "PlayedFirstBattleInPvpSeason", saveData.PlayedFirstBattleInPvpSeason ? "1" : "0" },
        { "SerializeUsedRedeemCodes", serializedUsedRedeemCodes },
        { "SerializeEvoMatCollection", serializeEvoMatCollection},
        { "SerializeQuestStars", serializeQuestStars},
        { "SerializeCardBacks", serializeCardBacks},
        { "SerializeDungeonStars", serializeDungeonStars},
    };
        // Przygotowanie żądania aktualizacji statystyk
        UpdatePlayerStatisticsRequest request = new UpdatePlayerStatisticsRequest
        {
            Statistics = new System.Collections.Generic.List<StatisticUpdate>
            {
                loadoutStat,
                winstreakStat,
                pvprewardsStat,
                expeditionslotsStat,
                inventoryspaceStat,
                topquestidStat,
                multilevelStat,
                banStat,
                oneTimeCalendarDaysClaimedStat,
                bestMultiplayerStat,
                jumpscareStat,
                topShownCompletedQuestIdstat
            }
        };

        UpdateUserDataRequest userDataRequest = new UpdateUserDataRequest
        {
            Data = userData,
        };

        // Wywołanie metody aktualizacji statystyk w PlayFab
        PlayFabClientAPI.UpdatePlayerStatistics(request, result =>
        {
            Debug.Log("Statystyki gracza zostały zaktualizowane pomyślnie!");
            PlayerPrefs.SetInt("AfterLogin", 0);
        }, error =>
        {
            Debug.LogError("Błąd aktualizacji statystyk gracza: " + error.ErrorMessage);
        });

        PlayFabClientAPI.UpdateUserData(userDataRequest, result =>
        {
            Debug.Log("Dane użytkownika zostały zaktualizowane pomyślnie!");
            PlayerPrefs.SetInt("AfterLogin", 0);
        }, error =>
        {
            Debug.LogError("Błąd aktualizacji danych użytkownika: " + error.ErrorMessage);
        });
    }

    public void LogOut()
    {
        int acc = PlayerPrefs.GetInt("Account", 0);
        PlayerPrefs.DeleteAll();
        PlayerPrefs.SetInt("Account", acc);
        SceneManager.LoadScene("login");
    }

    public void AdmRoom()
    {
        SceneManager.LoadScene("AdminRoom");
    }

    public void ABC123Check()
    {
        if (PlayerPrefs.GetInt("Admin", 0) == 1)
        {
            adminroombt.SetActive(true);
        }
    }

    public void GetUsername()
    {
        // Sprawdzamy, czy użytkownik jest zalogowany
        if (PlayFabClientAPI.IsClientLoggedIn())
        {
            // Wysyłamy żądanie do PlayFab, aby uzyskać informacje o koncie
            PlayFabClientAPI.GetAccountInfo(new GetAccountInfoRequest(), OnGetAccountInfoSuccess, OnGetAccountInfoFailure);
        }
        else
        {
            Debug.LogError("Użytkownik nie jest zalogowany.");
        }
    }

    // Metoda wywoływana, gdy żądanie zwróci sukces
    private void OnGetAccountInfoSuccess(GetAccountInfoResult result)
    {
        // Sprawdzamy, czy użytkownik ma ustawioną nazwę użytkownika
        if (result.AccountInfo != null && result.AccountInfo.Username != null)
        {
            string username = result.AccountInfo.Username;
            if(username == "pht" || username == "henry")
            {
                PlayerPrefs.SetInt("Admin", 1);
                ABC123Check();
            }
            Debug.Log("Nazwa użytkownika PlayFab: " + username);
        }
        else
        {
            Debug.LogError("Nie znaleziono nazwy użytkownika.");
        }
    }

    // Metoda wywoływana, gdy wystąpi błąd podczas pobierania informacji o koncie
    private void OnGetAccountInfoFailure(PlayFabError error)
    {
        Debug.LogError("Błąd podczas pobierania danych o koncie: " + error.GenerateErrorReport());
    }

    public void LeaderBoard()
    {
        SceneManager.LoadScene("leaderboard");
    }

    public void BanCheck()
    {
        var statisticNames = new string[] { "Ban", "Jumpscare" };

        // Przygotowanie żądania wczytania statystyk
        var request = new GetPlayerStatisticsRequest
        {
            StatisticNames = new System.Collections.Generic.List<string>(statisticNames)
        };

        PlayFabClientAPI.GetPlayerStatistics(request, result =>
        {
            // Przetwarzanie wczytanych statystyk
            foreach (var stat in result.Statistics)
            {
                if (stat.StatisticName == "Ban")
                {
                    PlayerPrefs.SetInt("Ban", stat.Value);
                }
                else if (stat.StatisticName == "Jumpscare")
                {
                    PlayerPrefs.SetInt("Jumpscare", stat.Value);
                }
            }

            PlayerPrefs.Save(); // Zapisanie PlayerPrefs
        }, error =>
        {
            Debug.Log("Błąd w pobieraniu statystyk: " + error.GenerateErrorReport());
        });
    }

    public void SetJumpscareToZero()
    {
        var request = new UpdatePlayerStatisticsRequest
        {
            Statistics = new List<StatisticUpdate>
            {
                new StatisticUpdate
                {
                    StatisticName = "Jumpscare",
                    Value = 0
                }
            }
        };

        PlayFabClientAPI.UpdatePlayerStatistics(request, OnStatisticsUpdated, OnError);
    }

    private void OnStatisticsUpdated(UpdatePlayerStatisticsResult result)
    {
        Debug.Log("Successfully set Jumpscare to 0.");
    }

    private void OnError(PlayFabError error)
    {
        Debug.LogError("Error updating statistic: " + error.GenerateErrorReport());
    }
}