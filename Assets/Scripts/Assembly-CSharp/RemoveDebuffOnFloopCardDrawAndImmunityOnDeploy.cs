public class RemoveDebuffOnFloopCardDrawAndImmunityOnDeploy : OnDeploy
{
    private bool _cardDrawSetup = false;

    public override bool OnEnable()
    {
        // Grant team immunity on deploy
        foreach (LaneState lane in base.Owner.Owner.Lanes)
        {
            if (lane.Creature != null)
            {
                ApplyStatus(lane.Creature, StatusEnum.Immunity, base.Val2);
            }
        }
        _cardDrawSetup = true;
        return true;
    }

    public override bool ProcessMessage(GameMessage Message)
    {
        if (_cardDrawSetup && Message.Action == GameEvent.DRAW_CARD && Message.WhichPlayer == base.Owner.Owner)
        {
            if (Message.Card != null && Message.Card.IsLeaderCard)
            {
                foreach (LaneState lane in base.Owner.Owner.Lanes)
                {
                    if (lane.Creature != null)
                    {
                        ApplyStatus(lane.Creature, StatusEnum.RemoveStatus, base.Val1, StatusRemovalData.RemoveRandomDebuffs());
                    }
                }
                return true;
            }
        }
        return base.ProcessMessage(Message);
    }
}
