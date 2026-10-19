// File: Assets/Editor/PreparePVPUI_Full.cs
// Run from Unity menu: Tools/Prepare PVP Prep UI/Apply Full Fixes
// Creates BanWarningAnchor/Node/Label, ensures PlayBlockedLabel, normalizes InventoryTile clones,
// assigns Button_StartBattle and Label_Play on PVPPrepScreenController (if present), and offers aspect presets.

using UnityEditor;
using UnityEngine;
using System.Linq;

public static class PreparePVPUI_Full
{
    private const string ROOT_NAME = "UI_FrontEnd_PVP_Vs";
    private const string PANEL_NAME = "Panel_PVP_Vs";
    private const string ANCHOR_NAME = "BanWarningAnchor";
    private const string NODE_NAME = "BanWarningNode";
    private const string LABEL_NAME = "BanWarningLabel";
    private const string PLAY_LABEL_NAME = "Label_Play";
    private const string PLAY_BLOCKED_NAME = "PlayBlockedLabel";

    [MenuItem("Tools/Prepare PVP Prep UI/Apply Full Fixes")]
    public static void ApplyFullFixesMenu()
    {
        GameObject root = GameObject.Find(ROOT_NAME);
        if (root == null)
        {
            Debug.LogError($"[PreparePVPUI_Full] Could not find GameObject named '{ROOT_NAME}' in the open scene.");
            return;
        }
        ApplyAllFixes(root);
    }

    [MenuItem("Tools/Prepare PVP Prep UI/Set Aspect Preset/16_9")]
    public static void SetPreset_16_9() => SetAnchorPreset(16f / 9f);
    [MenuItem("Tools/Prepare PVP Prep UI/Set Aspect Preset/9_16")]
    public static void SetPreset_9_16() => SetAnchorPreset(9f / 16f);
    [MenuItem("Tools/Prepare PVP Prep UI/Set Aspect Preset/4_3")]
    public static void SetPreset_4_3() => SetAnchorPreset(4f / 3f);

    private static void SetAnchorPreset(float aspect)
    {
        GameObject root = GameObject.Find(ROOT_NAME);
        if (root == null) { Debug.LogWarning("[PreparePVPUI_Full] Root not found."); return; }

        Transform anchor = FindOrCreateAnchor(root.transform);
        if (anchor == null) { Debug.LogWarning("[PreparePVPUI_Full] Anchor missing."); return; }

        if (Mathf.Approximately(aspect, 16f / 9f)) anchor.localPosition = new Vector3(0f, 40f, 0f);
        else if (Mathf.Approximately(aspect, 9f / 16f)) anchor.localPosition = new Vector3(0f, 80f, 0f);
        else anchor.localPosition = new Vector3(0f, 20f, 0f);

        EditorUtility.SetDirty(anchor.gameObject);
        Debug.Log("[PreparePVPUI_Full] Anchor Y set for aspect " + aspect + " -> " + anchor.localPosition);
    }

