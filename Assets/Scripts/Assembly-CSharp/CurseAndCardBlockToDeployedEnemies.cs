public class CurseAndCardBlockToDeployedEnemies : OnOppDeploy
{
	public override bool OnEnable()
	{
		ApplyStatus(CreatureDeployed, StatusEnum.Poison, base.Val1);
		ApplyStatus(CreatureDeployed, StatusEnum.CardBlock, base.Val2);
		return true;
	}
}
