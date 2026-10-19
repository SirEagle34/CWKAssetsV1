public class RemoveNegHealAllOnDied : OnDied
{
	public override bool OnEnable()
	{
		bool result = false;
		foreach (LaneState lane in base.Owner.Owner.Lanes)
		{
			if (lane.Creature != null)
			{
				lane.Creature.Heal(base.Val1Pct);
				ApplyStatus(lane.Creature, StatusEnum.RemoveStatus, base.Val2, StatusRemovalData.RemoveRandomDebuffs());
				result = true;
			}
		}
		return result;
	}
}