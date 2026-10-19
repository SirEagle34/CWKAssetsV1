public class FrozenAndBreakDEFIfNoFrozenOnAttack : OnAttack
{
	public override bool OnEnable()
	{
		if (Target == null)
			return false;

		if (Target.IsFrozen)
			return false;

		ApplyStatus(Target, StatusEnum.Frozen, base.Val1);
		ApplyStatus(Target, StatusEnum.BreakDEF, base.Val2);

		return true;
	}
}