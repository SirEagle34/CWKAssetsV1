public class AreaGrantMagicCounterVoidArmorFloopCardDraw : OnCardDraw
{
	public override bool OnEnable()
	{
		if (!CardDrawn.IsLeaderCard)
		{
			return false;
		}
		bool result = false;
		foreach (LaneState thisAndAdjacentLane in base.Owner.Lane.ThisAndAdjacentLanes)
		{
			if (thisAndAdjacentLane.Creature != null)
			{
				ApplyStatus(thisAndAdjacentLane.Creature, StatusEnum.MagicCounterattack, base.Val1);
				ApplyStatus(thisAndAdjacentLane.Creature, StatusEnum.VoidArmor, base.Val2);
				result = true;
			}
		}
		return result;
	}
}
