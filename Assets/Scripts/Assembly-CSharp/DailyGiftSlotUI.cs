using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class DailyGiftSlotUI : MonoBehaviour
{
    public UISprite icon;
    public UILabel valueLabel;
    public UILabel nameLabel;

    public void Bind(DailyRouletteGiftData data)
    {
        nameLabel.text = data.Name;

        // 🔹 ICON (NGUI atlas)
        icon.spriteName = data.Icon;

        // 🔹 VALUE RULE: Val_Item_01 sistemi
        string valueKey = "Val_Item_" + data.Index.ToString("00");

        valueLabel.text = GetValueFromKey(valueKey, data.Quantity);
    }

    private string GetValueFromKey(string key, int fallback)
    {
        // burada ileride table/atlas sistem bağlayabilirsin
        // şimdilik fallback + debug

        UILabel mapped = Resources.FindObjectsOfTypeAll<UILabel>()
            .FirstOrDefault(x => x.name == key);

        if (mapped != null)
            return mapped.text;

        return "x" + fallback;
    }
}