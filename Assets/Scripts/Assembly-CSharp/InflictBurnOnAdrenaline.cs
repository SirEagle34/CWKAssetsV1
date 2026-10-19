public class InflictBurnedOnAdrenaline : OnEnemyAdrenaline
{
	public override bool OnEnable()
	{
		StatusState statusState =
			CreatureAffected.StatusEffects.Find(s => s is Adrenaline);

		if (statusState != null)
		{
			ApplyStatus(
				CreatureAffected,
				StatusEnum.Burned,
				base.Val1
			);
			return true;
		}

		return false;
	}
}
