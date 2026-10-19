public class DealerLowestOnAttack : OnAttack
{
	public override bool OnEnable()
	{
		CreatureState creatureState = null;
		foreach (LaneState lane in base.Owner.Owner.Lanes)
		{
			if (lane.Creature != null && lane.Creature.Damage > 0 && (creatureState == null || lane.Creature.HP < creatureState.HP))
			{
				creatureState = lane.Creature;
			}
		}
		if (creatureState != null)
		{
			 ApplyStatus(creatureState, StatusEnum.Dealer, base.Val2);
			return true;
		}
		return false;
	}
}
