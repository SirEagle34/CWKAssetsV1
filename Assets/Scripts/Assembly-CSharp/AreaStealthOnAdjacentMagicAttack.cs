public class AreaStealthOnAdjacentMagicAttack : OnPlayedCardOnFriendlyCreature
{
    public override bool OnEnable()
    {
        if (CardPlayed.AttackBase != AttackBase.INT)
            return false;

        foreach (LaneState lane in base.Owner.Lane.ThisAndAdjacentLanes)
        {
            if (lane.Creature != null && lane.Creature.HasStealth)
                return false;
        }

        bool result = false;

        foreach (LaneState lane in base.Owner.Lane.ThisAndAdjacentLanes)
        {
            if (lane.Creature != null)
            {
                ApplyStatus(lane.Creature, StatusEnum.Stealth, base.Val1);
                result = true;
            }
        }

        return result;
    }
}