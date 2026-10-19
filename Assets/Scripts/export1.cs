using UnityEngine;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

// Test method configuration
[System.Serializable]
public class TestMethod {
    public string name;
    public float scrollWaitTime;
    public float perTileWaitTime;
    public float shrinkPercent;
    public float verticalOffsetPercent;
    public string notes;
}

[System.Serializable]
public class ExportLog {
    public string exportTime;
    public string exportPath;
    public int totalTilesExported;
    public string testMode;
    public List<TileExportInfo> tiles = new List<TileExportInfo>();
    public List<string> errors = new List<string>();
    public List<string> warnings = new List<string>();
    public SceneInfo sceneInfo;
}

[System.Serializable]
public class TileExportInfo {
    public string creatureId;
    public string variant;
    public string filename;
    public string methodUsed;
    public string tileName;
    public Vector3Data tilePosition;
    public string tileLayer;
    public int widgetCount;
    public BoundsData bounds;
    public CameraData camera;
    public PixelAnalysis pixelAnalysis;
    public List<string> childrenNames = new List<string>();
    public List<WidgetInfo> widgets = new List<WidgetInfo>();
}

[System.Serializable]
public class SceneInfo {
    public List<LayerInfo> layers = new List<LayerInfo>();
    public string scrollViewFound;
    public string uiPanelFound;
}

[System.Serializable]
public class LayerInfo {
    public int index;
    public string name;
}

[System.Serializable]
public class Vector3Data {
    public float x, y, z;
    public Vector3Data(Vector3 v) { x = v.x; y = v.y; z = v.z; }
}

[System.Serializable]
public class BoundsData {
    public Vector3Data center;
    public Vector3Data size;
    public BoundsData(Bounds b) { 
        center = new Vector3Data(b.center); 
        size = new Vector3Data(b.size); 
    }
}

[System.Serializable]
public class CameraData {
    public Vector3Data position;
    public float orthographicSize;
    public int cullingMask;
    public CameraData(Camera cam) {
        position = new Vector3Data(cam.transform.position);
        orthographicSize = cam.orthographicSize;
        cullingMask = cam.cullingMask;
    }
}

[System.Serializable]
public class PixelAnalysis {
    public int totalPixels;
    public int nonBlackPixels;
    public int nonTransparentPixels;
    public float nonBlackPercent;
    public float nonTransparentPercent;
}

[System.Serializable]
public class WidgetInfo {
    public string name;
    public int depth;
    public bool visible;
    public bool active;
    public Vector3Data corner0;
    public Vector3Data corner2;
}

public class MuseumAutoExporter : MonoBehaviour {

    // ===== TEST MODE CONFIGURATION =====
    public static bool TEST_MODE = false; // Set to false for full export
    public static int TEST_SCROLL_STEPS = 3; // How many scroll steps to test per method
    
    // ===== CROP CONFIGURATION =====
    public static bool USE_TIGHT_CROP = false; // Set true to only capture core elements
    public static float CROP_PADDING = 0.02f; // Padding as percentage (0.02 = 2%)
    
    // ===== SMART SCROLL ADJUSTMENT =====
    public static bool USE_SMART_SCROLL = true; // Auto-detect and fix incomplete rows
    public static float INCOMPLETE_THRESHOLD = 0.15f; // If top/bottom 15% is mostly empty, tile is incomplete
    public static bool USE_GRID_ALIGNED_SCROLL = true; // Scroll by exact row height instead of fixed percentage
    public static int ROWS_PER_SCROLL = 3; // How many complete rows to scroll each step (skip partial rows)
    
    // Fine-tune which elements to include (only used when USE_TIGHT_CROP = true)
    public static bool INCLUDE_RARITY_STARS = true; // Stars are inside frame, keep them
    public static bool INCLUDE_RARITY_FRAME = true; // Decorative rarity borders
    public static bool INCLUDE_LEVEL_LABEL = true; // Level number at bottom
    
    // Define your test methods here
    public static TestMethod[] testMethods = new TestMethod[] {
        new TestMethod {
            name = "Method1_Default",
            scrollWaitTime = 0.5f,
            perTileWaitTime = 0.1f,
            shrinkPercent = 0.02f,
            verticalOffsetPercent = 0.02f,
            notes = "Baseline settings - minimal adjustments"
        },
        new TestMethod {
            name = "Method2_MoreWait",
            scrollWaitTime = 1.0f,
            perTileWaitTime = 0.2f,
            shrinkPercent = 0.04f,
            verticalOffsetPercent = 0.035f,
            notes = "More wait time for texture loading, more upward shift"
        },
        new TestMethod {
            name = "Method3_MaxWait",
            scrollWaitTime = 1.2f,
            perTileWaitTime = 0.25f,
            shrinkPercent = 0.04f,
            verticalOffsetPercent = 0.04f,
            notes = "Maximum wait times to ensure full loading"
        }
    };

    public static void StartExport() {
        GameObject exporterObj = new GameObject("MuseumAutoExporter");
        exporterObj.AddComponent<MuseumAutoExporter>();
    }

    private HashSet<string> exported = new HashSet<string>();
    private UIScrollView scrollView;
    private UIPanel rootPanel;
    private Camera exportCamera;
    private ExportLog exportLog = new ExportLog();
    private TestMethod currentMethod;

