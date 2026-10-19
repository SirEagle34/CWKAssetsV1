public class BLOCKBraveryAndRevengeWhenDeploying : OnDeployed
{
    public override bool OnEnable()
    {
        if (DeployedCreature == null || DeployedCreature.Data?.Form == null)
            return false;

        // 🚫 Exclude by ID
        if (DeployedCreature.Data.Form.ID == "SandasaurusRex_Awaken")
            return false;

        if (DeployedCreature.Data.Form.Faction == CreatureFaction.Green)
        {
            ApplyStatus(DeployedCreature, StatusEnum.Bravery, base.Val1);
            ApplyStatus(base.Owner, StatusEnum.Revenge, 1f);
            return true;
        }

        return false;
    }
}