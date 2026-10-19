using UnityEngine;

public class InvisiblePlainsSwampNicelandsCreaturesOnDeployedAndMultiImmunityOnPlay6Card : AbilityState
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
			if (Message.Creature.Data.Faction == CreatureFaction.Blue ||
				Message.Creature.Data.Faction == CreatureFaction.Dark ||
				Message.Creature.Data.Faction == CreatureFaction.Light)
			{
				ApplyStatus(Message.Creature, StatusEnum.BurstINT, base.Val1);
				result = true;
			}
		}

		if (Message.Action == GameEvent.CARD_PLAYED &&
			Message.WhichPlayer == base.Owner.Owner)
		{
			CardsPlayedThisTurn++;

			if (CardsPlayedThisTurn >= 5)
			{
				foreach (LaneState lane in base.Owner.Owner.Lanes)
				{
					if (lane.Creature != null)
					{
						ApplyStatus(lane.Creature, StatusEnum.MultiImmunity, base.Val2);
						result = true;
					}
				}

				CardsPlayedThisTurn = -999;
			}
		}

		return result;
	}
}