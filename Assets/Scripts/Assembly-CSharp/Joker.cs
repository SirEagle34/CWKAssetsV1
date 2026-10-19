using UnityEngine;

public class Joker : StatusState
{
    private static readonly string[] RandomStatusPool =
    {
        "Immunity",
        "Flaming",
        "Stealth",
        "AutoRevive",
        "Deranged",
        "Brilliant",
        "Clumsily",
        "Parisitic"
    };

    public override void ProcessMessage(GameMessage Message)
    {
        if (Message.Action != GameEvent.CREATURE_ATTACKED)
            return;

        if (Message.Creature != Target)
            return;

        string statusId =
            RandomStatusPool[
                KFFRandom.RandomIndex(RandomStatusPool.Length)
            ];

        StatusData data =
            StatusDataManager.Instance.GetData(statusId);

        if (data != null)
        {
            Target.ApplyStatus(data, 3f, null);
        }

        TickCount();
    }
}