public class AreaShieldWhenTakeMagicDamage : OnFriendlyDamaged
{
	public override bool OnEnable()
	{
		if (Magic && !Physical)
		{
			foreach (LaneState thisAndAdjacentLane in Attacked.Lane.ThisAndAdjacentLanes)
			{
				if (thisAndAdjacentLane.Creature != null)
				{
					ApplyStatus(thisAndAdjacentLane.Creature, StatusEnum.Shield, base.Val1);
				}
			}
			return true;
		}
		return false;
	}
}
