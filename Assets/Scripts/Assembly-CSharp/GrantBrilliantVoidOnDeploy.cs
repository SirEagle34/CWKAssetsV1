using UnityEngine;
public class GrantBrilliantVoidOnDeploy : OnDeploy
{
	public override bool OnEnable()
	{
		ApplyStatus(base.Owner, StatusEnum.Brilliant, base.Val1);
		ApplyStatus(base.Owner, StatusEnum.Deranged, base.Val2);
		return true;
	}
}