    void Start() {
        Debug.Log("[Exporter] ===== STARTING EXPORT =====");
        if (TEST_MODE) {
            Debug.Log($"[Exporter] TEST MODE ENABLED - Will test {testMethods.Length} methods with {TEST_SCROLL_STEPS} scroll steps each");
        }
        
        exportLog.exportTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        exportLog.testMode = TEST_MODE ? $"Testing {testMethods.Length} methods" : "Full Export";
        exportLog.sceneInfo = new SceneInfo();
        
        GameObject scrollViewObj = GameObject.Find("ScrollView_Collection");
        if (scrollViewObj != null) {
            Debug.Log($"[Exporter] Found ScrollView object: {scrollViewObj.name}");
            scrollView = scrollViewObj.GetComponent<UIScrollView>();
            rootPanel = scrollViewObj.GetComponent<UIPanel>();
            
            exportLog.sceneInfo.scrollViewFound = scrollView != null ? "Yes" : "No";
            exportLog.sceneInfo.uiPanelFound = rootPanel != null ? "Yes" : "No";
            
            Debug.Log($"[Exporter] ScrollView component: {(scrollView != null ? "Found" : "NOT FOUND")}");
            Debug.Log($"[Exporter] UIPanel component: {(rootPanel != null ? "Found" : "NOT FOUND")}");
        } else {
            Debug.LogError("[Exporter] ScrollView_Collection GameObject not found!");
            exportLog.errors.Add("ScrollView_Collection GameObject not found");
        }

        if (scrollView == null || rootPanel == null) {
            Debug.LogError("[Exporter] Could not find ScrollView or UIPanel!");
            exportLog.errors.Add("Could not find ScrollView or UIPanel");
            SaveLogAndDestroy();
            return;
        }

        // Log all layers
        Debug.Log("[Exporter] === LAYER INFORMATION ===");
        for (int i = 0; i < 32; i++) {
            string layerName = LayerMask.LayerToName(i);
            if (!string.IsNullOrEmpty(layerName)) {
                Debug.Log($"[Exporter] Layer {i}: {layerName}");
                exportLog.sceneInfo.layers.Add(new LayerInfo { index = i, name = layerName });
            }
        }

        // Create dedicated export camera
        GameObject camObj = new GameObject("ExportCamera");
        exportCamera = camObj.AddComponent<Camera>();
        exportCamera.orthographic = true;
        exportCamera.clearFlags = CameraClearFlags.SolidColor;
        exportCamera.backgroundColor = new Color(0, 0, 0, 0);
        
        int guiLayer = LayerMask.NameToLayer("GUI");
        exportCamera.cullingMask = 1 << guiLayer;
        
        exportCamera.enabled = false;
        exportCamera.nearClipPlane = 0.1f;
        exportCamera.farClipPlane = 100f;
        
        Debug.Log($"[Exporter] Export camera created: {exportCamera.name}");
        Debug.Log($"[Exporter] Camera culling mask: {exportCamera.cullingMask}");
        
        StartCoroutine(AutoExport());
    }

    IEnumerator AutoExport() {
        string baseDir = Path.Combine(Application.persistentDataPath, "MuseumExports");
        Directory.CreateDirectory(baseDir);

        if (TEST_MODE) {
            // TEST MODE: Run each method on a few scroll steps
            for (int methodIndex = 0; methodIndex < testMethods.Length; methodIndex++) {
                currentMethod = testMethods[methodIndex];
                
                string methodDir = Path.Combine(baseDir, currentMethod.name);
                Directory.CreateDirectory(methodDir);
                exportLog.exportPath = methodDir;
                
                Debug.Log($"[Exporter] ===== TESTING METHOD: {currentMethod.name} =====");
                Debug.Log($"[Exporter] Settings: Scroll={currentMethod.scrollWaitTime}s, PerTile={currentMethod.perTileWaitTime}s, Shrink={currentMethod.shrinkPercent*100}%, VOffset={currentMethod.verticalOffsetPercent*100}%");
                Debug.Log($"[Exporter] Notes: {currentMethod.notes}");
                Debug.Log($"[Exporter] Output: {methodDir}");
                
                // Reset for each method
                exported.Clear();
                scrollView.SetDragAmount(0f, 1f, false);
                yield return new WaitForSeconds(0.5f);
                
                float scrollPos = 1f;
                for (int step = 0; step < TEST_SCROLL_STEPS; step++) {
                    scrollView.SetDragAmount(0f, scrollPos, false);
                    scrollView.UpdateScrollbars(true);
                    
                    yield return new WaitForSeconds(currentMethod.scrollWaitTime);
                    yield return null;
                    yield return null;
                    yield return null;
                    
                    yield return StartCoroutine(ProcessCurrentBatch(methodDir, step));
                    
                    scrollPos -= 0.12f;
                }
                
                Debug.Log($"[Exporter] Method {currentMethod.name} complete: {exported.Count} tiles exported");
            }
            
            exportLog.totalTilesExported = exportLog.tiles.Count;
            Debug.Log($"[Exporter] ===== ALL TESTS COMPLETE ===== Total: {exportLog.totalTilesExported} tiles");
            
        } else {
            // FULL EXPORT MODE
            currentMethod = testMethods[0]; // Use first method as default
            string outDir = Path.Combine(baseDir, "FullExport");
            Directory.CreateDirectory(outDir);
            exportLog.exportPath = outDir;

            Debug.Log($"[Exporter] Full export to: {outDir}");

            scrollView.SetDragAmount(0f, 1f, false);
            yield return new WaitForSeconds(0.5f);

            float scrollPos = 1f;
            int stepCount = 0;
            float scrollStep = 0.12f; // Default step

            // Calculate grid-aligned scroll step if enabled
            if (USE_GRID_ALIGNED_SCROLL) {
                scrollStep = CalculateGridAlignedScrollStep();
                Debug.Log($"[Exporter] Using grid-aligned scroll step: {scrollStep:F4}");
            }

            while (scrollPos >= -0.1f) {
                scrollView.SetDragAmount(0f, scrollPos, false);
                scrollView.UpdateScrollbars(true);
                
                yield return new WaitForSeconds(currentMethod.scrollWaitTime);
                yield return null;
                yield return null;
                yield return null;
                
                yield return StartCoroutine(ProcessCurrentBatch(outDir, stepCount));
                
                scrollPos -= scrollStep;
                stepCount++;
            }

            exportLog.totalTilesExported = exported.Count;
            Debug.Log($"[Exporter] === COMPLETE === Exported {exported.Count} unique tiles");
        }
        
        SaveLogAndDestroy();
    }

