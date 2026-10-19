using UnityEngine;

public class RemoveNegEffToRandomTeamOnTurnStartFix : OnStartTurn
{
    public override bool OnEnable()
	{
		bool result = false;

		LaneState[] array = new LaneState[base.Owner.Owner.Lanes.Count];
		int num = 0;

		foreach (LaneState lane in base.Owner.Owner.Lanes)
		{
			if (lane.Creature != null && lane.Creature.HasDebuff)
			{
				array[num] = lane;
				num++;
			}
		}

		if (num > 0)
		{
			int index = Random.Range(0, num);

			ApplyStatus(
				array[index].Creature,
				StatusEnum.RemoveStatus,
				base.Val1,
				StatusRemovalData.RemoveRandomDebuffs()
			);

			result = true;
		}

		return result;
	}
}