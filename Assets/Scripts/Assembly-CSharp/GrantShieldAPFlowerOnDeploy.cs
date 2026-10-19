public class GrantShieldAPFlowerOnDeploy : OnDeploy
{
	public override bool OnEnable()
	{
		ApplyStatus(base.Owner, StatusEnum.Shield, base.Val1);
		ApplyStatus(base.Owner, StatusEnum.APFlower, base.Val2);
		return true;
	}
}