public class BraveryAndBurstSTRForHighestHPOnDeploy : OnDeploy
{
	public override bool OnEnable()
	{
		CreatureState creatureState = null;
		foreach (LaneState lane in base.Owner.Owner.Lanes)
		{
			if (lane.Creature != null && (creatureState == null || lane.Creature.HP > creatureState.HP))
			{
				creatureState = lane.Creature;
			}
		}
		if (creatureState != null)
		{
			ApplyStatus(creatureState, StatusEnum.Bravery, base.Val1);
			ApplyStatus(creatureState, StatusEnum.BurstSTR, base.Val2);
			return true;
		}
		return false;
	}
}
