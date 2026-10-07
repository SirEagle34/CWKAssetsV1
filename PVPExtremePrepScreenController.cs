using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PVPExtremePrepScreenController : Singleton<PVPExtremePrepScreenController>
{
    private enum NextAction { NONE, WAITING, PROCEED, ERROR, RETRY }

    public float CountdownTime = 20f;

    public UILabel HardCurrencyLabel;
    public UILabel SoftCurrencyLabel;
    public UILabel PlayerStamina;
    public UILabel PlayerStaminaTimer;
    public UILabel EnergyCost;
    public LeagueBadge PlayerBadge;
    public LeagueBadge OpponentBadge;
    public UILabel Countdown;
    public GameObject ReadyBlockingCollider;
    public UILabel LevelSetNotificationLabel;
    public UITexture[] PIPIconTargets = new UITexture[2];
    public Transform[] MyCreatureNodes;
    public UILabel MyPlayerNameLabel;
    public UILabel TeamCost;
    public UILabel TeamName;
    public UILabel TitleLabel;
    public UILabel LeaderName;
    public GameObject CancelButton;
    public GameObject BackButton;
    public GameObject PressToSearchLabel;
    public GameObject HeartButtonDecoration;
    public GameObject CheckmarkButtonDecoration;
    public UITexture BackgroundTexture;

    [Header("Tab to Click on Init")]

    public GameObject InitTab;

    public UILabel SeasonLabel;

    public PVPExtremeUI extremeUI;

    public UILabel SeasonName;

    public UILabel ExtremeListLabel;

    public UILabel ExtremeList1;

    public UILabel ExtremeList2;

    public UILabel ExtremeList3;

    public UILabel ExtremeList4;

    public UILabel ExtremeList5;

    public UILabel ExtremeList6;

    public UILabel ExtremeList7;

    [Header("Ban UI (assign if available)")]
    [SerializeField] private GameObject Button_StartBattle;
    [SerializeField] private GameObject Label_Play;
    [SerializeField] private GameObject BanWarningNode;
    [SerializeField] private UILabel BanWarningLabel;

    [Header("Ban Visuals (optional)")]
    [SerializeField] private GameObject BanTileLabelPrefab;
    [SerializeField] private Vector3 BanTileLabelLocalOffset = new Vector3(120.1f, -3f, 0f);

    private Loadout currentLoadout;
    private Loadout mExtremeMatchLoadout;
    private List<InventorySlotItem> extremeTempTeam = new List<InventorySlotItem>();
    private List<GameObject> mMySpawnedCreatures = new List<GameObject>();
    private GameObject mSpawnedBanCreature;
    private GameObject mBanTileLabelInstance;
    private GameObject mButtonBlockedLabelInstance;
    private UITexture mStartButtonTexture;
    private Color mButtonOriginalColor = Color.white;

    private bool mWaitingForOpponentStartData;
    private bool mOpponentStartDataReceived;
    private bool mOpponentLoadoutValidated = false;
    private float mMatchStartCountdown = -1f;
    private bool mIsVsScreen;
    private PvpMode mMode;
    private HelperItem mAlly;
    private int mLastCountdownVal;
    private bool mActive;
    public bool matchFound;
    public float syncTime;
    public ParticleSystem CountdownVFX;
    public GameObject SwordShieldAnimation;

    [Header("Tweens")]
    public UITweenController ShowTween;
    public UITweenController ShowAllyTween;
    public UITweenController HideTween;
    public UITweenController ExceededTeamCostTween;
    public UITweenController ConsumeTicketTween;
    public UITweenController ShowConnectingTween;
    public UITweenController HideConnectingTween;
    public UITweenController OpponentFoundTween;
    public UITweenController ShowFlagsAndCounterTween;
    public UITweenController FriendMatchStartingTween;
    public UITweenController CountdownPulseTween;
    public UITweenController ReadyTween;


    private bool mSearching;
    private bool mBackButtonPressed;
    private bool mWaitForUserAction;
    private bool opponentDataFullyReady;
    private bool opponentPendingValidation;
    private NextAction mUserActionProceed;
    private PlayerSaveData.ProceedNextStep mNextFunction;

    private bool hasShownBanPopupThisOpen = false;

    private List<string> pvpBanlist = new List<string>()
    {
        "DullCoolDog_Awaken",
        "SandasaurusRex_Awaken",
        "LadyScarab_Awaken",
        "ShamanWoad_Awaken"
    };

private List<string> pvpLeaderBanlist = new List<string>()
{
    "Leader_PeppermintButler"
};

    private List<string> ExtremeCreatureIDs = new List<string>()
    {
        "SgtSandwich_Base",
        "CoolDog_Awaken",
        "CornRonin_Awaken",
        "SandasaurusRex_Awaken",
        "PreTeenWolf_Awaken",
        "PunkCat_Awaken",
        "FieldReaper_Awaken"
    };

    private List<string> ExtremeHeroIDs = new List<string>()
    {
        "Leader_GrandPrixe"
    };

    public List<CreatureData> GetCreaturePool()
    {
        List<CreatureData> pool = new List<CreatureData>();

        foreach (var id in ExtremeCreatureIDs)
        {
            var data = CreatureDataManager.Instance.GetData(id);

            if (data != null)
                pool.Add(data);
            else
                Debug.LogError("Creature bulunamadı: " + id);
        }

        return pool;
    }

    public List<LeaderData> GetHeroPool()
    {
        List<LeaderData> pool = new List<LeaderData>();

        foreach (var id in ExtremeHeroIDs)
        {
            var data = LeaderDataManager.Instance.GetData(id);

            if (data != null)
                pool.Add(data);
            else
                Debug.LogError("Hero bulunamadı: " + id);
        }

        return pool;
    }

    public SeasonLabelColorPallette[] SeasonLabelColors = new SeasonLabelColorPallette[0];

    private PvpSeasonData _ActiveSeason;

    public void Populate(PVPExtremeData pvpExtremeData)
    {
        PlayerSaveData saveData = Singleton<PlayerInfoScript>.Instance.SaveData;
        ExtremeListLabel.text = pvpExtremeData.ExtremeListLabel;
        ExtremeList1.text = pvpExtremeData.ExtremeList;
        ExtremeList2.text = pvpExtremeData.ExtremeList2;
        ExtremeList3.text = pvpExtremeData.ExtremeList3;
        ExtremeList4.text = pvpExtremeData.ExtremeList4;
        ExtremeList5.text = pvpExtremeData.ExtremeList5;
        ExtremeList6.text = pvpExtremeData.ExtremeList6;
        ExtremeList7.text = pvpExtremeData.ExtremeList7;
        Invoke("GoToInitTab", 0.1f);
    }


    private void GoToInitTab()
    {
        ColorSeasonNameLabel(_ActiveSeason.BannerTextureNameOnly);
        if (InitTab != null)
        {
            InitTab.SendMessage("OnClick");
        }
        InitTab = null;
    }

    private void SafePlay(UITweenController tween)
    {
        if (tween == null) return;
        try { tween.Play(); }
        catch (System.Exception e)
        {
            string name = (tween != null && tween.gameObject != null) ? tween.gameObject.name : "<unknown tween>";
            Debug.LogWarning("[PVPPrepScreenController] SafePlay failed on " + name + ": " + e.Message);
        }
    }

    private void ColorSeasonNameLabel(string inBannerTextureNameOnly)
    {
        int num = 0;
        switch (inBannerTextureNameOnly)
        {
        case "PVP_Season_Cinnamon":
            num = 1;
            break;
        case "PVP_Season_Mint":
            num = 2;
            break;
        case "PVP_Season_Nutmeg":
            num = 3;
            break;
        case "PVP_Season_ToeSalt":
            num = 4;
            break;
        case "PVP_Season_SkullKing":
            num = 5;
            break;
        }
        SeasonLabelColorPallette seasonLabelColorPallette = SeasonLabelColors[num];
        SeasonName.color = seasonLabelColorPallette.TextColor;
        SeasonName.effectColor = seasonLabelColorPallette.OutlineColor;
        if (SeasonName.LabelShadow != null)
        {
            SeasonName.LabelShadow.ShadowTextColor = seasonLabelColorPallette.ShadowTextColor;
            SeasonName.LabelShadow.ShadowEffectColor = seasonLabelColorPallette.ShadowOutlineColor;
            SeasonName.LabelShadow.RefreshShadowLabel();
        }
    }

    private void SafeStopAndReset(UITweenController tween)
    {
        if (tween == null) return;
        try { tween.StopAndReset(); }
        catch (System.Exception e)
        {
            string name = (tween != null && tween.gameObject != null) ? tween.gameObject.name : "<unknown tween>";
            Debug.LogWarning("[PVPPrepScreenController] SafeStopAndReset failed on " + name + ": " + e.Message);
        }
    }

    private void SafePlayWithCallback(UITweenController tween, EventDelegate.Callback callback)
    {
        if (tween == null) { callback?.Invoke(); return; }
        try { tween.PlayWithCallback(callback); }
        catch (System.Exception e)
        {
            string name = (tween != null && tween.gameObject != null) ? tween.gameObject.name : "<unknown tween>";
            Debug.LogWarning("[PVPPrepScreenController] SafePlayWithCallback failed on " + name + ": " + e.Message);
            callback?.Invoke();
        }
    }
    
    // -------------------------
    // Initialization helpers
    // -------------------------
    private void Awake()
    {
        ResolveLocalReferences();

        // Deactivate any baked-in PlayBlockedLabel so it doesn't show before the ban check runs.
        // Uses GetComponentsInChildren(true) to find it even if the parent panel is still inactive.
        Transform blockedLabel = FindChildRecursive(transform, "PlayBlockedLabel");
        if (blockedLabel != null)
        {
            blockedLabel.gameObject.SetActive(false);
            mButtonBlockedLabelInstance = blockedLabel.gameObject;
        }
    }

    private void ResolveLocalReferences()
    {
        // quick exit if already assigned
        if (Button_StartBattle != null && Label_Play != null && BanWarningNode != null && BanWarningLabel != null)
        {
            CacheStartButtonTexture();
            return;
        }

        System.Action<Transform> dumpChildren = (parent) =>
        {
            string names = "";
            for (int i = 0; i < parent.childCount; i++) names += parent.GetChild(i).name + ", ";
            Debug.LogWarning($"[PVPPrep] Children of {parent.name}: {names}");
        };

        // Button_StartBattle
        if (Button_StartBattle == null)
        {
            Transform t = transform.Find("GameStartButtons/Button_StartBattle");
            if (t == null) t = FindChildRecursive(transform, "Button_StartBattle");
            if (t != null) Button_StartBattle = t.gameObject;
            else
            {
                Debug.LogWarning("[PVPPrep] Button_StartBattle not found under " + name);
                dumpChildren(transform);
            }
        }

        CacheStartButtonTexture();

        // Label_Play
        if (Label_Play == null)
        {
            Transform t = transform.Find("GameStartButtons/Label_Play");
            if (t == null) t = FindChildRecursive(transform, "Label_Play");
            if (t != null) Label_Play = t.gameObject;
            else Debug.LogWarning("[PVPPrep] Label_Play not found under " + name);
        }

        // BanWarningNode
        if (BanWarningNode == null)
        {
            Transform t = FindChildRecursive(transform, "BanWarningNode");
            if (t != null) BanWarningNode = t.gameObject;
            else
            {
                Debug.LogWarning("[PVPPrep] BanWarningNode not found under " + name);
                dumpChildren(transform);
            }
        }

        // Move BanWarningNode to requested coordinates if present
        if (BanWarningNode != null)
        {
            // These coordinates are in BanWarningNode's parent's local space
            BanWarningNode.transform.localPosition = new Vector3(-670f, 610f, -76f);

            // If the label exists as a child, ensure it sits correctly inside the node
            if (BanWarningLabel == null)
            {
                BanWarningLabel = BanWarningNode.GetComponentInChildren<UILabel>(true);
            }
            if (BanWarningLabel != null)
            {
                // position label relative to BanWarningNode so the label text aligns as expected
                BanWarningLabel.transform.localPosition = Vector3.zero;
            }
        }

        if (BanWarningLabel == null && BanWarningNode != null)
        {
            BanWarningLabel = BanWarningNode.GetComponentInChildren<UILabel>(true);
            if (BanWarningLabel == null) Debug.LogWarning("[PVPPrep] BanWarningLabel not found under BanWarningNode");
        }
    }

    private void CacheStartButtonTexture()
    {
        if (Button_StartBattle == null) return;
        if (mStartButtonTexture != null) return;
        mStartButtonTexture = Button_StartBattle.GetComponent<UITexture>();
        if (mStartButtonTexture != null) mButtonOriginalColor = mStartButtonTexture.color;
    }

    private Transform FindChildRecursive(Transform parent, string name)
    {
        if (parent == null) return null;
        if (parent.name == name) return parent;
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            Transform r = FindChildRecursive(child, name);
            if (r != null) return r;
        }
        return null;
    }


    // Runtime debug helper — prints key refs and forces visible minimal ban/start UI for inspection
    public void DebugDumpAndForceShow()
    {
        Debug.Log("[PVPPrep] DebugDumpAndForceShow invoked.");
        Debug.Log("[PVPPrep] This controller activeInHierarchy: " + gameObject.activeInHierarchy);
        Debug.Log("[PVPPrep] BanWarningNode assigned: " + (BanWarningNode != null));
        Debug.Log("[PVPPrep] BanWarningLabel assigned: " + (BanWarningLabel != null));
        Debug.Log("[PVPPrep] Button_StartBattle assigned: " + (Button_StartBattle != null));
        Debug.Log("[PVPPrep] Label_Play assigned: " + (Label_Play != null));
        Debug.Log("[PVPPrep] ShowTween assigned: " + (ShowTween != null));
        Debug.Log("[PVPPrep] ShowAllyTween assigned: " + (ShowAllyTween != null));

        if (BanWarningNode != null) BanWarningNode.SetActive(true);
        if (Button_StartBattle != null) Button_StartBattle.SetActive(true);
        if (Label_Play != null) Label_Play.SetActive(true);

        if (BanWarningLabel != null) Debug.Log("[PVPPrep] BanWarningLabel.text: " + BanWarningLabel.text);
    }

    public bool InFriendMatchLobby() => mActive && mMode == PvpMode.Friend;

    // -------------------------
    // Show / lifecycle
    // -------------------------
    public void Show(PvpMode mode, string allyName, bool inShouldShowBackButton = false)
    {
        gameObject.SetActive(true);
        var data = PVPExtremeDataManager.Instance.GetData("ExtremeCreaturesID");
        FindObjectOfType<PVPExtremeUI>().Setup(data);
        
        hasShownBanPopupThisOpen = false;

        mActive = true;
        mExtremeMatchLoadout = null;
        if (ReadyBlockingCollider != null) ReadyBlockingCollider.SetActive(false);
        if (SwordShieldAnimation != null) SwordShieldAnimation.SetActive(false);
        if (LevelSetNotificationLabel != null) LevelSetNotificationLabel.text = string.Empty;
        Singleton<PlayerInfoScript>.Instance.StateData.MultiplayerMode = true;
        Singleton<PlayerInfoScript>.Instance.PvPData.ExtremeMode = mode == PvpMode.Extreme;
        mMode = mode;
        if (BackButton != null) BackButton.SetActive(inShouldShowBackButton);
        if (EnergyCost != null) EnergyCost.text = MiscParams.PvpStaminaMatchCost.ToString();

        if (BanWarningNode != null) BanWarningNode.SetActive(false);
        if (BanWarningLabel != null) { BanWarningLabel.text = string.Empty; BanWarningLabel.gameObject.SetActive(false); }

        // Ensure the baked-in PlayBlockedLabel starts hidden and Label_Play starts visible
        if (mButtonBlockedLabelInstance != null) mButtonBlockedLabelInstance.SetActive(false);
        var strayBlocked = GameObject.FindObjectsOfType<Transform>().Where(t => t.name == "PlayBlockedLabel").ToArray();
        foreach (var t in strayBlocked) t.gameObject.SetActive(false);
        if (Label_Play != null) Label_Play.SetActive(true);

        RefreshMyLoadout();
        mWaitingForOpponentStartData = false;
        mOpponentStartDataReceived = false;
        mMatchStartCountdown = -1f;

        switch (mode)
        {
            case PvpMode.Ranked:
                if (TitleLabel != null)
                    TitleLabel.text = KFFLocalization.Get("!!BASICAL_MATCH");
                break;

            case PvpMode.Extreme:
                if (TitleLabel != null)
                    TitleLabel.text = KFFLocalization.Get("!!EXTREME_MATCH");
                break;

            case PvpMode.Unranked:
                if (TitleLabel != null)
                    TitleLabel.text = KFFLocalization.Get("!!UNRANKED_MATCH");
                break;

            case PvpMode.Friend:
                if (TitleLabel != null)
                    TitleLabel.text = KFFLocalization.Get("!!ALLY_MATCH");
                break;
        }

        if (mode == PvpMode.Friend)
        {
            SafePlay(ShowAllyTween);

            if (PressToSearchLabel != null) PressToSearchLabel.SetActive(false);
            if (HeartButtonDecoration != null) HeartButtonDecoration.SetActive(false);
            if (CheckmarkButtonDecoration != null) CheckmarkButtonDecoration.SetActive(true);

            // 🔥 KRİTİK: rank / season logic çalışmasın
            return;
        }
        else
        {
            SafePlay(ShowTween);

            if (PressToSearchLabel != null) PressToSearchLabel.SetActive(true);
            if (HeartButtonDecoration != null) HeartButtonDecoration.SetActive(true);
            if (CheckmarkButtonDecoration != null) CheckmarkButtonDecoration.SetActive(false);
        }
        Singleton<PlayerInfoScript>.Instance.CheckPvpSeasonStatus();
    }

    public void ShowNormal(string allyName, bool backButton)
    {
        Show(PvpMode.Ranked, allyName, backButton); // or your normal mapping
    }

    public void ShowExtreme(string allyName, bool backButton)
    {
        Show(PvpMode.Extreme, allyName, backButton);
    }


    // -------------------------
    // Loadout / ban-check
    // -------------------------
    private void RefreshMyLoadout()
    {
        if (MyPlayerNameLabel != null) MyPlayerNameLabel.text = Singleton<PlayerInfoScript>.Instance.PvPData.PlayerName;
        Loadout currentLoadout = GenerateExtremeAutoLoadout();

        if (currentLoadout != null && PIPIconTargets != null && PIPIconTargets.Length > 0 && PIPIconTargets[0] != null)
        {
            try
            {
                Singleton<FrontEndPIPController>.Instance.ShowModelPortraits(new LeaderData[1] { currentLoadout.Leader.SelectedSkin }, new UIWidget[1] { PIPIconTargets[0] });
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[PVPPrep] ShowModelPortraits failed: " + e.Message);
            }
        }

        foreach (GameObject go in mMySpawnedCreatures) NGUITools.Destroy(go);
        mMySpawnedCreatures.Clear();
        InventoryTile.ClearDelegates(true);

        if (currentLoadout != null)
        {
            if (MyCreatureNodes != null)
            {
                for (int i = 0; i < currentLoadout.CreatureSet.Count && i < MyCreatureNodes.Length; i++)
                {
                    if (currentLoadout.CreatureSet[i] != null)
                    {
                        GameObject go = MyCreatureNodes[i].InstantiateAsChild(Singleton<PrefabReferences>.Instance.InventoryTile);
                        if (go != null)
                        {
                            go.ChangeLayer(gameObject.layer);
                            InventoryTile component = go.GetComponent<InventoryTile>();
                            if (component != null)
                            {
                                component.Populate(currentLoadout.CreatureSet[i]);
                                component.ShowRarityFrameMini();
                            }
                            mMySpawnedCreatures.Add(go);
                        }
                    }
                }
            }

            int selectedLoadout = Singleton<PlayerInfoScript>.Instance.SaveData.SelectedLoadout;
            if (TeamName != null) TeamName.text = Singleton<PlayerInfoScript>.Instance.GetTeamName(selectedLoadout);
            int teamCost = currentLoadout.GetTeamCost();
            int teamCost2 = Singleton<PlayerInfoScript>.Instance.RankData.TeamCost;
            if (TeamCost != null) TeamCost.text = KFFLocalization.Get("!!TEAM_WEIGHT") + "  " + teamCost + " / " + teamCost2;
            if (LeaderName != null) LeaderName.text = currentLoadout.Leader.Form.Name;
            if (BackgroundTexture != null) BackgroundTexture.ReplaceTexture(currentLoadout.Leader.Form.VSScreenBackground);

            if (teamCost > teamCost2) SafePlay(ExceededTeamCostTween);
            else SafeStopAndReset(ExceededTeamCostTween);

            RefreshMyLeagueBadge(true);
        }

        // Run ban check (safe local lookups)
        CheckPvPBanlistInPrep();
    }

   private void CheckPvPBanlistInPrep()
{
    ResolveLocalReferences();

    // Skip ban enforcement unless in Ranked mode
    if (!mMode.EnforceBans())
    {
        Debug.Log("[PVPPrep] Skipping ban check – mode is not Ranked.");
        ClearBanVisuals();
        DisableStartButtonInteraction(false);
        return;
    }

    Loadout currentLoadout = Singleton<PlayerInfoScript>.Instance.GetCurrentLoadout();
    if (currentLoadout == null) return;

    // central cleanup of previous visuals
    ClearBanVisuals();

    bool foundBanned = false;
    List<string> bannedNames = new List<string>();

  
    if (currentLoadout.Leader?.Form != null)
    {
        string leaderID = currentLoadout.Leader.Form.ID;
        
        if (pvpLeaderBanlist.Contains(leaderID))
        {
            foundBanned = true;
            bannedNames.Add(currentLoadout.Leader.Form.Name);
            
            DisableStartButtonInteraction(true);
            
            if (BanWarningNode != null)
            {
                BanWarningNode.SetActive(true);
                
            
            }
            
            CacheStartButtonTexture();
            if (mStartButtonTexture != null)
            {
                mStartButtonTexture.color = Color.Lerp(mButtonOriginalColor, Color.red, 0.85f);
            }
        }
    }

    // Existing creature ban check
    foreach (InventorySlotItem slotItem in currentLoadout.CreatureSet)
    {
        if (slotItem?.Creature?.Form == null) continue;
        CreatureItem creature = slotItem.Creature;

        if (pvpBanlist.Contains(creature.Form.ID))
        {
            foundBanned = true;
            bannedNames.Add(creature.Form.Name);

            // spawn visuals only once (first banned creature)
            if (mSpawnedBanCreature == null)
            {
                DisableStartButtonInteraction(true);

                if (BanWarningNode != null)
                {
                    BanWarningNode.SetActive(true);
                    mSpawnedBanCreature = BanWarningNode.InstantiateAsChild(Singleton<PrefabReferences>.Instance.InventoryTile);
                    if (mSpawnedBanCreature != null)
                    {
                        mSpawnedBanCreature.ChangeLayer(gameObject.layer);
                        InventoryTile tile = mSpawnedBanCreature.GetComponent<InventoryTile>();
                        if (tile != null) tile.PopulateAndForceDisplay(creature.Form);

                        Transform anchor = EnsureBanWarningAnchor();
                        ParentAndResetSpawn(mSpawnedBanCreature, anchor, BanTileLabelLocalOffset);

                        EnsureBanTileHint(mSpawnedBanCreature, "This creature is banned in PvP!", BanTileLabelLocalOffset);
                    }
                }
                else
                {
                    if (MyCreatureNodes != null && MyCreatureNodes.Length > 0)
                    {
                        mSpawnedBanCreature = MyCreatureNodes[0].InstantiateAsChild(Singleton<PrefabReferences>.Instance.InventoryTile);
                        if (mSpawnedBanCreature != null)
                        {
                            mSpawnedBanCreature.ChangeLayer(gameObject.layer);
                            InventoryTile tile = mSpawnedBanCreature.GetComponent<InventoryTile>();
                            if (tile != null) tile.PopulateAndForceDisplay(creature.Form);
                            ParentAndResetSpawn(mSpawnedBanCreature, MyCreatureNodes[0], BanTileLabelLocalOffset);
                            EnsureBanTileHint(mSpawnedBanCreature, "This creature is banned in PvP!", BanTileLabelLocalOffset);
                        }
                    }
                    else if (Singleton<PrefabReferences>.Instance?.InventoryTile != null)
                    {
                        mSpawnedBanCreature = Instantiate(Singleton<PrefabReferences>.Instance.InventoryTile);
                        mSpawnedBanCreature.ChangeLayer(gameObject.layer);
                        InventoryTile tile = mSpawnedBanCreature.GetComponent<InventoryTile>();
                        if (tile != null) tile.PopulateAndForceDisplay(creature.Form);
                        ParentAndResetSpawn(mSpawnedBanCreature, transform, BanTileLabelLocalOffset);
                        EnsureBanTileHint(mSpawnedBanCreature, "This creature is banned in PvP!", BanTileLabelLocalOffset);
                    }
                }

                CacheStartButtonTexture();
                if (mStartButtonTexture != null) mStartButtonTexture.color = Color.Lerp(mButtonOriginalColor, Color.red, 0.85f);
            }
        }
    }

    // If banned creatures/leaders found, show warning
    if (foundBanned)
    {
        string messageNames;
        if (bannedNames.Count == 1) messageNames = bannedNames[0];
        else if (bannedNames.Count == 2) messageNames = bannedNames[0] + " and " + bannedNames[1];
        else messageNames = string.Join(", ", bannedNames.GetRange(0, bannedNames.Count - 1).ToArray()) + " and " + bannedNames[bannedNames.Count - 1];

        if (BanWarningLabel != null)
        {
            BanWarningLabel.text = "⚠ " + messageNames + " is banned in PvP!";
            BanWarningLabel.gameObject.SetActive(true);
        }

        EnsureSinglePlayBlockedLabel();

        if (!hasShownBanPopupThisOpen)
        {
            hasShownBanPopupThisOpen = true;
            string msg = KFFLocalization.Get("!!PVP_BANNED_CREATURE").Replace("<creature>", messageNames);
            Singleton<SimplePopupController>.Instance.ShowMessage(string.Empty, msg);
        }
    }
    else
    {
        // restore normal UI if no banned creatures/leaders
        DisableStartButtonInteraction(false);
        ClearBanVisuals();
    }
}

    // Ensure there is a BanWarningAnchor under this controller and return it
    private Transform EnsureBanWarningAnchor()
    {
        if (BanWarningNode != null) return BanWarningNode.transform.parent ?? BanWarningNode.transform;

        Transform t = FindChildRecursive(transform, "BanWarningAnchor");
        if (t != null) return t;

        GameObject anchorGO = new GameObject("BanWarningAnchor");
        anchorGO.layer = gameObject.layer;
        anchorGO.transform.SetParent(transform, false);
        anchorGO.transform.localPosition = new Vector3(480f, -323.5f, 0f);
        anchorGO.transform.localRotation = Quaternion.identity;
        anchorGO.transform.localScale = Vector3.one;
        anchorGO.SetActive(true);
        return anchorGO.transform;
    }

    // Parent spawned tile to anchor and reset transforms for predictable placement
    private void ParentAndResetSpawn(GameObject spawned, Transform anchor, Vector3 labelOffset)
    {
        if (spawned == null || anchor == null) return;

        spawned.transform.SetParent(anchor, false);
        spawned.transform.localPosition = Vector3.zero;
        spawned.transform.localRotation = Quaternion.identity;
        spawned.transform.localScale = Vector3.one;

        spawned.transform.localEulerAngles = Vector3.zero;

        Transform label = spawned.transform.Find("BanTileLabel");
        if (label != null)
        {
            label.SetParent(spawned.transform, false);
            label.localPosition = labelOffset;
            label.localRotation = Quaternion.identity;
            label.localScale = Vector3.one;
        }
    }

    // Ensure or create NGUI hint/label above the spawned ban tile
    private void EnsureBanTileHint(GameObject spawnedTile, string hintText, Vector3 localOffset)
    {
        if (spawnedTile == null) return;

        // Hardcoded desired offset for BanTileHint (kept from your working example)
        Vector3 desiredHintOffset = new Vector3(117f, -1.5f, 0f);

        if (mBanTileLabelInstance != null)
        {
            var lblExisting = mBanTileLabelInstance.GetComponent<UILabel>();
            if (lblExisting != null) lblExisting.text = hintText;
            mBanTileLabelInstance.transform.SetParent(spawnedTile.transform, false);
            mBanTileLabelInstance.transform.localPosition = desiredHintOffset;
            return;
        }

        if (BanTileLabelPrefab != null)
        {
            mBanTileLabelInstance = NGUITools.AddChild(spawnedTile, BanTileLabelPrefab);
            if (mBanTileLabelInstance != null) mBanTileLabelInstance.transform.localPosition = desiredHintOffset;
            UILabel lb = mBanTileLabelInstance.GetComponentInChildren<UILabel>();
            UILabel tileLbl = spawnedTile.GetComponentInChildren<UILabel>();
            if (lb != null && tileLbl != null) lb.depth = tileLbl.depth + 1;
            return;
        }

        GameObject hintGO = new GameObject("BanTileHint");
        hintGO.layer = spawnedTile.layer;
        hintGO.transform.SetParent(spawnedTile.transform, false);
        hintGO.transform.localPosition = desiredHintOffset;
        hintGO.transform.localRotation = Quaternion.identity;
        hintGO.transform.localScale = Vector3.one;

        UILabel hintLbl = hintGO.AddComponent<UILabel>();
        if (BanWarningLabel != null) hintLbl.bitmapFont = BanWarningLabel.bitmapFont;
        hintLbl.fontSize = 20;
        hintLbl.color = Color.yellow;
        hintLbl.text = hintText;
        hintLbl.alignment = NGUIText.Alignment.Center;

        UILabel tileWidget = spawnedTile.GetComponentInChildren<UILabel>();
        if (tileWidget != null) hintLbl.depth = tileWidget.depth + 1;

        mBanTileLabelInstance = hintGO;
    }

    // Ensure single PlayBlockedLabel instance, activate it, and hide Label_Play
    private void EnsureSinglePlayBlockedLabel()
    {
        if (Label_Play == null) return;

        Transform parent = Label_Play.transform.parent != null ? Label_Play.transform.parent : Label_Play.transform;
        Transform existing = parent.Find("PlayBlockedLabel");

        // global search if not found locally
        if (existing == null)
        {
            var global = transform.root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "PlayBlockedLabel");
            if (global != null) existing = global;
        }

        // destroy extras globally except the one we will reuse
        var allBlocked = GameObject.FindObjectsOfType<Transform>().Where(t => t.name == "PlayBlockedLabel").ToArray();
        foreach (var t in allBlocked)
        {
            if (existing == null || t != existing)
            {
                NGUITools.Destroy(t.gameObject);
            }
        }

        if (existing != null)
        {
            existing.SetParent(parent, false);
            existing.localPosition = Label_Play.transform.localPosition + new Vector3(4.5f, 0f, 0f);
            existing.localEulerAngles = Vector3.zero;
            existing.localScale = Vector3.one;
            UILabel lblComp = existing.GetComponent<UILabel>();
            UILabel playLblComp = Label_Play.GetComponent<UILabel>();
            if (lblComp != null && playLblComp != null) lblComp.depth = playLblComp.depth + 1;
            existing.gameObject.SetActive(true);
            mButtonBlockedLabelInstance = existing.gameObject;
            Label_Play.SetActive(false);
            return;
        }

        // create a new one
        GameObject blk = new GameObject("PlayBlockedLabel");
        blk.layer = Label_Play.layer;
        blk.transform.SetParent(parent, false);
        blk.transform.localPosition = Label_Play.transform.localPosition + new Vector3(4.5f, 0f, 0f);
        blk.transform.localRotation = Quaternion.identity;
        blk.transform.localScale = Vector3.one;

        UILabel newLbl = blk.AddComponent<UILabel>();
        UILabel playLblComp2 = Label_Play.GetComponent<UILabel>();
        if (playLblComp2 != null)
        {
            newLbl.bitmapFont = playLblComp2.bitmapFont;
            newLbl.fontSize = Mathf.Max(16, playLblComp2.fontSize - 2);
            newLbl.depth = playLblComp2.depth + 1;
        }
        newLbl.color = Color.red;
        newLbl.text = "BLOCKED";
        newLbl.alignment = NGUIText.Alignment.Left;

        int playIndex = Label_Play.transform.GetSiblingIndex();
        blk.transform.SetSiblingIndex(playIndex + 1);

        mButtonBlockedLabelInstance = blk;
        Label_Play.SetActive(false);
    }

    // central cleanup for ban visuals and labels
    private void ClearBanVisuals()
    {
        if (mSpawnedBanCreature != null) { NGUITools.Destroy(mSpawnedBanCreature); mSpawnedBanCreature = null; }
        if (mBanTileLabelInstance != null) { NGUITools.Destroy(mBanTileLabelInstance); mBanTileLabelInstance = null; }

        // Deactivate the blocked label instead of destroying it (it may be baked into the scene)
        if (mButtonBlockedLabelInstance != null) { mButtonBlockedLabelInstance.SetActive(false); }

        // Also deactivate any stray PlayBlockedLabel objects not tracked by mButtonBlockedLabelInstance
        var allBlocked = GameObject.FindObjectsOfType<Transform>().Where(t => t.name == "PlayBlockedLabel").ToArray();
        foreach (var t in allBlocked) t.gameObject.SetActive(false);

        if (mStartButtonTexture != null) mStartButtonTexture.color = mButtonOriginalColor;

        if (Label_Play != null) Label_Play.SetActive(true);
        if (BanWarningLabel != null) { BanWarningLabel.text = string.Empty; BanWarningLabel.gameObject.SetActive(false); }
        if (BanWarningNode != null) BanWarningNode.SetActive(false);
        hasShownBanPopupThisOpen = false;
    }

    private void DisableStartButtonInteraction(bool disable)
    {
        if (Button_StartBattle == null) ResolveLocalReferences();
        if (Button_StartBattle == null) return;

        var nguiBtn = Button_StartBattle.GetComponent<UIButton>();
        if (nguiBtn != null) nguiBtn.isEnabled = !disable;

        var unityBtn = Button_StartBattle.GetComponent<UnityEngine.UI.Button>();
        if (unityBtn != null) unityBtn.interactable = !disable;

        var col = Button_StartBattle.GetComponent<Collider>();
        if (col != null) col.enabled = !disable;
    }

    private void RefreshMyLeagueBadge(bool includeNameAndFlag)
    {
        if (PlayerBadge != null) { PlayerBadge.PopulateMyData(); PlayerBadge.ShowNameAndFlag(includeNameAndFlag); }
    }

    private void RefreshOpponentLeagueBadge(bool includeNameAndFlag)
    {
        PvPGameStateData pvPData = Singleton<PlayerInfoScript>.Instance.PvPData;
        if (pvPData == null) return;
        if (OpponentBadge == null) return;

        if (pvPData.OpponentPortraitData.ID == "Facebook")
        {
            OpponentBadge.PopulateOtherPlayerData(pvPData.OpponentName, pvPData.OpponentPortrait, pvPData.OpponentLevel, pvPData.OpponentBestLevel, Singleton<CountryFlagManager>.Instance.TextureForCountryCode(Singleton<OnlinePvPManager>.Instance.GetOpponentCountryCode()));
        }
        else if (pvPData.OpponentPortraitData.ID == "Default")
        {
            OpponentBadge.PopulateOtherPlayerData(pvPData.OpponentName, pvPData.OpponentLoadout.Leader.SelectedSkin.PortraitTexture, pvPData.OpponentLevel, pvPData.OpponentBestLevel, Singleton<CountryFlagManager>.Instance.TextureForCountryCode(Singleton<OnlinePvPManager>.Instance.GetOpponentCountryCode()));
        }
        else
        {
            OpponentBadge.PopulateOtherPlayerData(pvPData.OpponentName, pvPData.OpponentPortraitData.Texture, pvPData.OpponentLevel, pvPData.OpponentBestLevel, Singleton<CountryFlagManager>.Instance.TextureForCountryCode(Singleton<OnlinePvPManager>.Instance.GetOpponentCountryCode()));
        }
    }

    public void OnClickNextLoadout() { Singleton<PlayerInfoScript>.Instance.GoToNextLoadout(); RefreshMyLoadout(); }
    public void OnClickPrevLoadout() { Singleton<PlayerInfoScript>.Instance.GoToPrevLoadout(); RefreshMyLoadout(); }

    public void OnClickPlayUpdated()
    {
        mSearching = false;
        Loadout currentLoadout;

        if (mMode == PvpMode.Extreme)
        {
            currentLoadout = GenerateExtremeAutoLoadout();
        }
        else
        {
            currentLoadout = Singleton<PlayerInfoScript>.Instance.GetCurrentLoadout();
        }
        if (!MiscParams.PvpEnable) { Singleton<SimplePopupController>.Instance.ShowMessage(KFFLocalization.Get("!!GAME_ERROR_CONTACTING"), KFFLocalization.Get("!!PVP_UNDER_MAINTENANCE"), true); return; }
        if (currentLoadout == null || !currentLoadout.IsUsable())
        {
            Singleton<SimplePopupController>.Instance.ShowMessage(
                string.Empty,
                KFFLocalization.Get("!!NEED_CREATURE")
            );
            return;
        }
        if (currentLoadout.GetTeamCost() > Singleton<PlayerInfoScript>.Instance.RankData.TeamCost) { Singleton<SimplePopupController>.Instance.ShowMessage(string.Empty, KFFLocalization.Get("!!EXCEEDS_WEIGHT")); Singleton<SLOTAudioManager>.Instance.PlayErrorSound(); return; }
        if (DetachedSingleton<StaminaManager>.Instance.GetStamina(StaminaType.Pvp) < MiscParams.PvpStaminaMatchCost) { Singleton<SimplePopupController>.Instance.ShowPurchasePrompt(KFFLocalization.Get("!!NO_PVP_STAMINA_BUY"), KFFLocalization.Get("!!NO_PVP_STAMINA_NOBUY"), MiscParams.StaminaRefillCost, RefillStaminaAndTryAgain); Singleton<SLOTAudioManager>.Instance.PlayErrorSound(); return; }

        Singleton<PlayerInfoScript>.Instance.StateData.CurrentLoadout = currentLoadout;
        if (mMode == PvpMode.Friend)
        {
            Debug.LogError("Invalid mode in ExtremePrepScreen");
            return;
        }
        else
        {
            mSearching = true;
            SafePlay(ShowConnectingTween);
            Singleton<MultiplayerMessageHandler>.Instance.StartMatchmaking();
        }
    }

    public void OnClickPlay()
    {
        mSearching = false;

        Loadout currentLoadout =
            GenerateExtremeAutoLoadout();

        if (!MiscParams.PvpEnable)
        {
            Singleton<SimplePopupController>.Instance.ShowMessage(
                KFFLocalization.Get("!!GAME_ERROR_CONTACTING"),
                KFFLocalization.Get("!!PVP_UNDER_MAINTENANCE"),
                true
            );
            return;
        }

        if (currentLoadout == null ||
            !currentLoadout.IsUsable())
        {
            Singleton<SimplePopupController>.Instance.ShowMessage(
                string.Empty,
                KFFLocalization.Get("!!NEED_CREATURE")
            );

            Singleton<SLOTAudioManager>.Instance.PlayErrorSound();
            return;
        }

        if (currentLoadout.GetTeamCost() >
            Singleton<PlayerInfoScript>.Instance.RankData.TeamCost)
        {
            Singleton<SimplePopupController>.Instance.ShowMessage(
                string.Empty,
                KFFLocalization.Get("!!EXCEEDS_WEIGHT")
            );

            Singleton<SLOTAudioManager>.Instance.PlayErrorSound();
            return;
        }

        if (DetachedSingleton<StaminaManager>.Instance
            .GetStamina(StaminaType.Pvp) <
            MiscParams.PvpStaminaMatchCost)
        {
            Singleton<SimplePopupController>.Instance.ShowPurchasePrompt(
                KFFLocalization.Get("!!NO_PVP_STAMINA_BUY"),
                KFFLocalization.Get("!!NO_PVP_STAMINA_NOBUY"),
                MiscParams.StaminaRefillCost,
                RefillStaminaAndTryAgain
            );

            Singleton<SLOTAudioManager>.Instance.PlayErrorSound();
            return;
        }

        PvPGameStateData pvpData =
            Singleton<PlayerInfoScript>.Instance.PvPData;

        pvpData.ExtremeMode = true;
        mExtremeMatchLoadout = currentLoadout;

        if (mMode == PvpMode.Friend)
        {
            ReadyBlockingCollider.SetActive(true);
            ReadyTween.Play();

            mWaitingForOpponentStartData = true;

            Singleton<MultiplayerExtremeMessageHandler>.Instance
                .SendMatchStartData(currentLoadout);
        }
        else
        {
            mSearching = true;

            ShowConnectingTween.Play();

            Singleton<MultiplayerExtremeMessageHandler>.Instance
                .StartExtremeMatchmaking();
        }
    }

    public void OnClickPlayOld2()
    {
        mSearching = false;

        var player = Singleton<PlayerInfoScript>.Instance;
        var pvpData = player.PvPData;

        // mMode bu ekranın gerçek seçilmiş PvP modudur.
        bool isExtreme = (mMode == PvpMode.Extreme);

        // Local state'i her maç başında kesin olarak güncelle.
        pvpData.ExtremeMode = isExtreme;

        Loadout loadout = isExtreme
            ? GenerateExtremeAutoLoadout()
            : player.GetCurrentLoadout();

        player.StateData.CurrentLoadout = loadout;

        Debug.Log(
            "[OnClickPlay] " +
            "mMode=" + mMode +
            ", ExtremeMode=" + pvpData.ExtremeMode +
            ", LoadoutCount=" +
            (loadout != null && loadout.CreatureSet != null
                ? loadout.CreatureSet.Count.ToString()
                : "NULL")
        );

        Debug.Log("Loadout null? " + (loadout == null));
        Debug.Log("IsUsable? " + loadout?.IsUsable());
        Debug.Log("Cost: " + loadout?.GetTeamCost());

        if (!MiscParams.PvpEnable)
            return;

        if (loadout == null || !loadout.IsUsable())
            return;

        if (loadout.GetTeamCost() > player.RankData.TeamCost)
            return;

        if (DetachedSingleton<StaminaManager>.Instance
            .GetStamina(StaminaType.Pvp) < MiscParams.PvpStaminaMatchCost)
            return;

        if (mMode == PvpMode.Friend)
        {
            SafePlay(ReadyTween);

            if (ReadyBlockingCollider != null)
                ReadyBlockingCollider.SetActive(true);

            mWaitingForOpponentStartData = true;

            // Her zaman bu maç için oluşturulan CurrentMatchLoadout gönder
            Singleton<MultiplayerMessageHandler>.Instance.SendMatchStartData(
                player.StateData.CurrentLoadout
            );
        }
        else
        {
            mSearching = true;

            SafePlay(ShowConnectingTween);

            Singleton<MultiplayerMessageHandler>.Instance.StartMatchmaking();
        }
    }

    public void OnConnectionComplete22(bool amIPrimary)
    {
        var player = Singleton<PlayerInfoScript>.Instance;
        var pvpData = player.PvPData;

        pvpData.AmIPrimary = amIPrimary;
        mWaitingForOpponentStartData = true;
        mSearching = false;

        // Maç modunu burada da kesinleştir.
        bool isExtreme = (mMode == PvpMode.Extreme);
        pvpData.ExtremeMode = isExtreme;

        if (player.StateData.CurrentLoadout == null)
        {
            player.StateData.CurrentLoadout = isExtreme
                ? GenerateExtremeAutoLoadout()
                : player.GetCurrentLoadout();
        }

        Debug.Log(
            "[OnConnectionComplete] " +
            "mMode=" + mMode +
            ", ExtremeMode=" + pvpData.ExtremeMode +
            ", LoadoutCount=" +
            (player.StateData.CurrentLoadout != null &&
            player.StateData.CurrentLoadout.CreatureSet != null
                ? player.StateData.CurrentLoadout.CreatureSet.Count.ToString()
                : "NULL")
        );

        Singleton<MultiplayerMessageHandler>.Instance
            .SendMatchStartData(player.StateData.CurrentLoadout);
    }

    private IEnumerator WaitForOpponentLoadout()
    {
        PvPGameStateData pvpData =
            Singleton<PlayerInfoScript>.Instance.PvPData;

        while (pvpData.OpponentLoadout == null ||
            pvpData.OpponentLoadout.CreatureSet == null)
        {
            yield return null;
        }

        while (pvpData.OpponentLoadout.CreatureSet.Count < 7)
        {
            yield return null;
        }

        Debug.Log(
            "[Extreme] Opponent loadout received: " +
            pvpData.OpponentLoadout.CreatureSet.Count
        );

        ValidateOpponentLoadout();

        mOpponentStartDataReceived = true;
    }
    
    public void OnOpponentDataReceived() { mOpponentStartDataReceived = true; mSearching = false; }

    public void OnConnectionComplete(bool amIPrimary)
    {
        PvPGameStateData pvpData =
            Singleton<PlayerInfoScript>.Instance.PvPData;

        pvpData.AmIPrimary = amIPrimary;
        pvpData.ExtremeMode = true;

        mWaitingForOpponentStartData = true;
        mSearching = false;

        if (player.StateData.CurrentLoadout == null)
        {
            player.StateData.CurrentLoadout =
                GenerateExtremeAutoLoadout();
        }

        Singleton<MultiplayerExtremeMessageHandler>.Instance
            .SendMatchStartData(
                player.StateData.CurrentLoadout
            );
    }

    public void OnClickCancelConnect() { SafePlay(HideConnectingTween); mWaitingForOpponentStartData = false; mOpponentStartDataReceived = false; mSearching = false; Singleton<MultiplayerExtremeMessageHandler>.Instance.CancelMatchmaking(); }

    private void LoadBattleScene() { Singleton<FrontEndPIPController>.Instance.UnloadModels(false); Singleton<PlayerInfoScript>.Instance.StateData.SelectedHelper = null; UICamera.UnlockInput(); Singleton<SLOTMusic>.Instance.StopMusic(0.5f); DetachedSingleton<SceneFlowManager>.Instance.LoadBattleScene(); }

    private void RefillStaminaAndTryAgain() { mWaitForUserAction = true; mUserActionProceed = NextAction.WAITING; Singleton<BusyIconPanelController>.Instance.Show(); Singleton<PlayerInfoScript>.Instance.SaveData.ConsumeHardCurrency2(MiscParams.StaminaRefillCost, "stamina refill", UserActionCallback); mNextFunction = StaminaRefillExecute; }

    public void UserActionCallback(PlayerSaveData.ActionResult result) { if (result.success) mUserActionProceed = NextAction.PROCEED; else mUserActionProceed = NextAction.ERROR; }
    private void StaminaRefillExecute() { DetachedSingleton<StaminaManager>.Instance.RefillStamina(); Singleton<BuyStaminaPopupController>.Instance.Show(OnClickPlay); }

    public void OnClickEditDeck() { Singleton<EditDeckController>.Instance.Show(RefreshMyLoadout); Singleton<FrontEndPIPController>.Instance.HideModelPortrait(); }

    public void OnClickExtremeEditDeck()
    {
        Singleton<EditDeckController>.Instance.Show(RefreshMyLoadout);

        Singleton<FrontEndPIPController>.Instance.HideModelPortrait();
    }

    public void OnClickMyLeader() { Loadout currentLoadout = GenerateExtremeAutoLoadout(); Singleton<LeaderDetailsController>.Instance.Show(currentLoadout.Leader, OnLeaderPopupClosed); }
    private void OnLeaderPopupClosed() { RefreshMyLoadout(); }

    private void Update()
    {
        UpdateTimers();
        RefreshCurrency();

        if (mWaitingForOpponentStartData && mOpponentStartDataReceived && mMatchStartCountdown == -1f)
        {
            PvPGameStateData pvPData = Singleton<PlayerInfoScript>.Instance.PvPData;
            RefreshMyLeagueBadge(true);
            UICamera.LockInput();
            if (mMode == PvpMode.Friend)
            {
                SafePlay(FriendMatchStartingTween);
                if (SwordShieldAnimation != null) SwordShieldAnimation.SetActive(true);
                SafePlay(ShowFlagsAndCounterTween);
                if (ReadyBlockingCollider != null) ReadyBlockingCollider.SetActive(false);
                RefreshOpponentLeagueBadge(true);
            }
            else
            {
                SafePlay(OpponentFoundTween);
                if (SwordShieldAnimation != null) SwordShieldAnimation.SetActive(true);
                SafePlay(ShowFlagsAndCounterTween);
                RefreshOpponentLeagueBadge(true);
            }
            DetachedSingleton<StaminaManager>.Instance.ConsumeStamina(StaminaType.Pvp, MiscParams.PvpStaminaMatchCost);
            Singleton<MultiplayerExtremeMessageHandler>.Instance.OnStartBattle();
            if (Singleton<PlayerInfoScript>.Instance.PvPData.RankedMode) Singleton<PlayerInfoScript>.Instance.SaveData.RankedPvpMatchStarted = true;
            pvPData.HistoryEntryForThisMatch = BattleHistoryLocalSavesManager.Instance.SaveNewBattleHistory();
            Singleton<PlayerInfoScript>.Instance.Save();
            mMatchStartCountdown = CountdownTime;
            mLastCountdownVal = -1;
        }

        if (mMatchStartCountdown > 0f)
        {
            if (OpponentFoundTween == null || !OpponentFoundTween.AnyTweenPlaying()) mMatchStartCountdown -= Time.deltaTime;
            if (mMatchStartCountdown < 0f)
            {
                BattleHistoryLocalSavesManager.Instance.CachedOpponentFlag = Singleton<OnlinePvPManager>.Instance.GetOpponentCountryCode();
                mMatchStartCountdown = -1f;
                LoadBattleScene();
            }
            int num = (int)Mathf.Ceil(Mathf.Max(0f, mMatchStartCountdown));
            if (Countdown != null) Countdown.text = num.ToString();
            if (num != mLastCountdownVal)
            {
                SafePlay(CountdownPulseTween);
                mLastCountdownVal = num;
                if (CountdownVFX != null) CountdownVFX.Play();
            }
        }

        if (!mWaitForUserAction) return;
        if (mUserActionProceed == NextAction.PROCEED)
        {
            Singleton<BusyIconPanelController>.Instance.Hide();
            mWaitForUserAction = false;
            mUserActionProceed = NextAction.NONE;
            if (mNextFunction != null) mNextFunction();
            mWaitForUserAction = false;
            mUserActionProceed = NextAction.NONE;
        }
        if (mUserActionProceed == NextAction.ERROR)
        {
            mWaitForUserAction = false;
            mUserActionProceed = NextAction.NONE;
            Singleton<BusyIconPanelController>.Instance.Hide();
            Singleton<SimplePopupController>.Instance.ShowMessage(string.Empty, KFFLocalization.Get("!!SERVER_ERROR_MESSAGE"), OnCloseServerAccessErrorPopup);
        }
    }


    private void RefreshCurrency()
    {
        if (SoftCurrencyLabel != null) SoftCurrencyLabel.text = Singleton<PlayerInfoScript>.Instance.SaveData.SoftCurrency.ToString();
        if (HardCurrencyLabel != null) HardCurrencyLabel.text = Singleton<PlayerInfoScript>.Instance.SaveData.HardCurrency.ToString();
    }

    private void OnCloseServerAccessErrorPopup() { }

    private void UpdateTimers()
    {
        if (matchFound) syncTime += Time.deltaTime;
        if (PlayerStamina != null && PlayerStamina.gameObject.activeInHierarchy)
        {
            int currentStamina, maxStamina, secondsUntilNextStamina;
            DetachedSingleton<StaminaManager>.Instance.GetStaminaInfo(StaminaType.Pvp, out currentStamina, out maxStamina, out secondsUntilNextStamina);
            if (PlayerStamina != null) PlayerStamina.text = maxStamina.ToString();
            if (PlayerStaminaTimer != null) PlayerStaminaTimer.text = ((secondsUntilNextStamina <= 0) ? string.Empty : PlayerInfoScript.BuildTimerString(secondsUntilNextStamina));
        }
    }

    public void OnBackClicked() { mBackButtonPressed = true; OnCloseClicked(); }

    public void OnCloseClicked()
    {
        Singleton<MouseOrbitCamera>.Instance.CheckTiltCamSettingBeforeTutorial();
        OnConfirmClose();
    }

    private void OnConfirmClose()
    {
        // 🔥 Friend bu screen'e hiç ait değil
        if (mMode == PvpMode.Friend)
        {
            Singleton<MultiplayerMessageHandler>.Instance.SendLeaveGame("leave");
            return;
        }

        mActive = false;
        ClearBanVisuals();

        Singleton<MultiplayerMessageHandler>.Instance.SendLeaveGame("leave");
        Singleton<FrontEndPIPController>.Instance.HideModelPortrait();
        SafePlay(HideTween);

        if (!mBackButtonPressed)
        {
            Singleton<PvPModeSelectController>.Instance.Hide();
            return;
        }

        Singleton<PvPModeSelectController>.Instance.ShowElements();
        mBackButtonPressed = false;
        hasShownBanPopupThisOpen = false;
    }

    private void OnCancelClose()
    {
        if (ShowAllyTween != null) ShowAllyTween.ReattachBackButtonTarget();
    }

    public void OnFriendLeft()
    {
        if (mActive)
        {
            mActive = false;
            Singleton<SimplePopupController>.Instance.ShowMessage(string.Empty, KFFLocalization.Get("!!OTHER_PLAYER_LEFT"), OnConfirmPlayerLeft);
        }
    }

    private void OnConfirmPlayerLeft()
    {
        Singleton<FrontEndPIPController>.Instance.HideModelPortrait();
        SafePlay(HideTween);
        MenuStackManager.RemoveTopItemFromStack(true);
        ClearBanVisuals();
    }

    public void Unload()
    {
        Singleton<PlayerInfoScript>.Instance.StateData.MultiplayerMode = false;

        foreach (GameObject g in mMySpawnedCreatures) if (g != null) NGUITools.Destroy(g);
        mMySpawnedCreatures.Clear();

        ClearBanVisuals();
        if (mStartButtonTexture != null) mStartButtonTexture.color = mButtonOriginalColor;
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus && IsAndroidScreenRecordingActive())
        {
            // Evita cancelar matchmaking por pausa transitoria cuando MediaProjection está activa.
            return;
        }

        if (mSearching) OnClickCancelConnect();
    }

    private bool IsAndroidScreenRecordingActive()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            return MobileVideoRecorder.Instance != null && MobileVideoRecorder.Instance.IsRecording;
        }
        catch
        {
            return false;
        }
