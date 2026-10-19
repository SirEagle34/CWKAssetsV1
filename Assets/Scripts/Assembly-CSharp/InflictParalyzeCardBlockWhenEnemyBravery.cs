public class InflictParalyzeCardBlockWhenEnemyBravery : OnEnemyBravery
{
	public override bool OnEnable()
	{
		if (CreatureAffected == null)
			return false;

		ApplyStatus(CreatureAffected, StatusEnum.Paralyze, base.Val1);

		return true;
	}
}