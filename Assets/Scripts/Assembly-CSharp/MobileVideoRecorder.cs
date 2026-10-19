using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

/// <summary>
/// Sistema de grabación de video optimizado para Android/iOS
/// Usa MediaRecorder nativo de Android o ReplayKit de iOS
/// </summary>
public class MobileVideoRecorder : MonoBehaviour
{
	private static MobileVideoRecorder _instance;
	
	public static MobileVideoRecorder Instance
	{
		get
		{
			if (_instance == null)
			{
				GameObject recorderObj = new GameObject("MobileVideoRecorder");
				_instance = recorderObj.AddComponent<MobileVideoRecorder>();
				DontDestroyOnLoad(recorderObj);
			}
			return _instance;
		}
	}

	private bool isRecording = false;
	private string currentOutputPath;
	private string currentReplayID;
	private AndroidJavaObject screenRecorderNative;
	private bool useNativeRecording = true;
	private bool nativeRecordingActive = false;
	
	// Componentes de grabación (fallback a frames)
	private FrameCaptureRecorder frameCaptureRecorder;
	private AudioCaptureSession audioCaptureSession;
	private AudioCaptureTap audioCaptureTap;
	private AudioListener mainAudioListener;

	public bool IsRecording
	{
		get { return isRecording; }
	}

	private void OnEnable()
	{
		// Suscribirse para re-adjuntar AudioCaptureTap cuando la escena cambie.
		// El tap vive en el GameObject del AudioListener de la escena anterior y
		// se destruye cuando esa escena se descarga, dejando el audio truncado.
		SceneManager.sceneLoaded += OnSceneLoaded;
	}

	private void OnDisable()
	{
		SceneManager.sceneLoaded -= OnSceneLoaded;
	}

	private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		if (!isRecording || nativeRecordingActive) return;
		StartCoroutine(ReattachAudioTapAfterSceneLoad());
	}

	/// <summary>
	/// Después de un cambio de escena, busca el nuevo AudioListener y re-adjunta
	/// el AudioCaptureTap para que la captura de audio continúe sin interrupción.
	/// </summary>
	private IEnumerator ReattachAudioTapAfterSceneLoad()
	{
		// Esperar a que la escena inicialice su AudioListener
		yield return new WaitForEndOfFrame();
		yield return new WaitForEndOfFrame();

		AudioListener newListener = FindBestAudioListener();
		if (newListener == null)
		{
			Debug.LogWarning("[MobileVideoRecorder] ReattachAudioTap: no AudioListener found after scene load");
			yield break;
		}

		// Quitar tap obsoleto del listener anterior si aún existe
		if (audioCaptureTap != null && audioCaptureTap.gameObject != newListener.gameObject)
		{
			Destroy(audioCaptureTap);
			audioCaptureTap = null;
		}

		audioCaptureTap = newListener.gameObject.GetComponent<AudioCaptureTap>();
		if (audioCaptureTap == null)
			audioCaptureTap = newListener.gameObject.AddComponent<AudioCaptureTap>();

		audioCaptureTap.Session = audioCaptureSession;
		mainAudioListener = newListener;
		Debug.Log("[MobileVideoRecorder] AudioCaptureTap re-adjuntado tras cambio de escena: "
			+ newListener.gameObject.name);
	}

	/// <summary>
	/// Inicia grabación de video nativa
	/// </summary>
	public void StartRecording()
	{
		if (isRecording)
		{
			Debug.LogWarning("[MobileVideoRecorder] Already recording");
			return;
		}

#if UNITY_ANDROID && !UNITY_EDITOR
		StartCoroutine(StartAndroidRecording());
#elif UNITY_IOS && !UNITY_EDITOR
		StartIOSRecording();
#else
		Debug.LogWarning("[MobileVideoRecorder] Not on mobile platform, recording disabled");
#endif
	}

	/// <summary>
	/// Detiene grabación de video
	/// </summary>
	public void StopRecording()
	{
		if (!isRecording)
		{
			Debug.LogWarning("[MobileVideoRecorder] Not currently recording");
			return;
		}

#if UNITY_ANDROID && !UNITY_EDITOR
		StopAndroidRecording();
#elif UNITY_IOS && !UNITY_EDITOR
		StopIOSRecording();
#endif

		isRecording = false;
	}

