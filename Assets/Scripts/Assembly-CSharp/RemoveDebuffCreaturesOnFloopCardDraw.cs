public class RemoveDebuffCreaturesOnFloopCardDraw : OnCardDraw
{
	public override bool OnEnable()
	{
		if (!CardDrawn.IsLeaderCard)
		{
			return false;
		}
		foreach (LaneState lane in base.Owner.Owner.Lanes)
        {
            if (lane.Creature != null)
            {
                ApplyStatus(lane.Creature, StatusEnum.RemoveStatus, base.Val1, StatusRemovalData.RemoveRandomDebuffs());
            }
        }
        return true;
    }
}
