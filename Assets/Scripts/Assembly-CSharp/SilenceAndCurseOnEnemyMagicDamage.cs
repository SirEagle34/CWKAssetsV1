public class SilenceAndCurseOnEnemyMagicDamage : OnEnemyDamaged
{
    public override bool OnEnable()
    {
        if (Magic && !Physical)
        {
            if (Attacked != null)
            {
                ApplyStatus(
                    Attacked,
                    StatusEnum.Mindless,
                    base.Val1
                );

                ApplyStatus(
                    Attacked,
                    StatusEnum.Poison,
                    base.Val2
                );
            }

            return true;
        }

        return false;
    }
}