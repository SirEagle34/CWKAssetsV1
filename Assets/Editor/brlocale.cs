using UnityEditor;
using System.Globalization;
using System.Threading;

[InitializeOnLoad]
public static class ForceEditorCulture
{
    static ForceEditorCulture()
    {
        CultureInfo culture = new CultureInfo("pt-BR");
        Thread.CurrentThread.CurrentCulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;

        UnityEngine.Debug.Log("Editor culture forced to: " + CultureInfo.CurrentCulture);
    }
}
