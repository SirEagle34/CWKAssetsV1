
using System;
using System.Collections.Generic;
using System.Net;
using Multiplayer;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

public class TownSettingsController : Singleton<TownSettingsController>
{
    public enum OptionTypeKPI
    {
        Music,
        SFX,
        CameraTilt,
        Language
    }

    public UITweenController ShowTween;
    public UITweenController HideTween;
    public UITweenController ShowMenuTween;
    public UITweenController HideMenuTween;

    public GameObject Button_Close;

    public UILabel PlayerName;
    public UILabel PlayerID;

    public GameObject HideFirstPopup;

    [Header("Multiplayer Name")]
    public GameObject ChangeNameButton;

    [Header("Multiplayer Name Popup")]
    public GameObject MultiplayerNameChangePopup;
    public UIInput PopupNewNameInput;
    public UIInput PopupConfirmNameInput;

    [Header("Name Change Cost UI")]
    public GameObject NameChangeCostFreeObject; // Object_0: UILabel FREE
    public GameObject NameChangeCostPaidObject; // Object: UISprite + UILabel 4500

    private string lastCostInput = null;

    public GameObject PopupAcceptButton;
    public GameObject PopupCancelButton;
    public GameObject Cancelbuttonclose;


    [Header("Name Change Request")]
    public string NameChangeAge = "18";

    private bool isChangingName;
    private const int NAME_CHANGE_COST = 4500;

    public UILabel ButtonConnectLabel;
    public GameObject PrivacyPolicyButton;
    public UIGrid ButtonsParent;
    public GameObject FaqButton;
    public GameObject DragonStonesButton;
    public GameObject SctlButton;
    public GameObject SettlementsButton;
    public GameObject CopyUserIdButton;
    public UITexture LeaderTex;
    public UILabel versionLbl;
    public UIPopupList langList;
    public PopupListLocalizationKeys langListPopupKeys;
    public UILabel langLbl;

    private string mLeaderName;
    private bool mAudioInitialized;
    private float mMusicVol;
    private float mSoundVol;
    private bool mTiltCam;

    public static List<string> langeSettingList = new List<string>
    {
        "ENGLISH",
        "SPANISH",
        "FRENCH",
        "ITALIAN",
        "JAPANESE",
        "KOREAN",
        "PORTUGUESE",
        "RUSSIAN",
        "TURKISH",
        "ARABIC"
    };

    public UIToggle AccountViewToggle;
    public UIToggle OptionsToggle;
    public UIToggle HelpToggle;
    public GameObject[] TogglePanels;
    private UIToggle mCurrentToggle;

    public GameObject MusicButtonOn;
    public GameObject MusicButtonOff;
    public GameObject SoundButtonOn;
    public GameObject SoundButtonOff;
    public GameObject TiltCamButtonOn;
    public GameObject TiltCamButtonOff;

    private bool isTabInitialized;

    public bool Privacy = true;
    public bool Custom;

    private string LinkPrivacy =
        "http://www.cartoonnetwork.com/legal/priv_tou.html#textBox_priv_a";

    private string LinkTerms =
        "http://www.cartoonnetwork.com/legal/priv_tou.html#textBox_priv_a";

    public string LinkCustom;

    public static ResponseFlag CodeRedeemFlag { get; set; }

