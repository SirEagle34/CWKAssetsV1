public class FirstBurnEnemyDragAttacks : OnEnemyAttack
{
    public override bool OnEnable()
    {
        if (IsDragAttack && Attacker.DragAttacksThisTurn >= 1)
        {
            ApplyStatus(Attacker, StatusEnum.Burned, base.Val1);
            return true;
        }
        return false;
    }
}