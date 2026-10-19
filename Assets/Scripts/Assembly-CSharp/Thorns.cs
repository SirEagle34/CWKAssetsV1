using UnityEngine;
public class Thorns : StatusState
{
    public override void ProcessMessage(GameMessage Message)
    {
        if (Message.Action == GameEvent.CREATURE_ATTACKED &&
            Message.SecondCreature == base.Target &&
            (Message.AttackType == AttackBase.STR || Message.AttackType == AttackBase.Both) &&
            !Message.IsMiss)
        {
            float num = Message.RawAmount * base.Intensity;

            if (!Message.Creature.BreakDEF)
            {
                num *= 1f - Message.Creature.DEF / 100f;
            }

            int dmg = Mathf.Max(1, Mathf.CeilToInt(num));

            CreatureState oldTarget = base.Target;
            base.Target = Message.Creature;
            ReportStatusAction(GameEvent.STATUS_ACTION_THORNS, dmg);
            base.Target = oldTarget;

            Message.Creature.DealDamage(dmg, AttackBase.None, false, null, this);
        }
    }
}
