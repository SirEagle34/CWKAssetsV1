using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

/// <summary>
/// Sistema de diagnóstico para grabación de video en Android
/// Verifica permisos, archivos, MediaCodec y genera reportes detallados
/// </summary>
public class AndroidRecordingDiagnostic : MonoBehaviour
{
    private static AndroidRecordingDiagnostic _instance;
    
    public static AndroidRecordingDiagnostic Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject obj = new GameObject("AndroidRecordingDiagnostic");
                _instance = obj.AddComponent<AndroidRecordingDiagnostic>();
                DontDestroyOnLoad(obj);
            }
            return _instance;
        }
    }

    [Header("Test Recording Settings")]
    [SerializeField] private int testDurationSeconds = 5;
    [SerializeField] private bool autoStartTest = false;
    [SerializeField] private KeyCode testTriggerKey = KeyCode.F9;

    private List<string> diagnosticLog = new List<string>();
    private bool isTestRunning = false;

    private void Start()
    {
        if (autoStartTest)
        {
            StartCoroutine(DelayedAutoTest());
        }
    }

    private IEnumerator DelayedAutoTest()
    {
        yield return new WaitForSeconds(2f);
        StartDiagnosticTest();
    }

    private void Update()
    {
        if (Input.GetKeyDown(testTriggerKey) && !isTestRunning)
        {
            StartDiagnosticTest();
        }
    }

    /// <summary>
    /// Inicia prueba diagnóstica completa
    /// </summary>
    public void StartDiagnosticTest()
    {
        if (isTestRunning)
        {
            LogDiagnostic("⚠️ Test already running");
            return;
        }

        diagnosticLog.Clear();
        LogDiagnostic("=== ANDROID RECORDING DIAGNOSTIC TEST ===");
        LogDiagnostic("Date: " + DateTime.Now.ToString());
        LogDiagnostic("Platform: " + Application.platform);
        LogDiagnostic("Device: " + SystemInfo.deviceModel);
        LogDiagnostic("OS: " + SystemInfo.operatingSystem);
        LogDiagnostic("");

        StartCoroutine(RunFullDiagnostic());
    }

    private IEnumerator RunFullDiagnostic()
    {
        isTestRunning = true;

        // PASO 1: Verificar plataforma
        LogDiagnostic("--- STEP 1: Platform Check ---");
        bool isPlatformValid = CheckPlatform();
        yield return new WaitForSeconds(0.5f);

        if (!isPlatformValid)
        {
            LogDiagnostic("❌ Not on Android platform, skipping further tests");
            FinishDiagnostic();
            yield break;
        }

        // PASO 2: Verificar permisos
        LogDiagnostic("\n--- STEP 2: Permissions Check ---");
        yield return StartCoroutine(CheckAndRequestPermissions());
        yield return new WaitForSeconds(0.5f);

        // PASO 3: Verificar sistema de archivos
        LogDiagnostic("\n--- STEP 3: File System Check ---");
        CheckFileSystem();
        yield return new WaitForSeconds(0.5f);

        // PASO 4: Verificar MediaCodec
        LogDiagnostic("\n--- STEP 4: MediaCodec Check ---");
        CheckMediaCodecAvailability();
        yield return new WaitForSeconds(0.5f);

        // PASO 5: Verificar componentes de grabación
        LogDiagnostic("\n--- STEP 5: Recording Components Check ---");
        CheckRecordingComponents();
        yield return new WaitForSeconds(0.5f);

        // PASO 6: Verificar AudioListener
        LogDiagnostic("\n--- STEP 6: Audio Check ---");
        CheckAudioSetup();
        yield return new WaitForSeconds(0.5f);

        // PASO 7: Prueba de grabación real
        LogDiagnostic("\n--- STEP 7: Recording Test ---");
        LogDiagnostic($"Starting {testDurationSeconds} second test recording...");
        yield return StartCoroutine(TestActualRecording());

        // PASO 8: Verificar resultados
        LogDiagnostic("\n--- STEP 8: Results Verification ---");
        yield return StartCoroutine(VerifyRecordingResults());

        FinishDiagnostic();
    }

    private bool CheckPlatform()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        LogDiagnostic("✅ Running on Android device");
        LogDiagnostic("   Unity Version: " + Application.unityVersion);
        LogDiagnostic("   Device Model: " + SystemInfo.deviceModel);
        LogDiagnostic("   GPU: " + SystemInfo.graphicsDeviceName);
        return true;
