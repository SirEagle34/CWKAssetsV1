using System.Collections.Generic;
using UnityEngine;

public class RemoveNegRandomOnStartTurns : OnStartTurn
{
    public override bool OnEnable()
    {
        List<CreatureState> targets = new List<CreatureState>();

        foreach (LaneState lane in base.Owner.Owner.Lanes)
        {
            CreatureState creature = lane.Creature;

            if (creature == null)
                continue;

            if (creature.Owner != base.Owner.Owner)
                continue;

            if (creature.HasDebuff)
                targets.Add(creature);
        }

        if (targets.Count == 0)
            return false;

        Shuffle(targets);

        CreatureState target = targets[0];

        ApplyStatus(
            target,
            StatusEnum.RemoveStatus,
            3,
            StatusRemovalData.RemoveRandomDebuffs()
        );

        return true;
    }

    private void Shuffle(List<CreatureState> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = KFFRandom.RandomIndex(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}