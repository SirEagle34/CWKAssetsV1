public class GrantHealHasteDefIfOppHasMoreCards : OnStartTurn
{
    public override bool OnEnable()
    {
        // skip if creature is dead
        if (base.Owner == null || base.Owner.HP <= 0)
        {
            return false;
        }

        if (base.Owner.Owner.Opponent.Hand.Count > base.Owner.Owner.Hand.Count)
        {
            ApplyStatus(base.Owner, StatusEnum.FortifyDEF, base.Val1);
            ApplyStatus(base.Owner, StatusEnum.Adrenaline, base.Val2);
            base.Owner.Heal(base.Val3Pct);
            return true;
        }
        return false;
    }
}
