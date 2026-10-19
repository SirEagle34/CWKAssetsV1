using UnityEngine;

public class Frostbite : StatusState
{
    public override void ProcessMessage(GameMessage Message)
    {
        // Only care about attack events
        if (Message.Action != GameEvent.CREATURE_ATTACKED)
            return;

        // Message.Creature = attacker
        if (Message.Creature != Target)
            return;

        // Report Frostbite activation
        ReportStatusAction(GameEvent.STATUS_ACTION_FROSTBITE, Count);

        // Apply Frozen to the attacking (friendly) creature
        Target.ApplyStatus(
            StatusDataManager.Instance.GetData("Frozen"),
            1f,
            null
        );

        // Reduce Frostbite count and auto-disable if 0
        TickCount();
    }
}
