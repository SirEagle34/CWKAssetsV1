using UnityEngine;

public static class CreatureFactionExtensions
{
	public static string IconTexture(this CreatureFaction faction)
	{
		return faction switch
		{
			CreatureFaction.Red => "Icon_Faction_Red", 
			CreatureFaction.Green => "Icon_Faction_Green", 
			CreatureFaction.Blue => "Icon_Faction_Blue", 
			CreatureFaction.Dark => "Icon_Faction_Dark", 
			CreatureFaction.Light => "Icon_Faction_Light", 
			CreatureFaction.White => "Icon_Faction_White", 
			CreatureFaction.Orange => "Icon_Faction_Orange", 
			CreatureFaction.Yellow => "Icon_Faction_Yellow", 
			CreatureFaction.Black => "Icon_Faction_Black", 
			CreatureFaction.Purple => "Icon_Faction_Purple", 
			CreatureFaction.Colorless => "Icon_Faction_Rainbow", 
			_ => string.Empty, 
		};
	}

	public static string CardFrameTexture(this CreatureFaction faction)
	{
		return faction switch
		{
			CreatureFaction.Red => "UI/CardFrames/UI_ActionCard_Frame_Corn_Landscape", 
			CreatureFaction.Green => "UI/CardFrames/UI_ActionCard_Frame_Sand_Landscape", 
			CreatureFaction.Blue => "UI/CardFrames/UI_ActionCard_Plains_Landscape", 
			CreatureFaction.Dark => "UI/CardFrames/UI_ActionCard_Frame_Swamp_Landscape", 
			CreatureFaction.Light => "UI/CardFrames/UI_ActionCard_Frame_Nicelands_Landscape", 
			CreatureFaction.White => "UI/CardFrames/UI_ActionCard_Frame_Icylands_Landscape", 
			CreatureFaction.Orange => "UI/CardFrames/UI_ActionCard_Frame_Magma_Landscape", 
			CreatureFaction.Yellow => "UI/CardFrames/UI_ActionCard_Frame_Building", 
			CreatureFaction.Black => "UI/CardFrames/UI_ObsidianCard_Frame", 
			CreatureFaction.Purple => "UI/CardFrames/UI_ActionCard_Frame_Spooky_Landscape", 
			CreatureFaction.Colorless => "UI/CardFrames/UI_ActionCard_Frame_Rainbow", 
			_ => string.Empty, 
		};
	}

	public static string CreatureFrameTexture(this CreatureFaction faction)
	{
		return faction switch
		{
			CreatureFaction.Red => "UI/CardFrames/UI_CreatureCard_Frame_Corn", 
			CreatureFaction.Green => "UI/CardFrames/UI_CreatureCard_Frame_Sand", 
			CreatureFaction.Blue => "UI/CardFrames/UI_CreatureCard_Frame_Plains", 
			CreatureFaction.Dark => "UI/CardFrames/UI_CreatureCard_Frame_Swamp", 
			CreatureFaction.Light => "UI/CardFrames/UI_CreatureCard_Frame_Nicelands", 
			CreatureFaction.White => "UI/CardFrames/UI_CreatureCard_Frame_Icylands", 
			CreatureFaction.Orange => "UI/CardFrames/UI_CreatureCard_Frame_Magma", 
			CreatureFaction.Yellow => "UI/CardFrames/UI_ActionCard_Frame_Building", 
			CreatureFaction.Black => "UI/CardFrames/UI_ObsidianCard_Frame", 
			CreatureFaction.Purple => "UI/CardFrames/UI_CreatureCard_Frame_Spooky", 
			CreatureFaction.Colorless => "UI/CardFrames/UI_ActionCard_Frame_Rainbow", 
			_ => string.Empty, 
		};
	}

	public static string CreatureShardFrameSpriteName(this CreatureFaction faction)
	{
		return faction switch
		{
			CreatureFaction.Red => "Shard_Frame_Corn", 
			CreatureFaction.Green => "Shard_Frame_Sand", 
			CreatureFaction.Blue => "Shard_Frame_Plains", 
			CreatureFaction.Dark => "Shard_Frame_Swamp", 
			CreatureFaction.Light => "Shard_Frame_Nicelands", 
			CreatureFaction.White => "Shard_Frame_Icylands", 
			CreatureFaction.Orange => "Shard_Frame_Magmalands", 
			CreatureFaction.Yellow => "Frame_Rune", 
			CreatureFaction.Black => "Frame_Rune", 
			CreatureFaction.Purple => "Shard_Frame_Spookylands", 
			CreatureFaction.Colorless => "Frame_Rune", 
			_ => string.Empty, 
		};
	}

	public static string CreaturePortraitFrameSpriteName(this CreatureFaction faction)
	{
		return faction switch
		{
			CreatureFaction.Red => "Frame_Creature_Red", 
			CreatureFaction.Green => "Frame_Creature_Green", 
			CreatureFaction.Blue => "Frame_Creature_Blue", 
			CreatureFaction.Dark => "Frame_Creature_Dark", 
			CreatureFaction.Light => "Frame_Creature_Light", 
			CreatureFaction.White => "Frame_Creature_White", 
			CreatureFaction.Orange => "Frame_Creature_Orange", 
			CreatureFaction.Yellow => "Frame_Creature_Yellow", 
			CreatureFaction.Black => "Frame_Creature_Black", 
			CreatureFaction.Purple => "Frame_Creature_Purple", 
			CreatureFaction.Colorless => "Frame_Creature_Rainbow", 
			_ => string.Empty, 
		};
	}

