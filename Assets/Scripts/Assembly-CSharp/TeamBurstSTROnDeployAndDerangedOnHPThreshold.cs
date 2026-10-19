using UnityEngine;

public class TeamBurstSTROnDeployAndDerangedOnHPThreshold : OnHPThreshold
{
    public override bool ProcessMessage(GameMessage Message)
    {
        // 🟢 DEPLOY EFFECT
        if (Message.Action == GameEvent.CREATURE_DEPLOYED && Message.Creature == base.Owner)
        {
            foreach (LaneState lane in base.Owner.Owner.Lanes)
            {
                if (lane.Creature == null)
                    continue;

                ApplyStatus(lane.Creature, StatusEnum.BurstSTR, base.Val3);
            }

            return true;
        }
		if (!IsActive && Message.Action == GameEvent.DAMAGE_CREATURE && Message.Creature == base.Owner && base.Owner.HPPct <= base.Val1Pct)
		{
			ApplyStatus(base.Owner, StatusEnum.Deranged, base.Val2);
			return true;
		}
        return false;
	}

    // 🔥 HP THRESHOLD EFFECT (BASE SYSTEM TRIGGER)
    public override bool OnEnable()
    {
        return true;
    }

}