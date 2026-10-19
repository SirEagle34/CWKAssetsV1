public class LandSandOnDeploy : OnDeploy
{
	public override bool OnEnable()
	{
		ApplyStatus(base.Owner, StatusEnum.LandSand, 1f);
		ApplyStatus(base.Owner, StatusEnum.Dealer, base.Val2);
		return true;
	}
}
