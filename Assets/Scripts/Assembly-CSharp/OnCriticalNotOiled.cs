public abstract class OnCriticalNotOiled : AbilityState
{
    protected CreatureState Target;

    public override bool ProcessMessage(GameMessage Message)
    {
        if (Message.Action == GameEvent.CREATURE_ATTACKED &&
            Message.IsCritical &&
            Message.Creature == base.Owner)
        {
            // Oiled durumundayken tetiklenmesin
            if (base.Owner.IsOiled) // veya HasStatus(StatusEnum.Oiled)
                return false;

            Target = Message.SecondCreature;
            return OnEnable();
        }

        return false;
    }
}