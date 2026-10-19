using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Herramienta de editor para verificar y probar el sistema de localización de Card Wars Kingdom
/// Incluye verificación del idioma árabe (AR)
/// </summary>
public class LocalizationEditorTool : EditorWindow
{
    private Vector2 scrollPosition;
    private string testKey = "!!GACHA_SPECIAL";
    private string selectedLanguage = "EN";

    [MenuItem("Card Wars/Localization Tool")]
    public static void ShowWindow()
    {
        GetWindow<LocalizationEditorTool>("Localization Tool");
    }

    private void OnGUI()
    {
        GUILayout.Label("Card Wars Kingdom - Localization Tool", EditorStyles.boldLabel);
        GUILayout.Space(10);

        // Sección de idiomas disponibles
        GUILayout.Label("Available Languages:", EditorStyles.boldLabel);
        
        if (Language.settings == null)
        {
            EditorGUILayout.HelpBox("LocalizationSettings not found! Initialize the Language system first.", MessageType.Error);
            return;
        }

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));
        
        string[] languages = Language.GetLanguages();
        if (languages != null && languages.Length > 0)
        {
            EditorGUILayout.BeginVertical("box");
            foreach (string lang in languages)
            {
                EditorGUILayout.BeginHorizontal();
                
                // Indicador visual si es el idioma actual
                string currentLangCode = Language.CurrentLanguage().ToString();
                if (currentLangCode == lang)
                {
                    GUILayout.Label("→", GUILayout.Width(20));
                }
                else
                {
                    GUILayout.Space(20);
                }
                
                // Nombre del idioma con indicador especial para árabe
                if (lang == "AR")
                {
                    GUILayout.Label(lang + " (Arabic - العربية) ✓", EditorStyles.boldLabel);
                }
                else
                {
                    GUILayout.Label(lang);
                }
                
                // Botón para cambiar de idioma
                if (GUILayout.Button("Switch", GUILayout.Width(60)))
                {
                    Language.SwitchLanguage(lang);
                    Debug.Log($"Switched to language: {lang}");
                }
                
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
            
            // Verificar si árabe está disponible
            bool arabicAvailable = System.Array.Exists(languages, lang => lang == "AR");
            if (arabicAvailable)
            {
                EditorGUILayout.HelpBox("✓ Arabic (AR) language file detected successfully!", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("✗ Arabic (AR) language file not found. Make sure AR_Sheet1.bytes exists in Resources/languages/", MessageType.Warning);
            }
        }
        else
        {
            EditorGUILayout.HelpBox("No languages found. Check your language files in Resources/languages/", MessageType.Warning);
        }
        
        EditorGUILayout.EndScrollView();

        GUILayout.Space(20);

        // Sección de prueba de traducción
        GUILayout.Label("Test Translation:", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");
        
        testKey = EditorGUILayout.TextField("Entry Key:", testKey);
        
        if (GUILayout.Button("Get Translation"))
        {
            string translation = Language.Get(testKey);
            Debug.Log($"Key: {testKey}\nTranslation: {translation}");
        }
        
        EditorGUILayout.EndVertical();

        GUILayout.Space(10);

        // Botón de información
        if (GUILayout.Button("Show Info"))
        {
            ShowLocalizationInfo();
        }

        // Botón para refrescar
        if (GUILayout.Button("Refresh Languages"))
        {
            RefreshLanguages();
        }
    }

    private void ShowLocalizationInfo()
    {
        Debug.Log("=== Card Wars Kingdom Localization Info ===");
        Debug.Log($"Current Language: {Language.CurrentLanguage()}");
        Debug.Log($"Default Language: {Language.settings.defaultLangCode}");
        Debug.Log($"Use System Language: {Language.settings.useSystemLanguagePerDefault}");
        
        string[] langs = Language.GetLanguages();
        Debug.Log($"Total Languages Available: {langs.Length}");
        Debug.Log("Languages: " + string.Join(", ", langs));
        
        bool hasArabic = System.Array.Exists(langs, lang => lang == "AR");
        Debug.Log($"Arabic Available: {hasArabic}");
    }

    private void RefreshLanguages()
    {
        // Forzar recarga
        Resources.UnloadUnusedAssets();
        System.GC.Collect();
        Debug.Log("Languages refreshed. Reopen this window to see changes.");
    }
}
