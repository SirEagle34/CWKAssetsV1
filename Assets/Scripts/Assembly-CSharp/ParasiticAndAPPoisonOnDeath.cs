using UnityEngine;
public class ParasiticAndAPPoisonOnDeath : OnDied
{
    public override bool OnEnable()
    {
        // Friendly team: Parasitic 3 turn
        foreach (LaneState lane in base.Owner.Owner.Lanes)
        {
            if (lane.Creature != null)
            {
                ApplyStatus(lane.Creature, StatusEnum.Parisitic, base.Val1);
            }
        }

        // Enemy team: APPoison 1x
        foreach (LaneState lane in base.Owner.Owner.Opponent.Lanes)
        {
            if (lane.Creature != null)
            {
                ApplyStatus(lane.Creature, StatusEnum.APPoison, base.Val2);
            }
        }

        return true;
    }
}
