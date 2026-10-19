public class FrozenAndBreakDEFOnAttackNonFrozen : OnAttack
{
    public override bool OnEnable()
    {
        if (Target != null && !Target.IsFrozen)
            return false;

        ApplyStatus(Target, StatusEnum.Frozen, base.Val1);
        ApplyStatus(Target, StatusEnum.BreakDEF, base.Val2);

        return true;
    }
}