	public static string CakeFrameSpriteName(this CreatureFaction faction)
	{
		return faction switch
		{
			CreatureFaction.Red => "UI_CakeCard_Frame_Corn", 
			CreatureFaction.Green => "UI_CakeCard_Frame_Sand", 
			CreatureFaction.Blue => "UI_CakeCard_Frame_Plains", 
			CreatureFaction.Dark => "UI_CakeCard_Frame_Swamp", 
			CreatureFaction.Light => "UI_CakeCard_Frame_Nicelands", 
			CreatureFaction.White => "UI_CakeCard_Frame_Icylands", 
			CreatureFaction.Orange => "UI_CakeCard_Frame_Magmalands", 
			CreatureFaction.Yellow => "UI_CakeCard_Frame_Rainbow", 
			CreatureFaction.Black => "UI_CakeCard_Frame_Rainbow", 
			CreatureFaction.Purple => "UI_CakeCard_Frame_Spookylands", 
			CreatureFaction.Colorless => "UI_CakeCard_Frame_Rainbow", 
			_ => string.Empty, 
		};
	}

	public static Color VFXColor(this CreatureFaction faction)
	{
		return Singleton<DWFactionColor>.Instance.FactionColors[(int)faction];
	}

	public static string ComboPipTexture(this CreatureFaction faction)
	{
		return faction switch
		{
			CreatureFaction.Red => "Icon_Faction_Red", 
			CreatureFaction.Green => "Icon_Faction_Green", 
			CreatureFaction.Blue => "Icon_Faction_Blue", 
			CreatureFaction.Dark => "Icon_Faction_Dark", 
			CreatureFaction.Light => "Icon_Faction_Light", 
			CreatureFaction.White => "Icon_Faction_White", 
			CreatureFaction.Orange => "Icon_Faction_Orange", 
			CreatureFaction.Yellow => "Icon_Faction_Yellow", 
			CreatureFaction.Black => "Icon_Faction_Black", 
			CreatureFaction.Purple => "Icon_Faction_Purple", 
			CreatureFaction.Colorless => "Icon_Faction_Rainbow", 
			_ => string.Empty, 
		};
	}

	public static string GetIcon(this CreatureFaction faction)
	{
		return faction switch
		{
			CreatureFaction.Red => "Icon_Card_Red", 
			CreatureFaction.Green => "Icon_Card_Green", 
			CreatureFaction.Blue => "Icon_Card_Blue", 
			CreatureFaction.Dark => "Icon_Card_Dark", 
			CreatureFaction.Light => "Icon_Card_Light", 
			CreatureFaction.White => "Icon_Card_White", 
			CreatureFaction.Orange => "Icon_Card_Orange", 
			CreatureFaction.Yellow => "Icon_Card_Yellow", 
			CreatureFaction.Black => "Icon_Card_Black", 
			CreatureFaction.Purple => "Icon_Card_Purple", 
			CreatureFaction.Colorless => "Icon_Card_Rainbow", 
			_ => string.Empty, 
		};
	}

	public static string ClassName(this CreatureFaction faction)
	{
		return faction switch
		{
			CreatureFaction.Red => "Fighter", 
			CreatureFaction.Green => "Guard", 
			CreatureFaction.Blue => "Mystic", 
			CreatureFaction.Dark => "Utility", 
			CreatureFaction.Light => "Recovery", 
			CreatureFaction.White => "Coldsoldier", 
			CreatureFaction.Orange => "Shooter", 
			CreatureFaction.Yellow => "Building", 
			CreatureFaction.Black => "Joker", 
			CreatureFaction.Purple => "Warrior", 
			CreatureFaction.Colorless => "Special", 
			_ => string.Empty, 
		};
	}

	public static string ClassDisplayName(this CreatureFaction faction)
	{
		return faction switch
		{
			CreatureFaction.Red => KFFLocalization.Get("!!FACTION_RED"), 
			CreatureFaction.Green => KFFLocalization.Get("!!FACTION_GREEN"), 
			CreatureFaction.Blue => KFFLocalization.Get("!!FACTION_BLUE"), 
			CreatureFaction.Dark => KFFLocalization.Get("!!FACTION_PURPLE"), 
			CreatureFaction.Light => KFFLocalization.Get("!!FACTION_YELLOW"), 
			CreatureFaction.White => KFFLocalization.Get("!!FACTION_WHITE"), 
			CreatureFaction.Orange => KFFLocalization.Get("!!FACTION_ORANGE"), 
			CreatureFaction.Yellow => KFFLocalization.Get("!!FACTION_BROWN"), 
			CreatureFaction.Black => KFFLocalization.Get("!!FACTION_BLACK"), 
			CreatureFaction.Purple => KFFLocalization.Get("Spookylands"), 
			CreatureFaction.Colorless => KFFLocalization.Get("!!FACTION_RAINBOW"), 
			_ => string.Empty, 
		};
	}

	public static CreatureFaction RandomFaction()
	{
		return (CreatureFaction)Random.Range(1, 6);
	}

	public static string CDubsName(this CreatureFaction faction)
	{
		return faction switch
		{
			CreatureFaction.Red => "Corn", 
			CreatureFaction.Green => "Sand", 
			CreatureFaction.Blue => "Plains", 
			CreatureFaction.Dark => "Swamp", 
			CreatureFaction.Light => "Nicelands", 
			CreatureFaction.White => "Icylands", 
			CreatureFaction.Orange => "Magmalands", 
			CreatureFaction.Yellow => "Buildings", 
			CreatureFaction.Black => "Obsidian", 
			CreatureFaction.Purple => "Spookylands", 
			CreatureFaction.Colorless => "Special", 
			_ => string.Empty, 
		};
	}
}
