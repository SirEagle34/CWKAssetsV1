using System;
using System.Collections.Generic;
using System.Globalization;

public class SpecialSaleData : ILoadableData
{

	public enum SaleMode
	{
		SpecialSale01,
		SpecialSale02,
		SpecialSale03
	}

	public enum CurrencyType
	{
		HardCurrency,
		PvPCurrency
	}

	public class CreatureQuantity
	{
		public CreatureData Data;

		public int Quantity;
	}

	public class CardQuantity
	{
		public CardData Data;

		public int Quantity;
	}

	public class EvoMaterialQuantity
	{
		public EvoMaterialData Data;

		public int Quantity;
	}

	private string PrerequisiteSaleString;

	public string ID { get; private set; }

	public string Name { get; private set; }

	public string Description { get; private set; }

	public string PackTitle { get; private set; }

	public string BadgeText { get; private set; }

	public string Texture { get; private set; }

	public string BadgeTexture { get; private set; }

	public SpecialSaleData PrerequisiteSale { get; private set; }

	public SaleMode Mode { get; private set; }

	public int ShowDuration { get; private set; }

	public int CooldownMin { get; private set; }

	public int CooldownMax { get; private set; }

    public List<StartEndDate> AvailableDates { get; private set; }

	public bool AlwaysAvailable { get; private set; }

	public int Priority { get; private set; }

	public bool Repeatable { get; private set; }

	public int RequiredLevel { get; private set; }

	public bool OneShot { get; private set; }

	public CurrencyType PurchaseCurrency { get; private set; }

	public int CurrencyCost { get; private set; }
	
	public List<GeneralReward> Items { get; private set; }

	public void Populate(Dictionary<string, object> dict)
	{
		ID = TFUtils.LoadString(dict, "ID", string.Empty);
		Name = TFUtils.LoadLocalizedString(dict, "Name", string.Empty);
		Description = TFUtils.LoadLocalizedString(dict, "Description", string.Empty);
		PackTitle = TFUtils.LoadLocalizedString(dict, "PackTitle", string.Empty);
		BadgeText = TFUtils.LoadLocalizedString(dict, "BadgeText", string.Empty);
		Texture = "UI/GeneralBundle/SaleArt/" + TFUtils.LoadString(dict, "Texture", string.Empty);
		BadgeTexture = "UI/GeneralBundle/SaleArt/" + TFUtils.LoadString(dict, "BadgeTexture", string.Empty);
		PrerequisiteSaleString = TFUtils.LoadString(dict, "PrerequisiteSale", null);
		Mode = (SaleMode)TFUtils.LoadInt(dict, "Mode", 0);
		ShowDuration = TFUtils.LoadInt(dict, "ShowDuration", 0);
		CooldownMin = TFUtils.LoadInt(dict, "CooldownMin", 0);
		CooldownMax = TFUtils.LoadInt(dict, "CooldownMax", 0);
		Priority = TFUtils.LoadInt(dict, "Priority", 0);
		Repeatable = TFUtils.LoadBool(dict, "Repeatable", false);
		RequiredLevel = TFUtils.LoadInt(dict, "RequiredLevel", 0);
		OneShot = TFUtils.LoadBool(dict, "OneShot", false);
		PurchaseCurrency =
			(CurrencyType)TFUtils.LoadInt(
				dict,
				"PurchaseCurrency",
				0
			);

		CurrencyCost =
			TFUtils.LoadInt(
				dict,
				"CurrencyCost",
				0
			);

		int num = 1;

		AvailableDates = new List<StartEndDate>();

		while (true)
		{
			string text = TFUtils.LoadString(dict, "SaleStartDate" + num, null);
			string text2 = TFUtils.LoadString(dict, "SaleEndDate" + num, null);

			// ALWAYS AVAILABLE
			if (!string.IsNullOrEmpty(text) &&
				text.ToLower() == "always")
			{
				AlwaysAvailable = true;
				AvailableDates = null;
				break;
			}

			// NO MORE DATES
			if (string.IsNullOrEmpty(text) ||
				string.IsNullOrEmpty(text2))
			{
				break;
			}

			// PARSE START DATE
			bool startValid = DateTime.TryParseExact(
				text,
				new[]
				{
					"M/d/yyyy",
					"d/M/yyyy",
					"dd/MM/yyyy",
					"yyyy-MM-dd",
					"dd.MM.yyyy"
				},
				CultureInfo.InvariantCulture,
				DateTimeStyles.None,
				out DateTime start);

			// PARSE END DATE
			bool endValid = DateTime.TryParseExact(
				text2,
				new[]
				{
					"M/d/yyyy",
					"d/M/yyyy",
					"dd/MM/yyyy",
					"yyyy-MM-dd",
					"dd.MM.yyyy"
				},
				CultureInfo.InvariantCulture,
				DateTimeStyles.None,
				out DateTime end);

			// INVALID DATE
			if (!startValid || !endValid)
			{
				UnityEngine.Debug.LogError(
					"Failed to parse SpecialSaleData dates. " +
					"StartDate: " + text +
					" EndDate: " + text2);

				num++;
				continue;
			}

			StartEndDate startEndDate = new StartEndDate();
			startEndDate.StartDate = start;
			startEndDate.EndDate = end;

			AvailableDates.Add(startEndDate);

			num++;
		}
		Items = GeneralReward.ParseFromJson(dict);
	}

	public void PostLoad()
	{
		if (PrerequisiteSaleString != null)
		{
			PrerequisiteSale = SpecialSaleDataManager.Instance.GetData(PrerequisiteSaleString);
			PrerequisiteSaleString = null;
		}
	}
}
