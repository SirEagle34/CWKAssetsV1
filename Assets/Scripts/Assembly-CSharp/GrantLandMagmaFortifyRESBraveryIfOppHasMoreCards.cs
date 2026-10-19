public class GrantLandMagmaFortifyRESBraveryIfOppHasMoreCards : OnStartTurn
{
	public override bool OnEnable()
	{
		if (base.Owner.Owner.Opponent.Hand.Count > base.Owner.Owner.Hand.Count)
		{
			ApplyStatus(base.Owner, StatusEnum.LandMagma, 1f);
			ApplyStatus(base.Owner, StatusEnum.FortifyRES, base.Val1);
			ApplyStatus(base.Owner, StatusEnum.Bravery, base.Val2);
			return true;
		}
		return false;
	}
}
