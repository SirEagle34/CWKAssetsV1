using UnityEngine;

public class FortifyTeamRemovDebuffOnLandSand : OnLandSand
{
    public override bool OnEnable()
    {
        foreach (LaneState lane in base.Owner.Owner.Lanes)
        {
            if (lane.Creature != null)
            {
                ApplyStatus(lane.Creature, StatusEnum.FortifyDEF, base.Val1);
                ApplyStatus(lane.Creature, StatusEnum.FortifyRES, base.Val2);

                // 2 debuff kaldır
                ApplyStatus(lane.Creature, StatusEnum.RemoveStatus, base.Val3, StatusRemovalData.RemoveRandomDebuffs());
            }
        }

        return true;
    }
}