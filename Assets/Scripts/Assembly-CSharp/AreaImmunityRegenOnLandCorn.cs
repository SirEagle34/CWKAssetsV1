using UnityEngine;

public class AreaImmunityRegenOnLandCorn : OnLandCorn
{
    public override bool OnEnable()
    {
        foreach (LaneState thisAndAdjacentLane in base.Owner.Lane.ThisAndAdjacentLanes)
        {
            if (thisAndAdjacentLane.Creature != null)
            {
                ApplyStatus(thisAndAdjacentLane.Creature, StatusEnum.Regen, base.Val1);
				ApplyStatus(thisAndAdjacentLane.Creature, StatusEnum.Immunity, base.Val2);
            }
        }

        return true;
    }
}