using UnityEngine;

public class Invisible : StatusState
{
    protected override void OnEnable()
    {
        if (base.Target != null)
        {
            base.Target.HasInvisible = true;
        }
    }

    public override void ProcessMessage(GameMessage Message)
    {
        if (base.Target == null)
            return;

        if (Message.Action == GameEvent.CREATURE_ATTACKED &&
            Message.Creature == base.Target)
        {
            // Drag attack breaks invisibility
            if (Message.IsDrag)
            {
                Disable();
                return;
            }

            // Magic attack breaks invisibility
            if (Message.AttackType == AttackBase.INT)
            {
                Disable();
                return;
            }
        }
    }
    public override bool Dodge(AttackCause attackCause)
	{
		if (attackCause == AttackCause.Drag || attackCause == AttackCause.Berserk || attackCause == AttackCause.Counter || attackCause == AttackCause.Revenge)
		{
			ReportStatusAction(GameEvent.STATUS_ACTION_INVISIBLE);
			return true;
		}
		return false;
	}

    protected override void OnDisable()
    {
        if (base.Target != null)
        {
            base.Target.HasInvisible = false;
        }
    }
}
