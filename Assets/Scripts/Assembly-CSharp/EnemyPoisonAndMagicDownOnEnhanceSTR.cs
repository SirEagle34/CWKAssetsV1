public class EnemyPoisonAndMagicDownOnEnhanceSTR : OnEnhanceSTR
{
	public override bool OnEnable()
	{
		bool result = false;

		foreach (LaneState lane in base.Owner.Owner.Opponent.Lanes)
		{
			if (lane.Creature != null)
			{
				ApplyStatus(
					lane.Creature,
					StatusEnum.Poison,
					base.Val1
				);

				ApplyStatus(
					lane.Creature,
					StatusEnum.WeakenINT,
					base.Val2
				);

				result = true;
			}
		}

		return result;
	}
}