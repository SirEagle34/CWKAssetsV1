using UnityEngine;

public class StoreBuyEvoMaterialScript : MonoBehaviour
{
    public UILabel Name;
    public UILabel Cost;

    public UILabel BuyCount;

    public Transform InventoryTileNode;

    private StoreBuyEvoMaterialData mData;

    public void RefreshBuyCount()
    {
        if (mData == null)
        {
            Debug.LogWarning(
                "[Evo Material UI] RefreshBuyCount: mData is NULL."
            );

            return;
        }

        if (BuyCount == null)
        {
            Debug.LogWarning(
                "[Evo Material UI] RefreshBuyCount: BuyCount UILabel is NULL."
            );

            return;
        }

        int buyCount = 0;

        if (StoreScreenController.Instance != null)
        {
            buyCount =
                StoreScreenController.Instance.GetEvoMaterialBuyCount(
                    mData.ID
                );
        }

        const int buyLimit = 1;

        int remaining =
            Mathf.Max(
                0,
                buyLimit - buyCount
            );

        BuyCount.text =
            remaining + "x Buy";

        Debug.Log(
            "[Evo Material UI] REFRESH: " +
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
        StoreBuyEvoMaterialData data =
            dataObj as StoreBuyEvoMaterialData;

        if (data == null)
        {
            Debug.LogError(
                "StoreBuyEvoMaterialScript: data is NULL."
            );

            return;
        }

        if (data.EvoMaterial == null)
        {
            Debug.LogError(
                "StoreBuyEvoMaterialScript: " +
                "EvoMaterial is NULL."
            );

            return;
        }

        if (InventoryTileNode == null)
        {
            Debug.LogError(
                "StoreBuyEvoMaterialScript: " +
                "InventoryTileNode is NULL."
            );

            return;
        }

        GameObject inventoryTilePrefab =
            Singleton<PrefabReferences>.Instance.InventoryTile;

        if (inventoryTilePrefab == null)
        {
            Debug.LogError(
                "StoreBuyEvoMaterialScript: " +
                "PrefabReferences.InventoryTile is NULL."
            );

            return;
        }

        mData = data;

        Name.text = data.EvoMaterial.Name;
        Cost.text = data.BuyCost + " Wishbones";

        // =====================================================
        // BUY COUNT
        // =====================================================

        int buyCount = 0;

        if (StoreScreenController.Instance != null)
        {
            buyCount =
                StoreScreenController.Instance.GetEvoMaterialBuyCount(
                    data.ID
                );
        }

        const int buyLimit = 1;

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
            "[Evo Material UI] " +
            data.ID +
            " BuyCount: " +
            buyCount +
            "/" +
            buyLimit +
            " Remaining: " +
            remaining
        );

        // =====================================================
        // INVENTORY TILE
        // =====================================================

        InventoryTileNode.DestroyAllChildren();

        GameObject tileObject =
            InventoryTileNode.InstantiateAsChild(
                inventoryTilePrefab
            );

        if (tileObject == null)
        {
            Debug.LogError(
                "StoreBuyEvoMaterialScript: " +
                "InventoryTile clone could not be created."
            );

            return;
        }

        Debug.Log(
            "EVO MATERIAL INVENTORY TILE CREATED: " +
            tileObject.name
        );

        // =====================================================
        // BRING INVENTORY TILE TO FRONT
        // =====================================================

        UIWidget[] widgets =
            tileObject.GetComponentsInChildren<UIWidget>(true);

        Debug.Log(
            "[Evo Material UI] InventoryTile widgets: " +
            widgets.Length
        );

        foreach (UIWidget widget in widgets)
        {
            widget.depth += 100;
        }

        // =====================================================
        // GET INVENTORY TILE
        // =====================================================

        InventoryTile tile =
            tileObject.GetComponent<InventoryTile>();

        if (tile == null)
        {
            Debug.LogError(
                "StoreBuyEvoMaterialScript: " +
                "Created InventoryTile clone has no " +
                "InventoryTile component."
            );

            return;
        }

        // =====================================================
        // POPULATE INVENTORY TILE
        // =====================================================

        InventorySlotItem item =
            new InventorySlotItem(data.EvoMaterial);

        tile.Populate(item);

        Debug.Log(
            "[Evo Material UI] Populate completed: " +
            data.ID
        );
    }

    public void OnEntryClicked()
    {
        if (mData == null)
        {
            Debug.LogError(
                "StoreBuyEvoMaterialScript: mData is NULL."
            );

            return;
        }

        if (StoreScreenController.Instance == null)
        {
            Debug.LogError(
                "StoreBuyEvoMaterialScript: " +
                "StoreScreenController.Instance is NULL."
            );

            return;
        }

        // =====================================================
        // BUY LIMIT CHECK
        // =====================================================

        int buyCount =
            StoreScreenController.Instance.GetEvoMaterialBuyCount(
                mData.ID
            );

        const int buyLimit = 1;

        Debug.Log(
            "[Evo Material UI] Click: " +
            mData.ID +
            " BuyCount: " +
            buyCount +
            "/" +
            buyLimit
        );

        if (buyCount >= buyLimit)
        {
            Debug.LogWarning(
                "[Monthly Store] Evo Material buy limit reached: " +
                mData.ID +
                " (" +
                buyCount +
                "/" +
                buyLimit +
                ")"
            );

            Singleton<SimplePopupController>.Instance.ShowMessage(
                string.Empty,
                "Buy limit reached."
            );

            return;
        }

        // =====================================================
        // BUY
        // =====================================================

        StoreScreenController.Instance.OnClickBuyEvoMaterial(
            mData
        );
    }
}