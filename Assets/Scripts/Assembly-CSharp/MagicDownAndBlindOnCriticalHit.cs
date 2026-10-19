public class MagicDownAndBlindOnCriticalHit : OnCritical
{
	public override bool OnEnable()
	{
		ApplyStatus(Target, StatusEnum.WeakenMagic, base.Val1);
		ApplyStatus(Target, StatusEnum.Blind, base.Val2);		
		return true;
	}
}
