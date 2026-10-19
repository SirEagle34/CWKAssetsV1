public class GrantLandSwampVampiricEnhanceSTROnDie : OnDied
{
	public override bool OnEnable()
	{
		bool result = false;
		foreach (LaneState lane in base.Owner.Owner.Lanes)
		{
			if (lane.Creature != null)
			{
				ApplyStatus(lane.Creature, StatusEnum.LandSwamp, 1f);
				ApplyStatus(lane.Creature, StatusEnum.Vampiric, base.Val1);
				ApplyStatus(lane.Creature, StatusEnum.EnhanceSTR, base.Val2);
				result = true;
			}
		}
		return result;
	}
}
