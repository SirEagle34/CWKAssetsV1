public class GrantFortifyRESandFortifyDEFOnDeploy : OnDeploy
{
	public override bool OnEnable()
	{
		ApplyStatus(base.Owner, StatusEnum.FortifyRES, base.Val1);
		ApplyStatus(base.Owner, StatusEnum.FortifyDEF, base.Val2);
		return true;
	}
}