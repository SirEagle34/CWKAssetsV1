public class EraseBraveryInflictAPLeakOnAttack : OnAttack
{
	public override bool OnEnable()
	{
		if ((bool)Target.HasBravery)
		{
			CancelStatus(Target, StatusEnum.Bravery);
			ApplyStatus(Target, StatusEnum.APLeak, base.Val1);
			return true;
		}
		return false;
	}
}
