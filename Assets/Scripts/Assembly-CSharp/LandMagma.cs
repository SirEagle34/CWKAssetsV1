using UnityEngine;

public class LandMagma : StatusState
{
	public override void ProcessMessage(GameMessage Message)
	{
		if (Message.Action == GameEvent.CREATURE_ATTACKED && Message.SecondCreature == base.Target && !Message.IsMiss)
		{
			Message.Creature.ApplyStatus(StatusDataManager.Instance.GetData(StatusEnum.Burned.Name()), base.Intensity * 100f, null);
		}
		if (base.Target != null && Message.Action == GameEvent.START_TURN && Message.WhichPlayer == base.Target.Owner)
		{
			base.Target.ApplyStatus(StatusDataManager.Instance.GetData(StatusEnum.Burned.Name()), base.Intensity2 * 100f, null);
			ReportStatusAction(GameEvent.STATUS_ACTION_BURNED);
		}
	}
}