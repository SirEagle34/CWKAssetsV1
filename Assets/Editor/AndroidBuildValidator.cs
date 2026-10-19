using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using System.IO;

/// <summary>
/// Valida y prepara el proyecto antes de hacer build para Android.
/// Verifica configuraciones importantes y limpia archivos innecesarios.
/// </summary>
public class AndroidBuildValidator : IPreprocessBuildWithReport, IPostprocessBuildWithReport
{
    public int callbackOrder { get { return 0; } }

    private const string WINDOWS_FOLDER = "Assets/StreamingAssets/Windows";
    private const string WINDOWS_TEMP_FOLDER = "Assets/StreamingAssets/Windows_TEMP_BUILD_EXCLUDED";

    public void OnPreprocessBuild(BuildReport report)
    {
        if (report.summary.platform == BuildTarget.Android)
        {
            Debug.Log("=== Validando configuración de Android Build ===");
            
            ValidateAndroidSettings();
            ValidateManifest();
            ValidatePlugins();
            ExcludeWindowsFilesFromAndroid();
            
            Debug.Log("=== Validación completada ===");
        }
    }

    public void OnPostprocessBuild(BuildReport report)
    {
        if (report.summary.platform == BuildTarget.Android)
        {
            Debug.Log("=== Limpieza post-build de Android ===");
            RestoreWindowsFiles();
            Debug.Log("=== Limpieza completada ===");
        }
    }

    [MenuItem("Tools/Android/Validate Build Configuration")]
    public static void ValidateBuildConfiguration()
    {
        Debug.Log("=== Iniciando validación manual ===");
        ValidateAndroidSettings();
        ValidateManifest();
        ValidatePlugins();
        Debug.Log("=== Validación manual completada ===");
    }

    [MenuItem("Tools/Android/Fix Plugin Configuration")]
    public static void FixPluginConfiguration()
    {
        PlatformPluginConfigurator.ConfigureAllPlugins();
    }

    private static void ValidateAndroidSettings()
    {
        Debug.Log("Verificando configuración de Android:");
        
        string bundleId = PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android);
        Debug.Log($"  • Bundle ID: {bundleId}");
        
        Debug.Log($"  • Min SDK: {PlayerSettings.Android.minSdkVersion}");
        Debug.Log($"  • Target SDK: {PlayerSettings.Android.targetSdkVersion}");
        Debug.Log($"  • Script Backend: {PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android)}");
        Debug.Log($"  • Target Architectures: {PlayerSettings.Android.targetArchitectures}");
        
        // Advertencias
        if (string.IsNullOrEmpty(bundleId) || bundleId == "com.Company.ProductName")
        {
            Debug.LogWarning("⚠️ El Bundle ID no está configurado correctamente!");
        }
        
        if (PlayerSettings.Android.minSdkVersion < AndroidSdkVersions.AndroidApiLevel22)
        {
            Debug.LogWarning("⚠️ Min SDK es muy bajo. Se recomienda API Level 22 o superior.");
        }