#if UNITY_ANDROID && !UNITY_EDITOR
	private IEnumerator StartAndroidRecording()
	{
		Debug.Log("[MobileVideoRecorder] Starting Android recording...");
		
		// Obtener ReplayID
		if (Singleton<BattleReplayRecorder>.Instance != null)
		{
			currentReplayID = Singleton<BattleReplayRecorder>.Instance.GetLastReplayID();
			Debug.Log($"[MobileVideoRecorder] Got ReplayID: {currentReplayID}");
		}
		else
		{
			currentReplayID = "replay_" + DateTime.UtcNow.UnixTimestamp();
			Debug.LogWarning($"[MobileVideoRecorder] Using timestamp ReplayID: {currentReplayID}");
		}
		
		// Crear carpeta de salida
		string exportDirectory = Path.Combine(Application.persistentDataPath, "ExportedReplays");
		if (!Directory.Exists(exportDirectory))
		{
			Directory.CreateDirectory(exportDirectory);
		}
		
		string videoFolder = string.Format("Video_{0}", currentReplayID);
		currentOutputPath = Path.Combine(exportDirectory, videoFolder);
		
		if (!Directory.Exists(currentOutputPath))
		{
			Directory.CreateDirectory(currentOutputPath);
		}
		
		// INTENTAR GRABACIÓN NATIVA PRIMERO (MediaRecorder)
		bool nativeRecordingSuccess = false;
		bool hasPermission = false;
		AndroidJavaClass recorderClass = null;
		
		// Paso 1: Inicializar la clase y verificar permisos
		try
		{
			recorderClass = new AndroidJavaClass("com.cardwarskingdom.videoencoder.ScreenRecorderNative");
			hasPermission = recorderClass.CallStatic<bool>("hasPermission");
			
			if (!hasPermission)
			{
				Debug.Log("[MobileVideoRecorder] Requesting screen recording permission...");
				recorderClass.CallStatic("requestPermission");
			}
		}
		catch (Exception ex)
		{
			Debug.LogWarning("[MobileVideoRecorder] Native recording not available: " + ex.Message);
			recorderClass = null;
		}
		
		// Paso 2: Esperar permisos (fuera del try-catch para poder usar yield)
		if (recorderClass != null && !hasPermission)
		{
			float waitTime = 0f;
			while (!hasPermission && waitTime < 5f)
			{
				yield return new WaitForSeconds(0.1f);
				waitTime += 0.1f;
				
				try
				{
					hasPermission = recorderClass.CallStatic<bool>("hasPermission");
				}
				catch
				{
					// Si falla al verificar permisos, salir del loop
					break;
				}
			}
		}
		
		// Paso 3: Intentar iniciar la grabación nativa
		if (recorderClass != null && hasPermission)
		{
			try
			{
				// Usar grabación nativa de alta eficiencia
				Debug.Log("[MobileVideoRecorder] ✅ Using NATIVE screen recording (like built-in screen recorder)");
				
				string outputFile = Path.Combine(currentOutputPath, "replay_final.mp4");
				
				// Configurar parámetros (resolución, fps, bitrate)
				int width = Screen.width;
				int height = Screen.height;
				int fps = 30;
				int bitrate = 6000000; // 6 Mbps
				
				recorderClass.CallStatic("setRecordingParams", width, height, fps, bitrate);
				
				// Iniciar grabación
				bool started = recorderClass.CallStatic<bool>("startRecording", outputFile);
				
				if (started)
				{
					nativeRecordingActive = true;
					isRecording = true;
					nativeRecordingSuccess = true;
					Debug.Log("[MobileVideoRecorder] ✅ Native recording started! Zero performance impact.");
					Debug.Log("[MobileVideoRecorder] Output will be: " + outputFile);
				}
				else
				{
					Debug.LogWarning("[MobileVideoRecorder] Native recording failed to start");
				}
			}
			catch (Exception ex)
			{
				Debug.LogWarning("[MobileVideoRecorder] Error starting native recording: " + ex.Message);
			}
			finally
			{
				if (recorderClass != null)
				{
					recorderClass.Dispose();
				}
			}
		}
		else if (recorderClass == null)
		{
			Debug.LogWarning("[MobileVideoRecorder] Native recording class not available");
		}
		else if (!hasPermission)
		{
			Debug.LogWarning("[MobileVideoRecorder] Screen recording permission denied or not granted in time");
			if (recorderClass != null)
			{
				recorderClass.Dispose();
			}
		}
		
		// Si la grabación nativa comenzó exitosamente, salir
		if (nativeRecordingSuccess)
		{
			yield break;
		}
		
		// FALLBACK: Usar sistema de frames (como antes)
		Debug.Log("[MobileVideoRecorder] Using FALLBACK frame capture system");
		nativeRecordingActive = false;
		yield return StartCoroutine(StartMobileRecording());
	}

	private void StopAndroidRecording()
	{
		Debug.Log("[MobileVideoRecorder] Stopping Android recording...");
		
		// Si está usando grabación nativa, detenerla
		if (nativeRecordingActive)
		{
			try
			{
				using (AndroidJavaClass recorderClass = new AndroidJavaClass("com.cardwarskingdom.videoencoder.ScreenRecorderNative"))
				{
					bool stopped = recorderClass.CallStatic<bool>("stopRecording");
					
					if (stopped)
					{
						string outputPath = recorderClass.CallStatic<string>("getOutputPath");
						Debug.Log("[MobileVideoRecorder] ✅ Native recording stopped successfully!");
						Debug.Log("[MobileVideoRecorder] Video saved at: " + outputPath);
						
						// Verificar que el archivo existe
						if (File.Exists(outputPath))
						{
							FileInfo fi = new FileInfo(outputPath);
							Debug.Log($"[MobileVideoRecorder] Video size: {fi.Length / 1024 / 1024}MB");
							ShowSuccessMessage("Battle replay saved!\n\nNative recording: replay_final.mp4");
						}
						else
						{
							Debug.LogError("[MobileVideoRecorder] Video file not found!");
						}
					}
					else
					{
						Debug.LogError("[MobileVideoRecorder] Failed to stop native recording");
					}
				}
				
				nativeRecordingActive = false;
			}
			catch (Exception ex)
			{
				Debug.LogError("[MobileVideoRecorder] Error stopping native recording: " + ex.Message);
				nativeRecordingActive = false;
			}
			
			RefreshMatchHistoryButtons();
		}
		else
		{
			// Usar el sistema de frames
			StopMobileRecording();
		}
	}
