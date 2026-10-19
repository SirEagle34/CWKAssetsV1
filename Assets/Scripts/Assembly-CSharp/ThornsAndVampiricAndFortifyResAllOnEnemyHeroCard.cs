public class ThornsAndVampiricAndFortifyResAllOnEnemyHeroCard : OnOpponentHeroCardDraw
{
	public override bool OnEnable()
	{
		bool result = false;
		foreach (LaneState lane in base.Owner.Owner.Lanes)
		{
			if (lane.Creature != null)
			{
				ApplyStatus(lane.Creature, StatusEnum.Thorns, base.Val1);
				ApplyStatus(lane.Creature, StatusEnum.Vampiric, base.Val2);
				ApplyStatus(lane.Creature, StatusEnum.FortifyRES, base.Val3);
				result = true;
			}
		}
		return result;
	}
}
