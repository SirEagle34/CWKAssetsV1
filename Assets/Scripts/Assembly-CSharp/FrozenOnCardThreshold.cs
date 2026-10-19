public class FrozenOnCardThreshold : OnCardThreshold
{
	public override bool OnEnable()
	{
		bool result = false;
		foreach (LaneState lane in base.Owner.Owner.Opponent.Lanes)
		{
			if (lane.Creature != null)
			{
				ApplyStatus(lane.Creature, StatusEnum.Frozen, base.Val2);
				result = true;
			}
		}
		return result;
	}
}
