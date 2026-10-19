public abstract class OnFriendlyMagicAttack : AbilityState
{
	protected CreatureState Attacker;
	protected CreatureState Target;

	protected GameMessage AttackMessage;

	public override bool ProcessMessage(GameMessage Message)
	{
		if (Message.Action == GameEvent.CREATURE_ATTACKED &&
			Message.Creature.Owner == base.Owner.Owner &&
			Message.AttackType == AttackBase.INT)
		{
			AttackMessage = Message;

			Attacker = Message.Creature;
			Target = Message.SecondCreature;

			return OnEnable();
		}

		return false;
	}
}