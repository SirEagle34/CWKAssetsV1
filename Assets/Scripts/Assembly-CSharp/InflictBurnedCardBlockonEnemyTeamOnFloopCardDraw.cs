public class InflictBurnedCardBlockonEnemyTeamOnFloopCardDraw : OnCardDraw
{
	public override bool OnEnable()
	{
		// Only trigger when a Leader card is drawn (Floop leader effect)
		if (!CardDrawn.IsLeaderCard)
		{
			return false;
		}

		bool result = false;

		// 🔥 TARGET ENEMY TEAM
		foreach (LaneState lane in base.Owner.Owner.Opponent.Lanes)
		{
			if (lane.Creature != null &&
				(lane.Creature.Data.Faction == CreatureFaction.Red ||
				 lane.Creature.Data.Faction == CreatureFaction.Green ||
				 lane.Creature.Data.Faction == CreatureFaction.Blue ||
				 lane.Creature.Data.Faction == CreatureFaction.Light ||
				 lane.Creature.Data.Faction == CreatureFaction.Dark ||
				 lane.Creature.Data.Faction == CreatureFaction.Colorless ||
				 lane.Creature.Data.Faction == CreatureFaction.White ||
				 lane.Creature.Data.Faction == CreatureFaction.Orange))
			{
				ApplyStatus(lane.Creature, StatusEnum.Burned, base.Val1);
				ApplyStatus(lane.Creature, StatusEnum.CardBlock, base.Val2);
				result = true;
			}
		}

		return result;
	}
}
