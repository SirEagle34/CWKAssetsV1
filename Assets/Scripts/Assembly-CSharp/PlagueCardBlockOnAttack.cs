public class PlagueCardBlockOnAttack : OnAttacked
{
	public override bool OnEnable()
	{
		if (base.Val1Chance)
		{
			ApplyStatus(Attacker, StatusEnum.Plague, base.Val2);
			ApplyStatus(Attacker, StatusEnum.CardBlock, base.Val3);
			return true;
		}
		return false;
	}
}
