public class InflictMagicDownAttackDownCardBlockWhenEnemyThorns : OnEnemyThorns
{
	public override bool OnEnable()
	{
		if (CreatureAffected == null)
			return false;

		ApplyStatus(CreatureAffected, StatusEnum.WeakenINT, base.Val1);
		ApplyStatus(CreatureAffected, StatusEnum.WeakenSTR, base.Val2);
		ApplyStatus(CreatureAffected, StatusEnum.CardBlock, base.Val3);

		return true;
	}
}