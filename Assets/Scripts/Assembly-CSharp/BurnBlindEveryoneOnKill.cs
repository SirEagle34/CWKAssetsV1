public class BurnBlindEveryoneOnKill : OnKill
{
	public override bool OnEnable()
	{
		bool result = false;
		foreach (LaneState lane in base.Owner.Owner.Opponent.Lanes)
		{
			if (lane.Creature != null)
			{
				ApplyStatus(lane.Creature, StatusEnum.Burned, base.Val1);
				ApplyStatus(lane.Creature, StatusEnum.Blind, base.Val2);
				result = true;
			}
		}
		return result;
	}
}
