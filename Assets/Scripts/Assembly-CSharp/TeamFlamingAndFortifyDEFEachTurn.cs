using UnityEngine;
public class TeamFlamingAndFortifyDEFEachTurn : OnStartTurn
{
	public override bool OnEnable()
	{
		bool result = false;
		foreach (LaneState lane in base.Owner.Owner.Lanes)
		{
			if (lane.Creature != null)
			{
				ApplyStatus(lane.Creature, StatusEnum.Flaming, base.Val1);
				ApplyStatus(lane.Creature, StatusEnum.FortifyDEF, base.Val1);
				result = true;
			}
		}
		return result;
	}
}
