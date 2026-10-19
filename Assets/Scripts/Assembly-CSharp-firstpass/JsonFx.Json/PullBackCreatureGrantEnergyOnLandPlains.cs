public class PushCreatureOnLandPlains : OnLandPlains
{
    public override bool OnEnable()
    {
        if (base.Owner != null)
        {
            ApplyStatus(base.Owner, StatusEnum.Push, base.Val1);
            base.Owner.Owner.AddActionPoints(base.Val2);
            return true;
        }
        return false;
    }
}