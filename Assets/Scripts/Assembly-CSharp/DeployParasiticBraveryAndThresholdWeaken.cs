using UnityEngine;

public class DeployParasiticBraveryAndThresholdWeaken : AbilityState
{
	public override bool ProcessMessage(GameMessage Message)
	{
		bool result = false;

		// OnDeploy
		if (Message.Action == GameEvent.CREATURE_DEPLOYED &&
			Message.Creature == base.Owner)
		{
			ApplyStatus(base.Owner, StatusEnum.Parisitic, base.Val3);
			ApplyStatus(base.Owner, StatusEnum.Bravery, base.Val3);
			result = true;
		}

		// OnCardThreshold reset
		if (Message.Action == GameEvent.START_TURN &&
			Message.WhichPlayer == base.Owner.Owner)
		{
			IsActive = false;
			StoredValue = 0;
		}

		// OnCardThreshold
		if (!IsActive &&
			Message.Action == GameEvent.CARD_PLAYED &&
			Message.WhichPlayer == base.Owner.Owner)
		{
			StoredValue++;

			if (StoredValue >= base.Val1)
			{
				foreach (LaneState lane in base.Owner.Owner.Opponent.Lanes)
				{
					if (lane.Creature != null)
					{
						ApplyStatus(lane.Creature, StatusEnum.WeakenINT, base.Val2);
						ApplyStatus(lane.Creature, StatusEnum.WeakenSTR, base.Val2);
						result = true;
					}
				}

				IsActive = true;
			}
		}

		return result;
	}
}