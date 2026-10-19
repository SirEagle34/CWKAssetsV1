using UnityEngine;

public class RemoveEnemyAttackUpHasteBullEyeCounterattackAndFortitudeOnStartTurn : OnStartTurn
{

    private int Count = 0;

    public override bool OnEnable()
    {
        Count++;

        if (Count < 2)
            return false;

        Count = 0;

        bool result = false;

        foreach (LaneState lane in base.Owner.Owner.Opponent.Lanes)
        {
            if (lane.Creature != null)
            {
                CancelStatus(lane.Creature, StatusEnum.EnhanceSTR);
                CancelStatus(lane.Creature, StatusEnum.BurstSTR);
                CancelStatus(lane.Creature, StatusEnum.Adrenaline);
                CancelStatus(lane.Creature, StatusEnum.Pinpoint);
                CancelStatus(lane.Creature, StatusEnum.StrengthCounter);

                result = true;
            }
        }

        return result;
    }
}