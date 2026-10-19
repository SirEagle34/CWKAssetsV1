using UnityEngine;

public class Heal30PctAndMindlessEndTurn : AbilityState
{
    private bool healedThisTurn;

    public override bool ProcessMessage(GameMessage Message)
    {
        // Sahibinin turu başladığında resetle
        if (Message.Action == GameEvent.START_TURN &&
            Message.WhichPlayer == base.Owner.Owner)
        {
            healedThisTurn = false;
        }

        // Bir turda sadece 1 kez heal
        if (!healedThisTurn &&
            Message.Action == GameEvent.CARD_PLAYED &&
            Message.Creature == base.Owner)
        {
            base.Owner.Heal(base.Val1Pct);
            healedThisTurn = true;
            return true;
        }

        // Tur sonunda Mindless ver
        if (Message.Action == GameEvent.END_TURN &&
            Message.WhichPlayer == base.Owner.Owner)
        {
            ApplyStatus(base.Owner, StatusEnum.Mindless, base.Val2);
            return true;
        }

        return false;
    }
}