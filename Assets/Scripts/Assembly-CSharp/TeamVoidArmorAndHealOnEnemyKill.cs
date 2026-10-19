public class TeamVoidArmorAndHealOnEnemyKill : OnEnemyDied
{
	public override bool OnEnable()
	{
       	bool result = false; 
		foreach (LaneState lane in base.Owner.Owner.Lanes)
		{
			if (lane.Creature != null)
			{
				lane.Creature.Heal(base.Val1Pct);				
				ApplyStatus(lane.Creature, StatusEnum.VoidArmor, base.Val2);
				result = true;
			}
		}
		return result;
	}
}
