using System.Collections.Generic;

public class CurrencyPackageData : ILoadableData
{
	private string _ID;

	private string _DisplayName;

	private string _UITexture;

	private int _PaidHardCurrency;

	private int _FreeHardCurrency;

	private int _SocialCurrency;

	private int _SoftCurrency;

	private bool _ShowInStore;

	public string ID => _ID;

	public string DisplayName => _DisplayName;

	public string UITexture => _UITexture;

	public int PaidHardCurrency => _PaidHardCurrency;

	public int FreeHardCurrency => _FreeHardCurrency;

	public int TotalHardCurrency => _PaidHardCurrency + _FreeHardCurrency;

	public int SocialCurrency => _SocialCurrency;

	public int SoftCurrency => _SoftCurrency;

	public bool ShowInStore => _ShowInStore;

	public int VFXIndex { get; private set; }

	public int CustomizationCurrency { get; private set; }

	public int EvolutionShards { get; private set; }

	public void Populate(Dictionary<string, object> dict)
	{
		_ID = TFUtils.LoadString(dict, "ProductID", string.Empty);
		_DisplayName = TFUtils.LoadString(dict, "DisplayName", string.Empty);
		_UITexture = TFUtils.LoadString(dict, "UITexture", string.Empty);
		_PaidHardCurrency = TFUtils.LoadInt(dict, "PaidHardCurrency", 0);
		_FreeHardCurrency = TFUtils.LoadInt(dict, "FreeHardCurrency", 0);
		_SocialCurrency = TFUtils.LoadInt(dict, "SocialCurrency", 0);
		_SoftCurrency = TFUtils.LoadInt(dict, "SoftCurrency", 0);
		_ShowInStore = TFUtils.LoadBool(dict, "ShowInStore", defaultValue: false);
		VFXIndex = TFUtils.LoadInt(dict, "VFXIndex", -1);
		CustomizationCurrency = TFUtils.LoadInt(dict, "CustomizationCurrency", 0);
		EvolutionShards = TFUtils.LoadInt(dict, "EvolutionShards", 0);
	}
}
