public class GainEnergyMagicBoostIfOppHasMoreCards : OnStartTurn
{
	public override bool OnEnable()
	{
		if (base.Owner.Owner.Opponent.Hand.Count > base.Owner.Owner.Hand.Count)
		{
			base.Owner.Owner.AddActionPoints(base.Val1);
			ApplyStatus(base.Owner, StatusEnum.MagicSpike, base.Val2);
			return true;
		}
		return false;
	}
}
