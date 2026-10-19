public class ChillLowestEnemyOn3Cards : OnCardThreshold
{
	public override bool OnEnable()
	{
		CreatureState creatureState = null;

		foreach (LaneState lane in base.Owner.Owner.Opponent.Lanes)
		{
			if (lane.Creature != null && !lane.Creature.IsFrozen)
			{
				if (creatureState == null || lane.Creature.HP < creatureState.HP)
				{
					creatureState = lane.Creature;
				}
			}
		}

		if (creatureState != null)
		{
			ApplyStatus(creatureState, StatusEnum.Frozen, base.Val2);
			return true;
		}

		return false;
	}
}