    private void Start()
    {
        showVersionNumber();

        if (langListPopupKeys != null)
        {
            langListPopupKeys.LocalizedKeys = new List<string>();

            for (int i = 0; i < langeSettingList.Count; i++)
            {
                langListPopupKeys.LocalizedKeys.Add(
                    "!!" + langeSettingList[i]);
            }
        }

        UpdateLangListPosition();

        // Copy User ID button
        if (CopyUserIdButton != null)
        {
            Button copyButton = CopyUserIdButton.GetComponent<Button>();

            if (copyButton != null)
            {
                copyButton.onClick.RemoveListener(CopyUserIdToClipboard);
                copyButton.onClick.AddListener(CopyUserIdToClipboard);
            }
        }

        // Popup starts closed.
        if (MultiplayerNameChangePopup != null)
        {
            MultiplayerNameChangePopup.SetActive(false);
        }

        // Accept button
        if (PopupAcceptButton != null)
        {
            UIButton button = PopupAcceptButton.GetComponent<UIButton>();

            if (button != null)
            {
                EventDelegate.Remove(button.onClick, OnAcceptMultiplayerNameChange);
                EventDelegate.Add(button.onClick, OnAcceptMultiplayerNameChange);
            }
        }

        // Cancel button
        if (PopupCancelButton != null)
        {
            Button cancelButton = PopupCancelButton.GetComponent<Button>();

            if (cancelButton != null)
            {
                cancelButton.onClick.RemoveListener(
                    OnCancelMultiplayerNameChange);

                cancelButton.onClick.AddListener(
                    OnCancelMultiplayerNameChange);
            }
        }

        // Change name button
        if (ChangeNameButton != null)
        {
            Button changeButton = ChangeNameButton.GetComponent<Button>();

            if (changeButton != null)
            {
                changeButton.onClick.RemoveListener(
                    OnClickChangeMultiplayerName);

                changeButton.onClick.AddListener(
                    OnClickChangeMultiplayerName);
            }
        }
    }

    public void CopyUserIdToClipboard()
    {
        if (PlayerID != null)
        {
            GUIUtility.systemCopyBuffer = PlayerID.text;
            Debug.Log("User ID Copied to Clipboard: " + PlayerID.text);
        }
    }

    private void OnApplicationPause(bool pauseStatus)
    {
    }

    public void Show()
    {
        if (ShowTween != null)
            ShowTween.Play();

        if (FaqButton != null)
            FaqButton.SetActive(true);

        if (DragonStonesButton != null)
            DragonStonesButton.SetActive(false);

        if (SctlButton != null)
            SctlButton.SetActive(false);

        if (SettlementsButton != null)
            SettlementsButton.SetActive(false);

        if (CopyUserIdButton != null)
            CopyUserIdButton.SetActive(true);

        if (ButtonsParent != null)
            ButtonsParent.Reposition();

        Singleton<HelpScreenController>.Instance.Populate();

        mTiltCam = Singleton<PlayerInfoScript>.Instance.SaveData.TownTiltCam;
        mMusicVol = Singleton<SLOTAudioManager>.Instance.musicVolume;
        mSoundVol = Singleton<SLOTAudioManager>.Instance.soundVolume;

        if (mTiltCam)
            ToggleTiltCamOn();
        else
            ToggleTiltCamOff();

        if (mMusicVol > 0f)
            ToggleMusicOn();
        else
            ToggleMusicOff();

        if (mSoundVol > 0f)
            ToggleSoundOn();
        else
            ToggleSoundOff();

        UpdatePanelState();
    }

    public void OnClickClose()
    {
        if (mTiltCam != Singleton<PlayerInfoScript>.Instance.SaveData.TownTiltCam ||
            mMusicVol != Singleton<SLOTAudioManager>.Instance.musicVolume ||
            mSoundVol != Singleton<SLOTAudioManager>.Instance.soundVolume)
        {
            Singleton<PlayerInfoScript>.Instance.Save();
        }

        if (HideTween != null)
            HideTween.Play();
    }

    public void OnClickMenuClose()
    {
        if (HideMenuTween != null)
            HideMenuTween.Play();
    }

