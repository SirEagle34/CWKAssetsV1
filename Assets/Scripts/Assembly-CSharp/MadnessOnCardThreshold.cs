using UnityEngine;

public class MadnessOnCardThreshold : OnCardThreshold
{
	public override bool OnEnable()
	{
		bool result = false;
		foreach (LaneState lane in base.Owner.Owner.Lanes)
		{
			if (lane.Creature != null && (lane.Creature.Data.Faction == CreatureFaction.Red || lane.Creature.Data.Faction == CreatureFaction.Green || lane.Creature.Data.Faction == CreatureFaction.Orange))
			{
				ApplyStatus(lane.Creature, StatusEnum.Madness, base.Val2);
				result = true;
			}
		}
		return result;
	}
}
