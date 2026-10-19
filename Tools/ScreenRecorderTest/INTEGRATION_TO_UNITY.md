# 🔄 Integración del Código Probado a Unity

## Workflow Completo

```
1. Desarrollar & Probar en Mini-App ✅
    ↓
2. Portar a Unity Plugin
    ↓
3. Crear Bridge C# ↔ Java
    ↓
4. Compilar Unity APK
```

---

## Paso 1: Código Ya Probado ✅

Has testeado y verificado que funciona:

```
Tools/ScreenRecorderTest/app/src/main/java/com/cardwarskingdom/recordertest/
└── ScreenRecorderEngine.java  ⭐ Código funcional
```

---

## Paso 2: Copiar a Unity Plugin

### Opción A: Clase Standalone (Recomendado)

```powershell
# Ubicación en Unity
$unityPlugin = "Assets\Plugins\Android\src\com\cardwarskingdom\videoencoder"

# Copiar y renombrar el namespace
Copy-Item "Tools\ScreenRecorderTest\app\src\main\java\com\cardwarskingdom\recordertest\ScreenRecorderEngine.java" `
          "$unityPlugin\ScreenRecorderEngine.java"
```

**Modificar manualmente:**

```java
// ANTES (en mini-app)
package com.cardwarskingdom.recordertest;

import android.content.Context;
// ...

public class ScreenRecorderEngine {
    public ScreenRecorderEngine(Context context, int resultCode, Intent data, String outputPath) {
        this.context = context;
        // ...
    }
}

// DESPUÉS (en Unity)
package com.cardwarskingdom.videoencoder;

import com.unity3d.player.UnityPlayer;
// ... (agregar imports de Unity)

public class ScreenRecorderEngine {
    public ScreenRecorderEngine(int resultCode, Intent data, String outputPath) {
        // Unity no pasa Context, obtenerlo así:
        this.context = UnityPlayer.currentActivity;
        // ...
    }
}
```

### Opción B: Integrar a Clase Existente

Si ya tienes `ScreenRecorderNative.java`, puedes copiar solo los métodos relevantes.

---

## Paso 3: Crear Bridge C# ↔ Java

### 3.1. Crear Wrapper C# en Unity

```csharp
// Assets/Scripts/Assembly-CSharp/NativeScreenRecorder.cs

using UnityEngine;
using System;

#if UNITY_ANDROID && !UNITY_EDITOR
public class NativeScreenRecorder : MonoBehaviour
{
    private const int REQUEST_CODE_SCREEN_CAPTURE = 1000;
    
    private AndroidJavaObject recorderEngine;
    private AndroidJavaObject projectionManager;
    private Action<bool> permissionCallback;
    
    public void RequestPermission(Action<bool> callback)
    {
        permissionCallback = callback;
        
        AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        
        // Obtener MediaProjectionManager
        AndroidJavaObject context = activity.Call<AndroidJavaObject>("getApplicationContext");
        projectionManager = context.Call<AndroidJavaObject>("getSystemService", "media_projection");
        
        // Solicitar permiso
        AndroidJavaObject captureIntent = projectionManager.Call<AndroidJavaObject>("createScreenCaptureIntent");
        activity.Call("startActivityForResult", captureIntent, REQUEST_CODE_SCREEN_CAPTURE);
    }
    
    public void StartRecording(int resultCode, AndroidJavaObject data, string outputPath)
    {
        try
        {
            // Crear instancia de ScreenRecorderEngine
            recorderEngine = new AndroidJavaObject(
                "com.cardwarskingdom.videoencoder.ScreenRecorderEngine",
                resultCode,
                data,
                outputPath
            );
            
            // Configurar callback
            ScreenRecorderCallback callback = new ScreenRecorderCallback();
            callback.OnStarted += () => Debug.Log("✅ Grabación iniciada");
            callback.OnStopped += (path) => Debug.Log($"✅ Video guardado: {path}");
            callback.OnError += (error) => Debug.LogError($"❌ Error: {error}");
            
            recorderEngine.Call("setCallback", callback);
            
            // Iniciar grabación
            recorderEngine.Call("start");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error starting recording: {e.Message}");
        }
    }
    
