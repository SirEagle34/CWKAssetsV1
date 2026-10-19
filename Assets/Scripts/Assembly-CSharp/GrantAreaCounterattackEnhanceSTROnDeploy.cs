public class GrantAreaCounterattackEnhanceSTROnDeploy : OnDeploy
{
	public override bool OnEnable()
	{
		bool result = false;
		foreach (LaneState thisAndAdjacentLane in base.Owner.Lane.ThisAndAdjacentLanes)
		{
			if (thisAndAdjacentLane.Creature != null)
			{
				ApplyStatus(thisAndAdjacentLane.Creature, StatusEnum.StrengthCounter, base.Val1);
				ApplyStatus(thisAndAdjacentLane.Creature, StatusEnum.EnhanceSTR, base.Val2);
				result = true;
			}
		}
		return result;
	}
}
