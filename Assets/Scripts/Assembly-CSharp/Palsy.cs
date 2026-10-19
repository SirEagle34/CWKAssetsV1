using UnityEngine;

public class Palsy : StatusState
{
    public override void ProcessMessage(GameMessage Message)
    {
        // Only care about attack events
        if (Message.Action != GameEvent.CREATURE_ATTACKED)
            return;

        // Message.Creature = attacker
        if (Message.Creature != Target)
            return;

        // Report Palsy activation
        ReportStatusAction(GameEvent.STATUS_ACTION_PALSY, Count);

        // Apply Paralyze to the attacking (friendly) creature
        Target.ApplyStatus(
            StatusDataManager.Instance.GetData("Paralyze"),
            1f,
            null
        );

        // Reduce Palsy count and auto-disable if 0
        TickCount();
    }
}
