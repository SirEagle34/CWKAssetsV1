public class MagicCounterBurstINTAboveHealthEachTurn : OnStartTurn
{
	public override bool OnEnable()
	{
		if (base.Owner.HPPct >= base.Val1Pct)
		{
			ApplyStatus(base.Owner, StatusEnum.MagicCounter, base.Val2);
			ApplyStatus(base.Owner, StatusEnum.BurstINT, base.Val3);
			return true;
		}
		return false;
	}
}
