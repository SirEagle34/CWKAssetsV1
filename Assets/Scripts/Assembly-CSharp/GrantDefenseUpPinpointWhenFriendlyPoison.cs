public class GrantDefenseUpPinpointWhenFriendlyPoison : OnFriendlyPoison
{
	public override bool OnEnable()
	{
		if (CreatureAffected == null)
			return false;

		ApplyStatus(CreatureAffected, StatusEnum.Pinpoint, base.Val1);
		ApplyStatus(CreatureAffected, StatusEnum.FortifyDEF, base.Val2);

		return true;
	}
}