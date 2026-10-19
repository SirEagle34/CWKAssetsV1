using UnityEngine;

public class AdrenalineDeployAndBurstSTRPerCard : AbilityState
{
    public override bool ProcessMessage(GameMessage Message)
    {
        // Deploy -> Adrenaline 3T
        if (Message.Action == GameEvent.CREATURE_DEPLOYED &&
            Message.Creature == base.Owner)
        {
            ApplyStatus(base.Owner, StatusEnum.Adrenaline, base.Val2);
            return true;
        }

        // Start Turn -> BurstSTR for each card in hand
        if (Message.Action == GameEvent.START_TURN &&
            Message.WhichPlayer == base.Owner.Owner)
        {
            int handCount = base.Owner.Owner.Hand.Count;

            if (handCount > 0)
            {
                ApplyStatus(
                    base.Owner,
                    StatusEnum.BurstSTR,
                    base.Val1 * handCount
                );

                return true;
            }
        }

        return false;
    }
}