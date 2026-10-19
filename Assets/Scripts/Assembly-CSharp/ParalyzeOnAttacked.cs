public class ParalyzeOnAttacked : OnAttacked
{
	public override bool OnEnable()
	{
		if (base.Val1Chance)
		{
			ApplyStatus(Attacker, StatusEnum.Paralyze, base.Val2);
			return true;
		}
		return false;
	}
}
