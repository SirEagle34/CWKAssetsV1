public class InflictParalyzeConfuseEnemyTeamOnFloopCardDraw : OnCardDraw
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
			if (lane.Creature != null)
			{
				ApplyStatus(lane.Creature, StatusEnum.Paralyze, base.Val1);
				ApplyStatus(lane.Creature, StatusEnum.Confuse, base.Val2);
				result = true;
			}
		}

		return result;
	}
}
