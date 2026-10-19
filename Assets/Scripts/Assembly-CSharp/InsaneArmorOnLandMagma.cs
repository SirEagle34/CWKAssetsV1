public class InsaneArmorOnLandMagma : OnLandMagma
{
	public override bool OnEnable()
	{
		if (base.Owner == null)
			return false;

		ApplyStatus(base.Owner, StatusEnum.InsanityArmor, base.Val1);
		return true;
	}
}
