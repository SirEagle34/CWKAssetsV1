public class DefAtkHealEachTurnWhenAlone : OnStartTurn
{
    public override bool OnEnable()
    {
        // skip if dead
        if (base.Owner == null || base.Owner.HP <= 0)
            return false;

        int num = 0;
        foreach (LaneState lane in base.Owner.Owner.Lanes)
        {
            if (lane.Creature != null)
            {
                num++;
            }
        }
        if (num == 1)
        {
            ApplyStatus(base.Owner, StatusEnum.DefenseBoost, base.Val1);
            ApplyStatus(base.Owner, StatusEnum.AttackSpike, base.Val2);
            base.Owner.Heal(base.Val3Pct);
            return true;
        }
        return false;
    }
}
