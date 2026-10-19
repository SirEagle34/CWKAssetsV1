using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

public static class AssetBundleBuilder
{
// ============================================================
// CONFIG
// ============================================================

private const string OutputRoot = "AssetBundles";

private const string CreatureRoot =
    "Assets/Resources/creatures";

private const string CharacterRoot =
    "Assets/Resources/characters";

private const string EnvironmentRoot =
    "Assets/Resources/environment";

private const string SharedShaderBundle =
    "shared_shaders";

private static readonly string[] ResourceFolders =
{
    CreatureRoot,
    CharacterRoot,
    EnvironmentRoot
};

// ============================================================
// MENU
// ============================================================

[MenuItem("Tools/AssetBundles/Build Live Bundles")]
public static void BuildLiveBundles()
{
    BuildBundles(BuildTarget.Android);
}

[MenuItem("Tools/AssetBundles/Build Live Bundles/Android")]
public static void BuildAndroidBundles()
{
    BuildBundles(BuildTarget.Android);
}

[MenuItem("Tools/AssetBundles/Build Live Bundles/Windows")]
public static void BuildWindowsBundles()
{
    BuildBundles(BuildTarget.StandaloneWindows64);
}

[MenuItem("Tools/AssetBundles/Build Live Bundles/iOS")]
public static void BuildIOSBundles()
{
    BuildBundles(BuildTarget.iOS);
}

[MenuItem("Tools/AssetBundles/Clear Bundle Names")]
public static void ClearBundleNames()
{
    ClearAllBundleNames();
}

// ============================================================
// BUILD
// ============================================================


private static void BuildBundles(BuildTarget target)
{
    Debug.Log("=================================================");
    Debug.Log("[AssetBundle] BUILD START");
    Debug.Log("Target: " + target);
    Debug.Log("=================================================");

    AssignBundleNames();
    LogAllDependencies();
    AssetDatabase.RemoveUnusedAssetBundleNames();

    string outputPath = GetOutputPath(target);

    if (Directory.Exists(outputPath))
    {
        Directory.Delete(outputPath, true);
    }

    Directory.CreateDirectory(outputPath);

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
        Debug.LogError("[AssetBundle] BUILD FAILED!");
        return;
    }

    // Digest'leri gerçek bundle dosyalarından hesapla.
    // Unity Hash128 değerlerini dosya adındaki digest'ten ayrı tut.
    GenerateDigestBundlesAndContentPacks(
        outputPath,
        manifest
    );

    AssetDatabase.RemoveUnusedAssetBundleNames();
    AssetDatabase.SaveAssets();
    AssetDatabase.Refresh();

    // Unity manifest dosyalarını yalnızca paket listesi üretildikten
    // sonra temizle.
    DeleteManifestFiles(outputPath);

    LogBuiltBundles(outputPath);

    Debug.Log("=================================================");
    Debug.Log("[AssetBundle] BUILD COMPLETE");
    Debug.Log("Target: " + target);
    Debug.Log("Output: " + outputPath);
    Debug.Log("Content pack list: " +
        Path.Combine(outputPath, "content_packs.json"));
    Debug.Log("=================================================");
}


