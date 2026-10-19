public class AdjacentImmunityVampiricOnHPThreshold : OnHPThreshold
{
	public override bool OnEnable()
	{
		bool result = false;
		foreach (LaneState adjacentLane in base.Owner.Lane.AdjacentLanes)
		{
			if (adjacentLane.Creature != null)
			{
				ApplyStatus(adjacentLane.Creature, StatusEnum.Immunity, base.Val2);
				ApplyStatus(adjacentLane.Creature, StatusEnum.Vampiric, base.Val3);
				result = true;
			}
		}
		return result;
	}
}
