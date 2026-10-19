public abstract class OnFriendlyPoison : AbilityState
{
	protected CreatureState CreatureAffected;

	public override bool ProcessMessage(GameMessage Message)
	{
		if (Message.Action == GameEvent.ENABLE_POISON &&
			Message.Creature.Owner == base.Owner.Owner)
		{
			CreatureAffected = Message.Creature;
			return OnEnable();
		}
		return false;
	}
}