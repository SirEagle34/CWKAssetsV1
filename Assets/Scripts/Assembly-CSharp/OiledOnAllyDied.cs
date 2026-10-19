using UnityEngine;

public class OiledOnAllyDied : OnAllyDied
{
    public override bool OnEnable()
    {
        bool result = false;

        foreach (LaneState lane in base.Owner.Owner.Opponent.Lanes)
        {
            if (lane.Creature != null &&
                lane.Creature.Owner != base.Owner.Owner)
            {
                ApplyStatus(lane.Creature, StatusEnum.Oiled, base.Val1);
                result = true;
            }
        }

        return result;
    }
}