using UnityEngine;
public class GrantClumsilyBraveryOnDeployAndAdrenalineOnFriendlyWeakenSTR : AbilityState
{
	protected CreatureState CreatureAffected;

	public override bool ProcessMessage(GameMessage Message)
	{
		// OnDeploy
		if (Message.Action == GameEvent.CREATURE_DEPLOYED &&
			Message.Creature == base.Owner)
		{
			ApplyStatus(base.Owner, StatusEnum.Clumsily, base.Val1);
			ApplyStatus(base.Owner, StatusEnum.Bravery, base.Val2);
			return true;
		}

		// OnFriendlyWeakenSTR
		if (Message.Action == GameEvent.ENABLE_WEAKENSTR &&
			Message.Creature.Owner == base.Owner.Owner)
		{
			CreatureAffected = Message.Creature;

			ApplyStatus(CreatureAffected, StatusEnum.Adrenaline, base.Val3);
			return true;
		}

		return false;
	}
}