public class GrantLandSandHealThornsFortifyDEFOnDie : OnDied
{
	public override bool OnEnable()
	{
		bool result = false;
		foreach (LaneState lane in base.Owner.Owner.Lanes)
		{
			if (lane.Creature != null)
			{
				ApplyStatus(lane.Creature, StatusEnum.LandSand, 1f);
				ApplyStatus(lane.Creature, StatusEnum.FortifyDEF, base.Val2);
				ApplyStatus(lane.Creature, StatusEnum.Thorns, base.Val3);
				lane.Creature.Heal(base.Val1Pct);
				result = true;
			}
		}
		return result;
	}
}
