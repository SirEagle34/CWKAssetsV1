using UnityEngine;

public class SwapStrength : StatusState
{
    private int originalMaxHP;
    private float originalSTR;
    private float savedHPPct;

    // Balance divisor
    private const float HP_TO_STR_DIVISOR = 2f;

    protected override void OnEnable()
    {
        if (base.Target == null)
            return;

        // Save originals
        originalMaxHP = base.Target.MaxHP;
        originalSTR = base.Target.STR;

        // Save HP percentage
        savedHPPct = (originalMaxHP > 0)
            ? (float)base.Target.HP / originalMaxHP
            : 1f;

        // HP becomes old STR
        int newMaxHP = Mathf.Max(1, Mathf.RoundToInt(originalSTR));

        // STR becomes scaled HP
        float newSTR = Mathf.Max(
            1f,
            originalMaxHP / HP_TO_STR_DIVISOR
        );

        // Apply swap
        base.Target.MaxHP = newMaxHP;
        base.Target.STR = newSTR;

        // Restore proportional HP
        int newHP = Mathf.Clamp(
            Mathf.RoundToInt(newMaxHP * savedHPPct),
            1,
            newMaxHP
        );

        base.Target.SetHP(newHP);
    }

    protected override void OnDisable()
    {
        if (base.Target == null)
            return;

        // Save current swapped HP percent
        float currentPct = (base.Target.MaxHP > 0)
            ? (float)base.Target.HP / base.Target.MaxHP
            : 1f;

        // Restore original stats
        base.Target.MaxHP = originalMaxHP;
        base.Target.STR = originalSTR;

        // Restore proportional HP
        int restoredHP = Mathf.Clamp(
            Mathf.RoundToInt(originalMaxHP * currentPct),
            1,
            originalMaxHP
        );

        base.Target.SetHP(restoredHP);
    }
}