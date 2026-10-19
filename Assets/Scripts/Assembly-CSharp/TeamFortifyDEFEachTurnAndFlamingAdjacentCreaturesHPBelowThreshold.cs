using UnityEngine;
using System.Collections.Generic;
public class TeamFortifyDEFEachTurnAndFlamingAdjacentCreaturesHPBelowThreshold : OnStartTurn
{
	private HashSet<CreatureState> triggered = new HashSet<CreatureState>();
    public override bool ProcessMessage(GameMessage Message)
    {
        if (Message.Action != GameEvent.START_TURN || Message.WhichPlayer != base.Owner.Owner)
            return false;

        bool result = false;

        foreach (LaneState lane in base.Owner.Owner.Lanes)
        {
            if (lane.Creature == null)
                continue;

            // 🟢 Herkese Fortify
            ApplyStatus(lane.Creature, StatusEnum.FortifyDEF, base.Val3);
            result = true;

            // 🔴 HP düşükse komşulara Flaming
            if (lane.Creature != null && lane.Creature.HPPct < base.Val1Pct)
			{
				if (!triggered.Contains(lane.Creature))
				{
					foreach (LaneState adjacentLane in lane.AdjacentLanes)
					{
						if (adjacentLane.Creature != null)
						{
							ApplyStatus(adjacentLane.Creature, StatusEnum.Flaming, base.Val2);
						}
					}

					triggered.Add(lane.Creature); // 🔥 sadece 1 kere
				}
			}
        }

        return result;
    }

    public override bool OnEnable()
    {
        return true;
    }
}