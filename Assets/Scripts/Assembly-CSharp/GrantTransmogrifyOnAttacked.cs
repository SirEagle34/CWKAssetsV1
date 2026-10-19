public class GrantTransmogrifyOnAttacked : OnAttacked
{
	public override bool OnEnable()
	{
		bool result = false;
		StatusState statusState = Attacker.StatusEffects.Find((StatusState s) => s is Transmogrify);
		if (statusState == null)
		{
			ApplyStatus(Attacker, StatusEnum.Transmogrify, base.Val1);
			result = true;
		}
		return result;
	}
}
