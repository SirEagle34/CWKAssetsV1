public class CardBlockAndWeakenSTRWhenAdrenalinePlayedOnEnemy : OnEnemyAdrenaline
{
	public override bool OnEnable()
	{
		if (CreatureAffected == null)
			return false;

		ApplyStatus(CreatureAffected, StatusEnum.CardBlock, base.Val1);
		ApplyStatus(CreatureAffected, StatusEnum.WeakenSTR, base.Val2);

		return true;
	}
}