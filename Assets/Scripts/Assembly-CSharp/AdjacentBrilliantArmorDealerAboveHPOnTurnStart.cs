public class AdjacentBrilliantArmorDealerAboveHPOnTurnStart : OnStartTurn
{
	public override bool OnEnable()
	{
		bool result = false;
		foreach (LaneState adjacentLane in base.Owner.Lane.AdjacentLanes)
		{
			if (adjacentLane.Creature != null && adjacentLane.Creature.HPPct > base.Val1Pct)
			{
				ApplyStatus(adjacentLane.Creature, StatusEnum.RadiantArmor, base.Val2);
				ApplyStatus(adjacentLane.Creature, StatusEnum.Dealer, base.Val3);
				result = true;
			}
		}
		return result;
	}
}
