using UnityEngine;

public class PoisonBlindOnAllyDied : OnAllyDied
{
    public override bool OnEnable()
    {
        bool result = false;

        foreach (LaneState lane in base.Owner.Owner.Opponent.Lanes)
        {
            if (lane.Creature != null &&
                lane.Creature.Owner != base.Owner.Owner)
            {
                ApplyStatus(lane.Creature, StatusEnum.Poison, base.Val1);
                ApplyStatus(lane.Creature, StatusEnum.Blind, base.Val2);
                result = true;
            }
        }

        return result;
    }
}