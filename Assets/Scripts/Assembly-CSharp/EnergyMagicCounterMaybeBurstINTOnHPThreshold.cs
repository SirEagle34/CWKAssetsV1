public class EnergyMagicCounterMaybeBurstINTOnHPThreshold : OnFriendlyAttacked  
{
    public override bool OnEnable()
    {
        if (Attacked != null && Attacked.HPPct < base.Val1Pct)
        {
            ApplyStatus(Attacked, StatusEnum.MagicCounter, base.Val2);
            ApplyStatus(Attacked, StatusEnum.BurstINT, base.Val3);
            base.Owner.Owner.AddActionPoints(base.Val4);
            return true;
        }
        return false;
    }
}