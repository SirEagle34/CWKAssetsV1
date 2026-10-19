using UnityEngine;
public class GrantImmunityInvisibleBrilliantDrawCardWhenEnemyUsesMagicAttack : OnEnemyDamaged
{
	public override bool OnEnable()
	{
		// sadece magic attack reaction
		if (!Magic || Physical)
			return false;

		if (Attacked == null)
			return false;

		// owner reward
		base.Owner.DrawCard();

		ApplyStatus(base.Owner, StatusEnum.Immunity, base.Val1);
		ApplyStatus(base.Owner, StatusEnum.Brilliant, base.Val2);

		return true;
	}
}