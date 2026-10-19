public class GrantHealPPIfOnLandIcy : OnLandIcy
{
	public override bool OnEnable()
	{
		base.Owner.Heal(base.Val1Pct);
		ApplyStatus(base.Owner, StatusEnum.Pinpoint, base.Val2);
		return true;
	}
}
