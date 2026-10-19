using UnityEngine;

public class AreaFrostArmorAboveHPOnTurnStart : OnStartTurn
{
	public override bool OnEnable()
	{
		bool result = false;
		// Check if adjacent creatures meet HP threshold
		bool shouldGrant = false;
		foreach (LaneState adjacentLane in base.Owner.Lane.AdjacentLanes)
		{
			if (adjacentLane.Creature != null && adjacentLane.Creature.HPPct > base.Val2Pct)
			{
				shouldGrant = true;
				break;
			}
		}
		
		
		if (shouldGrant)
		{
			foreach (LaneState thisAndAdjacentLane in base.Owner.Lane.ThisAndAdjacentLanes)
			{
				if (thisAndAdjacentLane.Creature != null)
				{
					ApplyStatus(thisAndAdjacentLane.Creature, StatusEnum.Chilled, base.Val1);
					result = true;
				}
			}
		}
		return result;
	}
}