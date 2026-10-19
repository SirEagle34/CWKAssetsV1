public class VampiricWhenBravery : OnFriendlyBravery
{
	public override bool OnEnable()
	{
		{
			ApplyStatus(Target, StatusEnum.Vampiric, base.Val1);
			return true;
		}
	}
}
