using UnityEngine;

public class MadnessOnCardThresholdCooldown : OnCardThreshold
{
    private int CooldownTurns;

    public override bool ProcessMessage(GameMessage Message)
    {
        if (Message.Action == GameEvent.START_TURN &&
            Message.WhichPlayer == base.Owner.Owner)
        {
            if (CooldownTurns > 0)
                CooldownTurns--;
        }

        return base.ProcessMessage(Message);
    }

    public override bool OnEnable()
    {
        if (CooldownTurns > 0)
            return false;

        bool result = false;

        foreach (LaneState lane in base.Owner.Owner.Lanes)
        {
            if (lane.Creature != null &&
                (lane.Creature.Data.Faction == CreatureFaction.Red ||
                 lane.Creature.Data.Faction == CreatureFaction.Green ||
                 lane.Creature.Data.Faction == CreatureFaction.Orange))
            {
                ApplyStatus(lane.Creature, StatusEnum.Madness, base.Val2);
                result = true;
            }
        }

        if (result)
            CooldownTurns = 3;

        return result;
    }
}