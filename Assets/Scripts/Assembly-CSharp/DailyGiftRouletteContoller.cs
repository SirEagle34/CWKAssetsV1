using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DailyGiftRouletteContoller : MonoBehaviour
{
    public Transform wheel;

    public Transform itemsRoot;

    private Transform[] slots;

    private DailyGiftSlotUI[] slotUIs;

    public float spinTime = 5f;

    public int extraSpins = 6;

    public int numSlots = 12;
    private const long COOLDOWN = 86400;
    public int ResetCost = 1250;
    private DailyRouletteGiftData[] cachedGifts;

    private bool spinning;

    private bool slotsCached;

    private DailyRouletteGiftData reward;
    private int rewardIndex;

    public DateTime NextRouletteTime;

    private bool isReady;
    private bool canSpin = false;

	public UITweenController DailyRouletteHide;
    public GameObject rouletteFade;

    public AudioSource sfxSource;
    public AudioClip claimGiftClip;

    public static System.Action OnLoaded;

    IEnumerator Start()
    {
        isReady = true;

        yield return DailyGiftDataManager.Instance.LoadDailyGiftData();

        cachedGifts = DailyGiftDataManager.Instance.GetAllGifts();

        if (cachedGifts == null || cachedGifts.Length == 0)
        {
            Debug.LogError("NO GIFTS LOADED!");
            yield break;
        }

        CacheSlots();
        canSpin = true;
    }

    public void OnClickSpin()
    {
        if (!isReady)
        {
            Debug.LogWarning("Not ready yet");
            return;
        }

        Spin();
    }

    public void Spin()
    {
        if (spinning)
            return;

        var player = Singleton<PlayerInfoScript>.Instance;

        if (!DailyGiftDataManager.Instance.Loaded)
            return;

        if (!player.CanSpinDailyGift())
        {
            SimplePopupController.Instance.ShowMessage(
                "Daily Gift",
                "Come back in 24 hours!",
                null
            );
            return;
        }

        player.SaveData.LastDailySpinTime =
            System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        var result = DailyGiftDataManager.Instance.PickGift();

        reward = result.reward;
        rewardIndex = Mathf.Clamp(result.index, 0, cachedGifts.Length - 1);

        StartCoroutine(SpinRoutine());
    }

    private void Awake()
    {
        //CacheSlots();
    }

    Transform FindDeep(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
                return child;

            Transform result = FindDeep(child, name);
            if (result != null)
                return result;
        }
        return null;
    }
    
    void CacheSlots()
    {
        
        if (slotsCached) return;

        if (itemsRoot == null)
        {
            Debug.LogError("ItemsRoot missing!");
            return;
        }

        slotUIs = new DailyGiftSlotUI[numSlots];

        for (int i = 0; i < numSlots; i++)
        {
            string index = (i + 1).ToString("00");
            string itemName = $"Item_{i + 1:D2}";

            Transform slot = FindDeep(itemsRoot, itemName);

            if (slot == null)
            {
                Debug.LogError("Missing slot: " + itemName);
                continue;
            }

            DailyGiftSlotUI ui = slot.GetComponent<DailyGiftSlotUI>();

            if (ui == null)
            {
                Debug.LogError("Missing DailyGiftSlotUI on " + itemName);
                continue;
            }

            slotUIs[i] = ui;

            var all = cachedGifts;

            if (i >= all.Length) continue;

            DailyRouletteGiftData data = all[i];

            if (data != null)
                ui.Bind(data);
        }

        slotsCached = true;
    }

    IEnumerator SpinRoutine()
    {
        spinning = true;

        if (cachedGifts == null || cachedGifts.Length == 0)
        {
            Debug.LogError("cachedGifts empty!");
            yield break;
        }

        rewardIndex = Mathf.Clamp(rewardIndex, 0, cachedGifts.Length - 1);

        float slice = 360f / cachedGifts.Length;

        float targetAngle =
            (360f * extraSpins) +
            (rewardIndex * slice);

        float startAngle = wheel.eulerAngles.z;

        float timer = 0f;

        while (timer < spinTime)
        {
            timer += Time.deltaTime;

            float t = timer / spinTime;
            t = 1f - Mathf.Pow(1f - t, 3f);

            float angle = Mathf.Lerp(startAngle, -targetAngle, t);
            angle = Mathf.Repeat(angle, 360f);

            wheel.localRotation = Quaternion.Euler(0f, 0f, angle);

            yield return null;
        }

        wheel.localRotation = Quaternion.Euler(0f, 0f, -targetAngle);

        StartCoroutine(PlayClaimSound());

        FinishSpin();

        spinning = false;
    }

    IEnumerator PlayClaimSound()
    {
        yield return new WaitForSeconds(0.1f);
        Singleton<SLOTAudioManager>.Instance.PlaySound("ui/UI_Claim_Gift");
    }

    void FinishSpin()
    {
        if (reward == null || rewardIndex < 0)
        {
            Debug.LogError("Invalid spin result!");
            spinning = false;
            return;
        }

        Debug.Log("Reward: " + reward.Name);

        GrantReward(reward);

        StartCoroutine(HideRoulette());
    }

    IEnumerator HideRoulette()
    {
        canSpin = false;

        DailyRouletteHide.Play();

        if (rouletteFade != null)
            TweenAlpha.Begin(rouletteFade, 0.2f, 0f);

        yield return new WaitForSeconds(0.35f);

        wheel.gameObject.SetActive(false);

        ResetWheelState();
    }

    void ResetWheelState()
    {
        wheel.localScale = Vector3.one;
        wheel.localRotation = Quaternion.identity;

        wheel.gameObject.SetActive(true);

        canSpin = true;
        spinning = false;
    }
    
    void GrantReward(DailyRouletteGiftData gift)
    {
        PlayerSaveData saveData =
            Singleton<PlayerInfoScript>.Instance.SaveData;

        switch (gift.Type)
        {
            case "Coins":
                saveData.SoftCurrency += gift.Quantity;
                break;

            case "Gems":
                saveData.HardCurrency += gift.Quantity;
                break;

            case "EvoCurrency":
                saveData.EvolutionShards += gift.Quantity;
                break;

            case "ActionCard":
                string actionCardID =
                    DailyRouteSpellPoolManager.PickSpell(gift.Pool);

                if (!string.IsNullOrEmpty(actionCardID))
                {
                    CardItem cardItem = new CardItem(actionCardID);

                    saveData.AddExCard(cardItem);
                }

                break;
                
            case "Creature":
                string creatureID =
                    DailyRouteCreaturePoolManager.PickCreature(gift.Pool);

                if (!string.IsNullOrEmpty(creatureID))
                {
                    CreatureItem creatureItem =
                        new CreatureItem(creatureID);

                    creatureItem.Xp =
                        creatureItem.XPTable.GetXpToReachLevel(1);

                    saveData.AddCreature(creatureItem);
                }
                break;
        }

        Singleton<PlayerInfoScript>.Instance.Save();
    }

    public bool ResetRouletteWithPvpCurrency()
    {
        var saveData = Singleton<PlayerInfoScript>.Instance.SaveData;

        if (saveData.PvPCurrency < ResetCost)
            return false;

        saveData.PvPCurrency -= ResetCost;

        saveData.LastDailySpinTime = DateTimeOffset.MinValue.ToUnixTimeSeconds();

        Singleton<PlayerInfoScript>.Instance.Save();

        return true;
    }

    public void TryResetWithPopup()
    {
        SimplePopupController.Instance.ShowPrompt(
            "Reset Roulette",
            "Spend 1.250 PvP Currency to reset the 24h cooldown?",
            OnConfirmReset,
            OnCancelReset,
            "Reset",
            "Cancel"
        );
    }

    private void OnConfirmReset()
    {
        bool success = ResetRouletteWithPvpCurrency();

        if (!success)
        {
            SimplePopupController.Instance.ShowMessage(
                "Error",
                "Not enough PvP Currency"
            );
        }
    }

    private void OnCancelReset()
    {
        // nothing
    }
}