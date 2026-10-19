using System;
using System.Collections.Generic;
using UnityEngine;

public static class PvpExtremeTeam
{
	private const int FirstUniqueId = 900000001;

	private static Loadout sTeam;

	private const string HeroPref = "ExtremeHero";

	public static int ForceIndex = -1;

	public static bool IsActive => Singleton<PlayerInfoScript>.Instance.PvPData?.ExtremeMode ?? false;

	public static Loadout Current
	{
		get
		{
			if (sTeam == null)
			{
				sTeam = Build();
			}
			return sTeam;
		}
	}

	public static void Reset()
	{
		sTeam = null;
	}

	private static LeaderItem ChosenHero()
	{
		string id = PlayerPrefs.GetString("ExtremeHero", string.Empty);
		if (string.IsNullOrEmpty(id))
		{
			return null;
		}
		return Singleton<PlayerInfoScript>.Instance.SaveData.Leaders.Find((LeaderItem l) => l != null && l.Form != null && l.Form.ID == id);
	}

	public static void CycleHero(int step)
	{
		List<LeaderItem> list = Singleton<PlayerInfoScript>.Instance.SaveData.Leaders.FindAll((LeaderItem l) => l != null && l.Form != null);
		if (list.Count != 0 && Current != null)
		{
			int num = list.FindIndex((LeaderItem l) => l.Form == Current.Leader.Form);
			num = ((num >= 0) ? ((num + step + list.Count) % list.Count) : ((step <= 0) ? (list.Count - 1) : 0));
			PlayerPrefs.SetString("ExtremeHero", list[num].Form.ID);
			sTeam.Leader = list[num];
		}
	}

	public static int ActiveIndex()
	{
		List<PvpExtremeTeamData> database = PvpExtremeTeamDataManager.Instance.GetDatabase();
		if (database.Count == 0)
		{
			return -1;
		}
		if (ForceIndex >= 0)
		{
			return ForceIndex % database.Count;
		}
		int num = Mathf.Max(1, database[0].RotateDays);
		return (int)((long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalDays / num % database.Count);
	}

	private static Loadout Build()
	{
		int num = ActiveIndex();
		PvpExtremeTeamData pvpExtremeTeamData = ((num >= 0) ? PvpExtremeTeamDataManager.Instance.GetDatabase()[num] : null);
		if (pvpExtremeTeamData == null || pvpExtremeTeamData.CreatureIDs.Count == 0)
		{
			Debug.LogError("[Extreme] db_PVPExtremeList has no team");
			return null;
		}
		LeaderData data = LeaderDataManager.Instance.GetData(pvpExtremeTeamData.HeroID);
		if (data == null)
		{
			Debug.LogError("[Extreme] unknown HeroID " + pvpExtremeTeamData.HeroID);
			return null;
		}
		Loadout loadout = new Loadout();
		loadout.Leader = ChosenHero() ?? new LeaderItem(data);
		loadout.CreatureSet = new List<InventorySlotItem>();
		for (int i = 0; i < pvpExtremeTeamData.CreatureIDs.Count; i++)
		{
			CreatureData data2 = CreatureDataManager.Instance.GetData(pvpExtremeTeamData.CreatureIDs[i]);
			if (data2 == null)
			{
				Debug.LogError("[Extreme] unknown creature " + pvpExtremeTeamData.CreatureIDs[i]);
				continue;
			}
			CreatureItem creatureItem = new CreatureItem(data2);
			creatureItem.SetPresetUniqueId(900000001 + i);
			creatureItem.StarRating = Mathf.Clamp(pvpExtremeTeamData.StarRating, 1, 5);
			int level = ((pvpExtremeTeamData.Level > 0) ? Mathf.Min(pvpExtremeTeamData.Level, creatureItem.MaxLevel) : creatureItem.MaxLevel);
			creatureItem.Xp = creatureItem.XPTable.GetXpToReachLevel(level);
			creatureItem.PassiveSkillLevel = pvpExtremeTeamData.PassiveLevel;
			loadout.CreatureSet.Add(new InventorySlotItem(creatureItem));
		}
		if (loadout.CreatureSet.Count <= 0)
		{
			return null;
		}
		return loadout;
	}
}
