public class Remove2DebuffsWhenFriendlyRegen : OnFriendlyRegen
{
	public override bool OnEnable()
	{
		if (Target == null)
			return false;

		ApplyStatus(
			Target,
			StatusEnum.RemoveStatus,
			base.Val1,
			StatusRemovalData.RemoveRandomDebuffs()
		);

		return true;
	}
}