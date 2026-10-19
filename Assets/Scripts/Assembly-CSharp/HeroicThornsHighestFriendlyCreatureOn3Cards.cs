public class HeroicThornsHighestFriendlyCreatureOn3Cards : OnCardThreshold
{
	public override bool OnEnable()
	{
		CreatureState highestHpCreature = null;

		foreach (LaneState lane in base.Owner.Owner.Lanes)
		{
			if (lane.Creature == null)
				continue;

			if (highestHpCreature == null || lane.Creature.HP > highestHpCreature.HP)
			{
				highestHpCreature = lane.Creature;
			}
		}

		if (highestHpCreature == null)
			return false;

		ApplyStatus(
			highestHpCreature,
			StatusEnum.Bravery,
			base.Val2,   
			null
		);

		ApplyStatus(
			highestHpCreature,
			StatusEnum.Thorns,
			base.Val3,   
			null
		);

		return true;
	}
}
