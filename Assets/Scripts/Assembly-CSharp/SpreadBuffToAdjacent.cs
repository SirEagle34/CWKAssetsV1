public class SpreadBuffToAdjacent : OnGainBuff
{
	public override bool OnEnable()
	{
		if (Card != null && Card.IsLeaderCard)
			return false;

		if (Status.ID == "Invisible")
			return false;

		if (Status.ID == "APSiphon")
			return false;

		if (Status.ID == "Shield")
			return false;

		if (Status.ID == "MindlessIFBuff")
			return false;

		if (Card != null &&
			(Card.TargetGroup == AttackRange.Double ||
			Card.TargetGroup == AttackRange.Triple ||
			Card.TargetGroup == AttackRange.All))
		{
			return false;
		}

	bool result = false;

    int laneIndex = base.Owner.Lane.Index;

    if (laneIndex > 0)
    {
        LaneState lane = base.Owner.Owner.Lanes[laneIndex - 1];

        if (lane.Creature != null)
        {
            lane.Creature.ApplyStatus(Status, Amount, null, this);
            result = true;
        }
    }

    if (laneIndex < base.Owner.Owner.Lanes.Count - 1)
    {
        LaneState lane = base.Owner.Owner.Lanes[laneIndex + 1];

        if (lane.Creature != null)
        {
            lane.Creature.ApplyStatus(Status, Amount, null, this);
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