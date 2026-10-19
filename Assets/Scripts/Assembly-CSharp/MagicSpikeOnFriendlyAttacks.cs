public class MagicSpikeOnFriendlyAttacks : OnFriendlyAttack
{
	public override bool OnEnable()
	{
		if (base.Val1Chance)
		{
			ApplyStatus(Attacker, StatusEnum.MagicSpike, base.Val2);
			return true;
		}
		return false;
	}
}
