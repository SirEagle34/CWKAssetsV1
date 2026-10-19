public class AreaDerangedToDeployed : OnDeployed
{
	public override bool OnEnable()
	{
		bool result = false;
		foreach (LaneState thisAndAdjacentLane in DeployedCreature.Lane.ThisAndAdjacentLanes)
		{
			if (thisAndAdjacentLane.Creature != null)
			{
				ApplyStatus(thisAndAdjacentLane.Creature, StatusEnum.VoidArmor, base.Val1);
				result = true;
			}
		}
		return result;
	}
}
