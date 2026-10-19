public abstract class OnEnemyBurned : AbilityState
{
	protected CreatureState CreatureAffected;

	public override bool ProcessMessage(GameMessage Message)
	{
		if (Message.Action == GameEvent.ENABLE_BURNED && Message.Creature.Owner == base.Owner.Owner.Opponent)
		{
			CreatureAffected = Message.Creature;
			return OnEnable();
		}
		return false;
	}
}
