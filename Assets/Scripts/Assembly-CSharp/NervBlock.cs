using UnityEngine;

public class NervBlock : StatusState
{
    public override void ProcessMessage(GameMessage Message)
    {
        // Only care about attack events
        if (Message.Action != GameEvent.CREATURE_ATTACKED)
            return;

        // Message.Creature = attacker
        if (Message.Creature != Target)
            return;

        // Report NervBlock activation
        ReportStatusAction(GameEvent.STATUS_ACTION_NERVBLOCK, Count);

        // Apply CardBlock to the attacking (friendly) creature
        Target.ApplyStatus(
            StatusDataManager.Instance.GetData("CardBlock"),
            3f,
            null
        );

        // Reduce NervBlock count and auto-disable if 0
        TickCount();
    }
}
