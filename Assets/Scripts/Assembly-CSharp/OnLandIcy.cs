public abstract class OnLandIcy : AbilityState
{
	public override bool ProcessMessage(GameMessage Message)
	{
		if (Message.Action == GameEvent.ENABLE_LANDICY && Message.Creature == base.Owner)
		{
			return OnEnable();
		}
		return false;
	}
}
