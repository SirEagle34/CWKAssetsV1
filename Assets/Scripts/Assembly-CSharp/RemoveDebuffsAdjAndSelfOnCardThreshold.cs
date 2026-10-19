public class RemoveDebuffsAdjAndSelfOnCardThreshold : AbilityState
{
    public override bool ProcessMessage(GameMessage Message)
    {
        // Reset each turn
        if (Message.Action == GameEvent.START_TURN && Message.WhichPlayer == base.Owner.Owner)
        {
            IsActive = false;
            StoredValue = 0;
            return false;
        }

        // Card played ON this creature
        if (Message.Action == GameEvent.CARD_PLAYED && Message.Creature == base.Owner)
        {
            // Remove 2 debuffs from each adjacent creature (unlimited times per turn)
            foreach (LaneState lane in base.Owner.Lane.AdjacentLanes)
            {
                if (lane.Creature == null) continue;
                for (int i = 0; i < 2; i++)
                {
                    StatusState debuff = lane.Creature.StatusEffects.Find(
                        s => s.Target != null && s.Data.StatusType == StatusType.Debuff
                    );
                    if (debuff != null)
                    {
                        lane.Creature.CancelStatusEffect(debuff.Data);
                    }
                }
            }

            // Track cards played on self this turn, remove 1 debuff from self once at threshold 2
            if (!IsActive)
            {
                StoredValue++;
                if (StoredValue >= 2)
                {
                    StatusState selfDebuff = base.Owner.StatusEffects.Find(
                        s => s.Target != null && s.Data.StatusType == StatusType.Debuff
                    );
                    if (selfDebuff != null)
                    {
                        base.Owner.CancelStatusEffect(selfDebuff.Data);
                    }
                    IsActive = true;
                }
            }

            return true;
        }

        return false;
    }
}