#else
        LogDiagnostic("⚠️ Not running on Android device");
        LogDiagnostic("   Current Platform: " + Application.platform);
        LogDiagnostic("   This test is designed for Android builds only");
        return false;
#endif
    }

    private IEnumerator CheckAndRequestPermissions()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        // Verificar Storage
        bool hasStorage = Permission.HasUserAuthorizedPermission(Permission.ExternalStorageWrite);
        LogDiagnostic($"Storage Permission: {(hasStorage ? "✅ Granted" : "❌ Denied")}");
        
        if (!hasStorage)
        {
            LogDiagnostic("   Requesting storage permission...");
            Permission.RequestUserPermission(Permission.ExternalStorageWrite);
            yield return new WaitForSeconds(2f);
            
            hasStorage = Permission.HasUserAuthorizedPermission(Permission.ExternalStorageWrite);
            LogDiagnostic($"   After request: {(hasStorage ? "✅ Granted" : "❌ Denied")}");
        }

        // Verificar Microphone
        bool hasMicrophone = Permission.HasUserAuthorizedPermission(Permission.Microphone);
        LogDiagnostic($"Microphone Permission: {(hasMicrophone ? "✅ Granted" : "⚠️ Denied (optional)")}");
        
        if (!hasMicrophone)
        {
            LogDiagnostic("   Requesting microphone permission...");
            Permission.RequestUserPermission(Permission.Microphone);
            yield return new WaitForSeconds(2f);
            
            hasMicrophone = Permission.HasUserAuthorizedPermission(Permission.Microphone);
            LogDiagnostic($"   After request: {(hasMicrophone ? "✅ Granted" : "⚠️ Denied")}");
        }
#else
        LogDiagnostic("⚠️ Permission check only available on Android");
        yield return null;
#endif
    }

    private void CheckFileSystem()
    {
        string persistentPath = Application.persistentDataPath;
        LogDiagnostic($"Persistent Data Path: {persistentPath}");

        try
        {
            // Verificar que podemos escribir
            string testFile = Path.Combine(persistentPath, "test_write.txt");
            File.WriteAllText(testFile, "Test");
            
            if (File.Exists(testFile))
            {
                File.Delete(testFile);
                LogDiagnostic("✅ File system write access OK");
            }
        }
        catch (Exception ex)
        {
            LogDiagnostic($"❌ File system write access FAILED: {ex.Message}");
        }

        // Verificar carpeta de replays
        string exportPath = Path.Combine(persistentPath, "ExportedReplays");
        if (Directory.Exists(exportPath))
        {
            LogDiagnostic($"✅ ExportedReplays folder exists");
            
            // Listar videos existentes
            string[] videoDirs = Directory.GetDirectories(exportPath, "Video_*");
            LogDiagnostic($"   Found {videoDirs.Length} existing video folders");
            
            foreach (string dir in videoDirs)
            {
                string mp4 = Path.Combine(dir, "replay_final.mp4");
                string framesDir = Path.Combine(dir, "frames");
                
                bool hasMp4 = File.Exists(mp4);
                bool hasFrames = Directory.Exists(framesDir);
                int frameCount = hasFrames ? Directory.GetFiles(framesDir, "frame_*.jpg").Length : 0;
                
                LogDiagnostic($"   {Path.GetFileName(dir)}: MP4={hasMp4}, Frames={frameCount}");
            }
        }
        else
        {
            LogDiagnostic("⚠️ ExportedReplays folder doesn't exist yet (will be created)");
        }
    }

    private void CheckMediaCodecAvailability()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (AndroidJavaClass encoderClass = new AndroidJavaClass("com.cardwarskingdom.videoencoder.MediaCodecVideoEncoder"))
            {
                LogDiagnostic("✅ MediaCodecVideoEncoder class found");
                
                // Probar si podemos llamar a un método (sin ejecutarlo)
                LogDiagnostic("   Java class loaded successfully from native plugin");
            }
        }
        catch (Exception ex)
        {
            LogDiagnostic($"❌ MediaCodecVideoEncoder NOT found: {ex.Message}");
            LogDiagnostic("   Make sure MediaCodecVideoEncoder.java is in Assets/Plugins/Android/");
            LogDiagnostic("   Package should be: com.cardwarskingdom.videoencoder");
        }

        // Verificar MediaCodec del sistema
        try
        {
            using (AndroidJavaClass mc = new AndroidJavaClass("android.media.MediaCodec"))
            {
                LogDiagnostic("✅ Android MediaCodec API accessible");
            }
        }
        catch (Exception ex)
        {
            LogDiagnostic($"❌ MediaCodec API error: {ex.Message}");
        }
