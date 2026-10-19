using UnityEngine;

public class Necro : StatusState
{
    public override void ProcessMessage(GameMessage Message)
    {
        if (Target == null)
            return;

        if (Message.Action != GameEvent.CREATURE_ATTACKED)
            return;

        if (Message.Creature != Target)
            return;

        if (!Message.Presented)
            return;

        CreatureState enemyCreature = Message.SecondCreature;
        if (enemyCreature == null)
            return;

        PlayerState attackerOwner = Target.Owner;
        PlayerState defenderOwner = enemyCreature.Owner;

        if (attackerOwner == null || defenderOwner == null)
            return;

        LaneState attackerLane = Target.Lane;
        LaneState defenderLane = enemyCreature.Lane;

        if (attackerLane == null || defenderLane == null)
            return;

        // IMPORTANT: update lanes before swapping
        Target.Lane = defenderLane;
        enemyCreature.Lane = attackerLane;

        if (attackerOwner == defenderOwner)
        {
            attackerOwner.SwapCreature(enemyCreature, attackerLane);
        }
        else
        {
            attackerOwner.RemoveCreature(attackerLane);
            defenderOwner.RemoveCreature(defenderLane);

            attackerOwner.PlaceCreature(enemyCreature, attackerLane);
            defenderOwner.PlaceCreature(Target, defenderLane);
        }

        ReportStatusAction(GameEvent.STATUS_ACTION_NECRO, 0);
    }
}


