using UnityEngine;

public class TeamDefenseAndResistanceOnDeploy_BurstSTROnStartTurn : AbilityState
{
    public override bool ProcessMessage(GameMessage Message)
    {
        // Building deploy edildiğinde
        if (Message.Action == GameEvent.CREATURE_DEPLOYED &&
            Message.Creature == base.Owner)
        {
            foreach (LaneState lane in base.Owner.Owner.Lanes)
            {
                if (lane.Creature == null)
                    continue;

                ApplyStatus(lane.Creature, StatusEnum.FortifyDEF, base.Val1);
                ApplyStatus(lane.Creature, StatusEnum.FortifyRES, base.Val1);
            }

            return true;
        }

        // Sahibinin her tur başlangıcında
        if (Message.Action == GameEvent.START_TURN &&
            Message.WhichPlayer == base.Owner.Owner)
        {
            foreach (LaneState lane in base.Owner.Owner.Lanes)
            {
                if (lane.Creature == null)
                    continue;

                ApplyStatus(lane.Creature, StatusEnum.BurstSTR, base.Val2);
            }

            return true;
        }

        return false;
    }
}