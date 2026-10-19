public class ShieldDealerOnStartTurnAndHPThreshold : OnStartTurn
{
	public override bool OnEnable()
	{
		if (base.Owner.HPPct < base.Val3Pct)
		{
			ApplyStatus(base.Owner, StatusEnum.Shield, base.Val1);
			ApplyStatus(base.Owner, StatusEnum.Dealer, base.Val2);
			return true;
		}
		return false;
	}
}
