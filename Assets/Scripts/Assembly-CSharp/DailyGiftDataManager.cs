using System.Collections;
using System.Collections.Generic;
using System.IO;
using JsonFx.Json;
using UnityEngine.Networking;
using UnityEngine;

public class DailyGiftDataManager : DataManager<DailyRouletteGiftData>
{
	private static DailyGiftDataManager _instance;

    public static DailyGiftDataManager Instance
    {
        get
        {
            if (_instance == null)
            {
                string path = Path.Combine(
                    SQSettings.CDN_URL,
                    "Blueprints",
                    "db_DailyGift.json"
                );

                _instance = new DailyGiftDataManager(path);
            }

            return _instance;
        }
    }

    public DailyGiftDataManager(string path)
    {
        base.FilePath = path;
    }    
	private const int kMaxDailyGifts = 12;

    private DailyGiftWeightTable dailyGiftWeightTable = new DailyGiftWeightTable();

	private List<DailyRouletteGiftData> gifts = new List<DailyRouletteGiftData>();

	public DailyRouletteGiftData[] GetAllGifts()
	{
		return gifts.ToArray();
	}

    private int numDailyGifts = 0;
    private int _GemCost = 0;

    public bool Loaded = false;

    public int RetryGemCost => _GemCost;

    public IEnumerator LoadDailyGiftData()
	{
		Loaded = false;

		numDailyGifts = 0;
		gifts.Clear(); // 🔥 IMPORTANT FIX
		dailyGiftWeightTable = new DailyGiftWeightTable();

		Database.Clear();
		DatabaseArray.Clear();

		Dictionary<string, object>[] array = null;

		yield return LoadDailyGiftsData(result =>
		{
			array = result;
		});

		if (array == null || array.Length == 0)
		{
			Debug.LogError("DailyGiftDataManager: Empty JSON");
			Loaded = true;
			yield break;
		}

		foreach (var dict in array)
		{
			if (dict == null)
				continue;

			if (numDailyGifts >= kMaxDailyGifts)
			{
				_GemCost = TFUtils.LoadInt(dict, "RetryGemCost");
				break;
			}

			DailyRouletteGiftData data = new DailyRouletteGiftData();
			data.Populate(dict);

			gifts.Add(data);

			dailyGiftWeightTable.Add(numDailyGifts, data.Weight);

			DatabaseArray.Add(data);

			if (!string.IsNullOrEmpty(data.ID))
				Database[data.ID] = data;

			numDailyGifts++;

			if (LoadingManager.ShouldYield())
				yield return null;
		}

		Loaded = true;
	}


	public struct SpinResult
	{
		public DailyRouletteGiftData reward;
		public int index;
	}

	public SpinResult PickGift()
	{
		var all = gifts;

		if (all == null || all.Count == 0)
		{
			Debug.LogError("NO GIFTS!");
			return new SpinResult { reward = null, index = -1 };
		}

		int total = 0;

		// sadece valid list
		List<int> validIndexes = new List<int>();

		for (int i = 0; i < all.Count; i++)
		{
			int w = Mathf.Max(0, all[i].Weight);

			if (w <= 0) continue;

			total += w;
			validIndexes.Add(i);
		}

		if (total <= 0 || validIndexes.Count == 0)
		{
			Debug.LogError("ALL WEIGHTS INVALID!");
			return new SpinResult { reward = all[0], index = 0 };
		}

		int roll = Random.Range(0, total);
		int current = 0;

		for (int j = 0; j < validIndexes.Count; j++)
		{
			int i = validIndexes[j];
			int w = all[i].Weight;

			current += w;

			if (roll < current)
			{
				return new SpinResult
				{
					reward = all[i],
					index = i
				};
			}
		}

		// fallback güvenli (bias yok)
		int last = validIndexes[validIndexes.Count - 1];

		return new SpinResult
		{
			reward = all[last],
			index = last
		};
	}


	public DailyRouletteGiftData GetGift(int index)
	{
		if (gifts == null || gifts.Count == 0)
			return null;

		if (index < 0 || index >= gifts.Count)
			return gifts[0];

		return gifts[index];
	}

    public void ResetInstance()
    {
        _instance = null;
    }

    private Dictionary<string, object>[] LoadDailyGiftsData()
    {
        string path = base.FilePath;

        if (string.IsNullOrEmpty(path))
        {
            Debug.LogError("FilePath null");
            return null;
        }

        if (!File.Exists(path))
        {
            Debug.LogError("File not found: " + path);
            return null;
        }

        string json = File.ReadAllText(path);

        if (string.IsNullOrEmpty(json))
        {
            Debug.LogError("JSON empty");
            return null;
        }

        return JsonReader.Deserialize<Dictionary<string, object>[]>(json);
    }

	private IEnumerator LoadDailyGiftsData(System.Action<Dictionary<string, object>[]> callback)
	{
		Debug.Log("LOADING URL = " + base.FilePath);

		UnityWebRequest www = UnityWebRequest.Get(base.FilePath);

		yield return www.SendWebRequest();

		if (www.result != UnityWebRequest.Result.Success)
		{
			Debug.LogError("DOWNLOAD ERROR: " + www.error);
			callback(null);
			yield break;
		}

		string json = www.downloadHandler.text;

		Debug.Log("JSON SIZE = " + json.Length);

		if (string.IsNullOrEmpty(json))
		{
			Debug.LogError("EMPTY JSON");
			callback(null);
			yield break;
		}

		Dictionary<string, object>[] data = null;

		try
		{
			data = JsonReader.Deserialize<Dictionary<string, object>[]>(json);
		}
		catch (System.Exception e)
		{
			Debug.LogError("JSON PARSE ERROR: " + e);
		}

		callback(data);
	}
}