private static void GenerateDigestBundlesAndContentPacks(
    string outputPath,
    AssetBundleManifest manifest)
{
    if (manifest == null)
    {
        Debug.LogError("[AssetBundle] Manifest is null.");
        return;
    }

    string[] bundleNames = manifest.GetAllAssetBundles();

    StringBuilder json = new StringBuilder();
    json.AppendLine("[");

    int written = 0;

    foreach (string bundleName in bundleNames)
    {
        string relativeSource = bundleName.Replace(
            '/',
            Path.DirectorySeparatorChar
        );

        string sourcePath = Path.Combine(
            outputPath,
            relativeSource
        );

        if (!File.Exists(sourcePath))
        {
            Debug.LogError(
                "[AssetBundle] Bundle file missing: " +
                sourcePath
            );
            continue;
        }

        // Gerçek AssetBundle baytlarından MD5 hesapla.
        string digest = ComputeBundleDigest(sourcePath);

        string directory = Path.GetDirectoryName(sourcePath);
        string originalFileName = Path.GetFileName(sourcePath);

        // Örnek: hero_marshalllee.a1b2c3d4
        string digestFileName = originalFileName + "." + digest;
        string targetPath = Path.Combine(
            directory ?? outputPath,
            digestFileName
        );

        if (!string.Equals(
            sourcePath,
            targetPath,
            StringComparison.OrdinalIgnoreCase))
        {
            if (File.Exists(targetPath))
            {
                File.Delete(targetPath);
            }

            File.Move(sourcePath, targetPath);
        }

        // URL için outputPath'e göre göreli dosya yolu.
        string relativeFile = targetPath
            .Substring(outputPath.Length)
            .TrimStart(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar
            )
            .Replace('\\', '/');

        // Bu değer 8 karakterlik digest DEĞİL.
        // Unity'nin kendi AssetBundle Hash128 değeridir.
        string unityHash =
            manifest.GetAssetBundleHash(bundleName).ToString();

        if (written > 0)
        {
            json.AppendLine(",");
        }

        json.AppendLine("  {");
        json.AppendLine("    \"name\": " +
            JsonQuote(bundleName) + ",");
        json.AppendLine("    \"file\": " +
            JsonQuote(relativeFile) + ",");
        json.AppendLine("    \"hash\": " +
            JsonQuote(unityHash) + ",");
        json.Append("    \"stage\": \"live\"");
        json.AppendLine();
        json.Append("  }");

        written++;

        Debug.Log(
            "[AssetBundle] Digest filename: " +
            relativeFile +
            " | MD5-8: " + digest +
            " | Unity Hash128: " + unityHash
        );
    }

    json.AppendLine();
    json.AppendLine("]");

    string contentPacksPath = Path.Combine(
        outputPath,
        "content_packs.json"
    );

    File.WriteAllText(
        contentPacksPath,
        json.ToString(),
        new UTF8Encoding(false)
    );

    Debug.Log(
        "[AssetBundle] content_packs.json generated. Bundles: " +
        written
    );
}

private static string ComputeBundleDigest(string filePath)
{
    using (MD5 md5 = MD5.Create())
    using (FileStream stream = File.OpenRead(filePath))
    {
        byte[] hash = md5.ComputeHash(stream);
        StringBuilder result = new StringBuilder(8);

        // İlk 4 byte = 8 hexadecimal karakter.
        // x2 formatı küçük harf üretir.
        for (int i = 0; i < 4; i++)
        {
            result.Append(hash[i].ToString("x2"));
        }

        return result.ToString();
    }
}

private static string JsonQuote(string value)
{
    if (value == null)
    {
        return "\"\"";
    }

    StringBuilder result = new StringBuilder();
    result.Append('"');

    foreach (char c in value)
    {
        switch (c)
        {
            case '"':
                result.Append("\\\"");
                break;

            case '\\':
                result.Append("\\\\");
                break;

            case '\n':
                result.Append("\\n");
                break;

            case '\r':
                result.Append("\\r");
                break;

            case '\t':
                result.Append("\\t");
                break;

            default:
                if (c < 32)
                {
                    result.Append("\\u");
                    result.Append(((int)c).ToString("x4"));
                }
                else
                {
                    result.Append(c);
                }
                break;
        }
    }

    result.Append('"');
    return result.ToString();
}

// ============================================================
// ASSIGN BUNDLE NAMES
// ============================================================

private static void AssignBundleNames()
{
    Debug.Log(
        "[AssetBundle] Assigning individual bundle names..."
    );

    ClearAllBundleNames();

    foreach (string rootFolder in ResourceFolders)
    {
        if (!AssetDatabase.IsValidFolder(rootFolder))
        {
            Debug.LogWarning(
                "[AssetBundle] Folder does not exist: " +
                rootFolder
            );

            continue;
        }

        AssignRootFolderBundles(rootFolder);
    }

    AssignSharedShaders();

    ValidateRequiredShaders();

    ValidateAllShaders();

    AssetDatabase.SaveAssets();
    AssetDatabase.Refresh();

    Debug.Log(
        "[AssetBundle] Bundle names assigned."
    );
}

// ============================================================
// ROOT FOLDER PROCESSING
// ============================================================

private static void AssignRootFolderBundles(
    string rootFolder
)
{
    string[] subFolders =
        AssetDatabase.GetSubFolders(rootFolder);

    foreach (string subFolder in subFolders)
    {
        AssignSubFolderBundle(
            rootFolder,
            subFolder
        );
    }

    /*
    * Also support assets directly inside:
    *
    * Assets/Resources/creatures/
    *
    * If such assets exist, they receive a bundle
    * based on the root folder name.
    */

    AssignDirectAssets(
        rootFolder
    );
}

// ============================================================
// SUB FOLDER PROCESSING
// ============================================================

