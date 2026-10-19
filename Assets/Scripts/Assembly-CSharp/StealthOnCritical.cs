public class StealthOnCritical : OnCritical
{
	public override bool OnEnable()
	{
		ApplyStatus(base.Owner, StatusEnum.Stealth, base.Val1);
		return true;
	}
}
