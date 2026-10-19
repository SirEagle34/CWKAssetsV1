using UnityEngine;

public class UselessDeform : StatusState
{
    private static readonly string[] RandomStatusPool =
    {
        "Paralyze",
        "Berserk",
        "Plague",
        "Poison",
        "Burned",
        "Frozen",
        "Blind",
        "Cripple",
        "Oiled",
        "CardBlock",
        "Bonebreaker",
        "Mindless",
        "Transmogrify",
        "APPoison",
        "APVeba"
    };

    public override void ProcessMessage(GameMessage Message)
    {
        if (Message.Action != GameEvent.CREATURE_ATTACKED)
            return;

        if (Message.Creature != Target)
            return;

        string statusId = RandomStatusPool[
            KFFRandom.RandomIndex(RandomStatusPool.Length)
        ];

        StatusData data = StatusDataManager.Instance.GetData(statusId);

        if (data != null)
        {
            float duration = 3f;

            switch (statusId)
            {
                case "Berserk":
                case "Bonebreaker":
                    duration = 1f;
                    break;

                case "Poison":
                    duration = 5f;
                    break;

                case "Burned":
                    duration = 45f;
                    break;

                case "Plague":
                case "APVeba":
                    duration = 30f;
                    break;
            }

            Target.ApplyStatus(data, duration, null);
        }

        TickCount();
    }
}