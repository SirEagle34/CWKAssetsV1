public class GrantDealerHealIfOppHasMoreCards : OnStartTurn
{
	public override bool OnEnable()
	{
		if (base.Owner.Owner.Opponent.Hand.Count > base.Owner.Owner.Hand.Count)
		{
			ApplyStatus(base.Owner, StatusEnum.Dealer, base.Val2);
			base.Owner.Heal(base.Val1Pct);
			return true;
		}
		return false;
	}
}
