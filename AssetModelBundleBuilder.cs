using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class AssetModelBundleBuilder
{
    // ============================================================
    // CONFIG
    // ============================================================

    private const string BundleName =
        "MainModelBundles";

    private const string OutputRoot =
        "AssetBundles";

    // ============================================================
    // MENU
    // ============================================================

    [MenuItem(
        "Tools/AssetBundles/Model/Build MainModelBundles")]
    public static void BuildMainModelBundles()
    {
        Build(BuildTarget.Android);
    }

    [MenuItem(
        "Tools/AssetBundles/Model/Build MainModelBundles/Android")]
    public static void BuildAndroid()
    {
        Build(BuildTarget.Android);
    }

    [MenuItem(
        "Tools/AssetBundles/Model/Build MainModelBundles/Windows")]
    public static void BuildWindows()
    {
        Build(BuildTarget.StandaloneWindows64);
    }

    [MenuItem(
        "Tools/AssetBundles/Model/Build MainModelBundles/iOS")]
    public static void BuildIOS()
    {
        Build(BuildTarget.iOS);
    }

    [MenuItem(
        "Tools/AssetBundles/Model/Clear MainModelBundles")]
    public static void ClearModelBundleNames()
    {
        ClearBundleAssignments();
    }

    // ============================================================
    // BUILD
    // ============================================================

    private static void Build(
        BuildTarget target)
    {
        Debug.Log(
            "=================================================");

        Debug.Log(
            "[AssetModelBundleBuilder] BUILD START");

        Debug.Log(
            "[AssetModelBundleBuilder] Target: " +
            target);

        Debug.Log(
            "[AssetModelBundleBuilder] Bundle: " +
            BundleName);

        Debug.Log(
            "[AssetModelBundleBuilder] Selection: " +
            "Mesh / AnimationClip / AnimatorController");

        Debug.Log(
            "[AssetModelBundleBuilder] Resources: EXCLUDED");

        Debug.Log(
            "=================================================");

        // --------------------------------------------------------
        // CLEAN ONLY OUR OWN BUNDLE ASSIGNMENTS
        // --------------------------------------------------------

        ClearBundleAssignments();

        // --------------------------------------------------------
        // FIND + ASSIGN
        // --------------------------------------------------------

        int assigned =
            AssignModelAssets();

        if (assigned == 0)
        {
            Debug.LogWarning(
                "[AssetModelBundleBuilder] " +
                "No Mesh / AnimationClip / " +
                "AnimatorController assets found.");

            return;
        }

        AssetDatabase.RemoveUnusedAssetBundleNames();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // --------------------------------------------------------
        // OUTPUT
        // --------------------------------------------------------

        string outputPath =
            GetOutputPath(target);

        if (Directory.Exists(outputPath))
        {
            try
            {
                Directory.Delete(
                    outputPath,
                    true);

                Debug.Log(
                    "[AssetModelBundleBuilder] " +
                    "Old output deleted: " +
                    outputPath);
            }
            catch (Exception ex)
            {
                Debug.LogError(
                    "[AssetModelBundleBuilder] " +
                    "Could not delete old output:\n" +
                    outputPath +
                    "\n" +
                    ex.Message);

                return;
            }
        }

        Directory.CreateDirectory(
            outputPath);

        // --------------------------------------------------------
        // BUILD
        // --------------------------------------------------------

        BuildAssetBundleOptions options =
            BuildAssetBundleOptions
                .ChunkBasedCompression;

        AssetBundleManifest manifest =
            BuildPipeline.BuildAssetBundles(
                outputPath,
                options,
                target);

        if (manifest == null)
        {
            Debug.LogError(
                "[AssetModelBundleBuilder] " +
                "BUILD FAILED!");

            return;
        }

        // --------------------------------------------------------
        // CLEAN MANIFESTS
        // --------------------------------------------------------

        DeleteManifestFiles(
            outputPath);

        // --------------------------------------------------------
        // LOG
        // --------------------------------------------------------

        LogBuiltBundles(
            outputPath);

        Debug.Log(
            "=================================================");

        Debug.Log(
            "[AssetModelBundleBuilder] BUILD COMPLETE");

        Debug.Log(
            "[AssetModelBundleBuilder] Assets assigned: " +
            assigned);

        Debug.Log(
            "[AssetModelBundleBuilder] Output: " +
            outputPath);

        Debug.Log(
            "=================================================");
    }

    // ============================================================
    // FIND MODEL ASSETS
    // ============================================================

    private static int AssignModelAssets()
    {
        int assigned = 0;

        HashSet<string> processed =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);

        // --------------------------------------------------------
        // SEARCH ALL ASSETS
        // --------------------------------------------------------
        //
        // IMPORTANT:
        //
        // There is NO extension filter here.
        //
        // .fbx
        // .asset
        // .anim
        // .controller
        // custom importer assets
        // etc.
        //
        // are all accepted if they contain the required
        // Unity asset types.
        //
        // --------------------------------------------------------

        string[] guids =
            AssetDatabase.FindAssets(
                "",
                new[] { "Assets" });

        Debug.Log(
            "[AssetModelBundleBuilder] " +
            "Scanning " +
            guids.Length +
            " assets...");

        foreach (string guid in guids)
        {
            string assetPath =
                AssetDatabase.GUIDToAssetPath(
                    guid);

            if (string.IsNullOrEmpty(
                    assetPath))
            {
                continue;
            }

            if (AssetDatabase.IsValidFolder(
                    assetPath))
            {
                continue;
            }

            if (ShouldIgnore(
                    assetPath))
            {
                continue;
            }

            if (processed.Contains(
                    assetPath))
            {
                continue;
            }

            // ----------------------------------------------------
            // CONTENT BASED SELECTION
            // ----------------------------------------------------

            if (!ContainsModelAsset(
                    assetPath))
            {
                continue;
            }

            // ----------------------------------------------------
            // SAFE ASSIGNMENT
            // ----------------------------------------------------

            if (!TryAssign(
                    assetPath))
            {
                continue;
            }

            processed.Add(
                assetPath);

            assigned++;

            Debug.Log(
                "[AssetModelBundleBuilder] ASSIGNED -> " +
                BundleName +
                " : " +
                assetPath);
        }

        Debug.Log(
            "[AssetModelBundleBuilder] " +
            "Total assigned: " +
            assigned);

        return assigned;
    }

    // ============================================================
    // CONTENT BASED MODEL CHECK
    // ============================================================

    private static bool ContainsModelAsset(
        string assetPath)
    {
        if (string.IsNullOrEmpty(
                assetPath))
        {
            return false;
        }

        if (ShouldIgnore(
                assetPath))
        {
            return false;
        }

        // --------------------------------------------------------
        // IMPORTANT:
        //
        // LoadAllAssetsAtPath is intentional.
        //
        // FBX and other imported files can contain:
        //
        // Main asset
        // Mesh
        // AnimationClip
        // Avatar
        // etc.
        //
        // We inspect ALL sub-assets instead of checking
        // the file extension.
        // --------------------------------------------------------

        UnityEngine.Object[] assets;

        try
        {
            assets =
                AssetDatabase.LoadAllAssetsAtPath(
                    assetPath);
        }
        catch (Exception ex)
        {
            Debug.LogWarning(
                "[AssetModelBundleBuilder] " +
                "Could not inspect asset:\n" +
                assetPath +
                "\n" +
                ex.Message);

            return false;
        }

        if (assets == null ||
            assets.Length == 0)
        {
            return false;
        }

        foreach (UnityEngine.Object asset
                 in assets)
        {
            if (asset == null)
            {
                continue;
            }

            // ----------------------------------------------------
            // MESH
            // ----------------------------------------------------

            if (asset is Mesh)
            {
                return true;
            }

            // ----------------------------------------------------
            // ANIMATION CLIP
            // ----------------------------------------------------

            if (asset is AnimationClip)
            {
                return true;
            }

            // ----------------------------------------------------
            // ANIMATOR CONTROLLER
            // ----------------------------------------------------

            if (asset is RuntimeAnimatorController)
            {
                return true;
            }
        }

        return false;
    }

    // ============================================================
    // SAFE BUNDLE ASSIGNMENT
    // ============================================================

    private static bool TryAssign(
        string assetPath)
    {
        AssetImporter importer =
            AssetImporter.GetAtPath(
                assetPath);

        if (importer == null)
        {
            return false;
        }

        string existingBundle =
            importer.assetBundleName;

        // --------------------------------------------------------
        // ALREADY OUR BUNDLE
        // --------------------------------------------------------

        if (string.Equals(
                existingBundle,
                BundleName,
                StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // --------------------------------------------------------
        // ANOTHER BUNDLE OWNS THIS ASSET
        // --------------------------------------------------------
        //
        // NEVER overwrite it.
        //
        // This prevents:
        //
        // MainResourcesBundle
        // creature bundle
        // character bundle
        // shader bundle
        // etc.
        //
        // from being silently replaced.
        // --------------------------------------------------------

        if (!string.IsNullOrEmpty(
                existingBundle))
        {
            Debug.LogWarning(
                "=================================================");

            Debug.LogWarning(
                "[AssetModelBundleBuilder] " +
                "ASSET ALREADY OWNED");

            Debug.LogWarning(
                "Asset: " +
                assetPath);

            Debug.LogWarning(
                "Existing bundle: " +
                existingBundle);

            Debug.LogWarning(
                "Requested bundle: " +
                BundleName);

            Debug.LogWarning(
                "ACTION: SKIPPED - no overwrite");

            Debug.LogWarning(
                "=================================================");

            return false;
        }

        // --------------------------------------------------------
        // ASSIGN
        // --------------------------------------------------------

        importer.assetBundleName =
            BundleName;

        return true;
    }

    // ============================================================
    // IGNORE RULES
    // ============================================================

    private static bool ShouldIgnore(
        string assetPath)
    {
        if (string.IsNullOrEmpty(
                assetPath))
        {
            return true;
        }

        assetPath =
            assetPath.Replace(
                "\\",
                "/");

        // --------------------------------------------------------
        // RESOURCES
        // --------------------------------------------------------
        //
        // ABSOLUTELY NEVER TOUCH:
        //
        // Assets/Resources/...
        //
        // MainModelBundles is intentionally separate.
        // --------------------------------------------------------

        if (assetPath.Equals(
                "Assets/Resources",
                StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (assetPath.StartsWith(
                "Assets/Resources/",
                StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // --------------------------------------------------------
        // SCRIPTS
        // --------------------------------------------------------

        if (assetPath.EndsWith(
                ".cs",
                StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (assetPath.EndsWith(
                ".js",
                StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (assetPath.EndsWith(
                ".boo",
                StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // --------------------------------------------------------
        // EDITOR
        // --------------------------------------------------------

        if (assetPath.Contains(
                "/Editor/",
                StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (assetPath.EndsWith(
                "/Editor",
                StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // --------------------------------------------------------
        // HIDDEN / META
        // --------------------------------------------------------

        if (assetPath.EndsWith(
                ".meta",
                StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    // ============================================================
    // CLEAR ONLY MAIN MODEL BUNDLE
    // ============================================================

    private static void ClearBundleAssignments()
    {
        string[] assetPaths =
            AssetDatabase.GetAssetPathsFromAssetBundle(
                BundleName);

        if (assetPaths == null ||
            assetPaths.Length == 0)
        {
            AssetDatabase.RemoveUnusedAssetBundleNames();

            AssetDatabase.SaveAssets();

            return;
        }

        int cleared = 0;

        foreach (string assetPath
                 in assetPaths)
        {
            AssetImporter importer =
                AssetImporter.GetAtPath(
                    assetPath);

            if (importer == null)
            {
                continue;
            }

            // ----------------------------------------------------
            // ONLY CLEAR OUR OWN BUNDLE
            // ----------------------------------------------------

            if (string.Equals(
                    importer.assetBundleName,
                    BundleName,
                    StringComparison.OrdinalIgnoreCase))
            {
                importer.assetBundleName =
                    null;

                cleared++;
            }
        }

        AssetDatabase.RemoveUnusedAssetBundleNames();

        AssetDatabase.SaveAssets();

        Debug.Log(
            "[AssetModelBundleBuilder] " +
            "Cleared " +
            cleared +
            " assets from " +
            BundleName);
    }

    // ============================================================
    // OUTPUT PATH
    // ============================================================

    private static string GetOutputPath(
        BuildTarget target)
    {
        string targetFolder;

        switch (target)
        {
            case BuildTarget.Android:
                targetFolder = "Android";
                break;

            case BuildTarget.StandaloneWindows64:
                targetFolder = "Windows";
                break;

            case BuildTarget.iOS:
                targetFolder = "iOS";
                break;

            case BuildTarget.StandaloneLinux64:
                targetFolder = "Linux";
                break;

            default:
                targetFolder =
                    target.ToString();
                break;
        }

        return Path.Combine(
            OutputRoot,
            targetFolder);
    }

    // ============================================================
    // DELETE MANIFEST FILES
    // ============================================================

    private static void DeleteManifestFiles(
        string outputPath)
    {
        if (!Directory.Exists(
                outputPath))
        {
            return;
        }

        string[] manifestFiles =
            Directory.GetFiles(
                outputPath,
                "*.manifest",
                SearchOption.AllDirectories);

        foreach (string file
                 in manifestFiles)
        {
            try
            {
                File.Delete(file);

                Debug.Log(
                    "[AssetModelBundleBuilder] " +
                    "Deleted manifest: " +
                    file);
            }
            catch (Exception ex)
            {
                Debug.LogError(
                    "[AssetModelBundleBuilder] " +
                    "Failed to delete manifest:\n" +
                    file +
                    "\n" +
                    ex.Message);
            }
        }
    }

    // ============================================================
    // LOG BUILT FILES
    // ============================================================

    private static void LogBuiltBundles(
        string outputPath)
    {
        if (!Directory.Exists(
                outputPath))
        {
            return;
        }

        string[] files =
            Directory.GetFiles(
                outputPath,
                "*",
                SearchOption.AllDirectories);

        Debug.Log(
            "[AssetModelBundleBuilder] " +
            "Built files:");

        foreach (string file
                 in files)
        {
            string relative =
                file.Substring(
                    outputPath.Length)
                .TrimStart(
                    Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar);

            Debug.Log(
                "    -> " +
                relative);
        }
    }
}