public class WeakenINTWeakenSTROnManyEnemyDragAttacks : OnEnemyAttack
{
	public override bool OnEnable()
	{
		if (Attacker.DragAttacksThisTurn == 2 && IsDragAttack)
		{
			ApplyStatus(Attacker, StatusEnum.WeakenINT, base.Val1);
			ApplyStatus(Attacker, StatusEnum.WeakenSTR, base.Val2);
			return true;
		}
		return false;
	}
}
