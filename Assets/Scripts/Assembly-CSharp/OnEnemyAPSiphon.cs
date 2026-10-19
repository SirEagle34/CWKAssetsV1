public abstract class OnEnemyAPSiphon : AbilityState
{
	protected CreatureState CreatureAffected;

	public override bool ProcessMessage(GameMessage Message)
	{
		if (Message.Action == GameEvent.ENABLE_APSIPHON && Message.Creature.Owner == base.Owner.Owner.Opponent)
		{
			CreatureAffected = Message.Creature;
			return OnEnable();
		}
		return false;
	}
}
