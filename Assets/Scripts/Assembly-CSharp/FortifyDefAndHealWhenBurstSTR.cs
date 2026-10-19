public class FortifyDefAndHealWhenBurstSTR : OnFriendlyBurstSTR
{
	public override bool OnEnable()
	{
		ApplyStatus(Target, StatusEnum.FortifyDEF, base.Val1);
		Target.Heal(base.Val2Pct);
		return true;
	}
}
