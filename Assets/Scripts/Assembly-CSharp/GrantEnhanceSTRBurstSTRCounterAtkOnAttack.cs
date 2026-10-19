public class GrantEnhanceSTRBurstSTRCounterAtkOnAttack : OnAttack
{
	public override bool OnEnable()
	{
		StatusState statusState = base.Owner.StatusEffects.Find((StatusState m) => m is CounterAttack);
		bool result = false;
		if (statusState == null)
		{
			foreach (LaneState thisAndAdjacentLane in base.Owner.Lane.ThisAndAdjacentLanes)
			{
				if (thisAndAdjacentLane.Creature != null)
				{
					ApplyStatus(thisAndAdjacentLane.Creature, StatusEnum.EnhanceSTR, base.Val1);
					ApplyStatus(thisAndAdjacentLane.Creature, StatusEnum.BurstSTR, base.Val2);
					result = true;
				}
			}
			return result;
		}
		return result;
	}
}
