using System.Collections.Generic;
using UnityEngine;

public class StoreBuyActionCardData : ILoadableData
{
    private string _ID;
    private string _CardID;
    private int _BuyCost;
    private int _Quantity;
    private bool _IsSpecial;

    private CardData _Card;

    public string ID
    {
        get
        {
            return _ID;
        }
    }

    public string CardID
    {
        get
        {
            return _CardID;
        }
    }

    public CardData Card
    {
        get
        {
            return _Card;
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

    public bool IsSpecial
    {
        get
        {
            return _IsSpecial;
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

        _CardID =
            TFUtils.LoadString(
                dict,
                "CardID",
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

        _IsSpecial =
            TFUtils.LoadBool(
                dict,
                "IsSpecial",
                false
            );

        // =====================================================
        // CARD
        // =====================================================

        _Card =
            CardDataManager.Instance.GetData(
                _CardID
            );

        if (_Card == null)
        {
            Debug.LogError(
                "StoreBuyActionCardData: " +
                "Card not found. " +
                "CardID: " +
                _CardID
            );

            return;
        }

        Debug.Log(
            "StoreBuyActionCardData LOADED | " +
            "ID: " +
            _ID +
            " | CardID: " +
            _CardID +
            " | CardName: " +
            _Card.Name +
            " | Quantity: " +
            _Quantity +
            " | BuyCost: " +
            _BuyCost +
            " | IsSpecial: " +
            _IsSpecial
        );
    }
}