    void SaveLogAndDestroy() {
        string logPath = Path.Combine(exportLog.exportPath ?? Application.persistentDataPath, "export_log.json");
        string json = JsonUtility.ToJson(exportLog, true);
        File.WriteAllText(logPath, json);
        Debug.Log($"[Exporter] Export log saved to: {logPath}");
        
        if (exportCamera != null) Destroy(exportCamera.gameObject);
        
        #if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
        #endif
        
        Destroy(gameObject);
    }

    IEnumerator ProcessCurrentBatch(string outDir, int stepNum) {
        GameObject[] overlays = new GameObject[] {
            GameObject.Find("UI_All_TileTouchBlackout"),
            GameObject.Find("CreatureBookUI"),
            GameObject.Find("TopBar"),
            GameObject.Find("BottomBar")
        };
        
        List<GameObject> hiddenOverlays = new List<GameObject>();
        foreach (var overlay in overlays) {
            if (overlay != null && overlay.activeInHierarchy) {
                overlay.SetActive(false);
                hiddenOverlays.Add(overlay);
            }
        }
        
        yield return null;
        
        var tileObjects = GameObject.FindObjectsOfType<GameObject>()
            .Where(go => go.name.Contains("UI_All_InventoryTile(Clone)") && go.activeInHierarchy)
            .ToArray();

        Debug.Log($"[Exporter] Step {stepNum}: Found {tileObjects.Length} visible tiles");

        // Smart scroll: Check if we have incomplete tiles and need to adjust
        if (USE_SMART_SCROLL && tileObjects.Length > 0) {
            // Use a wrapper class to get the bool result from the coroutine
            var incompleteCheck = new BoolResult();
            yield return StartCoroutine(CheckForIncompleteTiles(tileObjects, incompleteCheck));
            
            if (incompleteCheck.value) {
                Debug.Log($"[Exporter] ⚠ Incomplete tiles detected! Adjusting scroll position...");
                
                // Adjust scroll slightly and retry
                float currentScroll = scrollView.verticalScrollBar != null ? 
                    scrollView.verticalScrollBar.value : 0f;
                
                scrollView.SetDragAmount(0f, currentScroll + 0.03f, false); // Move up 3%
                scrollView.UpdateScrollbars(true);
                
                yield return new WaitForSeconds(currentMethod.scrollWaitTime);
                yield return null;
                yield return null;
                
                // Re-fetch tiles after adjustment
                tileObjects = GameObject.FindObjectsOfType<GameObject>()
                    .Where(go => go.name.Contains("UI_All_InventoryTile(Clone)") && go.activeInHierarchy)
                    .ToArray();
                
                Debug.Log($"[Exporter] After adjustment: Found {tileObjects.Length} tiles");
            }
        }

        foreach (var tileObj in tileObjects) {
            
            TileExportInfo tileInfo = new TileExportInfo();
            tileInfo.tileName = tileObj.name;
            tileInfo.methodUsed = currentMethod.name;
            tileInfo.tilePosition = new Vector3Data(tileObj.transform.position);
            tileInfo.tileLayer = LayerMask.LayerToName(tileObj.layer);
            
            InventoryTile tile = tileObj.GetComponentInChildren<InventoryTile>();
            if (tile == null) {
                string warning = $"No InventoryTile component found in {tileObj.name}";
                Debug.LogWarning($"[Exporter] {warning}");
                exportLog.warnings.Add(warning);
                continue;
            }
            
            if (tile.InventoryItem == null) {
                string warning = $"InventoryTile has no InventoryItem in {tileObj.name}";
                Debug.LogWarning($"[Exporter] {warning}");
                exportLog.warnings.Add(warning);
                continue;
            }

            CreatureData creature = GetCreatureFromTile(tile);
            if (creature == null) {
                string warning = $"Could not get CreatureData from tile {tileObj.name}";
                Debug.LogWarning($"[Exporter] {warning}");
                exportLog.warnings.Add(warning);
                continue;
            }

            tileInfo.creatureId = creature.ID;
            Debug.Log($"[Exporter] Creature ID: {creature.ID}");
            
            string variant = GetVariantName(creature);
            tileInfo.variant = variant;
            string uniqueKey = $"{creature.ID}_{variant}";

            if (exported.Contains(uniqueKey)) {
                Debug.Log($"[Exporter] Already exported: {uniqueKey}");
                continue;
            }

            // CRITICAL FIX: Freeze BEFORE checking stability!
            LogHierarchy(tileObj.transform, 0, tileInfo);
            FreezeTileState(tileObj);
            
            // Give it a moment to settle after freezing
            yield return new WaitForSeconds(currentMethod.perTileWaitTime);
            yield return null;
            yield return null;
            
            // NOW check if tile is stable and ready
            if (!IsTileStableAndReady(tileObj)) {
                Debug.Log($"[Exporter] Skipping unstable/fading tile: {tileObj.name}");
                continue;
            }

            // Add method name to filename in test mode
            string filename = TEST_MODE 
                ? $"{currentMethod.name}_{creature.ID}_{variant}.png"
                : $"{creature.ID}_{variant}.png";
            tileInfo.filename = filename;
            string outPath = Path.Combine(outDir, filename);

            yield return StartCoroutine(CaptureTileClean(tileObj, outPath, tileInfo));
            
            exportLog.tiles.Add(tileInfo);
            exported.Add(uniqueKey);
            
            yield return null;
        }
        
        foreach (var overlay in hiddenOverlays) {
            if (overlay != null) overlay.SetActive(true);
        }
    }

