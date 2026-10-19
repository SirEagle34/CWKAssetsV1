using System;
using System.Collections.Generic;

public class EvoMaterialData : ILoadableData
{
	private string _ID;

	private string _Name;

	private string _UITexture;

	private int _SellPrice;

	private int _Rarity;

	private CreatureFaction _Faction;

	public string ID => _ID;

	public string Name => _Name;

	public string UITexture => _UITexture;

	public int SellPrice => _SellPrice;

	public int Rarity => _Rarity;

	public CreatureFaction Faction => _Faction;

	public string CardFrame { get; private set; }

	public bool AwakenMat { get; private set; }

	public bool ExpeditionDrop { get; private set; }

	public bool AlreadySeen { get; set; }

	public bool AlreadyCollected { get; set; }

	public void Populate(Dictionary<string, object> dict)
	{
		_ID = TFUtils.LoadString(dict, "ID", string.Empty);
		_Name = TFUtils.LoadLocalizedString(dict, "Name", string.Empty);
		_UITexture = TFUtils.LoadString(dict, "UITexture", string.Empty);
		_SellPrice = TFUtils.LoadInt(dict, "SellPrice", 1);
		_Rarity = TFUtils.LoadInt(dict, "Rarity", 1);
		_Faction = (CreatureFaction)(int)Enum.Parse(typeof(CreatureFaction), TFUtils.LoadString(dict, "Faction", string.Empty), ignoreCase: true);
		CardFrame = TFUtils.LoadString(dict, "CardFrame", string.Empty);
		AwakenMat = TFUtils.LoadBool(dict, "AwakenMat", defaultValue: false);
		ExpeditionDrop = TFUtils.LoadBool(dict, "ExpeditionDrop", defaultValue: false);
	}
}
