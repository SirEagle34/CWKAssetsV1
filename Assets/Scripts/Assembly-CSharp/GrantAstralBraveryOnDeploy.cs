using UnityEngine;
public class GrantAstralBraveryOnDeploy : OnDeploy
{
	public override bool OnEnable()
	{
		ApplyStatus(base.Owner, StatusEnum.Astral, base.Val1);
		ApplyStatus(base.Owner, StatusEnum.Bravery, base.Val2);
		return true;
	}
}