    private void Update()
    {
        if (!mAudioInitialized &&
            Singleton<SLOTAudioManager>.Instance.IsInitialized())
        {
            float musicVolume =
                Singleton<SLOTAudioManager>.Instance.musicVolume;

            float soundVolume =
                Singleton<SLOTAudioManager>.Instance.soundVolume;

            if (musicVolume > 0f)
                ToggleMusicOn();
            else
                ToggleMusicOff();

            if (soundVolume > 0f)
                ToggleSoundOn();
            else
                ToggleSoundOff();

            mAudioInitialized = true;
        }

        if (Singleton<PlayerInfoScript>.Instance.IsInitialized)
        {
            Loadout currentLoadout =
                Singleton<PlayerInfoScript>.Instance.GetCurrentLoadout();

            currentLoadout.Leader.Form.ParseKeywords();

            if (string.IsNullOrEmpty(mLeaderName) ||
                mLeaderName != currentLoadout.Leader.SelectedSkin.Name)
            {
                Singleton<SLOTResourceManager>.Instance.QueueUITextureLoad(
                    currentLoadout.Leader.SelectedSkin.PortraitTexture,
                    "FTUEBundle",
                    "UI/UI/LoadingPlaceholder",
                    LeaderTex);

                mLeaderName = currentLoadout.Leader.Form.Name;
            }
        }

        // Update the price while the player types.
        if (MultiplayerNameChangePopup != null &&
            MultiplayerNameChangePopup.activeInHierarchy &&
            PopupNewNameInput != null)
        {
            string inputName = PopupNewNameInput.text ?? string.Empty;

            if (lastCostInput != inputName)
            {
                lastCostInput = inputName;
                UpdateNameChangeCostLabel();
            }
        }
    }

    private void UpdatePanelState()
    {
        PlayerInfoScript instance =
            Singleton<PlayerInfoScript>.Instance;

        if (instance == null)
            return;

        if (PlayerID != null)
            PlayerID.text = instance.GetFormattedPlayerCode();

        if (PlayerName != null)
            PlayerName.text = instance.GetPlayerName();

        if (ButtonConnectLabel != null)
        {
            ButtonConnectLabel.text = KFFLocalization.Get(
                (!instance.IsFacebookLogin())
                    ? "!!FB_CONNECT_BUTTON"
                    : "!!FB_LOGOUT");
        }
    }

    public void OnClickCustomize()
    {
        Singleton<PlayerCustomizationController>.Instance.ShowDefault();
    }

    public void OnClickUnblockPlayers()
    {
        Singleton<SimplePopupController>.Instance.ShowPrompt(
            string.Empty,
            KFFLocalization.Get("!!CLEAR_IGNORE_LIST_PROMPT"),
            ConfirmUnblock,
            null);
    }

    private void ConfirmUnblock()
    {
        Singleton<PlayerInfoScript>.Instance.SaveData.IgnoredPlayers.Clear();
        Singleton<PlayerInfoScript>.Instance.Save();
    }

    public void SwitchTab()
    {
        if (!isTabInitialized)
        {
            isTabInitialized = true;

            mCurrentToggle =
                UIToggle.GetActiveToggle(AccountViewToggle.group);

            for (int i = 0; i < TogglePanels.Length; i++)
            {
                TogglePanels[i].SetActive(
                    i == mCurrentToggle.toggleId);
            }
        }

        if (ShowMenuTween != null)
            ShowMenuTween.Play();
    }

    public void OnSoundVolumeChange()
    {
        Singleton<SLOTAudioManager>.Instance.SetSoundVolumeMasterAudio();
    }

    public void OnMusicVolumeChange()
    {
        Singleton<SLOTAudioManager>.Instance.SetMusicVolumeMasterAudio();
    }

    public void ToggleSoundOn()
    {
        Singleton<SLOTAudioManager>.Instance.SetSoundVolumeMasterAudio(1f);

        if (SoundButtonOff != null)
            SoundButtonOff.SetActive(true);

        if (SoundButtonOn != null)
            SoundButtonOn.SetActive(false);
    }

    public void ToggleSoundOff()
    {
        Singleton<SLOTAudioManager>.Instance.SetSoundVolumeMasterAudio(0f);

        if (SoundButtonOff != null)
            SoundButtonOff.SetActive(false);

        if (SoundButtonOn != null)
            SoundButtonOn.SetActive(true);
    }

    public void ToggleMusicOn()
    {
        Singleton<SLOTAudioManager>.Instance.SetMusicVolumeMasterAudio(1f);

        if (MusicButtonOff != null)
            MusicButtonOff.SetActive(true);

        if (MusicButtonOn != null)
            MusicButtonOn.SetActive(false);
    }

