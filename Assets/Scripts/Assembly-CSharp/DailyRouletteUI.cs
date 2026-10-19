using UnityEngine;

public class DailyRouletteUI : MonoBehaviour
{
    [SerializeField]
    private GameObject resetPopup;

    [SerializeField]
    private DailyGiftRouletteContoller rouletteController;

    public void OnResetButtonPressed()
    {
        resetPopup.SetActive(true);
    }

    public void OnCancelReset()
    {
        resetPopup.SetActive(false);
    }

    public void OnConfirmReset()
    {
        bool success =
            rouletteController.ResetRouletteWithPvpCurrency();

        if (success)
        {
            resetPopup.SetActive(false);
        }
    }
}