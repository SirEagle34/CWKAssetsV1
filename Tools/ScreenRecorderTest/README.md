# 🎬 Screen Recorder Test - Mini App de Prueba

## 📋 ¿Qué es esto?

Una **mini-app Android standalone** para probar el sistema de grabación **MediaProjection + MediaCodec + MediaMuxer** sin necesidad de compilar Unity.

### ✅ Ventajas

- ⚡ **Compilación ultra-rápida**: 15-30 segundos (vs 5-10 minutos de Unity)
- 🔄 **Iteración veloz**: Modifica código Java → compila → prueba en segundos
- 🐛 **Debugging aislado**: Prueba solo el sistema de grabación sin Unity
- 📦 **Tamaño mínimo**: Solo 2-3 MB vs 200+ MB del APK de Unity
- 🎯 **Validación rápida**: Verifica que tu implementación funciona antes de integrar a Unity

## 🚀 Quick Start

### 1️⃣ Primera Compilación

```powershell
cd Tools\ScreenRecorderTest
.\quick_test.ps1
```

Esto automáticamente:
- ✅ Compila el APK
- ✅ Instala en tu dispositivo
- ✅ Ejecuta la app
- ✅ Muestra logs en tiempo real

### 2️⃣ Usar la App

1. **Presiona "▶️ Iniciar Grabación (10 seg)"**
2. **Acepta el permiso** cuando aparezca el diálogo del sistema
3. **Espera 10 segundos** (o presiona "⏹️ Detener" antes)
4. **Verifica el resultado** en los logs

### 3️⃣ Ver el Video Grabado

```powershell
# Listar videos generados
adb shell ls -lh /sdcard/Android/data/com.cardwarskingdom.recordertest/files/Movies/

# Descargar a tu PC
adb pull /sdcard/Android/data/com.cardwarskingdom.recordertest/files/Movies/test_recording_XXXXXX.mp4 .
```

## 📊 Parámetros Configurados

Los parámetros están optimizados para dispositivos con **~8GB RAM** (como tu Nubia Z70 Ultra):

| Parámetro | Valor | Ubicación |
|-----------|-------|-----------|
| **Resolución** | 1280×720 (720p) | `ScreenRecorderEngine.java:29-30` |
| **FPS** | 30 | `ScreenRecorderEngine.java:31` |
| **Bitrate** | 4 Mbps | `ScreenRecorderEngine.java:32` |
| **Codec** | H.264 hardware | `ScreenRecorderEngine.java:34` |
| **I-frame interval** | 2 segundos | `ScreenRecorderEngine.java:33` |

### 🔧 Para Modificar Parámetros

Edita [app/src/main/java/com/cardwarskingdom/recordertest/ScreenRecorderEngine.java](app/src/main/java/com/cardwarskingdom/recordertest/ScreenRecorderEngine.java:29-34):

```java
// Cambiar resolución
private static final int VIDEO_WIDTH = 1920;  // Para 1080p
private static final int VIDEO_HEIGHT = 1080;

// Cambiar bitrate
private static final int VIDEO_BITRATE = 6_000_000; // 6 Mbps

// Cambiar FPS
private static final int VIDEO_FPS = 24;  // Para gameplay más cinético
```

Luego compila de nuevo con `.\quick_test.ps1`

## 🛠️ Comandos Útiles

### Desarrollo Iterativo (Recomendado)

```powershell
# Compilar + Instalar + Ejecutar + Ver logs
.\quick_test.ps1

# Solo compilar (sin instalar)
.\quick_test.ps1 -NoInstall

# Clean build (si algo no funciona)
.\quick_test.ps1 -Clean
```

### Debugging

```powershell
# Ver logs filtrados solo de la app
adb logcat -v time | Select-String "RecorderTest|RecorderEngine"

# Ver todos los errores
adb logcat -v time | Select-String "ERROR|FATAL|AndroidRuntime"

# Limpiar logs
adb logcat -c
```

### Gestión de APK

```powershell
# Desinstalar app
adb uninstall com.cardwarskingdom.recordertest

# Ver procesos en ejecución
adb shell ps | Select-String "recordertest"
```

## 📁 Estructura del Proyecto

```
Tools/ScreenRecorderTest/
├── app/
│   ├── src/main/
│   │   ├── AndroidManifest.xml          # Permisos y configuración
│   │   └── java/com/cardwarskingdom/recordertest/
│   │       ├── MainActivity.java        # UI y control principal
│   │       ├── ScreenRecorderEngine.java ⭐ Motor de grabación
│   │       └── RecordingService.java    # Servicio foreground (opcional)
│   └── build.gradle                     # Config de compilación
├── quick_test.ps1                       ⭐ Script de testing rápido
└── README.md                            # Esta documentación
```

## 🎯 Workflow de Desarrollo

### Ciclo de Iteración Recomendado

```
1. Editar código Java (ej: ScreenRecorderEngine.java)
   ↓
2. Ejecutar: .\quick_test.ps1
   ↓  (15-30 segundos)
3. Ver resultados en logs
   ↓
4. Repetir hasta que funcione perfectamente
   ↓
5. Copiar código probado a Unity plugin
```

### Ejemplo de Sesión Real

```powershell
# Primera prueba
PS> .\quick_test.ps1
# Compilando... ✅ APK compilado en 22 segundos
# Instalando... ✅ APK instalado
# Ejecutando... ✅ App iniciada
# [12:34:56] ✅ Grabación iniciada!
# [12:35:06] ⏹️ Grabación detenida
# [12:35:06] ✅ Video guardado: .../test_recording_20260227_123456.mp4
# [12:35:06] 📦 Tamaño: 4823 KB

# Hacer un cambio (ej: cambiar bitrate)
PS> code app\src\main\java\com\cardwarskingdom\recordertest\ScreenRecorderEngine.java
# (Editar: VIDEO_BITRATE = 6_000_000)

# Probar cambio
PS> .\quick_test.ps1
# Compilando... ✅ APK compilado en 18 segundos  (más rápido, caché)
# ... (resultado con bitrate mayor)
```