    public void ToggleMusicOff()
    {
        Singleton<SLOTAudioManager>.Instance.SetMusicVolumeMasterAudio(0f);

        if (MusicButtonOff != null)
            MusicButtonOff.SetActive(false);

        if (MusicButtonOn != null)
            MusicButtonOn.SetActive(true);
    }

    public void ToggleTiltCamOn()
    {
        Singleton<MouseOrbitCamera>.Instance.EnableTiltCam(true);

        if (TiltCamButtonOn != null)
            TiltCamButtonOn.SetActive(false);

        if (TiltCamButtonOff != null)
            TiltCamButtonOff.SetActive(true);

        PlayerPrefs.SetInt("EnableTiltCam", 1);
    }

    public void ToggleTiltCamOff()
    {
        Singleton<MouseOrbitCamera>.Instance.EnableTiltCam(false);

        if (TiltCamButtonOff != null)
            TiltCamButtonOff.SetActive(false);

        if (TiltCamButtonOn != null)
            TiltCamButtonOn.SetActive(true);

        PlayerPrefs.SetInt("EnableTiltCam", 0);
    }

    private string GetLinkPrivacy()
    {
        return LinkPrivacy;
    }

    private string GetLinkTerms()
    {
        return LinkTerms;
    }

    public void OpenUrl()
    {
        if (testInternetConnection())
        {
            if (Privacy)
            {
                string text = KFFLocalization.Get("!!PRIVACY_LINK");

                if (string.IsNullOrEmpty(text))
                    text = LinkPrivacy;

                Application.OpenURL(text);
            }
            else if (Custom)
            {
                Application.OpenURL(LinkCustom);
            }
        }
        else
        {
            Singleton<SimplePopupController>.Instance.ShowMessage(
                string.Empty,
                KFFLocalization.Get("!!CONNECTIONFAILED"));
        }
    }

    public void OpenPrivacyPolicy()
    {
        if (testInternetConnection())
        {
            string text = KFFLocalization.Get("!!PRIVACY_LINK");

            if (string.IsNullOrEmpty(text))
                text = GetLinkPrivacy();

            Application.OpenURL(text);
        }
        else
        {
            Singleton<SimplePopupController>.Instance.ShowMessage(
                string.Empty,
                KFFLocalization.Get("!!CONNECTIONFAILED"));
        }
    }

    public void OpenTermsOfUse()
    {
        if (testInternetConnection())
        {
            string text = KFFLocalization.Get("!!TERMSOFUSE_LINK");

            if (string.IsNullOrEmpty(text))
                text = GetLinkTerms();

            Application.OpenURL(text);
        }
        else
        {
            Singleton<SimplePopupController>.Instance.ShowMessage(
                string.Empty,
                KFFLocalization.Get("!!CONNECTIONFAILED"));
        }
    }

    public static bool testInternetConnection()
    {
        if (Application.internetReachability !=
                NetworkReachability.ReachableViaCarrierDataNetwork &&
            Application.internetReachability !=
                NetworkReachability.ReachableViaLocalAreaNetwork)
        {
            return false;
        }

        return true;
    }

    private void showInternetAlert()
    {
    }

    public void OnClickDragonStonesButton()
    {
        PlayerSaveData saveData =
            Singleton<PlayerInfoScript>.Instance.SaveData;

        string body = string.Format(
            KFFLocalization.Get("!!DRAGON_STONES_INFO_POPUP"),
            saveData.HardCurrency.ToString(),
            saveData.PaidHardCurrency.ToString(),
            saveData.FreeHardCurrency.ToString());

        Singleton<SimplePopupController>.Instance.ShowMessage(
            string.Empty,
            body);
    }

    public void OnClickSCTLButton()
    {
        if (testInternetConnection())
        {
            Application.OpenURL(
                "http://www.cartoonnetwork.com/legal/priv_tou.html#textBox_priv_a");
        }
        else
        {
            Singleton<SimplePopupController>.Instance.ShowMessage(
                string.Empty,
                KFFLocalization.Get("!!CONNECTIONFAILED"));
        }
    }

