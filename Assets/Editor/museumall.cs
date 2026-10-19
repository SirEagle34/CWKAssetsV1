using UnityEngine;
using UnityEditor;

public static class MuseumTools {
    [MenuItem("Tools/Museum/Unlock All Creatures")]
    public static void UnlockAllCreatures() {
        var db = CreatureDataManager.Instance.GetDatabase();
        int count = 0;
        foreach (var creature in db) {
            creature.AlreadySeen = true;
            creature.AlreadyCollected = true;
            count++;
        }
        Debug.Log($"[MuseumTools] Marked {count} creatures as seen+collected");
        // Refresh museum UI if open
        if (MuseumController.Instance != null) {
            MuseumController.Instance.RefreshCurrentTab();
        }
    }
}
