public class MagmaLandsFrdCreaturesEnhanceSTROnFloopCard : OnPlayedCardOnAnyCreature
{
	public override bool OnEnable()
	{
		bool result = false;
		if (CardPlayed.IsLeaderCard)
		{
			foreach (LaneState lane in base.Owner.Owner.Lanes)
			{
				if (lane.Creature != null)
				{
					ApplyStatus(lane.Creature, StatusEnum.LandMagma, 1f);
					ApplyStatus(lane.Creature, StatusEnum.EnhanceSTR, base.Val1);
					result = true;
				}
			}
			return result;
		}
		return result;
	}
}
