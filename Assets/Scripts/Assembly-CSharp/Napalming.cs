using UnityEngine;
public class Napalming : StatusState
{
   public override void ProcessMessage(GameMessage Message)
    {
        if (Message.Action != GameEvent.CREATURE_ATTACKED || Message.IsMiss)
            return;

        if (Message.SecondCreature != base.Target)
            return;

        var burned = StatusDataManager.Instance.GetData("Burned");
        if (burned == null)
            return;

        // 🔥 attacker'a uygula
        Message.Creature.ApplyStatus(burned, 20f, null);
    }
}