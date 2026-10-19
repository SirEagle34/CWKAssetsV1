public class BurnOnDied : OnDied
{
	public override bool OnEnable()
	{
		foreach (LaneState lane in Owner.Owner.Opponent.Lanes)
		{
			if (lane.Creature != null)
			{
				ApplyStatus(lane.Creature, StatusEnum.Burned, Val1);
			}
		}

		return true;
	}
}

