public class CardblockParalyzeAllOnDeath : OnDied
{
	public override bool OnEnable()
	{
		bool result = false;
		foreach (LaneState lane in base.Owner.Owner.Opponent.Lanes)
		{
			if (lane.Creature != null)
			{
				ApplyStatus(lane.Creature, StatusEnum.Paralyze, base.Val1);
				ApplyStatus(lane.Creature, StatusEnum.CardBlock, base.Val2);
				result = true;
			}
		}
		return result;
	}
}
