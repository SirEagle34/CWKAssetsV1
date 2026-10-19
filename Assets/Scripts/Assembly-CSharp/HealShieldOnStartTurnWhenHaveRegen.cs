public class HealShieldOnStartTurnWhenHaveRegen : OnStartTurn
{
	public override bool OnEnable()
	{
		if ((bool)base.Owner.HasRegen && !base.Owner.AtFullHealth)
		{
			base.Owner.Heal(base.Val1Pct);
			ApplyStatus(base.Owner, StatusEnum.Shield, base.Val2);
			return true;
		}
		return false;
	}
}
