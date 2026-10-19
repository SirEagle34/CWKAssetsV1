public class AreaStealthOnHPThreshold : OnHPThreshold
{
	public override bool OnEnable()
	{
		bool result = false;
		foreach (LaneState thisAndAdjacentLane in base.Owner.Lane.ThisAndAdjacentLanes)
		{
			if (thisAndAdjacentLane.Creature != null)
			{
				ApplyStatus(thisAndAdjacentLane.Creature, StatusEnum.Stealth, base.Val2);
				result = true;
			}
		}
		return result;
	}
}
