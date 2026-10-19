public class EnergyOnCardThreshold : OnCardThreshold
{
	public override bool OnEnable()
	{
		base.Owner.Owner.AddActionPoints(base.Val2);
		return true;
	}
}
