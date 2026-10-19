using UnityEngine;
public class HealLowHealthCreaturesEachTurnAndWisdomArmorOnDeploy : OnStartTurn
{
    public override bool ProcessMessage(GameMessage Message)
    {
        bool result = false;

        // Deploy olduğunda Brilliant ver
        if (Message.Action == GameEvent.CREATURE_DEPLOYED && Message.Creature == base.Owner)
        {
            ApplyStatus(base.Owner, StatusEnum.Brilliant, 5f);
            result = true;
        }

        // Tur başında düşük HP'lileri heal et
        if (Message.Action == GameEvent.START_TURN && Message.WhichPlayer == base.Owner.Owner)
        {
            foreach (LaneState lane in base.Owner.Owner.Lanes)
            {
                if (lane.Creature != null && lane.Creature.HPPct < base.Val1Pct)
                {
                    lane.Creature.Heal(base.Val2Pct);
                    result = true;
                }
            }
        }

        return result;
    }

    public override bool OnEnable()
    {
        return true;
    }
}