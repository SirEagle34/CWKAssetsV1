using UnityEngine;

public class FortifyDEFAndCleanseOnFriendlyBravery : OnFriendlyBravery
{
    public override bool OnEnable()
    {
        ApplyStatus(Target, StatusEnum.FortifyDEF, base.Val1);

        ApplyStatus(
            Target,
            StatusEnum.RemoveStatus,
            base.Val2,
            StatusRemovalData.RemoveRandomDebuffs()
        );

        return true;
    }
}