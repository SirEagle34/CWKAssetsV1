public class AdjacentVampiricOnDied : OnDied
{
	public override bool OnEnable()
	{
		bool result = false;
		foreach (LaneState adjacentLane in base.Owner.Lane.AdjacentLanes)
		{
			if (adjacentLane.Creature != null)
			{
				ApplyStatus(adjacentLane.Creature, StatusEnum.Vampiric, base.Val1);
				result = true;
			}
		}
		return result;
	}
}
