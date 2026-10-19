public class RemoveDebuffGrantBurstSTRstrengthCounterIfOppHasMoreCards : OnStartTurn
{
	public override bool OnEnable()
	{
		if (base.Owner.Owner.Opponent.Hand.Count > base.Owner.Owner.Hand.Count)
		{
			ApplyStatus(base.Owner, StatusEnum.BurstSTR, base.Val1);
			ApplyStatus(base.Owner, StatusEnum.StrengthCounter, base.Val2);
			ApplyStatus(base.Owner, StatusEnum.RemoveStatus, base.Val3, StatusRemovalData.RemoveRandomDebuffs());
			return true;
		}
		return false;
	}
}
