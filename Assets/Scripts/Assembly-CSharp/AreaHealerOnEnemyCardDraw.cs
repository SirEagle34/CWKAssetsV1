public class AreaHealerOnEnemyCardDraw : OnOpponentCardDraw
{
	public override bool OnEnable()
	{
		bool result = false;
		foreach (LaneState thisAndAdjacentLane in base.Owner.Lane.ThisAndAdjacentLanes)
		{
			if (thisAndAdjacentLane.Creature != null && !thisAndAdjacentLane.Creature.AtFullHealth)
			{
				ApplyStatus(thisAndAdjacentLane.Creature, StatusEnum.Healer, base.Val1);
				result = true;
			}
		}
		return result;
	}
}
