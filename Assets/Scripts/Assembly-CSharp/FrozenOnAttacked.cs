public class FrozenOnAttacked : OnAttacked
{
	public override bool OnEnable()
	{
		if (base.Val1Chance)
		{
			ApplyStatus(Attacker, StatusEnum.Frozen, base.Val2);
			return true;
		}
		return false;
	}
}
