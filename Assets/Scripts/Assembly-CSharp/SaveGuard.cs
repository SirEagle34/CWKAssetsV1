using System;
using System.IO;
using UnityEngine;

public static class SaveGuard
{
	public static string MarkerFile;

	public static bool Pending
	{
		get
		{
			try
			{
				return MarkerFile != null && File.Exists(MarkerFile);
			}
			catch (Exception)
			{
				return false;
			}
		}
	}

	public static void Record(bool success, int status)
	{
		bool flag = !success && status != 403 && status != 426 && status != 400 && status != 404 && status != 413 && status != 412 && status != 423 && status != 409;
		try
		{
			if (MarkerFile != null && flag != File.Exists(MarkerFile))
			{
				if (flag)
				{
					File.WriteAllText(MarkerFile, DateTime.UtcNow.ToString("o"));
					Debug.LogWarning("[SaveGuard] save not uploaded (status " + status + "), will retry");
				}
				else
				{
					File.Delete(MarkerFile);
					Debug.Log("[SaveGuard] pending save cleared (status " + status + ")");
				}
			}
		}
		catch (Exception ex)
		{
			Debug.LogWarning("[SaveGuard] " + ex.Message);
		}
	}
}
