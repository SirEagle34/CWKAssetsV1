public class VampiricAndEnhanceSTRThornsOnLandSwamp : OnLandSwamp
{
	public override bool OnEnable()
	{
		ApplyStatus(base.Owner, StatusEnum.Vampiric, base.Val1);
		ApplyStatus(base.Owner, StatusEnum.Thorns, base.Val2);
		ApplyStatus(base.Owner, StatusEnum.EnhanceSTR, base.Val3);
		return true;
	}
}