    public void OnClickSettlementsButton()
    {
        if (testInternetConnection())
        {
            Application.OpenURL(
                "http://www.cartoonnetwork.com/legal/priv_tou.html#textBox_priv_a");
        }
        else
        {
            Singleton<SimplePopupController>.Instance.ShowMessage(
                string.Empty,
                KFFLocalization.Get("!!CONNECTIONFAILED"));
        }
    }

    public void showVersionNumber()
    {
        if (versionLbl != null)
        {
            versionLbl.text =
                SQSettings.SERVER_PREFIX + " v." + Application.version;
        }
    }

    public void changeLangSetting()
    {
        try
        {
            string value =
                langeSettingList[getLangSettingListIndex()];

            SystemLanguage systemLanguage =
                (SystemLanguage)Enum.Parse(
                    typeof(SystemLanguage),
                    value,
                    true);

            LanguageCode languageCode =
                Language.LanguageNameToCode(systemLanguage);

            if (languageCode != Language.CurrentLanguage())
            {
                Singleton<PlayerInfoScript>.Instance.SaveData.selectedLang =
                    languageCode;

                PlayerPrefs.SetString(
                    "M2H_lastLanguage",
                    languageCode.ToString());

                Language.SwitchLanguage(languageCode);
                langList.Close();
                SessionManager.Instance.theSession.ReloadGame();
            }
        }
        catch (Exception exception)
        {
            Debug.LogWarning("[SETTINGS] Language change failed: " + exception);
        }
    }

    private int getLangSettingListIndex()
    {
        if (langList == null)
            return 0;

        for (int i = 0; i < langList.items.Count; i++)
        {
            if (langList.items[i].Equals(langList.value))
                return i;
        }

        return 0;
    }

    public void UpdateLangListPosition()
    {
        if (langList == null || langLbl == null)
            return;

        string text =
            Language.LanguageCodeToName(Language.CurrentLanguage()).ToString();

        for (int i = 0; i < langeSettingList.Count; i++)
        {
            if (text.ToUpper().Equals(langeSettingList[i]) &&
                i < langList.items.Count)
            {
                langList.value = langList.items[i];

                langLbl.text =
                    KFFLocalization.Get("!!" + langeSettingList[i]);

                langList.UpdatetextLabelToSelection(langLbl);
                break;
            }
        }
    }

