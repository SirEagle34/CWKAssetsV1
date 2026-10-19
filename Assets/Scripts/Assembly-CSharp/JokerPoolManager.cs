using System.Collections.Generic;
using UnityEngine;

public class JokerPoolManager : MonoBehaviour
{
	public static JokerPoolManager Instance;

	private void Awake()
	{
		if (Instance != null)
		{
			Destroy(gameObject);
			return;
		}

		Instance = this;
	}

	public List<CardData> GetPool(string poolID)
	{
		List<CardData> pool = new List<CardData>();

		JokerPoolData poolData = JokerPoolDataManager.Instance.GetData(poolID);

		if (poolData == null)
		{
			Debug.LogError("Joker Pool not found: " + poolID);
			return pool;
		}

		foreach (string cardID in poolData.Cards)
		{
			CardData cardData = CardDataManager.Instance.GetData(cardID);

			if (cardData == null)
			{
				Debug.LogError(
					"Joker Pool '" + poolID +
					"' contains missing card: " + cardID
				);

				continue;
			}

			pool.Add(cardData);
		}

		Debug.Log(
			"Joker Pool loaded: " +
			poolID +
			" | Cards: " +
			pool.Count
		);

		return pool;
	}
}