public class BraveryFrostArmorOnManyEnemyDragAttacks : OnEnemyAttack
{
    public override bool OnEnable()
    {
        if (Attacker.DragAttacksThisTurn == 3 && IsDragAttack)
        {
            if (Attacker != null)
            {
                CreatureState creature = Attacker;

                // Apply Bravery + FrostArmor
                ApplyStatus(base.Owner, StatusEnum.Bravery, base.Val1);
                ApplyStatus(base.Owner, StatusEnum.FrostArmor, base.Val1);
            }

            return true;
        }

        return false;
    }
}