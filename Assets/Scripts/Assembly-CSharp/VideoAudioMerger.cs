using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using UnityEngine;

/// <summary>
/// Utilidad para combinar video y audio usando FFmpeg
/// Combina replay.mp4 (video sin audio) + replay_audio.wav (audio) → replay_final.mp4
/// </summary>
public class VideoAudioMerger
{
    private const long MinValidMp4Bytes = 1024;

    /// <summary>
    /// Combina video y audio en un solo archivo MP4
    /// </summary>
    /// <param name="videoPath">Ruta al archivo de video (replay.mp4)</param>
    /// <param name="audioPath">Ruta al archivo de audio (replay_audio.wav)</param>
    /// <param name="outputPath">Ruta para el video final (replay_final.mp4)</param>
    /// <param name="onComplete">Callback cuando termina (éxito/fracaso)</param>
    public static void MergeVideoAudio(string videoPath, string audioPath, string outputPath, Action<bool, string> onComplete = null)
    {
        // Verificar que los archivos existan
        if (!File.Exists(videoPath))
        {
            string error = "Video file not found: " + videoPath;
            UnityEngine.Debug.LogError("[VideoAudioMerger] " + error);
            onComplete?.Invoke(false, error);
            return;
        }

        if (!File.Exists(audioPath))
        {
            string error = "Audio file not found: " + audioPath;
            UnityEngine.Debug.LogError("[VideoAudioMerger] " + error);
            onComplete?.Invoke(false, error);
            return;
        }

        // Buscar FFmpeg
        string ffmpegPath = FindFFmpegExecutable();
        if (string.IsNullOrEmpty(ffmpegPath))
        {
            string error = "FFmpeg not found. Video and audio files are kept separate.";
            UnityEngine.Debug.LogWarning("[VideoAudioMerger] " + error);
            onComplete?.Invoke(false, error);
            return;
        }

        UnityEngine.Debug.Log("[VideoAudioMerger] Merging video and audio...");
        UnityEngine.Debug.Log("[VideoAudioMerger] Video: " + videoPath);
        UnityEngine.Debug.Log("[VideoAudioMerger] Audio: " + audioPath);
        UnityEngine.Debug.Log("[VideoAudioMerger] Output: " + outputPath);

        try
        {
            // Comando FFmpeg: combinar video + audio
            // -i video.mp4 = input video
            // -i audio.wav = input audio
            // -c:v copy = copiar video sin recodificar (rápido)
            // -c:a aac = codificar audio a AAC
            // -shortest = terminar cuando termine el más corto
            string arguments = string.Format(
                "-i \"{0}\" -i \"{1}\" -c:v copy -c:a aac -b:a 192k -shortest \"{2}\" -y",
                videoPath,
                audioPath,
                outputPath
            );

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            Process process = new Process { StartInfo = startInfo };
            
            string output = "";
            string error = "";

            process.OutputDataReceived += (sender, e) => { if (e.Data != null) output += e.Data + "\n"; };
            process.ErrorDataReceived += (sender, e) => { if (e.Data != null) error += e.Data + "\n"; };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();

            int exitCode = process.ExitCode;
            process.Close();

            if (exitCode == 0 && File.Exists(outputPath))
            {
                UnityEngine.Debug.Log("[VideoAudioMerger] ✅ Success! Merged video saved to: " + outputPath);
                
                // Opcional: Eliminar archivos temporales
                // File.Delete(videoPath);
                // File.Delete(audioPath);
                
                onComplete?.Invoke(true, outputPath);
            }
            else
            {
                string errorMsg = "FFmpeg failed with exit code " + exitCode + "\n" + error;
                UnityEngine.Debug.LogError("[VideoAudioMerger] " + errorMsg);
                onComplete?.Invoke(false, errorMsg);
            }
        }
        catch (Exception ex)
        {
            string errorMsg = "Exception merging video and audio: " + ex.Message;
            UnityEngine.Debug.LogError("[VideoAudioMerger] " + errorMsg);
            onComplete?.Invoke(false, errorMsg);
        }
    }

