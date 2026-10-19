using UnityEngine;

public class FortifyDEFEachTurn : OnStartTurn
{
	public override bool OnEnable()
	{
		ApplyStatus(base.Owner, StatusEnum.FortifyDEF, base.Val1);
		return true;
	}
}
