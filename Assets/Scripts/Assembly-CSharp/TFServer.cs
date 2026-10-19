
#define ASSERTS_ON
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Text;
using MiniJSON;
using UnityEngine;

public class TFServer
{
	public delegate void JsonStringHandler(string jsonResponse, HttpStatusCode status);

	public delegate void JsonResponseHandler(Dictionary<string, object> dict, HttpStatusCode status);

	public const string ERROR_KEY = "error";

	public const string NETWORK_ERROR = "Network error";

	private const bool LOG_FAILED_REQUESTS = true;

	public static readonly string NETWORK_ERROR_JSON =
		"{\"success\": false, \"error\": \"Network error\"}";

	private static readonly string LOG_LOCATION;

	private static int errorCount;

	private CookieContainer cookies = new CookieContainer();

	private Dictionary<object, JsonStringHandler> reqs =
		new Dictionary<object, JsonStringHandler>();

	private bool shortCircuitRequests;

	public TFServer(CookieContainer cookies, int maxConnections)
	{
		this.cookies = cookies ?? new CookieContainer();
		TFWebClient.maxConnections = maxConnections;
	}

	public void ShortCircuitAllRequests()
	{
		shortCircuitRequests = true;
	}

	public void PostToJSON(
		string url,
		Dictionary<string, object> postDict,
		JsonResponseHandler callback,
		bool ignoreEtag = false)
	{
		string text = EncodePostData(postDict);
		TFWebClient client = RegisterCallback(callback);

		if (shortCircuitRequests)
		{
			Debug.Log("Short-circuiting a POST to " + url);

			JsonStringHandler handler = GetCallback(client);
			if (handler != null)
			{
				handler(NETWORK_ERROR_JSON, HttpStatusCode.ServiceUnavailable);
			}

			client.Dispose();
			return;
		}

		Debug.Log("Posting data to " + url);
		Debug.Log("POST data: " + text);

		client.Headers.Add(
			"Content-Type",
			"application/x-www-form-urlencoded");

		string etag = null;

		try
		{
			etag = SessionManager.Instance.theSession.WebFileServer.ReadETag();
		}
		catch (Exception ex)
		{
			Debug.LogWarning("Unable to read ETag: " + ex.Message);
		}

		if (!ignoreEtag && !string.IsNullOrEmpty(etag))
		{
			client.Headers.Add(HttpRequestHeader.IfMatch, etag);
		}

		client.UploadStringCompleted += OnUploadComplete;
		client.UploadStringAsync(new Uri(url), text);
	}

	public void PostToString(
		string url,
		Dictionary<string, object> postDict,
		JsonStringHandler callback)
	{
		string text = EncodePostData(postDict);
		TFWebClient client = RegisterCallback(callback);

		if (shortCircuitRequests)
		{
			Debug.Log("Short-circuiting a POST to " + url);

			JsonStringHandler handler = GetCallback(client);
			if (handler != null)
			{
				handler(NETWORK_ERROR_JSON, HttpStatusCode.ServiceUnavailable);
			}

			client.Dispose();
			return;
		}

		Debug.Log("Posting data to " + url);
		Debug.Log("POST data: " + text);

		client.Headers.Add(
			"Content-Type",
			"application/x-www-form-urlencoded");

		client.UploadStringCompleted += OnUploadComplete;
		client.UploadStringAsync(new Uri(url), text);
	}

	public void GetToJSON(string url, JsonResponseHandler callback)
	{
		TFWebClient client = RegisterCallback(callback);

		if (shortCircuitRequests)
		{
			Debug.Log("Short-circuiting a GET to " + url);

			JsonStringHandler handler = GetCallback(client);
			if (handler != null)
			{
				handler(NETWORK_ERROR_JSON, HttpStatusCode.ServiceUnavailable);
			}

			client.Dispose();
			return;
		}

		Debug.Log("Making request to " + url);

		client.DownloadStringCompleted += OnDownloadComplete;
		client.DownloadStringAsync(new Uri(url));
	}

	public Cookie GetCookie(Uri uri, string key)
	{
		if (uri == null || string.IsNullOrEmpty(key))
		{
			return null;
		}

		return cookies.GetCookies(uri)[key];
	}

	private TFWebClient RegisterCallback(JsonStringHandler callback)
	{
		TFWebClient client = new TFWebClient(cookies);

		reqs[client] = callback;
		client.NetworkError += OnNetworkError;

		return client;
	}

	private TFWebClient RegisterCallback(JsonResponseHandler callback)
	{
		TFWebClient client = new TFWebClient(cookies);

		reqs[client] = JsCallback(callback);
		client.NetworkError += OnNetworkError;

		return client;
	}

	private string EncodePostData(Dictionary<string, object> data)
	{
		if (data == null || data.Count == 0)
		{
			return string.Empty;
		}

		List<string> parts = new List<string>();

		foreach (KeyValuePair<string, object> item in data)
		{
			string key = item.Key ?? string.Empty;
			string value = item.Value == null
				? string.Empty
				: item.Value.ToString();

			parts.Add(
				WWW.EscapeURL(key) + "=" + WWW.EscapeURL(value));
		}

		return string.Join("&", parts.ToArray());
	}

	private JsonStringHandler JsCallback(JsonResponseHandler callback)
	{
		return delegate(string jsonResponse, HttpStatusCode status)
		{
			Dictionary<string, object> result = null;

			try
			{
				if (!string.IsNullOrEmpty(jsonResponse))
				{
					result = Json.Deserialize(jsonResponse)
						as Dictionary<string, object>;
				}
			}
			catch (Exception ex)
			{
				Debug.LogWarning(
					"TFServer JSON parsing failed: " + ex.Message);
			}

			if (result == null)
			{
				Debug.LogWarning(
					"TFServer received an invalid JSON object. HTTP status: "
					+ (int)status);

				result = new Dictionary<string, object>
				{
					["success"] = false,
					["error"] = "Invalid server response"
				};
			}

			if (callback != null)
			{
				callback(result, status);
			}
		};
	}

