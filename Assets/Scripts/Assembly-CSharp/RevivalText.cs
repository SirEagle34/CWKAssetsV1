using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;
using UnityEngine;

public static class RevivalText
{
	private static LanguageCode sBuiltFor = LanguageCode.N;

	private static Dictionary<string, string> sExact = new Dictionary<string, string>();

	private static List<KeyValuePair<string, string>> sFragments = new List<KeyValuePair<string, string>>();

	public static string Translate(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return text;
		}
		LanguageCode languageCode = Language.CurrentLanguage();
		if (languageCode == LanguageCode.EN || languageCode == LanguageCode.N)
		{
			return text;
		}
		if (languageCode != sBuiltFor)
		{
			try
			{
				Build(languageCode);
			}
			catch (Exception)
			{
				return text;
			}
		}
		if (sExact.TryGetValue(text, out var value))
		{
			return value;
		}
		string text2 = text.Trim();
		if (text2 != text && sExact.TryGetValue(text2, out value))
		{
			return text.Replace(text2, value);
		}
		foreach (KeyValuePair<string, string> sFragment in sFragments)
		{
			if (text.StartsWith(sFragment.Key))
			{
				return sFragment.Value + text.Substring(sFragment.Key.Length);
			}
			if (text.EndsWith(sFragment.Key))
			{
				return text.Substring(0, text.Length - sFragment.Key.Length) + sFragment.Value;
			}
		}
		return text;
	}

	private static void Build(LanguageCode lang)
	{
		sBuiltFor = lang;
		sExact = new Dictionary<string, string>();
		sFragments = new List<KeyValuePair<string, string>>();
		TextAsset textAsset = Resources.Load<TextAsset>("Languages/EN_Sheet1");
		if (textAsset != null)
		{
			foreach (Match item in Regex.Matches(textAsset.text, "<entry name=\"(!![^\"]+)\">([^<]+)</entry>"))
			{
				string text = WebUtility.HtmlDecode(item.Groups[2].Value).Trim();
				if (text.Length >= 2 && !sExact.ContainsKey(text))
				{
					string text2 = Language.Get(item.Groups[1].Value);
					if (!string.IsNullOrEmpty(text2) && !text2.StartsWith("MISSING LANG:") && text2 != text)
					{
						sExact[text] = text2;
					}
				}
			}
		}
		TextAsset textAsset2 = Resources.Load<TextAsset>("Languages/RV_" + lang);
		if (textAsset2 == null)
		{
			return;
		}
		string[] array = textAsset2.text.Split('\n');
		foreach (string text3 in array)
		{
			int num = text3.IndexOf('\t');
			if (num <= 0)
			{
				continue;
			}
			string text4 = text3.Substring(0, num).Replace("\\n", "\n");
			string text5 = text3.Substring(num + 1).TrimEnd('\r').Replace("\\n", "\n");
			if (text4.Length != 0 && text5.Length != 0)
			{
				if (lang == LanguageCode.AR)
				{
					text5 = ArabicSupport.Fix(text5, showTashkeel: true, useHinduNumbers: false);
				}
				sExact[text4] = text5;
				if (text4.EndsWith(" ") || text4.EndsWith(":") || text4.EndsWith("\n") || text4.StartsWith("\n") || text4.StartsWith(" "))
				{
					sFragments.Add(new KeyValuePair<string, string>(text4, text5));
				}
			}
		}
		sFragments.Sort((KeyValuePair<string, string> a, KeyValuePair<string, string> b) => b.Key.Length.CompareTo(a.Key.Length));
	}
}
