public class GrantAutoReviveRegenToOppTeamAndBuffSelfIfHPLow : OnAttacked
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

			// Kendisi
			ApplyStatus(base.Owner, StatusEnum.AutoRevive, base.Val2);
			ApplyStatus(base.Owner, StatusEnum.Regen, base.Val3);

			result = true;
		}

		return result;
	}
}