public class IFOppAttack5timesHealDrawandForifyDef : OnEnemyAttack
{
	public override bool OnEnable()
	{
		if (Attacker.DragAttacksThisTurn == 3 && IsDragAttack)
		{
			ApplyStatus(base.Owner, StatusEnum.FortifyDEF, base.Val1);
			base.Owner.Heal(base.Val2Pct);
			base.Owner.DrawCard();
			return true;
		}
		return false;
	}
}
