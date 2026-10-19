public class StealThornsAPSiphonAndDealerOnAttack : OnAttack
{
	public override bool OnEnable()
	{
		StatusState statusState = Target.StatusEffects.Find((StatusState m) => m is Thorns);
		if (statusState != null)
		{
			Target.CancelStatusEffect(statusState.Data);
			ApplyStatus(base.Owner, StatusEnum.APSiphon, base.Val1);
			ApplyStatus(base.Owner, StatusEnum.Dealer, base.Val2);
			return true;
		}
		return false;
	}
}
