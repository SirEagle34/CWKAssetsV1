using UnityEngine;

/// <summary>
/// Independent entry point for clone transformation cinematics.
/// Reuses the established cinematic implementation without modifying ShapeShift code.
/// </summary>
public sealed class CloneCinematic : ShapeShiftCinematic
{
    private static CloneCinematic sCloneInstance;

    public static CloneCinematic CloneInstance
    {
        get
        {
            if (sCloneInstance == null)
            {
                GameObject obj = new GameObject("CloneCinematic");
                sCloneInstance = obj.AddComponent<CloneCinematic>();
            }
            return sCloneInstance;
        }
    }

    private void OnDestroy()
    {
        if (sCloneInstance == this)
        {
            sCloneInstance = null;
        }
    }
}
