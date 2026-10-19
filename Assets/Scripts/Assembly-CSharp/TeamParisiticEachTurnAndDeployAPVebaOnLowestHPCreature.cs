using UnityEngine;
public class TeamParisiticEachTurnAndDeployAPVebaOnLowestHPCreature : OnStartTurn
{
    public override bool ProcessMessage(GameMessage Message)
    {
        bool result = false;

        // Deploy -> enemy lowest HP creature'a APVeba
        if (Message.Action == GameEvent.CREATURE_DEPLOYED && Message.Creature == base.Owner)
        {
            CreatureState lowest = null;

            foreach (LaneState lane in base.Owner.Owner.Opponent.Lanes)
            {
                if (lane.Creature != null && (lowest == null || lane.Creature.HP < lowest.HP))
                {
                    lowest = lane.Creature;
                }
            }

            if (lowest != null)
            {
                ApplyStatus(lowest, StatusEnum.APVeba, base.Val2);
                return true;
            }

            return false;
        }

        // Turn başı -> takımına Parasitic
        if (Message.Action == GameEvent.START_TURN && Message.WhichPlayer == base.Owner.Owner)
        {
            foreach (LaneState lane in base.Owner.Owner.Lanes)
            {
                if (lane.Creature != null)
                {
                    ApplyStatus(lane.Creature, StatusEnum.Parisitic, base.Val1);
                    result = true;
                }
            }

            return result;
        }

        return false;
    }

    public override bool OnEnable()
    {
        return true; // sadece pasif aktif olsun
    }
}