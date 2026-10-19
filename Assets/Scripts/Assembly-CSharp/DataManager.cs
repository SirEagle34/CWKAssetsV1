using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using MiniJSON;
using UnityEngine;

public abstract class DataManager<T> : IDataManager where T : ILoadableData
{
	private const string parttwo = "210429q";

	private const string partfour = "bmmfzDb";

	private bool isLoaded;

	private string partone = "_ijmMW";

	private string partthree = "m201510";

	protected Dictionary<string, T> Database = new Dictionary<string, T>();

	protected List<T> DatabaseArray = new List<T>();

	protected List<IDataManager> Dependencies = new List<IDataManager>();

	protected static object threadLock;

	private bool doneLoadingAndParsingJsonData;

	private bool donePostLoad;

	private Exception ex;

	public string FilePath { get; set; }

	public bool IsLoaded
	{
		get
		{
			return isLoaded;
		}
		set
		{
			isLoaded = value;
		}
	}

	private bool ExceptionThrown => ex != null;

	private void PrintException(Exception ex)
	{
		if (ex == null)
		{
			return;
		}
		Singleton<SimplePopupController>.Instance.ShowMessage(string.Empty, "An error occured while loading data. View log for details.", Application.Quit);
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine(ex.ToString());
		for (Exception innerException = ex.InnerException; innerException != null; innerException = innerException.InnerException)
		{
			stringBuilder.AppendLine($"\tInnerException: {innerException.ToString()}");
		}
		foreach (object key in ex.Data.Keys)
		{
			stringBuilder.AppendLine("****** Extra Data ******");
			stringBuilder.AppendLine($"[{key}] = {ex.Data[key]}");
		}
		Debug.LogError(stringBuilder.ToString());
	}

	private void CheckAndThrowExeptions()
	{
		if (ex != null)
		{
			PrintException(ex);
			throw ex;
		}
	}

	public IEnumerator Load()
	{
		IsLoaded = false;
		Database.Clear();
		DatabaseArray.Clear();
		doneLoadingAndParsingJsonData = false;
		donePostLoad = false;
		ex = null;
		if (threadLock == null)
		{
			threadLock = new object();
		}
		IDataManager manager2 = null;
		foreach (IDataManager manager3 in Dependencies)
		{
			new Thread((ThreadStart)delegate
			{
				loadDependencyThread(manager2);
			}).Start();
			while (!manager3.IsLoaded && !ExceptionThrown)
			{
				yield return null;
			}
			CheckAndThrowExeptions();
		}
		string text = BlueprintCache.Get(FilePath);
		if (text == null)
		{
			WWW www = new WWW(FilePath, null, LiveContent.Headers());
			while (!www.isDone)
			{
				yield return null;
			}
			text = www.text;
		}
		LoadAndParseJsonDataThread(null, text);
		while (!doneLoadingAndParsingJsonData && !ExceptionThrown)
		{
			yield return null;
		}
		CheckAndThrowExeptions();
		IsLoaded = true;
		new Thread((ThreadStart)delegate
		{
			PostLoadThread();
		}).Start();
		while (!donePostLoad && !ExceptionThrown)
		{
			yield return null;
		}
		CheckAndThrowExeptions();
	}

	private void loadDependencyThread(IDataManager manager)
	{
		lock (threadLock)
		{
			try
			{
				manager?.Load();
			}
			catch (Exception ex)
			{
				if (manager != null)
				{
					ex.Data.Add("Manager", manager.ToString());
				}
				this.ex = ex;
			}
		}
	}

	private void LoadAndParseJsonDataThread(string appliedFilePath)
	{
		LoadAndParseJsonDataThread(appliedFilePath, null);
	}

	private void LoadAndParseJsonDataThread(string appliedFilePath, string wwwText)
	{
		lock (threadLock)
		{
			try
			{
				doneLoadingAndParsingJsonData = false;
				Debug.Log("Loading JSON data from: " + appliedFilePath);
				try
				{
					List<object> jlist = (List<object>)Json.Deserialize(wwwText);
					ParseRows(jlist);
					doneLoadingAndParsingJsonData = true;
				}
				catch
				{
					List<object> list = new List<object>();
					object item = Json.Deserialize(wwwText);
					list.Add(item);
					ParseRows(list);
					doneLoadingAndParsingJsonData = true;
				}
			}
			catch (Exception ex)
			{
				ex.Data.Add("Filename", appliedFilePath);
				this.ex = ex;
			}
		}
	}

	private void PostLoadThread()
	{
		lock (threadLock)
		{
			try
			{
				donePostLoad = false;
				PostLoad();
				donePostLoad = true;
			}
			catch (Exception ex)
			{
				this.ex = ex;
			}
		}
	}

	protected virtual void ParseRows(List<object> jlist)
	{
		foreach (object item in jlist)
		{
			Dictionary<string, object> dict = item as Dictionary<string, object>;
			T val = (T)Activator.CreateInstance(typeof(T));
			if (TryPopulate(val, dict))
			{
				if (val.ID != string.Empty && !Database.ContainsKey(val.ID))
				{
					Database.Add(val.ID, val);
				}
				DatabaseArray.Add(val);
			}
		}
	}

	protected virtual void PostLoad()
	{
	}

	protected bool TryPopulate(ILoadableData val, Dictionary<string, object> dict)
	{
		if (dict == null)
		{
			return false;
		}
		try
		{
			val.Populate(dict);
			return true;
		}
		catch (Exception e)
		{
			LogSkip(dict, e);
			return false;
		}
	}

	protected void SafeRow(object row, Action body)
	{
		try
		{
			body();
		}
		catch (Exception e)
		{
			LogSkip(row as Dictionary<string, object>, e);
		}
	}

	private void LogSkip(Dictionary<string, object> dict, Exception e)
	{
		object obj = null;
		if (dict != null)
		{
			using Dictionary<string, object>.ValueCollection.Enumerator enumerator = dict.Values.GetEnumerator();
			if (enumerator.MoveNext())
			{
				obj = enumerator.Current;
			}
		}
		Debug.LogWarning("[DataSkip] " + Path.GetFileName(FilePath ?? "") + " row '" + obj?.ToString() + "': " + e.Message);
	}

	public void AddDependency(IDataManager manager)
	{
		if (Dependencies == null)
		{
			Dependencies = new List<IDataManager>();
		}
		Dependencies.Add(manager);
	}

	public T GetData(string ID)
	{
		if (Database.ContainsKey(ID))
		{
			return Database[ID];
		}
		return default(T);
	}

	public T GetData(int index)
	{
		if (index < 0 || index >= DatabaseArray.Count)
		{
			return default(T);
		}
		return DatabaseArray[index];
	}

	public int GetIndex(T data)
	{
		return DatabaseArray.IndexOf(data);
	}

	public T Find(Predicate<T> match)
	{
		return DatabaseArray.Find(match);
	}

	public List<T> GetDatabase()
	{
		return DatabaseArray;
	}

	public virtual void Unload()
	{
		Database.Clear();
		DatabaseArray.Clear();
		IsLoaded = false;
		doneLoadingAndParsingJsonData = false;
		donePostLoad = false;
		ex = null;
	}
}
