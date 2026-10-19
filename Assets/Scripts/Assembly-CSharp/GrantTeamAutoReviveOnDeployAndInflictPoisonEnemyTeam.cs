using UnityEngine;
public class GrantTeamAutoReviveOnDeployAndInflictPoisonEnemyTeam : OnStartTurn
{
    public override bool ProcessMessage(GameMessage Message)
    {
        if (Message.Action == GameEvent.CREATURE_DEPLOYED && Message.Creature == base.Owner)
        {
            bool result = false;

            // 🟢 Kendi takıma AutoRevive
            foreach (LaneState lane in base.Owner.Owner.Lanes)
            {
                if (lane.Creature != null)
                {
                    ApplyStatus(lane.Creature, StatusEnum.AutoRevive, base.Val1);
                    result = true;
                }
            }

            // 🔴 Enemy takıma Poison
            foreach (LaneState lane in base.Owner.Owner.Opponent.Lanes)
            {
                if (lane.Creature != null)
                {
                    ApplyStatus(lane.Creature, StatusEnum.Poison, base.Val2);
                    result = true;
                }
            }

            return result;
        }

        return false;
    }

    public override bool OnEnable()
    {
        return true;
    }
}