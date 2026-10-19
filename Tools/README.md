# Carpeta Tools

Esta carpeta contiene herramientas que se usan **solo en el Editor de Unity**.

**NO se incluyen en los builds de Android.**

## Contenido

- **ffmpeg.exe** (211 MB) - Usado para combinar frames + audio en el Editor de Windows
  - Solo para Unity Editor
  - NO se incluye en APK Android
  - Android usa MediaRecorder nativo en su lugar

## ¿Por qué esta carpeta?

Anteriormente `ffmpeg.exe` estaba en `Assets/StreamingAssets/`, lo que causaba:
- ❌ Se incluía en el APK de Android (+211 MB innecesarios)
- ❌ APK de 1.2 GB en lugar de ~900 MB

Ahora está en `Tools/` (fuera de Assets):
- ✅ Disponible para Unity Editor
- ✅ NO se incluye en APK Android
- ✅ Ahorro de ~211 MB en el APK

## En Android

Android no usa FFmpeg. En su lugar usa:
- **Primera opción**: MediaRecorder nativo (grabación de pantalla como en el sistema)
- **Fallback**: MediaCodec (procesa frames JPEG)

Ambos son nativos y no requieren FFmpeg.
