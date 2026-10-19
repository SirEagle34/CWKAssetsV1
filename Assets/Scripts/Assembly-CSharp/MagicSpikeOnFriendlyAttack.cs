public class MagicSpikeOnFriendlyAttack : OnFriendlyAttack
{
	public override bool OnEnable()
	{
		if (base.Val1Chance)
		{
			ApplyStatus(Attacker, StatusEnum.MagicSpike, base.Val1);
			return true;
		}
		return false;
	}
}
