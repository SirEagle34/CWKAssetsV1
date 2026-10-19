using UnityEngine;

public class APVeba : StatusState
{
	public override void ProcessMessage(GameMessage Message)
	{
		if (base.Target != null && Message.Action == GameEvent.START_TURN && Message.WhichPlayer == base.Target.Owner)
		{
			int num = Mathf.CeilToInt((float)base.Target.MaxHP * base.Intensity);
			ReportStatusAction(GameEvent.STATUS_ACTION_APVEBA, num);
			base.Target.DealDamage(num, AttackBase.None, false, null, this);
			base.Target.Owner.LoseActionPoints(1); // flat 1 energy
		}
	}
}
