using UnityEngine;

public abstract class OnDamagedCreature : AbilityState
{
	protected CreatureState Target;

	public override bool ProcessMessage(GameMessage Message)
	{
		if (Message.Action == GameEvent.DAMAGE_CREATURE &&
			Message.Creature == base.Owner)
		{
			Target = Message.Creature;
			return OnEnable();
		}

		return false;
	}

	public abstract bool OnEnable();
}