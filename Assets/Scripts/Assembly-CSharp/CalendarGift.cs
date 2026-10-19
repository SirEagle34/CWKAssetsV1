using System;
using System.Collections.Generic;

public class CalendarGift
{
	public CalendarGiftType GiftType { get; private set; }

	public int Quantity { get; private set; }

	public string GiftID { get; private set; }

	public string Icon { get; private set; }

	public string Texture { get; private set; }

	public bool Repeating { get; private set; }

	public string CalendarID { get; set; }

	public void Populate(Dictionary<string, object> dict)
	{
		Quantity = TFUtils.LoadInt(dict, "Quantity", 0);
		GiftID = TFUtils.LoadString(dict, "GiftID", string.Empty);
		Icon = TFUtils.LoadString(dict, "Sprite", null);
		Texture = TFUtils.LoadString(dict, "Texture", null);
		Repeating = TFUtils.LoadBool(dict, "Repeating", defaultValue: false);
		string value = TFUtils.LoadString(dict, "Type", string.Empty);
		GiftType = (CalendarGiftType)(int)Enum.Parse(typeof(CalendarGiftType), value);
	}

	public string Grant(out InventorySlotItem slotItem, out CardBackData cardBack)
	{
		PlayerSaveData.BeginGrant();
		string result = GrantLocal(out slotItem, out cardBack);
		string text = PlayerSaveData.EndGrant();
		if (text.Length > 0 || GiftType == CalendarGiftType.CardBack)
		{
			string items = ((GiftType == CalendarGiftType.CardBack && cardBack != null) ? ("CB:" + cardBack.ID) : text);
			Singleton<PlayerInfoScript>.Instance.SaveData.ClaimCurrency("calendar reward", CalendarID, 0, 0, 0, 0, null, items);
		}
		return result;
	}

	private string GrantLocal(out InventorySlotItem slotItem, out CardBackData cardBack)
	{
		slotItem = null;
		cardBack = null;
		PlayerSaveData saveData = Singleton<PlayerInfoScript>.Instance.SaveData;
		if (GiftType == CalendarGiftType.StaminaPvE)
		{
			DetachedSingleton<StaminaManager>.Instance.AddExtraStamina(StaminaType.Quests, Quantity);
			return Quantity + " " + KFFLocalization.Get("!!BATTLE_ENERGY");
		}
		if (GiftType == CalendarGiftType.StaminaPvP)
		{
			DetachedSingleton<StaminaManager>.Instance.AddExtraStamina(StaminaType.Pvp, Quantity);
			return Quantity + " " + KFFLocalization.Get("!!PVP_ENERGY");
		}
		if (GiftType == CalendarGiftType.HardCurrency)
		{
			saveData.ClaimCurrency("calendar reward", CalendarID, 0, Quantity, 0, 0);
			if (Quantity == 1)
			{
				return "1 " + KFFLocalization.Get("!!CURRENCY_HARD_1");
			}
			return Quantity + " " + KFFLocalization.Get("!!CURRENCY_HARD");
		}
		if (GiftType == CalendarGiftType.SoftCurrency)
		{
			saveData.ClaimCurrency("calendar reward", CalendarID, Quantity, 0, 0, 0);
			return Quantity + " " + KFFLocalization.Get("!!CURRENCY_SOFT");
		}
		if (GiftType == CalendarGiftType.SocialCurrency)
		{
			saveData.ClaimCurrency("calendar reward", CalendarID, 0, 0, Quantity, 0);
			return Quantity + " " + KFFLocalization.Get("!!CURRENCY_PVP");
		}
		if (GiftType == CalendarGiftType.Runes)
		{
			EvoMaterialData data = EvoMaterialDataManager.Instance.GetData(GiftID);
			for (int i = 0; i < Quantity; i++)
			{
				slotItem = saveData.AddEvoMaterial(data);
			}
			return Quantity + "x " + data.Name;
		}
		if (GiftType == CalendarGiftType.XPMaterials)
		{
			XPMaterialData data2 = XPMaterialDataManager.Instance.GetData(GiftID);
			for (int j = 0; j < Quantity; j++)
			{
				slotItem = saveData.AddXPMaterial(data2);
			}
			return Quantity + "x " + data2.Name;
		}
		if (GiftType == CalendarGiftType.CardBack)
		{
			cardBack = CardBackDataManager.Instance.GetData(GiftID);
			if (!saveData.UnlockedCardBacks.Contains(cardBack))
			{
				saveData.UnlockedCardBacks.Add(cardBack);
			}
			return cardBack.Name;
		}
		if (GiftType == CalendarGiftType.CreatureTable)
		{
			CreatureItem creatureItem = new CreatureItem(GachaWeightDataManager.Instance.GetData(GiftID).Spin().DropID);
			slotItem = Singleton<PlayerInfoScript>.Instance.SaveData.AddCreature(creatureItem);
			return creatureItem.Form.Name;
		}
		return null;
	}
}
