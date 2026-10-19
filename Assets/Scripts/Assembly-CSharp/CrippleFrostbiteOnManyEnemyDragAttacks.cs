public class CrippleFrostbiteOnManyEnemyDragAttacks : OnEnemyAttack
{
	public override bool OnEnable()
	{
		if (Attacker.DragAttacksThisTurn == 3 && IsDragAttack)
		{
			ApplyStatus(Attacker, StatusEnum.Frostbite, base.Val1);
			ApplyStatus(Attacker, StatusEnum.Cripple, base.Val2);
			return true;
		}
		return false;
	}
}
