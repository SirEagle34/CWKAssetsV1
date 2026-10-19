public class GrantThornsFortiftDEFIfAdjHPIsLow : OnStartTurn
{
	public override bool OnEnable()
	{
		bool result = false;
		foreach (LaneState adjacentLane in base.Owner.Lane.AdjacentLanes)
		{
			if (adjacentLane.Creature != null && adjacentLane.Creature.HPPct < base.Val1Pct)
			{
				ApplyStatus(adjacentLane.Creature, StatusEnum.Thorns, base.Val2);
				ApplyStatus(adjacentLane.Creature, StatusEnum.FortifyDEF, base.Val2);
				result = true;
			}
		}
		return result;
	}
}
