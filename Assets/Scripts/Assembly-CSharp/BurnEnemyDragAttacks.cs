public class BurnEnemyDragAttacks : OnEnemyAttack
{
	public override bool OnEnable()
	{
		if (Attacker.DragAttacksThisTurn == 1 && IsDragAttack)
		{
			ApplyStatus(Attacker, StatusEnum.Burned, base.Val1);
			return true;
		}
		return false;
	}
}
