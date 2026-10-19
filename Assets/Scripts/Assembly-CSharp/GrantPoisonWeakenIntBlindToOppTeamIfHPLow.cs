public class GrantPoisonWeakenIntBlindToOppTeamIfHPLow : OnAttacked
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

			foreach (LaneState lane in base.Owner.Owner.Opponent.Lanes)
			{
				if (lane.Creature != null)
				{
					ApplyStatus(lane.Creature, StatusEnum.Poison, base.Val2);
					ApplyStatus(lane.Creature, StatusEnum.WeakenINT, base.Val2);
					ApplyStatus(lane.Creature, StatusEnum.Blind, base.Val3);
					result = true;
				}
			}
		}

		return result;
	}
}