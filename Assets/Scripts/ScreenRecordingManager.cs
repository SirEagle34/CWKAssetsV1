using UnityEngine;
using System;

#if UNITY_ANDROID
using UnityEngine.Android;
#endif

/// <summary>
/// Manager para control de grabación de pantalla en Android 14+/15
/// Integra ScreenRecorderEngine y RecordingService desde Plugins/Android
/// 
/// FLUJO:
/// 1. RequestPermissions() - solicita RECORD_AUDIO, WRITE_EXTERNAL_STORAGE
/// 2. StartRecording() - llama a UnityPlayerActivityExtended.startScreenCapture()
/// 3. UnityPlayerActivityExtended.onActivityResult() maneja la respuesta de MediaProjection
/// 4. RecordingService recibe resultCode + data y ejecuta la grabación
/// 5. StopRecording() - detiene la grabación
/// </summary>
public class ScreenRecordingManager : MonoBehaviour
{
    private static readonly string TAG = "ScreenRecordingManager";

#if UNITY_ANDROID && !UNITY_EDITOR
    private AndroidJavaObject currentActivity;
#endif

    // Eventos para notificar estado
    public static event Action OnRecordingStarted;
    public static event Action<string> OnRecordingStopped;
    public static event Action<string> OnRecordingError;

    private bool isRecording = false;

    private void Awake()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        InitializeAndroidComponents();
#endif
    }

    private void InitializeAndroidComponents()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            
            if (currentActivity != null)
            {
                Debug.Log(TAG + ": Activity inicializada OK");
            }
        }
        catch (Exception e)
        {
            Debug.LogError(TAG + ": Error inicializando componentes Android: " + e.Message);
        }
#endif
    }

    /// <summary>
    /// Solicita permisos de grabación necesarios
    /// </summary>
    public void RequestPermissions()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            // Verificar y solicitar RECORD_AUDIO
            if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
            {
                Debug.Log(TAG + ": Solicitando permiso RECORD_AUDIO...");
                Permission.RequestUserPermission(Permission.Microphone);
            }

            Debug.Log(TAG + ": Permisos solicitados");
        }
        catch (Exception e)
        {
            Debug.LogError(TAG + ": Error solicitando permisos: " + e.Message);
        }
#endif
    }

    /// <summary>
    /// Inicia la grabación de pantalla llamando a UnityPlayerActivityExtended.startScreenCapture()
    /// </summary>
    public void StartRecording()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (isRecording)
        {
            Debug.LogWarning(TAG + ": Ya hay una grabación en progreso");
            return;
        }

        try
        {
            Debug.Log(TAG + ": Iniciando grabación de pantalla...");

            if (currentActivity == null)
            {
                InitializeAndroidComponents();
                if (currentActivity == null)
                {
                    OnRecordingError?.Invoke("Activity no inicializada");
                    return;
                }
            }

            // Llamar al método startScreenCapture() de UnityPlayerActivityExtended
            // Este método lanzará el intent de MediaProjection y manejará onActivityResult
            currentActivity.Call("startScreenCapture");
            
            isRecording = true;
            OnRecordingStarted?.Invoke();
            Debug.Log(TAG + ": startScreenCapture() llamado");
        }
        catch (Exception e)
        {
            Debug.LogError(TAG + ": Error iniciando grabación: " + e.Message);
            OnRecordingError?.Invoke(e.Message);
            isRecording = false;
        }
#endif
    }

    /// <summary>
    /// Detiene la grabación en progreso
    /// </summary>
    public void StopRecording()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!isRecording)
        {
            Debug.LogWarning(TAG + ": No hay grabación en progreso");
            return;
        }

        try
        {
            Debug.Log(TAG + ": Deteniendo grabación...");

            if (currentActivity == null)
            {
                return;
            }

            // Llamar al método stopScreenCapture() de UnityPlayerActivityExtended
            currentActivity.Call("stopScreenCapture");

            isRecording = false;
            Debug.Log(TAG + ": Comando de STOP enviado");
        }
        catch (Exception e)
        {
            Debug.LogError(TAG + ": Error deteniendo grabación: " + e.Message);
        }
#endif
    }

    /// <summary>
    /// Método auxiliar que de ser llamado desde Java (via AndroidJavaProxy si es necesario)
    /// notifica que la grabación finalizó
    /// </summary>
    public void NotifyRecordingStopped(string filePath)
    {
        isRecording = false;
        OnRecordingStopped?.Invoke(filePath);
        Debug.Log(TAG + ": Grabación finalizada: " + filePath);
    }

    /// <summary>
    /// Método auxiliar que notifica errores de grabación
    /// </summary>
    public void NotifyRecordingError(string errorMessage)
    {
        isRecording = false;
        OnRecordingError?.Invoke(errorMessage);
        Debug.LogError(TAG + ": Error de grabación: " + errorMessage);
    }

    public bool IsRecording => isRecording;
}
