public class Healer : StatusState
{
    public override void ProcessMessage(GameMessage Message)
    {
        if (base.Target != null &&
            Message.Action == GameEvent.CREATURE_ATTACKED &&
            Message.Creature == base.Target)
        {
            int healAmount = (int)(base.Target.MaxHP * base.Intensity);

            if (healAmount < 1)
                healAmount = 1;

            ReportStatusAction(GameEvent.STATUS_ACTION_HEALER, healAmount);

            base.Target.Heal(healAmount);

            Disable();
        }
    }
}