#else
        return false;
#endif
    }

    public void DebugFriend()
    {
        SafePlay(FriendMatchStartingTween);
        if (SwordShieldAnimation != null) SwordShieldAnimation.SetActive(true);
        SafePlay(ShowFlagsAndCounterTween);
        if (ReadyBlockingCollider != null) ReadyBlockingCollider.SetActive(false);

        Debug.Log("[PVPPrep] DebugFriend invoked: FriendMatchStartingTween and flags shown.");
    }

    public void DebugMatch()
    {
        SafePlay(OpponentFoundTween);
        if (SwordShieldAnimation != null) SwordShieldAnimation.SetActive(true);

        Debug.Log("[PVPPrep] DebugMatch invoked: OpponentFoundTween played.");
    }


   private List<CreatureItem> GenerateExtremeTeam()
    {
        List<CreatureItem> team = new List<CreatureItem>();

        for (int i = 0; i < ExtremeCreatureIDs.Count; i++)
        {
            string id = ExtremeCreatureIDs[i];

            CreatureData data = CreatureDataManager.Instance.GetData(id);

            if (data == null)
            {
                Debug.LogError("Creature bulunamadı: " + id);
                continue;
            }

            CreatureItem creature = new CreatureItem(data);

            creature.StarRating = 5;
            creature.Xp = 43075;
            creature.PassiveSkillLevel = 1;

            team.Add(creature);
        }

        return team;
    }

    public Loadout GenerateExtremeAutoLoadout()
    {
        var team = GenerateExtremeTeam();
        var heroPool = GetHeroPool();

        if (team == null || heroPool == null || heroPool.Count == 0)
            return null;

        Loadout loadout = new Loadout();

        var heroData = heroPool[UnityEngine.Random.Range(0, heroPool.Count)];
        LeaderItem leader = new LeaderItem(heroData);

        loadout.Leader = leader;
        loadout.CreatureSet = new List<InventorySlotItem>();

        int targetSize = 7;

        for (int i = 0; i < targetSize; i++)
        {
            CreatureItem creature = (i < team.Count) ? team[i] : null;

            if (creature == null)
            {
                Debug.LogError("Creature missing slot: " + i);
                continue;
            }

            loadout.CreatureSet.Add(new InventorySlotItem(creature));
        }

        return loadout;
    }

    private void ValidateOpponentLoadout()
    {
        var pvPData = Singleton<PlayerInfoScript>.Instance.PvPData;

        if (pvPData == null || pvPData.OpponentLoadout == null)
        {
            mOpponentLoadoutValidated = false;
            Debug.LogError("Opponent loadout NULL");
            return;
        }

        var loadout = pvPData.OpponentLoadout;

        if (loadout.Leader == null)
        {
            mOpponentLoadoutValidated = false;
            Debug.LogError("Opponent leader NULL");
            return;
        }

        if (loadout.CreatureSet == null)
            loadout.CreatureSet = new List<InventorySlotItem>();

        // 🔥 FIX: ensure exactly 7 valid slots
        while (loadout.CreatureSet.Count < 7)
        {
            loadout.CreatureSet.Add(null);
        }

        if (loadout.CreatureSet.Count != 7)
        {
            mOpponentLoadoutValidated = false;
            Debug.LogError("Opponent creature count invalid: " + loadout.CreatureSet.Count);
            return;
        }

        // 🔥 SAFETY CHECK
        foreach (var item in loadout.CreatureSet)
        {
            if (item == null || item.Creature == null || item.Creature.Form == null)
            {
                mOpponentLoadoutValidated = false;
                Debug.LogError("Invalid creature in opponent loadout");
                return;
            }
        }

        mOpponentLoadoutValidated = true;
        opponentDataFullyReady = true;
    }
}
