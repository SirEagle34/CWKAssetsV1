using System.Collections;
using UnityEngine;

public class FrontEndPIPController : Singleton<FrontEndPIPController>
{
	public LeaderData[] Leaders = new LeaderData[2];

	public Transform[] CharacterSpawnPoints = new Transform[2];

	public FrontEndPIP[] PIPs = new FrontEndPIP[2];

	private GameObject[] mLeaderModelInstances = new GameObject[2];

	private int mLoadGeneration;

	public void ShowModelPortraits(LeaderData[] leaders, UIWidget[] iconPortraits)
	{
		mLoadGeneration++;
		StartCoroutine(LoadModelsCo(leaders, iconPortraits, mLoadGeneration));
	}

	public void HideModelPortrait()
	{
		FrontEndPIP[] pIPs = PIPs;
		for (int i = 0; i < pIPs.Length; i++)
		{
			pIPs[i].ShowPortrait(show: false);
		}
		UnloadModels();
	}

	

	private IEnumerator LoadModelsCo(
		LeaderData[] leaders,
		UIWidget[] iconPortraits,
		int generation)
	{
		if (leaders == null)
		{
			Debug.LogError("[PIP] LoadModelsCo: leaders is null.");
			yield break;
		}

		for (int i = 0; i < leaders.Length && i < mLeaderModelInstances.Length; i++)
		{
			if (generation != mLoadGeneration)
				yield break;

			if (i >= CharacterSpawnPoints.Length ||
				i >= PIPs.Length ||
				CharacterSpawnPoints[i] == null ||
				PIPs[i] == null)
			{
				Debug.LogError("[PIP] Invalid slot configuration. Slot=" + i);
				continue;
			}

			if (Leaders[i] == leaders[i] &&
				mLeaderModelInstances[i] != null)
			{
				continue;
			}

			UnloadModel(i);
			ClearSpawnPoint(i);

			if (leaders[i] == null)
			{
				PIPs[i].ShowPortrait(false);
				Leaders[i] = null;
				continue;
			}

			int slot = i;
			LeaderData requestedLeader = leaders[i];
			GameObject loadedPrefab = null;
			bool loadCompleted = false;

			Debug.Log(
				"[PIP] Loading leader: " + requestedLeader.Prefab +
				" | Slot=" + slot
			);

			yield return StartCoroutine(
				Singleton<SLOTResourceManager>.Instance.LoadLeaderResources(
					requestedLeader,
					delegate(GameObject loadedObjData)
					{
						loadedPrefab = loadedObjData;
						loadCompleted = true;
					}
				)
			);

			// Coroutine/resource callback sonucu kontrolü
			if (!loadCompleted)
			{
				Debug.LogError(
					"[PIP] Leader load callback did not complete. Slot=" + slot
				);
				PIPs[slot].ShowPortrait(false);
				continue;
			}

			// Daha yeni bir portre isteği geldiyse eski sonucu kullanma.
			if (generation != mLoadGeneration)
			{
				Debug.Log(
					"[PIP] Discarding outdated load: " +
					requestedLeader.Prefab
				);
				continue;
			}

			if (loadedPrefab == null)
			{
				Debug.LogError(
					"[PIP] Leader prefab is NULL: " +
					requestedLeader.Prefab +
					". Check SLOTResourceManager, bundle name and prefab path."
				);

				PIPs[slot].ShowPortrait(false);
				Leaders[slot] = null;
				continue;
			}

			Transform spawnPoint = CharacterSpawnPoints[slot];

			if (spawnPoint == null)
			{
				Debug.LogError("[PIP] Spawn point is NULL. Slot=" + slot);
				PIPs[slot].ShowPortrait(false);
				continue;
			}

			GameObject instance = spawnPoint.InstantiateAsChild(loadedPrefab);

			if (instance == null)
			{
				Debug.LogError(
					"[PIP] InstantiateAsChild returned null for " +
					requestedLeader.Prefab
				);

				PIPs[slot].ShowPortrait(false);
				continue;
			}

			mLeaderModelInstances[slot] = instance;
			Leaders[slot] = requestedLeader;

			instance.ChangeLayer(spawnPoint.gameObject.layer);

			BattleCharacterAnimState animState =
				instance.GetComponent<BattleCharacterAnimState>();

			if (animState != null)
			{
				animState.player = slot;
				animState.Init(requestedLeader);
			}

			if (iconPortraits == null || slot >= iconPortraits.Length)
			{
				Debug.LogWarning(
					"[PIP] Portrait icon missing for slot " + slot
				);
				PIPs[slot].ShowPortrait(false);
				continue;
			}

			PIPs[slot].Init(
				slot,
				requestedLeader,
				spawnPoint.gameObject,
				iconPortraits[slot]
			);

			PIPs[slot].ShowPortrait(true);

			Renderer[] renderers =
				instance.GetComponentsInChildren<Renderer>(true);

			Debug.Log(
				"[PIP] Loaded '" + requestedLeader.Prefab +
				"' | Slot=" + slot +
				" | Renderers=" + renderers.Length
			);
		}
	}

	private void ClearSpawnPoint(int n)
	{
		Transform transform = ((n >= 0 && n < CharacterSpawnPoints.Length) ? CharacterSpawnPoints[n] : null);
		if (!(transform == null))
		{
			for (int num = transform.childCount - 1; num >= 0; num--)
			{
				Object.Destroy(transform.GetChild(num).gameObject);
			}
		}
	}

	public void UnloadModels(bool clearResource = true)
	{
		for (int i = 0; i < mLeaderModelInstances.Length; i++)
		{
			UnloadModel(i);
		}
		if (clearResource)
		{
			Resources.UnloadUnusedAssets();
		}
	}

	private void UnloadModel(int n)
	{
		if (mLeaderModelInstances[n] != null)
		{
			Object.Destroy(mLeaderModelInstances[n]);
			mLeaderModelInstances[n] = null;
			Leaders[n] = null;
		}
	}

	public void PlayBattleStartAnim()
	{
		for (int i = 0; i < mLeaderModelInstances.Length; i++)
		{
			if (mLeaderModelInstances[i] != null)
			{
				mLeaderModelInstances[i].GetComponent<Animator>().SetTrigger(CharAnimType.NoCards_MenuAdvance.ToString());
				PlayFaceAnimation(i, CharAnimType.NoCards_MenuAdvance.ToString());
			}
		}
	}

	private void PlayFaceAnimation(int player, string faceAnimBaseName)
	{
		mLeaderModelInstances[player].GetComponent<BattleCharacterAnimState>().PlayFaceAnimation(faceAnimBaseName);
	}
}
