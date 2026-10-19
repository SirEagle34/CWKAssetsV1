public class MultiImmunity : StatusState
{
	protected override void OnEnable()
	{
		base.Target.HasMultiImmunity = true;
		float num = (float)base.Target.Data.STR * base.Intensity;
		float num2 = (float)base.Target.Data.INT * base.Intensity2;
		base.Target.STR += num;
		base.Target.INT += num2;
	}

	protected override void OnDisable()
	{
		base.Target.HasMultiImmunity = false;
		float num = (float)base.Target.Data.STR * base.Intensity;
		float num2 = (float)base.Target.Data.INT * base.Intensity2;
		base.Target.STR -= num;
		base.Target.INT -= num2;
	}
}
