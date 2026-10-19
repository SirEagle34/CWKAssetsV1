public abstract class OnFriendlyDealer : AbilityState
{
	protected CreatureState Target;

	public override bool ProcessMessage(GameMessage Message)
	{
		if (Message.Action == GameEvent.ENABLE_DEALER && Message.Creature.Owner == base.Owner.Owner)
		{
			Target = Message.Creature;
			return OnEnable();
		}
		return false;
	}
}