        if (PlayerSettings.Android.targetArchitectures == AndroidArchitecture.None)
        {
            Debug.LogError("❌ No hay arquitecturas de destino seleccionadas!");
        }
    }

    private static void ValidateManifest()
    {
        string manifestPath = "Assets/Plugins/Android/AndroidManifest.xml";
        
        if (!File.Exists(manifestPath))
        {
            Debug.LogWarning("⚠️ No se encontró AndroidManifest.xml personalizado. Unity usará el predeterminado.");
            return;
        }

        Debug.Log("Verificando AndroidManifest.xml:");
        
        string manifestContent = File.ReadAllText(manifestPath);
        string bundleId = PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android);
        
        // Verificar que el package coincida con el bundle ID
        if (!manifestContent.Contains($"package=\"{bundleId}\""))
        {
            Debug.LogWarning($"⚠️ El package en AndroidManifest.xml no coincide con el Bundle ID de Unity!");
            Debug.LogWarning($"   Manifest: busca 'package=' en el archivo");
            Debug.LogWarning($"   Unity: {bundleId}");
        }
        
        // Verificar elementos importantes
        bool hasMainActivity = manifestContent.Contains("android.intent.action.MAIN");
        bool hasLauncher = manifestContent.Contains("android.intent.category.LAUNCHER");
        bool hasUnityActivity = manifestContent.Contains("UnityPlayerActivity");
        
        if (!hasMainActivity || !hasLauncher)
        {
            Debug.LogError("❌ El manifest no tiene configurado correctamente el LAUNCHER intent!");
        }
        else
        {
            Debug.Log("  ✓ Intent LAUNCHER configurado correctamente");
        }
        
        if (!hasUnityActivity)
        {
            Debug.LogWarning("⚠️ No se encontró UnityPlayerActivity en el manifest");
        }
        else
        {
            Debug.Log("  ✓ UnityPlayerActivity encontrada");
        }
    }

    private static void ValidatePlugins()
    {
        Debug.Log("Verificando plugins:");
        
        string[] problemPlugins = new string[]
        {
            "Assets/Plugins/x86_64/discord_game_sdk.dll",
            "Assets/Plugins/x86_64/discord_game_sdk.dylib",
            "Assets/Plugins/x86_64/discord_game_sdk.bundle"
        };

        int issuesFound = 0;
        
        foreach (string pluginPath in problemPlugins)
        {
            if (File.Exists(pluginPath))
            {
                PluginImporter importer = AssetImporter.GetAtPath(pluginPath) as PluginImporter;
                if (importer != null)
                {
                    bool enabledForAndroid = importer.GetCompatibleWithPlatform(BuildTarget.Android);
                    bool enabledForAny = importer.GetCompatibleWithAnyPlatform();
                    
                    if (enabledForAndroid || enabledForAny)
                    {
                        Debug.LogWarning($"⚠️ Plugin de Windows/Mac habilitado para Android: {pluginPath}");
                        Debug.LogWarning($"   Ejecuta 'Tools -> Android -> Fix Plugin Configuration' para corregir");
                        issuesFound++;
                    }
                }
            }
        }
        
        if (issuesFound == 0)
        {
            Debug.Log("  ✓ No se encontraron plugins problemáticos");
        }
        else
        {
            Debug.LogWarning($"  Se encontraron {issuesFound} plugins que pueden causar problemas en Android");
        }
    }

    [MenuItem("Tools/Android/Show Build Info")]
    public static void ShowBuildInfo()
    {
        Debug.Log("=== Información del Build de Android ===");
        Debug.Log($"Product Name: {PlayerSettings.productName}");
        Debug.Log($"Company Name: {PlayerSettings.companyName}");
        Debug.Log($"Bundle Identifier: {PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android)}");
        Debug.Log($"Version: {PlayerSettings.bundleVersion}");
        Debug.Log($"Bundle Version Code: {PlayerSettings.Android.bundleVersionCode}");
        Debug.Log($"Min SDK Version: {PlayerSettings.Android.minSdkVersion}");
        Debug.Log($"Target SDK Version: {PlayerSettings.Android.targetSdkVersion}");
        Debug.Log($"Scripting Backend: {PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android)}");
        Debug.Log($"Target Architectures: {PlayerSettings.Android.targetArchitectures}");
        Debug.Log($"Install Location: {PlayerSettings.Android.preferredInstallLocation}");
        Debug.Log($"Internet Access: {PlayerSettings.Android.forceInternetPermission}");
        Debug.Log($"Write Permission: {PlayerSettings.Android.forceSDCardPermission}");
    }

    /// <summary>
    /// Excluye archivos exclusivos de Windows (ffmpeg.exe) del build de Android
    /// renombrando la carpeta Windows/ temporalmente
    /// </summary>
    private static void ExcludeWindowsFilesFromAndroid()
    {
        Debug.Log("Excluyendo archivos de Windows del build de Android...");

        // Si la carpeta temporal existe (build anterior falló), restaurarla primero
        if (Directory.Exists(WINDOWS_TEMP_FOLDER))
        {
            Debug.LogWarning($"⚠️ Carpeta temporal de build anterior encontrada. Restaurando...");
            RestoreWindowsFiles();
        }

        if (Directory.Exists(WINDOWS_FOLDER))
        {
            try
            {
                // Renombrar Windows/ a Windows_TEMP_BUILD_EXCLUDED/
                Directory.Move(WINDOWS_FOLDER, WINDOWS_TEMP_FOLDER);
                
                // Renombrar también el .meta file
                string metaFile = WINDOWS_FOLDER + ".meta";
                string tempMetaFile = WINDOWS_TEMP_FOLDER + ".meta";
                if (File.Exists(metaFile))
                {
                    File.Move(metaFile, tempMetaFile);
                }

                Debug.Log($"  ✓ Carpeta Windows/ excluida del build (renombrada temporalmente)");
                Debug.Log($"  📦 Ahorro estimado: ~212 MB (ffmpeg.exe)");
                
                AssetDatabase.Refresh();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"❌ Error al excluir carpeta Windows/: {ex.Message}");
            }
        }
        else
        {
            Debug.Log($"  ℹ️ Carpeta Windows/ no encontrada (ya excluida o no existe)");
        }
    }

    /// <summary>
    /// Restaura la carpeta Windows/ después del build
    /// </summary>
    private static void RestoreWindowsFiles()
    {
        Debug.Log("Restaurando archivos de Windows...");

        if (Directory.Exists(WINDOWS_TEMP_FOLDER))
        {
            try
            {
                // Si la carpeta Windows/ existe por alguna razón, eliminarla primero
                if (Directory.Exists(WINDOWS_FOLDER))
                {
                    Directory.Delete(WINDOWS_FOLDER, true);
                    string existingMetaFile = WINDOWS_FOLDER + ".meta";
                    if (File.Exists(existingMetaFile))
                    {
                        File.Delete(existingMetaFile);
                    }
                }

                // Restaurar Windows_TEMP_BUILD_EXCLUDED/ a Windows/
                Directory.Move(WINDOWS_TEMP_FOLDER, WINDOWS_FOLDER);
                
                // Restaurar también el .meta file
                string tempMetaFile = WINDOWS_TEMP_FOLDER + ".meta";
                string metaFile = WINDOWS_FOLDER + ".meta";
                if (File.Exists(tempMetaFile))
                {
                    File.Move(tempMetaFile, metaFile);
                }

                Debug.Log($"  ✓ Carpeta Windows/ restaurada correctamente");
                
                AssetDatabase.Refresh();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"❌ Error al restaurar carpeta Windows/: {ex.Message}");
                Debug.LogError($"   IMPORTANTE: Renombra manualmente '{WINDOWS_TEMP_FOLDER}' a '{WINDOWS_FOLDER}'");
            }
        }
        else
        {
            Debug.Log($"  ℹ️ No hay carpeta temporal para restaurar");
        }
    }

    [MenuItem("Tools/Android/Restore Windows Files")]
    public static void ManualRestoreWindowsFiles()
    {
        RestoreWindowsFiles();
    }
}