    void LogHierarchy(Transform t, int depth, TileExportInfo tileInfo) {
        string indent = new string(' ', depth * 2);
        string components = "";
        Component[] comps = t.GetComponents<Component>();
        foreach (var c in comps) {
            if (c != null) components += c.GetType().Name + " ";
        }
        
        if (depth == 0) {
            foreach (Transform child in t) {
                tileInfo.childrenNames.Add(child.name);
            }
        }
        
        foreach (Transform child in t) {
            LogHierarchy(child, depth + 1, tileInfo);
        }
    }

    void FreezeTileState(GameObject tileObj) {
        UITweener[] tweeners = tileObj.GetComponentsInChildren<UITweener>(true);
        foreach (var tweener in tweeners) {
            tweener.enabled = false;
        }
        
        UITexture[] textures = tileObj.GetComponentsInChildren<UITexture>(true);
        foreach (var tex in textures) {
            if (tex.mainTexture != null && tex.gameObject.activeInHierarchy) {
                tex.MarkAsChanged();
            }
        }
        
        UISprite[] sprites = tileObj.GetComponentsInChildren<UISprite>(true);
        foreach (var sprite in sprites) {
            if (sprite.gameObject.activeInHierarchy) {
                sprite.MarkAsChanged();
            }
        }

        Transform shineFX = FindChildRecursive(tileObj.transform, "Shine");
        if (shineFX != null) shineFX.gameObject.SetActive(false);
        
        Transform sparklesFX = FindChildRecursive(tileObj.transform, "SparklesFX");
        if (sparklesFX != null) sparklesFX.gameObject.SetActive(false);
        
        Transform newTileParent = FindChildRecursive(tileObj.transform, "NewTileParent");
        if (newTileParent != null) newTileParent.gameObject.SetActive(false);
        
        Transform fade = FindChildRecursive(tileObj.transform, "Fade");
        if (fade != null) fade.gameObject.SetActive(false);
    }

