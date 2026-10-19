public class BraveryFortifyRESOnOppFloop : OnOppPlayedCard
{
	public override bool OnEnable()
	{
		bool result = false;

		if (CardPlayed.IsLeaderCard)
		{
			if (base.Owner.Lane.Creature != null)
			{
				ApplyStatus(base.Owner, StatusEnum.Bravery, base.Val1);
				ApplyStatus(base.Owner, StatusEnum.FortifyRES, base.Val1);
				result = true;
			}
		}

		return result;
	}
}