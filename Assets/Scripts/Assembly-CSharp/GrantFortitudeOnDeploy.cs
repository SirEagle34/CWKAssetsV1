public class GrantFortitudeOnDeploy : OnDeploy
{
	public override bool OnEnable()
	{
		ApplyStatus(base.Owner, StatusEnum.EnhanceSTR, base.Val1);
		return true;
	}
}