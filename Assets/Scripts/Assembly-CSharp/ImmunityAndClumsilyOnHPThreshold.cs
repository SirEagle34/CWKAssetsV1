using UnityEngine;
using System.Collections.Generic;

public class ImmunityAndClumsilyOnHPThreshold : OnHPThreshold 
{
    private bool _triggered = false;

    public override bool OnEnable()
    {
        if (_triggered)
            return false;

        if (base.Owner == null || base.Owner.Lane == null)
            return false;

        foreach (var adjacent in base.Owner.Lane.AdjacentLanes)
        {
            if (adjacent?.Creature == null)
                continue;

            ApplyStatus(adjacent.Creature, StatusEnum.Immunity, 3f);
            ApplyStatus(adjacent.Creature, StatusEnum.Clumsily, 3f);
        }

        _triggered = true;
        return true;
    }
}