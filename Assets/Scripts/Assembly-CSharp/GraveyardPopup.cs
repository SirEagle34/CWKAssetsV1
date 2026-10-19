using System;
using System.Collections.Generic;
using UnityEngine;

public class GraveyardPopup : MonoBehaviour
{
	public static GraveyardPopup Instance;

	public GameObject popupRoot;

	public Transform CreaturesParent;

	private readonly Transform[] nodes = new Transform[10];

	private readonly List<CardPrefabScript> spawned = new List<CardPrefabScript>();

	private readonly List<CreatureData> spawned2 = new List<CreatureData>();

	private Action<CreatureState> onSelected;

	private bool isOpen;

	private bool nodesCached;

	private void Awake()
	{
		if (Instance != null && Instance != this)
		{
			UnityEngine.Object.Destroy(base.gameObject);
			return;
		}
		Instance = this;
		UnityEngine.Object.DontDestroyOnLoad(base.gameObject);
		popupRoot.SetActive(value: false);
	}

	private void CacheNodes()
	{
		if (nodesCached)
		{
			return;
		}
		for (int i = 0; i < nodes.Length; i++)
		{
			string text = $"CreatureNode_{i + 1:D2}";
			nodes[i] = CreaturesParent.Find(text);
			if (nodes[i] == null)
			{
				Debug.LogError("[GraveyardPopup] Missing node: " + text);
			}
		}
		nodesCached = true;
	}

	public void Show(Action<CreatureState> callback)
	{
		if (!isOpen)
		{
			popupRoot.SetActive(value: true);
			CacheNodes();
			isOpen = true;
			onSelected = callback;
			UICamera.UnlockInput();
			List<CreatureState> graveyard = Singleton<DWGame>.Instance.CurrentBoardState.GetPlayerState(PlayerType.User).Graveyard;
			if (graveyard == null || graveyard.Count == 0)
			{
				Debug.LogWarning("Graveyard empty!");
				Close();
			}
			else
			{
				Spawn(graveyard);
			}
		}
	}

	private void Spawn(IReadOnlyList<CreatureState> list)
	{
		Clear();
		int num = Mathf.Min(list.Count, nodes.Length);
		for (int i = 0; i < num; i++)
		{
			if (nodes[i] == null)
			{
				Debug.LogError($"Node NULL at index {i}");
				continue;
			}
			CreatureState creatureState = list[i];
			if (creatureState != null && creatureState.Data != null)
			{
				GameObject obj = UnityEngine.Object.Instantiate(Singleton<PrefabReferences>.Instance.Card, nodes[i]);
				obj.transform.localPosition = Vector3.zero;
				obj.transform.localScale = Vector3.one;
				CardPrefabScript component = obj.GetComponent<CardPrefabScript>();
				component.Mode = CardPrefabScript.CardMode.BattlePopup;
				component.Populate(creatureState.Data);
				component.GraveyardMode = true;
				CreatureState localState = creatureState;
				_ = localState.Data;
				component.OnGraveyardClick = delegate
				{
					GraveyardManager.Instance.Remove(localState);
					onSelected?.Invoke(localState);
					Close();
				};
				spawned.Add(component);
			}
		}
	}

	private void Clear()
	{
		for (int i = 0; i < spawned.Count; i++)
		{
			if (spawned[i] != null)
			{
				UnityEngine.Object.Destroy(spawned[i].gameObject);
			}
		}
		spawned.Clear();
	}

	public void Close()
	{
		if (isOpen)
		{
			isOpen = false;
			popupRoot.SetActive(value: false);
			Clear();
			onSelected = null;
			UICamera.UnlockInput();
		}
	}

	private void OnDisable()
	{
		isOpen = false;
		Clear();
		onSelected = null;
	}
}
