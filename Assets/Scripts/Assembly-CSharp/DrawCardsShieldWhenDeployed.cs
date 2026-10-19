public class DrawCardsShieldWhenDeployed : OnDeploy
{
	public override bool OnEnable()
	{
		for (int i = 0; i < base.Val1; i++)
		{
			base.Owner.DrawCard();
		}
		ApplyStatus(base.Owner, StatusEnum.Shield, base.Val2);
		return true;
	}
}
