public class ParalyzeCardBlockOnManyEnemyDragAttacks : OnEnemyAttack
{
	public override bool OnEnable()
	{
		if (Attacker.DragAttacksThisTurn == 4 && IsDragAttack)
		{
			ApplyStatus(Attacker, StatusEnum.Paralyze, base.Val1);
			ApplyStatus(Attacker, StatusEnum.CardBlock, base.Val2);
			return true;
		}
		return false;
	}
}
