public abstract class OnCriticalMagic : AbilityState
{
    protected CreatureState Target;

    public override bool ProcessMessage(GameMessage Message)
    {
        if (Message.Action == GameEvent.CREATURE_ATTACKED &&
            Message.IsCritical &&
            Message.Creature == base.Owner &&
            Message.MagicDamage &&
            !Message.PhysicalDamage)
        {
            // Oiled durumundayken tetiklenmesin
            if (base.Owner.IsOiled)
                return false;

            Target = Message.SecondCreature;
            return OnEnable();
        }

        return false;
    }
}