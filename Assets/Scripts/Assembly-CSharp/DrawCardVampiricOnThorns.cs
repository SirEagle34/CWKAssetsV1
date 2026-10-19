public class DrawCardVampiricOnThorns : OnThorns
{
	public override bool OnEnable()
	{
		base.Owner.DrawCard();
		ApplyStatus(base.Owner, StatusEnum.Vampiric, base.Val1);
		return true;
	}
}
