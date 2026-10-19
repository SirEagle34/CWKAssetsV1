using UnityEngine;

public class FlamingDefenseUpWhenThorns : OnFriendlyThorns
{
	public override bool OnEnable()
	{
		{
			ApplyStatus(Target, StatusEnum.Flaming, base.Val1);
			ApplyStatus(Target, StatusEnum.FortifyDEF, base.Val2);
			return true;
		}
	}
}
