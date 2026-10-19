using UnityEngine;

public class FMSiphon : StatusState
{
    public override void ProcessMessage(GameMessage Message)
    {
        if (base.Target != null &&
            Message.Action == GameEvent.CREATURE_ATTACKED &&
            Message.Creature == base.Target)
        {
            // how much meter to steal
            int num = Mathf.RoundToInt(base.Intensity * 100f);

            // optional combat log
            ReportStatusAction(GameEvent.STATUS_ACTION_APSIPHON, num);

            // steal AP meter
            base.Target.Owner.Opponent.FillAPMeter(-num); // drain enemy meter
            base.Target.Owner.FillAPMeter(num);           // gain meter

            Disable(); // one-time like APSiphon
        }
    }
}
