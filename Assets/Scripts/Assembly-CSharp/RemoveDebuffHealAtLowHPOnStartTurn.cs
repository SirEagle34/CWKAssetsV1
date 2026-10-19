public class RemoveDebuffHealAtLowHPOnStartTurn : OnStartTurn
{
	public override bool OnEnable()
	{
		bool result = false;
		foreach (LaneState lane in base.Owner.Owner.Lanes)
		{
			if (lane.Creature != null && lane.Creature.HPPct < base.Val1Pct)
			{
				ApplyStatus(lane.Creature, StatusEnum.RemoveStatus, base.Val2, StatusRemovalData.RemoveRandomDebuffs());
				lane.Creature.Heal(base.Val3Pct);

				result = true;
			}
		}
		return result;
	}
}
