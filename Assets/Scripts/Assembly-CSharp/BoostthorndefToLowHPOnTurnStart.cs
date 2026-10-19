public class BoostthorndefToLowHPOnTurnStart : OnStartTurn
{
	public override bool OnEnable()
	{
		bool result = false;
		foreach (LaneState lane in base.Owner.Owner.Lanes)
		{
			if (lane.Creature != null && lane.Creature.HPPct < base.Val1Pct)
			{
				ApplyStatus(lane.Creature, StatusEnum.Thorns, base.Val2);
				ApplyStatus(lane.Creature, StatusEnum.FortifyDEF, base.Val3);
				result = true;
			}
		}
		return result;
	}
}