    /// <summary>
    /// Combina una secuencia de frames (frame_%06d.jpg) + audio WAV en un MP4 H.264.
    /// Se ejecuta como Coroutine para no bloquear el hilo principal.
    /// Incluye padding automático a dimensiones pares y fuerza yuv420p.
    /// </summary>
    public static IEnumerator MergeFramesWithAudioCoroutine(
        string framesFolder,
        string audioPath,
        string outputPath,
        int framerate,
        Action<bool, string> onComplete = null)
    {
        if (string.IsNullOrEmpty(framesFolder) || !Directory.Exists(framesFolder))
        {
            string error = "Frames folder not found: " + framesFolder;
            UnityEngine.Debug.LogError("[VideoAudioMerger] " + error);
            onComplete?.Invoke(false, error);
            yield break;
        }

        if (string.IsNullOrEmpty(audioPath) || !File.Exists(audioPath))
        {
            string error = "Audio file not found: " + audioPath;
            UnityEngine.Debug.LogError("[VideoAudioMerger] " + error);
            onComplete?.Invoke(false, error);
            yield break;
        }

        // If we already have a valid file, just return it.
        try
        {
            if (File.Exists(outputPath))
            {
                FileInfo existing = new FileInfo(outputPath);
                if (existing.Length > MinValidMp4Bytes)
                {
                    onComplete?.Invoke(true, outputPath);
                    yield break;
                }

                // Delete empty/invalid leftovers so they don't get "picked up" later.
                if (existing.Length == 0)
                {
                    File.Delete(outputPath);
                }
            }
        }
        catch
        {
        }

        // Ensure we actually have frames.
        string[] frames = null;
        try
        {
            frames = Directory.GetFiles(framesFolder, "frame_*.jpg");
        }
        catch (Exception ex)
        {
            string error = "Failed to enumerate frames: " + ex.Message;
            UnityEngine.Debug.LogError("[VideoAudioMerger] " + error);
            onComplete?.Invoke(false, error);
            yield break;
        }

        if (frames == null || frames.Length == 0)
        {
            string error = "No frames found in: " + framesFolder;
            UnityEngine.Debug.LogError("[VideoAudioMerger] " + error);
            onComplete?.Invoke(false, error);
            yield break;
        }

        string ffmpegPath = FindFFmpegExecutable();
        if (string.IsNullOrEmpty(ffmpegPath))
        {
            string error = "FFmpeg not found. Cannot combine frames.";
            UnityEngine.Debug.LogWarning("[VideoAudioMerger] " + error);
            onComplete?.Invoke(false, error);
            yield break;
        }

        // NOTE: x264 requires even dimensions; pad to even and force yuv420p for compatibility.
        string framePattern = Path.Combine(framesFolder, "frame_%06d.jpg");
        string arguments = string.Format(
            "-framerate {0} -i \"{1}\" -i \"{2}\" -vf \"pad=ceil(iw/2)*2:ceil(ih/2)*2:(ow-iw)/2:(oh-ih)/2\" -c:v libx264 -pix_fmt yuv420p -preset fast -crf 23 -c:a aac -b:a 192k -shortest \"{3}\" -y",
            Mathf.Max(1, framerate),
            framePattern,
            audioPath,
            outputPath
        );

        UnityEngine.Debug.Log("[VideoAudioMerger] Merging frames+audio with FFmpeg...");
        UnityEngine.Debug.Log("[VideoAudioMerger] FFmpeg: " + ffmpegPath);
        UnityEngine.Debug.Log("[VideoAudioMerger] Args: " + arguments);

        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = ffmpegPath,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        Process process = new Process { StartInfo = startInfo };
        string output = "";
        string errorOut = "";

        process.OutputDataReceived += (sender, e) => { if (e.Data != null) output += e.Data + "\n"; };
        process.ErrorDataReceived += (sender, e) => { if (e.Data != null) errorOut += e.Data + "\n"; };

        try
        {
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
        }
        catch (Exception ex)
        {
            string error = "Failed to start FFmpeg: " + ex.Message;
            UnityEngine.Debug.LogError("[VideoAudioMerger] " + error);
            onComplete?.Invoke(false, error);
            yield break;
        }

        while (!process.HasExited)
        {
            yield return new WaitForSeconds(0.25f);
        }

        process.WaitForExit();
        int exitCode = process.ExitCode;
        process.Close();

        bool success = false;
        try
        {
            if (exitCode == 0 && File.Exists(outputPath) && new FileInfo(outputPath).Length > MinValidMp4Bytes)
            {
                success = true;
            }
        }
        catch
        {
        }

        if (success)
        {
            UnityEngine.Debug.Log("[VideoAudioMerger] ✅ Success! Merged video saved to: " + outputPath);
            onComplete?.Invoke(true, outputPath);
            yield break;
        }

        // Clean up empty leftover output.
        try
        {
            if (File.Exists(outputPath) && new FileInfo(outputPath).Length == 0)
                File.Delete(outputPath);
        }
        catch
        {
        }

        string errorMsg = "FFmpeg failed with exit code " + exitCode + "\n" + errorOut;
        UnityEngine.Debug.LogError("[VideoAudioMerger] " + errorMsg);
        onComplete?.Invoke(false, errorMsg);
    }

