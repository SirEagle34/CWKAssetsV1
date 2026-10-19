public class BerserkBlindHighestEnemyOn6Cards : OnCardThreshold
{
    private int cooldown = 0;

    public override bool ProcessMessage(GameMessage Message)
    {
        // cooldown azaltma
        if (Message.Action == GameEvent.START_TURN &&
            Message.WhichPlayer == base.Owner.Owner)
        {
            if (cooldown > 0)
                cooldown--;
        }

        return base.ProcessMessage(Message);
    }

    public override bool OnEnable()
    {
        // cooldown aktifse çalışmasın
        if (cooldown > 0)
            return false;

        CreatureState highestHpCreature = null;

        foreach (LaneState lane in base.Owner.Owner.Opponent.Lanes)
        {
            if (lane.Creature == null)
                continue;

            if (highestHpCreature == null || lane.Creature.HP > highestHpCreature.HP)
            {
                highestHpCreature = lane.Creature;
            }
        }

        if (highestHpCreature != null)
        {
            ApplyStatus(highestHpCreature, StatusEnum.Berserk, 1f);
            ApplyStatus(highestHpCreature, StatusEnum.Blind, 1f);

            // cooldown başlat
            cooldown = 3;

            return true;
        }

        return false;
    }
}