public class FrozenAndCardBlockToDeployedEnemies : OnOppDeploy
{
	public override bool OnEnable()
	{
		ApplyStatus(CreatureDeployed, StatusEnum.Frozen, base.Val1);
		ApplyStatus(CreatureDeployed, StatusEnum.CardBlock, base.Val2);
		return true;
	}
}
