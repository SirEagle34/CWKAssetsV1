using UnityEngine;

public class DealerOnMagicAttack : OnMagicAttack
{
    public override bool OnEnable()
    {
        if (base.Val1Chance)
        {
            ApplyStatus(base.Owner, StatusEnum.Dealer, base.Val2);
            return true;
        }

        return false;
    }
}