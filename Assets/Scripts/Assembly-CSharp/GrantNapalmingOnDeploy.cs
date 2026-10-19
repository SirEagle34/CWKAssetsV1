public class GrantNapalmingOnDeploy : OnDeploy
{
	public override bool OnEnable()
	{
		ApplyStatus(base.Owner, StatusEnum.Napalming, base.Val1);
		return true;
	}
}