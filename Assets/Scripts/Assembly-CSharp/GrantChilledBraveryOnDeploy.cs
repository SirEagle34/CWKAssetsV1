using UnityEngine;

public class GrantChilledBraveryOnDeploy : OnDeploy
{
	public override bool OnEnable()
	{
		ApplyStatus(base.Owner, StatusEnum.Chilled, base.Val1);
		ApplyStatus(base.Owner, StatusEnum.Bravery, base.Val2);
		return true;
	}
}