using UnityEngine;

public class Sulfide : StatusState
{
    public override void ProcessMessage(GameMessage Message)
    {
        // Only care about attack events
        if (Message.Action != GameEvent.CREATURE_ATTACKED)
            return;

        // Message.Creature = attacker
        if (Message.Creature != Target)
            return;

        // Report Sulfide activation
        ReportStatusAction(GameEvent.STATUS_ACTION_SULFIDE, Count);

        // Apply Plague to the attacking (friendly) creature
        Target.ApplyStatus(
            StatusDataManager.Instance.GetData("Plague"),
            25f,
            null
        );

        // Reduce Sulfide count and auto-disable if 0
        TickCount();
    }
}
