using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Herramienta para comprimir audio automáticamente para Android
/// Reduce significativamente el tamaño del APK
/// </summary>
public class AudioCompressionHelper : EditorWindow
{
    private Vector2 scrollPosition;
    private List<AudioInfo> audioClips = new List<AudioInfo>();
    private string folderFilter = "Assets/Resources/audio";
    
    // Presets de compresión
    private CompressionPreset selectedPreset = CompressionPreset.Music;

    private enum CompressionPreset
    {
        Music,      // Música de fondo (alta calidad, streaming)
        Voice,      // Voces (calidad media, streaming)
        SFX         // Efectos de sonido (baja latencia, compressed in memory)
    }

    private class AudioInfo
    {
        public string path;
        public AudioClip clip;
        public long sizeBytes;
        public bool hasAndroidOverride;
        public AudioCompressionFormat currentFormat;
        public AudioClipLoadType currentLoadType;
        public float currentQuality;
        public bool selected = true;
    }

    [MenuItem("Tools/Android/Audio Compression Helper")]
    public static void ShowWindow()
    {
        var window = GetWindow<AudioCompressionHelper>("Audio Compression");
        window.minSize = new Vector2(800, 600);
    }

    void OnGUI()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Audio Compression Helper for Android", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Esta herramienta te ayuda a comprimir audio para reducir el tamaño del APK.\n" +
            "Elige un preset según el tipo de audio y aplica la compresión.",
            MessageType.Info
        );
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();

        // Configuración
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Configuración", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        folderFilter = EditorGUILayout.TextField("Carpeta a escanear:", folderFilter);
        if (GUILayout.Button("...", GUILayout.Width(30)))
        {
            string path = EditorUtility.OpenFolderPanel("Seleccionar carpeta", "Assets/Resources", "");
            if (!string.IsNullOrEmpty(path))
            {
                folderFilter = "Assets" + path.Substring(Application.dataPath.Length);
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Preset de Compresión:", EditorStyles.boldLabel);

        selectedPreset = (CompressionPreset)EditorGUILayout.EnumPopup("Tipo de audio:", selectedPreset);

        // Mostrar detalles del preset
        ShowPresetDetails();

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();

        // Botones de acción
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("🔍 Escanear Audio", GUILayout.Height(30)))
        {
            ScanAudio();
        }

        GUI.enabled = audioClips.Count > 0;
        if (GUILayout.Button("✅ Aplicar Compresión a Seleccionados", GUILayout.Height(30)))
        {
            ApplyCompression();
        }
        GUI.enabled = true;

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // Lista de audio clips
        if (audioClips.Count > 0)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField($"Audio clips encontrados: {audioClips.Count}", EditorStyles.boldLabel);

            // Estadísticas
            long totalSize = audioClips.Sum(a => a.sizeBytes);
            int toCompress = audioClips.Count(a => a.selected);
            EditorGUILayout.LabelField($"Tamaño total: {FormatBytes(totalSize)}");
            EditorGUILayout.LabelField($"Seleccionados: {toCompress}");

            EditorGUILayout.Space();

            // Botones de selección
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Seleccionar todos"))
            {
                audioClips.ForEach(a => a.selected = true);
            }
            if (GUILayout.Button("Deseleccionar todos"))
            {
                audioClips.ForEach(a => a.selected = false);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // Scroll view con lista de clips
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(300));

            foreach (var audioInfo in audioClips)
            {
                EditorGUILayout.BeginHorizontal("box");

                audioInfo.selected = EditorGUILayout.Toggle(audioInfo.selected, GUILayout.Width(20));

                EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField(System.IO.Path.GetFileName(audioInfo.path), EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"Path: {audioInfo.path}", EditorStyles.miniLabel);
                
                string status = audioInfo.hasAndroidOverride 
                    ? $"Android: {audioInfo.currentFormat}, Load: {audioInfo.currentLoadType}, Quality: {audioInfo.currentQuality:F0}%" 
                    : "Sin override de Android";
                
                GUIStyle statusStyle = new GUIStyle(EditorStyles.miniLabel);
                statusStyle.normal.textColor = audioInfo.hasAndroidOverride ? Color.green : Color.yellow;
                EditorGUILayout.LabelField(status, statusStyle);
                
                EditorGUILayout.EndVertical();

                EditorGUILayout.LabelField(FormatBytes(audioInfo.sizeBytes), GUILayout.Width(80));

                if (GUILayout.Button("Ping", GUILayout.Width(50)))
                {
                    EditorGUIUtility.PingObject(audioInfo.clip);
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }
        else
        {
            EditorGUILayout.HelpBox("Haz clic en 'Escanear Audio' para buscar archivos de audio en la carpeta especificada.", MessageType.Info);
        }
    }

    void ShowPresetDetails()
    {
        EditorGUILayout.BeginVertical("helpbox");
        
        switch (selectedPreset)
        {
            case CompressionPreset.Music:
                EditorGUILayout.LabelField("🎵 MÚSICA DE FONDO", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("• Formato: Vorbis");
                EditorGUILayout.LabelField("• Load Type: Streaming (ahorra RAM)");
                EditorGUILayout.LabelField("• Quality: 60%");
                EditorGUILayout.LabelField("• Ahorro estimado: 70-80%");
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Ideal para: bgm/, music/");
                break;

            case CompressionPreset.Voice:
                EditorGUILayout.LabelField("🎙️ VOCES / DIÁLOGOS", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("• Formato: Vorbis");
                EditorGUILayout.LabelField("• Load Type: Streaming");
                EditorGUILayout.LabelField("• Quality: 70%");
                EditorGUILayout.LabelField("• Ahorro estimado: 60-70%");
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Ideal para: vo/, voices/, dialogue/");
                break;

            case CompressionPreset.SFX:
                EditorGUILayout.LabelField("🔊 EFECTOS DE SONIDO", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("• Formato: ADPCM");
                EditorGUILayout.LabelField("• Load Type: Compressed in Memory (baja latencia)");
                EditorGUILayout.LabelField("• Quality: 70%");
                EditorGUILayout.LabelField("• Ahorro estimado: 50-60%");
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Ideal para: audio/, sfx/, sounds/");
                break;
        }

        EditorGUILayout.EndVertical();
    }

    void ScanAudio()
    {
        audioClips.Clear();

        string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { folderFilter });
        
        Debug.Log($"[AudioCompressionHelper] Escaneando {guids.Length} clips de audio en {folderFilter}...");

        int count = 0;
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            AudioImporter importer = AssetImporter.GetAtPath(path) as AudioImporter;
            
            if (importer != null)
            {
                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip != null)
                {
                    var androidSettings = importer.GetOverrideSampleSettings("Android");
                    var defaultSettings = importer.defaultSampleSettings;
                    bool hasOverride = (androidSettings.compressionFormat != defaultSettings.compressionFormat) || 
                                      (androidSettings.loadType != defaultSettings.loadType);
                    
                    var audioInfo = new AudioInfo
                    {
                        path = path,
                        clip = clip,
                        sizeBytes = GetAudioSizeBytes(path),
                        hasAndroidOverride = hasOverride,
                        currentFormat = androidSettings.compressionFormat,
                        currentLoadType = androidSettings.loadType,
                        currentQuality = androidSettings.quality
                    };

                    audioClips.Add(audioInfo);
                    count++;
                }
            }

            if (count % 50 == 0)
            {
                EditorUtility.DisplayProgressBar("Escaneando audio", $"Procesando {count}/{guids.Length}...", (float)count / guids.Length);
            }
        }

        EditorUtility.ClearProgressBar();

        Debug.Log($"[AudioCompressionHelper] Escaneo completado. Encontrados {audioClips.Count} clips.");
        Repaint();
    }

    void ApplyCompression()
    {
        var selectedClips = audioClips.Where(a => a.selected).ToList();
        
        if (selectedClips.Count == 0)
        {
            EditorUtility.DisplayDialog("Error", "No hay clips de audio seleccionados", "OK");
            return;
        }

        // Obtener configuración del preset
        AudioCompressionFormat format;
        AudioClipLoadType loadType;
        float quality;

        GetPresetSettings(out format, out loadType, out quality);

        bool confirm = EditorUtility.DisplayDialog(
            "Confirmar compresión",
            $"¿Aplicar compresión a {selectedClips.Count} clips de audio?\n\n" +
            $"Preset: {selectedPreset}\n" +
            $"Formato: {format}\n" +
            $"Load Type: {loadType}\n" +
            $"Quality: {quality}%\n\n" +
            "Esta operación puede tardar varios minutos.",
            "Aplicar",
            "Cancelar"
        );

        if (!confirm)
            return;

        int processed = 0;

        try
        {
            AssetDatabase.StartAssetEditing();

            foreach (var audioInfo in selectedClips)
            {
                AudioImporter importer = AssetImporter.GetAtPath(audioInfo.path) as AudioImporter;
                if (importer != null)
                {
                    // Configurar override para Android
                    var androidSettings = new AudioImporterSampleSettings
                    {
                        compressionFormat = format,
                        loadType = loadType,
                        quality = quality,
                        sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate
                    };
                    
                    importer.SetOverrideSampleSettings("Android", androidSettings);
                    importer.SaveAndReimport();

                    processed++;

                    if (processed % 10 == 0)
                    {
                        EditorUtility.DisplayProgressBar(
                            "Aplicando compresión",
                            $"Procesando {processed}/{selectedClips.Count}...",
                            (float)processed / selectedClips.Count
                        );
                    }
                }
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            EditorUtility.ClearProgressBar();
        }

        AssetDatabase.Refresh();

        Debug.Log($"[AudioCompressionHelper] Compresión completada. {processed} clips procesados.");
        
        EditorUtility.DisplayDialog(
            "Compresión completada",
            $"Se comprimieron {processed} clips de audio correctamente.\n\n" +
            "Los nuevos settings se aplicarán en el próximo build de Android.",
            "OK"
        );

        ScanAudio(); // Re-escanear
    }

    void GetPresetSettings(out AudioCompressionFormat format, out AudioClipLoadType loadType, out float quality)
    {
        switch (selectedPreset)
        {
            case CompressionPreset.Music:
                format = AudioCompressionFormat.Vorbis;
                loadType = AudioClipLoadType.Streaming;
                quality = 0.6f;  // 60%
                break;

            case CompressionPreset.Voice:
                format = AudioCompressionFormat.Vorbis;
                loadType = AudioClipLoadType.Streaming;
                quality = 0.7f;  // 70%
                break;

            case CompressionPreset.SFX:
                format = AudioCompressionFormat.ADPCM;
                loadType = AudioClipLoadType.CompressedInMemory;
                quality = 0.7f;  // 70%
                break;

            default:
                format = AudioCompressionFormat.Vorbis;
                loadType = AudioClipLoadType.CompressedInMemory;
                quality = 0.7f;
                break;
        }
    }

    long GetAudioSizeBytes(string path)
    {
        try
        {
            var fileInfo = new System.IO.FileInfo(path);
            return fileInfo.Exists ? fileInfo.Length : 0;
        }
        catch
        {
            return 0;
        }
    }

    string FormatBytes(long bytes)
    {
        if (bytes < 1024)
            return $"{bytes} B";
        else if (bytes < 1024 * 1024)
            return $"{bytes / 1024.0:F2} KB";
        else
            return $"{bytes / (1024.0 * 1024.0):F2} MB";
    }
}
