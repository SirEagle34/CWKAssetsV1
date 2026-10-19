public class APSiphonOnCardThreshold : OnCardThreshold
{
	public override bool OnEnable()
	{
		ApplyStatus(base.Owner, StatusEnum.APSiphon, base.Val2);
		return true;
	}
}
