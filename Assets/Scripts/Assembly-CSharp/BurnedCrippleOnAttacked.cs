public class BurnedCrippleOnAttacked : OnAttacked
{
	public override bool OnEnable()
	{
		if (base.Val1Chance)
		{
			ApplyStatus(Attacker, StatusEnum.Burned, base.Val2);
			ApplyStatus(Attacker, StatusEnum.Cripple, base.Val3);
			return true;
		}
		return false;
	}
}
