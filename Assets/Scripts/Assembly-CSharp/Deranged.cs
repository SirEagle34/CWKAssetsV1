using UnityEngine;
public class Deranged : InflictStatus
{
    public override void ProcessMessage(GameMessage Message)
    {
        if (Message.Action != GameEvent.CREATURE_ATTACKED || Message.SecondCreature != base.Target || Message.IsMiss)
        {
            return;
        }

        // Original InflictStatus behavior (WeakenSTR on attacker)
        base.ProcessMessage(Message);
    }
}