using UnityEngine;
using System.Collections;

/// <summary>
/// Verificador de idioma árabe para runtime.
/// Adjunta este script a un GameObject en la escena inicial para verificar
/// que el árabe está disponible cuando el juego inicia.
/// </summary>
public class ArabicLanguageRuntimeChecker : MonoBehaviour
{
    [Header("Auto-check on Start")]
    [Tooltip("Si está activado, verificará automáticamente al iniciar")]
    public bool checkOnStart = true;

    [Header("Debug Options")]
    [Tooltip("Si está activado, mostrará información detallada en consola")]
    public bool verboseLogging = true;

    private void Start()
    {
        if (checkOnStart)
        {
            StartCoroutine(CheckLanguagesAfterInit());
        }
    }

    private IEnumerator CheckLanguagesAfterInit()
    {
        // Esperar un frame para asegurar que el sistema Language esté inicializado
        yield return new WaitForEndOfFrame();
        
        CheckArabicLanguage();
    }

    [ContextMenu("Check Arabic Language Now")]
    public void CheckArabicLanguage()
    {
        if (verboseLogging)
        {
            Debug.Log("=== Arabic Language Runtime Check ===");
        }

        try
        {
            // Obtener idiomas disponibles
            string[] languages = Language.GetLanguages();
            
            if (languages == null || languages.Length == 0)
            {
                Debug.LogError("[Arabic Check] No languages detected! Language system may not be initialized.");
                Debug.LogError("[Arabic Check] Make sure Language.Init() is called during game startup.");
                return;
            }

            // Verificar si árabe está disponible
            bool arabicFound = System.Array.Exists(languages, lang => lang == "AR");

            if (arabicFound)
            {
                Debug.Log("<color=green>[Arabic Check] ✓ SUCCESS: Arabic (AR) is available!</color>");
                
                if (verboseLogging)
                {
                    Debug.Log($"[Arabic Check] Total languages available: {languages.Length}");
                    Debug.Log($"[Arabic Check] Languages: {string.Join(", ", languages)}");
                }
                
                // Intentar obtener una traducción de prueba
                TestArabicTranslation();
            }
            else
            {
                Debug.LogWarning("[Arabic Check] ✗ Arabic (AR) is NOT in the available languages list!");
                Debug.LogWarning($"[Arabic Check] Available languages: {string.Join(", ", languages)}");
                Debug.LogWarning("[Arabic Check] Possible issues:");
                Debug.LogWarning("  1. AR_Sheet1.bytes might be missing from Resources/languages/");
                Debug.LogWarning("  2. The file might have incorrect naming");
                Debug.LogWarning("  3. The .meta file might be corrupted");
            }

            // Mostrar idioma actual
            LanguageCode currentLang = Language.CurrentLanguage();
            Debug.Log($"[Arabic Check] Current language: {currentLang}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[Arabic Check] Error checking languages: {ex.Message}");
            Debug.LogError($"[Arabic Check] Stack trace: {ex.StackTrace}");
        }
    }

    private void TestArabicTranslation()
    {
        try
        {
            // Guardar idioma actual
            LanguageCode originalLang = Language.CurrentLanguage();
            
            // Cambiar a árabe temporalmente
            bool switched = Language.SwitchLanguage("AR");
            
            if (switched)
            {
                // Probar algunas traducciones
                string[] testKeys = new string[]
                {
                    "!!GACHA_SPECIAL",
                    "!!ACCOUNTNAME",
                    "!!HEALTH",
                    "!!FORFEIT"
                };

                Debug.Log("[Arabic Check] Testing translations:");
                foreach (string key in testKeys)
                {
                    string translation = Language.Get(key);
                    if (!string.IsNullOrEmpty(translation))
                    {
                        Debug.Log($"  {key} = {translation}");
                    }
                }

                // Restaurar idioma original
                Language.SwitchLanguage(originalLang.ToString());
            }
            else
            {
                Debug.LogWarning("[Arabic Check] Could not switch to Arabic language for testing");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[Arabic Check] Error testing translation: {ex.Message}");
        }
    }

    [ContextMenu("Switch to Arabic")]
    public void SwitchToArabic()
    {
        bool success = Language.SwitchLanguage("AR");
        if (success)
        {
            Debug.Log("<color=green>[Arabic Check] Successfully switched to Arabic!</color>");
        }
        else
        {
            Debug.LogError("[Arabic Check] Failed to switch to Arabic language");
        }
    }

    [ContextMenu("List All Languages")]
    public void ListAllLanguages()
    {
        try
        {
            string[] languages = Language.GetLanguages();
            Debug.Log($"=== All Available Languages ({languages.Length}) ===");
            for (int i = 0; i < languages.Length; i++)
            {
                Debug.Log($"{i + 1}. {languages[i]}");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error listing languages: {ex.Message}");
        }
    }
}
