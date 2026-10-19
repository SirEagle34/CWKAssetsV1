using UnityEngine;

public class IFOppAttack3TimesInflictAPPoisonOnAttacker : OnEnemyAttack 
{
	public override bool OnEnable()
	{
		if (Attacker.DragAttacksThisTurn == 3 && IsDragAttack)
		{
			ApplyStatus(Attacker, StatusEnum.APPoison, base.Val1); 

			return true;
		}
		return false;
	}
}
