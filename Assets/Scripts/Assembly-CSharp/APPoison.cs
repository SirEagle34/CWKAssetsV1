public class APPoison : StatusState
{
	public override void ProcessMessage(GameMessage Message)
	{
		if (base.Target != null &&
		    Message.Action == GameEvent.START_TURN &&
		    Message.WhichPlayer == base.Target.Owner &&
		    base.Count > 0)
		{
			base.Target.Owner.LoseActionPoints(1); // flat 1 energy
			TickCount();
		}
	}
}
