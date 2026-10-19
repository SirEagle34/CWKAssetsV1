public class AreaRemoveBlindBerserkBurnEachTurn : OnStartTurn
{
    public override bool OnEnable()
    {
       	bool result = false; 
        foreach (LaneState thisAndAdjacentLane in base.Owner.Lane.ThisAndAdjacentLanes)
        {
            if (thisAndAdjacentLane.Creature != null)
            {
				CancelStatus(thisAndAdjacentLane.Creature, StatusEnum.Berserk);
				CancelStatus(thisAndAdjacentLane.Creature, StatusEnum.Burned);
            	CancelStatus(thisAndAdjacentLane.Creature, StatusEnum.Blind);
				thisAndAdjacentLane.Creature.Heal(base.Val1Pct);
				result = true;
            }
        }
		return result;
    }
}