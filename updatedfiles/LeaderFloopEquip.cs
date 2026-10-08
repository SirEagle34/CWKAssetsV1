using System;
using UnityEngine;

public static class LeaderFloopEquipper
{
	public static void SetFloop(
		LeaderFloopSave save,
		CardData floop,
		int slotIndex)
	{
		if (save == null || floop == null)
			return;

		if (slotIndex < 0 ||
			slotIndex >= LeaderFloopSave.EQUIPPED_COUNT)
			return;

		// Aynı Floop başka equipped slotta varsa kaldır.
		for (int i = 0; i < save.Equipped.Length; i++)
		{
			if (save.Equipped[i] == floop.ID)
				save.Equipped[i] = null;
		}

		// Eğer reserve'deyse reserve'den çıkar.
		for (int i = 0; i < save.Reserve.Length; i++)
		{
			if (save.Reserve[i] == floop.ID)
				save.Reserve[i] = null;
		}

		save.Equipped[slotIndex] = floop.ID;
	}

	public static void ClearFloop(
		LeaderFloopSave save,
		int slotIndex)
	{
		if (save == null)
			return;

		if (slotIndex < 0 ||
			slotIndex >= LeaderFloopSave.EQUIPPED_COUNT)
			return;

		save.Equipped[slotIndex] = null;
	}

	public static void SetReserve(
		LeaderFloopSave save,
		CardData floop,
		int slotIndex)
	{
		if (save == null || floop == null)
			return;

		if (slotIndex < 0 ||
			slotIndex >= LeaderFloopSave.RESERVE_COUNT)
			return;

		// Equipped'den çıkar.
		for (int i = 0; i < save.Equipped.Length; i++)
		{
			if (save.Equipped[i] == floop.ID)
				save.Equipped[i] = null;
		}

		// Reserve'deki eski kopyayı çıkar.
		for (int i = 0; i < save.Reserve.Length; i++)
		{
			if (save.Reserve[i] == floop.ID)
				save.Reserve[i] = null;
		}

		save.Reserve[slotIndex] = floop.ID;
	}

	public static void ClearReserve(
		LeaderFloopSave save,
		int slotIndex)
	{
		if (save == null)
			return;

		if (slotIndex < 0 ||
			slotIndex >= LeaderFloopSave.RESERVE_COUNT)
			return;

		save.Reserve[slotIndex] = null;
	}
}