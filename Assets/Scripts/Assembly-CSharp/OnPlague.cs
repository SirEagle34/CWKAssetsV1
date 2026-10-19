public abstract class OnPlague : AbilityState
{
    public override bool ProcessMessage(GameMessage Message)
    {
        if (Message.Action == GameEvent.ENABLE_PLAGUE &&
            Message.Creature == base.Owner)
        {
            return OnEnable();
        }

        return false;
    }
}