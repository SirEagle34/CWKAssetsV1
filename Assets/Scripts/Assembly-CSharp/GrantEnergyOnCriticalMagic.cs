public class GrantEnergyOnCriticalMagic : OnCriticalMagic
{
    public override bool OnEnable()
    {
        base.Owner.Owner.AddActionPoints(base.Val1);
        return true;
    }
}