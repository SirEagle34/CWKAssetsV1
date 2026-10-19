public class DrawCardandDefenseUpWhenThorns : OnFriendlyThorns
{
	public override bool OnEnable()
	{
		{
			base.Owner.DrawCard();
		    ApplyStatus(Target, StatusEnum.FortifyDEF, base.Val1);
		    return true;
		}
	    return false;	
	}
}
