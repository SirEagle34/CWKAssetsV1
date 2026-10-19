public class StealPush : StatusState
{
    protected override void OnEnable()
    {
        if (Target == null || Target.Owner == null)
            return;

        PlayerState oldOwner = Target.Owner;
        PlayerState stealer = oldOwner.Opponent;
        if (stealer == null)
            return;

        // 🔥 MANUAL VISUAL REMOVAL (ONCE)
        Singleton<DWBattleLane>.Instance.RemoveCreatureObj(
            Target,
            null,
            false
        );

        CreatureItem data = Target.Data;

        // logic reset
        Target.Clean();
        Target.Init(stealer, data);
        stealer.DeploymentList.Add(Target);
    }
}
