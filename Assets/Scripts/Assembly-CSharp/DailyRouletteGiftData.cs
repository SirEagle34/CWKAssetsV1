using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class DailyRouletteGiftData : ILoadableData
{
    private string _ID;
    private string _Type;
    private string _Name;
    private string _Icon;
    private string _Pool;

    private int _Quantity;
    private int _Weight;
    private int _Index;
	private int _Rarity;
	public int Rarity => _Rarity;

    public string ID => _ID;
    public string Type => _Type;
    public string Name => _Name;
    public string Icon => _Icon;
    public string Pool => _Pool;

    public int Quantity => _Quantity;
    public int Weight => _Weight;
    public int Index => _Index;

	public void Populate(Dictionary<string, object> dict)
	{
		_Index = TFUtils.LoadInt(dict, "Index");
		_ID = TFUtils.LoadString(dict, "ID", _Index.ToString());

		_Type = TFUtils.LoadString(dict, "Type", string.Empty);
		_Name = TFUtils.LoadString(dict, "Name", string.Empty);
		_Icon = TFUtils.LoadString(dict, "Icon", string.Empty);
		_Pool = TFUtils.LoadString(dict, "Pool", string.Empty);

		_Quantity = TFUtils.LoadInt(dict, "Quantity", 0);

		_Rarity = TFUtils.LoadInt(dict, "Rarity", 1);
        _Rarity = Mathf.Max(1, _Rarity);

        _Weight = Mathf.Max(1, Mathf.RoundToInt(10000f / _Rarity));
	}
}