    IEnumerator CaptureTileClean(GameObject tileObj, string path, TileExportInfo tileInfo) {
        Debug.Log($"[Exporter] === CAPTURE START: {Path.GetFileName(path)} ===");
        
        yield return new WaitForEndOfFrame();

        Transform portraitParent = FindChildRecursive(tileObj.transform, "PortraitParent");
        if (portraitParent == null) {
            string error = $"Could not find PortraitParent in {tileObj.name}";
            Debug.LogError($"[Exporter] {error}");
            exportLog.errors.Add(error);
            yield break;
        }

        Transform flyInAnimRoot = FindChildRecursive(portraitParent, "FlyInAnimRoot");
        if (flyInAnimRoot == null) {
            Debug.LogWarning($"[Exporter] No FlyInAnimRoot found, using PortraitParent");
            flyInAnimRoot = portraitParent;
        }

        Debug.Log($"[Exporter] Using container: {flyInAnimRoot.name}");

        UIWidget[] allWidgets = flyInAnimRoot.GetComponentsInChildren<UIWidget>(true);
        List<UIWidget> visibleWidgets = new List<UIWidget>();
        
        // Build list of widgets to include based on crop mode
        foreach (var widget in allWidgets) {
            if (widget.gameObject.activeInHierarchy && widget.isVisible) {
                string wName = widget.name.ToLower();
                
                // Always exclude these
                if (wName.Contains("inuse") || 
                    wName.Contains("favorite") || 
                    wName.Contains("helper") || 
                    wName.Contains("expedition") ||
                    wName.Contains("buybg") ||
                    wName.Contains("buyinventory")) {
                    continue;
                }
                
                // CRITICAL FIX: Exclude flame effects - they offset the bounds!
                if (wName.Contains("flame")) {
                    Debug.Log($"[Exporter]   ✗ Excluding flame effect: {widget.name}");
                    continue;
                }
                
                // If tight crop mode, only include core elements
                if (USE_TIGHT_CROP) {
                    bool isCore = false;
                    
                    // Main frame (but not decorative frames)
                    if (wName.Contains("frame") && !wName.Contains("spin")) {
                        isCore = true;
                    }
                    
                    // Portrait icon
                    if (wName.Contains("icon_portrait") || wName.Contains("icon")) {
                        isCore = true;
                    }
                    
                    // Level label (configurable)
                    if (INCLUDE_LEVEL_LABEL && wName.Contains("val_creaturelevel")) {
                        isCore = true;
                    }
                    
                    // Rarity stars (configurable)
                    if (INCLUDE_RARITY_STARS && wName.Contains("raritystar")) {
                        isCore = true;
                    }
                    
                    // Rarity decorative frame (configurable)
                    if (INCLUDE_RARITY_FRAME && wName.Contains("rarityframe")) {
                        isCore = true;
                    }
                    
                    // Background
                    if (wName == "background" && !wName.Contains("buy")) {
                        isCore = true;
                    }
                    
                    // FlyInAnimRoot itself
                    if (wName == "flyinanimroot") {
                        isCore = true;
                    }
                    
                    if (isCore) {
                        visibleWidgets.Add(widget);
                        Debug.Log($"[Exporter]   ✓ Including: {widget.name}");
                    } else {
                        Debug.Log($"[Exporter]   ✗ Excluding: {widget.name}");
                    }
                } else {
                    // Include everything except excluded items and flames
                    visibleWidgets.Add(widget);
                }
            }
        }
        
        tileInfo.widgetCount = visibleWidgets.Count;
        Debug.Log($"[Exporter] Found {visibleWidgets.Count} visible widgets (tight crop: {USE_TIGHT_CROP})");
        
        if (visibleWidgets.Count == 0) {
            string error = $"No visible widgets found in {tileObj.name}";
            Debug.LogError($"[Exporter] {error}");
            exportLog.errors.Add(error);
            yield break;
        }

        Bounds tileBounds = new Bounds();
        bool initialized = false;
        
        foreach (var widget in visibleWidgets) {
            Vector3[] corners = widget.worldCorners;
            
            WidgetInfo wInfo = new WidgetInfo {
                name = widget.name,
                depth = widget.depth,
                visible = widget.isVisible,
                active = widget.gameObject.activeInHierarchy,
                corner0 = new Vector3Data(corners[0]),
                corner2 = new Vector3Data(corners[2])
            };
            tileInfo.widgets.Add(wInfo);
            
            foreach (var corner in corners) {
                if (!initialized) {
                    tileBounds = new Bounds(corner, Vector3.zero);
                    initialized = true;
                } else {
                    tileBounds.Encapsulate(corner);
                }
            }
        }
        
        Vector3 originalCenter = tileBounds.center;
        Vector3 originalSize = tileBounds.size;
        
        // Apply padding based on CROP_PADDING setting
        // Add MORE vertical padding to avoid cutting bottom
        Vector3 padding = new Vector3(
            tileBounds.size.x * CROP_PADDING,
            tileBounds.size.y * (CROP_PADDING + 0.03f), // Extra 3% vertical padding
            0f
        );
        tileBounds.size += padding * 2f; // Add padding on all sides
        
        // NO vertical offset - keep it centered
        // (The method's verticalOffsetPercent is removed to fix centering)
        
        tileInfo.bounds = new BoundsData(tileBounds);
        
        Debug.Log($"[Exporter] Method: {currentMethod.name}");
        Debug.Log($"[Exporter] Tight crop: {USE_TIGHT_CROP}, Padding: {CROP_PADDING * 100}%");
        Debug.Log($"[Exporter] Original bounds: center={originalCenter.x:F4},{originalCenter.y:F4} size={originalSize.x:F4},{originalSize.y:F4}");
        Debug.Log($"[Exporter] Final bounds: center={tileBounds.center.x:F4},{tileBounds.center.y:F4} size={tileBounds.size.x:F4},{tileBounds.size.y:F4}");
        
        if (tileBounds.size.x <= 0 || tileBounds.size.y <= 0) {
            string error = $"Invalid bounds for {tileObj.name}";
            Debug.LogError($"[Exporter] {error}");
            exportLog.errors.Add(error);
            yield break;
        }

        Vector3 center = tileBounds.center;
        float maxDimension = Mathf.Max(tileBounds.size.x, tileBounds.size.y);
        
        Vector3 camPos = new Vector3(center.x, center.y, center.z - 10f);
        exportCamera.transform.position = camPos;
        exportCamera.transform.rotation = Quaternion.identity;
        exportCamera.orthographicSize = maxDimension / 2f;
        exportCamera.aspect = 1f;

        tileInfo.camera = new CameraData(exportCamera);

        Debug.Log($"[Exporter] Camera positioned at: {camPos}");
        Debug.Log($"[Exporter] Camera orthographic size: {exportCamera.orthographicSize}");

        int resolution = 512;
        RenderTexture rt = RenderTexture.GetTemporary(resolution, resolution, 24, RenderTextureFormat.ARGB32);
        rt.antiAliasing = 4;
        
        exportCamera.targetTexture = rt;
        
        Debug.Log($"[Exporter] Rendering to {resolution}x{resolution} texture...");
        exportCamera.Render();
        Debug.Log($"[Exporter] Render complete");

        RenderTexture.active = rt;
        Texture2D capture = new Texture2D(resolution, resolution, TextureFormat.ARGB32, false);
        capture.ReadPixels(new Rect(0, 0, resolution, resolution), 0, 0);
        capture.Apply();

        Color[] pixels = capture.GetPixels();
        int nonBlackPixels = 0;
        int nonTransparentPixels = 0;
        for (int i = 0; i < pixels.Length; i++) {
            if (pixels[i].r > 0.01f || pixels[i].g > 0.01f || pixels[i].b > 0.01f) {
                nonBlackPixels++;
            }
            if (pixels[i].a > 0.01f) {
                nonTransparentPixels++;
            }
        }
        
        tileInfo.pixelAnalysis = new PixelAnalysis {
            totalPixels = pixels.Length,
            nonBlackPixels = nonBlackPixels,
            nonTransparentPixels = nonTransparentPixels,
            nonBlackPercent = (float)nonBlackPixels / pixels.Length * 100f,
            nonTransparentPercent = (float)nonTransparentPixels / pixels.Length * 100f
        };
        
        Debug.Log($"[Exporter] Pixel analysis: {nonBlackPixels} non-black ({tileInfo.pixelAnalysis.nonBlackPercent:F2}%), {nonTransparentPixels} non-transparent ({tileInfo.pixelAnalysis.nonTransparentPercent:F2}%)");

        File.WriteAllBytes(path, capture.EncodeToPNG());
        Debug.Log($"[Exporter] ✓ Saved: {Path.GetFileName(path)} at {path}");

        // Cleanup
        RenderTexture.active = null;
        exportCamera.targetTexture = null;
        RenderTexture.ReleaseTemporary(rt);
        Destroy(capture);
        
        Debug.Log($"[Exporter] === CAPTURE END ===");
    }

