public abstract class OnFortifyDEF : AbilityState
{
	public override bool ProcessMessage(GameMessage Message)
	{
		if (Message.Action == GameEvent.ENABLE_FORTIFYDEF && Message.Creature == base.Owner)
		{
			return OnEnable();
		}
		return false;
	}
}