    private static void ApplyAllFixes(GameObject root)
    {
        Undo.RegisterFullObjectHierarchyUndo(root, "PreparePVPUI_Full ApplyAllFixes");

        // Ensure root active (script expects active root for prefab editing); user can re-hide panel_node later.
        root.SetActive(true);

        // Ensure Panel_PVP_Vs exists and leave it inactive
        Transform panel = root.transform.Find(PANEL_NAME);
        if (panel != null)
        {
            if (panel.gameObject.activeSelf) { Undo.RecordObject(panel.gameObject, "Deactivate Panel_PVP_Vs"); panel.gameObject.SetActive(false); }
            Debug.Log("[PreparePVPUI_Full] Ensured Panel_PVP_Vs exists and is inactive.");
        }
        else
        {
            Debug.LogWarning("[PreparePVPUI_Full] Panel_PVP_Vs not found under root; please create it if required by your layout.");
        }

        // 1) Anchor creation
        Transform anchor = FindOrCreateAnchor(root.transform);
        anchor.localRotation = Quaternion.identity;
        anchor.localScale = Vector3.one;

        // baseline 16:9 coordinates
        anchor.localPosition = new Vector3(0f, 40f, 0f);
        EditorUtility.SetDirty(anchor.gameObject);
        Debug.Log("[PreparePVPUI_Full] BanWarningAnchor created/validated.");

        // 2) BanWarningNode
        Transform banNode = FindChildByName(root.transform, NODE_NAME);
        if (banNode == null)
        {
            GameObject bn = new GameObject(NODE_NAME);
            bn.layer = root.layer;
            bn.transform.SetParent(anchor, false);
            bn.transform.localPosition = Vector3.zero;
            bn.transform.localRotation = Quaternion.identity;
            bn.transform.localScale = Vector3.one;
            banNode = bn.transform;
            EditorUtility.SetDirty(bn);
            Debug.Log("[PreparePVPUI_Full] Created BanWarningNode under anchor.");
        }
        else
        {
            // reparent if not under anchor
            if (banNode.parent != anchor)
            {
                Undo.SetTransformParent(banNode, anchor, "Reparent BanWarningNode");
                banNode.localPosition = Vector3.zero;
            }
            banNode.localRotation = Quaternion.identity;
            banNode.localScale = Vector3.one;
            EditorUtility.SetDirty(banNode.gameObject);
            Debug.Log("[PreparePVPUI_Full] Found existing BanWarningNode and reparented to anchor.");
        }

        // 3) BanWarningLabel
        Transform banLabel = banNode.Find(LABEL_NAME);
        if (banLabel == null)
        {
            GameObject lbl = new GameObject(LABEL_NAME);
            lbl.layer = root.layer;
            lbl.transform.SetParent(banNode, false);
            lbl.transform.localPosition = Vector3.zero;
            lbl.transform.localRotation = Quaternion.identity;
            lbl.transform.localScale = Vector3.one;
            var uil = lbl.AddComponent<UILabel>();
            // try copy font from Label_Play if present
            Transform playLabel = FindChildByName(root.transform, PLAY_LABEL_NAME);
            var playLblComp = playLabel != null ? playLabel.GetComponent<UILabel>() : null;
            if (playLblComp != null) { uil.bitmapFont = playLblComp.bitmapFont; uil.fontSize = Mathf.Max(18, playLblComp.fontSize - 0); }
            uil.text = string.Empty;
            uil.color = Color.yellow;
            uil.alignment = NGUIText.Alignment.Left;
            lbl.SetActive(false);
            banLabel = lbl.transform;
            EditorUtility.SetDirty(lbl);
            Debug.Log("[PreparePVPUI_Full] Created BanWarningLabel under BanWarningNode and deactivated it.");
        }
        else
        {
            // ensure disabled by default
            if (banLabel.gameObject.activeSelf) { Undo.RecordObject(banLabel.gameObject, "Deactivate BanWarningLabel"); banLabel.gameObject.SetActive(false); }
            EditorUtility.SetDirty(banLabel.gameObject);
            Debug.Log("[PreparePVPUI_Full] Found existing BanWarningLabel and deactivated it.");
        }

        // 4) Fix Label_Play rotation and ensure PlayBlockedLabel
        Transform playLblT = FindChildByName(root.transform, PLAY_LABEL_NAME);
        if (playLblT != null)
        {
            Vector3 rot = playLblT.localEulerAngles;
            if (!Mathf.Approximately(rot.z, 0f)) { Undo.RecordObject(playLblT, "Reset Label_Play rotation"); playLblT.localEulerAngles = new Vector3(rot.x, rot.y, 0f); }
            EditorUtility.SetDirty(playLblT.gameObject);
            EnsurePlayBlockedLabel(playLblT);
        }
        else
        {
            Debug.LogWarning("[PreparePVPUI_Full] Label_Play not found; PlayBlockedLabel cannot be auto-created without it.");
        }

        // 5) Normalize InventoryTile clones under anchor (if any)
        NormalizeInventoryTiles(anchor);

        // 6) Assign Button_StartBattle and Label_Play fields on PVPPrepScreenController (if present)
        var controller = root.GetComponentInChildren<PVPPrepScreenController>(true);
        if (controller != null)
        {
            SerializedObject so = new SerializedObject(controller);
            bool changed = false;

            // Button_StartBattle
            if (so.FindProperty("Button_StartBattle") != null)
            {
                Transform startBtn = FindChildByName(root.transform, "Button_StartBattle") ?? FindChildByName(root.transform, "GameStartButtons/Button_StartBattle");
                if (startBtn != null)
                {
                    so.FindProperty("Button_StartBattle").objectReferenceValue = startBtn.gameObject;
                    changed = true;
                    Debug.Log("[PreparePVPUI_Full] Assigned Button_StartBattle on PVPPrepScreenController.");
                }
            }

            // Label_Play
            if (so.FindProperty("Label_Play") != null)
            {
                Transform lp = FindChildByName(root.transform, PLAY_LABEL_NAME) ?? FindChildByName(root.transform, "GameStartButtons/Label_Play");
                if (lp != null)
                {
                    so.FindProperty("Label_Play").objectReferenceValue = lp.gameObject;
                    changed = true;
                    Debug.Log("[PreparePVPUI_Full] Assigned Label_Play on PVPPrepScreenController.");
                }
            }

            // BanWarningNode
            if (so.FindProperty("BanWarningNode") != null)
            {
                so.FindProperty("BanWarningNode").objectReferenceValue = banNode.gameObject;
                changed = true;
                Debug.Log("[PreparePVPUI_Full] Assigned BanWarningNode on PVPPrepScreenController.");
            }

            // BanWarningLabel
            if (so.FindProperty("BanWarningLabel") != null)
            {
                var lblComp = banNode.GetComponentInChildren<UILabel>(true);
                if (lblComp != null)
                {
                    so.FindProperty("BanWarningLabel").objectReferenceValue = lblComp;
                    changed = true;
                    Debug.Log("[PreparePVPUI_Full] Assigned BanWarningLabel on PVPPrepScreenController.");
                }
            }

            if (changed) { so.ApplyModifiedProperties(); EditorUtility.SetDirty(controller); }
        }
        else
        {
            Debug.LogWarning("[PreparePVPUI_Full] PVPPrepScreenController not found under root. The script assignments were skipped.");
        }

        EditorUtility.SetDirty(root);
        Debug.Log("[PreparePVPUI_Full] ApplyAllFixes complete. Review scene and apply prefab overrides as needed.");
    }

