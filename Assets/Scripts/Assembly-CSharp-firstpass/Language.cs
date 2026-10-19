using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

public static class Language
{
	public static string settingsAssetPath = "Assets/Localization/Resources/Languages/LocalizationSettings.asset";

	public static LocalizationSettings settings = (LocalizationSettings)Resources.Load("Languages/" + Path.GetFileNameWithoutExtension(settingsAssetPath), typeof(LocalizationSettings));

	public static string Backup_settingsAssetPath = "Assets/Localization/Resources/Languages/BackupLocalizationSettings.asset";

	public static LocalizationSettings backup_settings = (LocalizationSettings)Resources.Load("Languages/" + Path.GetFileNameWithoutExtension(Backup_settingsAssetPath), typeof(LocalizationSettings));

	private static List<string> availableLanguages;

	private static LanguageCode currentLanguage = LanguageCode.N;

	private static Dictionary<string, Hashtable> currentEntrySheets;

	private static string _persistentDataPath = string.Empty;

	public static Dictionary<string, Dictionary<string, string>> RemoteEntries;

	private static readonly HashSet<string> sReportedMissing = new HashSet<string>();

	public static void Init(string persistentPath)
	{
		_persistentDataPath = persistentPath;
		LoadAvailableLanguages();
		if (settings == null)
		{
			return;
		}
		bool useSystemLanguagePerDefault = settings.useSystemLanguagePerDefault;
		LanguageCode code = LocalizationSettings.GetLanguageEnum(settings.defaultLangCode);
		if (useSystemLanguagePerDefault)
		{
			LanguageCode languageCode = LanguageNameToCode(Application.systemLanguage);
			if (languageCode == LanguageCode.N)
			{
				string twoLetterISOLanguageName = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
				if (twoLetterISOLanguageName != "iv")
				{
					languageCode = LocalizationSettings.GetLanguageEnum(twoLetterISOLanguageName);
				}
			}
			if (availableLanguages.Contains(string.Concat(languageCode, string.Empty)))
			{
				code = languageCode;
			}
		}
		string text = PlayerPrefs.GetString("M2H_lastLanguage", string.Empty);
		if (text != string.Empty && availableLanguages.Contains(text))
		{
			SwitchLanguage(text);
		}
		else
		{
			SwitchLanguage(code);
		}
	}

	private static void LoadAvailableLanguages()
	{
		availableLanguages = new List<string>();
		if (settings == null || settings.sheetTitles == null || settings.sheetTitles.Length == 0)
		{
			settings = backup_settings;
			if (backup_settings == null || backup_settings.sheetTitles == null || backup_settings.sheetTitles.Length == 0)
			{
				return;
			}
		}
		foreach (int value in Enum.GetValues(typeof(LanguageCode)))
		{
			if (HasLanguageFile(string.Concat((LanguageCode)value, string.Empty), settings.sheetTitles[0]))
			{
				availableLanguages.Add(string.Concat((LanguageCode)value, string.Empty));
			}
		}
		availableLanguages.Remove("ZH");
		Resources.UnloadUnusedAssets();
	}

	public static string[] GetLanguages()
	{
		return availableLanguages.ToArray();
	}

	public static bool SwitchLanguage(string langCode)
	{
		return SwitchLanguage(LocalizationSettings.GetLanguageEnum(langCode));
	}

	public static bool SwitchLanguage(LanguageCode code)
	{
		if (availableLanguages.Contains(string.Concat(code, string.Empty)))
		{
			DoSwitch(code);
			return true;
		}
		if (currentLanguage == LanguageCode.N && availableLanguages.Count > 0)
		{
			DoSwitch(LocalizationSettings.GetLanguageEnum("EN"));
		}
		return false;
	}

	public static void ApplyRemoteEntries()
	{
		if (RemoteEntries == null || currentEntrySheets == null || settings == null || settings.sheetTitles.Length == 0 || !currentEntrySheets.ContainsKey(settings.sheetTitles[0]))
		{
			return;
		}
		Hashtable hashtable = currentEntrySheets[settings.sheetTitles[0]];
		string[] array = new string[2]
		{
			"EN",
			currentLanguage.ToString()
		};
		foreach (string key in array)
		{
			if (!RemoteEntries.TryGetValue(key, out var value) || value == null)
			{
				continue;
			}
			foreach (KeyValuePair<string, string> item in value)
			{
				if (!string.IsNullOrEmpty(item.Value))
				{
					hashtable[item.Key] = item.Value;
				}
			}
		}
	}

