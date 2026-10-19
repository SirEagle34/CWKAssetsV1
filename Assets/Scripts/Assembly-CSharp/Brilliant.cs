using UnityEngine;
public class Brilliant : InflictStatus
{
    public override void ProcessMessage(GameMessage Message)
    {
        if (Message.Action != GameEvent.CREATURE_ATTACKED || Message.SecondCreature != base.Target || Message.IsMiss)
        {
            return;
        }

        // Reflect INT damage back at attacker
        if (Message.AttackType == AttackBase.INT || Message.AttackType == AttackBase.Both)
        {
            float num = Message.RawAmount;
            if (!Message.Creature.BreakRES)
            {
                num *= 1f - Message.Creature.RES / 100f;
            }
            int dmg = Mathf.Max(1, Mathf.CeilToInt(num));
            CreatureState oldTarget = base.Target;
            base.Target = Message.Creature;
            ReportStatusAction(GameEvent.STATUS_ACTION_THORNS, dmg);
            base.Target = oldTarget;
            Message.Creature.DealDamage(dmg, AttackBase.None, false, null, this);
        }

        // Original InflictStatus behavior (WeakenINT on attacker)
        base.ProcessMessage(Message);
    }
}