private static void AssignSubFolderBundle(
    string rootFolder,
    string subFolder
)
{
    string folderName =
        Path.GetFileName(
            subFolder.TrimEnd(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar
            )
        );

    string bundleName =
        SanitizeBundleName(folderName);

    string[] guids =
        AssetDatabase.FindAssets(
            "",
            new[] { subFolder }
        );

    int count = 0;

    foreach (string guid in guids)
    {
        string assetPath =
            AssetDatabase.GUIDToAssetPath(guid);

        if (AssetDatabase.IsValidFolder(assetPath))
        {
            continue;
        }

        if (IsIgnoredAsset(assetPath))
        {
            continue;
        }

        AssetImporter importer =
            AssetImporter.GetAtPath(assetPath);

        if (importer == null)
        {
            continue;
        }

        importer.assetBundleName =
            bundleName;

        count++;
    }

    Debug.Log(
        "[AssetBundle] " +
        bundleName +
        " <- " +
        count +
        " assets"
    );
}

// ============================================================
// DIRECT ASSETS
// ============================================================

private static void AssignDirectAssets(
    string rootFolder
)
{
    string[] guids =
        AssetDatabase.FindAssets(
            "",
            new[] { rootFolder }
        );

    int count = 0;

    foreach (string guid in guids)
    {
        string assetPath =
            AssetDatabase.GUIDToAssetPath(
                guid
            );

        if (AssetDatabase.IsValidFolder(assetPath))
        {
            continue;
        }

        string directory =
            Path.GetDirectoryName(
                assetPath
            );

        directory =
            directory?.Replace(
                "\\",
                "/"
            );

        if (directory != rootFolder)
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

        string fileName =
            Path.GetFileNameWithoutExtension(
                assetPath
            );

        string bundleName =
            SanitizeBundleName(
                fileName
            );

        importer.assetBundleName =
            bundleName;

        count++;

        Debug.Log(
            "[AssetBundle] Direct asset -> " +
            bundleName +
            " : " +
            assetPath
        );
    }

    if (count > 0)
    {
        Debug.Log(
            "[AssetBundle] Direct assets: " +
            count
        );
    }
}

// ============================================================
// CATEGORY
// ============================================================

private static string GetCategoryName(
    string rootFolder
)
{
    if (string.Equals(
        rootFolder,
        CreatureRoot,
        StringComparison.OrdinalIgnoreCase))
    {
        return "creatures";
    }

    if (string.Equals(
        rootFolder,
        CharacterRoot,
        StringComparison.OrdinalIgnoreCase))
    {
        return "characters";
    }

    if (string.Equals(
        rootFolder,
        EnvironmentRoot,
        StringComparison.OrdinalIgnoreCase))
    {
        return "environment";
    }

    return Path.GetFileName(
        rootFolder.TrimEnd(
            Path.DirectorySeparatorChar,
            Path.AltDirectorySeparatorChar
        )
    ).ToLowerInvariant();
}

// ============================================================
// BUNDLE NAME SANITIZATION
// ============================================================

