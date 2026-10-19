public class StealthOnHPThreshold : OnHPThreshold
{
	public override bool OnEnable()
	{
		ApplyStatus(base.Owner, StatusEnum.Stealth, base.Val2);
		return true;
	}
}