#endif

	/// <summary>
	/// Sistema de grabación universal para móviles (Android/iOS)
	/// Usa FrameCaptureRecorder + AudioCaptureSession
	/// </summary>
	private IEnumerator StartMobileRecording()
	{
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
		// Verificar y solicitar permisos (Android)
#if UNITY_ANDROID
		// Android 13+ (API 33+) no usa WRITE_EXTERNAL_STORAGE
		// Los archivos de la app en persistentDataPath NO requieren permisos especiales
		// Solo mostramos un log informativo
		
		int sdkVersion = 0;
		using (AndroidJavaClass versionClass = new AndroidJavaClass("android.os.Build$VERSION"))
		{
			sdkVersion = versionClass.GetStatic<int>("SDK_INT");
		}
		
		Debug.Log($"[MobileVideoRecorder] Android SDK: {sdkVersion}");
		
		if (sdkVersion < 33)
		{
			// Android 12 y anteriores: Solicitar permisos de almacenamiento
			if (!Permission.HasUserAuthorizedPermission(Permission.ExternalStorageWrite))
			{
				Debug.Log("[MobileVideoRecorder] Requesting storage permission (Android < 13)");
				Permission.RequestUserPermission(Permission.ExternalStorageWrite);
				yield return new WaitForSeconds(1f);
				
				if (!Permission.HasUserAuthorizedPermission(Permission.ExternalStorageWrite))
				{
					Debug.LogError("[MobileVideoRecorder] Storage permission denied");
					yield break;
				}
			}
		}
		else
		{
			// Android 13+: No se necesitan permisos para persistentDataPath
			Debug.Log("[MobileVideoRecorder] Android 13+: Using app-specific storage (no permissions needed)");
		}

		// Micrófono es opcional
		if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
		{
			Debug.Log("[MobileVideoRecorder] Requesting microphone permission");
			Permission.RequestUserPermission(Permission.Microphone);
			yield return new WaitForSeconds(1f);
			
			if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
			{
				Debug.LogWarning("[MobileVideoRecorder] Microphone permission denied (audio recording may not work)");
			}
		}
#endif

		// Obtener ReplayID del recorder
		if (Singleton<BattleReplayRecorder>.Instance != null)
		{
			currentReplayID = Singleton<BattleReplayRecorder>.Instance.GetLastReplayID();
			Debug.Log($"[MobileVideoRecorder] Got ReplayID from BattleReplayRecorder: {currentReplayID}");
		}
		else
		{
			currentReplayID = "replay_" + DateTime.UtcNow.UnixTimestamp();
			Debug.LogWarning($"[MobileVideoRecorder] BattleReplayRecorder not found, using timestamp: {currentReplayID}");
		}

		// Crear ruta de salida
		string exportDirectory = Path.Combine(Application.persistentDataPath, "ExportedReplays");
		Debug.Log($"[MobileVideoRecorder] persistentDataPath: {Application.persistentDataPath}");
		Debug.Log($"[MobileVideoRecorder] Export directory: {exportDirectory}");
		
		if (!Directory.Exists(exportDirectory))
		{
			try
			{
				Directory.CreateDirectory(exportDirectory);
				Debug.Log("[MobileVideoRecorder] Created export directory");
			}
			catch (Exception ex)
			{
				Debug.LogError($"[MobileVideoRecorder] Failed to create export directory: {ex.Message}");
				yield break;
			}
		}
		else
		{
			Debug.Log("[MobileVideoRecorder] Export directory already exists");
		}

		string videoFolder = string.Format("Video_{0}", currentReplayID);
		currentOutputPath = Path.Combine(exportDirectory, videoFolder);
		Debug.Log($"[MobileVideoRecorder] Video output path: {currentOutputPath}");
		
		if (!Directory.Exists(currentOutputPath))
		{
			try
			{
				Directory.CreateDirectory(currentOutputPath);
				Debug.Log("[MobileVideoRecorder] Created video folder");
			}
			catch (Exception ex)
			{
				Debug.LogError($"[MobileVideoRecorder] Failed to create video folder: {ex.Message}");
				yield break;
			}
		}
		else
		{
			Debug.Log("[MobileVideoRecorder] Video folder already exists");
		}

		Debug.Log("[MobileVideoRecorder] Using FrameCapture + Audio system (compatible with all platforms)");
		
		// Esperar unos frames para que la escena esté lista
		yield return null;
		yield return null;
		
		// PASO 1: Encontrar AudioListener
		mainAudioListener = FindBestAudioListener();
		if (mainAudioListener != null)
		{
			mainAudioListener.enabled = true;
			AudioListener.pause = false;
			AudioListener.volume = 1.0f;
			Debug.Log("[MobileVideoRecorder] Found AudioListener on: " + mainAudioListener.gameObject.name);
		}
		else
		{
			Debug.LogWarning("[MobileVideoRecorder] No AudioListener found. Audio will not be recorded.");
		}
		
		// PASO 2: Iniciar captura de audio
		audioCaptureSession = gameObject.GetComponent<AudioCaptureSession>();
		if (audioCaptureSession == null)
		{
			audioCaptureSession = gameObject.AddComponent<AudioCaptureSession>();
		}
		
		string audioFile = Path.Combine(currentOutputPath, "replay_audio.wav");
		audioCaptureSession.StartCapture(audioFile);
		Debug.Log("[MobileVideoRecorder] Audio capture started: " + audioFile);
		
		// Adjuntar tap al listener
		if (mainAudioListener != null)
		{
			audioCaptureTap = mainAudioListener.gameObject.GetComponent<AudioCaptureTap>();
			if (audioCaptureTap == null)
			{
				audioCaptureTap = mainAudioListener.gameObject.AddComponent<AudioCaptureTap>();
			}
			audioCaptureTap.Session = audioCaptureSession;
		}
		
		// PASO 3: Iniciar captura de frames
		frameCaptureRecorder = gameObject.GetComponent<FrameCaptureRecorder>();
		if (frameCaptureRecorder == null)
		{
			frameCaptureRecorder = gameObject.AddComponent<FrameCaptureRecorder>();
		}
		
		frameCaptureRecorder.StartCapture(currentOutputPath);
		Debug.Log("[MobileVideoRecorder] Frame capture started (lightweight, no performance impact)");
		
		isRecording = true;
		Debug.Log("[MobileVideoRecorder] ✅ Recording started successfully: " + currentOutputPath);
#else
		yield return null;
		Debug.LogWarning("[MobileVideoRecorder] Not on mobile platform");
#endif
	}

	/// <summary>
	/// Detiene la grabación móvil (Android/iOS)
	/// </summary>
	private void StopMobileRecording()
	{
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
		Debug.Log("[MobileVideoRecorder] Stopping recording...");
		
		// Detener captura de audio
		if (audioCaptureSession != null && audioCaptureSession.IsCapturing)
		{
			audioCaptureSession.StopCapture();
			Debug.Log("[MobileVideoRecorder] Audio capture stopped");
		}
		
		// Detener captura de frames
		if (frameCaptureRecorder != null && frameCaptureRecorder.IsCapturing)
		{
			frameCaptureRecorder.StopCapture();
			Debug.Log(string.Format("[MobileVideoRecorder] Frame capture stopped ({0} frames)", 
				frameCaptureRecorder.GetFrameCount()));
			
			// Combinar frames + audio en background
			string audioPath = Path.Combine(currentOutputPath, "replay_audio.wav");
			if (File.Exists(audioPath))
			{
				Debug.Log("[MobileVideoRecorder] 🎬 Combining frames with audio in background...");
				frameCaptureRecorder.CombineFramesWithAudio(audioPath, (success, message) =>
				{
					if (success)
					{
						Debug.Log("[MobileVideoRecorder] ✅ Video created successfully!");
						Debug.Log("[MobileVideoRecorder] Final video: " + message);
						
						// Mostrar mensaje de éxito al usuario
						ShowSuccessMessage("Battle replay saved!\n\nVideo: replay_final.mp4");
					}
					else
					{
						Debug.LogWarning("[MobileVideoRecorder] ⚠️ Could not create video: " + message);
						Debug.Log("[MobileVideoRecorder] Frames saved in: " + Path.Combine(currentOutputPath, "frames"));
						
						// Mostrar mensaje parcial
						ShowSuccessMessage("Battle replay saved!\n\nFrames captured, but video encoding failed.\nCheck logs for details.");
					}
					
					// Refrescar botones de historial
					RefreshMatchHistoryButtons();
				});
			}
			else
			{
				Debug.LogWarning("[MobileVideoRecorder] Audio file not found: " + audioPath);
				ShowSuccessMessage("Battle replay saved!\n\nVideo frames saved (no audio).");
				RefreshMatchHistoryButtons();
			}
		}
		
		// Mantener audio activo
		if (mainAudioListener != null)
		{
			mainAudioListener.enabled = true;
		}
		AudioListener.pause = false;
		AudioListener.volume = 1.0f;
		
		Debug.Log("[MobileVideoRecorder] Recording stopped: " + currentOutputPath);
#endif
	}

