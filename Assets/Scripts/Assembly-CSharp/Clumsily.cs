using UnityEngine;
public class Clumsily : StatusState
{
   public override void ProcessMessage(GameMessage Message)
    {
        if (Message.Action != GameEvent.CREATURE_ATTACKED || Message.IsMiss)
            return;

        if (Message.SecondCreature != base.Target)
            return;

        var confuse = StatusDataManager.Instance.GetData("Confuse");
        if (confuse == null)
            return;

        // 🔥 attacker'a uygula
        Message.Creature.ApplyStatus(confuse, 3f, null);
    }
}