	private static string BodyOrNetworkError(WebException exception)
	{
		try
		{
			if (exception != null && exception.Response != null)
			{
				using (Stream stream = exception.Response.GetResponseStream())
				{
					if (stream != null)
					{
						using (StreamReader reader =
							new StreamReader(stream, Encoding.UTF8))
						{
							string body = reader.ReadToEnd();

							if (!string.IsNullOrEmpty(body))
							{
								Dictionary<string, object> parsed = null;

								try
								{
									parsed = Json.Deserialize(body)
										as Dictionary<string, object>;
								}
								catch
								{
									// The response is not valid JSON.
								}

								if (parsed != null &&
									parsed.ContainsKey("error"))
								{
									return body;
								}
							}
						}
					}
				}
			}
		}
		catch (Exception ex)
		{
			Debug.LogWarning(
				"Unable to read HTTP error response: " + ex.Message);
		}

		return NETWORK_ERROR_JSON;
	}

	private void OnNetworkError(object sender, WebException exception)
	{
		JsonStringHandler callback = GetCallback(sender);

		if (callback == null)
		{
			return;
		}

		HttpStatusCode status = HttpStatusCode.ServiceUnavailable;
		string body = NETWORK_ERROR_JSON;

		try
		{
			if (exception != null && exception.Response != null)
			{
				HttpWebResponse response =
					exception.Response as HttpWebResponse;

				if (response != null)
				{
					status = response.StatusCode;
				}

				body = BodyOrNetworkError(exception);

				if (LOG_FAILED_REQUESTS)
				{
					Debug.LogWarning(
						"Request failed. HTTP status: "
						+ (int)status + ", response: " + body);
				}
			}
			else if (exception != null)
			{
				Debug.LogWarning(
					"Network request failed: " + exception.Message);
			}
		}
		catch (Exception ex)
		{
			Debug.LogWarning(
				"Error while handling network failure: " + ex.Message);
		}

		callback(body, status);
	}

	private void OnDownloadComplete(
		object sender,
		DownloadStringCompletedEventArgs e)
	{
		Debug.Log("In OnDownloadComplete...");

		JsonStringHandler callback = OnRequestComplete(sender, e);

		if (callback == null || e == null || e.Cancelled || e.Error != null)
		{
			return;
		}

		string result = e.Result ?? string.Empty;

		if (!result.Contains("\"ip\":"))
		{
			Debug.Log("Web result: " + result);
		}

		callback(result, HttpStatusCode.OK);
	}

	private void OnUploadComplete(
		object sender,
		UploadStringCompletedEventArgs e)
	{
		Debug.Log("In OnUploadComplete...");

		JsonStringHandler callback = OnRequestComplete(sender, e);

		if (callback == null || e == null || e.Cancelled || e.Error != null)
		{
			return;
		}

		string result = e.Result ?? string.Empty;

		if (LOG_FAILED_REQUESTS && string.IsNullOrEmpty(result))
		{
			Debug.LogWarning("Server returned an empty response.");
		}

		callback(result, HttpStatusCode.OK);
	}

	private JsonStringHandler OnRequestComplete(
		object sender,
		AsyncCompletedEventArgs e)
	{
		if (e == null)
		{
			Debug.LogWarning("TFServer received null event arguments.");
			return null;
		}

		Debug.Log("In OnRequestComplete... " + e.Error);

		JsonStringHandler callback = GetCallback(sender);

		if (e.Cancelled)
		{
			Debug.LogWarning("HTTP request was cancelled.");
			return null;
		}

		if (e.Error != null)
		{
			if (callback == null)
			{
				return null;
			}

			WebException webException = e.Error as WebException;

			if (webException != null)
			{
				HttpStatusCode status = HttpStatusCode.RequestTimeout;

				if (webException.Response != null)
				{
					HttpWebResponse response =
						webException.Response as HttpWebResponse;

					if (response != null)
					{
						status = response.StatusCode;
					}
				}

				string body = BodyOrNetworkError(webException);

				if (LOG_FAILED_REQUESTS)
				{
					Debug.LogWarning(
						"Request failed. HTTP status: "
						+ (int)status + ", response: " + body);
				}

				callback(body, status);
			}
			else
			{
				Debug.LogWarning(
					"Unexpected HTTP request error: " + e.Error.Message);

				callback(
					NETWORK_ERROR_JSON,
					HttpStatusCode.Unused);
			}

			return null;
		}

		return callback;
	}

	private JsonStringHandler GetCallback(object sender)
	{
		if (sender == null || !reqs.ContainsKey(sender))
		{
			return null;
		}

		JsonStringHandler callback = reqs[sender];
		reqs.Remove(sender);

		return callback;
	}

	private void LogResponse(
		HttpWebResponse response,
		WebExceptionStatus? status = null)
	{
		if (response == null)
		{
			return;
		}

		try
		{
			using (Stream responseStream = response.GetResponseStream())
			{
				if (responseStream == null)
				{
					return;
				}

				using (StreamReader reader =
					new StreamReader(responseStream, Encoding.UTF8))
				{
					reader.ReadToEnd();
				}
			}
		}
		catch (Exception ex)
		{
			Debug.LogWarning(
				"Unable to log HTTP response: " + ex.Message);
		}
	}

	static TFServer()
	{
		string persistentDataPath = Application.persistentDataPath;
		char separator = Path.DirectorySeparatorChar;

		LOG_LOCATION = persistentDataPath + separator + "error";
		errorCount = 0;
	}
}