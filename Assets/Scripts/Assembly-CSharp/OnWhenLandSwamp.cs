public abstract class OnWhenLandSwamp : AbilityState
{
	protected CreatureState Target;

	public override bool ProcessMessage(GameMessage Message)
	{
		if (Message.Action == GameEvent.ENABLE_LANDSWAMP &&
			Message.Creature != null)
		{
			Target = Message.Creature;

			return OnEnable();
		}

		return false;
	}
}