using UnityEngine;

public class RemoveShieldCrippleAndWeakenOnEnemyThorns : OnEnemyThorns
{
    public override bool OnEnable()
    {
        CancelStatus(CreatureAffected, StatusEnum.Shield);

        ApplyStatus(CreatureAffected, StatusEnum.Cripple, base.Val1);
        ApplyStatus(CreatureAffected, StatusEnum.WeakenSTR, base.Val2);

        return true;
    }
}