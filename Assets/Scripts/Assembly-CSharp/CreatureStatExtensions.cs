public static class CreatureStatExtensions
{
	public static string DisplayName(this CreatureStat stat)
	{
		return stat switch
		{
			CreatureStat.STR => KFFLocalization.Get("!!STR"), 
			CreatureStat.INT => KFFLocalization.Get("!!INT"), 
			CreatureStat.DEX => KFFLocalization.Get("!!DEX"), 
			CreatureStat.DEF => KFFLocalization.Get("!!DEF"), 
			CreatureStat.RES => KFFLocalization.Get("!!RES"), 
			CreatureStat.HP => KFFLocalization.Get("!!HP"), 
			_ => string.Empty, 
		};
	}

	public static bool IsPercent(this CreatureStat stat)
	{
		if (stat != CreatureStat.DEX && stat != CreatureStat.DEF)
		{
			return stat == CreatureStat.RES;
		}
		return true;
	}
}
