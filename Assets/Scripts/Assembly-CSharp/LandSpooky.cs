using UnityEngine;

public class LandSpooky : StatusState
{
	public override void ProcessMessage(GameMessage Message)
	{
		if (Message.Action == GameEvent.CREATURE_ATTACKED && Message.SecondCreature == base.Target && !Message.IsMiss)
		{
			Message.Creature.ApplyStatus(StatusDataManager.Instance.GetData(StatusEnum.Poison.Name()), 2f, null);
		}
	}
	
	protected override void OnEnable()
	{
		base.Target.IsParalyzed = true;
		float num = (float)base.Target.Data.INT * base.Intensity;
		base.Target.INT += num;
	}

	protected override void OnDisable()
	{
		base.Target.IsParalyzed = false;
		float num = (float)base.Target.Data.INT * base.Intensity;
		base.Target.INT -= num;
	}
}
