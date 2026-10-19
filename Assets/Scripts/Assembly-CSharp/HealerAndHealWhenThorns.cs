public class HealerAndHealWhenThorns : OnFriendlyThorns
{
	public override bool OnEnable()
	{
		ApplyStatus(Target, StatusEnum.Healer, base.Val1);
		Target.Heal(base.Val2Pct);
		return true;
	}
}
