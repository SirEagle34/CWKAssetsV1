public abstract class OnEnhanceSTR : AbilityState
{
	public override bool ProcessMessage(GameMessage Message)
	{
		if (Message.Action == GameEvent.ENABLE_ENHANCESTR && Message.Creature == base.Owner)
		{
			return OnEnable();
		}
		return false;
	}
}
