public class CrippleBreakDEFOnManyEnemyDragAttacks : OnEnemyAttack
{
	public override bool OnEnable()
	{
		if (Attacker.DragAttacksThisTurn == 3 && IsDragAttack)
		{
			ApplyStatus(Attacker, StatusEnum.Cripple, base.Val1);
			ApplyStatus(Attacker, StatusEnum.BreakDEF, base.Val2);
			return true;
		}
		return false;
	}
}
