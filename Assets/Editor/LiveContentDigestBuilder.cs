
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Builds live-content AssetBundles for Android, iOS, and Windows.
/// Export filename: CWK_2026_HAL_001.<8-character-MD5>
/// Manifest filename: content_packs.json
/// Place under Assets/Editor/LiveContentDigestBuilder.cs
/// </summary>
public static class LiveContentDigestBuilder
{
    private const string BundlePrefix = "MainEffectsBundles.";
    private const string TemporaryBundleName = "live_content_build_temp";
    private const string BuildDirectoryName = "LiveContentBuild";
    private const string ExportDirectoryName = "LiveExport";
    private const string LogicalBundleName = "live_content";
    private const string ManifestFileName = "content_packs.json";

    private static readonly string[] BundleAssetPaths =
    {
        "Assets/Resources/vfx/actions",
        "Assets/Resources/vfx/creatures",
        "Assets/Resources/vfx/leader",
        "Assets/Resources/vfx/textures"

        // Examples:
        // "Assets/Resources/ui/icon_actioncards/icon_example.png",
        // "Assets/Resources/creatures/TeleportGuy",
        // "Assets/Resources/characters/MarshallLee"
    };

    [Serializable]
    private class BundlePatchEntry
    {
        public string file;
        public string hash;
    }

    [MenuItem("Tools/Live Content/Build/Android")]
    private static void BuildAndroid()
    {
        BuildForTarget(BuildTarget.Android, "Android");
    }

    [MenuItem("Tools/Live Content/Build/iOS")]
    private static void BuildIOS()
    {
        BuildForTarget(BuildTarget.iOS, "iOS");
    }

    [MenuItem("Tools/Live Content/Build/Windows 64-bit")]
    private static void BuildWindows()
    {
        BuildForTarget(BuildTarget.StandaloneWindows64, "Windows");
    }

    [MenuItem("Tools/Live Content/Build/All Platforms")]
    private static void BuildAllPlatforms()
    {
        BuildForTarget(BuildTarget.Android, "Android");
        BuildForTarget(BuildTarget.iOS, "iOS");
        BuildForTarget(BuildTarget.StandaloneWindows64, "Windows");
    }

    private static void BuildForTarget(
        BuildTarget target,
        string platformFolderName)
    {
        string projectRoot =
            Directory.GetParent(Application.dataPath).FullName;

        string buildDirectory = Path.Combine(
            projectRoot,
            BuildDirectoryName,
            platformFolderName);

        string exportDirectory = Path.Combine(
            projectRoot,
            ExportDirectoryName,
            platformFolderName);

        try
        {
            string[] assetPaths =
                CollectAssetPaths(BundleAssetPaths);

            if (assetPaths.Length == 0)
            {
                throw new InvalidOperationException(
                    "No valid assets were found. Check BundleAssetPaths.");
            }

            if (!BuildPipeline.IsBuildTargetSupported(
                    BuildTargetGroupFor(target), target))
            {
                throw new InvalidOperationException(
                    "Build support for " + platformFolderName +
                    " is not installed or this target is unsupported.");
            }

            // Delete only this platform's temporary build directory.
            if (Directory.Exists(buildDirectory))
                Directory.Delete(buildDirectory, true);

            Directory.CreateDirectory(buildDirectory);
            Directory.CreateDirectory(exportDirectory);

            AssetBundleBuild build = new AssetBundleBuild
            {
                assetBundleName = TemporaryBundleName,
                assetNames = assetPaths
            };

            Debug.Log(
                "[LiveContentDigest] Building " +
                platformFolderName +
                " AssetBundle with " +
                assetPaths.Length + " assets.");

            foreach (string assetPath in assetPaths)
            {
                Debug.Log(
                    "[LiveContentDigest] Included: " + assetPath);
            }

            AssetBundleManifest manifest =
                BuildPipeline.BuildAssetBundles(
                    buildDirectory,
                    new[] { build },
                    BuildAssetBundleOptions.ChunkBasedCompression,
                    target);

            if (manifest == null)
            {
                throw new InvalidOperationException(
                    "BuildPipeline.BuildAssetBundles failed for " +
                    platformFolderName +
                    ". Check the Unity Console and platform support.");
            }

            FinalizeSingleBundle(
                buildDirectory,
                exportDirectory,
                manifest,
                platformFolderName);

            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "Live Content - " + platformFolderName,
                "Build and export completed.\n\n" +
                "Platform: " + platformFolderName + "\n" +
                "Assets: " + assetPaths.Length + "\n" +
                "Bundle: " + BundlePrefix + "abcdefgh\n" +
                "Manifest: " + ManifestFileName + "\n\n" +
                "Export directory:\n" + exportDirectory,
                "OK");
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);

