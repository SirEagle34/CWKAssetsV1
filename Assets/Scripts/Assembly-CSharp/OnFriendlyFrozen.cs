public abstract class OnFriendlyFrozen : AbilityState
{
	protected CreatureState CreatureAffected;

	public override bool ProcessMessage(GameMessage Message)
	{
		if (Message.Action == GameEvent.ENABLE_FROZEN &&
			Message.Creature.Owner == base.Owner.Owner)
		{
			CreatureAffected = Message.Creature;
			return OnEnable();
		}
		return false;
	}
}