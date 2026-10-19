public class GrantLandPlainsandAPFlowerOnDeploy : OnDeploy
{
    private int delay = 2;

    public override bool OnEnable()
    {
        delay = 2;

        // Sadece deploy edildiğinde çalışır
        foreach (LaneState lane in base.Owner.Owner.Opponent.Lanes)
        {
            if (lane.Creature != null)
            {
                CancelStatus(lane.Creature, StatusEnum.APSiphon);
                CancelStatus(lane.Creature, StatusEnum.APFlower);
                CancelStatus(lane.Creature, StatusEnum.LandPlains);
            }
        }

        return true;
    }

    public override bool ProcessMessage(GameMessage Message)
    {
        if (Message.Action == GameEvent.START_TURN &&
            Message.WhichPlayer == base.Owner.Owner)
        {
            delay--;

            if (delay > 0)
                return false;

            // Her 2 turda bir çalışır
            ApplyStatus(base.Owner, StatusEnum.LandPlains, 1f);
            ApplyStatus(base.Owner, StatusEnum.APFlower, base.Val1);

            delay = 2;

            return true;
        }

        return false;
    }
}