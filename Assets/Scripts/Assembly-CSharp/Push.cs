public class Push : StatusState
{
	protected override void OnEnable()
{
    if (base.Target == null || base.Target.HP <= 0) return;
    if (base.Target.Lane == null) return;

    GameMessage pushMessage = new GameMessage();
    pushMessage.Action = GameEvent.PUSH_CREATURE_TO_HAND;
    pushMessage.WhichPlayer = base.Target.Owner;
    pushMessage.Creature = base.Target;
    base.Target.Owner.Game.AddMessage(pushMessage);
}

}