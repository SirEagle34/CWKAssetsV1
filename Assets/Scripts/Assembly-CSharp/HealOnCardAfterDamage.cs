using System;
using UnityEngine;
public class HealOnCardAfterDamage : AbilityState
{
	private bool WasDamaged;
	private bool TriggeredThisTurn;
	private int LastDamageTaken;

	public override bool ProcessMessage(GameMessage Message)
	{
		if (Message.Action == GameEvent.DAMAGE_CREATURE &&
			Message.Creature == base.Owner)
		{
			WasDamaged = true;
			LastDamageTaken = (int)Message.Amount;
		}

		if (!TriggeredThisTurn &&
			WasDamaged &&
			Message.Action == GameEvent.CARD_PLAYED &&
			Message.Creature == base.Owner)
		{
			int healAmount = (LastDamageTaken * 30 + 99) / 100;

			base.Owner.Heal(healAmount);

			TriggeredThisTurn = true;
			WasDamaged = false;

			return true;
		}

		if (Message.Action == GameEvent.END_TURN)
		{
			if (TriggeredThisTurn)
			{
				ApplyStatus(base.Owner, StatusEnum.Mindless, 1);
			}

			TriggeredThisTurn = false;
		}

		return false;
	}
}