            EditorUtility.DisplayDialog(
                "Live Content - " + platformFolderName,
                "Build/export failed:\n\n" + ex.Message,
                "OK");
        }
    }

    private static BuildTargetGroup BuildTargetGroupFor(
        BuildTarget target)
    {
        switch (target)
        {
            case BuildTarget.Android:
                return BuildTargetGroup.Android;

            case BuildTarget.iOS:
                return BuildTargetGroup.iOS;

            case BuildTarget.StandaloneWindows:
            case BuildTarget.StandaloneWindows64:
                return BuildTargetGroup.Standalone;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(target),
                    target,
                    "Unsupported live-content build target.");
        }
    }

    private static string[] CollectAssetPaths(
        string[] configuredPaths)
    {
        HashSet<string> paths =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);

        foreach (string configuredPath in configuredPaths)
        {
            if (string.IsNullOrWhiteSpace(configuredPath))
                continue;

            string normalizedPath =
                configuredPath.Replace('\\', '/');

            if (!normalizedPath.StartsWith(
                    "Assets/", StringComparison.Ordinal))
            {
                Debug.LogWarning(
                    "[LiveContentDigest] Path must start with Assets/: " +
                    normalizedPath);
                continue;
            }

            if (AssetDatabase.IsValidFolder(normalizedPath))
            {
                string[] guids = AssetDatabase.FindAssets(
                    string.Empty,
                    new[] { normalizedPath });

                foreach (string guid in guids)
                {
                    string assetPath =
                        AssetDatabase.GUIDToAssetPath(guid);

                    if (string.IsNullOrEmpty(assetPath) ||
                        AssetDatabase.IsValidFolder(assetPath) ||
                        assetPath.EndsWith(
                            ".meta",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    paths.Add(assetPath);
                }
            }
            else
            {
                string guid =
                    AssetDatabase.AssetPathToGUID(normalizedPath);

                if (string.IsNullOrEmpty(guid))
                {
                    Debug.LogWarning(
                        "[LiveContentDigest] Asset not found: " +
                        normalizedPath);
                    continue;
                }

                paths.Add(normalizedPath);
            }
        }

        List<string> result = new List<string>(paths);
        result.Sort(StringComparer.OrdinalIgnoreCase);

        return result.ToArray();
    }

    private static void FinalizeSingleBundle(
        string buildDirectory,
        string exportDirectory,
        AssetBundleManifest manifest,
        string platformFolderName)
    {
        if (manifest == null)
            throw new ArgumentNullException(nameof(manifest));

        string sourcePath = Path.Combine(
            buildDirectory,
            TemporaryBundleName);

        if (!File.Exists(sourcePath))
        {
            throw new FileNotFoundException(
                "Built AssetBundle file was not found.",
                sourcePath);
        }

        Hash128 unityHash =
            manifest.GetAssetBundleHash(TemporaryBundleName);

        if (!unityHash.isValid)
        {
            throw new InvalidOperationException(
                "Invalid Unity bundle hash: " +
                TemporaryBundleName);
        }

        // Hash the actual built bundle file.
        string digest = ComputeFileDigest(sourcePath);

        // Final output name: CWK_2026_HAL_001.<8 hex characters>
        string outputName = BundlePrefix + digest;

        string targetPath = Path.Combine(
            exportDirectory,
            outputName);

        string manifestPath = Path.Combine(
            exportDirectory,
            ManifestFileName);

        string temporaryManifestPath = manifestPath + ".tmp";
        string backupManifestPath = manifestPath + ".bak";

        if (File.Exists(targetPath))
        {
            throw new IOException(
                "Export target already exists:\n" + targetPath +
                "\nRemove the old export file before building again.");
        }

        Dictionary<string, BundlePatchEntry> patches =
            new Dictionary<string, BundlePatchEntry>(
                StringComparer.OrdinalIgnoreCase)
            {
                {
                    LogicalBundleName,
                    new BundlePatchEntry
                    {
                        file = outputName,
                        hash = unityHash.ToString()
                    }
                }
            };

        string json = BuildPatchJson(patches);

        File.WriteAllText(
            temporaryManifestPath,
            json,
            new UTF8Encoding(false));

        bool bundleCopied = false;
        bool manifestBackedUp = false;

        try
        {
            // Only the exported filename uses the digest.
            File.Copy(sourcePath, targetPath, false);
            bundleCopied = true;

            if (File.Exists(manifestPath))
            {
                if (File.Exists(backupManifestPath))
                    File.Delete(backupManifestPath);

                File.Move(
                    manifestPath,
                    backupManifestPath);

                manifestBackedUp = true;
            }

            File.Move(
                temporaryManifestPath,
                manifestPath);

            if (manifestBackedUp &&
                File.Exists(backupManifestPath))
            {
                File.Delete(backupManifestPath);
            }

            Debug.Log(
                "[LiveContentDigest] Platform: " +
                platformFolderName +
                "\nLogical bundle key: " + LogicalBundleName +
                "\nOutput: " + outputName +
                "\nMD5 (first 8 hex): " + digest +
                "\nUnity Hash128: " + unityHash +
                "\nManifest: " + manifestPath);
        }
        catch
        {
            if (bundleCopied && File.Exists(targetPath))
                File.Delete(targetPath);

            if (manifestBackedUp &&
                File.Exists(backupManifestPath))
            {
                if (File.Exists(manifestPath))
                    File.Delete(manifestPath);

                File.Move(
                    backupManifestPath,
                    manifestPath);
            }

            if (File.Exists(temporaryManifestPath))
                File.Delete(temporaryManifestPath);

            throw;
        }
    }

    private static string ComputeFileDigest(string path)
    {
        using (MD5 md5 = MD5.Create())
        using (FileStream stream = File.OpenRead(path))
        {
            byte[] bytes = md5.ComputeHash(stream);
            StringBuilder result = new StringBuilder(8);

            for (int i = 0; i < 4; i++)
                result.Append(bytes[i].ToString("x2"));

            return result.ToString();
        }
    }

    // Preserves the JSON object shape expected by the existing parser.
    private static string BuildPatchJson(
        Dictionary<string, BundlePatchEntry> patches)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("{");

        int index = 0;

        foreach (KeyValuePair<string, BundlePatchEntry> item in patches)
        {
            sb.Append("  ")
                .Append(Quote(item.Key))
                .AppendLine(": {");

            sb.Append("    \"file\": ")
                .Append(Quote(item.Value.file))
                .AppendLine(",");

            sb.Append("    \"hash\": ")
                .Append(Quote(item.Value.hash))
                .AppendLine();

            sb.Append("  }");

            if (++index < patches.Count)
                sb.Append(',');

            sb.AppendLine();
        }

        sb.AppendLine("}");
        return sb.ToString();
    }

    private static string Quote(string value)
    {
        if (value == null)
            return "\"\"";

        StringBuilder sb =
            new StringBuilder(value.Length + 2);

        sb.Append('"');

        foreach (char c in value)
        {
            switch (c)
            {
                case '"':
                    sb.Append("\\\"");
                    break;

                case '\\':
                    sb.Append("\\\\");
                    break;

                case '\n':
                    sb.Append("\\n");
                    break;

                case '\r':
                    sb.Append("\\r");
                    break;

                case '\t':
                    sb.Append("\\t");
                    break;

                default:
                    if (c < 0x20)
                    {
                        sb.Append("\\u")
                            .Append(((int)c).ToString("x4"));
                    }
                    else
                    {
                        sb.Append(c);
                    }

                    break;
            }
        }

        sb.Append('"');
        return sb.ToString();
    }
}
