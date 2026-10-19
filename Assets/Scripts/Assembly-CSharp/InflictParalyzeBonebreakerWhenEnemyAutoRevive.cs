public class InflictParalyzeBonebreakerWhenEnemyAutoRevive : OnEnemyAutoRevive
{
	public override bool OnEnable()
	{
		if (CreatureAffected == null)
			return false;

		ApplyStatus(CreatureAffected, StatusEnum.Paralyze, base.Val1);
		ApplyStatus(CreatureAffected, StatusEnum.Bonebreaker, base.Val1);

		return true;
	}
}