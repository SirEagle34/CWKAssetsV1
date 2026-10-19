using System;
using System.Collections.Generic;
using System.Text;

public class SpecialSaleItem
{
	public SpecialSaleData Form { get; private set; }

	public int PurchasedCount { get; set; }
	public DateTime LastPurchaseDate { get; set; }

	public bool ConsumedOnce { get; set; }

	public SpecialSaleItem(Dictionary<string, object> dict)
	{
		Deserialize(dict);
	}

	public SpecialSaleItem(SpecialSaleData data)
	{
		Form = data;
	}

	public string Serialize()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("{");
		stringBuilder.Append(PlayerInfoScript.MakeJS("ID", Form.ID) + ",");
		stringBuilder.Append(PlayerInfoScript.MakeJS("PurchasedCount", PurchasedCount) + ",");
		stringBuilder.Append(PlayerInfoScript.MakeJS("LastPurchaseDate", LastPurchaseDate.ToString()) + ",");
		stringBuilder.Append(PlayerInfoScript.MakeJS("ConsumedOnce", ConsumedOnce) + ",");
		stringBuilder.Append("}");
		return stringBuilder.ToString();
	}

	public void Deserialize(Dictionary<string, object> dict)
	{
		Form = SpecialSaleDataManager.Instance.GetData(
			TFUtils.LoadString(dict, "ID", string.Empty)
		);

		PurchasedCount = TFUtils.LoadInt(dict, "PurchasedCount", 0);

		DateTime result;

		if (DateTime.TryParse(TFUtils.LoadString(dict, "LastPurchaseDate", string.Empty), out result))
			LastPurchaseDate = result;

		ConsumedOnce = TFUtils.LoadBool(dict, "ConsumedOnce", false);
	}

	public bool IsAvailable()
	{
		if (Form == null)
			return false;

		if (Form.Repeatable)
			return true;

		return !ConsumedOnce;
	}
	
	public void OnPurchased()
	{
		PurchasedCount++;
		LastPurchaseDate = TFUtils.ServerTime;

		if (Form != null && Form.Repeatable == false)
			ConsumedOnce = true;
	}
}