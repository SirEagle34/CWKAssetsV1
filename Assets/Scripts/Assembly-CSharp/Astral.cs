using UnityEngine;
public class Astral : StatusState
{
   public override void ProcessMessage(GameMessage Message)
    {
        if (Message.Action != GameEvent.CREATURE_ATTACKED || Message.IsMiss)
            return;

        if (Message.SecondCreature != base.Target)
            return;

        var curseastral = StatusDataManager.Instance.GetData("APPoison");
        if (curseastral == null)
            return;

        // 🔥 attacker'a uygula
        Message.Creature.ApplyStatus(curseastral, 1f, null);
    }
}