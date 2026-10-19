public class DealerAdrenalineHealAllOnEnemyHeroCard : OnOpponentHeroCardDraw
{
	public override bool OnEnable()
	{
		bool result = false;
		foreach (LaneState lane in base.Owner.Owner.Lanes)
		{
			if (lane.Creature != null)
			{
				ApplyStatus(lane.Creature, StatusEnum.Dealer, base.Val1);
				ApplyStatus(lane.Creature, StatusEnum.Adrenaline, base.Val2);
				lane.Creature.Heal(base.Val3Pct);
				result = true;
			}
		}
		return result;
	}
}
