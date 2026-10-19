public class EraseEnhanceSTRorBurstSTRInflictWeakenSTRandGrantBurstSTROnAttack : OnAttack
{
	public override bool OnEnable()
	{
		if ((bool)Target.HasEnhanceSTR || (bool)Target.HasBurstSTR)
		{
			CancelStatus(Target, StatusEnum.EnhanceSTR);
			CancelStatus(Target, StatusEnum.BurstSTR);

			ApplyStatus(Target, StatusEnum.WeakenSTR, base.Val1);
			ApplyStatus(base.Owner, StatusEnum.BurstSTR, base.Val2);

			return true;
		}

		return false;
	}
}