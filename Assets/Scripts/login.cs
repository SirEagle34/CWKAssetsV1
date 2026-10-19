using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PlayFab;
using PlayFab.ClientModels;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class login : MonoBehaviour
{
    public UIInput nicknameInputField;
    public UIInput passwordInputField;
    public UILabel errorText;

    [Header("Login Buttons")]
    public GameObject loginbt;
    public GameObject register_oldbt;
    public GameObject registerbt;
    public GameObject skipbt;

    
    private void Start()
    {
        BindButton(loginbt, Login);
        BindButton(register_oldbt, RegisterAccount);
        BindButton(registerbt, SignInWithGoogle);
        BindButton(skipbt, SkipLogin);

        SetButtonLabel(registerbt, "Sign in with Google");
        SetButtonLabel(register_oldbt, "Register (Recommend)");
    }

    private void BindButton(
        GameObject target,
        UnityEngine.Events.UnityAction action)
    {
        if (target == null)
        {
            Debug.LogWarning("[Login] Button GameObject is not assigned.");
            return;
        }

        // Unity UI Button
        UnityEngine.UI.Button unityButton =
            target.GetComponent<UnityEngine.UI.Button>();

        if (unityButton != null)
        {
            unityButton.onClick.RemoveListener(action);
            unityButton.onClick.AddListener(action);
            unityButton.interactable = true;
            return;
        }

        // NGUI UIButton
        UIButton nguiButton = target.GetComponent<UIButton>();

        if (nguiButton != null)
        {
            UIEventListener listener =
                UIEventListener.Get(target);

            listener.onClick -= OnNGUIButtonClicked;
            listener.onClick += OnNGUIButtonClicked;

            return;
        }

        Debug.LogWarning(
            "[Login] No Unity UI Button or NGUI UIButton found on: "
            + target.name);
    }

    private void OnNGUIButtonClicked(GameObject clickedObject)
    {
        if (clickedObject == loginbt)
            Login();
        else if (clickedObject == register_oldbt)
            RegisterAccount();
        else if (clickedObject == registerbt)
            SignInWithGoogle();
        else if (clickedObject == skipbt)
            SkipLogin();
    }


    private void SetButtonLabel(GameObject target, string label)
    {
        if (target == null)
            return;

        TextMeshProUGUI text = target.GetComponentInChildren<TextMeshProUGUI>();

        if (text != null)
            text.text = label;
    }

    private void SetButtonsActive(bool active)
    {
        if (loginbt != null)
            loginbt.SetActive(active);

        if (register_oldbt != null)
            register_oldbt.SetActive(active);

        if (registerbt != null)
            registerbt.SetActive(active);

        if (skipbt != null)
            skipbt.SetActive(active);
    }

    private void SetError(string message)
    {
        if (errorText == null)
            return;

        errorText.color = Color.red;
        errorText.text = message;
    }

    public void SkipLogin()
    {
        PlayerPrefs.SetInt("SkipLogin", 1);
        PlayerPrefs.Save();

        SceneManager.LoadScene("BootVideoScene");
    }

    public void SignInWithGoogle()
    {
        if (CWKGoogleLink.Instance == null)
        {
            SetError("Google sign-in is unavailable.");
            return;
        }

        if (errorText != null)
        {
            errorText.color = Color.white;
            errorText.text = "Opening Google...";
        }

        CWKGoogleLink.Instance.OnStatus = delegate(string msg)
        {
            if (errorText != null)
            {
                errorText.color = Color.white;
                errorText.text = msg;
            }
        };

        CWKGoogleLink.Instance.BeginSignIn();
    }

    // ---------------------------------------------------------
    // OLD PLAYFAB REGISTRATION
    // ---------------------------------------------------------

    public void RegisterAccount()
    {
        string nickname = nicknameInputField != null
            ? nicknameInputField.text
            : string.Empty;

        string password = passwordInputField != null
            ? passwordInputField.text
            : string.Empty;

        if (string.IsNullOrEmpty(nickname) || nickname.Length < 3)
        {
            SetError("Nickname must consist of at least 3 letters!");
            return;
        }

        if (string.IsNullOrEmpty(password) || password.Length < 3)
        {
            SetError("Password must consist of at least 3 letters!");
            return;
        }

        if (PlayerPrefs.GetInt("Account", 0) == 1)
        {
            SetError("You already have an account on this device!");
            return;
        }

        string email = nickname + "@gmail.com";
        string displayName = nickname;

        SetButtonsActive(false);

        if (errorText != null)
        {
            errorText.color = Color.green;
            errorText.text = "Registering account...\nPlease wait...";
        }

        RegisterPlayFabAccount(nickname, email, password, displayName);
    }

    private void RegisterPlayFabAccount(
        string nickname,
        string email,
        string password,
        string displayName)
    {
        RegisterPlayFabUserRequest request = new RegisterPlayFabUserRequest
        {
            Username = nickname,
            Email = email,
            Password = password,
            DisplayName = displayName
        };

        PlayFabClientAPI.RegisterPlayFabUser(
            request,
            result =>
            {
                PlayerPrefs.SetInt("Account", 1);
                PlayerPrefs.SetString("Nickname", nickname);
                PlayerPrefs.SetString("Password", password);
                PlayerPrefs.SetString("Email", email);
                PlayerPrefs.SetInt("SkipLogin", 0);
                PlayerPrefs.Save();

                SceneManager.LoadScene("BootVideoScene");
            },
            error =>
            {
                SetError("Register error!: " + error.ErrorMessage);
                SetButtonsActive(true);
            });
    }

    // ---------------------------------------------------------
    // OLD PLAYFAB LOGIN
    // ---------------------------------------------------------

    public void Login()
    {
        string nickname = nicknameInputField != null
            ? nicknameInputField.text
            : string.Empty;

        string password = passwordInputField != null
            ? passwordInputField.text
            : string.Empty;

        if (string.IsNullOrEmpty(nickname) ||
            string.IsNullOrEmpty(password))
        {
            SetError("Enter nickname and password!");
            return;
        }

        string email = nickname + "@gmail.com";

        if (errorText != null)
        {
            errorText.color = Color.green;
            errorText.text = "Logging in...\nPlease wait...";
        }

        SetButtonsActive(false);
        LoginWithPlayFab(nickname, email, password);
    }

    private void LoginWithPlayFab(
        string nickname,
        string email,
        string password)
    {
        LoginWithPlayFabRequest request = new LoginWithPlayFabRequest
        {
            Username = nickname,
            Password = password
        };

        PlayFabClientAPI.LoginWithPlayFab(
            request,
            result =>
            {
                PlayerPrefs.SetString("Nickname", nickname);
                PlayerPrefs.SetString("Password", password);
                PlayerPrefs.SetString("Email", email);
                PlayerPrefs.SetInt("SkipLogin", 0);
                PlayerPrefs.Save();

                LoadAndAssignCustomPlayerPrefs();
            },
            error =>
            {
                SetError("Login failed!: " + error.ErrorMessage);
                SetButtonsActive(true);
            });
    }

    private void RecoverFromLoadFailure(string detail)
    {
        Debug.LogWarning("[Login] Post-auth load failed: " + detail);

        if (errorText != null)
        {
            errorText.color = Color.red;
            errorText.text =
                "Couldn't load your profile.\nPlease check your connection and try again.";
        }

        SetButtonsActive(true);
    }

    // ---------------------------------------------------------
    // IMPORTANT:
    // Keep the FULL original LoadAndAssignCustomPlayerPrefs()
    // implementation here.
    //
    // Do not replace it with a method that only loads
    // BootVideoScene. The original method loads PlayFab
    // statistics and user data, including inventory, leaders,
    // cards, loadouts, collections, expeditions and quest stars.
    // ---------------------------------------------------------

    public void LoadAndAssignCustomPlayerPrefs()
    {
        PlayerSaveData saveData = Singleton<PlayerInfoScript>.Instance.SaveData;
        PlayerInfoScript playerInfo = Singleton<PlayerInfoScript>.Instance;

        string[] statisticNames =
        {
            "Gems",
            "Coins",
            "RankXP",
            "Muryo",
            "ReadWriteParam",
            "SelectedLoadout",
            "MyHelperCreatureID",
            "PvpSpecialDomainNumber",
            "MultiplayerWinStreak",
            "ExpeditionSlots",
            "ADMIN",
            "InventorySpace",
            "TopCompletedQuestId",
            "Ban",
            "OneTimeCalendarDaysClaimed",
            "BestMultiplayerLevel",
            "MultiplayerLevel",
            "PvpRankRewardsGranted",
            "PointsInMultiplayerLevel",
            "TopShownCompletedQuestId"
        };

        GetPlayerStatisticsRequest request = new GetPlayerStatisticsRequest
        {
            StatisticNames = new List<string>(statisticNames)
        };

        PlayFabClientAPI.GetPlayerStatistics(
            request,
            result =>
            {
                foreach (var stat in result.Statistics)
                {
                    switch (stat.StatisticName)
                    {
                        case "Gems":
                            saveData.HardCurrency = stat.Value;
                            break;

                        case "Coins":
                            saveData.SoftCurrency = stat.Value;
                            break;

                        case "RankXP":
                            saveData.RankXP = stat.Value;
                            break;

                        case "Muryo":
                            saveData.Muryo = stat.Value;
                            break;

                        case "ReadWriteParam":
                            saveData.ReadWriteParam = stat.Value;
                            break;

                        case "SelectedLoadout":
                            saveData.SelectedLoadout = stat.Value;
                            break;

                        case "MyHelperCreatureID":
                            saveData.MyHelperCreatureID = stat.Value;
                            break;

                        case "PvpSpecialDomainNumber":
                            saveData.PvpSpecialDomainNumber = stat.Value;
                            break;

                        case "MultiplayerWinStreak":
                            saveData.MultiplayerWinStreak = stat.Value;
                            break;

                        case "ExpeditionSlots":
                            saveData.ExpeditionSlots = stat.Value;
                            break;

                        case "ADMIN":
                            PlayerPrefs.SetInt("Admin", stat.Value);
                            break;

                        case "InventorySpace":
                            saveData.InventorySpace = stat.Value;
                            break;

                        case "TopCompletedQuestId":
                            saveData.TopCompletedQuestId = stat.Value;
                            break;

                        case "Ban":
                            PlayerPrefs.SetInt("Ban", stat.Value);
                            break;

                        case "OneTimeCalendarDaysClaimed":
                            saveData.OneTimeCalendarDaysClaimed = stat.Value;
                            break;

                        case "BestMultiplayerLevel":
                            saveData.BestMultiplayerLevel = stat.Value;
                            break;

                        case "MultiplayerLevel":
                            saveData.MultiplayerLevel = stat.Value;
                            break;

                        case "PvpRankRewardsGranted":
                            saveData.PvpRankRewardsGranted = stat.Value;
                            break;

                        case "PointsInMultiplayerLevel":
                            saveData.PointsInMultiplayerLevel = stat.Value;
                            break;

                        case "TopShownCompletedQuestId":
                            saveData.TopShownCompletedQuestId = stat.Value;
                            break;
                    }
                }

                PlayFabClientAPI.GetUserData(
                    new GetUserDataRequest(),
                    userDataResult =>
                    {
                        var data = userDataResult.Data;

                        if (data != null && data.ContainsKey("Nickname"))
                        {
                            string nickName = data["Nickname"].Value;
                            saveData.MultiplayerPlayerName = nickName;
                        }

                        if (data != null && data.ContainsKey("HasAuthenticated"))
                        {
                            bool.TryParse(
                                data["HasAuthenticated"].Value,
                                out bool hasAuthenticated);

                            saveData.HasAuthenticated = hasAuthenticated;
                        }

                        if (data != null && data.ContainsKey("SerializedUnlocks"))
                        {
                            try
                            {
                                JArray array = JArray.Parse(
                                    data["SerializedUnlocks"].Value);

                                playerInfo.DeserializeUnlocks(
                                    array.ToObject<object[]>());
                            }
                            catch (JsonException ex)
                            {
                                Debug.LogError(
                                    "SerializedUnlocks deserialization error: " +
                                    ex.Message);
                            }
                        }

                        if (data != null && data.ContainsKey("TutorialsDone"))
                        {
                            try
                            {
                                JArray array = JArray.Parse(
                                    data["TutorialsDone"].Value);

                                TutorialDataManager.Instance.Deserialize(
                                    array.ToObject<object[]>());
                            }
                            catch (JsonException ex)
                            {
                                Debug.LogError(
                                    "TutorialsDone deserialization error: " +
                                    ex.Message);
                            }
                        }

                        if (data != null &&
                            data.ContainsKey("PlayedFirstBattleInPvpSeason"))
                        {
                            saveData.PlayedFirstBattleInPvpSeason =
                                data["PlayedFirstBattleInPvpSeason"].Value == "1";
                        }

                        if (data != null && data.ContainsKey("SerializedInventory"))
                        {
                            try
                            {
                                JArray array = JArray.Parse(
                                    data["SerializedInventory"].Value);

                                List<Dictionary<string, object>> list =
                                    array.ToObject<List<Dictionary<string, object>>>();

                                if (list != null && list.Count > 0)
                                    playerInfo.DeserializeInventory(list.ToArray());
                            }
                            catch (JsonException ex)
                            {
                                Debug.LogError(
                                    "SerializedInventory deserialization error: " +
                                    ex.Message);
                            }
                        }

                        if (data != null && data.ContainsKey("Leaders"))
                        {
                            try
                            {
                                JArray array = JArray.Parse(data["Leaders"].Value);

                                List<Dictionary<string, object>> list =
                                    array.ToObject<List<Dictionary<string, object>>>();

                                if (list != null && list.Count > 0)
                                    playerInfo.DeserializeLeaders(list.ToArray());
                            }
                            catch (JsonException ex)
                            {
                                Debug.LogError(
                                    "Leaders deserialization error: " + ex.Message);
                            }
                        }

                        if (data != null && data.ContainsKey("EvoMatCollection"))
                        {
                            try
                            {
                                JArray array = JArray.Parse(
                                    data["EvoMatCollection"].Value);

                                List<Dictionary<string, object>> list =
                                    array.ToObject<List<Dictionary<string, object>>>();

                                if (list != null && list.Count > 0)
                                    playerInfo.DeserializeEvoMatCollection(list.ToArray());
                            }
                            catch (JsonException ex)
                            {
                                Debug.LogError(
                                    "EvoMatCollection deserialization error: " +
                                    ex.Message);
                            }
                        }

                        if (data != null && data.ContainsKey("SerializeCardBacks"))
                        {
                            try
                            {
                                string serialized = data["SerializeCardBacks"].Value;

                                if (serialized.StartsWith("\"") &&
                                    serialized.EndsWith("\""))
                                {
                                    serialized =
                                        JsonConvert.DeserializeObject<string>(serialized);
                                }

                                JToken parsed = JToken.Parse(serialized);

                                if (parsed is JArray array)
                                {
                                    playerInfo.DeserializeCardBacks(
                                        array.ToObject<string[]>());
                                }
                            }
                            catch (JsonException ex)
                            {
                                Debug.LogError(
                                    "SerializeCardBacks deserialization error: " +
                                    ex.Message);
                            }
                        }

                        if (data != null &&
                            data.ContainsKey("SerializeUsedRedeemCodes"))
                        {
                            try
                            {
                                JArray array = JArray.Parse(
                                    data["SerializeUsedRedeemCodes"].Value);

                                List<Dictionary<string, object>> list =
                                    array.ToObject<List<Dictionary<string, object>>>();

                                if (list != null && list.Count > 0)
                                    playerInfo.DeserializeUsedRedeemCodes(list.ToArray());
                            }
                            catch (JsonException ex)
                            {
                                Debug.LogError(
                                    "SerializeUsedRedeemCodes deserialization error: " +
                                    ex.Message);
                            }
                        }

                        if (data != null &&
                            data.ContainsKey("SerializedCreatureCollection"))
                        {
                            try
                            {
                                JArray array = JArray.Parse(
                                    data["SerializedCreatureCollection"].Value);

                                List<Dictionary<string, object>> list =
                                    array.ToObject<List<Dictionary<string, object>>>();

                                if (list != null && list.Count > 0)
                                    playerInfo.DeserializeCreatureCollection(list.ToArray());
                            }
                            catch (JsonException ex)
                            {
                                Debug.LogError(
                                    "SerializedCreatureCollection deserialization error: " +
                                    ex.Message);
                            }
                        }

                        if (data != null &&
                            data.ContainsKey("SerializeCardCollection"))
                        {
                            try
                            {
                                JArray array = JArray.Parse(
                                    data["SerializeCardCollection"].Value);

                                List<Dictionary<string, object>> list =
                                    array.ToObject<List<Dictionary<string, object>>>();

                                if (list != null && list.Count > 0)
                                    playerInfo.DeserializeCardCollection(list.ToArray());
                            }
                            catch (JsonException ex)
                            {
                                Debug.LogError(
                                    "SerializeCardCollection deserialization error: " +
                                    ex.Message);
                            }
                        }

                        if (data != null && data.ContainsKey("SerializeCreatureSorts"))
                        {
                            try
                            {
                                JArray array = JArray.Parse(
                                    data["SerializeCreatureSorts"].Value);

                                List<Dictionary<string, object>> list =
                                    array.ToObject<List<Dictionary<string, object>>>();

                                if (list != null && list.Count > 0)
                                    playerInfo.DeserializeCreatureSorts(list.ToArray());
                            }
                            catch (JsonException ex)
                            {
                                Debug.LogError(
                                    "SerializeCreatureSorts deserialization error: " +
                                    ex.Message);
                            }
                        }

                        if (data != null && data.ContainsKey("Expeditions"))
                        {
                            try
                            {
                                JArray array = JArray.Parse(data["Expeditions"].Value);

                                List<Dictionary<string, object>> list =
                                    array.ToObject<List<Dictionary<string, object>>>();

                                if (list != null && list.Count > 0)
                                {
                                    DetachedSingleton<ExpeditionManager>
                                        .Instance.Deserialize(list.ToArray());
                                }
                            }
                            catch (JsonException ex)
                            {
                                Debug.LogError(
                                    "Expeditions deserialization error: " + ex.Message);
                            }
                        }

                        if (data != null && data.ContainsKey("SerializedLoadouts"))
                        {
                            try
                            {
                                JArray array = JArray.Parse(
                                    data["SerializedLoadouts"].Value);

                                List<Dictionary<string, object>> list =
                                    array.ToObject<List<Dictionary<string, object>>>();

                                if (list != null && list.Count > 0)
                                    playerInfo.DeserializeLoadouts(list.ToArray());
                            }
                            catch (JsonException ex)
                            {
                                Debug.LogError(
                                    "SerializedLoadouts deserialization error: " +
                                    ex.Message);
                            }
                        }

                        if (data != null && data.ContainsKey("SerializeQuestStars"))
                        {
                            try
                            {
                                int[] stars = JsonConvert.DeserializeObject<int[]>(
                                    data["SerializeQuestStars"].Value);

                                if (stars != null && stars.Length > 0)
                                    playerInfo.DeserializeQuestStars(stars);
                            }
                            catch (JsonException ex)
                            {
                                Debug.LogError(
                                    "SerializeQuestStars deserialization error: " +
                                    ex.Message);
                            }
                        }

                        PlayerPrefs.SetInt("AfterLogin", 1);
                        PlayerPrefs.Save();

                        SceneManager.LoadScene("BootVideoScene");
                    },
                    error =>
                    {
                        Debug.LogError(
                            "Failed to load PlayFab user data: " +
                            error.GenerateErrorReport());

                        RecoverFromLoadFailure(error.ErrorMessage);
                    });
            },
            error =>
            {
                Debug.LogError(
                    "Failed to load PlayFab statistics: " +
                    error.GenerateErrorReport());

                RecoverFromLoadFailure(error.ErrorMessage);
            });
    }
}