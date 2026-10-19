using UnityEngine;

public class BonebreakerCardBlockOnPlayCardAndImmunityThornsOnDeploy : OnCardThreshold
{
    private int _cooldownTurns;

    public override bool ProcessMessage(GameMessage Message)
    {
        // Deploy efekti
        if (Message.Action == GameEvent.CREATURE_DEPLOYED &&
            Message.Creature == base.Owner)
        {
            ApplyStatus(base.Owner, StatusEnum.Immunity, base.Val3);
            ApplyStatus(base.Owner, StatusEnum.Bravery, base.Val3);
            ApplyStatus(base.Owner, StatusEnum.Thorns, base.Val4);
            ApplyStatus(base.Owner, StatusEnum.Brilliant, base.Val4);
            return true;
        }

        // Cooldown azalt
        if (Message.Action == GameEvent.START_TURN &&
            Message.WhichPlayer == base.Owner.Owner)
        {
            if (_cooldownTurns > 0)
                _cooldownTurns--;
        }

        return base.ProcessMessage(Message);
    }

    public override bool OnEnable()
    {
        // Cooldown varsa tetiklenme
        if (_cooldownTurns > 0)
            return false;

        CreatureState highestHpCreature = null;

        foreach (LaneState lane in base.Owner.Owner.Opponent.Lanes)
        {
            if (lane.Creature == null)
                continue;

            if (highestHpCreature == null ||
                lane.Creature.HP > highestHpCreature.HP)
            {
                highestHpCreature = lane.Creature;
            }
        }

        if (highestHpCreature != null)
        {
            ApplyStatus(highestHpCreature, StatusEnum.Bonebreaker, 1f);
            ApplyStatus(highestHpCreature, StatusEnum.CardBlock, base.Val2);

            // 3 tur cooldown başlat
            _cooldownTurns = 3;

            return true;
        }

        return false;
    }
}