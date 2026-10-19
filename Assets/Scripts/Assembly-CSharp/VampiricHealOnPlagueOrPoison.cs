public class VampiricHealOnPlagueOrPoison : OnEnemyPoisonOrPlague
{
    public override bool OnEnable()
    {
        ApplyStatus(base.Owner, StatusEnum.Vampiric, base.Val1);
        base.Owner.Heal(base.Val2Pct);
        return true;
    }
}