public class GrantReviveDealerCreaturesOnFloopCardDraw : OnCardDrawSelect
{
	public override bool OnEnable()
	{
		// Sadece friendly Floop kartı çekildiğinde
		if (WhichPlayer != base.Owner.Owner)
		{
			return false;
		}

		// Sadece Floop
		if (!CardDrawn.IsLeaderCard)
			return false;

		bool result = false;

		foreach (LaneState lane in base.Owner.Owner.Lanes)
		{
			if (lane.Creature != null)
			{
				ApplyStatus(lane.Creature, StatusEnum.AutoRevive, base.Val1);
				ApplyStatus(lane.Creature, StatusEnum.Dealer, base.Val2);
				result = true;
			}
		}

		return result;
	}
}