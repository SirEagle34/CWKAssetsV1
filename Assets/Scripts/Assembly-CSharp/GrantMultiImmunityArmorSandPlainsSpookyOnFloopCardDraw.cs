public class GrantMultiImmunityArmorSandPlainsSpookyOnFloopCardDraw : OnCardDraw
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
			if (lane.Creature != null && (lane.Creature.Data.Faction == CreatureFaction.Green || lane.Creature.Data.Faction == CreatureFaction.Blue || lane.Creature.Data.Faction == CreatureFaction.Purple))
			{
				ApplyStatus(lane.Creature, StatusEnum.MultiImmunity, base.Val1);
				result = true;
			}
		}
		return result;
	}
}
