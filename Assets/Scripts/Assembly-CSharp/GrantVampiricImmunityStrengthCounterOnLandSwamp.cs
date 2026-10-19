using UnityEngine;

public class GrantVampiricImmunityStrengthCounterOnLandSwamp : OnWhenFriendlyLandSwamp
{
	public override bool OnEnable()
	{
		ApplyStatus(Target, StatusEnum.Vampiric, base.Val1);
		ApplyStatus(Target, StatusEnum.Immunity, base.Val2);
		ApplyStatus(Target, StatusEnum.StrengthCounter, base.Val3);

		return true;
	}
}