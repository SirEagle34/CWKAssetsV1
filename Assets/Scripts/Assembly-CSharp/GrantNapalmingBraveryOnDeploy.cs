public class GrantNapalmingBraveryOnDeploy : OnDeploy
{
	public override bool OnEnable()
	{
		ApplyStatus(base.Owner, StatusEnum.Napalming, base.Val1);
		ApplyStatus(base.Owner, StatusEnum.Bravery, base.Val2);
		return true;
	}
}