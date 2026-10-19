using UnityEngine;
public class StealImmunityGrantImmunityShieldOnAttack : OnAttack
{
	public override bool OnEnable()
	{
		StatusState statusState = Target.StatusEffects.Find((StatusState m) => m is Immunity);
		if (statusState != null)
		{
			Target.CancelStatusEffect(statusState.Data);
			ApplyStatus(base.Owner, StatusEnum.Immunity, base.Val1);
			ApplyStatus(base.Owner, StatusEnum.Shield, base.Val2);
			return true;
		}
		return false;
	}
}
