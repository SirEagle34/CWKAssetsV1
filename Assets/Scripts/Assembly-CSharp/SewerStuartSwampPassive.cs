using UnityEngine;

public class SewerStuartSwampPassive : OnLandSwamp
{
    private int Count = 0;

    public override bool ProcessMessage(GameMessage Message)
    {
        if (Message.Action == GameEvent.START_TURN)
        {
            if (Count > 0)
                Count--;

            return false;
        }

        if (Message.Action == GameEvent.ENABLE_LANDSWAMP &&
            Message.Creature == base.Owner)
        {
            if (Count > 0)
                return false;

            OnEnable();

            // 2 tur cooldown
            Count = 2;

            return true;
        }

        return false;
    }

    public override bool OnEnable()
    {
        if (base.Owner == null || base.Owner.Owner == null)
            return false;

        // Friendly Team
        foreach (LaneState lane in base.Owner.Owner.Lanes)
        {
            if (lane.Creature != null)
            {
                ApplyStatus(
                    lane.Creature,
                    StatusEnum.ParisiticArmor,
                    2
                );
            }
        }

        // Enemy Team
        // Burada karşı takımın lane'leri kullanılacak.
        foreach (LaneState lane in base.Owner.Owner.Opponent.Lanes)
        {
            if (lane.Creature != null)
            {
                CancelStatus(
                    lane.Creature,
                    StatusEnum.Immunity
                );

                CancelStatus(
                    lane.Creature,
                    StatusEnum.MultiImmunity
                );

                ApplyStatus(
                    lane.Creature,
                    StatusEnum.Paralyze,
                    1
                );
            }
        }

        return true;
    }
}