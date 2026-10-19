public class FortifyDEFHighHPEachTurn : OnStartTurn
{
	public override bool OnEnable()
	{
		if (base.Owner.HPPct >= base.Val1Pct)
		{
		   ApplyStatus(base.Owner, StatusEnum.FortifyDEF, base.Val2);
           return true;
		}
		return false;
	}
}
