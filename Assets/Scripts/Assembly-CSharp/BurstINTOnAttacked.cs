public class BurstINTOnAttacked : OnAttacked
{
	public override bool OnEnable()
	{
		if (base.Val1Chance)
		{
			ApplyStatus(base.Owner, StatusEnum.MagicSpike, base.Val2);
			return true;
		}
		return false;
	}
}
