public class GrantImmunityArmorNiceCornCreaturesOnFloopCardDraw : OnCardDraw
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
			if (lane.Creature != null && (lane.Creature.Data.Faction == CreatureFaction.Red || lane.Creature.Data.Faction == CreatureFaction.Light))
			{
				ApplyStatus(lane.Creature, StatusEnum.Immunity, base.Val1);
				result = true;
			}
		}
		return result;
	}
}