	public static void ReloadLanguage()
	{
		DoSwitch(currentLanguage);
	}

	private static void DoSwitch(LanguageCode newLang)
	{
		if (settings == null)
		{
			return;
		}
		PlayerPrefs.GetString("M2H_lastLanguage", string.Concat(newLang, string.Empty));
		currentLanguage = newLang;
		currentEntrySheets = new Dictionary<string, Hashtable>();
		XMLParser xMLParser = new XMLParser();
		string[] sheetTitles = settings.sheetTitles;
		foreach (string text in sheetTitles)
		{
			currentEntrySheets[text] = new Hashtable();
			if (newLang != LanguageCode.EN)
			{
				TextAsset textAsset = (TextAsset)Resources.Load("Languages/EN_" + text, typeof(TextAsset));
				if (textAsset != null)
				{
					AddEntries(currentEntrySheets[text], textAsset.text, xMLParser);
				}
			}
			string languageFileContents = GetLanguageFileContents(text);
			if (languageFileContents != null)
			{
				AddEntries(currentEntrySheets[text], languageFileContents, xMLParser);
			}
		}
		ApplyRemoteEntries();
		LocalizedAsset[] array = (LocalizedAsset[])UnityEngine.Object.FindObjectsOfType(typeof(LocalizedAsset));
		for (int i = 0; i < array.Length; i++)
		{
			array[i].LocalizeAsset();
		}
		SendMonoMessage("ChangedLanguage", currentLanguage);
	}

	public static UnityEngine.Object GetAsset(string name)
	{
		return Resources.Load(string.Concat("Languages/Assets/", CurrentLanguage(), "/", name));
	}

	private static void AddEntries(Hashtable sheet, string contents, XMLParser xMLParser)
	{
		foreach (Hashtable item in (ArrayList)(((ArrayList)((Hashtable)xMLParser.Parse(contents))["entries"])[0] as Hashtable)["entry"])
		{
			string text = string.Concat(item["_text"], string.Empty);
			if (text.Length != 0 || !sheet.ContainsKey((string)item["@name"]))
			{
				sheet[(string)item["@name"]] = text.UnescapeXML();
			}
		}
	}

	private static bool HasLanguageFile(string lang, string sheetTitle)
	{
		return (TextAsset)Resources.Load("Languages/" + lang + "_" + sheetTitle, typeof(TextAsset)) != null;
	}

	private static string GetLanguageFileContents(string sheetTitle)
	{
		string text = string.Concat("Languages/", currentLanguage, "_", sheetTitle);
		string path = _persistentDataPath + "/" + text + ".xml";
		if (File.Exists(path))
		{
			return File.ReadAllText(path);
		}
		TextAsset textAsset = (TextAsset)Resources.Load(text, typeof(TextAsset));
		if (textAsset != null)
		{
			return textAsset.text;
		}
		return null;
	}

	public static LanguageCode CurrentLanguage()
	{
		return currentLanguage;
	}

	private static string UnmirrorPlaceholders(string text)
	{
		text = Regex.Replace(text, ">(val\\d+)<", "<$1>");
		return Regex.Replace(text, "\\}(\\d+)\\{", "{$1}");
	}

