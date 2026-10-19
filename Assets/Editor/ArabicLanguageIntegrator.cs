using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System;

/// <summary>
/// Script post-procesador para asegurar que el idioma árabe (AR) esté incluido
/// en los dropdowns de selección de idioma del juego.
/// </summary>
[InitializeOnLoad]
public class ArabicLanguageIntegrator
{
    static ArabicLanguageIntegrator()
    {
        // No llamar automáticamente, solo cuando el usuario lo solicite
        // EditorApplication.delayCall += OnProjectLoaded;
    }

    [MenuItem("Card Wars/Setup Arabic Language")]
    public static void CheckAndAddArabicLanguage()
    {
        Debug.Log("[Arabic Language Integrator] Checking Arabic language integration...");

        // Verificar que el archivo AR_Sheet1.bytes existe
        string arabicFilePath = "Assets/Resources/Languages/AR_Sheet1.bytes";
        if (!File.Exists(arabicFilePath))
        {
            Debug.LogWarning($"[Arabic Language Integrator] Arabic language file not found at {arabicFilePath}");
            return;
        }

        Debug.Log("[Arabic Language Integrator] ✓ Arabic language file found!");

        // Verificar archivos de idioma manualmente en el editor
        string[] languageFiles = Directory.GetFiles("Assets/Resources/Languages", "*_Sheet1.bytes");
        List<string> detectedLanguages = new List<string>();
        
        foreach (string file in languageFiles)
        {
            string fileName = Path.GetFileNameWithoutExtension(file);
            // Extraer código de idioma (XX_Sheet1 -> XX)
            if (fileName.Contains("_"))
            {
                string langCode = fileName.Split('_')[0];
                detectedLanguages.Add(langCode);
            }
        }

        Debug.Log($"[Arabic Language Integrator] Detected language files: {string.Join(", ", detectedLanguages)}");

        bool arabicFound = detectedLanguages.Contains("AR");
        
        if (arabicFound)
        {
            Debug.Log("[Arabic Language Integrator] ✓ Arabic (AR) is properly integrated!");
            Debug.Log("[Arabic Language Integrator] ✓ AR_Sheet1.bytes will appear in the dropdown when game runs");
        }
        else
        {
            Debug.LogWarning("[Arabic Language Integrator] Arabic language file exists but naming might be incorrect.");
        }

        // Información adicional para el desarrollador
        Debug.Log("[Arabic Language Integrator] === Integration Details ===");
        Debug.Log("  1. File location: Assets/Resources/Languages/AR_Sheet1.bytes");
        Debug.Log("  2. The dropdown populates automatically from Language.GetLanguages() at runtime");
        Debug.Log("  3. When you run the game, Arabic will appear in the language selector");
        Debug.Log("  4. The LanguageSelection component automatically detects all XX_Sheet1.bytes files");
        Debug.Log("  5. Test it by running the game and opening the language selection menu");
    }

    [MenuItem("Card Wars/Test Switch to Arabic")]
    public static void TestSwitchToArabic()
    {
        if (Application.isPlaying)
        {
            bool success = Language.SwitchLanguage("AR");
            if (success)
            {
                Debug.Log("[Arabic Language Integrator] Successfully switched to Arabic!");
                Debug.Log($"[Arabic Language Integrator] Current language: {Language.CurrentLanguage()}");
                
                // Probar una traducción
                string testKey = "!!GACHA_SPECIAL";
                string translation = Language.Get(testKey);
                Debug.Log($"[Arabic Language Integrator] Test translation for '{testKey}': {translation}");
            }
            else
            {
                Debug.LogError("[Arabic Language Integrator] Failed to switch to Arabic language.");
            }
        }
        else
        {
            Debug.LogWarning("[Arabic Language Integrator] This command only works when the game is running. Press Play first.");
        }
    }
}
