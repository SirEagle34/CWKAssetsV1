public class DrawAndMagicSpikeNiceSandToFullHPAtTurnStart : OnStartTurn
{
	public override bool OnEnable()
	{
		bool result = false;

		foreach (LaneState adjacentLane in base.Owner.Lane.AdjacentLanes)
		{
			if (adjacentLane.Creature != null
				&& adjacentLane.Creature.AtFullHealth
				&& (adjacentLane.Creature.Data.Faction == CreatureFaction.Light
					|| adjacentLane.Creature.Data.Faction == CreatureFaction.Green || adjacentLane.Creature.Data.Faction == CreatureFaction.Red || adjacentLane.Creature.Data.Faction == CreatureFaction.White || adjacentLane.Creature.Data.Faction == CreatureFaction.Orange || adjacentLane.Creature.Data.Faction == CreatureFaction.Dark || adjacentLane.Creature.Data.Faction == CreatureFaction.Colorless || adjacentLane.Creature.Data.Faction == CreatureFaction.Black || adjacentLane.Creature.Data.Faction == CreatureFaction.Yellow || adjacentLane.Creature.Data.Faction == CreatureFaction.Purple))
			{
				for (int i = 0; i < base.Val1; i++)
				{
					adjacentLane.Creature.DrawCard();
				}

				ApplyStatus(adjacentLane.Creature, StatusEnum.MagicSpike, base.Val2);
				result = true;
			}
		}

		return result;
	}
}