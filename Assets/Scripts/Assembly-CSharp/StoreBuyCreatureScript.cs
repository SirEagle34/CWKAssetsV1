using UnityEngine;

public class StoreBuyCreatureScript : MonoBehaviour
{
    public UILabel Name;
    public UILabel Cost;

    public Transform InventoryTileNode;

    private StoreBuyCreatureData mData;

    public void Populate(object dataObj)
    {
        StoreBuyCreatureData data =
            dataObj as StoreBuyCreatureData;

        if (data == null)
        {
            Debug.LogError(
                "StoreBuyCreatureScript: data is NULL."
            );
            return;
        }

        if (InventoryTileNode == null)
        {
            Debug.LogError(
                "StoreBuyCreatureScript: InventoryTileNode is NULL."
            );
            return;
        }

        GameObject inventoryTilePrefab =
            Singleton<PrefabReferences>.Instance.InventoryTile;

        if (inventoryTilePrefab == null)
        {
            Debug.LogError(
                "StoreBuyCreatureScript: " +
                "PrefabReferences.InventoryTile is NULL."
            );
            return;
        }

        mData = data;

        Name.text = data.Creature.Name;
        Cost.text = data.BuyCost + " Gems";

        InventoryTileNode.DestroyAllChildren();

        GameObject tileObject =
            InventoryTileNode.InstantiateAsChild(
                inventoryTilePrefab
            );

        if (tileObject == null)
        {
            Debug.LogError(
                "StoreBuyCreatureScript: " +
                "InventoryTile clone could not be created."
            );
            return;
        }

        Debug.Log(
            "CREATURE INVENTORY TILE CREATED: " +
            tileObject.name
        );

        // =====================================================
        // BRING INVENTORY TILE TO FRONT
        // =====================================================

        UIWidget[] widgets =
            tileObject.GetComponentsInChildren<UIWidget>(true);

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
                "StoreBuyCreatureScript: " +
                "Created InventoryTile clone has no InventoryTile component."
            );
            return;
        }

        tile.PopulateAndForceDisplay(
            data.Creature
        );
    }

    public void OnEntryClicked()
    {
        if (mData == null)
        {
            Debug.LogError(
                "StoreBuyCreatureScript: mData is NULL."
            );

            return;
        }

        if (StoreScreenController.Instance == null)
        {
            Debug.LogError(
                "StoreBuyCreatureScript: " +
                "StoreScreenController.Instance is NULL."
            );

            return;
        }

        StoreScreenController.Instance.OnClickBuyCreature(
            mData
        );
    }
}