public class InflictFrozenAndDealDamageHPThreshold : OnHPThreshold
{
    public override bool OnEnable()
    {
        bool result = false;
        if (base.Owner.HPPct < base.Val1Pct)
        {
            foreach (LaneState lane in base.Owner.Owner.Opponent.Lanes)
            {
                if (lane.Creature != null)
                {
                    ApplyStatus(lane.Creature, StatusEnum.DirectDamage, base.Val2);
                    ApplyStatus(lane.Creature, StatusEnum.Frozen, base.Val3);
                    result = true;
                }
            }
            return result;
        }
        return result;
    }
}