    // Check if tile is stable and ready for capture (after freezing)
    bool IsTileStableAndReady(GameObject tileObj) {
        Debug.Log($"[Exporter] === Checking tile stability: {tileObj.name} ===");
        
        // 1. Check for InventoryTile component
        InventoryTile tile = tileObj.GetComponentInChildren<InventoryTile>();
        if (tile == null || tile.InventoryItem == null) {
            Debug.Log($"[Exporter] ❌ REJECT: No InventoryTile or InventoryItem");
            return false;
        }
        Debug.Log($"[Exporter] ✓ InventoryTile exists with item");

        // 2. Find PortraitParent
        Transform portraitParent = FindChildRecursive(tileObj.transform, "PortraitParent");
        if (portraitParent == null) {
            Debug.Log($"[Exporter] ❌ REJECT: PortraitParent missing");
            return false;
        }
        Debug.Log($"[Exporter] ✓ PortraitParent found");

        // 3. Check if any VISIBLE widgets still have low alpha (shouldn't happen after freeze, but check anyway)
        UIWidget[] widgets = portraitParent.GetComponentsInChildren<UIWidget>(false); // Only active
        int visibleCount = 0;
        foreach (var widget in widgets) {
            if (widget.isVisible) {
                visibleCount++;
                // Skip shadow widgets - they're often partially transparent by design
                if (!widget.name.ToLower().Contains("shadow") && widget.alpha < 0.5f) {
                    Debug.Log($"[Exporter] ❌ REJECT: Widget '{widget.name}' has low alpha={widget.alpha:F2}");
                    return false;
                }
            }
        }
        Debug.Log($"[Exporter] ✓ Checked {visibleCount} visible widgets (shadows excluded)");

        // 4. Verify portrait texture/sprite exists
        Transform iconPortrait = FindChildRecursive(portraitParent, "Icon_Portrait");
        if (iconPortrait != null) {
            UITexture tex = iconPortrait.GetComponent<UITexture>();
            UISprite sprite = iconPortrait.GetComponent<UISprite>();
            
            if (tex != null && tex.mainTexture == null) {
                Debug.Log($"[Exporter] ❌ REJECT: Portrait texture not loaded");
                return false;
            }
            if (sprite != null && sprite.atlas == null) {
                Debug.Log($"[Exporter] ❌ REJECT: Portrait sprite atlas missing");
                return false;
            }
            Debug.Log($"[Exporter] ✓ Portrait visual loaded (texture or sprite)");
        }

        Debug.Log($"[Exporter] ✅ ACCEPT: Tile is stable and ready!");
        return true;
    }

    Transform FindChildRecursive(Transform parent, string name) {
        if (parent.name == name) return parent;
        
        foreach (Transform child in parent) {
            Transform found = FindChildRecursive(child, name);
            if (found != null) return found;
        }
        
        return null;
    }

    CreatureData GetCreatureFromTile(InventoryTile tile) {
        if (tile.InventoryItem.SlotType == InventorySlotType.Creature) {
            return tile.InventoryItem.Creature.Form;
        } else if (tile.InventoryItem.SlotType == InventorySlotType.DisplayCreature) {
            return tile.InventoryItem.DisplayCreature;
        }
        return null;
    }

    string GetVariantName(CreatureData creature) {
        string id = creature.ID.ToLower();
        if (id.Contains("awaken")) return "awk";
        if (id.Contains("special")) return "special";
        if (creature.Faction == CreatureFaction.Colorless) return "special";
        return "base";
    }

    void OnDestroy() {
        if (exportCamera != null) {
            Destroy(exportCamera.gameObject);
        }
    }

