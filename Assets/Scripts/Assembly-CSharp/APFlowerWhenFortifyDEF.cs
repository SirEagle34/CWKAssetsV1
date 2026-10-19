public class APFlowerWhenFortifyDEF : OnFriendlyFortifyDEF
{
	public override bool OnEnable()
	{
		{
			ApplyStatus(Target, StatusEnum.APFlower, base.Val1);
			return true;
		}
	}
}
