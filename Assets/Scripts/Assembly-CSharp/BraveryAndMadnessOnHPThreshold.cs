using UnityEngine;
public class BraveryAndMadnessOnHPThreshold : OnFriendlyDamaged
{
    private bool triggered = false;

    public override bool OnEnable()
    {
        if (triggered)
            return false;

        float previousHpPct = (float)(Attacked.HP + (int)Damage) / (float)Attacked.MaxHP;

        if (Attacked != base.Owner &&
            Attacked.HPPct < base.Val1Pct &&
            previousHpPct >= base.Val1Pct)
        {
            ApplyStatus(base.Owner, StatusEnum.Bravery, base.Val2);
            ApplyStatus(base.Owner, StatusEnum.Madness, base.Val2);

            triggered = true; // 🔥 bir daha çalışmaz
            return true;
        }

        return false;
    }
}