public class NervBlockOnManyEnemyDragAttacks : OnEnemyAttack
{
	public override bool OnEnable()
	{
		if (Attacker.DragAttacksThisTurn == 4 && IsDragAttack)
		{
			ApplyStatus(Attacker, StatusEnum.NervBlock, base.Val1);
			return true;
		}
		return false;
	}
}
