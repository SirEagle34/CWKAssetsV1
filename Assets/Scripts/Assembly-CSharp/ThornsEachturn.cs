public class ThornsEachturn : OnStartTurn
{
	public override bool OnEnable()
	{
		ApplyStatus(base.Owner, StatusEnum.Thorns, base.Val1);
		return true;
	}
}
