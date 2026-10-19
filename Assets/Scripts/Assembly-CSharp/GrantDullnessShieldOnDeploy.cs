using UnityEngine;
public class GrantDullnessShieldOnDeploy : OnDeploy
{
	public override bool OnEnable()
	{
		ApplyStatus(base.Owner, StatusEnum.Dullness, base.Val1);
		ApplyStatus(base.Owner, StatusEnum.Shield, base.Val2);
		return true;
	}
}
