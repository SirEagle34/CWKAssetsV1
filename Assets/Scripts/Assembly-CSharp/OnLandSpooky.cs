public abstract class OnLandSpooky : AbilityState
{
	public override bool ProcessMessage(GameMessage Message)
	{
		if (Message.Action == GameEvent.ENABLE_LANDSPOOKY && Message.Creature == base.Owner)
		{
			return OnEnable();
		}
		return false;
	}
}
