using UnityEngine;

public class ParalyzeOnPlagueOver30 : AbilityState
{
    public override bool ProcessMessage(GameMessage Message)
    {
        if (Message.Action == GameEvent.GAIN_DEBUFF &&
            Message.Creature == base.Owner &&
            Message.Status != null &&
            Message.Status.ID == "Plague")
        {
            // Önceki Plague miktarı
            float previousAmount = Message.RawAmount - Message.AmountChange;

            // Bu kazanımla %30 sınırını geçiyorsa
            if (Message.RawAmount > 0.30f && previousAmount <= 0.30f)
            {
                return OnEnable();
            }
        }

        return false;
    }

    public override bool OnEnable()
    {
        foreach (LaneState lane in base.Owner.Owner.Opponent.Lanes)
        {
            if (lane.Creature != null)
            {
                ApplyStatus(lane.Creature, StatusEnum.Paralyze, base.Val1);
                CancelStatus(lane.Creature, StatusEnum.Shield);
            }
        }

        return true;
    }
}