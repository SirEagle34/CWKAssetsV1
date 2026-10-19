public class GrantVanishToCornRainbowTeamOnDeploy : OnDeployed
{
	public override bool OnEnable()
	{
		if (DeployedCreature.Data.Faction == CreatureFaction.Red || DeployedCreature.Data.Faction == CreatureFaction.Colorless)
		{
			ApplyStatus(DeployedCreature, StatusEnum.Stealth, base.Val1);
			return true;
		}
		return false;
	}
}
