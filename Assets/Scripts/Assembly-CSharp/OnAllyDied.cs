public abstract class OnAllyDied : AbilityState
{
    protected StatusData CausedByStatus;

    public override bool ProcessMessage(GameMessage Message)
    {
        if (Message.Action == GameEvent.CREATURE_DIED &&
            Message.Creature.Owner == base.Owner.Owner)
        {
            CausedByStatus = Message.Status;
            return OnEnable();
        }
        return false;
    }
}