	public static string FixArabicText(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return text;
		}
		if (currentLanguage != LanguageCode.AR)
		{
			return text;
		}
		if (text.StartsWith("MISSING LANG:"))
		{
			return text;
		}
		try
		{
			Type type = Type.GetType("ArabicSupport, Assembly-CSharp") ?? Type.GetType("ArabicSupport");
			if (type == null)
			{
				return text;
			}
			MethodInfo method = type.GetMethod("Fix", new Type[3]
			{
				typeof(string),
				typeof(bool),
				typeof(bool)
			});
			if (method != null)
			{
				return UnmirrorPlaceholders((method.Invoke(null, new object[3] { text, true, false }) as string) ?? text);
			}
			method = type.GetMethod("Fix", new Type[1] { typeof(string) });
			if (method != null)
			{
				return (method.Invoke(null, new object[1] { text }) as string) ?? text;
			}
			return text;
		}
		catch
		{
			return text;
		}
	}

	public static string Get(string key)
	{
		if (key.StartsWith("!!") && (object)settings != null)
		{
			return Get(key, settings.sheetTitles[0]);
		}
		return key;
	}

	public static string Get(string key, string sheetTitle)
	{
		if (currentEntrySheets == null || !currentEntrySheets.ContainsKey(sheetTitle))
		{
			return string.Empty;
		}
		if (currentEntrySheets[sheetTitle].ContainsKey(key))
		{
			return FixArabicText((string)currentEntrySheets[sheetTitle][key]);
		}
		ReportMissing(key);
		return "MISSING LANG:" + key;
	}

	private static void ReportMissing(string key)
	{
		lock (sReportedMissing)
		{
			if (sReportedMissing.Count >= 10 || !sReportedMissing.Add(key))
			{
				return;
			}
		}
		Debug.LogError("[MissingText] " + key + " (" + currentLanguage.ToString() + ")");
	}

	public static bool ContainsKey(string key)
	{
		if (key.StartsWith("!!"))
		{
			return ContainsKey(key, settings.sheetTitles[0]);
		}
		return false;
	}

	public static bool ContainsKey(string key, string sheetTitle)
	{
		if (currentEntrySheets == null || !currentEntrySheets.ContainsKey(sheetTitle))
		{
			return false;
		}
		return currentEntrySheets[sheetTitle].ContainsKey(key);
	}

	private static void SendMonoMessage(string methodString, params object[] parameters)
	{
		if (parameters != null)
		{
			_ = parameters.Length;
			_ = 1;
		}
		GameObject[] array = (GameObject[])UnityEngine.Object.FindObjectsOfType(typeof(GameObject));
		foreach (GameObject gameObject in array)
		{
			if ((bool)gameObject && gameObject.transform.parent == null)
			{
				if (parameters != null && parameters.Length == 1)
				{
					gameObject.gameObject.BroadcastMessage(methodString, parameters[0], SendMessageOptions.DontRequireReceiver);
				}
				else
				{
					gameObject.gameObject.BroadcastMessage(methodString, SendMessageOptions.DontRequireReceiver);
				}
			}
		}
	}

	public static LanguageCode LanguageNameToCode(SystemLanguage name)
	{
		switch (name)
		{
		case SystemLanguage.Afrikaans:
			return LanguageCode.AF;
		case SystemLanguage.Arabic:
			return LanguageCode.AR;
		case SystemLanguage.Basque:
			return LanguageCode.BA;
		case SystemLanguage.Belarusian:
			return LanguageCode.BE;
		case SystemLanguage.Bulgarian:
			return LanguageCode.BG;
		case SystemLanguage.Catalan:
			return LanguageCode.CA;
		case SystemLanguage.Chinese:
		case SystemLanguage.ChineseSimplified:
		case SystemLanguage.ChineseTraditional:
			return LanguageCode.ZH;
		case SystemLanguage.Czech:
			return LanguageCode.CS;
		case SystemLanguage.Danish:
			return LanguageCode.DA;
		case SystemLanguage.Dutch:
			return LanguageCode.NL;
		case SystemLanguage.English:
			return LanguageCode.EN;
		case SystemLanguage.Estonian:
			return LanguageCode.ET;
		case SystemLanguage.Faroese:
			return LanguageCode.FA;
		case SystemLanguage.Finnish:
			return LanguageCode.FI;
		case SystemLanguage.French:
			return LanguageCode.FR;
		case SystemLanguage.German:
			return LanguageCode.DE;
		case SystemLanguage.Greek:
			return LanguageCode.EL;
		case SystemLanguage.Hebrew:
			return LanguageCode.HE;
		case SystemLanguage.Hungarian:
			return LanguageCode.HU;
		case SystemLanguage.Icelandic:
			return LanguageCode.IS;
		case SystemLanguage.Indonesian:
			return LanguageCode.ID;
		case SystemLanguage.Italian:
			return LanguageCode.IT;
		case SystemLanguage.Japanese:
			return LanguageCode.JA;
		case SystemLanguage.Korean:
			return LanguageCode.KO;
		case SystemLanguage.Latvian:
			return LanguageCode.LA;
		case SystemLanguage.Lithuanian:
			return LanguageCode.LT;
		case SystemLanguage.Norwegian:
			return LanguageCode.NO;
		case SystemLanguage.Polish:
			return LanguageCode.PL;
		case SystemLanguage.Portuguese:
			return LanguageCode.PT;
		case SystemLanguage.Romanian:
			return LanguageCode.RO;
		case SystemLanguage.Russian:
			return LanguageCode.RU;
		case SystemLanguage.SerboCroatian:
			return LanguageCode.SH;
		case SystemLanguage.Slovak:
			return LanguageCode.SK;
		case SystemLanguage.Slovenian:
			return LanguageCode.SL;
		case SystemLanguage.Spanish:
			return LanguageCode.ES;
		case SystemLanguage.Swedish:
			return LanguageCode.SW;
		case SystemLanguage.Thai:
			return LanguageCode.TH;
		case SystemLanguage.Turkish:
			return LanguageCode.TR;
		case SystemLanguage.Ukrainian:
			return LanguageCode.UK;
		case SystemLanguage.Vietnamese:
			return LanguageCode.VI;
		default:
			return name switch
			{
				SystemLanguage.Hungarian => LanguageCode.HU, 
				SystemLanguage.Unknown => LanguageCode.N, 
				_ => LanguageCode.N, 
			};
		}
	}

	public static SystemLanguage LanguageCodeToName(LanguageCode code)
	{
		return code switch
		{
			LanguageCode.AF => SystemLanguage.Afrikaans, 
			LanguageCode.AR => SystemLanguage.Arabic, 
			LanguageCode.BA => SystemLanguage.Basque, 
			LanguageCode.BE => SystemLanguage.Belarusian, 
			LanguageCode.BG => SystemLanguage.Bulgarian, 
			LanguageCode.CA => SystemLanguage.Catalan, 
			LanguageCode.ZH => SystemLanguage.Chinese, 
			LanguageCode.CS => SystemLanguage.Czech, 
			LanguageCode.DA => SystemLanguage.Danish, 
			LanguageCode.NL => SystemLanguage.Dutch, 
			LanguageCode.EN => SystemLanguage.English, 
			LanguageCode.ET => SystemLanguage.Estonian, 
			LanguageCode.FA => SystemLanguage.Faroese, 
			LanguageCode.FI => SystemLanguage.Finnish, 
			LanguageCode.FR => SystemLanguage.French, 
			LanguageCode.DE => SystemLanguage.German, 
			LanguageCode.EL => SystemLanguage.Greek, 
			LanguageCode.HE => SystemLanguage.Hebrew, 
			LanguageCode.HU => SystemLanguage.Hungarian, 
			LanguageCode.IS => SystemLanguage.Icelandic, 
			LanguageCode.ID => SystemLanguage.Indonesian, 
			LanguageCode.IT => SystemLanguage.Italian, 
			LanguageCode.JA => SystemLanguage.Japanese, 
			LanguageCode.KO => SystemLanguage.Korean, 
			LanguageCode.LA => SystemLanguage.Latvian, 
			LanguageCode.LT => SystemLanguage.Lithuanian, 
			LanguageCode.NO => SystemLanguage.Norwegian, 
			LanguageCode.PL => SystemLanguage.Polish, 
			LanguageCode.PT => SystemLanguage.Portuguese, 
			LanguageCode.RO => SystemLanguage.Romanian, 
			LanguageCode.RU => SystemLanguage.Russian, 
			LanguageCode.SH => SystemLanguage.SerboCroatian, 
			LanguageCode.SK => SystemLanguage.Slovak, 
			LanguageCode.SL => SystemLanguage.Slovenian, 
			LanguageCode.ES => SystemLanguage.Spanish, 
			LanguageCode.SW => SystemLanguage.Swedish, 
			LanguageCode.TH => SystemLanguage.Thai, 
			LanguageCode.TR => SystemLanguage.Turkish, 
			LanguageCode.UK => SystemLanguage.Ukrainian, 
			LanguageCode.VI => SystemLanguage.Vietnamese, 
			_ => code switch
			{
				LanguageCode.HU => SystemLanguage.Hungarian, 
				LanguageCode.N => SystemLanguage.Unknown, 
				_ => SystemLanguage.English, 
			}, 
		};
	}
}
