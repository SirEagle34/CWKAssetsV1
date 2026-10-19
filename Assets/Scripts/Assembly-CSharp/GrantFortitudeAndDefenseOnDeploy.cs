public class GrantFortitudeAndDefenseOnDeploy : OnDeploy
{
	public override bool OnEnable()
	{
		ApplyStatus(base.Owner, StatusEnum.EnhanceSTR, base.Val1);
		ApplyStatus(base.Owner, StatusEnum.FortifyDEF, base.Val1);
		return true;
	}
}