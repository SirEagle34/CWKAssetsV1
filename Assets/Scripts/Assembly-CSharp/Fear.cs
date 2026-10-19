public class Fear : Frozen
{
	protected override void OnEnable()
	{
		base.OnEnable();
		base.Target.IsParalyzed = true;
	}
}
