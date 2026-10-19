public class GrantDirectDamageFrozenToOppTeamAndBuffSelfIfHPLow : OnAttacked
{
	private bool Triggered;

	public override bool OnEnable()
	{
		if (Triggered)
			return false;

		bool result = false;

		if (base.Owner.HPPct < base.Val1Pct)
		{
			Triggered = true;

			// Rakip takım
			foreach (LaneState lane in base.Owner.Owner.Opponent.Lanes)
			{
				if (lane.Creature != null)
				{
					ApplyStatus(lane.Creature, StatusEnum.DirectDamage, base.Val2);
					ApplyStatus(lane.Creature, StatusEnum.Frozen, base.Val3);
					result = true;
				}
			}

			// Kendisi
			ApplyStatus(base.Owner, StatusEnum.MultiImmunity, base.Val4);
			ApplyStatus(base.Owner, StatusEnum.FortifyRES, base.Val4);

			result = true;
		}

		return result;
	}
}