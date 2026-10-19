public class InvisibleOnCritical : OnCritical
{
	public override bool OnEnable()
	{
		ApplyStatus(base.Owner, StatusEnum.Invisible, base.Val1);
		return true;
	}
}
