public class RegenEnergyOnAttack : OnAttack
{
	public override bool OnEnable()
	{
		bool result = false;
		foreach (LaneState thisAndAdjacentLane in base.Owner.Lane.ThisAndAdjacentLanes)
		{
			if (thisAndAdjacentLane.Creature != null)
			{
				base.Owner.Owner.AddActionPoints(base.Val1);
				ApplyStatus(thisAndAdjacentLane.Creature, StatusEnum.Regen, base.Val2);
				result = true;
			}
		}
		return result;
	}
}
