public class TeamBrillianceEachTurn : OnStartTurn
{
    public override bool OnEnable()
    {
        foreach (LaneState lane in base.Owner.Owner.Lanes)
        {
            if (lane.Creature != null)
            {
                ApplyStatus(lane.Creature, StatusEnum.Brilliant, base.Val1);
            }
        }
        return true;
    }
}
