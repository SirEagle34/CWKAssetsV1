using UnityEngine;

public class GrantDealerCornSandIcylandCreaturesOnDeployAndSwapStrengthOnCardThreshold : AbilityState
{
	private int CardsPlayedThisTurn;

	public override bool ProcessMessage(GameMessage Message)
	{
		bool result = false;

		if (Message.Action == GameEvent.START_TURN &&
			Message.WhichPlayer == base.Owner.Owner)
		{
			CardsPlayedThisTurn = 0;
		}

		if (Message.Action == GameEvent.CREATURE_DEPLOYED &&
			Message.Creature != base.Owner &&
			Message.WhichPlayer == base.Owner.Owner)
		{
			if (Message.Creature.Data.Faction == CreatureFaction.Red ||
				Message.Creature.Data.Faction == CreatureFaction.Green ||
				Message.Creature.Data.Faction == CreatureFaction.White)
			{
				ApplyStatus(Message.Creature, StatusEnum.Dealer, base.Val1);
				result = true;
			}
		}

		if (Message.Action == GameEvent.CARD_PLAYED &&
			Message.WhichPlayer == base.Owner.Owner)
		{
			CardsPlayedThisTurn++;

			if (CardsPlayedThisTurn >= 3)
			{
				foreach (LaneState lane in base.Owner.Owner.Lanes)
				{
					if (lane.Creature != null)
					{
						ApplyStatus(lane.Creature, StatusEnum.SwapStrength, base.Val2);
						result = true;
					}
				}

				CardsPlayedThisTurn = -999; // aynı turda tekrar tetiklenmesin
			}
		}

		return result;
	}
}