    /// <summary>
    /// Busca el ejecutable de FFmpeg en ubicaciones comunes
    /// </summary>
    public static string FindFFmpegExecutable()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        // Android: Buscar en ubicaciones específicas para Android
        
        // 1. En persistentDataPath (donde podemos extraer archivos de StreamingAssets)
        string persistentPath = Path.Combine(Application.persistentDataPath, "ffmpeg", "ffmpeg");
        if (File.Exists(persistentPath))
        {
            UnityEngine.Debug.Log("[VideoAudioMerger] Found FFmpeg in persistentDataPath");
            return persistentPath;
        }
        
        // 2. Intentar extraer de StreamingAssets si existe
        string streamingPath = Path.Combine(Application.streamingAssetsPath, "ffmpeg", "ffmpeg");
        if (ExtractFFmpegFromStreamingAssets())
        {
            // Después de extraer, verificar de nuevo
            if (File.Exists(persistentPath))
            {
                // Dar permisos de ejecución
                try
                {
                    // En Android, usar chmod para dar permisos
                    System.Diagnostics.ProcessStartInfo chmodInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "chmod",
                        Arguments = "755 " + persistentPath,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    System.Diagnostics.Process.Start(chmodInfo)?.WaitForExit();
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogWarning("[VideoAudioMerger] Could not set FFmpeg permissions: " + ex.Message);
                }
                
                UnityEngine.Debug.Log("[VideoAudioMerger] FFmpeg extracted from StreamingAssets");
                return persistentPath;
            }
        }
        
        UnityEngine.Debug.LogWarning("[VideoAudioMerger] FFmpeg not found on Android. Video encoding will not work.");
        UnityEngine.Debug.LogWarning("[VideoAudioMerger] Please include FFmpeg binary in StreamingAssets/ffmpeg/ffmpeg");
        return null;
#else
        // Windows/Editor: Usar búsqueda de Windows
        
        // 1. En carpeta Tools (fuera de Assets, NO se incluye en builds)
        string toolsPath = Path.Combine(Application.dataPath, "..", "Tools", "ffmpeg.exe");
        if (File.Exists(toolsPath))
        {
            UnityEngine.Debug.Log("[VideoAudioMerger] Found FFmpeg in Tools folder (Editor only)");
            return Path.GetFullPath(toolsPath);
        }
        
        // 2. En StreamingAssets/Windows (SE INCLUYE SOLO en builds de Windows)
        string windowsBuildPath = Path.Combine(Application.streamingAssetsPath, "Windows", "ffmpeg.exe");
        if (File.Exists(windowsBuildPath))
        {
            UnityEngine.Debug.Log("[VideoAudioMerger] Found FFmpeg in StreamingAssets/Windows/ (Windows builds)");
            return windowsBuildPath;
        }
        
        // 3. En la carpeta del proyecto (legacy)
        string projectPath = Path.Combine(Application.dataPath, "..", "ffmpeg", "bin", "ffmpeg.exe");
        if (File.Exists(projectPath))
        {
            UnityEngine.Debug.Log("[VideoAudioMerger] Found FFmpeg in project folder");
            return Path.GetFullPath(projectPath);
        }

