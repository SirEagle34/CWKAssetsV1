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
    public static void ClearMainModelBundles()
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
            "Mesh / AnimationClip / RuntimeAnimatorController");

        Debug.Log(
            "[AssetModelBundleBuilder] Extension filter: NONE");

        Debug.Log(
            "[AssetModelBundleBuilder] Resources: EXCLUDED");

        Debug.Log(
            "=================================================");

        // --------------------------------------------------------
        // IMPORTANT
        //
        // ONLY remove assignments that belong to MainModelBundles.
        //
        // Never touch creature / character / environment / etc.
        // --------------------------------------------------------

        ClearBundleAssignments();

        // --------------------------------------------------------
        // FIND + ASSIGN
        // --------------------------------------------------------

        int assigned =
            AssignModelAssets();

        if (assigned <= 0)
        {
            Debug.LogWarning(
                "[AssetModelBundleBuilder] " +
                "No Mesh / AnimationClip / " +
                "RuntimeAnimatorController assets found.");

            return;
        }

        AssetDatabase.RemoveUnusedAssetBundleNames();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Build directly from the assets assigned by this builder.
        // Do not depend on Unity's GetAssetPathsFromAssetBundle()
        // cache being immediately refreshed.

        List<string> validAssets =
            new List<string>();

        HashSet<string> uniqueAssets =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);

        string[] assignedGuids =
            AssetDatabase.FindAssets(
                "",
                new[] { "Assets" });

        foreach (string guid in assignedGuids)
        {
            string assetPath =
                AssetDatabase.GUIDToAssetPath(guid);

            if (string.IsNullOrEmpty(assetPath))
            {
                continue;
            }

            assetPath =
                NormalizePath(assetPath);

            if (AssetDatabase.IsValidFolder(assetPath) ||
                ShouldIgnore(assetPath))
            {
                continue;
            }

            AssetImporter importer =
                AssetImporter.GetAtPath(assetPath);

            if (importer == null ||
                !string.Equals(
                    importer.assetBundleName,
                    BundleName,
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!ContainsModelAsset(assetPath))
            {
                continue;
            }

            if (uniqueAssets.Add(assetPath))
            {
                validAssets.Add(assetPath);
            }
        }

        if (validAssets.Count == 0)
        {
            Debug.LogError(
                "[AssetModelBundleBuilder] " +
                "MainModelBundles has no valid model assets after assignment.");

            return;
        }

        // --------------------------------------------------------
        // OUTPUT
        // --------------------------------------------------------

        // --------------------------------------------------------
        //
        // DO NOT DELETE THE WHOLE OUTPUT DIRECTORY.
        //
        // There may be:
        //
        // creatures
        // characters
        // environment
        // gameboard
        // mainresourcesbundle
        // etc.
        //
        // MainModelBundles must NEVER delete those files.
        // --------------------------------------------------------

        string outputPath =
            GetOutputPath(target);

        if (!Directory.Exists(
            outputPath))
        {
            Directory.CreateDirectory(
                outputPath);
        }

        // --------------------------------------------------------
        // DELETE ONLY OUR OWN OUTPUT
        // --------------------------------------------------------

        DeleteOwnOutputFiles(
            outputPath);

        // --------------------------------------------------------
        // BUILD MAP
        // --------------------------------------------------------
        //
        // IMPORTANT:
        //
        // BuildAssetBundles(outputPath, options, target)
        //
        // builds all Editor-assigned AssetBundles.
        //
        // Instead we explicitly provide ONE build definition.
        // Therefore this build can only generate:
        //
        // mainmodelbundles
        //
        // and cannot rebuild/overwrite the other bundle files.
        // --------------------------------------------------------

        AssetBundleBuild modelBuild =
            new AssetBundleBuild();

        modelBuild.assetBundleName =
            BundleName.ToLowerInvariant();

        modelBuild.assetBundleVariant =
            null;

        modelBuild.assetNames =
            validAssets.ToArray();

        AssetBundleBuild[] buildMap =
            new AssetBundleBuild[]
            {
                modelBuild
            };

        Debug.Log(
            "[AssetModelBundleBuilder] " +
            "Build map created.");

        Debug.Log(
            "[AssetModelBundleBuilder] " +
            "Model assets: " +
            validAssets.Count);

        foreach (string assetPath in validAssets)
        {
            Debug.Log(
                "[AssetModelBundleBuilder] " +
                "BUILD ASSET -> " +
                assetPath);
        }

        // --------------------------------------------------------
        // BUILD
        // --------------------------------------------------------

        BuildAssetBundleOptions options =
            BuildAssetBundleOptions
                .ChunkBasedCompression;

        AssetBundleManifest manifest;

        try
        {
            manifest =
                BuildPipeline.BuildAssetBundles(
                    outputPath,
                    buildMap,
                    options,
                    target);
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "[AssetModelBundleBuilder] " +
                "BUILD EXCEPTION:\n" +
                ex);

            return;
        }

        if (manifest == null)
        {
            Debug.LogError(
                "[AssetModelBundleBuilder] " +
                "BUILD FAILED!");

            return;
        }

        // --------------------------------------------------------
        // VERIFY OUR BUNDLE
        // --------------------------------------------------------

        string bundleFile =
            Path.Combine(
                outputPath,
                BundleName.ToLowerInvariant());

        if (!File.Exists(
            bundleFile))
        {
            Debug.LogError(
                "[AssetModelBundleBuilder] " +
                "Expected bundle was not generated:\n" +
                bundleFile);

            return;
        }

        // --------------------------------------------------------
        // DELETE ONLY OUR MANIFEST
        // --------------------------------------------------------
        //
        // NEVER delete manifests belonging to other bundles.
        // --------------------------------------------------------

        DeleteOwnManifestFile(
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
            bundleFile);

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

        string[] guids =
            AssetDatabase.FindAssets(
                "",
                new[] { "Assets" });

        Debug.Log(
            "[AssetModelBundleBuilder] Scanning " +
            guids.Length +
            " assets.");

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

            assetPath =
                NormalizePath(
                    assetPath);

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

            if (!processed.Add(
                assetPath))
            {
                continue;
            }

            if (!ContainsModelAsset(
                assetPath))
            {
                continue;
            }

            // ----------------------------------------------------
            // NEVER OVERWRITE ANOTHER BUNDLE
            // ----------------------------------------------------

            if (!TryAssign(
                assetPath))
            {
                continue;
            }

            assigned++;

            Debug.Log(
                "[AssetModelBundleBuilder] ASSIGNED\n" +
                "Bundle: " +
                BundleName +
                "\nAsset: " +
                assetPath);
        }

        Debug.Log(
            "[AssetModelBundleBuilder] Total assigned: " +
            assigned);

        return assigned;
    }

    // ============================================================
    // CONTENT CHECK
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

        // Scene files are not model sources. More importantly, Unity
        // does not allow LoadAllAssetsAtPath/ReadObjectThreaded on
        // scene objects during this scan.
        if (assetPath.EndsWith(
            ".unity",
            StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

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
                "Could not inspect:\n" +
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

        for (int i = 0;
            i < assets.Length;
            i++)
        {
            UnityEngine.Object asset =
                assets[i];

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
            // ANIMATION
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
    // SAFE ASSIGNMENT
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
        // ALREADY OURS
        // --------------------------------------------------------

        if (string.Equals(
            existingBundle,
            BundleName,
            StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // --------------------------------------------------------
        // OWNED BY ANOTHER BUNDLE
        // --------------------------------------------------------

        if (!string.IsNullOrEmpty(
            existingBundle))
        {
            Debug.LogWarning(
                "[AssetModelBundleBuilder] " +
                "SKIPPED - asset already belongs to another bundle.\n" +
                "Asset: " +
                assetPath +
                "\nExisting: " +
                existingBundle +
                "\nRequested: " +
                BundleName);

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
    // IGNORE
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
            NormalizePath(
                assetPath);

        // --------------------------------------------------------
        // RESOURCES
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
        // META
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
    // CLEAR
    // ============================================================

    private static void ClearBundleAssignments()
    {
        string[] assetPaths =
            AssetDatabase.GetAssetPathsFromAssetBundle(
                BundleName);

        int cleared = 0;

        if (assetPaths != null)
        {
            foreach (string assetPath in assetPaths)
            {
                AssetImporter importer =
                    AssetImporter.GetAtPath(
                        assetPath);

                if (importer == null)
                {
                    continue;
                }

                if (!string.Equals(
                    importer.assetBundleName,
                    BundleName,
                    StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                importer.assetBundleName =
                    null;

                cleared++;
            }
        }

        AssetDatabase.RemoveUnusedAssetBundleNames();

        AssetDatabase.SaveAssets();

        Debug.Log(
            "[AssetModelBundleBuilder] Cleared " +
            cleared +
            " existing MainModelBundles assignments.");
    }

    // ============================================================
    // OUTPUT
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
    // MANIFEST CLEANUP
    // ============================================================

    private static void DeleteOwnManifestFile(
        string outputPath)
    {
        if (!Directory.Exists(
            outputPath))
        {
            return;
        }

        string manifestFile =
            Path.Combine(
                outputPath,
                BundleName.ToLowerInvariant() +
                ".manifest");

        if (!File.Exists(manifestFile))
        {
            return;
        }

        try
        {
            File.Delete(manifestFile);

            Debug.Log(
                "[AssetModelBundleBuilder] " +
                "Deleted old MainModelBundles manifest: " +
                manifestFile);
        }
        catch (Exception ex)
        {
            Debug.LogWarning(
                "[AssetModelBundleBuilder] " +
                "Could not delete MainModelBundles manifest:\n" +
                manifestFile +
                "\n" +
                ex.Message);
        }
    }

    // ============================================================
    // LOG
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
                SearchOption.TopDirectoryOnly);

        foreach (string file in files)
        {
            string name =
                Path.GetFileName(
                    file);

            if (name.EndsWith(
                ".manifest",
                StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            Debug.Log(
                "[AssetModelBundleBuilder] OUTPUT: " +
                name);
        }
    }

    // ============================================================
    // PATH
    // ============================================================

    private static string NormalizePath(
        string path)
    {
        if (string.IsNullOrEmpty(
            path))
        {
            return string.Empty;
        }

        return path
            .Replace("\\", "/")
            .Trim();
    }

    private static void DeleteOwnOutputFiles(
        string outputPath)
    {
        if (!Directory.Exists(
            outputPath))
        {
            return;
        }

        string bundleFile =
            Path.Combine(
                outputPath,
                BundleName.ToLowerInvariant());

        string bundleManifest =
            bundleFile +
            ".manifest";

        // --------------------------------------------------------
        // ONLY DELETE MainModelBundles
        // --------------------------------------------------------

        if (File.Exists(bundleFile))
        {
            try
            {
                File.Delete(
                    bundleFile);

                Debug.Log(
                    "[AssetModelBundleBuilder] " +
                    "Deleted old MainModelBundles: " +
                    bundleFile);
            }
            catch (Exception ex)
            {
                Debug.LogError(
                    "[AssetModelBundleBuilder] " +
                    "Could not delete old MainModelBundles:\n" +
                    bundleFile +
                    "\n" +
                    ex.Message);

                throw;
            }
        }

        if (File.Exists(bundleManifest))
        {
            try
            {
                File.Delete(
                    bundleManifest);
            }
            catch (Exception ex)
            {
                Debug.LogWarning(
                    "[AssetModelBundleBuilder] " +
                    "Could not delete old bundle manifest:\n" +
                    bundleManifest +
                    "\n" +
                    ex.Message);
            }
        }
    }
}