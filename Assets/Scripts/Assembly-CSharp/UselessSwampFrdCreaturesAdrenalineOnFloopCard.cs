public class UselessSwampFrdCreaturesAdrenalineOnFloopCard : OnPlayedCardOnAnyCreature
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
					ApplyStatus(lane.Creature, StatusEnum.LandSwamp, 1f);
					ApplyStatus(lane.Creature, StatusEnum.Adrenaline, base.Val1);
					result = true;
				}
			}
			return result;
		}
		return result;
	}
}
