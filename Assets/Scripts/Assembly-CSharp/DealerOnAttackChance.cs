public class DealerOnAttackChance: OnPercentAttack
{
    public override bool OnEnable()
    {
        ApplyStatus(base.Owner, StatusEnum.Dealer, base.Val2);
        return true;
    }
}