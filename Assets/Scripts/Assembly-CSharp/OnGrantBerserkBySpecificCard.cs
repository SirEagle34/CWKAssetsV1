using UnityEngine;

public abstract class OnGrantBerserkBySpecificCard : AbilityState
{
    protected CreatureState CreatureAffected;
    protected CardData SourceCard;

    public override bool ProcessMessage(GameMessage Message)
    {
        if (Message.Action != GameEvent.ENABLE_BERSERK)
            return false;

        if (Message.Card == null)
            return false;

        // Sadece bu 3 kart
        if (Message.Card.ID != "SAND_0033" &&
            Message.Card.ID != "SAND_0034" &&
            Message.Card.ID != "MAGMA_0009")
            return false;

        CreatureAffected = Message.Creature;
        SourceCard = Message.Card;

        return OnEnable();
    }
}