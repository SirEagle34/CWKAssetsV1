using UnityEngine;

public class PVPExtremeUI : MonoBehaviour
{
    public UILabel titleLabel;
    public UILabel[] listLabels;

    public void Setup(PVPExtremeData data)
    {
        if (data == null)
        {
            Debug.LogError("PVPExtremeData NULL!");
            return;
        }

        if (titleLabel != null)
            titleLabel.text = data.ExtremeListLabel;

        if (listLabels != null && listLabels.Length >= 7)
        {
            if (listLabels[0] != null) listLabels[0].text = data.ExtremeList;
            if (listLabels[1] != null) listLabels[1].text = data.ExtremeList2;
            if (listLabels[2] != null) listLabels[2].text = data.ExtremeList3;
            if (listLabels[3] != null) listLabels[3].text = data.ExtremeList4;
            if (listLabels[4] != null) listLabels[4].text = data.ExtremeList5;
            if (listLabels[5] != null) listLabels[5].text = data.ExtremeList6;
            if (listLabels[6] != null) listLabels[6].text = data.ExtremeList7;
        }
    }

    public void OpenScreen()
    {
        gameObject.SetActive(true);

        var data = PVPExtremeDataManager.Instance.GetData("ExtremeCreaturesID");
        Setup(data); // 🔥 direkt kendini çağır
    }
}