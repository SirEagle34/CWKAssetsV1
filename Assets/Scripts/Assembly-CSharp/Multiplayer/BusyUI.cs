using System;
using System.Collections;
using System.Collections.Generic;
using CodeStage.AntiCheat.ObscuredTypes;
using MiniJSON;
using UnityEngine;
    
public static class BusyUI
{
    public static void Show()
    {
        var inst = Singleton<BusyIconPanelController>.Instance;
        if (inst != null)
            inst.Show();
    }

    public static void Hide()
    {
        var inst = Singleton<BusyIconPanelController>.Instance;
        if (inst != null)
            inst.Hide();
    }
}