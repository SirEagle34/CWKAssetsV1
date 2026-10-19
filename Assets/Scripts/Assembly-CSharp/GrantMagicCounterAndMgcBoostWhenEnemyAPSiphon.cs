public class GrantMagicCounterAndMgcBoostWhenEnemyAPSiphon : OnEnemyAPSiphon
{
	public override bool OnEnable()
	{
		if (CreatureAffected == null)
			return false;

		// This creature gains buffs
		ApplyStatus(base.Owner, StatusEnum.MagicCounter, base.Val1);
		ApplyStatus(base.Owner, StatusEnum.BurstINT, base.Val2);

		return true;
	}
}