using UnityEngine;

public class SpreadPositiveBuffToAdjacent : OnGainBuff
{
	public override bool OnEnable()
	{
		if (Card != null && Card.IsLeaderCard)
			return false;

		if (Status.ID != "Regen" &&
			Status.ID != "APFlower" &&
			Status.ID != "Thorns" &&
			Status.ID != "Pinpoint" &&
			Status.ID != "FortifyDEF")
		{
			return false;
		}

		if (Status.ID == "MindlessIFBuff")
			return false;


		bool result = false;

		// Uni Wizard maximum spread duration = 2 turns
		float spreadAmount = Mathf.Min(Amount, 2);


		int laneIndex = base.Owner.Lane.Index;


		// Left
		if (laneIndex > 0)
		{
			LaneState lane = base.Owner.Owner.Lanes[laneIndex - 1];

			if (lane.Creature != null)
			{
				lane.Creature.ApplyStatus(Status, spreadAmount, null, this);
				result = true;
			}
		}


		// Right
		if (laneIndex < base.Owner.Owner.Lanes.Count - 1)
		{
			LaneState lane = base.Owner.Owner.Lanes[laneIndex + 1];

			if (lane.Creature != null)
			{
				lane.Creature.ApplyStatus(Status, spreadAmount, null, this);
				result = true;
			}
		}


		if (result)
		{
			ApplyStatus(base.Owner, StatusEnum.MindlessIFBuff, base.Val1);
		}


		return result;
	}
}