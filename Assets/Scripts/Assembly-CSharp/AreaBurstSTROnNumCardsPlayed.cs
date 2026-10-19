public class AreaBurstSTROnNumCardsPlayed : OnCardThreshold
{
	public override bool OnEnable()
	{
		foreach (LaneState thisAndAdjacentLane in base.Owner.Lane.ThisAndAdjacentLanes)
		{
			if (thisAndAdjacentLane.Creature != null)
			{
				ApplyStatus(thisAndAdjacentLane.Creature, StatusEnum.BurstSTR, base.Val2);
			}
		}
		return true;
	}
}
