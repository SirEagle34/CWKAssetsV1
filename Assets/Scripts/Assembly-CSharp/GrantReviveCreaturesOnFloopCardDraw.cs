public class GrantReviveCreaturesOnFloopCardDraw : OnCardDraw
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
			if (lane.Creature != null && (lane.Creature.Data.Faction == CreatureFaction.Red || lane.Creature.Data.Faction == CreatureFaction.Green || lane.Creature.Data.Faction == CreatureFaction.Blue || lane.Creature.Data.Faction == CreatureFaction.Light || lane.Creature.Data.Faction == CreatureFaction.Dark || lane.Creature.Data.Faction == CreatureFaction.Colorless || lane.Creature.Data.Faction == CreatureFaction.White || lane.Creature.Data.Faction == CreatureFaction.Orange))
			{
				ApplyStatus(lane.Creature, StatusEnum.AutoRevive, base.Val1);
				result = true;
			}
		}
		return result;
	}
}
