using UnityEngine;

public class GrantImmunityWisdomArmorOnFriendlyMagicAttack : OnFriendlyMagicAttack
{
	public override bool OnEnable()
	{
		if (Attacker == null)
			return false;

		ApplyStatus(Attacker, StatusEnum.Immunity, base.Val1);
		ApplyStatus(Attacker, StatusEnum.Brilliant, base.Val2);

		return true;
	}
}