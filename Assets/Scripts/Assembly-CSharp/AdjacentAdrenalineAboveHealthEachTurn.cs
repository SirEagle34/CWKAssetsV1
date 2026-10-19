public class AdjacentAdrenalineAboveHealthEachTurn : OnStartTurn
{
    private int turnCount = 0;

    public override bool OnEnable()
    {
        turnCount++;

        // Her 2 turda bir çalış
        if (turnCount % 2 != 0)
        {
            return false;
        }

        if (base.Owner.HPPct < base.Val1Pct)
        {
            return false;
        }

        bool result = false;

        foreach (LaneState lane in base.Owner.Lane.AdjacentLanes)
        {
            if (lane.Creature != null)
            {
                ApplyStatus(
                    lane.Creature,
                    StatusEnum.Adrenaline,
                    base.Val2
                );

                result = true;
            }
        }

        return result;
    }
}