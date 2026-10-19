using UnityEngine;
public class TeamDerangedEachTurnAndGrantShieldOnDeploy : OnStartTurn
{
    public override bool ProcessMessage(GameMessage Message)
    {
        bool result = false;

        // Deploy -> Owner'a Shield
        if (Message.Action == GameEvent.CREATURE_DEPLOYED && Message.Creature == base.Owner)
        {
            ApplyStatus(base.Owner, StatusEnum.Shield, base.Val2); // Val2 = shield amount
            return true;
        }

        // Turn başı -> takıma Deranged
        if (Message.Action == GameEvent.START_TURN && Message.WhichPlayer == base.Owner.Owner)
        {
            foreach (LaneState lane in base.Owner.Owner.Lanes)
            {
                if (lane.Creature != null)
                {
                    ApplyStatus(lane.Creature, StatusEnum.Deranged, base.Val1);
                    result = true;
                }
            }

            return result;
        }

        return false;
    }

    public override bool OnEnable()
    {
        return true; // pasif aktif
    }
}