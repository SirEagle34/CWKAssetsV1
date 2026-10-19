using UnityEngine;
using System.Collections;

/// <summary>
/// Helper para debug rápido de grabación de video
/// Presiona F8 para ejecutar diagnóstico
/// Presiona F9 para iniciar/detener grabación manual
/// </summary>
public class RecordingDebugHelper : MonoBehaviour
{
    [Header("Debug Keys")]
    [SerializeField] private KeyCode diagnosticKey = KeyCode.F8;
    [SerializeField] private KeyCode recordToggleKey = KeyCode.F9;
    [SerializeField] private KeyCode showInfoKey = KeyCode.F10;

    [Header("Display")]
    [SerializeField] private bool showOnScreenInfo = true;
    [SerializeField] private int fontSize = 24;
    
    private bool isManualRecording = false;
    private float recordingTime = 0f;
    private string statusMessage = "";

    private void Update()
    {
        // F8: Ejecutar diagnóstico completo
        if (Input.GetKeyDown(diagnosticKey))
        {
            ExecuteDiagnostic();
        }

        // F9: Toggle grabación manual
        if (Input.GetKeyDown(recordToggleKey))
        {
            ToggleManualRecording();
        }

        // F10: Mostrar info
        if (Input.GetKeyDown(showInfoKey))
        {
            ShowRecordingInfo();
        }

        // Actualizar timer
        if (isManualRecording)
        {
            recordingTime += Time.deltaTime;
        }
    }

    private void OnGUI()
    {
        if (!showOnScreenInfo)
            return;

        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = fontSize;
        style.normal.textColor = Color.white;
        style.alignment = TextAnchor.UpperLeft;

        // Fondo semi-transparente
        GUI.Box(new Rect(10, 10, 400, 150), "");

        string info = "<b>RECORDING DEBUG</b>\n";
        info += $"{diagnosticKey}: Run Diagnostic\n";
        info += $"{recordToggleKey}: Toggle Recording\n";
        info += $"{showInfoKey}: Show Info\n\n";

        if (MobileVideoRecorder.Instance != null)
        {
            bool recording = MobileVideoRecorder.Instance.IsRecording;
            info += $"Status: {(recording ? "<color=red>● RECORDING</color>" : "<color=green>● READY</color>")}\n";
            
            if (isManualRecording)
            {
                info += $"Time: {recordingTime:F1}s";
            }
        }
        else
        {
            info += "<color=yellow>MobileVideoRecorder not found</color>";
        }

        if (!string.IsNullOrEmpty(statusMessage))
        {
            info += $"\n\n{statusMessage}";
        }

        GUI.Label(new Rect(20, 20, 380, 130), info, style);
    }

    private void ExecuteDiagnostic()
    {
        Debug.Log("[RecordingDebugHelper] Executing diagnostic...");
        statusMessage = "Running diagnostic... check logs";
        
        if (AndroidRecordingDiagnostic.Instance != null)
        {
            AndroidRecordingDiagnostic.RunTest();
        }
        else
        {
            Debug.LogWarning("[RecordingDebugHelper] AndroidRecordingDiagnostic not found in scene");
            statusMessage = "Diagnostic script not found!";
        }

        StartCoroutine(ClearStatusAfterDelay(3f));
    }

    private void ToggleManualRecording()
    {
        if (MobileVideoRecorder.Instance == null)
        {
            Debug.LogWarning("[RecordingDebugHelper] MobileVideoRecorder not found");
            statusMessage = "MobileVideoRecorder not found!";
            StartCoroutine(ClearStatusAfterDelay(3f));
            return;
        }

        if (!isManualRecording)
        {
            // Iniciar grabación
            Debug.Log("[RecordingDebugHelper] Starting manual recording...");
            MobileVideoRecorder.Instance.StartRecording();
            isManualRecording = true;
            recordingTime = 0f;
            statusMessage = "Recording started!";
        }
        else
        {
            // Detener grabación
            Debug.Log("[RecordingDebugHelper] Stopping manual recording...");
            MobileVideoRecorder.Instance.StopRecording();
            isManualRecording = false;
            statusMessage = $"Recording stopped ({recordingTime:F1}s).\nProcessing video...";
        }

        StartCoroutine(ClearStatusAfterDelay(3f));
    }

    private void ShowRecordingInfo()
    {
        Debug.Log("=== RECORDING INFO ===");
        Debug.Log($"Platform: {Application.platform}");
        Debug.Log($"Persistent Path: {Application.persistentDataPath}");
        
        if (MobileVideoRecorder.Instance != null)
        {
            Debug.Log($"Is Recording: {MobileVideoRecorder.Instance.IsRecording}");
        }

        // Verificar última grabación
        string exportPath = System.IO.Path.Combine(Application.persistentDataPath, "ExportedReplays");
        if (System.IO.Directory.Exists(exportPath))
        {
            string[] dirs = System.IO.Directory.GetDirectories(exportPath, "Video_*");
            Debug.Log($"Found {dirs.Length} video folders");
            
            if (dirs.Length > 0)
            {
                string latest = dirs[dirs.Length - 1];
                Debug.Log($"Latest: {latest}");
                
                // Verificar contenido
                string mp4 = System.IO.Path.Combine(latest, "replay_final.mp4");
                string framesDir = System.IO.Path.Combine(latest, "frames");
                string audioFile = System.IO.Path.Combine(latest, "replay_audio.wav");
                
                Debug.Log($"  MP4 exists: {System.IO.File.Exists(mp4)}");
                Debug.Log($"  Audio exists: {System.IO.File.Exists(audioFile)}");
                
                if (System.IO.Directory.Exists(framesDir))
                {
                    string[] frames = System.IO.Directory.GetFiles(framesDir, "frame_*.jpg");
                    Debug.Log($"  Frames: {frames.Length}");
                }
            }
        }

        statusMessage = "Info logged to console!";
        StartCoroutine(ClearStatusAfterDelay(3f));
    }

    private IEnumerator ClearStatusAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        statusMessage = "";
    }

    private void OnApplicationQuit()
    {
        // Si estamos grabando al cerrar, detener
        if (isManualRecording && MobileVideoRecorder.Instance != null)
        {
            Debug.Log("[RecordingDebugHelper] Stopping recording on quit");
            MobileVideoRecorder.Instance.StopRecording();
        }
    }
}