    public void StopRecording()
    {
        if (recorderEngine != null)
        {
            recorderEngine.Call("stop");
        }
    }
    
    // Unity recibirá este mensaje desde Java
    public void OnScreenCaptureResult(string resultData)
    {
        // Parsear resultado: "resultCode|intentDataUri"
        string[] parts = resultData.Split('|');
        int resultCode = int.Parse(parts[0]);
        bool granted = resultCode == -1; // RESULT_OK
        
        permissionCallback?.Invoke(granted);
    }
}

// Callback adapter para Java
class ScreenRecorderCallback : AndroidJavaProxy
{
    public event Action OnStarted;
    public event Action<string> OnStopped;
    public event Action<string> OnError;
    
    public ScreenRecorderCallback() : base("com.cardwarskingdom.videoencoder.ScreenRecorderEngine$RecorderCallback")
    {
    }
    
    void onRecordingStarted()
    {
        OnStarted?.Invoke();
    }
    
    void onRecordingStopped(string outputPath)
    {
        OnStopped?.Invoke(outputPath);
    }
    
    void onError(string error)
    {
        OnError?.Invoke(error);
    }
}
#endif
```

### 3.2. Modificar Activity de Unity

Crear o modificar `UnityPlayerActivityExtended.java`:

```java
package com.cardwarskingdom.videoencoder;

import android.content.Intent;
import android.os.Bundle;
import com.unity3d.player.UnityPlayerActivity;

public class UnityPlayerActivityExtended extends UnityPlayerActivity {
    private static final int REQUEST_CODE_SCREEN_CAPTURE = 1000;
    private static final String UNITY_GAME_OBJECT = "RecordingManager";
    
    @Override
    protected void onActivityResult(int requestCode, int resultCode, Intent data) {
        super.onActivityResult(requestCode, resultCode, data);
        
        if (requestCode == REQUEST_CODE_SCREEN_CAPTURE) {
            // Enviar resultado a Unity
            String result = resultCode + "|" + (data != null ? data.toUri(0) : "");
            UnityPlayer.UnitySendMessage(UNITY_GAME_OBJECT, "OnScreenCaptureResult", result);
        }
    }
}
```

### 3.3. Actualizar AndroidManifest.xml

```xml
<application ...>
    <activity
        android:name="com.cardwarskingdom.videoencoder.UnityPlayerActivityExtended"
        android:exported="true"
        ...>
        <intent-filter>
            <action android:name="android.intent.action.MAIN" />
            <category android:name="android.intent.category.LAUNCHER" />
        </intent-filter>
    </activity>
</application>
```

---

## Paso 4: Uso en Unity

### En tu script de batalla (`BattleController.cs` o similar):

```csharp
#if UNITY_ANDROID && !UNITY_EDITOR

using UnityEngine;
using System.IO;

public class BattleController : MonoBehaviour
{
    private NativeScreenRecorder recorder;
    private string outputPath;
    
    void Start()
    {
        // Crear recorder
        GameObject recorderObj = new GameObject("RecordingManager");
        recorder = recorderObj.AddComponent<NativeScreenRecorder>();
        DontDestroyOnLoad(recorderObj);
    }
    
    public void OnBattleStart()
    {
        Debug.Log("Batalla iniciada, solicitando permiso de grabación...");
        
        recorder.RequestPermission((granted) =>
        {
            if (granted)
            {
                // Generar path de salida
                string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
                outputPath = Path.Combine(
                    Application.persistentDataPath,
                    "Replays",
                    $"battle_{timestamp}.mp4"
                );
                
                // Iniciar grabación
                // (resultCode y data vienen del callback de Activity)
                recorder.StartRecording(-1, intentData, outputPath);
                
                Debug.Log($"✅ Grabación iniciada: {outputPath}");
            }
            else
            {
                Debug.LogWarning("⚠️ Permiso de grabación denegado");
            }
        });
    }
    
    public void OnBattleEnd()
    {
        Debug.Log("Batalla terminada, deteniendo grabación...");
        recorder.StopRecording();
    }
}

