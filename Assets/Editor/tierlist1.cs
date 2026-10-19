using UnityEngine;
using UnityEditor;

public class MuseumAutoExporterMenu
{
    [MenuItem("Tools/Museum/Run Creature Frame Export")]
    public static void ExportCreatureFrames()
    {
        if (!Application.isPlaying)
        {
            Debug.LogError("Must be in Play Mode! Enter the museum scene and try again.");
            return;
        }
        
        // Use the static method from the exporter
        MuseumAutoExporter.StartExport();
        
        Debug.Log("Creature frame export started! Check Console for progress...");
    }
}