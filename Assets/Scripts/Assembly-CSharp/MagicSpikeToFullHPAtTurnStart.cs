public class MagicSpikeToFullHPAtTurnStart : OnStartTurn
{
	public override bool OnEnable()
	{
		bool result = false;
		foreach (LaneState adjacentLane in base.Owner.Lane.AdjacentLanes)
		{
			if (adjacentLane.Creature != null && adjacentLane.Creature.AtFullHealth)
			{
				ApplyStatus(adjacentLane.Creature, StatusEnum.MagicSpike, base.Val1);
				result = true;
			}
		}
		return result;
	}
}
