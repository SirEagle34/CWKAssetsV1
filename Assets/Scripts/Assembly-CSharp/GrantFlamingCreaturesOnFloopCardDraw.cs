public class GrantFlamingCreaturesOnFloopCardDraw : OnCardDraw
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
				ApplyStatus(thisAndAdjacentLane.Creature, StatusEnum.FlameArmor, base.Val1);
				result = true;
			}
		}
		return result;
	}
}
