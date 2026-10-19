using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Unity Editor script to inject custom starting hands and floop sequences
/// Place in Assets/Editor/ folder
/// </summary>
public class LoadoutInjectorWindow : EditorWindow
{
    [System.Serializable]
    public class LoadoutPreset
    {
        public string name = "Custom Loadout";
        public List<string> startingHand = new List<string>();
        public List<string> floopSequence = new List<string>();
    }

    private static LoadoutInjectorWindow window;
    private Vector2 scrollPosition;
    private int selectedPresetIndex = 0;
    private bool injectEnabled = true;
    private bool randomizeHandOrder = true;
    private bool waitingForMatch = false;
    private bool hasInjected = false;

    private List<LoadoutPreset> presets = new List<LoadoutPreset>
    {
        new LoadoutPreset
        {
            name = "#5 Swamp Combo",
            startingHand = new List<string> { "SWAMP_0043", "SWAMP_0015", "PLAINS_0016", "PLAINS_0009", "SWAMP_0017" },
            floopSequence = new List<string> { "Leader_PeppermintButler_05", "Leader_PeppermintButler_04", "Leader_PeppermintButler_02", "Leader_PeppermintButler_01", "Leader_PeppermintButler_03" }
        },
        new LoadoutPreset
        {
            name = "#3 Midrange",
            startingHand = new List<string> { "CORN_0008", "PLAINS_0002", "SAND_0043", "SAND_0002", "PLAINS_0013" },
            floopSequence = new List<string> { "Leader_IceKing_01", "Leader_IceKing_03", "Leader_IceKing_02", "Leader_IceKing_04", "Leader_IceKing_05" }
        },
        new LoadoutPreset
        {
            name = "#12 Dev Test",
            startingHand = new List<string> { "Leader_Banana_04", "Leader_IceKing_05", "Leader_IceKing_05", "SWAMP_0043", "Leader_GrandPrixe_01" },
            floopSequence = new List<string> { "Leader_Banana_01", "Leader_Banana_02", "Leader_Banana_03", "Leader_Banana_04", "Leader_Banana_05" }
        }
    };

    [MenuItem("Card Wars/Loadout Injector")]
    public static void ShowWindow()
    {
        window = GetWindow<LoadoutInjectorWindow>("Loadout Injector");
        window.minSize = new Vector2(400, 600);
    }

    private void OnEnable()
    {
        // Subscribe to play mode state changes
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        // Subscribe to update loop to check for game state
        EditorApplication.update += CheckForMatchStart;
    }

