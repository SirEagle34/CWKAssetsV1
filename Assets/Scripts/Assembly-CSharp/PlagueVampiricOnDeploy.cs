public class PlagueVampiricOnDeploy : OnDeploy
{
	public override bool OnEnable()
	{
		bool result = false;
		foreach (LaneState lane in base.Owner.Owner.Opponent.Lanes)
		{
			if (lane.Creature != null)
			{
				ApplyStatus(lane.Creature, StatusEnum.Plague, base.Val1);
				ApplyStatus(base.Owner, StatusEnum.Vampiric, base.Val2);
				result = true;
			}
		}
		return result;
	}
}
