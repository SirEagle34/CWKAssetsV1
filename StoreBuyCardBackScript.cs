using UnityEngine;

public class StoreBuyCardBackScript : MonoBehaviour
{
    public UITexture CardBackTexture;

    public UILabel Name;
    public UILabel Cost;
    public UILabel BuyCount;

    private StoreBuyCardBackData mData;

    // =====================================================
    // DISABLE / HIDE ENTRY
    // =====================================================

    public void Disable()
    {
        mData = null;
        gameObject.SetActive(false);
    }

    // =====================================================
    // BUY COUNT
    // =====================================================

    public void RefreshBuyCount()
    {
        if (mData == null)
        {
            Debug.LogWarning(
                "[CardBack UI] RefreshBuyCount: mData is NULL."
            );

            return;
        }

        if (BuyCount == null)
        {
            Debug.LogWarning(
                "[CardBack UI] RefreshBuyCount: BuyCount UILabel is NULL."
            );

            return;
        }

        if (StoreScreenController.Instance == null)
        {
            Debug.LogError(
                "[CardBack UI] StoreScreenController.Instance is NULL."
            );

            return;
        }

        int buyCount =
            StoreScreenController.Instance.GetCardBackBuyCount(
                mData.CardBackID
            );

        const int buyLimit = 1;

        int remaining =
            Mathf.Max(
                0,
                buyLimit - buyCount
            );

        BuyCount.text =
            remaining + "x Buy";

        Debug.Log(
            "[CardBack UI] REFRESH: " +
            mData.CardBackID +
            " BuyCount: " +
            buyCount +
            "/" +
            buyLimit +
            " Remaining: " +
            remaining
        );
    }

    // =====================================================
    // POPULATE
    // =====================================================

    public void Populate(object dataObj)
    {
        Debug.Log(
            "[CardBack UI] >>> Populate START"
        );

        StoreBuyCardBackData data =
            dataObj as StoreBuyCardBackData;

        if (data == null)
        {
            Debug.LogError(
                "StoreBuyCardBackScript: data is NULL."
            );

            Disable();
            return;
        }

        Debug.Log(
            "[CardBack UI] Populate Data: " +
            data.ID
        );

        if (data.CardBack == null)
        {
            Debug.LogError(
                "StoreBuyCardBackScript: CardBack is NULL. " +
                data.CardBackID
            );

            Disable();
            return;
        }

        if (!string.Equals(data.Set, "Permanent", System.StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(data.Set, "Route", System.StringComparison.OrdinalIgnoreCase))
        {
            Debug.LogWarning(
                "StoreBuyCardBackScript: Unsupported Set value. ID: " +
                data.ID +
                " | Set: " +
                data.Set
            );

            Disable();
            return;
        }

        mData = data;

        // =====================================================
        // NAME
        // =====================================================

        if (Name != null)
        {
            Name.text =
                data.CardBack.Name;
        }

        // =====================================================
        // COST
        // =====================================================

        if (Cost != null)
        {
            Cost.text =
                data.BuyCost + " Gems";
        }

        // =====================================================
        // CARD BACK TEXTURE
        // =====================================================

        if (CardBackTexture == null)
        {
            Debug.LogError(
                "StoreBuyCardBackScript: " +
                "CardBackTexture is NULL."
            );

            Disable();
            return;
        }

        CardBackTexture.ReplaceTexture(
            data.CardBack.TextureUI
        );

        Debug.Log(
            "[CardBack UI] Texture populated: " +
            data.CardBack.TextureUI
        );

        // =====================================================
        // BUY COUNT
        // =====================================================

        RefreshBuyCount();

        Debug.Log(
            "[CardBack UI] <<< Populate END: " +
            data.CardBackID
        );
    }

    // =====================================================
    // CLICK
    // =====================================================

    public void OnEntryClicked()
    {
        if (mData == null)
        {
            Debug.LogError(
                "StoreBuyCardBackScript: mData is NULL."
            );

            return;
        }

        if (StoreScreenController.Instance == null)
        {
            Debug.LogError(
                "StoreBuyCardBackScript: " +
                "StoreScreenController.Instance is NULL."
            );

            return;
        }

        // =====================================================
        // BUY LIMIT CHECK
        // =====================================================

        int buyCount =
            StoreScreenController.Instance.GetCardBackBuyCount(
                mData.CardBackID
            );

        const int buyLimit = 1;

        Debug.Log(
            "[CardBack UI] Click: " +
            mData.CardBackID +
            " BuyCount: " +
            buyCount +
            "/" +
            buyLimit
        );

        if (buyCount >= buyLimit)
        {
            Debug.LogWarning(
                "[CardBack Store] Buy limit reached: " +
                mData.CardBackID +
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

            RefreshBuyCount();

            return;
        }

        // =====================================================
        // BUY
        // =====================================================

        StoreScreenController.Instance.OnClickBuyCardBack(
            mData
        );
    }
}