#else
        LogDiagnostic("⚠️ MediaCodec check only available on Android");
#endif
    }

    private void CheckRecordingComponents()
    {
        // Verificar MobileVideoRecorder
        if (MobileVideoRecorder.Instance != null)
        {
            LogDiagnostic("✅ MobileVideoRecorder instance OK");
            LogDiagnostic($"   Is Recording: {MobileVideoRecorder.Instance.IsRecording}");
        }
        else
        {
            LogDiagnostic("❌ MobileVideoRecorder instance is NULL");
        }

        // Verificar FrameCaptureRecorder
        FrameCaptureRecorder fcr = FindObjectOfType<FrameCaptureRecorder>();
        if (fcr != null)
        {
            LogDiagnostic($"✅ FrameCaptureRecorder found (IsCapturing: {fcr.IsCapturing})");
        }
        else
        {
            LogDiagnostic("⚠️ FrameCaptureRecorder not found (will be created on demand)");
        }

        // Verificar AudioCaptureSession
        AudioCaptureSession acs = FindObjectOfType<AudioCaptureSession>();
        if (acs != null)
        {
            LogDiagnostic($"✅ AudioCaptureSession found (IsCapturing: {acs.IsCapturing})");
        }
        else
        {
            LogDiagnostic("⚠️ AudioCaptureSession not found (will be created on demand)");
        }
    }

    private void CheckAudioSetup()
    {
        AudioListener[] listeners = FindObjectsOfType<AudioListener>();
        LogDiagnostic($"Found {listeners.Length} AudioListener(s)");

        if (listeners.Length == 0)
        {
            LogDiagnostic("❌ No AudioListener in scene - audio won't be recorded!");
        }
        else if (listeners.Length > 1)
        {
            LogDiagnostic("⚠️ Multiple AudioListeners found - may cause issues");
        }

        foreach (AudioListener listener in listeners)
        {
            bool active = listener.gameObject.activeInHierarchy;
            bool enabled = listener.enabled;
            Camera cam = listener.GetComponent<Camera>();
            
            LogDiagnostic($"   - {listener.gameObject.name}: Active={active}, Enabled={enabled}, HasCamera={cam != null}");
        }

        LogDiagnostic($"AudioListener.volume: {AudioListener.volume}");
        LogDiagnostic($"AudioListener.pause: {AudioListener.pause}");

        // Verificar AudioSource activos
        AudioSource[] sources = FindObjectsOfType<AudioSource>();
        int activeSources = 0;
        foreach (AudioSource src in sources)
        {
            if (src.gameObject.activeInHierarchy && src.enabled)
                activeSources++;
        }
        LogDiagnostic($"Active AudioSources: {activeSources}/{sources.Length}");
    }

    private IEnumerator TestActualRecording()
    {
        if (MobileVideoRecorder.Instance == null)
        {
            LogDiagnostic("❌ Cannot test - MobileVideoRecorder instance is null");
            yield break;
        }

        // Iniciar grabación
        LogDiagnostic("🎬 Starting recording...");
        
        // Intentar iniciar - capturar errores sin try-catch
        bool startSuccess = SafeStartRecording();
        if (!startSuccess)
        {
            LogDiagnostic("❌ Failed to start recording (see logs above)");
            yield break;
        }
        
        yield return new WaitForSeconds(1f);

        if (!MobileVideoRecorder.Instance.IsRecording)
        {
            LogDiagnostic("❌ Recording did not start!");
            yield break;
        }

        LogDiagnostic($"✅ Recording started successfully");
        
        // Grabar por el tiempo especificado
        for (int i = 1; i <= testDurationSeconds; i++)
        {
            LogDiagnostic($"   Recording... {i}/{testDurationSeconds} seconds");
            yield return new WaitForSeconds(1f);
        }

        // Detener grabación
        LogDiagnostic("🛑 Stopping recording...");
        
        bool stopSuccess = SafeStopRecording();
        if (!stopSuccess)
        {
            LogDiagnostic("⚠️ Error stopping recording (see logs above)");
        }
        
        yield return new WaitForSeconds(1f);

        if (MobileVideoRecorder.Instance.IsRecording)
        {
            LogDiagnostic("⚠️ Recording did not stop properly");
        }
        else
        {
            LogDiagnostic("✅ Recording stopped");
        }

        // Esperar a que se procesen los frames (puede tardar)
        LogDiagnostic("⏳ Waiting for frame processing and video encoding...");
        LogDiagnostic("   (This may take 10-30 seconds depending on device)");
    }

    private bool SafeStartRecording()
    {
        try
        {
            MobileVideoRecorder.Instance.StartRecording();
            return true;
        }
        catch (Exception ex)
        {
            LogDiagnostic($"❌ Exception starting recording: {ex.Message}");
            return false;
        }
    }

    private bool SafeStopRecording()
    {
        try
        {
            MobileVideoRecorder.Instance.StopRecording();
            return true;
        }
        catch (Exception ex)
        {
            LogDiagnostic($"❌ Exception stopping recording: {ex.Message}");
            return false;
        }
    }

    private IEnumerator VerifyRecordingResults()
    {
        // Esperar tiempo extra para encoding
        yield return new WaitForSeconds(15f);

        string exportPath = Path.Combine(Application.persistentDataPath, "ExportedReplays");
        
        if (!Directory.Exists(exportPath))
        {
            LogDiagnostic("❌ ExportedReplays folder was not created");
            yield break;
        }

        string[] videoDirs = Directory.GetDirectories(exportPath, "Video_*");
        
        if (videoDirs.Length == 0)
        {
            LogDiagnostic("❌ No video folders found");
            yield break;
        }

        // Verificar la carpeta más reciente
        string latestDir = videoDirs[videoDirs.Length - 1];
        LogDiagnostic($"\n📁 Latest video folder: {Path.GetFileName(latestDir)}");

        // Verificar frames
        string framesDir = Path.Combine(latestDir, "frames");
        if (Directory.Exists(framesDir))
        {
            string[] frameFiles = Directory.GetFiles(framesDir, "frame_*.jpg");
            LogDiagnostic($"   ✅ Frames: {frameFiles.Length} files");
            
            if (frameFiles.Length > 0)
            {
                // Verificar tamaño del primer frame
                FileInfo firstFrame = new FileInfo(frameFiles[0]);
                LogDiagnostic($"   Frame size: {firstFrame.Length / 1024}KB");
            }
        }
        else
        {
            LogDiagnostic($"   ❌ Frames folder not found");
        }

        // Verificar audio
        string audioFile = Path.Combine(latestDir, "replay_audio.wav");
        if (File.Exists(audioFile))
        {
            FileInfo audioInfo = new FileInfo(audioFile);
            LogDiagnostic($"   ✅ Audio: {audioInfo.Length / 1024}KB");
        }
        else
        {
            LogDiagnostic($"   ❌ Audio file not found");
        }

        // Verificar video final
        string videoFile = Path.Combine(latestDir, "replay_final.mp4");
        if (File.Exists(videoFile))
        {
            FileInfo videoInfo = new FileInfo(videoFile);
            LogDiagnostic($"   ✅ Final Video: {videoInfo.Length / (1024 * 1024)}MB - SUCCESS!");
            LogDiagnostic($"   Location: {videoFile}");
        }
        else
        {
            LogDiagnostic($"   ⚠️ Final MP4 not found yet (encoding may still be in progress)");
            LogDiagnostic($"   Check again in 30 seconds or look for errors in logcat");
        }
    }

    private void FinishDiagnostic()
    {
        isTestRunning = false;
        LogDiagnostic("\n=== DIAGNOSTIC TEST COMPLETE ===");
        LogDiagnostic($"Total checks: {diagnosticLog.Count}");
        
        // Guardar log a archivo
        SaveLogToFile();
        
        LogDiagnostic("\n📋 Full report saved to: diagnostic_report.txt");
        LogDiagnostic("   You can pull it with: adb pull " + 
            Path.Combine(Application.persistentDataPath, "diagnostic_report.txt"));
    }

    private void LogDiagnostic(string message)
    {
        diagnosticLog.Add(message);
        Debug.Log($"[DIAGNOSTIC] {message}");
    }

    private void SaveLogToFile()
    {
        try
        {
            string logPath = Path.Combine(Application.persistentDataPath, "diagnostic_report.txt");
            File.WriteAllLines(logPath, diagnosticLog.ToArray());
            LogDiagnostic($"✅ Log saved to: {logPath}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[DIAGNOSTIC] Failed to save log: {ex.Message}");
        }
    }

    /// <summary>
    /// Permite ejecutar el diagnóstico desde código externo
    /// </summary>
    public static void RunTest()
    {
        Instance.StartDiagnosticTest();
    }
}
