public class AreaRemoveCrippleCurseCardblockAndHealEachTurn : OnStartTurn
{
	public override bool OnEnable()
	{
		if (base.Owner.HPPct > base.Val1Pct)
		{
			foreach (LaneState thisAndAdjacentLane in base.Owner.Lane.ThisAndAdjacentLanes)
			{
				if (thisAndAdjacentLane.Creature != null)
				{
					CancelStatus(thisAndAdjacentLane.Creature, StatusEnum.Poison);
					CancelStatus(thisAndAdjacentLane.Creature, StatusEnum.CardBlock);
					CancelStatus(thisAndAdjacentLane.Creature, StatusEnum.Cripple);
					thisAndAdjacentLane.Creature.Heal(base.Val2Pct);
				}
			}
			return true;
		}
		return false;
	}
}
