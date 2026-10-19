public class ThornsFortifyDEFEachTurn : OnStartTurn
{
	public override bool OnEnable()
	{
		ApplyStatus(base.Owner, StatusEnum.Thorns, base.Val1);
		ApplyStatus(base.Owner, StatusEnum.FortifyDEF, base.Val2);
		return true;
	}
}
