public class GrantLandIcyOnDeploy : OnDeploy
{
	public override bool OnEnable()
	{
		ApplyStatus(base.Owner, StatusEnum.LandIcy, base.Val1);
		return true;
	}
}