        // 4. En StreamingAssets/ffmpeg (ADVERTENCIA: se incluirá en TODOS los builds)
        string streamingPath = Path.Combine(Application.streamingAssetsPath, "ffmpeg", "ffmpeg.exe");
        if (File.Exists(streamingPath))
        {
            UnityEngine.Debug.LogWarning("[VideoAudioMerger] Found FFmpeg in StreamingAssets/ffmpeg - This will be included in ALL platforms!");
            UnityEngine.Debug.LogWarning("[VideoAudioMerger] Move to StreamingAssets/Windows/ to only include in Windows builds");
            return streamingPath;
        }

        // 4. En PATH del sistema
        try
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "where",
                Arguments = "ffmpeg",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            Process process = Process.Start(startInfo);
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode == 0 && !string.IsNullOrEmpty(output))
            {
                string path = output.Split('\n')[0].Trim();
                if (File.Exists(path))
                {
                    UnityEngine.Debug.Log("[VideoAudioMerger] Found FFmpeg in system PATH");
                    return path;
                }
            }
        }
        catch
        {
            // Ignorar error
        }

        // 5. En ubicaciones comunes
        string[] commonPaths = new string[]
        {
            @"C:\ffmpeg\bin\ffmpeg.exe",
            @"C:\Program Files\ffmpeg\bin\ffmpeg.exe",
            @"C:\Program Files (x86)\ffmpeg\bin\ffmpeg.exe",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "ffmpeg", "bin", "ffmpeg.exe")
        };

        foreach (string path in commonPaths)
        {
            if (File.Exists(path))
            {
                UnityEngine.Debug.Log("[VideoAudioMerger] Found FFmpeg at: " + path);
                return path;
            }
        }

        UnityEngine.Debug.LogWarning("[VideoAudioMerger] FFmpeg not found. Install from: https://ffmpeg.org/download.html");
        return null;
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    /// <summary>
    /// Extrae FFmpeg de StreamingAssets a persistentDataPath en Android
    /// </summary>
    private static bool ExtractFFmpegFromStreamingAssets()
    {
        try
        {
            string sourcePath = Path.Combine(Application.streamingAssetsPath, "ffmpeg", "ffmpeg");
            string destPath = Path.Combine(Application.persistentDataPath, "ffmpeg", "ffmpeg");
            
            // Crear directorio si no existe
            string destDir = Path.GetDirectoryName(destPath);
            if (!Directory.Exists(destDir))
            {
                Directory.CreateDirectory(destDir);
            }
            
            // En Android, StreamingAssets está dentro del APK, usar UnityWebRequest
            UnityEngine.Networking.UnityWebRequest www = UnityEngine.Networking.UnityWebRequest.Get(sourcePath);
            www.SendWebRequest();
            
            // Esperar a que termine (bloquear)
            while (!www.isDone)
            {
                System.Threading.Thread.Sleep(10);
            }
            
            if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                File.WriteAllBytes(destPath, www.downloadHandler.data);
                UnityEngine.Debug.Log("[VideoAudioMerger] FFmpeg extracted successfully");
                return true;
            }
            else
            {
                UnityEngine.Debug.LogWarning("[VideoAudioMerger] Could not extract FFmpeg: " + www.error);
                return false;
            }
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogWarning("[VideoAudioMerger] Error extracting FFmpeg: " + ex.Message);
            return false;
        }
    }
#endif

    /// <summary>
    /// Verifica si FFmpeg está instalado
    /// </summary>
    public static bool IsFFmpegAvailable()
    {
        return !string.IsNullOrEmpty(FindFFmpegExecutable());
    }

    /// <summary>
    /// Muestra instrucciones de instalación de FFmpeg
    /// </summary>
    public static void ShowFFmpegInstallInstructions()
    {
        string message = @"FFmpeg is not installed or not found.

To merge video and audio automatically:

1. Download FFmpeg from: https://ffmpeg.org/download.html
2. Extract to: C:\ffmpeg\
3. Add to PATH or place ffmpeg.exe in project folder

Alternative:
- Keep video (replay.mp4) and audio (replay_audio.wav) separate
- Use any video editor to combine them manually
- Or use the videos as-is (video without audio)";

        UnityEngine.Debug.LogWarning(message);
    }
}
