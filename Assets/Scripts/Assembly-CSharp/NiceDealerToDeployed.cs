public class NiceDealerToDeployed : OnDeployed
{
	public override bool OnEnable()
	{
		ApplyStatus(DeployedCreature, StatusEnum.Dealer, base.Val1);
		ApplyStatus(DeployedCreature, StatusEnum.LandNice, 1f);
		return true;
	}
}
