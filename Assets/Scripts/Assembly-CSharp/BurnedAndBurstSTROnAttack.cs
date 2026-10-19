public class BurnedAndBurstSTROnAttack : OnAttack
{
	public override bool OnEnable()
	{
		{
			ApplyStatus(Target, StatusEnum.Burned, base.Val1);
			foreach (LaneState thisAndAdjacentLane in base.Owner.Lane.ThisAndAdjacentLanes)
			{
				if (thisAndAdjacentLane.Creature != null)
				{
					ApplyStatus(thisAndAdjacentLane.Creature, StatusEnum.BurstSTR, base.Val2);
				}
			}
			return true;
		}
		return false;
	}
}
