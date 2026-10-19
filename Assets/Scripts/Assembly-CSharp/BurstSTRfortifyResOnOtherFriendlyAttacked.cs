public class BurstSTRfortifyResOnOtherFriendlyAttacked : OnFriendlyAttacked
{
	public override bool OnEnable()
	{
		if (Attacked != base.Owner)
		{
			ApplyStatus(base.Owner, StatusEnum.BurstSTR, base.Val1);
			ApplyStatus(base.Owner, StatusEnum.FortifyRES, base.Val2);
			ApplyStatus(base.Owner, StatusEnum.EnhanceSTR, base.Val3);
			return true;
		}
		return false;
	}
}
