using UnityEngine;

public class GrantJokerArmorFortifyDEFNiceSandRainbowOnFloopCardDraw : OnCardDraw
{
	public override bool OnEnable()
	{
		if (!CardDrawn.IsLeaderCard)
		{
			return false;
		}
		bool result = false;
		foreach (LaneState lane in base.Owner.Owner.Lanes)
		{
			if (lane.Creature != null && (lane.Creature.Data.Faction == CreatureFaction.Green || lane.Creature.Data.Faction == CreatureFaction.Light || lane.Creature.Data.Faction == CreatureFaction.Colorless))
			{
				ApplyStatus(lane.Creature, StatusEnum.Joker, base.Val1);
				ApplyStatus(lane.Creature, StatusEnum.FortifyDEF, base.Val2);
				result = true;
			}
		}
		return result;
	}
}
