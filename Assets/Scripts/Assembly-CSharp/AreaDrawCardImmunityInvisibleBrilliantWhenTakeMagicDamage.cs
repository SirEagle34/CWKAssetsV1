using UnityEngine;
public class AreaDrawCardImmunityInvisibleBrilliantWhenTakeMagicDamage : OnFriendlyDamaged
{
	public override bool OnEnable()
	{
		if (!Magic || Physical)
			return false;

		bool result = false;

		foreach (LaneState lane in base.Owner.Lane.ThisAndAdjacentLanes)
		{
			if (lane.Creature == null)
				continue;

			if (!lane.Creature.AtFullHealth)
				continue;

			for (int i = 0; i < base.Val1; i++)
			{
				lane.Creature.DrawCard();
			}

			ApplyStatus(lane.Creature, StatusEnum.Immunity, base.Val2);
			ApplyStatus(lane.Creature, StatusEnum.Invisible, base.Val3);
			ApplyStatus(lane.Creature, StatusEnum.Brilliant, base.Val4);
			result = true;
		}

		return result;
	}
}