using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class RevivalLabelLocalizer
{
	private static readonly Dictionary<string, string> Keys = new Dictionary<string, string>
	{
		{ "Daily Spin Wheel", "!!CWK_DAILY_SPIN" },
		{ "Craft Cards", "!!CWK_CRAFT_CARDS" },
		{ "Join Discord", "!!CWK_JOIN_DISCORD" },
		{ "TRADE", "!!CWK_TRADE" },
		{ "News", "!!NEWS" }
	};

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
	private static void Hook()
	{
		SceneManager.sceneLoaded += delegate
		{
			UILabel[] array = Resources.FindObjectsOfTypeAll<UILabel>();
			foreach (UILabel uILabel in array)
			{
				if (uILabel != null && uILabel.gameObject.scene.IsValid() && uILabel.text != null && Keys.TryGetValue(uILabel.text.Trim(), out var value))
				{
					string text = KFFLocalization.Get(value);
					if (!string.IsNullOrEmpty(text) && !text.StartsWith("MISSING LANG:"))
					{
						uILabel.text = text;
					}
				}
			}
		};
	}
}