#if UNITY_IOS && !UNITY_EDITOR
private void StartIOSRecording()
{
    Debug.Log("[MobileVideoRecorder] iOS using FrameCapture + Audio system");
    StartCoroutine(StartMobileRecording());
}

private void StopIOSRecording()
{
    Debug.Log("[MobileVideoRecorder] Stopping iOS recording");
    StopMobileRecording();
}
#endif

	/// <summary>
	/// Busca el mejor AudioListener disponible en la escena
	/// </summary>
	private AudioListener FindBestAudioListener()
	{
		AudioListener[] listeners = FindObjectsOfType<AudioListener>();
		AudioListener fallback = null;
		
		for (int i = 0; i < listeners.Length; i++)
		{
			AudioListener l = listeners[i];
			if (l == null)
				continue;

			if (fallback == null)
				fallback = l;

			if (!l.gameObject.activeInHierarchy)
				continue;

			if (!l.enabled)
				continue;

			Camera cam = l.GetComponent<Camera>();
			if (cam != null && cam.enabled)
			{
				return l;
			}
		}

		// Si no hay listener activo con cámara, retornar cualquier listener activo
		for (int i = 0; i < listeners.Length; i++)
		{
			AudioListener l = listeners[i];
			if (l != null && l.gameObject.activeInHierarchy && l.enabled)
				return l;
		}

		// Si ninguno está habilitado, retornar uno activo en jerarquía
		for (int i = 0; i < listeners.Length; i++)
		{
			AudioListener l = listeners[i];
			if (l != null && l.gameObject.activeInHierarchy)
				return l;
		}

		return fallback;
	}

	/// <summary>
	/// Sistema de respaldo: Graba frames y usa encoder de Unity
	/// </summary>
	private IEnumerator FallbackFrameRecording()
	{
		Debug.Log("[MobileVideoRecorder] Using fallback frame recording");
		
		// Obtener ReplayID
		if (Singleton<BattleReplayRecorder>.Instance != null)
		{
			currentReplayID = Singleton<BattleReplayRecorder>.Instance.GetLastReplayID();
		}
		else
		{
			currentReplayID = "replay_" + DateTime.UtcNow.UnixTimestamp();
		}

		// Crear ruta de salida
		string exportDirectory = Path.Combine(Application.persistentDataPath, "ExportedReplays");
		if (!Directory.Exists(exportDirectory))
		{
			Directory.CreateDirectory(exportDirectory);
		}

		string videoFolder = string.Format("Video_{0}", currentReplayID);
		currentOutputPath = Path.Combine(exportDirectory, videoFolder);
		
		if (!Directory.Exists(currentOutputPath))
		{
			Directory.CreateDirectory(currentOutputPath);
		}

		List<Texture2D> frames = new List<Texture2D>();
		float frameTime = 0f;
		int targetFrameRate = 30;

		isRecording = true;

		while (isRecording)
		{
			frameTime += Time.deltaTime;
			
			if (frameTime >= 1f / targetFrameRate)
			{
				frameTime = 0f;
				
				yield return new WaitForEndOfFrame();
				
				Texture2D frame = CaptureFrame();
				if (frame != null)
				{
					frames.Add(frame);
				}
			}
			
			yield return null;
		}

		// Guardar frames
		Debug.Log("[MobileVideoRecorder] Saving " + frames.Count + " frames");
		
		for (int i = 0; i < frames.Count; i++)
		{
			string framePath = Path.Combine(currentOutputPath, string.Format("frame_{0:D6}.png", i));
			byte[] bytes = frames[i].EncodeToPNG();
			File.WriteAllBytes(framePath, bytes);
			
			Destroy(frames[i]);
			
			if (i % 10 == 0)
			{
				yield return null;
			}
		}

		frames.Clear();

		// Guardar información
		string infoPath = Path.Combine(currentOutputPath, "info.txt");
		string info = string.Format(
			"Frame Count: {0}\nFrame Rate: {1} fps\nResolution: {2}x{3}\nPlatform: {4}\nExport Date: {5}\nReplay ID: {6}",
			frames.Count,
			targetFrameRate,
			Screen.width,
			Screen.height,
			Application.platform,
			DateTime.Now.ToString(),
			currentReplayID
		);
		File.WriteAllText(infoPath, info);

		Debug.Log("[MobileVideoRecorder] Frames saved successfully to: " + currentOutputPath);
		
		// Intentar codificar a MP4 usando Unity Recorder API si está disponible
		yield return StartCoroutine(TryEncodeWithUnityRecorder());
		
		RefreshMatchHistoryButtons();
		ShowSuccessMessage();
	}

	private Texture2D CaptureFrame()
	{
		try
		{
			int width = Screen.width;
			int height = Screen.height;
			Texture2D screenshot = new Texture2D(width, height, TextureFormat.RGB24, false);
			screenshot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
			screenshot.Apply();
			return screenshot;
		}
		catch (Exception ex)
		{
			Debug.LogError("[MobileVideoRecorder] Error capturing frame: " + ex.Message);
			return null;
		}
	}

	/// <summary>
	/// Intenta usar Unity Recorder API para encoder
	/// </summary>
	private IEnumerator TryEncodeWithUnityRecorder()
	{
		Debug.Log("[MobileVideoRecorder] Checking for Unity Recorder API...");
		
		// Unity Recorder requiere el paquete instalado
		// Esta es una implementación placeholder
		
		yield return null;
		
		Debug.Log("[MobileVideoRecorder] Unity Recorder not available, frames saved as PNG");
	}

	private void RefreshMatchHistoryButtons()
	{
		if (BattleHistoryController.Instance != null)
		{
			BattleHistoryController.Instance.RefreshAllReplayButtons();
			Debug.Log("[MobileVideoRecorder] Match history buttons refreshed");
		}
	}

	private void ShowSuccessMessage(string customMessage = null)
	{
		string message = customMessage ?? ("Battle replay recorded!\n\nLocation: " + currentOutputPath);
		
		if (Singleton<SimplePopupController>.Instance != null)
		{
			Singleton<SimplePopupController>.Instance.ShowMessage(
				"Recording Complete",
				message
			);
		}
		else
		{
			Debug.Log("[MobileVideoRecorder] " + message);
		}
	}

	/// <summary>
	/// Obtiene la ruta del video para un replayID
	/// </summary>
	public string GetVideoPath(string replayID)
	{
		if (string.IsNullOrEmpty(replayID))
			return null;
			
		string exportDirectory = Path.Combine(Application.persistentDataPath, "ExportedReplays");
		string videoFolder = string.Format("Video_{0}", replayID);
		string folderPath = Path.Combine(exportDirectory, videoFolder);
		if (!Directory.Exists(folderPath))
			return null;

		// Prefer returning the final MP4 file path when available.
		// FrameCaptureRecorder genera "replay_final.mp4"; también soportamos "replay.mp4" (nativo).
		string[] mp4Candidates = { "replay_final.mp4", "replay.mp4" };
		foreach (string candidate in mp4Candidates)
		{
			string mp4 = Path.Combine(folderPath, candidate);
			if (File.Exists(mp4) && new System.IO.FileInfo(mp4).Length > 1024)
				return mp4;
		}

		// Fallback: folder path (frames disponibles, sin MP4 final)
		return folderPath;
	}

	/// <summary>
	/// Verifica si existe un video para un replayID
	/// </summary>
	public bool VideoExists(string replayID)
	{
		if (string.IsNullOrEmpty(replayID)) return false;

		string exportDirectory = Path.Combine(Application.persistentDataPath, "ExportedReplays");
		string folderPath = Path.Combine(exportDirectory, string.Format("Video_{0}", replayID));
		if (!Directory.Exists(folderPath)) return false;

		// Verificar si existe MP4 final válido
		string[] mp4Candidates = { "replay_final.mp4", "replay.mp4" };
		foreach (string candidate in mp4Candidates)
		{
			string mp4 = Path.Combine(folderPath, candidate);
			if (File.Exists(mp4) && new System.IO.FileInfo(mp4).Length > 1024)
				return true;
		}

		// Verificar si hay frames JPEG o PNG (grabación parcial)
		if (Directory.GetFiles(folderPath, "frame_*.jpg").Length > 0) return true;
		if (Directory.GetFiles(folderPath, "frame_*.png").Length > 0) return true;
		return false;
	}
}
