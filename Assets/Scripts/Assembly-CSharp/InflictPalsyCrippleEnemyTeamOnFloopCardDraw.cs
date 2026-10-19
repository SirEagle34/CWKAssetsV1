public class InflictPalsyCrippleEnemyTeamOnFloopCardDraw : OnCardDraw
{
	public override bool OnEnable()
	{
		// Only trigger when a Leader/Floop card is drawn
		if (!CardDrawn.IsLeaderCard)
		{
			return false;
		}

		bool result = false;

		// TARGET ENEMY TEAM
		foreach (LaneState lane in base.Owner.Owner.Opponent.Lanes)
		{
			if (lane.Creature != null)
			{
				ApplyStatus(lane.Creature, StatusEnum.Palsy, base.Val1);
				ApplyStatus(lane.Creature, StatusEnum.Cripple, base.Val2);

				result = true;
			}
		}

		return result;
	}
}