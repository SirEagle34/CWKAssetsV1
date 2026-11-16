using UnityEngine;

public static class CurrencyUI
{
	public const string CraftIconName = "Icon_Currency_Craft";

	private static Texture2D sCraftIcon;

	private static Texture2D sTradeIcon;

	public static Texture2D CraftIcon
	{
		get
		{
			if (sCraftIcon == null)
			{
				sCraftIcon = Resources.Load<Texture2D>("atlases/Icon_Currency_Craft");
			}
			return sCraftIcon;
		}
	}

	public static Texture2D TradeIcon
	{
		get
		{
			if (sTradeIcon == null)
			{
				sTradeIcon = Resources.Load<Texture2D>("atlases/TradeCurrency");
			}
			return sTradeIcon;
		}
	}

	public static string Format(long amount)
	{
		if (amount >= 10000000)
		{
			return ((float)amount / 1000000f).ToString("0.#") + "M";
		}
		return amount.ToString("N0");
	}

	public static void UseCraftIcon(UILabel label)
	{
		if (label == null || label.transform.parent == null || CraftIcon == null)
		{
			return;
		}
		Transform parent = label.transform.parent;
		if (parent.Find("Icon_Currency_Craft") != null)
		{
			return;
		}
		UISprite[] componentsInChildren = parent.GetComponentsInChildren<UISprite>(includeInactive: true);
		foreach (UISprite uISprite in componentsInChildren)
		{
			if (!(uISprite.transform.parent != parent) && !string.IsNullOrEmpty(uISprite.spriteName))
			{
				string spriteName = uISprite.spriteName;
				if (spriteName.IndexOf("Shard") >= 0 || spriteName.IndexOf("Currency") >= 0)
				{
					ReplaceWithCraftIcon(uISprite);
					break;
				}
			}
		}
	}

	public static void RemoveCopiedShadows(GameObject copy)
	{
		Transform[] componentsInChildren = copy.GetComponentsInChildren<Transform>(includeInactive: true);
		foreach (Transform transform in componentsInChildren)
		{
			if (transform != null && transform != copy.transform && transform.name.EndsWith("_shadow"))
			{
				Object.DestroyImmediate(transform.gameObject);
			}
		}
	}

	public static UITexture ReplaceWithCraftIcon(UIWidget icon)
	{
		return ReplaceWithIcon(icon, CraftIcon, CraftIconName);
	}

	public static UITexture ReplaceWithTradeIcon(UIWidget icon)
	{
		return ReplaceWithIcon(icon, TradeIcon, "TradeCurrency");
	}

	private static UITexture ReplaceWithIcon(UIWidget icon, Texture2D texture, string iconName)
	{
		if (icon == null || texture == null)
		{
			if (texture == null)
			{
				Debug.LogError("Unable to load currency icon: " + iconName);
			}
			return null;
		}

		GameObject gameObject = new GameObject(iconName);
		gameObject.layer = icon.gameObject.layer;
		gameObject.transform.parent = icon.transform.parent;
		gameObject.transform.localPosition = icon.transform.localPosition;
		gameObject.transform.localRotation = icon.transform.localRotation;
		gameObject.transform.localScale = icon.transform.localScale;
		UITexture uITexture = gameObject.AddComponent<UITexture>();
		uITexture.mainTexture = texture;
		uITexture.depth = icon.depth;
		int height = (uITexture.width = Mathf.Min(icon.width, icon.height));
		uITexture.height = height;
		icon.enabled = false;
		return uITexture;
	}
}
