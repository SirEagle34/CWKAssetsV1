public class RemoveBuffHighestEnemyOn3Cards : OnCardThreshold
{
	public override bool OnEnable()
	{
		CreatureState creatureState = null;

		foreach (LaneState lane in base.Owner.Owner.Opponent.Lanes)
		{
			if (lane.Creature != null && lane.Creature.HasBuff)
			{
				if (creatureState == null || lane.Creature.HP > creatureState.HP)
				{
					creatureState = lane.Creature;
				}
			}
		}

		if (creatureState != null)
		{
			ApplyStatus(
				creatureState,
				StatusEnum.RemoveStatus,
				base.Val2,
				StatusRemovalData.RemoveRandomBuffs()
			);
			return true;
		}

		return false;
	}
}
