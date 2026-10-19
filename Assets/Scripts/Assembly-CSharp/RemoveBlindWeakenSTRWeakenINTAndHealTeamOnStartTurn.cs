public class RemoveBlindWeakenSTRWeakenINTAndHealTeamOnStartTurn : OnStartTurn
{
	public override bool OnEnable()
	{
		bool result = false;

		// =========================
		// OWN TEAM: REMOVE DEBUFFS + HEAL
		// =========================
		foreach (LaneState lane in base.Owner.Owner.Lanes)
		{
			if (lane.Creature == null)
				continue;

			var creature = lane.Creature;

			// DEBUFF CLEANSE
			if (creature.IsBlind)
			{
				CancelStatus(creature, StatusEnum.Blind);
				result = true;
			}

			if (creature.IsWeakenSTR)
			{
				CancelStatus(creature, StatusEnum.WeakenSTR);
				result = true;
			}

			if (creature.IsWeakenINT)
			{
				CancelStatus(creature, StatusEnum.WeakenINT);
				result = true;
			}

			if (creature.HasCripple)
			{
				CancelStatus(creature, StatusEnum.Cripple);
				result = true;
			}

			// HEAL
			if (!creature.AtFullHealth)
			{
				creature.Heal(base.Val1Pct);
				result = true;
			}
		}

		// =========================
		// ENEMY TEAM: REMOVE BUFFS
		// =========================
		foreach (LaneState lane in base.Owner.Owner.Opponent.Lanes)
		{
			if (lane.Creature == null)
				continue;

			var enemy = lane.Creature;

			// POSITIVE EFFECT REMOVAL (DISPEL)

			if (enemy.HasSiphon)
			{
				CancelStatus(enemy, StatusEnum.APSiphon);
				result = true;
			}

			if (enemy.HasAPFlower)
			{
				CancelStatus(enemy, StatusEnum.APFlower);
				result = true;
			}

			if (enemy.HasInvisible)
			{
				CancelStatus(enemy, StatusEnum.Invisible);
				result = true;
			}

			if (enemy.HasStealth)
			{
				CancelStatus(enemy, StatusEnum.Stealth);
				result = true;
			}
		}

		return result;
	}
}