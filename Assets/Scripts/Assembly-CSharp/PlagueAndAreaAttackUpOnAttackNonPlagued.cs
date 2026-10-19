public class PlagueAndAreaAttackUpOnAttackNonPlagued : OnAttack
{
	public override bool OnEnable()
	{
		if (Target != null && !Target.IsPlagued)
		{
			ApplyStatus(Target, StatusEnum.Plague, base.Val1);
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