    private void SetNameChangeStatus(string message)
    {
        if (string.IsNullOrEmpty(message))
            return;

        if (message.IndexOf("failed", StringComparison.OrdinalIgnoreCase) >= 0 ||
            message.IndexOf("error", StringComparison.OrdinalIgnoreCase) >= 0 ||
            message.IndexOf("not enough", StringComparison.OrdinalIgnoreCase) >= 0 ||
            message.IndexOf("insufficient", StringComparison.OrdinalIgnoreCase) >= 0 ||
            message.IndexOf("unavailable", StringComparison.OrdinalIgnoreCase) >= 0 ||
            message.IndexOf("not configured", StringComparison.OrdinalIgnoreCase) >= 0 ||
            message.IndexOf("mismatch", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            Debug.LogError("[NAME CHANGE] " + message);
        }
        else
        {
            Debug.Log("[NAME CHANGE] " + message);
        }
    }
    
    
    public void OnClickCancelNameChange()
    {
        if (isChangingName)
            return;

        if (MultiplayerNameChangePopup != null)
            MultiplayerNameChangePopup.SetActive(false);

        if (Cancelbuttonclose != null)
            Cancelbuttonclose.SetActive(false);

        if (Button_Close != null)
            Button_Close.SetActive(true);

        if (HideFirstPopup != null)
            HideFirstPopup.SetActive(true);

        if (HideTween != null)
            HideTween.Play();
    }



    public void OnClickChangeMultiplayerName()
    {
        if (isChangingName)
            return;

        if (MultiplayerNameChangePopup == null ||
            PopupNewNameInput == null ||
            PopupConfirmNameInput == null)
        {
            SetNameChangeStatus(
                "Name change popup or input fields are not configured.");
            return;
        }

        if (HideFirstPopup != null)
            HideFirstPopup.SetActive(false);

        if (Button_Close != null)
            Button_Close.SetActive(false);

        PopupNewNameInput.text = string.Empty;
        PopupConfirmNameInput.text = string.Empty;

        lastCostInput = null;

        UpdateNameChangeCostLabel();

        MultiplayerNameChangePopup.SetActive(true);
    }


    public void OnCancelMultiplayerNameChange()
    {
        OnClickCancelNameChange();
    }


    
    private void ConfirmNameChange()
    {
        if (isChangingName)
            return;

        SimplePopupController popup =
            Singleton<SimplePopupController>.Instance;

        if (popup == null)
        {
            SetNameChangeStatus("Confirmation popup is unavailable.");
            return;
        }

        // Satın alma popup'ı kullanmıyoruz.
        // Sunucu ilk değişikliği ücretsiz yapabilir; ücreti sunucu belirler.
        popup.ShowPrompt(
            "Confirm Name Change",
            "Do you want to change your multiplayer name? " +
            "Depending on your account history, this may cost 4500 free Gems.",
            OnConfirmNameChangePurchase,
            OnCancelNameChangePurchase
        );
    }

    private void OnCancelNameChangePurchase()
    {
        // Kullanıcı vazgeçti. İsim değiştirme penceresi açık kalsın.
    }

    private void OnConfirmNameChangePurchase()
    {
        if (isChangingName)
            return;

        if (PopupNewNameInput == null)
        {
            SetNameChangeStatus("Name input field is not configured.");
            return;
        }

        if (!testInternetConnection())
        {
            SetNameChangeStatus(
                KFFLocalization.Get("!!CONNECTIONFAILED"));
            return;
        }

        string newName = (PopupNewNameInput.text ?? string.Empty).Trim();

        // İstek sürerken isim penceresini kapatma.
        // Hata olursa kullanıcı yeniden deneyebilsin.
        StartCoroutine(ChangeMultiplayerNameRequest(newName));
    }


    private void UpdateNameChangeCostLabel()
    {
        PlayerInfoScript playerInfo =
            Singleton<PlayerInfoScript>.Instance;

        string currentName = string.Empty;

        if (playerInfo != null && playerInfo.SaveData != null)
        {
            currentName = Convert.ToString(
                playerInfo.SaveData.MultiplayerPlayerName) ?? string.Empty;
        }

        currentName = currentName.Trim();

        string newName = PopupNewNameInput != null
            ? (PopupNewNameInput.text ?? string.Empty).Trim()
            : string.Empty;

        // This is only a UI estimate. The server decides the real cost.
        // The client does not know the server's persistent name_change_count.
        bool isFree =
            string.IsNullOrEmpty(currentName) ||
            string.IsNullOrEmpty(newName) ||
            string.Equals(currentName, newName, StringComparison.Ordinal);

        if (NameChangeCostFreeObject != null)
        {
            NameChangeCostFreeObject.SetActive(isFree);

            if (isFree)
            {
                UILabel freeLabel =
                    NameChangeCostFreeObject.GetComponentInChildren<UILabel>(true);

                if (freeLabel != null)
                    freeLabel.text = "FREE";
                else
                    Debug.LogError(
                        "[NAME CHANGE] FREE UILabel not found under Object_0.");
            }
        }

        if (NameChangeCostPaidObject != null)
        {
            NameChangeCostPaidObject.SetActive(!isFree);

            if (!isFree)
            {
                UILabel paidLabel =
                    NameChangeCostPaidObject.GetComponentInChildren<UILabel>(true);

                if (paidLabel != null)
                    paidLabel.text = NAME_CHANGE_COST.ToString();
                else
                    Debug.LogError(
                        "[NAME CHANGE] 4500 UILabel not found under Object.");
            }
        }
    }

    
    public void OnAcceptMultiplayerNameChange()
    {
        if (isChangingName)
            return;

        if (PopupNewNameInput == null ||
            PopupConfirmNameInput == null)
        {
            SetNameChangeStatus("Name input fields are not configured.");
            return;
        }

        string newName =
            (PopupNewNameInput.text ?? string.Empty).Trim();

        string confirmName =
            (PopupConfirmNameInput.text ?? string.Empty).Trim();

        if (newName.Length > 128)
        {
            SetNameChangeStatus("Name cannot exceed 128 characters.");
            return;
        }

        if (!string.Equals(newName, confirmName, StringComparison.Ordinal))
        {
            SetNameChangeStatus(
                "Names do not match. Please confirm your name.");
            return;
        }

        PlayerInfoScript playerInfo =
            Singleton<PlayerInfoScript>.Instance;

        if (playerInfo == null || playerInfo.SaveData == null)
        {
            SetNameChangeStatus("Player data is not ready.");
            return;
        }

        string currentName =
            Convert.ToString(
                playerInfo.SaveData.MultiplayerPlayerName) ?? string.Empty;

        currentName = currentName.Trim();

        if (string.Equals(newName, currentName, StringComparison.Ordinal))
        {
            SetNameChangeStatus("The name has not changed.");
            return;
        }

        if (!testInternetConnection())
        {
            SetNameChangeStatus(
                KFFLocalization.Get("!!CONNECTIONFAILED"));
            return;
        }

        // Bundan sonrası SimplePopup onayıyla devam eder.
        ConfirmNameChange();
    }



    private System.Collections.IEnumerator ChangeMultiplayerNameRequest(
        string newName)
    {
        isChangingName = true;
        SetChangeNameButtonInteractable(false);

        SetNameChangeStatus("Updating name...");

        PlayerInfoScript playerInfo =
            Singleton<PlayerInfoScript>.Instance;

        if (playerInfo == null || playerInfo.SaveData == null)
        {
            SetNameChangeStatus("Player data is not ready.");
            FinishNameChangeRequest();
            yield break;
        }

        string playerId = playerInfo.GetPlayerCode();

        if (string.IsNullOrEmpty(playerId))
        {
            SetNameChangeStatus("Player ID is unavailable.");
            FinishNameChangeRequest();
            yield break;
        }

        string baseUrl = SQSettings.SERVER_URL;

        if (string.IsNullOrEmpty(baseUrl))
        {
            SetNameChangeStatus("Server URL is not configured.");
            FinishNameChangeRequest();
            yield break;
        }

        string endpoint =
            baseUrl.TrimEnd('/') + "/persist/multiplayer_name_change/";

        WWWForm form = new WWWForm();
        form.AddField("player_id", playerId);
        form.AddField("name", newName);

        using (UnityWebRequest request =
            UnityWebRequest.Post(endpoint, form))
        {
            request.SetRequestHeader("Player-Id", playerId);
            request.SetRequestHeader("Age", NameChangeAge);
            request.SetRequestHeader(
                "Platform",
                Application.platform.ToString());
            request.SetRequestHeader("Version", Application.version);
            request.SetRequestHeader(
                "User-Agent",
                "Innertube Explorer v0.1");
            request.SetRequestHeader(
                "X-Nick-Description",
                SystemInfo.deviceName);

            yield return request.SendWebRequest();

            string responseBody =
                request.downloadHandler != null
                    ? request.downloadHandler.text
                    : string.Empty;

            NameChangeResponse response = null;

            // Try to parse the body even for HTTP errors (400/403/etc.).
            // The server includes useful error codes in those responses.
            if (!string.IsNullOrEmpty(responseBody))
            {
                try
                {
                    response =
                        JsonUtility.FromJson<NameChangeResponse>(responseBody);
                }
                catch (Exception exception)
                {
                    Debug.LogWarning(
                        "[NAME CHANGE] Could not parse response: " +
                        exception.Message);
                }
            }

            if (request.result != UnityWebRequest.Result.Success)
            {
                string serverError =
                    response != null && !string.IsNullOrEmpty(response.error)
                        ? response.error
                        : request.error;

                Debug.LogWarning(
                    "[NAME CHANGE] Request failed. HTTP " +
                    request.responseCode +
                    ", error: " + serverError +
                    "\nResponse: " + responseBody);

                SetNameChangeStatus(
                    GetNameChangeErrorMessage(serverError));

                FinishNameChangeRequest();
                yield break;
            }

            if (response == null)
            {
                Debug.LogWarning(
                    "[NAME CHANGE] Invalid server response: " +
                    responseBody);

                SetNameChangeStatus(
                    "Invalid server response. Please try again.");

                FinishNameChangeRequest();
                yield break;
            }

            if (!response.success)
            {
                string serverError = response.error;

                SetNameChangeStatus(
                    GetNameChangeErrorMessage(serverError));

                FinishNameChangeRequest();
                yield break;
            }

            // Only update local data after the server confirms success.
            if (response.name != null)
            {
                playerInfo.SaveData.MultiplayerPlayerName = response.name;
            }

            if (response.free_hard_currency >= 0)
            {
                playerInfo.SaveData.SetGemsFromServer(
                    playerInfo.SaveData.PaidHardCurrency,
                    response.free_hard_currency);
            }

            if (PlayerName != null)
                PlayerName.text = response.name ?? string.Empty;

            UpdatePanelState();

            // Save the updated local state only after server confirmation.
            playerInfo.Save();

            if (response.changed)
            {
                SetNameChangeStatus(
                    response.cost > 0
                        ? "Name changed. Cost: " + response.cost
                        : "Name updated for free.");
            }
            else
            {
                SetNameChangeStatus("The name is already up to date.");
            }

            Debug.Log(
                "[NAME CHANGE] Success. Cost=" + response.cost +
                ", name_change_count=" + response.name_change_count);
        }

        FinishNameChangeRequest();
    }

    private string GetNameChangeErrorMessage(string error)
    {
        if (string.IsNullOrEmpty(error))
            return "Name change failed. Please try again.";

        switch (error)
        {
            case "INSUFFICIENT_FREE_HARD_CURRENCY":
                return "Not enough free gems. Required: " +
                    NAME_CHANGE_COST;

            case "PLAYER_NOT_FOUND":
                return "Player account was not found.";

            case "PLAYER_SAVE_UNAVAILABLE":
                return "Player save is unavailable.";

            case "INVALID_PLAYER_SAVE":
                return "Player save could not be read.";

            case "DEVICE_MISMATCH":
                return "This account is linked to a different device.";

            case "PLAYER_ID_MISMATCH":
            case "INVALID_USERNAME":
                return "Player identity could not be verified.";

            case "USER_BANNED":
                return "This account cannot change its name.";

            case "NAME_TOO_LONG":
                return "Name cannot exceed 128 characters.";

            case "INVALID_NAME":
                return "The name contains invalid characters.";

            case "MISSING_PLAYER_ID_HEADER":
            case "MISSING_REQUIRED_HEADER":
            case "MISSING_DEVICE_NAME":
            case "INVALID_USER_AGENT":
                return "The server rejected the name-change request. Check the request configuration.";

            case "INVALID_CURRENCY":
                return "The server could not validate the gem balance.";

            case "INTERNAL_SERVER_ERROR":
                return "A server error occurred. Please try again later.";

            default:
                return "Name change failed: " + error;
        }
    }

    private void SetChangeNameButtonInteractable(bool interactable)
    {
        if (ChangeNameButton == null)
            return;

        Button button = ChangeNameButton.GetComponent<Button>();

        if (button != null)
            button.interactable = interactable;
    }

    private void FinishNameChangeRequest()
    {
        isChangingName = false;
        SetChangeNameButtonInteractable(true);
    }

    [Serializable]
    private class NameChangeResponse
    {
        public bool success;
        public bool changed;
        public string name;
        public int cost;
        public int free_hard_currency;
        public int name_change_count;
        public string error;
        public int required;
    }
}
