public class RemoveEnemyImmunityAutoReviveShieldEvery3Turns : OnStartTurn
{
	private int turnCounter = 0;

	public override bool OnEnable()
	{
		turnCounter++;

		if (turnCounter < 3)
			return false;

		turnCounter = 0;

		bool result = false;

		foreach (LaneState lane in base.Owner.Owner.Opponent.Lanes)
		{
			if (lane.Creature != null)
			{
				CancelStatus(lane.Creature, StatusEnum.Immunity);
				CancelStatus(lane.Creature, StatusEnum.AutoRevive);
				CancelStatus(lane.Creature, StatusEnum.Shield);

				result = true;
			}
		}

		base.Owner.Heal(base.Val1Pct);

		return result;
	}
}