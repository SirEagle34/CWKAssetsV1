public abstract class OnGainBuff : AbilityState
{
    protected StatusData Status;
    protected float Amount;
    protected CardData Card;

    public override bool ProcessMessage(GameMessage Message)
    {
        if (Message.Action == GameEvent.GAIN_BUFF &&
            Message.Creature == base.Owner)
        {
            Status = Message.Status;
            Amount = Message.Amount;

            return OnEnable();
        }

        return false;
    }

    public abstract bool OnEnable();
}