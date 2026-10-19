using UnityEngine;

public class TeamRegenAdrenalineOnLandSwamp : OnLandSwamp
{
	public override bool OnEnable()
	{
		foreach (LaneState lane in base.Owner.Owner.Lanes)
		{
			if (lane.Creature != null)
			{
				ApplyStatus(lane.Creature, StatusEnum.Adrenaline, base.Val1);
				ApplyStatus(lane.Creature, StatusEnum.Regen, base.Val2);
			}
		}
		return true;
	}
}
