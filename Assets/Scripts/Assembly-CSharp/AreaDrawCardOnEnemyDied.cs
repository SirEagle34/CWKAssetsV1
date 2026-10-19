public class AreaDrawCardOnEnemyDied : OnEnemyDied
{
	public override bool OnEnable()
	{
		bool result = false;
		foreach (LaneState thisAndAdjacentLane in base.Owner.Lane.ThisAndAdjacentLanes)
		{
			if (thisAndAdjacentLane.Creature != null)
			{
				if (thisAndAdjacentLane.Creature != base.Owner)
				{
					thisAndAdjacentLane.Creature.DrawCard();
				}
				result = true;
			}
		}
		return false;
	}
}
