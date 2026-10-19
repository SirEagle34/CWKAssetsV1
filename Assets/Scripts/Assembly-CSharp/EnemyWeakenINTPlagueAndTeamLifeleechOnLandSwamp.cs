public class EnemyWeakenINTPlagueAndTeamLifeleechOnLandSwamp : OnLandSwamp
{
	public override bool OnEnable()
	{
		bool result = false;
		foreach (LaneState lane in base.Owner.Owner.Opponent.Lanes)
		{
			if (lane.Creature != null)
			{
				ApplyStatus(lane.Creature, StatusEnum.WeakenINT, base.Val1);
				ApplyStatus(lane.Creature, StatusEnum.Plague, base.Val2);
				ApplyStatus(base.Owner, StatusEnum.Vampiric, base.Val3);
				result = true;
			}
		}
		return result;
	}
}