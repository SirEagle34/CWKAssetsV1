public class GrantFortifyDEFRegenTIcylandsNicelandsTeamOnDeploy : OnDeployed
{
	public override bool OnEnable()
	{
		if (DeployedCreature.Data.Faction == CreatureFaction.Light || DeployedCreature.Data.Faction == CreatureFaction.White)
		{
			ApplyStatus(DeployedCreature, StatusEnum.FortifyDEF, base.Val1);
			ApplyStatus(DeployedCreature, StatusEnum.Regen, base.Val2);
			return true;
		}
		return false;
	}
}
