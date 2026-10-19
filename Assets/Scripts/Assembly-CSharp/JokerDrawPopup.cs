using System.Collections.Generic;
using UnityEngine;

public class JokerDrawPopup : MonoBehaviour
{
    public static JokerDrawPopup Instance;

    public GameObject popupRoot;
    public Transform CardsParent;

    private Transform[] cardNodes = new Transform[14];
    private List<CardPrefabScript> mSpawnedCards = new();

    private System.Action<CardData> onCardSelected;

    private void Awake()
    {
        Instance = this;
        popupRoot.SetActive(false);
        CacheNodes();
    }

    private void CacheNodes()
    {
        for (int i = 0; i < cardNodes.Length; i++)
        {
            string nodeName = $"ActionCardNode_{i + 1:D2}";
            cardNodes[i] = CardsParent.Find(nodeName);

            if (cardNodes[i] == null)
                Debug.LogError($"Missing node: {nodeName}");
        }
    }

    public void Show(string poolID, System.Action<CardData> callback)
    {
        popupRoot.SetActive(true);

        UICamera.UnlockInput(); // 🔥 EKLE

        onCardSelected = callback;

        var items = JokerPoolManager.Instance.GetPool(poolID);
        Spawn(items);
    }

    private void Spawn(List<CardData> items)
    {
        Clear();

        int count = Mathf.Min(items.Count, cardNodes.Length);

        for (int i = 0; i < count; i++)
        {
            var go = Instantiate(
                Singleton<PrefabReferences>.Instance.Card,
                cardNodes[i]
            );

            go.transform.localPosition = Vector3.zero;
            go.transform.localScale = Vector3.one;

            var card = go.GetComponent<CardPrefabScript>();
            card.Mode = CardPrefabScript.CardMode.BattlePopup;

            card.Populate(items[i]);

            CardData captured = items[i];

            card.JokerMode = true;
            card.OnJokerClick = null;
            card.OnJokerClick += () =>
            {
                onCardSelected?.Invoke(captured);
                Close(); // 🔥 sadece kapat
            };

            mSpawnedCards.Add(card);
        }
    }

    private void Clear()
    {
        foreach (var c in mSpawnedCards)
        {
            if (c != null)
                c.JokerMode = false;

            Destroy(c.gameObject);
        }

        mSpawnedCards.Clear();
    }

    public void Close()
    {    
        popupRoot.SetActive(false);
        Clear();
        onCardSelected = null;
    }
}