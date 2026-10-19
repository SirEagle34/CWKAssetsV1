public class StealStealthGrantStealthAndBurstINTOnAttack : OnAttack
{
	public override bool OnEnable()
	{
		StatusState statusState = Target.StatusEffects.Find((StatusState m) => m is Stealth);
		if (statusState != null)
		{
			Target.CancelStatusEffect(statusState.Data);
			ApplyStatus(base.Owner, StatusEnum.Stealth, base.Val1);
			ApplyStatus(base.Owner, StatusEnum.BurstINT, base.Val2);
			return true;
		}
		return false;
	}
}
