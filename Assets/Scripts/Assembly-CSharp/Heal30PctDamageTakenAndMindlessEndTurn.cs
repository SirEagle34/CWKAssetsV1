using UnityEngine;

public class Heal30PctDamageTakenAndMindlessEndTurn : AbilityState
{
    private bool TriggeredThisTurn;

    public override bool ProcessMessage(GameMessage Message)
    {
        if (!TriggeredThisTurn &&
            Message.Action == GameEvent.CARD_PLAYED &&
            Message.Creature == base.Owner)
        {
            // Kendine %30 heal
            base.Owner.Heal(base.Val1Pct);

            TriggeredThisTurn = true;
            return true;
        }

        if (Message.Action == GameEvent.END_TURN)
        {
            if (TriggeredThisTurn)
            {
                ApplyStatus(base.Owner, StatusEnum.Mindless, base.Val2);
            }

            TriggeredThisTurn = false;
        }

        return false;
    }
}