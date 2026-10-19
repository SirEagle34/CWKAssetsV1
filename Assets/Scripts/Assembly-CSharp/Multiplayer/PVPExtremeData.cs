using System;
using System.Collections.Generic;
using System.Globalization;

public class PVPExtremeData : ILoadableData
{
	public string ID { get; private set; }

	public string ExtremeListLabel { get; private set; }

	public string ExtremeList { get; private set; }

	public string ExtremeList2 { get; private set; }

    public string ExtremeList3 { get; private set; }

	public string ExtremeList4 { get; private set; }

	public string ExtremeList5 { get; private set; }

	public string ExtremeList6 { get; private set; }

	public string ExtremeList7 { get; private set; }

	public void Populate(Dictionary<string, object> dict)
	{
		ID = TFUtils.LoadString(dict, "ID", string.Empty);
		ExtremeListLabel = TFUtils.LoadLocalizedString(dict, "ExtremeListLabel", string.Empty);
		ExtremeList = TFUtils.LoadLocalizedString(dict, "ExtremeList1", string.Empty);
		ExtremeList2 = TFUtils.LoadLocalizedString(dict, "ExtremeList2", string.Empty);
		ExtremeList3 = TFUtils.LoadLocalizedString(dict, "ExtremeList3", string.Empty);
		ExtremeList4 = TFUtils.LoadLocalizedString(dict, "ExtremeList4", string.Empty);
		ExtremeList5 = TFUtils.LoadLocalizedString(dict, "ExtremeList5", string.Empty);
		ExtremeList6 = TFUtils.LoadLocalizedString(dict, "ExtremeList6", string.Empty);
		ExtremeList7 = TFUtils.LoadLocalizedString(dict, "ExtremeList7", string.Empty);
	}

}
