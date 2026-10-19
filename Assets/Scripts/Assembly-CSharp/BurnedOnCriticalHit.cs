public class BurnedOnCriticalHit : OnCritical
{
	public override bool OnEnable()
	{
		ApplyStatus(Target, StatusEnum.Burned, base.Val1);
		return true;
	}
}
