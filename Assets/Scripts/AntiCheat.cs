using UnityEngine;
using UnityEngine.SceneManagement;

public class AntiCheat : MonoBehaviour
{
    private int LastGems;
    private int LastCoins;
    private int LastRankXP;
    private float checkTimer = 0f;
    private float delayTimer = 0f;
    private bool delayComplete = false;
    private PlayerSaveData saveData;

    private const int GEMS_LIMIT = 30000;
    private const int COINS_LIMIT = 50000;
    private const int RANKXP_LIMIT = 500000;
    private const float CHECK_INTERVAL = 5f;
    private const float START_DELAY = 30f;

    void Start()
    {
        saveData = Singleton<PlayerInfoScript>.Instance?.SaveData;
        if (saveData == null)
        {
            Debug.LogError("[AntiCheat] SaveData is null in Start!");
            return;
        }

        // Pobranie początkowych wartości
        LastGems = saveData.HardCurrency;
        LastCoins = saveData.SoftCurrency;
        LastRankXP = saveData.RankXP;

        Debug.Log($"[AntiCheat] Initial values -> Gems: {LastGems}, Coins: {LastCoins}, RankXP: {LastRankXP}");

        // Jeśli scena to FrontEndScene, czekamy 30s przed startem sprawdzania
        if (SceneManager.GetActiveScene().name == "FrontEndScene")
        {
            delayComplete = false;
            delayTimer = START_DELAY;
            Debug.Log($"[AntiCheat] Delaying checks for {START_DELAY} seconds in FrontEndScene...");
        }
        else
        {
            delayComplete = true;
        }
    }

    void Update()
    {
        if (saveData == null)
        {
            saveData = Singleton<PlayerInfoScript>.Instance?.SaveData;
            if (saveData == null) return;
        }

        // Jeśli mamy opóźnienie, odliczamy
        if (!delayComplete)
        {
            delayTimer -= Time.deltaTime;
            if (delayTimer <= 0f)
            {
                delayComplete = true;
                Debug.Log("[AntiCheat] Delay finished. Starting regular checks...");
            }
            return; // Nie sprawdzamy wartości do zakończenia opóźnienia
        }

        // Liczymy czas do kolejnego sprawdzenia
        checkTimer += Time.deltaTime;
        if (checkTimer >= CHECK_INTERVAL)
        {
            checkTimer = 0f;

            int gemDiff = saveData.HardCurrency - LastGems;
            int coinDiff = saveData.SoftCurrency - LastCoins;
            int xpDiff = saveData.RankXP - LastRankXP;

            Debug.Log($"[AntiCheat] Checking -> Gems Δ{gemDiff}, Coins Δ{coinDiff}, RankXP Δ{xpDiff}");

            if (gemDiff > GEMS_LIMIT || coinDiff > COINS_LIMIT || xpDiff > RANKXP_LIMIT)
            {
                Debug.LogWarning($"[AntiCheat] Cheating detected! Gems Δ{gemDiff}, Coins Δ{coinDiff}, RankXP Δ{xpDiff}");
                PlayerPrefs.SetInt("Ban", 1);
                PlayerPrefs.Save(); // Natychmiastowy zapis
            }
            else
            {
                // Aktualizacja wartości referencyjnych
                LastGems = saveData.HardCurrency;
                LastCoins = saveData.SoftCurrency;
                LastRankXP = saveData.RankXP;
            }
        }
    }
}