using System;
using System.Net;
using UnityEngine;

public class TFWebClient : WebClient
{
	public delegate void OnNetworkError(object sender, WebException e);

	private const int TIMEOUT = 30000;

	private const string USER_AGENT = "Innertube Explorer v0.1";

	private static int _maxConnections = 2;

	private static string mDeviceHeader;

	public string uriaddress;

	private CookieContainer cookies;

	public static int maxConnections
	{
		get
		{
			return maxConnections;
		}
		set
		{
			_maxConnections = Math.Max(1, value);
			ServicePointManager.DefaultConnectionLimit = _maxConnections;
		}
	}

	public event OnNetworkError NetworkError;

	public TFWebClient(CookieContainer cookieContainer)
	{
		cookies = cookieContainer;
	}

	protected override WebRequest GetWebRequest(Uri address)
	{
		uriaddress = address.AbsoluteUri;
		HttpWebRequest httpWebRequest = base.GetWebRequest(address) as HttpWebRequest;
		httpWebRequest.CookieContainer = cookies;
		httpWebRequest.Timeout = 30000;
		httpWebRequest.UserAgent = "Innertube Explorer v0.1";
		httpWebRequest.Headers.Add("version", Application.version);
		httpWebRequest.Headers.Add("platform", Application.platform.ToString());
		if (Singleton<PlayerInfoScript>.Instance != null)
		{
			httpWebRequest.Headers.Add("player-id", Singleton<PlayerInfoScript>.Instance.GetPlayerCode());
		}
		if (!string.IsNullOrEmpty(SQSettings.SERVER_URL) && uriaddress.StartsWith(SQSettings.SERVER_URL) && httpWebRequest.Headers["X-Nick-Description"] == null)
		{
			if (mDeviceHeader == null)
			{
				mDeviceHeader = WWW.EscapeURL(TFUtils.DeviceName ?? string.Empty).Replace("+", "%20");
			}
			httpWebRequest.Headers.Add("X-Nick-Description", mDeviceHeader);
			httpWebRequest.Headers.Add("X-Nick-Legacy", WWW.EscapeURL(TFUtils.LegacyDeviceName ?? string.Empty).Replace("+", "%20"));
		}
		if (!string.IsNullOrEmpty(SQSettings.SERVER_URL) && uriaddress.StartsWith(SQSettings.SERVER_URL) && httpWebRequest.Headers["X-Session"] == null)
		{
			httpWebRequest.Headers.Add("X-Session", PlayerInfoScript.LaunchSession);
		}
		ServicePoint servicePoint = ServicePointManager.FindServicePoint(address);
		servicePoint.Expect100Continue = false;
		servicePoint.ConnectionLimit = _maxConnections;
		return httpWebRequest;
	}

	protected override WebResponse GetWebResponse(WebRequest request)
	{
		try
		{
			return base.GetWebResponse(request);
		}
		catch (WebException ex)
		{
			Debug.LogWarning(string.Concat(request.RequestUri, ", Exception status: ", Enum.GetName(typeof(WebExceptionStatus), ex.Status)));
			this.NetworkError(this, ex);
			throw ex;
		}
	}
}
