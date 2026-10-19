public class BerserkBlindOppHighestHPOnDeploy : OnDeploy
{
	public override bool OnEnable()
	{
		CreatureState creatureState = null;
		foreach (LaneState lane in base.Owner.Owner.Opponent.Lanes)
		{
			if (lane.Creature != null && (creatureState == null || lane.Creature.HP > creatureState.HP))
			{
				creatureState = lane.Creature;
			}
		}
		if (creatureState != null)
		{
			ApplyStatus(creatureState, StatusEnum.Berserk, 1f);
			ApplyStatus(creatureState, StatusEnum.Blind, base.Val1);
			return true;
		}
		return false;
	}
}
