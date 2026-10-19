public abstract class OnLandMagma : AbilityState
{
	public override bool ProcessMessage(GameMessage Message)
	{
		if (Message.Action == GameEvent.ENABLE_LANDMAGMA && Message.Creature == base.Owner)
		{
			return OnEnable();
		}
		return false;
	}
}
