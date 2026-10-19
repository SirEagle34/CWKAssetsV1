public class AreaAttackSpikeAndHealBraveryOnAttack : OnAttack
{
	public override bool OnEnable()
	{
		bool result = false;
		foreach (LaneState thisAndAdjacentLane in base.Owner.Lane.ThisAndAdjacentLanes)
		{
			if (thisAndAdjacentLane.Creature != null)
			{
				ApplyStatus(thisAndAdjacentLane.Creature, StatusEnum.AttackSpike, base.Val1);
				thisAndAdjacentLane.Creature.Heal(base.Val2Pct);
				ApplyStatus(base.Owner, StatusEnum.Bravery, base.Val3);
				result = true;
			}
		}
		return result;
	}
}
