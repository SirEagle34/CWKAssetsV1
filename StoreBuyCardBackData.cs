using System.Collections.Generic;
using UnityEngine;

public class StoreBuyCardBackData : ILoadableData
{
    private string _ID;
    private string _CardBackID;
    private int _BuyCost;
    private int _Quantity;

    private CardBackData _CardBack;

    public string ID
    {
        get
        {
            return _ID;
        }
    }

    public string CardBackID
    {
        get
        {
            return _CardBackID;
        }
    }

    public CardBackData CardBack
    {
        get
        {
            return _CardBack;
        }
    }

    public int BuyCost
    {
        get
        {
            return _BuyCost;
        }
    }

    public int Quantity
    {
        get
        {
            return _Quantity;
        }
    }

    public void Populate(
        Dictionary<string, object> dict
    )
    {
        _ID =
            TFUtils.LoadString(
                dict,
                "ID",
                string.Empty
            );

        _CardBackID =
            TFUtils.LoadString(
                dict,
                "CardBackID",
                string.Empty
            );

        _BuyCost =
            TFUtils.LoadInt(
                dict,
                "BuyCost",
                0
            );

        _Quantity =
            TFUtils.LoadInt(
                dict,
                "Quantity",
                1
            );

        // =====================================================
        // CARD BACK
        // =====================================================

        _CardBack =
            CardBackDataManager.Instance.GetData(
                _CardBackID
            );

        if (_CardBack == null)
        {
            Debug.LogError(
                "StoreBuyCardBackData: " +
                "CardBack not found. " +
                "CardBackID: " +
                _CardBackID
            );

            return;
        }

        Debug.Log(
            "StoreBuyCardBackData LOADED | " +
            "ID: " +
            _ID +
            " | CardBackID: " +
            _CardBackID +
            " | Quantity: " +
            _Quantity +
            " | BuyCost: " +
            _BuyCost
        );
    }
}