    private void OnDisable()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.update -= CheckForMatchStart;
    }

    private void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            hasInjected = false;
            if (injectEnabled)
            {
                waitingForMatch = true;
                Debug.Log("[LoadoutInjector] Waiting for match to start...");
            }
        }
        else if (state == PlayModeStateChange.ExitingPlayMode)
        {
            waitingForMatch = false;
            hasInjected = false;
        }
    }

    private void CheckForMatchStart()
    {
        if (!Application.isPlaying || !waitingForMatch || hasInjected)
            return;

        DWGame game = DWGame.Instance;
        if (game == null || !game.IsSetUp)
            return;

        PlayerState userPlayer = game.CurrentBoardState?.GetPlayerState(PlayerType.User);
        if (userPlayer == null)
            return;

        GameState currentState = game.GetCurrentGameState();
        
        // Inject right when game is set up, before any card dealing
        // This is before DealCreatureCards so TutorialInitialHandOverride takes effect
        if (currentState == GameState.FirstTurnCoinFlip)
        {
            waitingForMatch = false;
            hasInjected = true;
            InjectLoadout();
        }
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Loadout Injector", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Automatically injects custom starting hands and floop sequences when entering play mode.", MessageType.Info);

        EditorGUILayout.Space(10);
        
        // Enable/Disable injection
        EditorGUI.BeginChangeCheck();
        injectEnabled = EditorGUILayout.Toggle("Auto-Inject on Match Start", injectEnabled);
        if (EditorGUI.EndChangeCheck())
        {
            if (injectEnabled && Application.isPlaying && !hasInjected)
            {
                waitingForMatch = true;
                Debug.Log("[LoadoutInjector] Auto-inject activated. Waiting for next match...");
            }
            else if (!injectEnabled)
            {
                waitingForMatch = false;
                Debug.Log("[LoadoutInjector] Auto-inject disabled.");
            }
        }
        
        randomizeHandOrder = EditorGUILayout.Toggle("Randomize Hand Order", randomizeHandOrder);

        EditorGUILayout.Space(10);

        // Status indicator
        if (Application.isPlaying)
        {
            if (waitingForMatch && !hasInjected)
            {
                EditorGUILayout.HelpBox("⏳ Waiting for match to start... Start a battle to auto-inject!", MessageType.Info);
            }
            else if (hasInjected)
            {
                EditorGUILayout.HelpBox("✓ Injection complete! Start a new match to inject again.", MessageType.Info);
                if (GUILayout.Button("Reset (Enable for Next Match)"))
                {
                    hasInjected = false;
                    waitingForMatch = true;
                    Debug.Log("[LoadoutInjector] Reset. Waiting for next match...");
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Auto-inject is disabled. Enable it or click 'Inject Now'.", MessageType.Warning);
            }
        }
        else
        {
            EditorGUILayout.HelpBox("Press Play to start testing. Auto-inject will trigger when you start a match.", MessageType.None);
        }

        EditorGUILayout.Space(10);

        // Preset selection
        EditorGUILayout.LabelField("Select Preset:", EditorStyles.boldLabel);
        string[] presetNames = new string[presets.Count];
        for (int i = 0; i < presets.Count; i++)
        {
            presetNames[i] = presets[i].name;
        }
        selectedPresetIndex = EditorGUILayout.Popup("Preset", selectedPresetIndex, presetNames);

        EditorGUILayout.Space(10);

        // Display selected preset
        if (selectedPresetIndex >= 0 && selectedPresetIndex < presets.Count)
        {
            LoadoutPreset preset = presets[selectedPresetIndex];

            EditorGUILayout.LabelField($"Preset: {preset.name}", EditorStyles.boldLabel);
            
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Starting Hand:", EditorStyles.boldLabel);
            for (int i = 0; i < preset.startingHand.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                preset.startingHand[i] = EditorGUILayout.TextField($"Card {i + 1}:", preset.startingHand[i]);
                if (GUILayout.Button("X", GUILayout.Width(25)))
                {
                    preset.startingHand.RemoveAt(i);
                    break;
                }
                EditorGUILayout.EndHorizontal();
            }
            if (GUILayout.Button("+ Add Card to Hand"))
            {
                preset.startingHand.Add("");
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Floop Sequence:", EditorStyles.boldLabel);
            for (int i = 0; i < preset.floopSequence.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                preset.floopSequence[i] = EditorGUILayout.TextField($"Floop {i + 1}:", preset.floopSequence[i]);
                if (GUILayout.Button("X", GUILayout.Width(25)))
                {
                    preset.floopSequence.RemoveAt(i);
                    break;
                }
                EditorGUILayout.EndHorizontal();
            }
            if (GUILayout.Button("+ Add Floop Card"))
            {
                preset.floopSequence.Add("");
            }
        }

        EditorGUILayout.Space(20);

        // Manual injection button (for when already in play mode)
        GUI.enabled = Application.isPlaying;
        if (GUILayout.Button("Inject Now (Manual)", GUILayout.Height(30)))
        {
            hasInjected = false;
            InjectLoadout();
            hasInjected = true;
        }
        GUI.enabled = true;

        EditorGUILayout.Space(10);

        // Add new preset
        if (GUILayout.Button("+ Add New Preset"))
        {
            presets.Add(new LoadoutPreset { name = "New Preset " + (presets.Count + 1) });
        }

        EditorGUILayout.EndScrollView();
    }

    private void InjectLoadout()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[LoadoutInjector] Can only inject in Play Mode!");
            return;
        }

        if (selectedPresetIndex < 0 || selectedPresetIndex >= presets.Count)
        {
            Debug.LogError("[LoadoutInjector] Invalid preset selected!");
            return;
        }

        LoadoutPreset preset = presets[selectedPresetIndex];
        Debug.Log($"[LoadoutInjector] Injecting preset: {preset.name}");

        try
        {
            // Get DWGame instance
            DWGame game = DWGame.Instance;
            if (game == null)
            {
                Debug.LogError("[LoadoutInjector] DWGame.Instance is null! Make sure you've started a match.");
                return;
            }

            if (!game.IsSetUp)
            {
                Debug.LogWarning("[LoadoutInjector] Game is not set up yet. Wait for the match to start loading.");
                return;
            }

            // Get User PlayerState
            PlayerState userPlayer = game.CurrentBoardState.GetPlayerState(PlayerType.User);
            if (userPlayer == null)
            {
                Debug.LogError("[LoadoutInjector] User PlayerState is null!");
                return;
            }

            // Inject starting hand
            InjectStartingHand(userPlayer, preset.startingHand);

            // Inject floop sequence
            InjectFloopSequence(userPlayer, preset.floopSequence);

            Debug.Log($"[LoadoutInjector] ✓ Injection complete! Cards: {preset.startingHand.Count} hand, {preset.floopSequence.Count} floops");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[LoadoutInjector] Injection failed: {e.Message}\n{e.StackTrace}");
        }
    }

    private void InjectStartingHand(PlayerState player, List<string> cardIDs)
    {
        if (cardIDs == null || cardIDs.Count == 0) return;

        // Get card order (randomized or sequential)
        List<string> orderedCards = new List<string>(cardIDs);
        if (randomizeHandOrder)
        {
            ShuffleList(orderedCards);
            Debug.Log($"[LoadoutInjector] Randomized hand order: {string.Join(", ", orderedCards)}");
        }

        // Use reflection to access the private TutorialInitialHandOverride field
        // This is what the game uses to force specific starting hands in tutorials
        System.Reflection.FieldInfo tutorialHandField = typeof(PlayerState).GetField(
            "TutorialInitialHandOverride", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
        );

        if (tutorialHandField != null)
        {
            // Set the tutorial override to our custom cards
            tutorialHandField.SetValue(player, orderedCards);
            Debug.Log($"[LoadoutInjector] Set TutorialInitialHandOverride with {orderedCards.Count} cards");
        }
        else
        {
            Debug.LogError("[LoadoutInjector] Failed to find TutorialInitialHandOverride field!");
        }
    }

    private void InjectFloopSequence(PlayerState player, List<string> floopIDs)
    {
        if (floopIDs == null || floopIDs.Count == 0) return;

        // Clear draw queue
        player.DrawQueue.Clear();

        // Add floops to draw queue in order
        foreach (string floopID in floopIDs)
        {
            CardData card = CardDataManager.Instance.GetData(floopID);
            if (card != null)
            {
                player.DrawQueue.Add(card);
                Debug.Log($"[LoadoutInjector] Added to floop queue: {floopID}");
            }
            else
            {
                Debug.LogWarning($"[LoadoutInjector] Floop card not found: {floopID}");
            }
        }
    }

    private void ShuffleList<T>(List<T> list)
    {
        System.Random rng = new System.Random();
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }
}