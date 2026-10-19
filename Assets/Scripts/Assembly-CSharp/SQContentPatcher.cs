using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Ionic.Zlib;
using MiniJSON;
using UnityEngine;

public class SQContentPatcher : EventDispatcher<string>
{
	private class Manifest
	{
		public Dictionary<string, ManifestEntry> Contents;

		public long Version { get; set; }

		public Manifest()
		{
			Contents = new Dictionary<string, ManifestEntry>();
		}
	}

	private class ManifestEntry
	{
		public string Name { get; set; }

		public string Digest { get; set; }

		public override string ToString()
		{
			return Name + "(" + Digest + ")";
		}
	}

	public delegate void PatchContentListener(string eventStr);

	public delegate void DownloadFileHandler(object sender, DownloadStringCompletedEventArgs e);

	private const string ETAG_FILE_NAME = "lastManifestEtag";

	private const string DIGEST_FILE_NAME = "ManifestDigest";

	public const string PATCHING_DONE_EVENT = "patchingDone";

	public const string PATCHING_NECESSARY_EVENT = "patchingNecessary";

	public const string PATCHING_NOT_NECESSARY_EVENT = "patchingNotNecessary";

	private Manifest _localManifest;

	private Manifest _remoteManifest;

	private string _manifestContents;

	private string _remoteManifestEtag;

	private Dictionary<string, ManifestEntry> _inProgressContent = new Dictionary<string, ManifestEntry>();

	private Dictionary<string, ManifestEntry> _remainingContent = new Dictionary<string, ManifestEntry>();

	private string dataPath;

	private string persistentAssetsPath;

	public bool ContentChanged;

	private bool patchingIsFinished;

	public static long CurrentManifestVersion = -1L;

	public SQContentPatcher()
	{
		dataPath = Path.Combine(Application.persistentDataPath, "TmpContents");
		persistentAssetsPath = TFUtils.GetPersistentAssetsPath();
	}

	public string LocalManifestVersion()
	{
		return _localManifest.Version.ToString();
	}

	private static void DeleteIfPresent(string path)
	{
		try
		{
			if (File.Exists(path))
			{
				File.Delete(path);
			}
		}
		catch (Exception ex)
		{
			Debug.LogWarning("[Patch] could not remove '" + path + "': " + ex.Message);
		}
	}

	public void ValidateAndFixDownloadedManifests()
	{
		Debug.Log("Validating manifest...");
		bool flag = false;
		try
		{
			flag = ValidateDownloadedManifests();
		}
		catch (Exception ex)
		{
			Debug.Log("Patch validation exception: " + ex.ToString());
		}
		if (!flag)
		{
			Debug.Log("Bad downloaded manifest files detected. Removing bad files...");
			string text = persistentAssetsPath;
			char directorySeparatorChar = Path.DirectorySeparatorChar;
			DeleteIfPresent(text + directorySeparatorChar + "manifest.json");
			string text2 = persistentAssetsPath;
			directorySeparatorChar = Path.DirectorySeparatorChar;
			DeleteIfPresent(text2 + directorySeparatorChar + "lastManifestEtag");
			string text3 = persistentAssetsPath;
			directorySeparatorChar = Path.DirectorySeparatorChar;
			DeleteIfPresent(text3 + directorySeparatorChar + "ManifestDigest");
		}
		else
		{
			Debug.Log("Validation successful.");
		}
	}

	public void ReadManifests()
	{
		_localManifest = new Manifest();
		_localManifest.Version = -1L;
		try
		{
			string streamingAssetsFile = TFUtils.GetStreamingAssetsFile("manifest.json");
			_localManifest = ReadManifest((Dictionary<string, object>)Json.Deserialize(TFUtils.GetJsonLocalContent(streamingAssetsFile)));
			string currentETag = null;
			string text = persistentAssetsPath;
			char directorySeparatorChar = Path.DirectorySeparatorChar;
			string path = text + directorySeparatorChar + "lastManifestEtag";
			if (File.Exists(path))
			{
				currentETag = File.ReadAllText(path);
			}
			FetchRemoteManifest(currentETag);
		}
		catch (Exception e)
		{
			OnError(e);
		}
	}

