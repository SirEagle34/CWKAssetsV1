public class InflictMagicDownAttackDownPoisonWhenEnemyThorns : OnEnemyThorns
{
	public override bool OnEnable()
	{
		if (CreatureAffected == null)
			return false;

		ApplyStatus(CreatureAffected, StatusEnum.WeakenINT, base.Val1);
		ApplyStatus(CreatureAffected, StatusEnum.WeakenSTR, base.Val2);
		ApplyStatus(CreatureAffected, StatusEnum.Poison, base.Val3);

		return true;
	}
}