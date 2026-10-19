public class ImmunityDerangedHealOnEnemyDied : OnEnemyDied
{
	public override bool OnEnable()
	{
		bool result = false;
		foreach (LaneState thisAndAdjacentLane in base.Owner.Lane.ThisAndAdjacentLanes)
		{
			if (thisAndAdjacentLane.Creature != null)
			{
				ApplyStatus(thisAndAdjacentLane.Creature, StatusEnum.Immunity, base.Val1);
				ApplyStatus(thisAndAdjacentLane.Creature, StatusEnum.VoidArmor, base.Val2);
				thisAndAdjacentLane.Creature.Heal(base.Val3Pct);
				result = true;
			}
		}
		return result;
	}
}