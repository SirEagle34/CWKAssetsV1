public class GrantDealerToPlainsRainbowTeamOnDeploy : OnDeployed
{
	public override bool OnEnable()
	{
		if (DeployedCreature.Data.Faction == CreatureFaction.Blue || DeployedCreature.Data.Faction == CreatureFaction.Colorless)
		{
			ApplyStatus(DeployedCreature, StatusEnum.Dealer, base.Val1);
			return true;
		}
		return false;
	}
}
