public class CounterAndBraveryAndAttackSpikeOnOtherFriendlyAttackedV1 : OnFriendlyAttacked
{
    public override bool ProcessMessage(GameMessage Message)
    {
        if (Message.Action == GameEvent.CREATURE_ATTACKED &&
            Message.SecondCreature != null &&
            Message.SecondCreature.Owner == base.Owner.Owner &&
            Message.SecondCreature != base.Owner)
        {
            ApplyStatus(base.Owner, StatusEnum.Counterattack, base.Val1);
            ApplyStatus(base.Owner, StatusEnum.Bravery, base.Val2);
            ApplyStatus(base.Owner, StatusEnum.AttackSpike, base.Val3);
            return true;
        }

        return false;
    }

    public override bool OnEnable()
    {
        return false;
    }
}