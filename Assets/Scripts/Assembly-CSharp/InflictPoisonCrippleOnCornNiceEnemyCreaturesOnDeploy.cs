public class InflictPoisonCrippleOnCornNiceEnemyCreaturesOnDeploy : OnDeploy
{
	public override bool OnEnable()
	{
		bool result = false;
		foreach (LaneState lane in base.Owner.Owner.Opponent.Lanes)
		{
			if (lane.Creature != null && (lane.Creature.Data.Faction == CreatureFaction.Red || lane.Creature.Data.Faction == CreatureFaction.Light))
			{
				ApplyStatus(lane.Creature, StatusEnum.Poison, base.Val1);
				ApplyStatus(lane.Creature, StatusEnum.Cripple, base.Val2);
				result = true;
			}
		}
		return result;
	}
}
