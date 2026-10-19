public static class TradeErrorText
{
	public const string Generic = "Couldn't reach the trade server. Check your connection and try again.";

	private static string SentenceCase(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return "Couldn't reach the trade server. Check your connection and try again.";
		}
		text = char.ToUpperInvariant(text[0]) + text.Substring(1);
		char c = text[text.Length - 1];
		if (c != '.' && c != '!' && c != '?')
		{
			return text + ".";
		}
		return text;
	}

	public static string Friendly(string raw)
	{
		if (string.IsNullOrEmpty(raw))
		{
			return "Couldn't reach the trade server. Check your connection and try again.";
		}
		string text = raw.ToLowerInvariant();
		if (text.Contains("device is not linked"))
		{
			return "This account is locked to another device. Open Account > Device Lock to move it here.";
		}
		if (text.Contains("update the game") || text.Contains("update_required"))
		{
			return "Please update the game to keep trading.";
		}
		if (text.Contains("network error"))
		{
			return "Can't reach the server. Check your internet connection and try again.";
		}
		if (text.Contains("enough trade coins"))
		{
			return "You don't have enough Trade Coins.";
		}
		if (text.Contains("not enough coins"))
		{
			return "You don't have enough coins.";
		}
		if (text.Contains("not enough gems"))
		{
			return "You don't have enough gems.";
		}
		if (text.Contains("your inventory is full"))
		{
			return "Your inventory is full.";
		}
		if (text.Contains("their inventory is full"))
		{
			return "That player's inventory is full.";
		}
		if (text.Contains("listing not available"))
		{
			return "That listing is no longer available.";
		}
		if (text.Contains("offer not available"))
		{
			return "That offer is no longer available.";
		}
		if (text.Contains("cannot buy your own"))
		{
			return "You can't buy your own listing.";
		}
		if (text.Contains("not your listing"))
		{
			return "That listing is not yours.";
		}
		if (text.Contains("not your offer"))
		{
			return "That offer is not yours.";
		}
		if (text.Contains("not in your inventory") || text.Contains("you no longer have creature"))
		{
			return "You no longer have that creature.";
		}
		if (text.Contains("player not found") || text.Contains("buyer not found"))
		{
			return "That player could not be found.";
		}
		if (text.Contains("offer something"))
		{
			return "Add a creature or some coins to your offer first.";
		}
		if (text.Contains("invalid creature"))
		{
			return "That creature can't be traded.";
		}
		if (text.Contains("escrow missing"))
		{
			return "This offer's items are no longer being held. It may have been cancelled.";
		}
		if (text.Contains("coins must be >= 0") || text.Contains("integer coins required") || text.Contains("integer gems required") || text.Contains("unique_id and integer price required") || text.Contains("gems>0 required") || text.Contains("price>0 required"))
		{
			return "Please enter a valid amount.";
		}
		if (text.Contains("required") || text.Contains("distinct to_id") || text.Contains("currency must be"))
		{
			return "That request was incomplete. Please try again.";
		}
		if (text.StartsWith("internal:") || text.Contains("server_error"))
		{
			return "The trade service hit a problem. Please try again in a moment.";
		}
		return SentenceCase(raw.Trim());
	}
}
