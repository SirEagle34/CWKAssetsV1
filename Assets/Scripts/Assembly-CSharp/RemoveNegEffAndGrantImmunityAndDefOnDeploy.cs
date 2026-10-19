public class RemoveNegEffAndGrantImmunityAndDefOnDeploy : OnDeploy
{
    public override bool OnEnable()
    {
        bool result = false;
        foreach (LaneState lane in base.Owner.Owner.Lanes)
        {
            if (lane.Creature != null)
            {
                ApplyStatus(lane.Creature, StatusEnum.RemoveStatus, base.Val1, StatusRemovalData.RemoveRandomDebuffs());
                ApplyStatus(lane.Creature, StatusEnum.Immunity, base.Val2);
                ApplyStatus(lane.Creature, StatusEnum.FortifyDEF, base.Val3);
                result = true;
            }
        }
        return result;
    }
}