private static string SanitizeBundleName(
    string value
)
{
    if (string.IsNullOrEmpty(value))
    {
        return "bundle";
    }

    value =
        value.Trim()
            .ToLowerInvariant()
            .Replace(" ", "_")
            .Replace("-", "_");

    char[] characters =
        value.ToCharArray();

    for (int i = 0; i < characters.Length; i++)
    {
        char c = characters[i];

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
// SHARED SHADERS
// ============================================================

private static void AssignSharedShaders()
{
    string[] shaderNames =
    {
        "Unlit/Transparent Cutout",
        "Transparent/Vertex With Z"
    };

    foreach (string shaderName in shaderNames)
    {
        Shader shader =
            Shader.Find(
                shaderName
            );

        if (shader == null)
        {
            Debug.LogError(
                "[AssetBundle] Shader NOT FOUND: " +
                shaderName
            );

            continue;
        }

        string shaderPath =
            AssetDatabase.GetAssetPath(
                shader
            );

        if (string.IsNullOrEmpty(
            shaderPath))
        {
            Debug.LogError(
                "[AssetBundle] Shader has no asset path: " +
                shaderName
            );

            continue;
        }

        AssetImporter importer =
            AssetImporter.GetAtPath(
                shaderPath
            );

        if (importer == null)
        {
            Debug.LogError(
                "[AssetBundle] Shader importer NOT FOUND: " +
                shaderName
            );

            continue;
        }

        importer.assetBundleName =
            SharedShaderBundle;

        Debug.Log(
            "[AssetBundle] Shared shader -> " +
            SharedShaderBundle +
            " : " +
            shaderName
        );
    }
}

// ============================================================
// SHADER VALIDATION
// ============================================================

private static void ValidateRequiredShaders()
{
    string[] requiredShaders =
    {
        "Unlit/Transparent Cutout",
        "Transparent/Vertex With Z"
    };

    foreach (string shaderName in requiredShaders)
    {
        Shader shader =
            Shader.Find(
                shaderName
            );

        if (shader == null)
        {
            Debug.LogError(
                "[AssetBundle] REQUIRED SHADER NOT FOUND: " +
                shaderName
            );
        }
        else
        {
            Debug.Log(
                "[AssetBundle] Required shader OK: " +
                shaderName
            );
        }
    }
}

private static void ValidateAllShaders()
{
    foreach (string rootFolder in ResourceFolders)
    {
        if (!AssetDatabase.IsValidFolder(
            rootFolder))
        {
            continue;
        }

        string[] guids =
            AssetDatabase.FindAssets(
                "t:Prefab",
                new[] { rootFolder }
            );

        foreach (string guid in guids)
        {
            string prefabPath =
                AssetDatabase.GUIDToAssetPath(
                    guid
                );

            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    prefabPath
                );

            if (prefab == null)
            {
                continue;
            }

            Renderer[] renderers =
                prefab.GetComponentsInChildren<Renderer>(
                    true
                );

            foreach (Renderer renderer in renderers)
            {
                if (renderer == null)
                {
                    continue;
                }

                Material[] materials =
                    renderer.sharedMaterials;

                foreach (Material material in materials)
                {
                    if (material == null)
                    {
                        Debug.LogError(
                            "[AssetBundle] NULL MATERIAL: " +
                            prefabPath +
                            " / " +
                            renderer.name
                        );

                        continue;
                    }

                    Shader shader =
                        material.shader;

                    if (shader == null)
                    {
                        Debug.LogError(
                            "[AssetBundle] NULL SHADER: " +
                            prefabPath +
                            " / Material: " +
                            material.name
                        );

                        continue;
                    }

                    if (
                        shader.name ==
                        "Unlit/Transparent Cutout"
                    )
                    {
                        Debug.Log(
                            "[AssetBundle] CUTOUT OK: " +
                            prefabPath +
                            " / " +
                            material.name
                        );
                    }
                    else if (
                        shader.name ==
                        "Transparent/Vertex With Z"
                    )
                    {
                        Debug.Log(
                            "[AssetBundle] VERTEX WITH Z OK: " +
                            prefabPath +
                            " / " +
                            material.name
                        );
                    }
                }
            }
        }
    }
}

// ============================================================
// DEPENDENCY DEBUG
// ============================================================

private static void LogAllDependencies()
{
    foreach (string rootFolder in ResourceFolders)
    {
        if (!AssetDatabase.IsValidFolder(
            rootFolder))
        {
            continue;
        }

        string[] guids =
            AssetDatabase.FindAssets(
                "t:Prefab",
                new[] { rootFolder }
            );

        foreach (string guid in guids)
        {
            string prefabPath =
                AssetDatabase.GUIDToAssetPath(
                    guid
                );

            string[] dependencies =
                AssetDatabase.GetDependencies(
                    prefabPath,
                    true
                );

            Debug.Log(
                "================================================="
            );

            Debug.Log(
                "[AssetBundle Dependencies] " +
                prefabPath
            );

            foreach (string dependency in dependencies)
            {
                Debug.Log(
                    "    -> " +
                    dependency
                );
            }
        }
    }
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
// LOG BUILT BUNDLES
// ============================================================

private static void LogBuiltBundles(
    string outputPath
)
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
            SearchOption.AllDirectories
        );

    Debug.Log(
        "[AssetBundle] Built files:"
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

// ============================================================
// CLEAR BUNDLE NAMES
// ============================================================

private static void ClearAllBundleNames()
{
    string[] bundleNames =
        AssetDatabase.GetAllAssetBundleNames();

    foreach (string bundleName in bundleNames)
    {
        string[] assetPaths =
            AssetDatabase.GetAssetPathsFromAssetBundle(
                bundleName
            );

        foreach (string assetPath in assetPaths)
        {
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

    AssetDatabase.RemoveUnusedAssetBundleNames();

    AssetDatabase.SaveAssets();
    AssetDatabase.Refresh();

    Debug.Log(
        "[AssetBundle] All bundle names cleared."
    );
}

private static void DeleteManifestFiles(string outputPath)
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
                "[AssetBundle] Deleted manifest: " +
                file
            );
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "[AssetBundle] Failed to delete manifest: " +
                file +
                " / " +
                ex.Message
            );
        }
    }
}

}
