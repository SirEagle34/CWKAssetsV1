using System.Collections.Generic;
using UnityEngine;

public class StoreBuyCreatureData : ILoadableData
{
    private string _ID;
    private string _CreatureID;
    private int _BuyCost;
    private int _Quantity;

    private CreatureData _Creature;

    public string ID
    {
        get
        {
            return _ID;
        }
    }

    public string CreatureID
    {
        get
        {
            return _CreatureID;
        }
    }

    public CreatureData Creature
    {
        get
        {
            return _Creature;
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

        _CreatureID =
            TFUtils.LoadString(
                dict,
                "CreatureID",
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
        // CREATURE
        // =====================================================

        _Creature =
            CreatureDataManager.Instance.GetData(
                _CreatureID
            );

        if (_Creature == null)
        {
            Debug.LogError(
                "StoreBuyCreatureData: " +
                "Creature not found. " +
                "CreatureID: " +
                _CreatureID
            );

            return;
        }

        Debug.Log(
            "StoreBuyCreatureData LOADED | " +
            "ID: " +
            _ID +
            " | CreatureID: " +
            _CreatureID +
            " | CreatureName: " +
            _Creature.Name +
            " | Quantity: " +
            _Quantity +
            " | BuyCost: " +
            _BuyCost
        );
    }
}