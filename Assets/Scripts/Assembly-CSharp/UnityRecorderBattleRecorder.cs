using System;
using System.Collections;
using System.IO;
using UnityEngine;

/// <summary>
/// Grabador de batallas usando Unity Recorder Package
/// Genera MP4 directamente sin necesidad de FFmpeg
/// REQUIERE: Unity Recorder package instalado via Package Manager
/// </summary>
public class UnityRecorderBattleRecorder : MonoBehaviour
{
    private static UnityRecorderBattleRecorder _instance;
    
    public static UnityRecorderBattleRecorder Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject recorderObj = new GameObject("UnityRecorderBattleRecorder");
                _instance = recorderObj.AddComponent<UnityRecorderBattleRecorder>();
                DontDestroyOnLoad(recorderObj);
            }
            return _instance;
        }
    }

#if UNITY_EDITOR
    private object recorderController;
    private object movieRecorderSettings;
#endif
    
    private bool isRecording = false;
    private string currentReplayID;
    private string currentOutputPath;
    private AudioListener mainAudioListener; // Referencia al AudioListener existente
    private AudioCaptureSession audioCaptureSession; // Captura de audio separada (persistente)
    private AudioCaptureTap audioCaptureTap; // Tap adjunto al AudioListener activo
    private FrameCaptureRecorder frameCaptureRecorder; // Captura de frames (ligera)
    
    // ⚠️ CONFIGURACIÓN CRÍTICA DE RENDIMIENTO ⚠️
    // Unity Recorder afecta MUCHO el rendimiento (5x más lento)
    // Alternativa: Frame Capture System (capturas frames rápido, combina después)
    
    [SerializeField]
    [Tooltip("Deshabilita TODA la grabación (video y audio) para máximo rendimiento")]
    private bool disableAllRecording = false; // FALSE = Audio se graba | TRUE = Nada se graba
    
    [SerializeField]
    [Tooltip("Captura frames + audio (RÁPIDO, combina después) | Unity Recorder (LENTO, graba directo)")]
    private bool useFrameCaptureSystem = true; // TRUE = Rápido (RECOMENDADO) | FALSE = Unity Recorder
    
    [SerializeField]
    [Tooltip("Solo si useFrameCaptureSystem = false: Usar Unity Recorder (LENTO)")]
    private bool enableVideoRecording = false; // FALSE = Rendimiento normal cuando useFrameCaptureSystem = false
    
    [SerializeField]
    [Tooltip("Solo relevante si enableVideoRecording = true")]
    private bool preserveAudioForVideo = false; // Debe ser false para captura separada

    public bool IsRecording
    {
        get { return isRecording; }
    }

    private void OnDestroy()
    {
        // Limpiar referencias
        mainAudioListener = null;
        
        // Limpiar capturador de audio si existe
        if (audioCaptureSession != null)
        {
            Destroy(audioCaptureSession);
            audioCaptureSession = null;
        }

        if (audioCaptureTap != null)
        {
            Destroy(audioCaptureTap);
            audioCaptureTap = null;
        }
    }

    private float audioCheckTimer = 0f;
    private const float AUDIO_CHECK_INTERVAL = 0.5f; // Verificar audio cada 0.5 segundos en lugar de cada frame
    
    private void Update()
    {
        #if UNITY_EDITOR
        // Monitorear audio mientras grabamos (frame-capture o Unity Recorder)
        if (isRecording)
        {
            audioCheckTimer += Time.deltaTime;
            if (audioCheckTimer >= AUDIO_CHECK_INTERVAL)
            {
                audioCheckTimer = 0f;

                AudioListener bestListener = FindBestAudioListener();
                if (bestListener != null)
                {
                    mainAudioListener = bestListener;
                    // Mantener activo el listener principal (si se desactiva, el audio capturado se corta)
                    if (!mainAudioListener.enabled)
                    {
                        mainAudioListener.enabled = true;
                        Debug.LogWarning("[UnityRecorderBattleRecorder] Re-enabled main AudioListener during recording");
                    }

                    // Evitar duplicados: desactivar otros listeners habilitados
                    AudioListener[] listeners = FindObjectsOfType<AudioListener>();
                    for (int i = 0; i < listeners.Length; i++)
                    {
                        AudioListener l = listeners[i];
                        if (l != null && l != mainAudioListener && l.enabled && l.gameObject.activeInHierarchy)
                        {
                            l.enabled = false;
                        }
                    }

                    // Asegurar que el tap está en el listener actual
                    EnsureAudioTapAttached(mainAudioListener);
                }

                AudioListener.pause = false;
                AudioListener.volume = 1.0f;
            }
        }
        #endif
    }

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

		// If no enabled+active listener with enabled camera, return any enabled+active listener
		for (int i = 0; i < listeners.Length; i++)
		{
			AudioListener l = listeners[i];
			if (l != null && l.gameObject.activeInHierarchy && l.enabled)
				return l;
		}

        // If none are enabled, prefer an active-in-hierarchy listener (we can enable it)
        for (int i = 0; i < listeners.Length; i++)
        {
            AudioListener l = listeners[i];
            if (l != null && l.gameObject.activeInHierarchy)
                return l;
        }

		return fallback;
	}

	private void EnsureAudioTapAttached(AudioListener listener)
	{
		if (listener == null)
			return;

		if (audioCaptureSession == null)
			return;

		AudioCaptureTap existingTap = listener.gameObject.GetComponent<AudioCaptureTap>();
		if (existingTap == null)
		{
			existingTap = listener.gameObject.AddComponent<AudioCaptureTap>();
		}
		existingTap.Session = audioCaptureSession;
		audioCaptureTap = existingTap;
	}

    /// <summary>
    /// Inicia grabación de batalla
    /// </summary>
    public void StartBattleRecording()
    {
#if !UNITY_EDITOR
        Debug.LogWarning("[UnityRecorderBattleRecorder] Unity Recorder only works in Editor. Use MobileVideoRecorder for builds.");
        return;
#else
        // Verificar si todo el sistema está deshabilitado
        if (disableAllRecording)
        {
            Debug.Log("[UnityRecorderBattleRecorder] ⚠️ All recording DISABLED - Maximum performance mode");
            return;
        }
        
        if (isRecording)
        {
            Debug.LogWarning("[UnityRecorderBattleRecorder] Already recording");
            return;
        }

        try
        {
            // Obtener ReplayID
            if (Singleton<BattleReplayRecorder>.Instance != null)
            {
                currentReplayID = Singleton<BattleReplayRecorder>.Instance.GetLastReplayID();
            }
            else
            {
                currentReplayID = "replay_" + DateTime.UtcNow.UnixTimestamp();
            }

            // Crear directorio de salida
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

            // Configurar sistema de grabación según preferencias
            if (useFrameCaptureSystem)
            {
                // Sistema de captura de frames (RÁPIDO)
                Debug.Log("[UnityRecorderBattleRecorder] ✅ Using Frame Capture System - Game at normal speed");
                Debug.Log("[UnityRecorderBattleRecorder] Video will be created in background after battle");
            }
            else if (enableVideoRecording)
            {
                // Unity Recorder (LENTO)
                if (!TrySetupUnityRecorder())
                {
                    Debug.LogWarning("[UnityRecorderBattleRecorder] Unity Recorder package not found. Falling back to Frame Capture System.");
                    useFrameCaptureSystem = true;
                    enableVideoRecording = false;
                }
                Debug.Log("[UnityRecorderBattleRecorder] ⚠️ Video recording ENABLED - Game will be SLOWER");
            }
            else
            {
                // Solo audio
                Debug.Log("[UnityRecorderBattleRecorder] ✅ Audio-only mode - Game at normal speed");
                Debug.Log("[UnityRecorderBattleRecorder] Use OBS/Game Bar for video.");
            }
            
            // Iniciar grabación con manejo de audio simplificado
            StartCoroutine(InitializeAudioAndStartRecording());
            
        }
        catch (Exception ex)
        {
            Debug.LogError("[UnityRecorderBattleRecorder] Failed to start recording: " + ex.Message);
            isRecording = false;
        }
#endif
    }

