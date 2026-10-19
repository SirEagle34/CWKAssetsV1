public class EnemyCrippleAndParisiticArmorOnLandSwamp : OnLandSwamp
{
	public override bool OnEnable()
	{
		bool result = false;
		foreach (LaneState lane in base.Owner.Owner.Opponent.Lanes)
		{
			if (lane.Creature != null)
			{
				ApplyStatus(lane.Creature, StatusEnum.Cripple, base.Val1);
				ApplyStatus(base.Owner, StatusEnum.ParisiticArmor, base.Val2);
				result = true;
			}
		}
		return result;
	}
}