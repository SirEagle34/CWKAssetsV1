public class GrantFortifyRESBurstSTRTIcylandsCornfieldsTeamOnDeploy : OnDeployed
{
	public override bool OnEnable()
	{
		if (DeployedCreature.Data.Faction == CreatureFaction.Red || DeployedCreature.Data.Faction == CreatureFaction.White)
		{
			ApplyStatus(DeployedCreature, StatusEnum.FortifyRES, base.Val1);
			ApplyStatus(DeployedCreature, StatusEnum.BurstSTR, base.Val2);
			return true;
		}
		return false;
	}
}
