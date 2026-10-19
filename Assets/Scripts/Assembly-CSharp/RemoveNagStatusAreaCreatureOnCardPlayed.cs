public class RemoveNagStatusAreaCreatureOnCardPlayed : OnPlayedCardOnCreature
{
    public override bool OnEnable()
    {
        foreach (LaneState thisAndAdjacentLane in base.Owner.Lane.ThisAndAdjacentLanes)
        {
            if (thisAndAdjacentLane.Creature != null)
            {
                ApplyStatus(thisAndAdjacentLane.Creature, StatusEnum.RemoveStatus, base.Val1, StatusRemovalData.RemoveRandomDebuffs());
            }
        }
        return true;
    }
}