    // Calculate the exact scroll step needed to move by N rows of tiles
    float CalculateGridAlignedScrollStep() {
        Debug.Log("[Exporter] ===== DETECTING GRID LAYOUT =====");
        
        // Find the UIGrid component that arranges the tiles
        GameObject uiGridObj = GameObject.Find("UIGrid");
        if (uiGridObj == null) {
            Debug.LogWarning("[Exporter] ❌ UIGrid GameObject not found!");
            Debug.LogWarning("[Exporter] Searching for all GameObjects with 'Grid' in name...");
            
            GameObject[] allObjects = FindObjectsOfType<GameObject>();
            foreach (var obj in allObjects) {
                if (obj.name.ToLower().Contains("grid")) {
                    Debug.Log($"[Exporter] Found: {obj.name}");
                }
            }
            
            return 0.12f;
        }

        Debug.Log($"[Exporter] ✓ Found UIGrid GameObject: {uiGridObj.name}");

        // Check for UIStreamingGrid (custom NGUI component)
        Component streamingGrid = uiGridObj.GetComponent("UIStreamingGrid");
        if (streamingGrid != null) {
            Debug.Log($"[Exporter] ✓ Found UIStreamingGrid component (custom grid)");
        }

        // Try standard UIGrid
        UIGrid grid = uiGridObj.GetComponent<UIGrid>();
        if (grid == null && streamingGrid == null) {
            Debug.LogWarning("[Exporter] ❌ No grid component found!");
            Debug.LogWarning("[Exporter] Available components:");
            Component[] components = uiGridObj.GetComponents<Component>();
            foreach (var comp in components) {
                if (comp != null) {
                    Debug.Log($"[Exporter]   - {comp.GetType().Name}");
                }
            }
        } else if (grid != null) {
            Debug.Log($"[Exporter] ✓ Found UIGrid component");
            Debug.Log($"[Exporter] Grid arrangement: {grid.arrangement}");
            Debug.Log($"[Exporter] Grid cellWidth: {grid.cellWidth}");
            Debug.Log($"[Exporter] Grid cellHeight: {grid.cellHeight}");
            Debug.Log($"[Exporter] Grid maxPerLine: {grid.maxPerLine}");
        }

        // ALWAYS measure actual tile positions - this is the most reliable method!
        Debug.Log($"[Exporter] ===== VERIFYING WITH ACTUAL TILES =====");
        
        var tileObjects = GameObject.FindObjectsOfType<GameObject>()
            .Where(go => go.name.Contains("UI_All_InventoryTile(Clone)") && go.activeInHierarchy)
            .Take(16) // Sample more tiles to find multiple rows
            .ToArray();

        Debug.Log($"[Exporter] Sampling {tileObjects.Length} visible tiles:");
        
        if (tileObjects.Length >= 2) {
            for (int i = 0; i < tileObjects.Length; i++) {
                Vector3 pos = tileObjects[i].transform.position;
                Debug.Log($"[Exporter]   Tile {i}: position=({pos.x:F3}, {pos.y:F3}, {pos.z:F3})");
            }

            // Calculate actual spacing between tiles
            float[] yPositions = tileObjects.Select(t => t.transform.position.y).OrderByDescending(y => y).ToArray();
            
            Debug.Log($"[Exporter] Unique Y positions found: {yPositions.Distinct().Count()}");
            
            // Find the first vertical gap (different Y positions = different rows)
            float? measuredRowHeight = null;
            for (int i = 0; i < yPositions.Length - 1; i++) {
                float gap = Mathf.Abs(yPositions[i] - yPositions[i + 1]);
                if (gap > 0.01f) { // Significant difference = different row
                    measuredRowHeight = gap;
                    Debug.Log($"[Exporter] 📏 MEASURED row height from tiles: {gap:F4}");
                    break;
                }
            }

            if (measuredRowHeight.HasValue && rootPanel != null) {
                float cellHeight = measuredRowHeight.Value;
                
                // Get visible height of scroll view
                Vector4 clipRegion = rootPanel.finalClipRegion;
                
                // CRITICAL: clipRegion is in PIXEL coordinates, need to convert to world units
                // The panel's height in world units can be calculated from its transform scale
                float pixelHeight = clipRegion.w;
                
                // Convert pixel height to world height using the actual measured tile positions
                // We know the measured row height in world units, and we can count visible rows directly
                int visibleRows = yPositions.Distinct().Count();
                float visibleHeightWorld = cellHeight * visibleRows;
                
                Debug.Log($"[Exporter] 📊 Pixel height from clipRegion: {pixelHeight:F0}");
                Debug.Log($"[Exporter] 📊 Visible rows counted: {visibleRows}");
                Debug.Log($"[Exporter] 📊 Visible height (world units): {visibleHeightWorld:F3}");
                Debug.Log($"[Exporter] 📊 Row height (world units): {cellHeight:F3}");
                
                // Calculate scroll step as a fraction of total scrollable content
                // Since we want to move N complete rows, and we can see ~visibleRows
                float scrollFraction = (float)ROWS_PER_SCROLL / (float)visibleRows;
                
                Debug.Log($"[Exporter] ✅ FINAL: Scrolling by {ROWS_PER_SCROLL} rows out of {visibleRows} visible = {scrollFraction:F4} scroll units");
                Debug.Log($"[Exporter] ========================================");
                
                return Mathf.Clamp(scrollFraction, 0.05f, 0.5f);
            } else {
                Debug.LogWarning($"[Exporter] ❌ Could not measure row height from {tileObjects.Length} tiles");
            }
        } else {
            Debug.LogWarning($"[Exporter] ❌ Not enough tiles visible ({tileObjects.Length}) to measure");
        }

        // Fallback to grid settings if measurement fails
        if (grid != null) {
            float gridCellHeight = grid.cellHeight;
            
            if (rootPanel != null) {
                Vector4 clipRegion = rootPanel.finalClipRegion;
                float visibleHeight = clipRegion.w;
                float totalRowHeight = gridCellHeight * ROWS_PER_SCROLL;
                float scrollStep = totalRowHeight / visibleHeight;
                
                Debug.Log($"[Exporter] ⚠ Using grid.cellHeight fallback: {scrollStep:F4}");
                return Mathf.Clamp(scrollStep, 0.05f, 0.5f);
            }
        }

        Debug.LogWarning("[Exporter] ❌ Could not calculate grid-aligned scroll, using default 0.12");
        return 0.12f;
    }

    // Helper class to return bool from coroutine
    class BoolResult {
        public bool value = false;
    }

    // Check if any tiles are incomplete (cut off at top/bottom)
    IEnumerator CheckForIncompleteTiles(GameObject[] tileObjects, BoolResult result) {
        // Sample a few tiles to check for incomplete rendering
        int samplesToCheck = Mathf.Min(3, tileObjects.Length);
        
        for (int i = 0; i < samplesToCheck; i++) {
            GameObject tileObj = tileObjects[i];
            
            // Quick render to check pixel distribution
            var incompleteCheck = new BoolResult();
            yield return StartCoroutine(IsTileIncomplete(tileObj, incompleteCheck));
            
            if (incompleteCheck.value) {
                result.value = true;
                yield break; // Found at least one incomplete tile
            }
        }
        
        result.value = false; // All sampled tiles look complete
    }

