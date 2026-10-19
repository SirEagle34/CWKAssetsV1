using System.Collections.Generic;
using UnityEngine;

public class StoreBuyEvoMaterialData : ILoadableData
{
    private string _ID;
    private string _EvoMaterialID;
    private int _BuyCost;
    private int _Quantity;

    private EvoMaterialData _EvoMaterial;

    public string ID
    {
        get
        {
            return _ID;
        }
    }

    public string EvoMaterialID
    {
        get
        {
            return _EvoMaterialID;
        }
    }

    public EvoMaterialData EvoMaterial
    {
        get
        {
            return _EvoMaterial;
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

    public void Populate(Dictionary<string, object> dict)
    {
        _ID = TFUtils.LoadString(
            dict,
            "ID",
            string.Empty
        );

        _EvoMaterialID = TFUtils.LoadString(
            dict,
            "EvoMaterialID",
            string.Empty
        );

        _BuyCost = TFUtils.LoadInt(
            dict,
            "BuyCost",
            0
        );

        _Quantity = TFUtils.LoadInt(
            dict,
            "Quantity",
            1
        );

        _EvoMaterial =
            EvoMaterialDataManager.Instance.GetData(
                _EvoMaterialID
            );

        if (_EvoMaterial == null)
        {
            Debug.LogError(
                "StoreBuyEvoMaterialData: " +
                "EvoMaterial not found. ID: " +
                _EvoMaterialID
            );

            return;
        }

        Debug.Log(
            "StoreBuyEvoMaterialData LOADED | " +
            "ID: " + _ID +
            " | EvoMaterialID: " + _EvoMaterialID +
            " | Name: " + _EvoMaterial.Name +
            " | Quantity: " + _Quantity +
            " | BuyCost: " + _BuyCost
        );
    }
}
