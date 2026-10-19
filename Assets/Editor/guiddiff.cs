// Assets/Editor/guiddiff.cs
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using System.Collections.Generic;

public static class GuidDiffExporter
{
    private class Entry
    {
        public string path;
        public string type;
        public string oldGuid;
        public string newGuid;
        public string status; // MATCH, MISMATCH, ONLY_OLD, ONLY_NEW, TYPE_MISMATCH, MULTI_OLD, MULTI_NEW
    }

    private struct AssetInfo
    {
        public string guid;
        public string type;
    }

    private class EntryList { public List<Entry> entries; }

    [MenuItem("Tools/Assets/Export OLD→NEW GUID map")]
    public static void Export()
    {
        string newRoot = EditorUtility.OpenFolderPanel("Select NEW project folder", "", "");
        if (string.IsNullOrEmpty(newRoot)) return;

        string oldRoot = EditorUtility.OpenFolderPanel("Select OLD project folder", "", "");
        if (string.IsNullOrEmpty(oldRoot)) return;

        string oldAssets = Path.Combine(oldRoot, "Assets");
        string newAssets = Path.Combine(newRoot, "Assets");
        if (!Directory.Exists(oldAssets) || !Directory.Exists(newAssets))
        {
            EditorUtility.DisplayDialog(
                "Error",
                "Both selections must be Unity project roots (each must contain an 'Assets' folder).",
                "OK"
            );
            return;
        }

        var oldIndex = IndexProject(oldAssets);
        var newIndex = IndexProject(newAssets);

        var allPaths = new HashSet<string>(oldIndex.Keys);
        allPaths.UnionWith(newIndex.Keys);

        var rows = new List<Entry>();
        foreach (var relPath in allPaths)
        {
            var oldInfos = oldIndex.ContainsKey(relPath) ? oldIndex[relPath] : null;
            var newInfos = newIndex.ContainsKey(relPath) ? newIndex[relPath] : null;

            if (oldInfos != null && oldInfos.Count > 1)
            {
                rows.Add(new Entry
                {
                    path = relPath,
                    type = oldInfos[0].type,
                    oldGuid = string.Join("|", oldInfos.Select(i => i.guid)),
                    newGuid = newInfos != null ? string.Join("|", newInfos.Select(i => i.guid)) : "",
                    status = "MULTI_OLD"
                });
                continue;
            }
            if (newInfos != null && newInfos.Count > 1)
            {
                rows.Add(new Entry
                {
                    path = relPath,
                    type = newInfos[0].type,
                    oldGuid = oldInfos != null ? string.Join("|", oldInfos.Select(i => i.guid)) : "",
                    newGuid = string.Join("|", newInfos.Select(i => i.guid)),
                    status = "MULTI_NEW"
                });
                continue;
            }

            var hasOld = oldInfos != null && oldInfos.Count == 1;
            var hasNew = newInfos != null && newInfos.Count == 1;

            var oldInfo = hasOld ? oldInfos[0] : default;
            var newInfo = hasNew ? newInfos[0] : default;

            if (!hasOld && hasNew)
            {
                rows.Add(new Entry { path = relPath, type = newInfo.type, oldGuid = "", newGuid = newInfo.guid, status = "ONLY_NEW" });
                continue;
            }
            if (hasOld && !hasNew)
            {
                rows.Add(new Entry { path = relPath, type = oldInfo.type, oldGuid = oldInfo.guid, newGuid = "", status = "ONLY_OLD" });
                continue;
            }
            if (!hasOld && !hasNew)
            {
                // Shouldn't happen; skip
                continue;
            }

            if (!string.Equals(oldInfo.type, newInfo.type))
            {
                rows.Add(new Entry
                {
                    path = relPath,
                    type = oldInfo.type + "->" + newInfo.type,
                    oldGuid = oldInfo.guid,
                    newGuid = newInfo.guid,
                    status = "TYPE_MISMATCH"
                });
                continue;
            }

            rows.Add(new Entry
            {
                path = relPath,
                type = oldInfo.type,
                oldGuid = oldInfo.guid,
                newGuid = newInfo.guid,
                status = string.Equals(oldInfo.guid, newInfo.guid) ? "MATCH" : "MISMATCH"
            });
        }

        string outDir = "Assets/Editor/GuidDiff";
        Directory.CreateDirectory(outDir);

        // CSV
        string csvPath = Path.Combine(outDir, "guid-map.csv");
        using (var sw = new StreamWriter(csvPath, false))
        {
            sw.WriteLine("path,type,old_guid,new_guid,status");
            foreach (var r in rows.OrderBy(r => r.status).ThenBy(r => r.path))
            {
                sw.WriteLine(
                    $"{Csv(r.path)},{Csv(r.type)},{Csv(r.oldGuid)},{Csv(r.newGuid)},{Csv(r.status)}"
                );
            }
        }

        // JSON (Unity JsonUtility requires a wrapper)
        string jsonPath = Path.Combine(outDir, "guid-map.json");
        var listWrapper = new EntryList { entries = rows };
        File.WriteAllText(jsonPath, JsonUtility.ToJson(listWrapper, true));

        AssetDatabase.Refresh();

        int mismatchCount = rows.Count(r => r.status == "MISMATCH");
        Debug.Log("[GuidDiffExporter] Wrote:\n- " + csvPath + "\n- " + jsonPath + "\nMISMATCH count: " + mismatchCount);

        EditorUtility.DisplayDialog(
            "GUID Map Exported",
            "CSV: " + csvPath + "\nJSON: " + jsonPath + "\nMISMATCH count: " + mismatchCount,
            "OK"
        );
    }

    // Helpers

    private static string Csv(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        bool needsQuotes = s.Contains(",") || s.Contains("\"");
        if (needsQuotes)
        {
            s = "\"" + s.Replace("\"", "\"\"") + "\"";
        }
        return s;
    }

    private static Dictionary<string, List<AssetInfo>> IndexProject(string assetsRoot)
    {
        var dict = new Dictionary<string, List<AssetInfo>>();
        foreach (var meta in Directory.GetFiles(assetsRoot, "*.meta", SearchOption.AllDirectories))
        {
            string assetPath = meta.Substring(0, meta.Length - 5);
            if (!File.Exists(assetPath)) continue;

            string rel = NormalizeRelPath(assetsRoot, assetPath);
            string type = Path.GetExtension(assetPath).ToLowerInvariant();
            string guid = ExtractGuid(meta) ?? "";

            var info = new AssetInfo { guid = guid, type = type };
            if (!dict.TryGetValue(rel, out var list))
            {
                list = new List<AssetInfo>();
                dict[rel] = list;
            }
            list.Add(info);
        }
        return dict;
    }

    private static string NormalizeRelPath(string root, string full)
    {
        string rel = full.Substring(root.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return rel.Replace('\\', '/');
    }

    private static string ExtractGuid(string metaPath)
    {
        foreach (var line in File.ReadAllLines(metaPath))
        {
            if (line.StartsWith("guid: "))
                return line.Substring(6).Trim();
        }
        return null;
    }
}
