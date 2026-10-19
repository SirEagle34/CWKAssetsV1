public class ParalyzeOnAttackWhenNoParalyze : OnAttack
{
	public override bool OnEnable()
	{
		if (!Target.IsParalyzed)
		{
			ApplyStatus(Target, StatusEnum.Paralyze, base.Val1);
			return true;
		}
		return false;
	}
}
