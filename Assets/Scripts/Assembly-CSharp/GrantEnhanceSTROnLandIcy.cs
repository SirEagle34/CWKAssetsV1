public class GrantEnhanceSTROnLandIcy : OnLandIcy
{
	public override bool OnEnable()
	{
		ApplyStatus(base.Owner, StatusEnum.EnhanceSTR, base.Val1);
		ApplyStatus(base.Owner, StatusEnum.Adrenaline, base.Val2);
		return true;
	}
}
