public class DealerDefenseUpWhenBravery : OnFriendlyBravery
{
	public override bool OnEnable()
	{
		{
			ApplyStatus(Target, StatusEnum.FortifyDEF, base.Val1);
			ApplyStatus(Target, StatusEnum.Dealer, base.Val2);
			return true;
		}
	}
}
