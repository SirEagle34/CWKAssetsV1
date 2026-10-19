using UnityEngine;

public class MultiImmunityArmorOnMoreThan4Debuffs : AbilityState
{
    public override bool ProcessMessage(GameMessage Message)
    {
        if (Message.Creature == base.Owner)
        {
            int debuffCount = base.Owner.GetStatusTypeCount(StatusType.Debuff);

            if (debuffCount > 4)
            {
                ApplyStatus(base.Owner, StatusEnum.MultiImmunity, base.Val1);
                return true;
            }
        }

        return false;
    }
}