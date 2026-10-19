public class TeamFrostbiteOnAllyDeath : OnFriendlyDamaged
{
	public override bool OnEnable()
	{
		if (Attacked == null || Attacked.HP > 0)
			return false;

		foreach (LaneState lane in base.Owner.Owner.Opponent.Lanes)
		{
			if (lane.Creature != null)
			{
				ApplyStatus(lane.Creature, StatusEnum.Frostbite, base.Val1);
			}
		}

		return true;
	}
}