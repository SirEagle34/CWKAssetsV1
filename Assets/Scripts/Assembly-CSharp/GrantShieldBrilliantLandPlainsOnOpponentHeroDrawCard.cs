using UnityEngine;

public class GrantShieldBrilliantLandPlainsOnOpponentHeroDrawCard : OnOpponentHeroCardDraw
{
    private int drawCount = 0;

    public override bool ProcessMessage(GameMessage Message)
    {
        if (Message.Action == GameEvent.DRAW_CARD &&
            Message.WhichPlayer == base.Owner.Owner.Opponent &&
            Message.Creature == null)
        {
            drawCount++;

            // 2, 4, 6... draw
            if (drawCount % 2 == 0)
            {
                CardDrawn = Message.Card;
                return OnEnable();
            }
        }

        // turn reset
        if (Message.Action == GameEvent.END_TURN)
        {
            drawCount = 0;
        }

        return false;
    }

    public override bool OnEnable()
    {
        bool result = false;

        foreach (LaneState lane in base.Owner.Owner.Lanes)
        {
            if (lane.Creature != null)
            {
                ApplyStatus(lane.Creature, StatusEnum.LandPlains, 1f);
                ApplyStatus(lane.Creature, StatusEnum.Shield, base.Val1);
                ApplyStatus(lane.Creature, StatusEnum.Brilliant, base.Val2);
                result = true;
            }
        }

        return result;
    }
}