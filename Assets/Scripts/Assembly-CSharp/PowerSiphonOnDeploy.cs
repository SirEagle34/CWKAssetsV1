public class PowerSiphonOnDeploy : OnDeploy
{
	public override bool OnEnable()
	{
		ApplyStatus(base.Owner, StatusEnum.FloopSiphon, base.Val1);
		return true;
	}
}
