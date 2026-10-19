public static class GameStateExtensions
{
	public static bool IsP1Turn(this GameState state)
	{
		if (state != GameState.P1StartTurn && state != GameState.P1Turn)
		{
			return state == GameState.P1EndTurn;
		}
		return true;
	}

	public static bool IsP2Turn(this GameState state)
	{
		if (state != GameState.P2StartTurn && state != GameState.P2Turn)
		{
			return state == GameState.P2EndTurn;
		}
		return true;
	}
}
