using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class AssetBundlesBuilderExtra
{
    // ============================================================
    // CONFIG
    // ============================================================

    private const string OutputRoot = "AssetBundlesExtra";

    // ============================================================
    // EXTRA BUNDLE PACKS
    // ============================================================
    //
    // Extra000001 is one complete Resources bundle.
    //
    // IMPORTANT:
    // We intentionally use Assets/Resources as the root.
    // Do NOT strip "Resources" from the asset path.
    //
    // Example:
    //
    // Assets/Resources/creatures/Pig/Pig.prefab
    // -> Resources/creatures/Pig/Pig.prefab
    //
    // Assets/Resources/characters/Finn/Finn.prefab
    // -> Resources/characters/Finn/Finn.prefab
    //
    // Assets/Resources/vfx/...
    // Assets/Resources/textures/...
    // Assets/Resources/battle_portrait_background/...
    // Assets/Resources/landscapes/...
    // Assets/Resources/atlases/...
    // Assets/Resources/materials/...
    // Assets/Resources/ui/...
    // and every other Resources subfolder are included automatically.
    //
    // No hard-coded list of resource folders is required.
    //
    // ============================================================

    private static readonly List<ExtraBundlePack> ExtraPacks =
        new List<ExtraBundlePack>
        {
            new ExtraBundlePack
            {
                BundleName = "Extra000001",

                Paths = new List<string>
                {
                    "Assets/Resources"
                }
            }
        };

    // ============================================================
    // DATA
    // ============================================================

    private class ExtraBundlePack
    {
        public string BundleName;

        public List<string> Paths =
            new List<string>();
    }

    // ============================================================
    // MENU
    // ============================================================

    [MenuItem("Tools/AssetBundles/Build Extra Bundles/Android")]
    public static void BuildAndroid()
    {
        Build(BuildTarget.Android);
    }

    [MenuItem("Tools/AssetBundles/Build Extra Bundles/Windows")]
    public static void BuildWindows()
    {
        Build(BuildTarget.StandaloneWindows64);
    }

    [MenuItem("Tools/AssetBundles/Build Extra Bundles/iOS")]
    public static void BuildIOS()
    {
        Build(BuildTarget.iOS);
    }

    // ============================================================
    // BUILD
    // ============================================================

    private static void Build(BuildTarget target)
    {
        Debug.Log("=================================================");
        Debug.Log("[ExtraAssetBundles] BUILD START");
        Debug.Log("Target: " + target);
        Debug.Log("=================================================");

        if (ExtraPacks == null ||
            ExtraPacks.Count == 0)
        {
            Debug.LogWarning(
                "[ExtraAssetBundles] No extra bundle packs configured."
            );

            return;
        }

        ClearExtraBundleNames();

        int validPackCount =
            AssignExtraBundleNames();

        if (validPackCount == 0)
        {
            Debug.LogError(
                "[ExtraAssetBundles] No valid bundle packs found."
            );

            return;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        string outputPath =
            GetOutputPath(target);

        if (Directory.Exists(outputPath))
        {
            Directory.Delete(
                outputPath,
                true
            );
        }

        Directory.CreateDirectory(
            outputPath
        );

        BuildAssetBundleOptions options =
            BuildAssetBundleOptions.ChunkBasedCompression;

        AssetBundleManifest manifest =
            BuildPipeline.BuildAssetBundles(
                outputPath,
                options,
                target
            );

        if (manifest == null)
        {
            Debug.LogError(
                "[ExtraAssetBundles] BUILD FAILED!"
            );

            return;
        }

        AssetDatabase.RemoveUnusedAssetBundleNames();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        DeleteManifestFiles(
            outputPath
        );

        LogBuiltBundles(
            outputPath
        );

        Debug.Log("=================================================");
        Debug.Log("[ExtraAssetBundles] BUILD COMPLETE");
        Debug.Log("Output: " + outputPath);
        Debug.Log("=================================================");
    }

    // ============================================================
    // ASSIGN BUNDLE NAMES
    // ============================================================

    private static int AssignExtraBundleNames()
    {
        int validPackCount = 0;

        foreach (ExtraBundlePack pack in ExtraPacks)
        {
            if (pack == null)
            {
                continue;
            }

            if (string.IsNullOrEmpty(pack.BundleName))
            {
                Debug.LogWarning(
                    "[ExtraAssetBundles] Bundle name is empty."
                );

                continue;
            }

            if (pack.Paths == null ||
                pack.Paths.Count == 0)
            {
                Debug.LogWarning(
                    "[ExtraAssetBundles] Empty path list: " +
                    pack.BundleName
                );

                continue;
            }

            string bundleName =
                SanitizeBundleName(
                    pack.BundleName
                );

            int assetCount = 0;

            Debug.Log(
                "-------------------------------------------------"
            );

            Debug.Log(
                "[ExtraAssetBundles] PACK: " +
                bundleName
            );

            foreach (string sourcePath in pack.Paths)
            {
                if (string.IsNullOrEmpty(sourcePath))
                {
                    continue;
                }

                string normalizedPath =
                    sourcePath.Replace(
                        "\\",
                        "/"
                    );

                if (!AssetExists(normalizedPath))
                {
                    Debug.LogWarning(
                        "[ExtraAssetBundles] PATH NOT FOUND: " +
                        normalizedPath
                    );

                    continue;
                }

                if (AssetDatabase.IsValidFolder(
                    normalizedPath))
                {
                    assetCount +=
                        AssignFolder(
                            normalizedPath,
                            bundleName
                        );
                }
                else
                {
                    if (AssignFile(
                        normalizedPath,
                        bundleName))
                    {
                        assetCount++;
                    }
                }
            }

            if (assetCount > 0)
            {
                validPackCount++;

                Debug.Log(
                    "[ExtraAssetBundles] " +
                    bundleName +
                    " <- " +
                    assetCount +
                    " assets"
                );
            }
            else
            {
                Debug.LogWarning(
                    "[ExtraAssetBundles] " +
                    bundleName +
                    " contains no valid assets."
                );
            }
        }

        Debug.Log(
            "-------------------------------------------------"
        );

        return validPackCount;
    }

    // ============================================================
    // FOLDER
    // ============================================================

    private static int AssignFolder(
        string folderPath,
        string bundleName
    )
    {
        string[] guids =
            AssetDatabase.FindAssets(
                "",
                new[] { folderPath }
            );

        int count = 0;

        foreach (string guid in guids)
        {
            string assetPath =
                AssetDatabase.GUIDToAssetPath(
                    guid
                );

            if (AssetDatabase.IsValidFolder(
                assetPath))
            {
                continue;
            }

            if (IsIgnoredAsset(assetPath))
            {
                continue;
            }

            AssetImporter importer =
                AssetImporter.GetAtPath(
                    assetPath
                );

            if (importer == null)
            {
                continue;
            }

            importer.assetBundleName =
                bundleName;

            count++;

            Debug.Log(
                "[ExtraAssetBundles] " +
                bundleName +
                " <- " +
                assetPath
            );
        }

        return count;
    }

    // ============================================================
    // FILE
    // ============================================================

    private static bool AssignFile(
        string filePath,
        string bundleName
    )
    {
        if (IsIgnoredAsset(filePath))
        {
            Debug.LogWarning(
                "[ExtraAssetBundles] IGNORED: " +
                filePath
            );

            return false;
        }

        AssetImporter importer =
            AssetImporter.GetAtPath(
                filePath
            );

        if (importer == null)
        {
            Debug.LogWarning(
                "[ExtraAssetBundles] IMPORTER NOT FOUND: " +
                filePath
            );

            return false;
        }

        importer.assetBundleName =
            bundleName;

        Debug.Log(
            "[ExtraAssetBundles] " +
            bundleName +
            " <- " +
            filePath
        );

        return true;
    }

    // ============================================================
    // CHECK ASSET
    // ============================================================

    private static bool AssetExists(
        string path
    )
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            return true;
        }

        UnityEngine.Object asset =
            AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(
                path
            );

        return asset != null;
    }

    // ============================================================
    // CLEAR EXTRA BUNDLE NAMES
    // ============================================================

    private static void ClearExtraBundleNames()
    {
        if (ExtraPacks == null)
        {
            return;
        }

        foreach (ExtraBundlePack pack in ExtraPacks)
        {
            if (pack == null ||
                pack.Paths == null)
            {
                continue;
            }

            foreach (string sourcePath in pack.Paths)
            {
                if (string.IsNullOrEmpty(sourcePath))
                {
                    continue;
                }

                string path =
                    sourcePath.Replace(
                        "\\",
                        "/"
                    );

                if (AssetDatabase.IsValidFolder(path))
                {
                    string[] guids =
                        AssetDatabase.FindAssets(
                            "",
                            new[] { path }
                        );

                    foreach (string guid in guids)
                    {
                        string assetPath =
                            AssetDatabase.GUIDToAssetPath(
                                guid
                            );

                        if (AssetDatabase.IsValidFolder(
                            assetPath))
                        {
                            continue;
                        }

                        AssetImporter importer =
                            AssetImporter.GetAtPath(
                                assetPath
                            );

                        if (importer != null)
                        {
                            importer.assetBundleName =
                                null;
                        }
                    }
                }
                else
                {
                    AssetImporter importer =
                        AssetImporter.GetAtPath(
                            path
                        );

                    if (importer != null)
                    {
                        importer.assetBundleName =
                            null;
                    }
                }
            }
        }

        AssetDatabase.RemoveUnusedAssetBundleNames();

        Debug.Log(
            "[ExtraAssetBundles] Extra bundle names cleared."
        );
    }

    // ============================================================
    // SANITIZE
    // ============================================================

    private static string SanitizeBundleName(
        string value
    )
    {
        if (string.IsNullOrEmpty(value))
        {
            return "extra";
        }

        value =
            value.Trim()
                .ToLowerInvariant()
                .Replace(" ", "_")
                .Replace("-", "_");

        char[] characters =
            value.ToCharArray();

        for (int i = 0;
             i < characters.Length;
             i++)
        {
            char c =
                characters[i];

            if (!(
                (c >= 'a' && c <= 'z') ||
                (c >= '0' && c <= '9') ||
                c == '_'
            ))
            {
                characters[i] = '_';
            }
        }

        return new string(
            characters
        );
    }

    // ============================================================
    // IGNORED ASSETS
    // ============================================================

    private static bool IsIgnoredAsset(
        string assetPath
    )
    {
        if (string.IsNullOrEmpty(assetPath))
        {
            return true;
        }

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

        if (assetPath.Contains(
            "/Editor/",
            StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    // ============================================================
    // OUTPUT PATH
    // ============================================================

    private static string GetOutputPath(
        BuildTarget target
    )
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

            case BuildTarget.StandaloneLinux64:
                targetFolder = "Linux";
                break;

            case BuildTarget.iOS:
                targetFolder = "iOS";
                break;

            default:
                targetFolder =
                    target.ToString();
                break;
        }

        return Path.Combine(
            OutputRoot,
            targetFolder
        );
    }

    // ============================================================
    // DELETE MANIFEST FILES
    // ============================================================

    private static void DeleteManifestFiles(
        string outputPath
    )
    {
        if (!Directory.Exists(outputPath))
        {
            return;
        }

        string[] manifestFiles =
            Directory.GetFiles(
                outputPath,
                "*.manifest",
                SearchOption.AllDirectories
            );

        foreach (string file in manifestFiles)
        {
            try
            {
                File.Delete(file);

                Debug.Log(
                    "[ExtraAssetBundles] Deleted manifest: " +
                    file
                );
            }
            catch (Exception ex)
            {
                Debug.LogError(
                    "[ExtraAssetBundles] Failed to delete manifest: " +
                    file +
                    " / " +
                    ex.Message
                );
            }
        }
    }

    // ============================================================
    // LOG BUILT FILES
    // ============================================================

    private static void LogBuiltBundles(
        string outputPath
    )
    {
        if (!Directory.Exists(outputPath))
        {
            return;
        }

        string[] files =
            Directory.GetFiles(
                outputPath,
                "*",
                SearchOption.AllDirectories
            );

        Debug.Log(
            "[ExtraAssetBundles] Built files:"
        );

        foreach (string file in files)
        {
            string relative =
                file.Substring(
                    outputPath.Length
                ).TrimStart(
                    Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar
                );

            Debug.Log(
                "    -> " +
                relative
            );
        }
    }
}