    private static Transform FindOrCreateAnchor(Transform root)
    {
        Transform t = FindChildByName(root, ANCHOR_NAME);
        if (t != null) return t;

        GameObject go = new GameObject(ANCHOR_NAME);
        go.layer = root.gameObject.layer;
        go.transform.SetParent(root, false);
        go.transform.localPosition = new Vector3(0f, 40f, 0f);
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;
        go.SetActive(true);
        Undo.RegisterCreatedObjectUndo(go, "Create BanWarningAnchor");
        return go.transform;
    }

    private static void EnsurePlayBlockedLabel(Transform playLabel)
    {
        Transform parent = playLabel.parent != null ? playLabel.parent : playLabel;
        Transform existing = parent.Find(PLAY_BLOCKED_NAME);
        if (existing != null)
        {
            int playIdx = playLabel.GetSiblingIndex();
            existing.SetSiblingIndex(playIdx + 1);
            existing.localPosition = playLabel.localPosition + new Vector3(4.5f, 0f, 0f);
            existing.localEulerAngles = Vector3.zero;
            existing.localScale = Vector3.one;
            UILabel lbl = existing.GetComponent<UILabel>();
            UILabel playLbl = playLabel.GetComponent<UILabel>();
            if (lbl != null && playLbl != null) lbl.depth = playLbl.depth + 1;
            existing.gameObject.SetActive(false);
            EditorUtility.SetDirty(existing.gameObject);
            Debug.Log("[PreparePVPUI_Full] Adjusted existing PlayBlockedLabel (inactive by default).");
            return;
        }

        GameObject blk = new GameObject(PLAY_BLOCKED_NAME);
        blk.layer = playLabel.gameObject.layer;
        blk.transform.SetParent(parent, false);
        blk.transform.localPosition = playLabel.localPosition + new Vector3(4.5f, 0f, 0f);
        blk.transform.localRotation = Quaternion.identity;
        blk.transform.localScale = Vector3.one;

        UILabel newLbl = blk.AddComponent<UILabel>();
        UILabel playLblComp = playLabel.GetComponent<UILabel>();
        if (playLblComp != null)
        {
            newLbl.bitmapFont = playLblComp.bitmapFont;
            newLbl.fontSize = Mathf.Max(16, playLblComp.fontSize - 2);
            newLbl.depth = playLblComp.depth + 1;
        }
        newLbl.color = Color.red;
        newLbl.text = "BLOCKED";
        newLbl.alignment = NGUIText.Alignment.Left;

        int playIndex = playLabel.GetSiblingIndex();
        blk.transform.SetSiblingIndex(playIndex + 1);

        blk.SetActive(false);

        Undo.RegisterCreatedObjectUndo(blk, "Create PlayBlockedLabel");
        EditorUtility.SetDirty(blk);
        Debug.Log("[PreparePVPUI_Full] Created PlayBlockedLabel as sibling of " + playLabel.name + " (inactive by default)");
    }

    private static void NormalizeInventoryTiles(Transform anchor)
    {
        if (anchor == null) return;
        var found = anchor.GetComponentsInChildren<Transform>(true).Where(t => t.name.Contains("InventoryTile")).ToArray();
        foreach (var t in found)
        {
            Undo.RecordObject(t, "Normalize InventoryTile");
            t.localPosition = Vector3.zero;
            t.localRotation = Quaternion.identity;
            t.localScale = Vector3.one;
            Vector3 e = t.localEulerAngles;
            if (!Mathf.Approximately(e.z, 0f)) t.localEulerAngles = new Vector3(e.x, e.y, 0f);
            EditorUtility.SetDirty(t.gameObject);
            Debug.Log("[PreparePVPUI_Full] Normalized inventory tile: " + t.name);
        }
    }

    private static Transform FindChildByName(Transform parent, string name)
    {
        if (parent == null) return null;
        Transform direct = parent.Find(name);
        if (direct != null) return direct;
        var all = parent.GetComponentsInChildren<Transform>(true);
        foreach (var t in all) if (t.name == name) return t;
        return null;
    }
}
