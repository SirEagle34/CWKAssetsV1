public class RegenFortifyDEFEachTurn : OnStartTurn
{
	public override bool OnEnable()
	{
		ApplyStatus(base.Owner, StatusEnum.Regen, base.Val1);
		ApplyStatus(base.Owner, StatusEnum.FortifyDEF, base.Val2);
		return true;
	}
}
