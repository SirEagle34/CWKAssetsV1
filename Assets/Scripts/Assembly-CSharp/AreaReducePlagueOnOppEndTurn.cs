public class AreaReducePlagueOnOppEndTurn : OnEndTurn
{
    public override bool OnEnable()
    {
        return false;
    }

    public override bool ProcessMessage(GameMessage Message)
    {
        if (Message.Action != GameEvent.END_TURN || Message.WhichPlayer != base.Owner.Owner.Opponent)
        {
            return false;
        }

        bool result = false;
        foreach (LaneState lane in base.Owner.Lane.ThisAndAdjacentLanes)
        {
            if (lane.Creature == null) continue;

           // Reduce Plague by flat 20 percentage points
            for (int i = 0; i < lane.Creature.StatusEffects.Count; i++)
            {
                StatusState s = lane.Creature.StatusEffects[i];
                if (s is Plague && s.Target != null)
                {
                    float newIntensity = s.Intensity - 0.20f;
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

            // Remove Blind
            CancelStatus(lane.Creature, StatusEnum.Blind);

            // Heal
            lane.Creature.Heal(base.Val1Pct);

            result = true;
        }
        return result;
    }
}