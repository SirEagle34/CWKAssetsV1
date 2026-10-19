using UnityEngine;

public class RemoveDebuffStatusIfLowHP : OnStartTurn
{
	public override bool OnEnable()
	{
		if (base.Owner.HPPct < base.Val1Pct)
		{
			ApplyStatus(base.Owner, StatusEnum.RemoveStatus, base.Val2, StatusRemovalData.RemoveRandomDebuffs());
			return true;
		}
		return false;
	}
}
