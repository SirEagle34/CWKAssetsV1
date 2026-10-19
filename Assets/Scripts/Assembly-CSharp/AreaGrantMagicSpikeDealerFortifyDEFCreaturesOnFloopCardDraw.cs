public class AreaGrantMagicSpikeDealerFortifyDEFCreaturesOnFloopCardDraw : OnCardDraw
{
	public override bool OnEnable()
	{
		if (!CardDrawn.IsLeaderCard)
		{
			return false;
		}
		bool result = false;
		foreach (LaneState thisAndAdjacentLane in base.Owner.Lane.ThisAndAdjacentLanes)
		{
			if (thisAndAdjacentLane.Creature != null)
			{
				ApplyStatus(thisAndAdjacentLane.Creature, StatusEnum.MagicSpike, base.Val1);
				ApplyStatus(thisAndAdjacentLane.Creature, StatusEnum.Dealer, base.Val2);
				ApplyStatus(thisAndAdjacentLane.Creature, StatusEnum.FortifyDEF, base.Val3);
				result = true;
			}
		}
		return result;
	}
}
