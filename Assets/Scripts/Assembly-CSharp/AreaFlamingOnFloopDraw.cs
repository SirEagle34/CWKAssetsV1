public class AreaFlamingOnFloopDraw : OnHeroCardDraw
{
	public override bool OnEnable()
	{
		foreach (LaneState thisAndAdjacentLane in base.Owner.Lane.ThisAndAdjacentLanes)
		{
			if (thisAndAdjacentLane.Creature != null)
			{
				ApplyStatus(thisAndAdjacentLane.Creature, StatusEnum.FlameArmor, base.Val1);
			}
		}
		return true;
	}
}
