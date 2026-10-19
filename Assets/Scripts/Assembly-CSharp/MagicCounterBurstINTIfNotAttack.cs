public class MagicCounterBurstINTIfNotAttack : OnAttackOREndTurn
{
	public override bool OnEnable()
	{
		if (!isEndTurn)
		{
			StoredValue++;
		}
		else
		{
			if (StoredValue <= 0)
			{
				ApplyStatus(base.Owner, StatusEnum.MagicCounter, base.Val1);
				ApplyStatus(base.Owner, StatusEnum.BurstINT, base.Val2);
				return true;
			}
			StoredValue = 0;
		}
		return false;
	}
}
