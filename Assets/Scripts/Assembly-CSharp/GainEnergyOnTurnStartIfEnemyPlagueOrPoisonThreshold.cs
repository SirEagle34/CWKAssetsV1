public class GainEnergyOnTurnStartIfEnemyPlagueOrPoisonThreshold : OnStartTurn
{
    public override bool OnEnable()
    {
        bool flag = false;
        foreach (LaneState lane in base.Owner.Owner.Opponent.Lanes)
        {
            if (lane.Creature == null) continue;

            StatusState plague = lane.Creature.StatusEffects.Find(s => s is Plague);
            bool hasValidPlague = plague != null && plague.Intensity >= base.Val2Pct;
            bool hasPoisoned = lane.Creature.IsPoisoned;

            if (!hasValidPlague && !hasPoisoned)
            {
                return false;
            }
            flag = true;
        }
        if (flag)
        {
            base.Owner.Owner.AddActionPoints(base.Val1);
            return true;
        }
        return false;
    }
}
