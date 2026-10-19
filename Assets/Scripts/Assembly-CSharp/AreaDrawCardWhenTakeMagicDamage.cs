public class AreaDrawCardWhenTakeMagicDamage : OnFriendlyDamaged
{
	public override bool OnEnable()
	{
		bool result = false;
		if (Magic && !Physical)
		foreach (LaneState thisAndAdjacentLane in base.Owner.Lane.ThisAndAdjacentLanes)
		{
			if (thisAndAdjacentLane.Creature != null && thisAndAdjacentLane.Creature.AtFullHealth)
			{
				for (int i = 0; i < base.Val1; i++)
				{
					thisAndAdjacentLane.Creature.DrawCard();
				}
				ApplyStatus(thisAndAdjacentLane.Creature, StatusEnum.Shield, base.Val2);
				result = true;
			}
		}
		return result;
	}
}
