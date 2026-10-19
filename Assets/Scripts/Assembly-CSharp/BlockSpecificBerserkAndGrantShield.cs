using UnityEngine;

public class BlockSpecificBerserkAndGrantShield : AbilityState
{
    public override bool OnEnable()
    {
        return false;
    }

    public override bool ProcessMessage(GameMessage Message)
    {
        // ==========================================
        // DEPLOY BUFF
        // ==========================================
		if (Message.Action == GameEvent.CREATURE_DEPLOYED &&
			Message.Creature != base.Owner &&
			Message.WhichPlayer == base.Owner.Owner)
        {
            ApplyStatus(Message.Creature, StatusEnum.Astral, base.Val2);
            ApplyStatus(Message.Creature, StatusEnum.Adrenaline, base.Val3);
            Debug.Log($"[Berserk Block] {Message.Creature} çağrıldı, Astral + Adrenaline verildi.");

            return true;
        }

        // ==========================================
        // END TURN BERSERK CLEANUP
        // ==========================================
        if (Message.Action == GameEvent.END_TURN &&
            Message.WhichPlayer == base.Owner.Owner.Opponent)
        {
            bool foundBerserk = false;

            foreach (LaneState lane in base.Owner.Owner.Lanes)
            {
                if (lane?.Creature == null)
                    continue;

                if (lane.Creature.IsBerserk)
                {
                    foundBerserk = true;

                    CancelStatus(lane.Creature, StatusEnum.Berserk);
                    ApplyStatus(lane.Creature, StatusEnum.Shield, base.Val1);                }
            }

            if (!foundBerserk)
                return false;

            Debug.Log("[Berserk Block] END TURN -> Berserk temizlendi + Shield verildi");

            return true;
        }

        return false;
    }
}