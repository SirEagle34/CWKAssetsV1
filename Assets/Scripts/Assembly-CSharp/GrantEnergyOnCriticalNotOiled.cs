public class GrantEnergyOnCriticalNotOiled : OnCriticalNotOiled
{
    public override bool OnEnable()
    {
        base.Owner.Owner.AddActionPoints(base.Val1);
        return true;
    }
}