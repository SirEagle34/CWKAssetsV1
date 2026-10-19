public class GrantInvisibleOnDeploy : OnDeploy
{
	public override bool OnEnable()
	{
		ApplyStatus(base.Owner, StatusEnum.Invisible, base.Val1);
		return true;
	}
}
