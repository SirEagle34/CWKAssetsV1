public static class PvpModeExtensions
{
	public static bool EnforceBans(this PvpMode mode)
	{
		if (mode != PvpMode.Ranked)
		{
			return mode == PvpMode.Unranked;
		}
		return true;
	}
}
