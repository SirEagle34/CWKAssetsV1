using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Herramienta para comprimir texturas automáticamente para Android
/// Ayuda a reducir el tamaño del APK significativamente
/// </summary>
public class TextureCompressionHelper : EditorWindow
{
    private Vector2 scrollPosition;
    private List<TextureInfo> textures = new List<TextureInfo>();
    private bool showOnlyUncompressed = true;
    private string folderFilter = "Assets/Resources/creatures";
    private int textureMaxSize = 1024;
    private TextureImporterFormat androidFormat = TextureImporterFormat.ASTC_6x6;

    private class TextureInfo
    {
        public string path;
        public Texture2D texture;
        public long sizeBytes;
        public bool hasAndroidOverride;
        public TextureImporterFormat currentAndroidFormat;
        public int currentMaxSize;
        public bool selected = true;
    }

    [MenuItem("Tools/Android/Texture Compression Helper")]
    public static void ShowWindow()
    {
        var window = GetWindow<TextureCompressionHelper>("Texture Compression");
        window.minSize = new Vector2(800, 600);
    }

    void OnGUI()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Texture Compression Helper for Android", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Esta herramienta te ayuda a comprimir texturas para reducir el tamaño del APK.\n" +
            "Selecciona una carpeta, escanea las texturas y aplica compresión para Android.",
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

        showOnlyUncompressed = EditorGUILayout.Toggle("Solo mostrar sin comprimir", showOnlyUncompressed);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Configuración de Compresión para Android:", EditorStyles.boldLabel);

        androidFormat = (TextureImporterFormat)EditorGUILayout.EnumPopup("Formato:", androidFormat);
        EditorGUILayout.HelpBox(
            "ASTC_6x6: Mejor calidad, más compresión (recomendado para móviles modernos)\n" +
            "ETC2_RGBA8: Buena compatibilidad con dispositivos antiguos\n" +
            "ASTC_4x4: Mejor calidad pero menos compresión",
            MessageType.Info
        );

        textureMaxSize = EditorGUILayout.IntPopup("Tamaño máximo:", textureMaxSize, 
            new string[] { "256", "512", "1024", "2048", "4096" },
            new int[] { 256, 512, 1024, 2048, 4096 }
        );

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();

