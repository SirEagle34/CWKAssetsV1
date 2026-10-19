using UnityEngine;

public class DealDamageHighestEnemyOn2Cards : OnCardThreshold
{
    public override bool OnEnable()
    {
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
            ApplyStatus(
                highestHpCreature,
                StatusEnum.DirectDamage,
                base.Val2
            );

            return true;
        }

        return false;
    }
}