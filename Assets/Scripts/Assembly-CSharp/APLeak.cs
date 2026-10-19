public class APLeak : StatusState
{
    public override void ProcessMessage(GameMessage Message)
    {
        if (Message.Action != GameEvent.CREATURE_ATTACKED)
            return;

        if (Message.Creature != Target)
            return;

        int num = 2;

        ReportStatusAction(GameEvent.STATUS_ACTION_APLEAK, num);
        Target.Owner.LoseActionPoints(num);

		TickCount();
    }
}