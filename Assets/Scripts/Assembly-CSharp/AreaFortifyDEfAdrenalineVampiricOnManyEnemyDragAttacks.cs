public class AreaFortifyDEfAdrenalineVampiricOnManyEnemyDragAttacks : OnEnemyAttack
{
	public override bool OnEnable()
	{
		if (Attacker.DragAttacksThisTurn == 3 && IsDragAttack)
		{
			foreach (LaneState thisAndAdjacentLane in base.Owner.Lane.ThisAndAdjacentLanes)
			{
				if (thisAndAdjacentLane.Creature != null)
				{
					ApplyStatus(thisAndAdjacentLane.Creature, StatusEnum.FortifyDEF, base.Val1);
					ApplyStatus(thisAndAdjacentLane.Creature, StatusEnum.Adrenaline, base.Val2);
					ApplyStatus(thisAndAdjacentLane.Creature, StatusEnum.Vampiric, base.Val3);
				}
			}
			return true;
		}
		return false;
	}
}
