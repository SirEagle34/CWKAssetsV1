public class EnhanceSTRlandCornHasteAttackUpCornFriendlyOnHPThreshold : OnFriendlyDamaged
{
	public override bool OnEnable()
	{
		float num = (float)(Attacked.HP + (int)Damage) / (float)Attacked.MaxHP;
		if (Attacked.HPPct < base.Val1Pct && num >= base.Val1Pct)
		{
			ApplyStatus(Attacked, StatusEnum.LandCorn, 1f);
			ApplyStatus(Attacked, StatusEnum.EnhanceSTR, base.Val2);
			ApplyStatus(Attacked, StatusEnum.BurstSTR, base.Val3);
			ApplyStatus(Attacked, StatusEnum.Adrenaline, base.Val4);
			return true;
		}
		return false;
	}
}
