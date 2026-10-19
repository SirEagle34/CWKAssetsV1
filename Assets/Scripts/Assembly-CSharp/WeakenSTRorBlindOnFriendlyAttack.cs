public class WeakenSTRorBlindOnFriendlyAttack : OnFriendlyAttack
{
	public override bool OnEnable()
	{
		ApplyStatus(Target, StatusEnum.Blind, base.Val1);
		ApplyStatus(Target, StatusEnum.WeakenSTR, base.Val1);
		return true;
	}
}