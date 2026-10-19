using System;
using System.Collections.Generic;
using System.Globalization;

public class ChangeLogData : ILoadableData
{
    public string ID { get; private set; }
    public string Version { get; private set; }
    public DateTime Date { get; private set; }

    public string Title { get; private set; }

    public string Line1 { get; private set; }
    public string Line2 { get; private set; }
    public string Line3 { get; private set; }
    public string Line4 { get; private set; }
    public string Line5 { get; private set; }
    public string Line6 { get; private set; }
    public string Line7 { get; private set; }

    public void Populate(Dictionary<string, object> dict)
    {
        ID = TFUtils.LoadString(dict, "ID", string.Empty);
        Version = TFUtils.LoadString(dict, "Version", string.Empty);
        Title = TFUtils.LoadLocalizedString(dict, "Title", string.Empty);

        Line1 = TFUtils.LoadLocalizedString(dict, "Line1", string.Empty);
        Line2 = TFUtils.LoadLocalizedString(dict, "Line2", string.Empty);
        Line3 = TFUtils.LoadLocalizedString(dict, "Line3", string.Empty);
        Line4 = TFUtils.LoadLocalizedString(dict, "Line4", string.Empty);
        Line5 = TFUtils.LoadLocalizedString(dict, "Line5", string.Empty);
        Line6 = TFUtils.LoadLocalizedString(dict, "Line6", string.Empty);
        Line7 = TFUtils.LoadLocalizedString(dict, "Line7", string.Empty);
    }
}
