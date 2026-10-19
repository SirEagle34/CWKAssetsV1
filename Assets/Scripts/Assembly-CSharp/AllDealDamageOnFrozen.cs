public class AllDealDamageOnFrozen : OnEnemyFrozen
{
	public override bool OnEnable()
	{
		StatusState statusState = CreatureAffected.StatusEffects.Find((StatusState s) => s is Frozen);
		if (statusState != null)
		{
			ApplyStatus(CreatureAffected, StatusEnum.DirectDamage, base.Val1);
			return true;
		}
		return false;
	}
}
