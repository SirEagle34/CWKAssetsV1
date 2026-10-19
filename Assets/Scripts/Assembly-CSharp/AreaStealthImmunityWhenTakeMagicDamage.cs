public class AreaStealthImmunityWhenTakeMagicDamage : OnFriendlyDamaged
{
	public override bool OnEnable()
	{
		if (Magic && !Physical)
		{
			foreach (LaneState thisAndAdjacentLane in Attacked.Lane.ThisAndAdjacentLanes)
			{
				if (thisAndAdjacentLane.Creature != null)
				{
					ApplyStatus(thisAndAdjacentLane.Creature, StatusEnum.Stealth, base.Val1);
				    ApplyStatus(thisAndAdjacentLane.Creature, StatusEnum.Immunity, base.Val2);
				}
			}
			return true;
		}
		return false;
	}
}
