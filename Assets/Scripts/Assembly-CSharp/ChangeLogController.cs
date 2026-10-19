using UnityEngine;

public class ChangeLogController
{
    public void OpenLatest()
    {
        var latest = ChangeLogDataManager.Instance.GetLatest();

        if (latest == null)
            return;

        ChangeLogPopupUI.Instance.Show(latest);
    }

    public void Close()
    {
        ChangeLogPopupUI.Instance.Hide();
    }

    public void ToggleLatest()
    {
        var latest = ChangeLogDataManager.Instance.GetLatest();

        if (latest == null)
            return;
    }
}