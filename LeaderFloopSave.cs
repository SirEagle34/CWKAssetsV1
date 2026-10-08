using System;
using System.Collections.Generic;

[Serializable]
public class LeaderFloopSave
{
	public const int EQUIPPED_COUNT = 5;
	public const int RESERVE_COUNT = 3;

	public string LeaderID;

	public string[] Equipped = new string[EQUIPPED_COUNT];

	public string[] Reserve = new string[RESERVE_COUNT];

	public LeaderFloopSave()
	{
	}

	public LeaderFloopSave(string leaderID)
	{
		LeaderID = leaderID;
	}
}