using UnityEngine;

public class StoreBuyActionCardScript : MonoBehaviour
{
    public UILabel Name;
    public UILabel Cost;

    public UILabel BuyCount;

    public Transform InventoryTileNode;

    private StoreBuyActionCardData mData;

    public void RefreshBuyCount()
    {
        if (mData == null)
        {
            Debug.LogWarning(
                "[Action Card UI] RefreshBuyCount: mData is NULL."
            );

            return;
        }

        if (BuyCount == null)
        {
            Debug.LogWarning(
                "[Action Card UI] RefreshBuyCount: BuyCount UILabel is NULL."
            );

            return;
        }

        int buyCount = 0;

        if (StoreScreenController.Instance != null)
        {
            buyCount =
                StoreScreenController.Instance.GetActionCardBuyCount(
                    mData.ID
                );
        }

        const int buyLimit = 3;

        int remaining =
            Mathf.Max(
                0,
                buyLimit - buyCount
            );

        BuyCount.text =
            remaining + "x Buy";

        Debug.Log(
            "[Action Card UI] REFRESH: " +
            mData.ID +
            " BuyCount: " +
            buyCount +
            "/" +
            buyLimit +
            " Remaining: " +
            remaining
        );
    }
    
    public void Populate(object dataObj)
    {
        Debug.Log(
            "[Action Card UI] >>> Populate START"
        );

        StoreBuyActionCardData data =
            dataObj as StoreBuyActionCardData;

        if (data == null)
        {
            Debug.LogError(
                "StoreBuyActionCardScript: data is NULL."
            );
            return;
        }

        Debug.Log(
            "[Action Card UI] Populate Data: " +
            data.ID
        );

        if (InventoryTileNode == null)
        {
            Debug.LogError(
                "StoreBuyActionCardScript: InventoryTileNode is NULL."
            );
            return;
        }

        Debug.Log(
            "[Action Card UI] InventoryTileNode OK"
        );

        GameObject inventoryTilePrefab =
            Singleton<PrefabReferences>.Instance.InventoryTile;

        if (inventoryTilePrefab == null)
        {
            Debug.LogError(
                "StoreBuyActionCardScript: InventoryTile prefab is NULL."
            );
            return;
        }

        Debug.Log(
            "[Action Card UI] InventoryTile prefab OK"
        );

        mData = data;

        Name.text = data.Card.Name;
        Cost.text = data.BuyCost + " Gems";

        Debug.Log(
            "[Action Card UI] Labels populated"
        );

        int buyCount = 0;

        if (StoreScreenController.Instance != null)
        {
            buyCount =
                StoreScreenController.Instance.GetActionCardBuyCount(
                    data.ID
                );
        }

        int buyLimit = 3;

        int remaining =
            Mathf.Max(
                0,
                buyLimit - buyCount
            );

        if (BuyCount != null)
        {
            BuyCount.text =
                remaining + "x Buy";
        }

        Debug.Log(
            "[Action Card UI] BuyCount: " +
            buyCount +
            "/" +
            buyLimit +
            " Remaining: " +
            remaining
        );

        // =====================================================
        // DESTROY OLD TILE
        // =====================================================

        Debug.Log(
            "[Action Card UI] BEFORE DestroyAllChildren"
        );

        InventoryTileNode.DestroyAllChildren();

        Debug.Log(
            "[Action Card UI] AFTER DestroyAllChildren"
        );

        // =====================================================
        // INSTANTIATE TILE
        // =====================================================

        Debug.Log(
            "[Action Card UI] BEFORE InstantiateAsChild"
        );

        GameObject tileObject =
            InventoryTileNode.InstantiateAsChild(
                inventoryTilePrefab
            );

        Debug.Log(
            "[Action Card UI] AFTER InstantiateAsChild"
        );

        if (tileObject == null)
        {
            Debug.LogError(
                "StoreBuyActionCardScript: " +
                "InventoryTile clone could not be created."
            );
            return;
        }

        Debug.Log(
            "[Action Card UI] Tile CREATED: " +
            tileObject.name
        );

        // =====================================================
        // DEPTH
        // =====================================================

        Debug.Log(
            "[Action Card UI] BEFORE depth"
        );

        UIWidget[] widgets =
            tileObject.GetComponentsInChildren<UIWidget>(true);

        Debug.Log(
            "[Action Card UI] Widget count: " +
            widgets.Length
        );

        foreach (UIWidget widget in widgets)
        {
            widget.depth += 100;
        }

        Debug.Log(
            "[Action Card UI] AFTER depth"
        );

        // =====================================================
        // GET INVENTORY TILE
        // =====================================================

        Debug.Log(
            "[Action Card UI] BEFORE GetComponent"
        );

        InventoryTile tile =
            tileObject.GetComponent<InventoryTile>();

        Debug.Log(
            "[Action Card UI] AFTER GetComponent"
        );

        if (tile == null)
        {
            Debug.LogError(
                "StoreBuyActionCardScript: " +
                "Created InventoryTile clone has no InventoryTile component."
            );
            return;
        }

        // =====================================================
        // POPULATE INVENTORY TILE
        // =====================================================

        Debug.Log(
            "[Action Card UI] BEFORE InventoryTile.Populate"
        );

        InventorySlotItem item =
            new InventorySlotItem(data.Card);

        tile.Populate(item);

        Debug.Log(
            "[Action Card UI] AFTER InventoryTile.Populate"
        );

        Debug.Log(
            "[Action Card UI] <<< Populate END"
        );
    }

    public void OnEntryClicked()
    {
        if (mData == null)
        {
            Debug.LogError(
                "StoreBuyActionCardScript: mData is NULL."
            );

            return;
        }

        if (StoreScreenController.Instance == null)
        {
            Debug.LogError(
                "StoreBuyActionCardScript: " +
                "StoreScreenController.Instance is NULL."
            );

            return;
        }

        // =====================================================
        // BUY LIMIT CHECK
        // =====================================================

        int buyCount =
            StoreScreenController.Instance.GetActionCardBuyCount(
                mData.ID
            );

        const int buyLimit = 3;

        Debug.Log(
            "[Action Card UI] Click: " +
            mData.ID +
            " BuyCount: " +
            buyCount +
            "/" +
            buyLimit
        );

        if (buyCount >= buyLimit)
        {
            Debug.LogWarning(
                "[Monthly Store] Action Card buy limit reached: " +
                mData.ID +
                " (" +
                buyCount +
                "/" +
                buyLimit +
                ")"
            );

            return;
        }

        StoreScreenController.Instance.OnClickBuyActionCard(
            mData
        );
    }
}
