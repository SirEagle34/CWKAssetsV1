using UnityEngine;

public class LandIcy : StatusState
{
	public override void ProcessMessage(GameMessage Message)
	{
		if (Message.Action == GameEvent.CREATURE_ATTACKED && Message.SecondCreature == base.Target && !Message.IsMiss)
		{
			Message.Creature.ApplyStatus(StatusDataManager.Instance.GetData(StatusEnum.Frozen.Name()), 1f, null);
		}
		if (base.Target != null && Message.Action == GameEvent.START_TURN && Message.WhichPlayer == base.Target.Owner)
		{
			int num = Mathf.CeilToInt((float)base.Target.MaxHP * base.Intensity);
			ReportStatusAction(GameEvent.STATUS_ACTION_ICYLAND, num);
			base.Target.DealDamage(num, AttackBase.None, false, null, this);
		}
	}
}
