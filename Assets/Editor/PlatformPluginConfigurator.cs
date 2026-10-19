using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Configura automáticamente los plugins para cada plataforma.
/// Excluye archivos de Windows (.dll), macOS (.dylib, .bundle) y Linux (.so) 
/// de las builds que no correspondan.
/// </summary>
public class PlatformPluginConfigurator : AssetPostprocessor
{
    private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        foreach (string assetPath in importedAssets)
        {
            if (assetPath.StartsWith("Assets/Plugins/"))
            {
                ConfigurePlugin(assetPath);
            }
        }
    }

    [MenuItem("Tools/Configure All Platform Plugins")]
    public static void ConfigureAllPlugins()
    {
        string pluginsPath = "Assets/Plugins";
        
        if (!Directory.Exists(pluginsPath))
        {
            Debug.LogWarning("No se encontró la carpeta Plugins");
            return;
        }

        int configured = 0;
        string[] allFiles = Directory.GetFiles(pluginsPath, "*.*", SearchOption.AllDirectories);
        
        foreach (string filePath in allFiles)
        {
            string assetPath = filePath.Replace("\\", "/");
            if (ConfigurePlugin(assetPath))
            {
                configured++;
            }
        }
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log($"Configuración de plugins completada: {configured} archivos configurados");
    }

    private static bool ConfigurePlugin(string assetPath)
    {
        // Ignorar archivos .meta y otros que no son plugins
        if (assetPath.EndsWith(".meta") || assetPath.EndsWith(".cs") || assetPath.EndsWith(".java"))
        {
            return false;
        }

        string extension = Path.GetExtension(assetPath).ToLower();
        string fileName = Path.GetFileName(assetPath).ToLower();
        
        // Determinar el tipo de plugin
        PluginImporter pluginImporter = AssetImporter.GetAtPath(assetPath) as PluginImporter;
        
        if (pluginImporter == null)
        {
            return false;
        }

        bool changed = false;

        // Configurar plugins de Windows (.dll)
        if (extension == ".dll")
        {
            changed = ConfigureWindowsPlugin(pluginImporter, assetPath);
        }
        // Configurar plugins de macOS (.dylib, .bundle)
        else if (extension == ".dylib" || extension == ".bundle")
        {
            changed = ConfigureMacPlugin(pluginImporter, assetPath);
        }
        // Configurar plugins de Linux (.so)
        else if (extension == ".so")
        {
            changed = ConfigureLinuxPlugin(pluginImporter, assetPath);
        }

        if (changed)
        {
            pluginImporter.SaveAndReimport();
            Debug.Log($"Plugin configurado: {assetPath}");
        }

        return changed;
    }

    private static bool ConfigureWindowsPlugin(PluginImporter pluginImporter, string assetPath)
    {
        bool is64Bit = assetPath.Contains("x86_64") || assetPath.Contains("x64");
        
        // Desactivar para todas las plataformas primero
        pluginImporter.SetCompatibleWithAnyPlatform(false);
        
        // Activar solo para Windows Editor y Standalone
        pluginImporter.SetCompatibleWithEditor(true);
        pluginImporter.SetEditorData("OS", "Windows");
        pluginImporter.SetEditorData("CPU", is64Bit ? "x86_64" : "x86");
        
        pluginImporter.SetCompatibleWithPlatform(BuildTarget.StandaloneWindows, !is64Bit);
        pluginImporter.SetCompatibleWithPlatform(BuildTarget.StandaloneWindows64, is64Bit);
        
        // IMPORTANTE: Desactivar explícitamente para Android e iOS
        pluginImporter.SetCompatibleWithPlatform(BuildTarget.Android, false);
        pluginImporter.SetCompatibleWithPlatform(BuildTarget.iOS, false);
        
        Debug.Log($"[Windows DLL] {Path.GetFileName(assetPath)} - Editor: Windows, Standalone: {(is64Bit ? "Win64" : "Win32")}, Android/iOS: Desactivado");
        
        return true;
    }

    private static bool ConfigureMacPlugin(PluginImporter pluginImporter, string assetPath)
    {
        // Desactivar para todas las plataformas primero
        pluginImporter.SetCompatibleWithAnyPlatform(false);
        
        // Activar solo para macOS Editor y Standalone
        pluginImporter.SetCompatibleWithEditor(true);
        pluginImporter.SetEditorData("OS", "OSX");
        pluginImporter.SetEditorData("CPU", "x86_64");
        
        pluginImporter.SetCompatibleWithPlatform(BuildTarget.StandaloneOSX, true);
        
        // Desactivar para Windows, Android e iOS
        pluginImporter.SetCompatibleWithPlatform(BuildTarget.StandaloneWindows, false);
        pluginImporter.SetCompatibleWithPlatform(BuildTarget.StandaloneWindows64, false);
        pluginImporter.SetCompatibleWithPlatform(BuildTarget.Android, false);
        pluginImporter.SetCompatibleWithPlatform(BuildTarget.iOS, false);
        
        Debug.Log($"[macOS Plugin] {Path.GetFileName(assetPath)} - Editor: macOS, Standalone: macOS, Otros: Desactivado");
        
        return true;
    }

    private static bool ConfigureLinuxPlugin(PluginImporter pluginImporter, string assetPath)
    {
        // Verificar si es para Android (archivos en carpeta Android) o Linux desktop
        bool isAndroid = assetPath.Contains("/Android/") || assetPath.Contains("\\Android\\");
        
        // Desactivar para todas las plataformas primero
        pluginImporter.SetCompatibleWithAnyPlatform(false);
        
        if (isAndroid)
        {
            // Plugin de Android
            pluginImporter.SetCompatibleWithPlatform(BuildTarget.Android, true);
            
            // Desactivar para otras plataformas
            pluginImporter.SetCompatibleWithEditor(false);
            pluginImporter.SetCompatibleWithPlatform(BuildTarget.StandaloneWindows, false);
            pluginImporter.SetCompatibleWithPlatform(BuildTarget.StandaloneWindows64, false);
            pluginImporter.SetCompatibleWithPlatform(BuildTarget.StandaloneOSX, false);
            pluginImporter.SetCompatibleWithPlatform(BuildTarget.iOS, false);
            
            Debug.Log($"[Android .so] {Path.GetFileName(assetPath)} - Solo Android");
        }
        else
        {
            // Plugin de Linux desktop
            pluginImporter.SetCompatibleWithEditor(true);
            pluginImporter.SetEditorData("OS", "Linux");
            pluginImporter.SetEditorData("CPU", "x86_64");
            
            pluginImporter.SetCompatibleWithPlatform(BuildTarget.StandaloneLinux64, true);
            
            // Desactivar para otras plataformas
            pluginImporter.SetCompatibleWithPlatform(BuildTarget.StandaloneWindows, false);
            pluginImporter.SetCompatibleWithPlatform(BuildTarget.StandaloneWindows64, false);
            pluginImporter.SetCompatibleWithPlatform(BuildTarget.StandaloneOSX, false);
            pluginImporter.SetCompatibleWithPlatform(BuildTarget.Android, false);
            pluginImporter.SetCompatibleWithPlatform(BuildTarget.iOS, false);
            
            Debug.Log($"[Linux .so] {Path.GetFileName(assetPath)} - Editor: Linux, Standalone: Linux64, Otros: Desactivado");
        }
        
        return true;
    }
}
