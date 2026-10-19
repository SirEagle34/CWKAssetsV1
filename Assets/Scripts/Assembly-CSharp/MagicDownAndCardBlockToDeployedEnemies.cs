public class MagicDownAndCardBlockToDeployedEnemies : OnOppDeploy
{
    public override bool OnEnable()
    {
        ApplyStatus(CreatureDeployed, StatusEnum.WeakenINT, base.Val1);
        ApplyStatus(CreatureDeployed, StatusEnum.CardBlock, base.Val2);
        return true;
    }
}