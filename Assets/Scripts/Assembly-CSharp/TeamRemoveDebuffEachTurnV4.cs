public class TeamRemoveDebuffEachTurnV4 : OnStartTurn
{
    public override bool OnEnable()
    {
        foreach (LaneState lane in base.Owner.Owner.Lanes)
        {
            if (lane.Creature == null) continue;

            // Find both BEFORE making any change
            StatusState nonPlague = lane.Creature.StatusEffects.Find(
                s => s.Target != null && s.Data.StatusType == StatusType.Debuff && s.Data.ID != "Plague"
            );
           // remove 1 non-Plague debuff
            if (nonPlague != null)
            {
                lane.Creature.CancelStatusEffect(nonPlague.Data);
            }

             // Reduce Plague intensity by 15%
            for (int i = 0; i < lane.Creature.StatusEffects.Count; i++)
            {
                StatusState s = lane.Creature.StatusEffects[i];
                if (s is Plague && s.Target != null)
                {
                    float newIntensity = s.Intensity - 0.15f;
                    lane.Creature.CancelStatusEffect("Plague");
                    if (newIntensity > 0.01f)
                    {
                        lane.Creature.ApplyStatus(
                            StatusDataManager.Instance.GetData("Plague"),
                            newIntensity * 100f,
                            null
                        );
                    }
                    break;
                }
            }
        }
        return true;
    }
}