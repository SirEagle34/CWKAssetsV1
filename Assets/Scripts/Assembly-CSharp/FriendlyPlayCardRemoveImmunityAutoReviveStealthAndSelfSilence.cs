using UnityEngine;

public class FriendlyPlayCardRemoveImmunityAutoReviveStealthAndSelfSilence : AbilityState
{
    public override bool ProcessMessage(GameMessage Message)
    {
        // Dost player üzerinde kart oynandığında
        if (Message.Action == GameEvent.CARD_PLAYED &&
            Message.WhichPlayer == base.Owner.Owner)
        {
            // Tüm düşman yaratıkların Immunity, AutoRevive ve Stealth durumlarını kaldır
            foreach (LaneState lane in base.Owner.Owner.Opponent.Lanes)
            {
                if (lane.Creature == null)
                    continue;

                CancelStatus(lane.Creature, StatusEnum.Immunity);
                CancelStatus(lane.Creature, StatusEnum.AutoRevive);
                CancelStatus(lane.Creature, StatusEnum.Stealth);
            }

            // Kendisine 2 tur Silence ver (Mindless yerine)
            ApplyStatus(base.Owner, StatusEnum.Mindless, base.Val1);

            return true;
        }

        return false;
    }
}