## 🔍 Troubleshooting

### ❌ "ADB no encontrado"

```powershell
# Instalar Android SDK Platform Tools
# Agregar a PATH: C:\Users\<TU_USUARIO>\AppData\Local\Android\Sdk\platform-tools
```

### ❌ "No hay dispositivos conectados"

1. Habilita **USB Debugging** en tu dispositivo
2. Conecta por USB
3. Acepta el diálogo de autorización
4. Verifica: `adb devices`

### ❌ "Compilación fallida - SDK not found"

```powershell
# Instalar Android SDK a través de Android Studio, o configurar ANDROID_HOME:
$env:ANDROID_HOME = "C:\Users\<TU_USUARIO>\AppData\Local\Android\Sdk"
```

### ❌ Gradle muy lento

Primera compilación descarga dependencias y puede tardar 1-2 minutos. Las siguientes serán mucho más rápidas gracias al caché.

### ⚠️ "MediaCodec error" en el dispositivo

Algunos dispositivos no soportan ciertos parámetros. Prueba:

```java
// Reducir resolución
VIDEO_WIDTH = 1280;
VIDEO_HEIGHT = 720;

// Reducir bitrate
VIDEO_BITRATE = 2_000_000;
```

## 🎓 Cómo Funciona

### Flujo de Grabación

```
Usuario presiona "Iniciar"
    ↓
MainActivity solicita permiso de MediaProjection
    ↓
Sistema muestra diálogo de permiso
    ↓
Usuario acepta
    ↓
ScreenRecorderEngine inicia:
    ├─ MediaProjection → captura pantalla
    ├─ MediaCodec → codifica H.264 por hardware
    ├─ VirtualDisplay → renderiza captura a Surface
    └─ MediaMuxer → escribe MP4 directamente
    ↓
Grabación en tiempo real (0 uso de disco/RAM temporal)
    ↓
Usuario presiona "Detener" (o auto-detiene a los 10 seg)
    ↓
MediaCodec drena buffers → MediaMuxer cierra archivo
    ↓
Video MP4 listo inmediatamente
```

### Ventajas de este Enfoque

- ✅ **Hardware encoding**: Usa chip de video del dispositivo
- ✅ **Cero buffers intermedios**: Escribe directamente a MP4
- ✅ **Bajo consumo RAM**: No guarda frames en memoria
- ✅ **Bajo consumo CPU**: El hardware hace todo
- ✅ **Sin post-procesamiento**: Video listo al instante

## 🔄 Integración a Unity

Una vez que esta mini-app funcione perfectamente:

### 1. Copiar el código probado

```
ScreenRecorderEngine.java 
    → Assets/Plugins/Android/src/.../ScreenRecorderEngine.java
```

### 2. Crear wrapper C#

```csharp
// MobileVideoRecorder.cs
AndroidJavaObject recorder = new AndroidJavaObject("com.cardwarskingdom.videoencoder.ScreenRecorderEngine");
recorder.Call("start");
```

### 3. Compilar Unity APK

El código ya está probado y funcional, solo necesitas integrarlo.

## 📝 Personalización Avanzada

### Agregar Audio

En `ScreenRecorderEngine.java`, agregar MediaCodec de audio:

```java
// Configurar audio encoder
MediaFormat audioFormat = MediaFormat.createAudioFormat(
    MediaFormat.MIMETYPE_AUDIO_AAC, 44100, 2);
// ... (configurar MediaCodec de audio)
```

### Segmentación de Video

Para crear archivos de 5-10 minutos:

```java
private static final long MAX_DURATION_MS = 10 * 60 * 1000; // 10 min

if (System.currentTimeMillis() - startTimeMs > MAX_DURATION_MS) {
    // Cerrar muxer actual
    // Crear nuevo muxer con timestamp diferente
}
```

### Overlay de UI

Para grabar UI encima del juego, modificar `VirtualDisplay` flags.

## 📊 Benchmarks Esperados

En un Nubia Z70 Ultra (Android 15, 8GB RAM):

| Métrica | Valor Esperado |
|---------|----------------|
| **Tiempo de compilación** | 15-30 seg (primera: 60-90 seg) |
| **Tiempo de instalación** | 2-3 seg |
| **Tamaño APK** | 2-3 MB |
| **RAM usado durante grabación** | ~150-200 MB |
| **CPU usado** | 5-15% (hardware codec) |
| **Tamaño video 10 seg @ 720p/4Mbps** | ~5 MB |

## 💡 Tips

- **Usa logs abundantemente**: Agrega `Log.d(TAG, "...")` donde sea necesario
- **Prueba parámetros diferentes**: Bitrate, resolución, FPS
- **Verifica en varios dispositivos**: Lo que funciona en uno puede fallar en otro
- **Monitorea temperatura**: Grabación intensiva puede calentar el dispositivo

## ❓ FAQ

**P: ¿Por qué crear esto en lugar de probar directo en Unity?**  
R: Compilar Unity tarda 5-10 minutos vs 20 segundos de esta app. Para desarrollo de plugins nativos, iterar rápido es crucial.

**P: ¿Funcionará igual en Unity?**  
R: Sí, el código Java es idéntico. Unity solo agrega la capa C# de comunicación.

**P: ¿Puedo agregar más features?**  
R: ¡Absolutamente! Esta es una base mínima para que experimentes.

---

**🎉 ¡Listo! Ahora puedes iterar rápidamente en tu sistema de grabación.**

```powershell
.\quick_test.ps1
```
