using UnityEngine;

public class RemoveDebuffMagicCounterOnLandSpooky : OnLandSpooky
{
    public override bool OnEnable()
    {
		foreach (LaneState thisAndAdjacentLane in base.Owner.Lane.ThisAndAdjacentLanes)
        {
            if (thisAndAdjacentLane.Creature != null)
			{
				ApplyStatus(
					thisAndAdjacentLane.Creature,
					StatusEnum.RemoveStatus,
					base.Val1,
					StatusRemovalData.RemoveRandomDebuffs()
				);

				ApplyStatus(
					thisAndAdjacentLane.Creature,
					StatusEnum.MagicCounter,
					base.Val2
				);
			}
		}
		return true;

	}
}