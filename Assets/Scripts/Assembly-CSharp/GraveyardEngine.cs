using UnityEngine;

public static class GraveyardEngine
{
	public static CreatureItem ResolveCreatureItem(CreatureState state)
	{
		if (state == null)
		{
			return null;
		}
		if (state.Data != null)
		{
			return state.Data;
		}
		Debug.LogError("CreatureItem resolve failed");
		return null;
	}

	public static void SpawnFromGraveyard(CreatureState state, PlayerState owner, int laneIndex)
	{
		if (state != null && owner != null)
		{
			CreatureState creatureState = state.DeepCopy();
			creatureState.Owner = owner;
			creatureState.IsInGraveyard = false;
			creatureState.Lane = owner.Lanes[laneIndex];
			owner.Graveyard.Add(creatureState);
			CreatureItem creatureItem = ResolveCreatureItem(creatureState);
			if (creatureItem == null)
			{
				Debug.LogError("GraveyardEngine: CreatureItem NULL");
			}
			else
			{
				Singleton<DWBattleLane>.Instance.AddLaneObject(creatureState, creatureItem, laneIndex);
			}
		}
	}
}
