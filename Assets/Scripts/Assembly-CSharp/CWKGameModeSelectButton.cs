using UnityEngine;

public class CWKGameModeSelectButton : MonoBehaviour
{
    [SerializeField] private string modeId = CWKGameModeManager.Mode001;

    // Assign this method to the NGUI UIButton OnClick event when using the Inspector modeId.
    public void SelectGame()
    {
        SelectMode(modeId);
    }

    // These no-argument methods can be assigned directly to NGUI UIButton OnClick events.
    public void SelectCWK001()
    {
        SelectMode(CWKGameModeManager.Mode001);
    }

    public void SelectCWK002()
    {
        SelectMode(CWKGameModeManager.Mode002);
    }

    private void SelectMode(string id)
    {
        CWKGameModeManager manager = GetManager();
        if (manager == null)
        {
            Debug.LogError("[CWKGameModeSelectButton] Could not create or find CWKGameModeManager.");
            return;
        }

        Debug.Log("[CWKGameModeSelectButton] NGUI click received. Requested mode: " + id);
        manager.SelectModeAndLoadConnect(id);
    }

    private CWKGameModeManager GetManager()
    {
        if (CWKGameModeManager.Instance != null)
            return CWKGameModeManager.Instance;

        // Defensive fallback in case the runtime initializer has not created the singleton.
        CWKGameModeManager existing = FindObjectOfType<CWKGameModeManager>();
        if (existing != null)
            return existing;

        GameObject managerObject = new GameObject("CWKGameModeManager");
        return managerObject.AddComponent<CWKGameModeManager>();
    }
}
