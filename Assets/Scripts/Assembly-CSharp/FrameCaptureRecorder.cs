using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

public class FrameCaptureRecorder : MonoBehaviour
{
    #if UNITY_IOS && !UNITY_EDITOR
    // Importar función nativa de iOS
    [DllImport("__Internal")]
    extern static bool AVFoundationEncodeVideo(string framesDirPath, string audioPath, string outputPath, int frameRate, int width, int height);
    #endif

    /// <summary>
    /// Sistema de grabación ligero que captura frames individuales
    /// Combina frames + audio en background después de la batalla
    /// NO afecta el rendimiento durante gameplay
    /// </summary>
    private bool isCapturing = false;
    private string outputPath;
    private int frameCount = 0;
    private float captureStartTime;
    
    [SerializeField]
    [Tooltip("Frames por segundo a capturar (30 = buen balance)")]
    private int targetFPS = 30;
    
    [SerializeField]
    [Tooltip("Calidad de las capturas (1-100, 75 recomendado)")]
    private int jpegQuality = 75;
    
    [SerializeField]
    [Tooltip("Escala de resolución para móviles (0.5 = mitad de resolución)")]
    private float mobileResolutionScale = 0.65f;
    
    private float frameInterval;
    private float nextCaptureTime;
    
    private Queue<CaptureData> captureQueue = new Queue<CaptureData>();
    private bool processingQueue = false;
    
    private class CaptureData
    {
        public Texture2D texture;
        public string filename;
    }
    
    public bool IsCapturing
    {
        get { return isCapturing; }
    }
    
    public int GetFrameCount()
    {
        return frameCount;
    }
    
    /// <summary>
    /// Inicia la captura de frames
    /// </summary>
    public void StartCapture(string outputFolder)
    {
        if (isCapturing)
        {
            Debug.LogWarning("[FrameCaptureRecorder] Already capturing");
            return;
        }
        
        // Optimizaciones automáticas para móviles
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
        targetFPS = 24; // Reducir FPS para mejor rendimiento
        jpegQuality = 60; // Reducir calidad para archivos más pequeños
        Debug.Log("[FrameCaptureRecorder] Mobile optimizations: 24 FPS, Quality 60, Resolution 65%");
#endif
        
        outputPath = outputFolder;
        Debug.Log($"[FrameCaptureRecorder] Output folder: {outputPath}");
        
        // Crear carpeta para frames
        string framesFolder = Path.Combine(outputPath, "frames");
        Debug.Log($"[FrameCaptureRecorder] Frames folder: {framesFolder}");
        
        if (!Directory.Exists(framesFolder))
        {
            try
            {
                Directory.CreateDirectory(framesFolder);
                Debug.Log("[FrameCaptureRecorder] Created frames folder");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FrameCaptureRecorder] Failed to create frames folder: {ex.Message}");
                return;
            }
        }
        else
        {
            Debug.Log("[FrameCaptureRecorder] Frames folder already exists");
        }
        
        frameCount = 0;
        // Use unscaled time so capture rate is based on real time, not Time.timeScale.
        captureStartTime = Time.unscaledTime;
        frameInterval = 1f / targetFPS;
        nextCaptureTime = Time.unscaledTime;
        
        isCapturing = true;
        
        // Iniciar procesamiento de cola
        if (!processingQueue)
        {
            StartCoroutine(ProcessCaptureQueue());
        }
        
