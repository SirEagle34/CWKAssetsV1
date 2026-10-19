public class DealerAndMagicSpikeToFullHPAtTurnStart : OnStartTurn
{
	public override bool OnEnable()
	{
		bool result = false;
		foreach (LaneState adjacentLane in base.Owner.Lane.AdjacentLanes)
		{
			if (adjacentLane.Creature != null && adjacentLane.Creature.AtFullHealth)
			{
				ApplyStatus(adjacentLane.Creature, StatusEnum.Dealer, base.Val1);
				ApplyStatus(adjacentLane.Creature, StatusEnum.MagicSpike, base.Val2);
				result = true;
			}
		}
		return result;
	}
}
