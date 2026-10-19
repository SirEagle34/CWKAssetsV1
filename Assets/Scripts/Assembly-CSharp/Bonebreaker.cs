using UnityEngine;

public class Bonebreaker : StatusState
{
    public override void ProcessMessage(GameMessage Message)
    {
        // Only trigger on attack
        if (Message.Action != GameEvent.CREATURE_ATTACKED)
            return;

        // Only when the affected creature attacks
        if (Message.Creature != Target)
            return;

        int damage = Mathf.CeilToInt(Target.MaxHP * Intensity);

        // Report status action
        ReportStatusAction(GameEvent.STATUS_ACTION_BONEBREAKER, damage);

        // Deal percentage-based damage
        Target.DealDamage(
            damage,
            AttackBase.None,
            false,
            null,
            this
        );

        TickCount();
    }
}
