public class HealShieldWhenLandIcy : OnFriendlyIcyLands 
{
	public override bool OnEnable()
	{
		ApplyStatus(Target, StatusEnum.Shield, base.Val2);
		Target.Heal(base.Val1Pct);

		return true;
	}
}