#endif
```

---

## Paso 5: Build & Test

### En Unity:

1. **File → Build Settings → Android**
2. **Player Settings:**
   - Minimum API Level: 24+
   - Target API Level: 35
   - Scripting Backend: IL2CPP
3. **Build and Run**

### Verificar:

```powershell
# Ver logs
adb logcat -s Unity ScreenRecorderEngine

# Listar videos generados
adb shell ls -lh /sdcard/Android/data/com.cwkrevived.cwkingdom/files/Replays/

# Descargar video
adb pull /sdcard/Android/data/com.cwkrevived.cwkingdom/files/Replays/battle_XXXXXX.mp4 .
```

---

## Troubleshooting

### ❌ "Class not found: ScreenRecorderEngine"

**Causa:** El archivo Java no se compiló en el APK.

**Solución:**
```
1. Verificar estructura de carpetas coincide con package
2. Verificar .meta no excluye Android
3. Clean build de Unity
```

### ❌ "MediaProjection is null"

**Causa:** Permiso no concedido correctamente.

**Solución:**
- Verificar que `onActivityResult` se llama
- Log en Java para debug
- Probar primero en mini-app

### ❌ Funciona en mini-app pero no en Unity

**Diferencias comunes:**
- Context: Unity usa `UnityPlayer.currentActivity`
- Lifecycle: Unity puede pausar/resumir la Activity
- Threading: Unity tiene su propio thread principal

---

## Optimizaciones Adicionales

### 1. Segmentación de Video (5-10 min por archivo)

```java
// En ScreenRecorderEngine.java
private static final long MAX_DURATION_MS = 10 * 60 * 1000;
private long recordingStartTime;

private void drainEncoder(boolean endOfStream) {
    // ... código existente ...
    
    // Verificar duración
    long elapsed = System.currentTimeMillis() - recordingStartTime;
    if (elapsed > MAX_DURATION_MS) {
        // Cerrar muxer actual
        stopRecordingInternal();
        
        // Iniciar nuevo segmento
        String newPath = outputPath.replace(".mp4", "_part" + (++segmentIndex) + ".mp4");
        startNewSegment(newPath);
    }
}
```

### 2. Almacenamiento Inteligente

```csharp
// Usar MediaStore para Android 10+
string outputDir = Application.persistentDataPath;

#if UNITY_ANDROID && !UNITY_EDITOR
if (Application.platform == RuntimePlatform.Android)
{
    AndroidJavaClass environment = new AndroidJavaClass("android.os.Environment");
    AndroidJavaObject moviesDir = environment.CallStatic<AndroidJavaObject>("getExternalStoragePublicDirectory", 
        environment.GetStatic<string>("DIRECTORY_MOVIES"));
    outputDir = moviesDir.Call<string>("getAbsolutePath");
}
#endif
```

### 3. Notificación Durante Grabación

```java
// Mostrar notificación persistente
Notification notification = new NotificationCompat.Builder(context, CHANNEL_ID)
    .setContentTitle("Card Wars Kingdom")
    .setContentText("🔴 Grabando batalla...")
    .setSmallIcon(R.drawable.ic_record)
    .setOngoing(true)
    .build();

NotificationManager manager = context.getSystemService(NotificationManager.class);
manager.notify(NOTIFICATION_ID, notification);
```

---

## Checklist de Integración

```
☐ Código probado en mini-app funciona correctamente
☐ ScreenRecorderEngine.java copiado a Unity
☐ Package name actualizado
☐ Context cambiado a UnityPlayer.currentActivity
☐ NativeScreenRecorder.cs creado
☐ UnityPlayerActivityExtended.java creado
☐ AndroidManifest.xml actualizado
☐ Permisos correctos en manifest
☐ Build desde Unity exitoso
☐ Probado en dispositivo real
☐ Videos se generan correctamente
☐ Logs muestran proceso completo
```

---

## 🎉 ¡Listo!

Ahora tienes:
1. ✅ Código nativo probado y funcional
2. ✅ Integración con Unity lista
3. ✅ Sistema de grabación eficiente para 8GB RAM
4. ✅ Grabación DirectToMP4 sin frames intermedios

**Resultado:** Grabación de pantalla de alta calidad con mínimo impacto en rendimiento.
