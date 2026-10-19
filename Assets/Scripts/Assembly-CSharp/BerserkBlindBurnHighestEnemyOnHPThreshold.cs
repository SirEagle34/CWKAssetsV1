using System.Collections.Generic;

public class BerserkBlindBurnHighestEnemyOnHPThreshold : OnEnemyDamaged
{
    private HashSet<CreatureState> _triggeredCreatures = new HashSet<CreatureState>();

    public override bool OnEnable()
    {
        return TryActivate();
    }

    private bool TryActivate()
    {
        if (_triggeredCreatures.Contains(Attacked))
            return false;

        float targetHpPct = (float)Attacked.HP / (float)Attacked.MaxHP * 100f;
        if (targetHpPct >= base.Val1Pct)
            return false;

        CreatureState highestHpCreature = null;

        foreach (LaneState lane in base.Owner.Owner.Opponent.Lanes)
        {
            if (lane.Creature == null)
                continue;

            if (highestHpCreature == null || lane.Creature.HP > highestHpCreature.HP)
                highestHpCreature = lane.Creature;
        }

        if (highestHpCreature != null)
        {
            ApplyStatus(highestHpCreature, StatusEnum.Berserk, 1f);
            ApplyStatus(highestHpCreature, StatusEnum.Blind, 1f);
            ApplyStatus(highestHpCreature, StatusEnum.Burn, base.Val2);
            _triggeredCreatures.Add(Attacked);
            return true;
        }

        return false;
    }
}