    // Analyze a tile's pixel distribution to detect if it's cut off
    IEnumerator IsTileIncomplete(GameObject tileObj, BoolResult result) {
        yield return new WaitForEndOfFrame();
        
        Transform portraitParent = FindChildRecursive(tileObj.transform, "PortraitParent");
        if (portraitParent == null) {
            result.value = false;
            yield break;
        }

        // Get rough bounds for a quick test render
        UIWidget[] widgets = portraitParent.GetComponentsInChildren<UIWidget>(false);
        if (widgets.Length == 0) {
            result.value = false;
            yield break;
        }

        Bounds bounds = new Bounds();
        bool init = false;
        foreach (var w in widgets) {
            if (w.isVisible) {
                foreach (var corner in w.worldCorners) {
                    if (!init) {
                        bounds = new Bounds(corner, Vector3.zero);
                        init = true;
                    } else {
                        bounds.Encapsulate(corner);
                    }
                }
            }
        }

        if (!init) {
            result.value = false;
            yield break;
        }

        // Quick low-res render for analysis
        int testRes = 128;
        Vector3 center = bounds.center;
        float maxDim = Mathf.Max(bounds.size.x, bounds.size.y) * 1.1f;
        
        exportCamera.transform.position = new Vector3(center.x, center.y, center.z - 10f);
        exportCamera.orthographicSize = maxDim / 2f;
        
        RenderTexture rt = RenderTexture.GetTemporary(testRes, testRes, 24);
        exportCamera.targetTexture = rt;
        exportCamera.Render();
        
        RenderTexture.active = rt;
        Texture2D testCapture = new Texture2D(testRes, testRes, TextureFormat.RGB24, false);
        testCapture.ReadPixels(new Rect(0, 0, testRes, testRes), 0, 0);
        testCapture.Apply();
        
        // Analyze pixel distribution
        Color[] pixels = testCapture.GetPixels();
        int stripHeight = Mathf.RoundToInt(testRes * INCOMPLETE_THRESHOLD);
        
        // Check top strip
        int topNonBlack = 0;
        for (int y = testRes - stripHeight; y < testRes; y++) {
            for (int x = 0; x < testRes; x++) {
                Color c = pixels[y * testRes + x];
                if (c.r > 0.1f || c.g > 0.1f || c.b > 0.1f) {
                    topNonBlack++;
                }
            }
        }
        
        // Check bottom strip
        int bottomNonBlack = 0;
        for (int y = 0; y < stripHeight; y++) {
            for (int x = 0; x < testRes; x++) {
                Color c = pixels[y * testRes + x];
                if (c.r > 0.1f || c.g > 0.1f || c.b > 0.1f) {
                    bottomNonBlack++;
                }
            }
        }
        
        // Check middle strip for comparison
        int middleNonBlack = 0;
        int middleStart = (testRes - stripHeight) / 2;
        for (int y = middleStart; y < middleStart + stripHeight; y++) {
            for (int x = 0; x < testRes; x++) {
                Color c = pixels[y * testRes + x];
                if (c.r > 0.1f || c.g > 0.1f || c.b > 0.1f) {
                    middleNonBlack++;
                }
            }
        }
        
        RenderTexture.active = null;
        exportCamera.targetTexture = null;
        RenderTexture.ReleaseTemporary(rt);
        Destroy(testCapture);
        
        int stripPixels = testRes * stripHeight;
        float topPercent = (float)topNonBlack / stripPixels;
        float bottomPercent = (float)bottomNonBlack / stripPixels;
        float middlePercent = (float)middleNonBlack / stripPixels;
        
        Debug.Log($"[Exporter] Tile completeness check: top={topPercent:P0}, middle={middlePercent:P0}, bottom={bottomPercent:P0}");
        
        // If top or bottom has significantly less content than middle, tile is incomplete
        result.value = (topPercent < middlePercent * 0.3f) || (bottomPercent < middlePercent * 0.3f);
    }

    // Calculate the visible bounds of the scroll view
    Bounds CalculateScrollViewBounds() {
        if (rootPanel == null) {
            Debug.LogWarning("[Exporter] No rootPanel, using default bounds");
            return new Bounds(Vector3.zero, new Vector3(10f, 10f, 10f));
        }

        // Get the panel's clipping region in world space
        Vector4 clipRegion = rootPanel.finalClipRegion;
        Vector3 center = rootPanel.transform.TransformPoint(new Vector3(clipRegion.x, clipRegion.y, 0f));
        Vector3 size = new Vector3(clipRegion.z, clipRegion.w, 1f);
        
        // VERY STRICT: Only capture tiles in the CENTER 40% of the scroll view
        // This ensures NO edge tiles with visible neighbors get captured
        size *= 0.4f;
        
        Debug.Log($"[Exporter] Scroll bounds (STRICT): center=({center.x:F3},{center.y:F3}), size=({size.x:F3}x{size.y:F3})");
        
        return new Bounds(center, size);
    }

    // Check if tile is fully within visible bounds
    bool IsTileFullyVisible(GameObject tileObj, Bounds scrollBounds) {
        // Get the tile's portrait parent to get actual visual bounds
        Transform portraitParent = FindChildRecursive(tileObj.transform, "PortraitParent");
        if (portraitParent == null) {
            Debug.LogWarning($"[Exporter] No PortraitParent for bounds check");
            return false;
        }

        // Get the tile's actual visual position (center of PortraitParent)
        Vector3 tileCenter = portraitParent.position;
        
        // Calculate how far from scroll center the tile is
        Vector3 offset = tileCenter - scrollBounds.center;
        
        // Check if tile is within the "safe zone" (middle of scroll view)
        // Use a tighter check to ensure no edge tiles
        bool withinHorizontal = Mathf.Abs(offset.x) < (scrollBounds.size.x / 2f);
        bool withinVertical = Mathf.Abs(offset.y) < (scrollBounds.size.y / 2f);
        
        bool isVisible = withinHorizontal && withinVertical;
        
        Debug.Log($"[Exporter] Tile at ({tileCenter.x:F3},{tileCenter.y:F3}), offset from center: ({offset.x:F3},{offset.y:F3}) - Within bounds: {isVisible}");
        
        return isVisible;
    }
}