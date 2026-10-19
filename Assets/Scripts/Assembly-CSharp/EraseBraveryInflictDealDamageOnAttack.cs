public class EraseBraveryInflictDealDamageOnAttack : OnAttack
{
	public override bool OnEnable()
	{
		if ((bool)Target.HasBravery)
		{
			CancelStatus(Target, StatusEnum.Bravery);
			ApplyStatus(Target, StatusEnum.DirectDamage, base.Val1);
			return true;
		}
		return false;
	}
}
