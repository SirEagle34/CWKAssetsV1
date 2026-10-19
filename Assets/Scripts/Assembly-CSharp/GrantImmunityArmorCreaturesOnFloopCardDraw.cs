public class GrantImmunityArmorCreaturesOnFloopCardDraw : OnCardDraw
{
	public override bool OnEnable()
	{
		if (!CardDrawn.IsLeaderCard)
		{
			return false;
		}
		bool result = false;
		foreach (LaneState lane in base.Owner.Owner.Lanes)
		{
			if (lane.Creature != null)
			{
				ApplyStatus(lane.Creature, StatusEnum.Immunity, base.Val1);
				result = true;
			}
		}
		return result;
	}
}
