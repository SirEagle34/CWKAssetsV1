public class EraseShieldThornsStealthAndInflictPoisonToHighHPOnDeploy : OnDeploy
{
	public override bool OnEnable()
	{
		bool result = false;
		foreach (LaneState lane in base.Owner.Owner.Opponent.Lanes)
		{
			if (lane.Creature != null)
			{
				if ((bool)lane.Creature.HasStealth)
				{
					CancelStatus(lane.Creature, StatusEnum.Stealth);
					result = true;
				}
				if ((bool)lane.Creature.HasThorns)
				{
					CancelStatus(lane.Creature, StatusEnum.Thorns);
					result = true;
				}				
				if ((bool)lane.Creature.HasShield)
				{
					CancelStatus(lane.Creature, StatusEnum.Shield);
					result = true;
				}
				if (lane.Creature.HPPct > base.Val2Pct)
				{
					ApplyStatus(lane.Creature, StatusEnum.Poison, base.Val1);
					result = true;
				}
			}
		}
		return result;
	}
}
