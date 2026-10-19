public class GrantBurstINTBraveryOnLandPlains : OnLandPlains
{
	public override bool OnEnable()
	{
		ApplyStatus(base.Owner, StatusEnum.MagicSpike, base.Val1);
		ApplyStatus(base.Owner, StatusEnum.Bravery, base.Val2);		
		return true;
	}
}
