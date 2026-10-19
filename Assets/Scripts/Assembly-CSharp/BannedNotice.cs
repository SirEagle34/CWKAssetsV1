using System;
using UnityEngine;

public class BannedNotice : MonoBehaviour
{
	private static readonly string[] LINES = new string[9] { "Nice try, skiddy. LOL.", "", "You edited your gems. We edited your account.", "Turns out the server does the maths, not your memory editor.", "", "Every gem, every creature, every chest - counted on our side.", "You didn't hack the game. You hacked a number on your screen.", "", "GG. Thanks for the free strike." };

	private static bool mShown;

	private static Font mFont;

	public static void Show()
	{
		if (mShown)
		{
			return;
		}
		mShown = true;
		try
		{
			GameObject obj = new GameObject("BannedNotice");
			obj.AddComponent<BannedNotice>();
			UnityEngine.Object.DontDestroyOnLoad(obj);
			Debug.Log("[anticheat] ban notice displayed");
		}
		catch (Exception ex)
		{
			Debug.LogError("[anticheat] could not show ban notice: " + ex);
		}
	}

	private void OnGUI()
	{
		GUI.depth = -1000;
		Color color = GUI.color;
		GUI.color = new Color(0.02f, 0.02f, 0.04f, 1f);
		GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
		GUI.color = color;
		float num = Mathf.Max(1f, (float)Screen.height / 720f);
		if (mFont == null)
		{
			mFont = Resources.Load<Font>("Font/FREDFREDBURGERARA-REGULAR") ?? Resources.Load<Font>("Font/arial") ?? Font.CreateDynamicFontFromOSFont(new string[3] { "Arial", "Helvetica", "Roboto" }, 24);
		}
		GUIStyle gUIStyle = new GUIStyle(GUI.skin.label);
		gUIStyle.font = mFont;
		gUIStyle.alignment = TextAnchor.MiddleCenter;
		gUIStyle.fontSize = Mathf.RoundToInt(64f * num);
		gUIStyle.fontStyle = FontStyle.Bold;
		gUIStyle.normal.textColor = new Color(1f, 0.25f, 0.25f);
		GUIStyle gUIStyle2 = new GUIStyle(GUI.skin.label);
		gUIStyle2.font = mFont;
		gUIStyle2.alignment = TextAnchor.MiddleCenter;
		gUIStyle2.fontSize = Mathf.RoundToInt(22f * num);
		gUIStyle2.wordWrap = true;
		gUIStyle2.normal.textColor = Color.white;
		float num2 = (float)Screen.height * 0.18f;
		GUI.Label(new Rect(0f, num2, Screen.width, 90f * num), "BANNED", gUIStyle);
		float num3 = num2 + 110f * num;
		for (int i = 0; i < LINES.Length; i++)
		{
			GUI.Label(new Rect((float)Screen.width * 0.1f, num3, (float)Screen.width * 0.8f, 30f * num), LINES[i], gUIStyle2);
			num3 += 30f * num;
		}
	}

	private void Update()
	{
		Time.timeScale = 0f;
	}
}