        // Botones de acción
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("🔍 Escanear Texturas", GUILayout.Height(30)))
        {
            ScanTextures();
        }

        GUI.enabled = textures.Count > 0;
        if (GUILayout.Button("✅ Aplicar Compresión a Seleccionadas", GUILayout.Height(30)))
        {
            ApplyCompression();
        }
        GUI.enabled = true;

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // Lista de texturas
        if (textures.Count > 0)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField($"Texturas encontradas: {textures.Count}", EditorStyles.boldLabel);

            // Estadísticas
            long totalSize = textures.Sum(t => t.sizeBytes);
            int toCompress = textures.Count(t => t.selected);
            EditorGUILayout.LabelField($"Tamaño total: {FormatBytes(totalSize)}");
            EditorGUILayout.LabelField($"Seleccionadas: {toCompress}");

            EditorGUILayout.Space();

            // Botones de selección
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Seleccionar todas"))
            {
                textures.ForEach(t => t.selected = true);
            }
            if (GUILayout.Button("Deseleccionar todas"))
            {
                textures.ForEach(t => t.selected = false);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // Scroll view con lista de texturas
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(300));

            foreach (var texInfo in textures)
            {
                EditorGUILayout.BeginHorizontal("box");

                texInfo.selected = EditorGUILayout.Toggle(texInfo.selected, GUILayout.Width(20));

                EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField(System.IO.Path.GetFileName(texInfo.path), EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"Path: {texInfo.path}", EditorStyles.miniLabel);
                
                string status = texInfo.hasAndroidOverride 
                    ? $"Android: {texInfo.currentAndroidFormat}, Max: {texInfo.currentMaxSize}" 
                    : "Sin override de Android";
                
                GUIStyle statusStyle = new GUIStyle(EditorStyles.miniLabel);
                statusStyle.normal.textColor = texInfo.hasAndroidOverride ? Color.green : Color.yellow;
                EditorGUILayout.LabelField(status, statusStyle);
                
                EditorGUILayout.EndVertical();

                EditorGUILayout.LabelField(FormatBytes(texInfo.sizeBytes), GUILayout.Width(80));

                if (GUILayout.Button("Ping", GUILayout.Width(50)))
                {
                    EditorGUIUtility.PingObject(texInfo.texture);
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }
        else
        {
            EditorGUILayout.HelpBox("Haz clic en 'Escanear Texturas' para buscar texturas en la carpeta especificada.", MessageType.Info);
        }
    }

    void ScanTextures()
    {
        textures.Clear();

        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folderFilter });
        
        Debug.Log($"[TextureCompressionHelper] Escaneando {guids.Length} texturas en {folderFilter}...");

        int count = 0;
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            
            if (importer != null)
            {
                bool hasAndroidOverride = importer.GetPlatformTextureSettings("Android").overridden;

                // Filtrar según configuración
                if (showOnlyUncompressed && hasAndroidOverride)
                    continue;

                Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (tex != null)
                {
                    var androidSettings = importer.GetPlatformTextureSettings("Android");
                    
                    var texInfo = new TextureInfo
                    {
                        path = path,
                        texture = tex,
                        sizeBytes = GetTextureSizeBytes(path),
                        hasAndroidOverride = hasAndroidOverride,
                        currentAndroidFormat = androidSettings.format,
                        currentMaxSize = androidSettings.maxTextureSize
                    };

                    textures.Add(texInfo);
                    count++;
                }
            }

            if (count % 100 == 0)
            {
                EditorUtility.DisplayProgressBar("Escaneando texturas", $"Procesando {count}/{guids.Length}...", (float)count / guids.Length);
            }
        }

        EditorUtility.ClearProgressBar();

        Debug.Log($"[TextureCompressionHelper] Escaneo completado. Encontradas {textures.Count} texturas.");
        Repaint();
    }

    void ApplyCompression()
    {
        var selectedTextures = textures.Where(t => t.selected).ToList();
        
        if (selectedTextures.Count == 0)
        {
            EditorUtility.DisplayDialog("Error", "No hay texturas seleccionadas", "OK");
            return;
        }

        bool confirm = EditorUtility.DisplayDialog(
            "Confirmar compresión",
            $"¿Aplicar compresión a {selectedTextures.Count} texturas?\n\n" +
            $"Formato: {androidFormat}\n" +
            $"Tamaño máximo: {textureMaxSize}\n\n" +
            "Esta operación puede tardar varios minutos.",
            "Aplicar",
            "Cancelar"
        );

        if (!confirm)
            return;

        int processed = 0;
        long sizeBefore = 0;

        try
        {
            AssetDatabase.StartAssetEditing();

            foreach (var texInfo in selectedTextures)
            {
                sizeBefore += texInfo.sizeBytes;

                TextureImporter importer = AssetImporter.GetAtPath(texInfo.path) as TextureImporter;
                if (importer != null)
                {
                    // Configurar override para Android
                    var androidSettings = importer.GetPlatformTextureSettings("Android");
                    androidSettings.overridden = true;
                    androidSettings.format = androidFormat;
                    androidSettings.maxTextureSize = textureMaxSize;
                    
                    importer.SetPlatformTextureSettings(androidSettings);
                    importer.SaveAndReimport();

                    processed++;

                    if (processed % 10 == 0)
                    {
                        EditorUtility.DisplayProgressBar(
                            "Aplicando compresión",
                            $"Procesando {processed}/{selectedTextures.Count}...",
                            (float)processed / selectedTextures.Count
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

        // Re-escanear para ver nuevos tamaños
        Debug.Log($"[TextureCompressionHelper] Compresión completada. {processed} texturas procesadas.");
        
        EditorUtility.DisplayDialog(
            "Compresión completada",
            $"Se comprimieron {processed} texturas correctamente.\n\n" +
            "Nota: Los nuevos tamaños se aplicarán en el próximo build de Android.",
            "OK"
        );

        ScanTextures(); // Re-escanear
    }

    long GetTextureSizeBytes(string path)
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
