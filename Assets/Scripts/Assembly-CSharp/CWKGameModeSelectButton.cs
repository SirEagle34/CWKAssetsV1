using UnityEngine;

public class CWKGameModeSelectButton : MonoBehaviour
{
    [SerializeField] private string modeId = "cwk_0001";

    public void SelectGame()
    {
        if (CWKGameModeManager.Instance == null)
        {
            Debug.LogError("[CWKGameModeSelectButton] Add CWKGameModeManager to the ChooseGame scene.");
            return;
        }

        CWKGameModeManager.Instance.SelectModeAndLoadConnect(modeId);
    }

    public void SelectCWK001() { SelectMode("cwk_0001"); }
    public void SelectCWK002() { SelectMode("cwk_0002"); }

    private void SelectMode(string id)
    {
        if (CWKGameModeManager.Instance == null)
        {
            Debug.LogError("[CWKGameModeSelectButton] Add CWKGameModeManager to the ChooseGame scene.");
            return;
        }
        CWKGameModeManager.Instance.SelectModeAndLoadConnect(id);
    }
}
