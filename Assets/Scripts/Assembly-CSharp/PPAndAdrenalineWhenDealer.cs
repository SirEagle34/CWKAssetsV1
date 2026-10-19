public class PPAndAdrenalineWhenDealer : OnFriendlyDealer
{
	public override bool OnEnable()
	{
		ApplyStatus(Target, StatusEnum.Pinpoint, base.Val1);
		ApplyStatus(Target, StatusEnum.Adrenaline, base.Val2);
		return true;
	}
}
