using System.Collections.Generic;

public class JokerPoolData : ILoadableData
{
	private string _ID;

	public string ID
	{
		get
		{
			return _ID;
		}
	}

	public List<string> Cards = new List<string>();

	public void Populate(Dictionary<string, object> dict)
	{
		_ID = TFUtils.LoadString(dict, "ID", string.Empty);

		object cardsObject;

		if (dict.TryGetValue("Cards", out cardsObject))
		{
			List<object> cardList = cardsObject as List<object>;

			if (cardList != null)
			{
				foreach (object card in cardList)
				{
					if (card != null)
					{
						Cards.Add(card.ToString());
					}
				}
			}
		}
	}
}
