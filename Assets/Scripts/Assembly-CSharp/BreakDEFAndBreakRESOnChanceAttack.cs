public class BreakDEFAndBreakRESOnChanceAttack : OnAttack
{
	public override bool OnEnable()
	{
		if (base.Val1Chance)
		{
			ApplyStatus(Target, StatusEnum.BreakDefense, base.Val2);
			ApplyStatus(Target, StatusEnum.BreakResistance, base.Val3);
			return true;
		}
		return false;
	}
}
