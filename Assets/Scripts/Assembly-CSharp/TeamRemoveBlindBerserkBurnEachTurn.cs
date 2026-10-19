public class TeamRemoveBlindBerserkBurnEachTurn : OnStartTurn
{
    public override bool OnEnable()
    {
       	bool result = false; 
        foreach (LaneState lane in base.Owner.Owner.Lanes)
        {
            if (lane.Creature != null)
            {
				CancelStatus(lane.Creature, StatusEnum.Berserk);
				CancelStatus(lane.Creature, StatusEnum.Burned);
            	CancelStatus(lane.Creature, StatusEnum.Blind);
				lane.Creature.Heal(base.Val1Pct);
				result = true;
            }
        }
		return result;
    }
}