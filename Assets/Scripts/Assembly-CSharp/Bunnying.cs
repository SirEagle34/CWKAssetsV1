using UnityEngine;

public class Bunnying : StatusState
{
    public override void ProcessMessage(GameMessage Message)
    {
        // Only care about attack events
        if (Message.Action != GameEvent.CREATURE_ATTACKED)
            return;

        // Message.Creature = attacker
        if (Message.Creature != Target)
            return;

        // Report Bunnying activation
        ReportStatusAction(GameEvent.STATUS_ACTION_BUNNYING, Count);

        // Apply Transmogrify to the attacking (friendly) creature
        Target.ApplyStatus(
            StatusDataManager.Instance.GetData("Transmogrify"),
            2f,
            null
        );

        // Reduce Bunnying count and auto-disable if 0
        TickCount();
    }
}
