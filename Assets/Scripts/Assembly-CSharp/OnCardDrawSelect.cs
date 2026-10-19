public abstract class OnCardDrawSelect : AbilityState
{
    protected PlayerState WhichPlayer;
    protected CardData CardDrawn;

    public override bool ProcessMessage(GameMessage Message)
    {
        if (Message.Action == GameEvent.DRAW_CARD)
        {
            WhichPlayer = Message.WhichPlayer;
            CardDrawn = Message.Card;
            return OnEnable();
        }

        return false;
    }
}