	private bool ValidateDownloadedManifests()
	{
		string text = persistentAssetsPath;
		char directorySeparatorChar = Path.DirectorySeparatorChar;
		string text2 = text + directorySeparatorChar;
		string text3 = text2 + "manifest.json";
		string path = text2 + "lastManifestEtag";
		string path2 = text2 + "ManifestDigest";
		bool flag = File.Exists(text3);
		bool flag2 = File.Exists(path);
		bool flag3 = File.Exists(path2);
		if (!flag || !flag2 || !flag3)
		{
			if (!flag && !flag2)
			{
				return !flag3;
			}
			return false;
		}
		string jsonLocalContent = TFUtils.GetJsonLocalContent(text3);
		if (!string.Equals(File.ReadAllText(path2), TFUtils.ComputeDigest(jsonLocalContent + File.ReadAllText(path)), StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		foreach (ManifestEntry value in ReadManifest((Dictionary<string, object>)Json.Deserialize(jsonLocalContent)).Contents.Values)
		{
			string text4 = text2 + value.Name;
			if (!File.Exists(text4))
			{
				return false;
			}
			if (!string.Equals(value.Digest, TFUtils.ComputeDigest(TFUtils.GetJsonLocalContent(text4)), StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}
		}
		return true;
	}

	private Manifest ReadManifest(Dictionary<string, object> dict)
	{
		Manifest manifest = null;
		try
		{
			manifest = new Manifest();
			List<object> list = (List<object>)dict["contents"];
			manifest.Version = (long)dict["version"];
			foreach (Dictionary<string, object> item in list)
			{
				ManifestEntry manifestEntry = new ManifestEntry();
				manifestEntry.Name = item["n"].ToString();
				manifestEntry.Digest = item["d"].ToString();
				manifest.Contents[manifestEntry.Name] = manifestEntry;
			}
			return manifest;
		}
		catch (Exception)
		{
			manifest = new Manifest();
			manifest.Version = -1L;
			return manifest;
		}
	}

	private void FetchRemoteManifest(string currentETag)
	{
		GetFile(SQSettings.MANIFEST_URL, OnDownloadManifestComplete, currentETag);
	}

	private void OnDownloadManifestComplete(
		object sender,
		DownloadDataCompletedEventArgs e)
	{
		try
		{
			if (e == null)
			{
				OnError(new InvalidDataException(
					"[ContentPatcher] Download callback arguments are null."));
				return;
			}

			if (e.Cancelled)
			{
				OnError(new IOException(
					"[ContentPatcher] Manifest download was cancelled."));
				return;
			}

			if (e.Error != null)
			{
				OnError(e.Error);
				return;
			}

			byte[] result = e.Result;

			if (result == null || result.Length == 0)
			{
				OnError(new InvalidDataException(
					"[ContentPatcher] Manifest response is empty."));
				return;
			}

			Debug.Log(
				"[ContentPatcher] Manifest response size: " +
				result.Length +
				" bytes. Header: " +
				BitConverter.ToString(
					result,
					0,
					Math.Min(result.Length, 8)));

			string text = decodeZippedData(result);

			if (string.IsNullOrEmpty(text))
			{
				OnError(new InvalidDataException(
					"[ContentPatcher] Decoded manifest is empty."));
				return;
			}

			object parsed = Json.Deserialize(text);

			Dictionary<string, object> dict =
				parsed as Dictionary<string, object>;

			if (dict == null)
			{
				OnError(new InvalidDataException(
					"[ContentPatcher] Manifest is not a valid JSON object. " +
					"Response starts with: " +
					text.Substring(0, Math.Min(text.Length, 160))));
				return;
			}

			Manifest remoteManifest = ReadManifest(dict);

			if (remoteManifest == null || remoteManifest.Version < 0)
			{
				OnError(new InvalidDataException(
					"[ContentPatcher] Manifest JSON is missing valid " +
					"'version' or 'contents' data."));
				return;
			}

			TFWebClient client = sender as TFWebClient;

			if (client == null)
			{
				OnError(new InvalidDataException(
					"[ContentPatcher] Download sender is not a TFWebClient."));
				return;
			}

			string etag = null;

			if (client.ResponseHeaders != null)
			{
				etag = client.ResponseHeaders[HttpResponseHeader.ETag];
			}

			_manifestContents = text;
			_remoteManifestEtag = etag;
			_remoteManifest = remoteManifest;

			ProcessManifests();
		}
		catch (Exception ex)
		{
			Debug.LogError(
				"[ContentPatcher] Failed to process remote manifest: " + ex);

			OnError(ex);
		}
	}

	private void ProcessManifests()
	{
		CurrentManifestVersion = _remoteManifest.Version;
		Debug.Log($"Processing manifests remote version {_remoteManifest.Version} local version {_localManifest.Version} ");
		if (_remoteManifest.Version > _localManifest.Version)
		{
			if (Directory.Exists(persistentAssetsPath))
			{
				string[] files = Directory.GetFiles(persistentAssetsPath, "*.*", SearchOption.AllDirectories);
				foreach (string obj in files)
				{
					FileInfo fileInfo = new FileInfo(obj);
					FileInfo fileInfo2 = new FileInfo(obj.Replace(persistentAssetsPath, dataPath));
					if (!fileInfo2.Directory.Exists)
					{
						CreateDirectory(fileInfo2.FullName);
					}
					File.Copy(fileInfo.FullName, fileInfo2.FullName, overwrite: true);
					Debug.Log($"Copying {fileInfo.FullName} to {fileInfo2.FullName}");
				}
			}
			lock (this)
			{
				foreach (ManifestEntry value in _remoteManifest.Contents.Values)
				{
					if (_localManifest.Contents.ContainsKey(value.Name))
					{
						if (!_localManifest.Contents[value.Name].Digest.Equals(value.Digest))
						{
							Debug.Log("Remote manifest contains new digest for " + value);
							string key = SQSettings.CDN_URL + value.Digest;
							_remainingContent[key] = value;
						}
					}
					else
					{
						Debug.Log("Remote manifest contains entries that the local manifest does not have " + value.ToString());
						string key2 = SQSettings.CDN_URL + value.Digest;
						_remainingContent[key2] = value;
					}
				}
				if (_remainingContent.Keys.Count == 0)
				{
					FireEvent("patchingNotNecessary");
					PatchingDone();
				}
				else
				{
					FireEvent("patchingNecessary");
				}
				return;
			}
		}
		Debug.Log("Remote manifest and local manifest both running content version " + _localManifest.Version);
		FireEvent("patchingNotNecessary");
		PatchingDone();
	}

	public void StartDownloadingPatchedContent()
	{
		lock (this)
		{
			IEnumerable<KeyValuePair<string, ManifestEntry>> enumerable = _remainingContent.Take(SQSettings.PATCHING_FILE_LIMIT - _inProgressContent.Count);
			List<string> list = new List<string>();
			foreach (KeyValuePair<string, ManifestEntry> item in enumerable)
			{
				GetFile(item.Key, OnDownloadComplete);
				_inProgressContent.Add(item.Key, item.Value);
				list.Add(item.Key);
			}
			foreach (string item2 in list)
			{
				_remainingContent.Remove(item2);
			}
		}
	}

	private void GetFile(string url, DownloadDataCompletedEventHandler onDownloadComplete, string currentETag = null)
	{
		using TFWebClient tFWebClient = new TFWebClient(null);
		Debug.Log("fetching content from " + url + ", ETag:" + currentETag);
		if (currentETag != null)
		{
			tFWebClient.Headers.Add(HttpRequestHeader.IfNoneMatch, currentETag);
		}
		tFWebClient.DownloadDataCompleted += onDownloadComplete;
		tFWebClient.NetworkError += OnNetworkError;
		tFWebClient.DownloadDataAsync(new Uri(url), url);
	}

	private bool IsExpectedStatus(Exception ex)
	{
		if (ex is WebException ex2)
		{
			HttpWebResponse httpWebResponse = (HttpWebResponse)ex2.Response;
			if (httpWebResponse != null && httpWebResponse.StatusCode == HttpStatusCode.NotModified)
			{
				return true;
			}
		}
		return false;
	}

	private void OnNetworkError(object sender, WebException e)
	{
		if (!IsExpectedStatus(e))
		{
			Debug.Log("Error downloading file during content patching. Continuing without version patching");
		}
	}

	private void OnError(Exception e)
	{
		if (e == null)
		{
			Debug.LogError("[ContentPatcher] Unknown patching error.");
		}
		else if (IsExpectedStatus(e))
		{
			Debug.Log("Expected status on web error. Skip Patching.");

			if (_localManifest != null)
			{
				CurrentManifestVersion = _localManifest.Version;
			}

			FireEvent("patchingNotNecessary");
		}
		else
		{
			Debug.LogError(
				"[ContentPatcher] Patching failed: " + e);
		}

		PatchingDone();
	}

	private void CreateDirectory(string path)
	{
		Directory.CreateDirectory(Path.GetDirectoryName(path));
	}

	private void SaveContentFile(string path, string contents)
	{
		string text = dataPath;
		char directorySeparatorChar = Path.DirectorySeparatorChar;
		string text2 = text + directorySeparatorChar + path;
		CreateDirectory(text2);
		File.WriteAllText(text2, contents);
		Debug.Log("Saved patch file" + text2);
	}

	public static string decodeZippedData(byte[] input)
	{
		if (input == null || input.Length == 0)
		{
			throw new InvalidDataException(
				"[ContentPatcher] No data was provided for decoding.");
		}

		byte[] data = input;

		// Preserve the existing Base64-encoded GZip format.
		if (input.Length >= 3 &&
			input[0] == (byte)'H' &&
			input[1] == (byte)'4' &&
			input[2] == (byte)'s')
		{
			try
			{
				data = Convert.FromBase64String(
					Encoding.UTF8.GetString(input).Trim());
			}
			catch (FormatException ex)
			{
				throw new InvalidDataException(
					"[ContentPatcher] Invalid Base64-encoded content.",
					ex);
			}
		}

		if (data.Length == 0)
		{
			throw new InvalidDataException(
				"[ContentPatcher] Decoded data is empty.");
		}

		// GZip magic bytes: 1F 8B.
		bool isGzip =
			data.Length >= 2 &&
			data[0] == 0x1F &&
			data[1] == 0x8B;

		if (isGzip)
		{
			try
			{
				return TFUtils.Unzip(data);
			}
			catch (Exception ex)
			{
				throw new InvalidDataException(
					"[ContentPatcher] Response has a GZip header, " +
					"but decompression failed.",
					ex);
			}
		}

		// The response may legitimately be plain JSON/text.
		string plainText = Encoding.UTF8.GetString(data);

		Debug.LogWarning(
			"[ContentPatcher] Response is not GZip; treating it as plain text. " +
			"Size: " + data.Length + " bytes.");

		return plainText;
	}

	private void OnDownloadComplete(object sender, DownloadDataCompletedEventArgs e)
	{
		try
		{
			if (e.Error == null)
			{
				lock (this)
				{
					TFWebClient tFWebClient = (TFWebClient)sender;
					string text = e.UserState.ToString();
					Debug.Log(text + " has finished downloading");
					if (_inProgressContent.ContainsKey(text))
					{
						ManifestEntry manifestEntry = _inProgressContent[text];
						Debug.Log("Saving " + text + " to " + dataPath + manifestEntry.Name);
						string contents = decodeZippedData(e.Result);
						SaveContentFile(manifestEntry.Name, contents);
						_inProgressContent.Remove(text);
						StartDownloadingPatchedContent();
					}
					else
					{
						Debug.Log(string.Concat("Entry ", tFWebClient.QueryString, " does not exist in the in progress content"));
					}
					if (_remainingContent.Keys.Count == 0 && _inProgressContent.Count == 0)
					{
						PatchingSuccess();
					}
					else
					{
						Debug.Log($"{_remainingContent.Count} pieces of patched content left with {_inProgressContent.Count} in progress");
					}
					return;
				}
			}
			OnError(e.Error);
		}
		catch (Exception ex)
		{
			Debug.LogError(ex);
			OnError(ex);
		}
	}

	private void PatchingSuccess()
	{
		SaveContentFile("manifest.json", _manifestContents);
		SaveContentFile("lastManifestEtag", _remoteManifestEtag);
		SaveContentFile("ManifestDigest", TFUtils.ComputeDigest(_manifestContents + _remoteManifestEtag));
		if (Directory.Exists(persistentAssetsPath))
		{
			Directory.Delete(persistentAssetsPath, recursive: true);
		}
		Directory.Move(dataPath, persistentAssetsPath);
		ContentChanged = true;
		PatchingDone();
	}

	private void PatchingDone()
	{
		lock (this)
		{
			if (!patchingIsFinished)
			{
				FireEvent("patchingDone");
				patchingIsFinished = true;
			}
		}
	}

	public void PerformSanityCheck()
	{
		foreach (ManifestEntry value in _remoteManifest.Contents.Values)
		{
			string text = null;
			string text2 = null;
			if (value.Name.Contains("Language"))
			{
				string text3 = Application.dataPath;
				char pathSeparator = Path.PathSeparator;
				string text4 = text3 + pathSeparator + value.Name;
				if (File.Exists(text4))
				{
					text = text4;
					Debug.Log("Latest asset for " + value.Name + " is at " + text);
					text2 = File.ReadAllText(text);
				}
				else
				{
					Debug.Log("Latest asset for " + value.Name + " is at in bundled resources");
					text2 = ((TextAsset)Singleton<SLOTResourceManager>.Instance.LoadResource(value.Name.Substring(0, value.Name.Length - 4), typeof(TextAsset))).text;
				}
			}
			else
			{
				text = TFUtils.GetStreamingAssetsFile(value.Name);
				Debug.Log("Latest asset for " + value.Name + " is at " + text);
				text2 = File.ReadAllText(text);
			}
			string text5 = CalculateMD5Hash(text2);
			if (!value.Digest.Equals(text5))
			{
				Debug.LogError(string.Concat("After done with patching, assets do not match digest ", value, " current digest is ", text5));
			}
		}
		Debug.Log("Asset contents have been verified.");
	}

	public string CalculateMD5Hash(string input)
	{
		MD5 mD = MD5.Create();
		byte[] bytes = Encoding.UTF8.GetBytes(input);
		byte[] array = mD.ComputeHash(bytes);
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = 0; i < array.Length; i++)
		{
			stringBuilder.Append(array[i].ToString("x2"));
		}
		return stringBuilder.ToString();
	}
}
