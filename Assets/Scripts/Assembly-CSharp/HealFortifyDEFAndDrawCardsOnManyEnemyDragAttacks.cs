public class HealFortifyDEFAndDrawCardsOnManyEnemyDragAttacks : OnEnemyAttack
{
	public override bool OnEnable()
	{
		if (Attacker.DragAttacksThisTurn == 3 && IsDragAttack)
		{
			foreach (LaneState thisAndAdjacentLane in base.Owner.Lane.ThisAndAdjacentLanes)
			{
				if (thisAndAdjacentLane.Creature != null)
				{
					for (int i = 0; i < base.Val1; i++)
					{
						thisAndAdjacentLane.Creature.DrawCard();
					}
					thisAndAdjacentLane.Creature.Heal(base.Val2Pct);
					ApplyStatus(thisAndAdjacentLane.Creature, StatusEnum.FortifyDEF, base.Val3);
				}
			}
			return true;
		}
		return false;
	}
}