#if UNITY_EDITOR
    /// <summary>
    /// Corrutina SIMPLIFICADA para inicializar audio y grabación
    /// Usa el AudioListener existente sin crear nuevos componentes
    /// </summary>
    private IEnumerator InitializeAudioAndStartRecording()
    {
        // Esperar unos frames para que la escena esté lista
        Debug.Log("[UnityRecorderBattleRecorder] Waiting for scene to load...");
        yield return null;
        yield return null;
        
        // PASO 1: Encontrar el AudioListener principal (que ya existe)
        Debug.Log("[UnityRecorderBattleRecorder] Step 1: Finding existing AudioListener...");
        mainAudioListener = FindBestAudioListener();
        
        if (mainAudioListener == null)
        {
            Debug.LogError("[UnityRecorderBattleRecorder] ❌ No AudioListener found in scene! Audio will not work.");
            yield break;
        }
        
        Debug.Log(string.Format(
            "[UnityRecorderBattleRecorder] ✅ Found AudioListener on: {0}",
            mainAudioListener.gameObject.name
        ));
        
        // PASO 2: Asegurar que el AudioListener existente esté activo
        Debug.Log("[UnityRecorderBattleRecorder] Step 2: Ensuring AudioListener is active...");
        mainAudioListener.enabled = true;
        AudioListener.pause = false;
        AudioListener.volume = 1.0f;
		
        // Desactivar otros listeners para evitar duplicados
        AudioListener[] listeners = FindObjectsOfType<AudioListener>();
        for (int i = 0; i < listeners.Length; i++)
        {
            AudioListener l = listeners[i];
            if (l != null && l != mainAudioListener && l.enabled)
            {
                l.enabled = false;
            }
        }
        
        // PASO 3: Configurar volúmenes del juego
        Debug.Log("[UnityRecorderBattleRecorder] Step 3: Setting audio volumes...");
        EnsureAudioPlayerPrefs();
        
        try
        {
            if (Singleton<SLOTAudioManager>.Instance != null)
            {
                var audioManager = Singleton<SLOTAudioManager>.Instance;
                audioManager.soundVolume = 1.0f;
                audioManager.voVolume = 1.0f;
                audioManager.musicVolume = 1.0f;
                audioManager.UpdateAudioVolumes();
                Debug.Log("[UnityRecorderBattleRecorder] ✅ All audio volumes set to 1.0");
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[UnityRecorderBattleRecorder] Could not set audio manager volumes: " + ex.Message);
        }
        
        // PASO 3.5: Iniciar captura de audio separada
        Debug.Log("[UnityRecorderBattleRecorder] Step 3.5: Starting audio capture...");
        // Crear una sesión persistente en ESTE GameObject (no depende de qué cámara tenga el AudioListener)
        audioCaptureSession = gameObject.GetComponent<AudioCaptureSession>();
        if (audioCaptureSession == null)
        {
            audioCaptureSession = gameObject.AddComponent<AudioCaptureSession>();
        }

        string audioFile = Path.Combine(currentOutputPath, "replay_audio.wav");
        audioCaptureSession.StartCapture(audioFile);
        Debug.Log("[UnityRecorderBattleRecorder] ✅ Audio session started: " + audioFile);

        // Adjuntar tap al listener actual (y se re-adjunta en Update si cambia de cámara)
        EnsureAudioTapAttached(mainAudioListener);
        
        // PASO 3.6: Iniciar captura de frames (si está habilitado)
        if (useFrameCaptureSystem)
        {
            Debug.Log("[UnityRecorderBattleRecorder] Step 3.6: Starting frame capture...");
            
            // Crear FrameCaptureRecorder
            frameCaptureRecorder = gameObject.GetComponent<FrameCaptureRecorder>();
            if (frameCaptureRecorder == null)
            {
                frameCaptureRecorder = gameObject.AddComponent<FrameCaptureRecorder>();
            }
            
            frameCaptureRecorder.StartCapture(currentOutputPath);
            Debug.Log("[UnityRecorderBattleRecorder] ✅ Frame capture started (lightweight)");
        }
        
        // PASO 4: Iniciar grabación de video con Unity Recorder (solo si está habilitado y NO estamos usando frame capture)
        if (!useFrameCaptureSystem && enableVideoRecording)
        {
            Debug.Log("[UnityRecorderBattleRecorder] Step 4: Starting Unity Recorder video recording...");
            
            try
            {
                if (!TryPrepareUnityRecorderRecording())
                {
                    Debug.LogWarning("[UnityRecorderBattleRecorder] Unity Recorder not available. Switching to Frame Capture System.");
                    useFrameCaptureSystem = true;
                    enableVideoRecording = false;

                    // Start frame capture as fallback.
                    frameCaptureRecorder = gameObject.GetComponent<FrameCaptureRecorder>();
                    if (frameCaptureRecorder == null)
                    {
                        frameCaptureRecorder = gameObject.AddComponent<FrameCaptureRecorder>();
                    }
                    frameCaptureRecorder.StartCapture(currentOutputPath);
                }
                else
                {
                    Debug.Log("[UnityRecorderBattleRecorder] ✅ Unity Recorder started (video will be slower)");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("[UnityRecorderBattleRecorder] ❌ Failed to start recording: " + ex.Message);
                Debug.LogWarning("[UnityRecorderBattleRecorder] Falling back to Frame Capture System.");
                useFrameCaptureSystem = true;
                enableVideoRecording = false;

                frameCaptureRecorder = gameObject.GetComponent<FrameCaptureRecorder>();
                if (frameCaptureRecorder == null)
                {
                    frameCaptureRecorder = gameObject.AddComponent<FrameCaptureRecorder>();
                }
                frameCaptureRecorder.StartCapture(currentOutputPath);
            }
        }
        else
        {
            Debug.Log("[UnityRecorderBattleRecorder] Step 4: Skipping video recording (disabled for performance)");
        }
        
        // PASO 5: Verificar que el audio sigue activo después de iniciar grabación
        yield return null;
        
        if (mainAudioListener != null && !mainAudioListener.enabled)
        {
            mainAudioListener.enabled = true;
            Debug.LogWarning("[UnityRecorderBattleRecorder] Re-enabled AudioListener after recording started");
        }
        
        AudioListener.pause = false;
        AudioListener.volume = 1.0f;
        
        isRecording = true;
        Debug.Log("[UnityRecorderBattleRecorder] ✅ Recording started successfully: " + currentOutputPath);
        Debug.Log("[UnityRecorderBattleRecorder] AudioListener: " + mainAudioListener.gameObject.name + " (enabled=" + mainAudioListener.enabled + ")");
        
        if (useFrameCaptureSystem)
        {
            Debug.Log("[UnityRecorderBattleRecorder] 🎬 Frame capture + Audio recording active (FAST)");
            Debug.Log("[UnityRecorderBattleRecorder] 💡 Video will be created in background after battle");
        }
        else if (enableVideoRecording)
        {
            if (!preserveAudioForVideo && audioCaptureSession != null)
            {
                Debug.Log("[UnityRecorderBattleRecorder] 🎤 Video + Audio capture active (game will be slower)");
            }
        }
        else
        {
            Debug.Log("[UnityRecorderBattleRecorder] 🎤 Audio-only capture active (game at normal speed)");
            Debug.Log("[UnityRecorderBattleRecorder] 💡 Use OBS Studio or Game Bar (Win+G) to record video");
        }
    }
#endif

    /// <summary>
    /// Detiene grabación de batalla
    /// </summary>
    public void StopBattleRecording()
    {
#if !UNITY_EDITOR
        return;
#else
        if (!isRecording)
        {
            Debug.LogWarning("[UnityRecorderBattleRecorder] Not currently recording");
            return;
        }

        try
        {
            // Detener captura de audio separada si existe
            if (audioCaptureSession != null && audioCaptureSession.IsCapturing)
            {
                Debug.Log("[UnityRecorderBattleRecorder] Stopping audio capture...");
                audioCaptureSession.StopCapture();
                Debug.Log("[UnityRecorderBattleRecorder] ✅ Audio capture stopped and saved");
            }
            
            // Detener captura de frames si está activo
            if (useFrameCaptureSystem && frameCaptureRecorder != null && frameCaptureRecorder.IsCapturing)
            {
                Debug.Log("[UnityRecorderBattleRecorder] Stopping frame capture...");
                frameCaptureRecorder.StopCapture();
                Debug.Log(string.Format("[UnityRecorderBattleRecorder] ✅ Frame capture stopped ({0} frames)", 
                    frameCaptureRecorder.GetFrameCount()));
                
                // Combinar frames + audio en background
                string audioPath = Path.Combine(currentOutputPath, "replay_audio.wav");
                if (File.Exists(audioPath))
                {
                    Debug.Log("[UnityRecorderBattleRecorder] 🎬 Combining frames with audio in background...");
                    frameCaptureRecorder.CombineFramesWithAudio(audioPath, (success, message) =>
                    {
                        if (success)
                        {
                            Debug.Log("[UnityRecorderBattleRecorder] ✅ Video created successfully!");
                            Debug.Log("[UnityRecorderBattleRecorder] Final video: " + message);
                        }
                        else
                        {
                            Debug.LogWarning("[UnityRecorderBattleRecorder] ⚠️ Could not create video: " + message);
                            Debug.Log("[UnityRecorderBattleRecorder] Frames saved in: " + Path.Combine(currentOutputPath, "frames"));
                        }
                    });
                }
            }
            
            // Detener grabación de video Unity Recorder solo si está habilitado
            if (!useFrameCaptureSystem && enableVideoRecording && recorderController != null)
            {
                Debug.Log("[UnityRecorderBattleRecorder] Stopping Unity Recorder video recording...");
                TryStopUnityRecorderRecording();
                Debug.Log("[UnityRecorderBattleRecorder] ✅ Unity Recorder video stopped");
            }
            
            isRecording = false;
            
            // Asegurar que el AudioListener principal permanezca activo
            if (mainAudioListener != null)
            {
                mainAudioListener.enabled = true;
                Debug.Log("[UnityRecorderBattleRecorder] AudioListener remains enabled after recording");
            }
            
            // Mantener el audio activo
            AudioListener.pause = false;
            AudioListener.volume = 1.0f;
            
            Debug.Log("[UnityRecorderBattleRecorder] Recording stopped: " + currentOutputPath);
            
            // Combinar video y audio solo si usamos Unity Recorder (no frame capture)
            if (!useFrameCaptureSystem && enableVideoRecording && !preserveAudioForVideo && audioCaptureSession != null)
            {
                string videoPath = Path.Combine(currentOutputPath, "replay.mp4");
                string audioPath = Path.Combine(currentOutputPath, "replay_audio.wav");
                string finalPath = Path.Combine(currentOutputPath, "replay_final.mp4");
                
                if (File.Exists(videoPath) && File.Exists(audioPath))
                {
                    Debug.Log("[UnityRecorderBattleRecorder] Merging Unity Recorder video and audio...");
                    VideoAudioMerger.MergeVideoAudio(videoPath, audioPath, finalPath, (success, message) =>
                    {
                        if (success)
                        {
                            Debug.Log("[UnityRecorderBattleRecorder] ✅ Video and audio merged successfully!");
                            Debug.Log("[UnityRecorderBattleRecorder] Final video: " + finalPath);
                        }
                        else
                        {
                            Debug.LogWarning("[UnityRecorderBattleRecorder] ⚠️ Could not merge automatically: " + message);
                            Debug.Log("[UnityRecorderBattleRecorder] Video (no audio): " + videoPath);
                            Debug.Log("[UnityRecorderBattleRecorder] Audio (WAV): " + audioPath);
                        }
                    });
                }
            }
            else if (useFrameCaptureSystem)
            {
                // Frame capture system - video se está creando en background
                Debug.Log("[UnityRecorderBattleRecorder] 🎬 Video being created in background from frames...");
                Debug.Log("[UnityRecorderBattleRecorder] Final video will be at: " + Path.Combine(currentOutputPath, "replay_final.mp4"));
            }
            else if (!enableVideoRecording)
            {
                string audioPath = Path.Combine(currentOutputPath, "replay_audio.wav");
                Debug.Log("[UnityRecorderBattleRecorder] ✅ Audio saved: " + audioPath);
                Debug.Log("[UnityRecorderBattleRecorder] 💡 Video not recorded (disabled for performance)");
                Debug.Log("[UnityRecorderBattleRecorder] 💡 Use OBS/Game Bar recording to get video + this audio");
            }
            
            // Refreshar botones en historial
            StartCoroutine(RefreshAfterDelay());
        }
        catch (Exception ex)
        {
            Debug.LogError("[UnityRecorderBattleRecorder] Failed to stop recording: " + ex.Message);
        }
#endif
    }

#if UNITY_EDITOR
    /// <summary>
    /// [DEPRECATED - Función simplificada]
    /// Ya no gestiona AudioListeners duplicados, solo verifica que exista el principal
    /// </summary>
    private void EnsureAudioListenerActive()
    {
        // Esta función ya no se usa - mantenida por compatibilidad
        Debug.Log("[UnityRecorderBattleRecorder] EnsureAudioListenerActive - DEPRECATED");
    }

    /// <summary>
    /// [DEPRECATED - Ya no se usa]
    /// Monitoreo de audio simplificado - ahora se hace en Update()
    /// </summary>
    private void MonitorAudioState()
    {
        // Esta función ya no se usa - mantenida por compatibilidad
    }

    /// <summary>
    /// Asegura que los PlayerPrefs de audio tengan valores correctos
    /// SLOTAudioManager lee estos valores al inicializar
    /// </summary>
    private void EnsureAudioPlayerPrefs()
    {
        // Verificar y corregir volúmenes en PlayerPrefs
        float soundVol = PlayerPrefs.GetFloat("Options_SFX_Volume", 1f);
        float voVol = PlayerPrefs.GetFloat("Options_VO_Volume", 1f);
        float musicVol = PlayerPrefs.GetFloat("Options_Music_Volume", 1f);
        
        bool needsSave = false;
        
        if (soundVol < 0.5f)
        {
            PlayerPrefs.SetFloat("Options_SFX_Volume", 1f);
            needsSave = true;
            Debug.LogWarning(string.Format(
                "[UnityRecorderBattleRecorder] SFX Volume in PlayerPrefs was {0:F2}, set to 1.0", soundVol
            ));
        }
        
        if (voVol < 0.5f)
        {
            PlayerPrefs.SetFloat("Options_VO_Volume", 1f);
            needsSave = true;
            Debug.LogWarning(string.Format(
                "[UnityRecorderBattleRecorder] VO Volume in PlayerPrefs was {0:F2}, set to 1.0", voVol
            ));
        }
        
        if (musicVol < 0.5f)
        {
            PlayerPrefs.SetFloat("Options_Music_Volume", 1f);
            needsSave = true;
            Debug.LogWarning(string.Format(
                "[UnityRecorderBattleRecorder] Music Volume in PlayerPrefs was {0:F2}, set to 1.0", musicVol
            ));
        }
        
        if (needsSave)
        {
            PlayerPrefs.Save();
            Debug.Log("[UnityRecorderBattleRecorder] PlayerPrefs audio volumes corrected and saved");
        }
        else
        {
            Debug.Log(string.Format(
                "[UnityRecorderBattleRecorder] PlayerPrefs audio volumes OK - SFX: {0:F2}, VO: {1:F2}, Music: {2:F2}",
                soundVol, voVol, musicVol
            ));
        }
    }

    /// <summary>
    /// [DEPRECATED - Ya no se usa]
    /// Función antigua que limpiaba AudioListeners
    /// </summary>
    private void CleanupAllDuplicateAudioListeners()
    {
        // Esta función ya no se usa - mantenida por compatibilidad
        Debug.Log("[UnityRecorderBattleRecorder] CleanupAllDuplicateAudioListeners - DEPRECATED");
    }

    /// <summary>
    /// [DEPRECATED - Ya no se usa]
    /// Función antigua que monitoreaba AudioListeners durante grabación
    /// </summary>
    private void CleanupPersistentAudioListener()
    {
        // Ya no hacemos nada - el AudioListener principal permanece intacto
        Debug.Log("[UnityRecorderBattleRecorder] Cleanup complete - AudioListener remains active");
    }

    private static Type FindType(string fullName)
    {
        try
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                var t = assemblies[i].GetType(fullName, false);
                if (t != null)
                    return t;
            }
        }
        catch
        {
        }
        return null;
    }

    private static void TrySetProperty(object obj, string propertyName, object value)
    {
        if (obj == null)
            return;
        try
        {
            var prop = obj.GetType().GetProperty(propertyName);
            if (prop == null || !prop.CanWrite)
                return;
            prop.SetValue(obj, value, null);
        }
        catch
        {
        }
    }

    private static object TryGetProperty(object obj, string propertyName)
    {
        if (obj == null)
            return null;
        try
        {
            var prop = obj.GetType().GetProperty(propertyName);
            if (prop == null || !prop.CanRead)
                return null;
            return prop.GetValue(obj, null);
        }
        catch
        {
            return null;
        }
    }

    private static bool TryCallMethod(object obj, string methodName)
    {
        if (obj == null)
            return false;
        try
        {
            var method = obj.GetType().GetMethod(methodName);
            if (method == null)
                return false;
            method.Invoke(obj, null);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private bool TrySetupUnityRecorder()
    {
        // Unity Recorder package is optional. Use reflection so the project compiles even if it's not installed.
        try
        {
            Type controllerSettingsType = FindType("UnityEditor.Recorder.RecorderControllerSettings");
            Type controllerType = FindType("UnityEditor.Recorder.RecorderController");
            Type movieSettingsType = FindType("UnityEditor.Recorder.MovieRecorderSettings");

            if (controllerSettingsType == null || controllerType == null || movieSettingsType == null)
            {
                recorderController = null;
                movieRecorderSettings = null;
                return false;
            }

            ScriptableObject controllerSettings = ScriptableObject.CreateInstance(controllerSettingsType);
            recorderController = Activator.CreateInstance(controllerType, controllerSettings);

            movieRecorderSettings = ScriptableObject.CreateInstance(movieSettingsType);
            TrySetProperty(movieRecorderSettings, "Enabled", true);

            string outputFile = Path.Combine(currentOutputPath, "replay");
            TrySetProperty(movieRecorderSettings, "OutputFile", outputFile);

            // Try to set resolution/frame rate if those APIs exist in this Recorder version.
            Type gameViewInputSettingsType = FindType("UnityEditor.Recorder.Input.GameViewInputSettings");
            if (gameViewInputSettingsType != null)
            {
                object gameViewInput = Activator.CreateInstance(gameViewInputSettingsType);
                TrySetProperty(gameViewInput, "OutputWidth", 1280);
                TrySetProperty(gameViewInput, "OutputHeight", 720);
                TrySetProperty(movieRecorderSettings, "ImageInputSettings", gameViewInput);
            }

            TrySetProperty(movieRecorderSettings, "FrameRate", 30.0f);

            // Add recorder settings + set manual mode.
            try
            {
                var addRecorderSettings = controllerSettingsType.GetMethod("AddRecorderSettings");
                if (addRecorderSettings != null)
                {
                    addRecorderSettings.Invoke(controllerSettings, new object[] { movieRecorderSettings });
                }
                var setManual = controllerSettingsType.GetMethod("SetRecordModeToManual");
                if (setManual != null)
                {
                    setManual.Invoke(controllerSettings, null);
                }
            }
            catch
            {
                // If API changed, just let PrepareRecording fail and we will fallback.
            }

            Debug.Log("[UnityRecorderBattleRecorder] Unity Recorder configured via reflection");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[UnityRecorderBattleRecorder] Unity Recorder setup failed: " + ex.Message);
            recorderController = null;
            movieRecorderSettings = null;
            return false;
        }
    }

    private bool TryPrepareUnityRecorderRecording()
    {
        return TryCallMethod(recorderController, "PrepareRecording");
    }

    private bool TryStopUnityRecorderRecording()
    {
        return TryCallMethod(recorderController, "StopRecording");
    }
#endif

    private IEnumerator RefreshAfterDelay()
    {
        // Esperar a que el archivo se termine de escribir
        yield return new WaitForSeconds(2.0f);
        
        RefreshMatchHistoryButtons();
        ShowSuccessMessage();
    }

    private void RefreshMatchHistoryButtons()
    {
        if (BattleHistoryController.Instance != null)
        {
            BattleHistoryController.Instance.RefreshAllReplayButtons();
            Debug.Log("[UnityRecorderBattleRecorder] Match history buttons refreshed");
        }
    }

    private void ShowSuccessMessage()
    {
        string mp4Path = Path.Combine(currentOutputPath, "replay.mp4");
        string message = "Battle replay recorded!\n\nVideo: " + mp4Path;
        
        if (Singleton<SimplePopupController>.Instance != null)
        {
            Singleton<SimplePopupController>.Instance.ShowMessage(
                "Recording Complete",
                message
            );
        }
        else
        {
            Debug.Log("[UnityRecorderBattleRecorder] " + message);
        }
    }

    /// <summary>
    /// Verifica si existe video para replayID
    /// </summary>
    public bool VideoExists(string replayID)
    {
        if (string.IsNullOrEmpty(replayID))
            return false;
            
        string exportDirectory = Path.Combine(Application.persistentDataPath, "ExportedReplays");
        string videoFolder = string.Format("Video_{0}", replayID);
        string folderPath = Path.Combine(exportDirectory, videoFolder);
        if (!Directory.Exists(folderPath))
            return false;

        // Support multiple output names depending on the recording pipeline.
        string[] candidates =
        {
            Path.Combine(folderPath, "replay_final.mp4"),
            Path.Combine(folderPath, "final.mp4"),
            Path.Combine(folderPath, "replay.mp4")
        };

        for (int i = 0; i < candidates.Length; i++)
        {
            string path = candidates[i];
            if (!File.Exists(path))
                continue;

            // Verificar que el archivo no esté vacío
            FileInfo fileInfo = new FileInfo(path);
            if (fileInfo.Length > 1024)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Obtiene ruta del video MP4 (prioriza replay_final.mp4)
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
        
        // Priorizar replay_final.mp4 (Frame Capture System con audio)
        string finalMp4Path = Path.Combine(folderPath, "replay_final.mp4");
        if (File.Exists(finalMp4Path))
        {
            try
            {
                FileInfo fi = new FileInfo(finalMp4Path);
                if (fi.Length > 1024)
                    return finalMp4Path;
            }
            catch
            {
            }
        }

        // Algunos pipelines usan final.mp4
        string altFinalPath = Path.Combine(folderPath, "final.mp4");
        if (File.Exists(altFinalPath))
        {
            try
            {
                FileInfo fi = new FileInfo(altFinalPath);
                if (fi.Length > 1024)
                    return altFinalPath;
            }
            catch
            {
            }
        }
        
        // Fallback a replay.mp4 (Unity Recorder)
        string mp4Path = Path.Combine(folderPath, "replay.mp4");
        if (!File.Exists(mp4Path))
            return null;

        try
        {
            FileInfo fi = new FileInfo(mp4Path);
            if (fi.Length > 1024)
                return mp4Path;
        }
        catch
        {
        }

        return null;
    }
    
    /// <summary>
    /// Obtiene la carpeta donde está guardado el video
    /// </summary>
    public string GetVideoFolder(string replayID)
    {
        if (string.IsNullOrEmpty(replayID))
            return null;
            
        string exportDirectory = Path.Combine(Application.persistentDataPath, "ExportedReplays");
        string videoFolder = string.Format("Video_{0}", replayID);
        string fullPath = Path.Combine(exportDirectory, videoFolder);
        
        return Directory.Exists(fullPath) ? fullPath : null;
    }
}
