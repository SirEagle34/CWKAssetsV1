public class AreaRegenFortifyRESOnTurnStart : OnStartTurn
{
	public override bool OnEnable()
	{
		foreach (LaneState adjacentLane in base.Owner.Lane.AdjacentLanes)
		{
			if (adjacentLane.Creature != null)
			{
				ApplyStatus(adjacentLane.Creature, StatusEnum.Regen, base.Val1);
				ApplyStatus(adjacentLane.Creature, StatusEnum.FortifyRES, base.Val2);
			}
		}
		return true;
	}
}
