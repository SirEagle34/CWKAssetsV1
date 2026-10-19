public class ParisiticOnStartTurnAndHPThreshold : OnStartTurn
{
	public override bool OnEnable()
	{
		if (base.Owner.HPPct < base.Val2Pct)
		{
			ApplyStatus(base.Owner, StatusEnum.ParisiticArmor, base.Val1);
			return true;
		}
		return false;
	}
}
