public class CounterAndBraveryAndAttackSpikeOnOtherFriendlyAttackedV4 : OnFriendlyAttacked
{
    public override bool ProcessMessage(GameMessage Message)
    {
        // Reset once per turn
        if (Message.Action == GameEvent.START_TURN && Message.WhichPlayer == base.Owner.Owner)
        {
            IsActive = false;
            return false;
        }

        // A different friendly was attacked - once per turn only
        if (Message.Action == GameEvent.CREATURE_ATTACKED && 
            Message.SecondCreature.Owner == base.Owner.Owner && 
            Message.SecondCreature != base.Owner)
        {
            if (IsActive) return false;
            ApplyStatus(base.Owner, StatusEnum.Counterattack, base.Val1);
            ApplyStatus(base.Owner, StatusEnum.Bravery, base.Val2);
            ApplyStatus(base.Owner, StatusEnum.AttackSpike, base.Val3);
            IsActive = true;
            return true;
        }

        return false;
    }

    public override bool OnEnable()
    {
        return false;
    }
}