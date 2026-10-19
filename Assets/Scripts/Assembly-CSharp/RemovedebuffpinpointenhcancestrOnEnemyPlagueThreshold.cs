public class RemovedebuffpinpointenhcancestrOnEnemyPlagueThreshold : OnEnemyPlague
{
	public override bool OnEnable()
	{
		StatusState statusState = CreatureAffected.StatusEffects.Find((StatusState s) => s is Plague);
		if (statusState != null && statusState.Intensity >= base.Val1Pct && statusState.Intensity - AmountChange < base.Val1Pct)
		{
			ApplyStatus(base.Owner, StatusEnum.EnhanceSTR, base.Val2);
			ApplyStatus(base.Owner, StatusEnum.Pinpoint, base.Val3);
			ApplyStatus(base.Owner, StatusEnum.RemoveStatus, base.Val4, StatusRemovalData.RemoveRandomDebuffs());
			return true;
		}
		return false;
	}
}
