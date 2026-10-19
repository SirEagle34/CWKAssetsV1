public class GrantLandSwampBraveryIfOppHasMoreCards : OnStartTurn
{
	public override bool OnEnable()
	{
		if (base.Owner.Owner.Opponent.Hand.Count > base.Owner.Owner.Hand.Count)
		{
			ApplyStatus(base.Owner, StatusEnum.LandSwamp, 1f);
			ApplyStatus(base.Owner, StatusEnum.Bravery, base.Val1);
			return true;
		}
		return false;
	}
}
