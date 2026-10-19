public abstract class OnWhenFriendlyLandSwamp : AbilityState
{
	protected CreatureState Target;

	public override bool ProcessMessage(GameMessage Message)
	{
		if (Message.Action == GameEvent.ENABLE_LANDSWAMP &&
			Message.Creature != null &&
			Message.Creature.Owner == base.Owner.Owner)
		{
			Target = Message.Creature;
			return OnEnable();
		}

		return false;
	}
}