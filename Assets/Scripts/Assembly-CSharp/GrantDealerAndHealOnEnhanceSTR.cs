public class GrantDealerAndHealOnEnhanceSTR : OnEnhanceSTR
{
	public override bool OnEnable()
	{
		bool result = false;
		foreach (LaneState adjacentLane in base.Owner.Lane.AdjacentLanes)
		{
			if (adjacentLane.Creature != null)
			{
				ApplyStatus(adjacentLane.Creature, StatusEnum.Dealer, base.Val1);
				adjacentLane.Creature.Heal(base.Val2Pct);
				result = true;
			}
		}
		return result;
	}
}