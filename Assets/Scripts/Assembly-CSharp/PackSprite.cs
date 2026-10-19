using System.Text.RegularExpressions;
using UnityEngine;

public static class PackSprite
{
	public const string Folder = "UI/Sprites";

	public static void Apply(UISprite sprite, string name, string folder = "UI/Sprites")
	{
		if (sprite == null)
		{
			return;
		}
		sprite.spriteName = name;
		Transform transform = sprite.transform.Find("PackIcon");
		UITexture uITexture = ((transform != null) ? transform.GetComponent<UITexture>() : null);
		if (string.IsNullOrEmpty(name) || sprite.atlas == null || sprite.atlas.GetSprite(name) != null)
		{
			Hide(uITexture);
			return;
		}
		Texture texture = LiveContent.TryLoad(folder + "/" + name, typeof(Texture2D)) as Texture;
		if (texture != null)
		{
			if (uITexture == null)
			{
				uITexture = NGUITools.AddWidget<UITexture>(sprite.gameObject);
				uITexture.name = "PackIcon";
			}
			uITexture.mainTexture = texture;
			uITexture.width = sprite.width;
			uITexture.height = sprite.height;
			uITexture.depth = sprite.depth + 1;
			uITexture.enabled = true;
		}
		else
		{
			Hide(uITexture);
			string text = Regex.Replace(name, "_v\\d+$", string.Empty);
			if (text != name && sprite.atlas.GetSprite(text) != null)
			{
				sprite.spriteName = text;
			}
		}
	}

	private static void Hide(UITexture overlay)
	{
		if (overlay != null)
		{
			overlay.enabled = false;
		}
	}
}
