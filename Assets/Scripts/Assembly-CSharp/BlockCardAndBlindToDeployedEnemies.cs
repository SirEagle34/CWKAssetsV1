public class BlockCardAndBlindToDeployedEnemies : OnOppDeploy
{
	public override bool OnEnable()
	{
		ApplyStatus(CreatureDeployed, StatusEnum.CardBlock, base.Val1);
		ApplyStatus(CreatureDeployed, StatusEnum.Blind, base.Val2);
		return true;
	}
}
