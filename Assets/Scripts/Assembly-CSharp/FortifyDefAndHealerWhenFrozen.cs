public class FortifyDefAndHealerWhenFrozen : OnFriendlyFrozen
{
	public override bool OnEnable()
	{
		ApplyStatus(CreatureAffected, StatusEnum.FortifyDEF, base.Val1);
		ApplyStatus(CreatureAffected, StatusEnum.Healer, base.Val2);
		return true;
	}
}
