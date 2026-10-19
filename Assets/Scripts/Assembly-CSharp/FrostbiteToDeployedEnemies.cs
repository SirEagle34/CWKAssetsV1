public class FrostbiteToDeployedEnemies : OnOppDeploy
{
	public override bool OnEnable()
	{
		ApplyStatus(CreatureDeployed, StatusEnum.Frostbite, base.Val1);
		return true;
	}
}
