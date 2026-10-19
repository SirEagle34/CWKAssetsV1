public class DealerRegenOnCardThreshold : OnCardThreshold
{
	public override bool OnEnable()
	{
		ApplyStatus(base.Owner, StatusEnum.Dealer, base.Val2);
		ApplyStatus(base.Owner, StatusEnum.Regen, base.Val3);
		return true;
	}
}
