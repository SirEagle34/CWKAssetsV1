public class AreaMagicSpikeAboveHealthEachTurn : OnStartTurn
{
    public override bool OnEnable()
    {
        if (base.Owner.HPPct < base.Val1Pct)
        {
            return false;
        }
        bool result = false;
        foreach (LaneState lane in base.Owner.Lane.ThisAndAdjacentLanes)
        {
            if (lane.Creature != null)
            {
                ApplyStatus(lane.Creature, StatusEnum.MagicSpike, base.Val2);
                result = true;
            }
        }
        return result;
    }
}
