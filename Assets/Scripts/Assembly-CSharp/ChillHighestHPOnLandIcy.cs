public class ChillHighestHPOnLandIcy : OnLandIcy
{
	public override bool OnEnable()
	{
		CreatureState creatureState = null;

		foreach (LaneState lane in base.Owner.Owner.Lanes)
		{
			if (lane.Creature != null &&
			    (creatureState == null || lane.Creature.HP > creatureState.HP))
			{
				creatureState = lane.Creature;
			}
		}

		if (creatureState != null)
		{
			ApplyStatus(creatureState, StatusEnum.Frozen, 2);
			return true;
		}

		return false;
	}
}