        Debug.Log(string.Format(
            "[FrameCaptureRecorder] ✅ Started capturing at {0} FPS to: {1}",
            targetFPS,
            framesFolder
        ));
    }
    
    /// <summary>
    /// Detiene la captura
    /// </summary>
    public void StopCapture()
    {
        if (!isCapturing)
        {
            Debug.LogWarning("[FrameCaptureRecorder] Not currently capturing");
            return;
        }
        
        isCapturing = false;
        
        float duration = Time.unscaledTime - captureStartTime;

        float expectedVideoSeconds = (targetFPS > 0) ? ((float)frameCount / targetFPS) : 0f;
        
        Debug.Log(string.Format(
            "[FrameCaptureRecorder] Capture stopped. {0} frames in {1:F2}s (avg {2:F1} FPS). Expected video length: {3:F2}s",
            frameCount,
            duration,
            (duration > 0.0001f) ? (frameCount / duration) : 0f,
            expectedVideoSeconds
        ));
    }
    
    void LateUpdate()
    {
        if (!isCapturing)
            return;
        
        // Capturar frame si es tiempo
        // Use unscaled time so we keep capturing even if the game changes Time.timeScale.
        if (Time.unscaledTime >= nextCaptureTime)
        {
            StartCoroutine(CaptureFrameCoroutine());
            nextCaptureTime = Time.unscaledTime + frameInterval;
        }
    }
    
    /// <summary>
    /// Captura un frame a memoria (síncrono, rápido)
    /// </summary>
    private IEnumerator CaptureFrameCoroutine()
    {
        // Esperar al final del frame cuando el render está completo
        yield return new WaitForEndOfFrame();
        
        try
        {
            string framesFolder = Path.Combine(outputPath, "frames");
            string filename = Path.Combine(framesFolder, string.Format("frame_{0:D6}.jpg", frameCount));
            
            // Log cada 30 frames (cada segundo a 30fps)
            if (frameCount % 30 == 0)
            {
                Debug.Log($"[FrameCaptureRecorder] Capturing frame {frameCount}...");
            }
            
            // Capturar la pantalla actual con ReadPixels (síncrono y confiable)
            // En móviles, reducir resolución para mejor rendimiento
            int captureWidth = Screen.width;
            int captureHeight = Screen.height;
            
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            captureWidth = Mathf.RoundToInt(Screen.width * mobileResolutionScale);
            captureHeight = Mathf.RoundToInt(Screen.height * mobileResolutionScale);
            
            // Asegurar que sean múltiplos de 2 (requisito de video codecs)
            captureWidth = (captureWidth / 2) * 2;
            captureHeight = (captureHeight / 2) * 2;
#endif
            
            Texture2D screenshot = new Texture2D(captureWidth, captureHeight, TextureFormat.RGB24, false);
            
            // Si reducimos la resolución, necesitamos crear un RenderTexture temporal
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            RenderTexture rt = RenderTexture.GetTemporary(captureWidth, captureHeight, 24);
            RenderTexture currentRT = RenderTexture.active;
            
            // Copiar pantalla al RenderTexture escalado
            Graphics.Blit(null, rt);
            RenderTexture.active = rt;
            
            screenshot.ReadPixels(new Rect(0, 0, captureWidth, captureHeight), 0, 0);
            screenshot.Apply();
            
            RenderTexture.active = currentRT;
            RenderTexture.ReleaseTemporary(rt);
#else
            screenshot.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
            screenshot.Apply();
#endif
            
            if (screenshot != null)
            {
                // Encolar para guardar en background
                captureQueue.Enqueue(new CaptureData { texture = screenshot, filename = filename });
                frameCount++;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("[FrameCaptureRecorder] Error capturing frame: " + ex.Message);
        }
    }
    
    /// <summary>
    /// Procesa la cola de capturas y las guarda como JPEG
    /// </summary>
    private IEnumerator ProcessCaptureQueue()
    {
        processingQueue = true;
        
        while (isCapturing || captureQueue.Count > 0)
        {
            if (captureQueue.Count > 0)
            {
                CaptureData data = captureQueue.Dequeue();
                
                try
                {
                    // Codificar a JPEG en memoria
                    byte[] jpegBytes = data.texture.EncodeToJPG(jpegQuality);
                    
                    // Guardar a disco
                    File.WriteAllBytes(data.filename, jpegBytes);
                    
                    // Liberar textura
                    Destroy(data.texture);
                }
                catch (Exception ex)
                {
                    Debug.LogError("[FrameCaptureRecorder] Error saving frame: " + ex.Message);
                }
            }
            
            // Yield cada frame para no bloquear
            yield return null;
        }
        
        processingQueue = false;
        Debug.Log("[FrameCaptureRecorder] Queue processing finished. All frames saved.");
    }
    
    /// <summary>
    /// Combina frames con audio usando FFmpeg (en background)
    /// </summary>
    public void CombineFramesWithAudio(string audioPath, Action<bool, string> onComplete)
    {
        StartCoroutine(CombineFramesCoroutine(audioPath, onComplete));
    }
    
    private IEnumerator CombineFramesCoroutine(string audioPath, Action<bool, string> onComplete)
    {
        Debug.Log("[FrameCaptureRecorder] Starting frame combination in background...");
        
        // CRÍTICO: Esperar a que se termine de guardar todos los frames
        Debug.Log("[FrameCaptureRecorder] Waiting for all frames to be saved...");
        while (captureQueue.Count > 0 || processingQueue)
        {
            yield return new WaitForSeconds(0.1f);
        }
        
        // Esperar un frame extra para asegurar que todo está escrito
        yield return new WaitForSeconds(0.5f);
        Debug.Log("[FrameCaptureRecorder] All frames saved. Starting video encoding...");
        
        string framesFolder = Path.Combine(outputPath, "frames");
        string outputVideo = Path.Combine(outputPath, "replay_final.mp4");
        string error = "";
        
        // Verificar que existan frames
        if (!Directory.Exists(framesFolder))
        {
            error = "Frames folder not found: " + framesFolder;
            Debug.LogError("[FrameCaptureRecorder] " + error);
            onComplete?.Invoke(false, error);
            yield break;
        }
        
        // Verificar que haya al menos un frame
        string[] frameFiles = Directory.GetFiles(framesFolder, "frame_*.jpg");
        if (frameFiles.Length == 0)
        {
            error = "No frames found in: " + framesFolder;
            Debug.LogError("[FrameCaptureRecorder] " + error);
            onComplete?.Invoke(false, error);
            yield break;
        }
        
        Debug.Log(string.Format("[FrameCaptureRecorder] Found {0} frames to combine", frameFiles.Length));
        
        // Detectar plataforma y usar el encoder apropiado
#if UNITY_ANDROID && !UNITY_EDITOR
        // Android: Usar MediaCodec (hardware nativo)
        Debug.Log("[FrameCaptureRecorder] Using Android MediaCodec for encoding...");

        // IMPORTANTE: capturar dimensiones en el hilo principal antes de crear el thread.
        // Screen.width/height desde un hilo secundario puede devolver 0 en algunas versiones de Unity.
        // Además, los frames fueron capturados a mobileResolutionScale, así que debemos pasar
        // esas mismas dimensiones al encoder (no la resolución completa), lo que evita que
        // MediaCodec intente escalar cada frame de 65% a 100% (causaba OOM/fallo silencioso).
        int encoderWidth  = ((int)(Screen.width  * mobileResolutionScale) / 2) * 2;
        int encoderHeight = ((int)(Screen.height * mobileResolutionScale) / 2) * 2;
        encoderWidth  = Mathf.Max(2, encoderWidth);
        encoderHeight = Mathf.Max(2, encoderHeight);
        Debug.Log(string.Format("[FrameCaptureRecorder] Encoder dimensions (main thread): {0}x{1}", encoderWidth, encoderHeight));

        bool success = false;
        System.Threading.Thread encodingThread = new System.Threading.Thread(() =>
        {
            bool jniAttached = false;
            try
            {
                AndroidJNI.AttachCurrentThread();
                jniAttached = true;

                // Usar dimensiones capturadas en el hilo principal
                Debug.Log(string.Format("[FrameCaptureRecorder] MediaCodec request: {0}x{1} @ {2}fps", encoderWidth, encoderHeight, targetFPS));
                
                // Llamar al plugin de Android
                using (AndroidJavaClass encoderClass = new AndroidJavaClass("com.cardwarskingdom.videoencoder.MediaCodecVideoEncoder"))
                {
                    success = encoderClass.CallStatic<bool>("encodeVideo", 
                        framesFolder, 
                        audioPath, 
                        outputVideo,
                        targetFPS,
                        encoderWidth,
                        encoderHeight
                    );
                }

                Debug.Log("[FrameCaptureRecorder] MediaCodec encodeVideo() returned: " + success);
            }
            catch (Exception ex)
            {
                Debug.LogError("[FrameCaptureRecorder] MediaCodec encoding error: " + ex.Message);
                Debug.LogException(ex);
                error = ex.Message;
                success = false;
            }
            finally
            {
                if (jniAttached)
                {
                    try
                    {
                        AndroidJNI.DetachCurrentThread();
                    }
                    catch
                    {
                    }
                }
            }
        });
        
        encodingThread.Start();
        
        // Esperar a que termine (sin bloquear el hilo principal)
        while (encodingThread.IsAlive)
        {
            yield return new WaitForSeconds(0.5f);
        }
        
        if (success && File.Exists(outputVideo))
        {
            Debug.Log("[FrameCaptureRecorder] ✅ Video created successfully with MediaCodec!");
            Debug.Log("[FrameCaptureRecorder] Output: " + outputVideo);
            onComplete?.Invoke(true, outputVideo);
        }
        else
        {
            string errorMsg = "MediaCodec encoding failed" + (string.IsNullOrEmpty(error) ? "" : ": " + error);
            Debug.LogError("[FrameCaptureRecorder] " + errorMsg);
            onComplete?.Invoke(false, errorMsg);
        }
#elif UNITY_IOS && !UNITY_EDITOR
        // iOS: Usar AVFoundation (hardware nativo)
        Debug.Log("[FrameCaptureRecorder] Using iOS AVFoundation for encoding...");
        
        bool success = false;
        System.Threading.Thread encodingThread = new System.Threading.Thread(() =>
        {
            try
            {
                // Obtener dimensiones del primer frame
                int width = Screen.width;
                int height = Screen.height;
                
                // Llamar al plugin nativo de iOS
                success = AVFoundationEncodeVideo(
                    framesFolder,
                    audioPath,
                    outputVideo,
                    targetFPS,
                    width,
                    height
                );
            }
            catch (Exception ex)
            {
                Debug.LogError("[FrameCaptureRecorder] AVFoundation encoding error: " + ex.Message);
                error = ex.Message;
                success = false;
            }
        });
        
        encodingThread.Start();
        
        // Esperar a que termine (sin bloquear el hilo principal)
        while (encodingThread.IsAlive)
        {
            yield return new WaitForSeconds(0.5f);
        }
        
        if (success && File.Exists(outputVideo))
        {
            Debug.Log("[FrameCaptureRecorder] ✅ Video created successfully with AVFoundation!");
            Debug.Log("[FrameCaptureRecorder] Output: " + outputVideo);
            onComplete?.Invoke(true, outputVideo);
        }
        else
        {
            string errorMsg = "AVFoundation encoding failed" + (string.IsNullOrEmpty(error) ? "" : ": " + error);
            Debug.LogError("[FrameCaptureRecorder] " + errorMsg);
            onComplete?.Invoke(false, errorMsg);
        }
#else
        // Windows/Editor: Usar FFmpeg
        Debug.Log("[FrameCaptureRecorder] Using FFmpeg for encoding...");
        
        // Buscar FFmpeg
        string ffmpegPath = VideoAudioMerger.FindFFmpegExecutable();
        if (string.IsNullOrEmpty(ffmpegPath))
        {
            error = "FFmpeg not found. Cannot combine frames.";
            Debug.LogWarning("[FrameCaptureRecorder] " + error);
            onComplete?.Invoke(false, error);
            yield break;
        }
        
        // Comando FFmpeg para combinar frames + audio
        // -framerate: FPS de entrada
        // -i frames/frame_%06d.jpg: Patrón de archivos
        // -i audio.wav: Audio
        // -c:v libx264: Codec H.264
        // -preset fast: Balance velocidad/calidad
        // -crf 23: Calidad (18-28, 23 = bueno)
        // -c:a aac: Audio AAC
        // NOTE: x264 requiere dimensiones pares. Si el GameView/captura genera un tamaño impar (ej: 770x433),
        // FFmpeg falla con "height not divisible by 2". Para evitarlo, hacemos padding automático a dimensiones pares.
        // También forzamos yuv420p para máxima compatibilidad con reproductores.
        string arguments = string.Format(
            "-framerate {0} -i \"{1}\" -i \"{2}\" -vf \"pad=ceil(iw/2)*2:ceil(ih/2)*2:(ow-iw)/2:(oh-ih)/2\" -c:v libx264 -pix_fmt yuv420p -preset fast -crf 23 -c:a aac -b:a 192k -shortest \"{3}\" -y",
            targetFPS,
            Path.Combine(framesFolder, "frame_%06d.jpg"),
            audioPath,
            outputVideo
        );
        
        Debug.Log("[FrameCaptureRecorder] FFmpeg command: " + ffmpegPath + " " + arguments);
        
        // Ejecutar FFmpeg en un proceso separado
        System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = ffmpegPath,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        
        System.Diagnostics.Process process = new System.Diagnostics.Process { StartInfo = startInfo };
        
        string output = "";
        error = "";
        
        process.OutputDataReceived += (sender, e) => { if (e.Data != null) output += e.Data + "\n"; };
        process.ErrorDataReceived += (sender, e) => { if (e.Data != null) error += e.Data + "\n"; };
        
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        
        // Esperar a que FFmpeg termine (en background)
        while (!process.HasExited)
        {
            yield return new WaitForSeconds(0.5f);
        }
        
        process.WaitForExit();
        int exitCode = process.ExitCode;
        process.Close();
        
        if (exitCode == 0 && File.Exists(outputVideo))
        {
            Debug.Log("[FrameCaptureRecorder] ✅ Video created successfully!");
            Debug.Log("[FrameCaptureRecorder] Output: " + outputVideo);
            
            // Opcional: Limpiar frames temporales
            // Directory.Delete(framesFolder, true);
            
            onComplete?.Invoke(true, outputVideo);
        }
        else
        {
            // FFmpeg often creates the output file before failing; remove empty leftovers.
            try
            {
                if (File.Exists(outputVideo))
                {
                    FileInfo fi = new FileInfo(outputVideo);
                    if (fi.Length == 0)
                        File.Delete(outputVideo);
                }
            }
            catch
            {
            }

            string errorMsg = "FFmpeg failed with exit code " + exitCode + "\n" + error;
            Debug.LogError("[FrameCaptureRecorder] " + errorMsg);
            onComplete?.Invoke(false, errorMsg);
        }
#endif
    }
    
    /// <summary>
    /// Obtiene la duración estimada del video
    /// </summary>
    public float GetEstimatedDuration()
    {
        if (!isCapturing && frameCount == 0)
            return 0f;
        
        return frameCount / (float)targetFPS;
    }
    
    void OnDestroy()
    {
        if (isCapturing)
        {
            StopCapture();
        }
    }
    
    void OnApplicationQuit()
    {
        if (isCapturing)
        {
            StopCapture();
        }
    }
}
