using System.Collections.Generic;
using UnityEngine;

public class GraveyardManager : MonoBehaviour
{
	public static GraveyardManager Instance;

	public List<CreatureState> Graveyard
	{
		get
		{
			if (Singleton<PlayerInfoScript>.Instance == null)
			{
				Debug.LogError("PlayerInfoScript Instance NULL");
				return new List<CreatureState>();
			}
			if (Singleton<PlayerInfoScript>.Instance.PlayerStateData == null)
			{
				Debug.LogError("PlayerStateData NULL");
				return new List<CreatureState>();
			}
			return Singleton<PlayerInfoScript>.Instance.PlayerStateData.Graveyard;
		}
	}

	private void Awake()
	{
		Instance = this;
	}

	public void Add(CreatureState state)
	{
		if (state != null)
		{
			Graveyard.Add(state);
		}
	}

	public void Remove(CreatureState state)
	{
		Graveyard.Remove(state);
	}

	public List<CreatureState> GetByOwner(PlayerType owner)
	{
		return Graveyard.FindAll((CreatureState x) => x.Owner01 == owner);
	}
}
